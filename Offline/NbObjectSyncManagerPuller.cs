using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    // オブジェクト同期: Pull
    public partial class NbObjectSyncManager
    {
        /// <summary>
        /// Pullオフセット時間 (秒)
        /// </summary>
        internal static readonly int PullTimeOffsetSeconds = 3;

        /// <summary>
        /// Pull分割数
        /// </summary>
        internal static readonly int PullDivideNumber = 500;

        /// <summary>
        /// Pull。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="query">同期範囲</param>
        /// <param name="resolver">衝突解決レゾルバ</param>
        /// <returns>更新(INSERT/UPDATE/DELETE)されたオブジェクト数</returns>
        /// <remarks>引数はコール元でチェックすること</remarks>
        internal virtual async Task<int> Pull(string bucketName, NbQuery query, NbObjectConflictResolver.Resolver resolver)
        {
            // ensure cache table
            _objectCache.CreateCacheTable(bucketName);

            // queryに前回同期時刻を加味した条件を付与する
            var pullQuery = GetFirstQuery(bucketName, query);

            // バケットインスタンス生成
            var bucket = GetObjectBucket(bucketName);

            // 更新オブジェクト累計
            var pullTotalUpdatedCount = 0;
            // PullUpdate待機用Task
            Task<int> pullUpdateTask = null;
            // 初回Pull時刻
            string firstPullTime = null;

            Debug.WriteLine("[--Pull start--] " + bucketName + " argument Query: " + query.ToString());

            // 初回は必ずPull実行する
            // 分割Pullの結果が分割数に満たない場合、新規クエリを生成せず終了
            while (pullQuery != null)
            {
                Debug.WriteLine("[Pull] start : Condition: " + pullQuery.ToString());
                // Pull実行
                var pullObjectsResults = await bucket.QueryWithOptionsAsync(pullQuery);
                Debug.WriteLine("[Pull] finished : ObjectCount: " + pullObjectsResults.Objects.Count()
                    + " ServerTime: " + pullObjectsResults.CurrentTime);

                // 初回の分割Pull時刻を保持
                if (firstPullTime == null)
                {
                    firstPullTime = pullObjectsResults.CurrentTime;
                }

                // 初回を除き、前回のPullUpdateの完了を待機
                pullTotalUpdatedCount += await WaitPullUpdate(pullUpdateTask);

                // PullUpdateを非同期実行
                pullUpdateTask = Task.Run(() => PullUpdate(bucketName, pullObjectsResults.Objects, resolver));

                // 次回Pullの条件を生成
                pullQuery = GetSecondOrLaterQuery(query, pullObjectsResults.Objects);
            }

            // 最終のPullUpdateを待機
            pullTotalUpdatedCount += await WaitPullUpdate(pullUpdateTask);

            // 初回Pull時のサーバ時刻を保存
            SetObjectBucketCacheData(bucketName, LastPullServerTime, firstPullTime);

            Debug.WriteLine("[--Pull finished--] TotalUpdatedCount: " + pullTotalUpdatedCount + " firstPullTime: " + firstPullTime);

            return pullTotalUpdatedCount;
        }

        /// <summary>
        /// 初回分割Pullに使用する条件を生成する
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="baseQuery">ユーザ指定のクエリ</param>
        /// <returns>Pull条件を設定したクエリ</returns>
        internal virtual NbQuery GetFirstQuery(string bucketName, NbQuery baseQuery)
        {
            var pullQuery = new NbQuery();

            // where
            // 前回Pullサーバ時刻を取得
            var lastUpdatedAt = GetObjectBucketCacheData(bucketName, LastPullServerTime);
            if (lastUpdatedAt != null)
            {
                // 取りこぼしを防止するため、時刻条件を
                // 「前回 Pull 時のサーバ時刻 - オフセット時間」にする。
                var dt = NbDateUtils.ParseDateTime(lastUpdatedAt).AddSeconds(-PullTimeOffsetSeconds);
                lastUpdatedAt = NbDateUtils.ToString(dt);
                pullQuery.GreaterThanOrEqual(Field.UpdatedAt, lastUpdatedAt);
                // ユーザ指定の条件と、前回時刻を加味したwhere条件を作成
                pullQuery = NbQuery.And(pullQuery, baseQuery);
            }
            else
            {
                // ユーザ指定の条件のみ付与
                pullQuery.Conditions = baseQuery.Conditions;
            }

            // where以外のパラメータに固定値を設定
            SetFixedQueryParameters(pullQuery);

            return pullQuery;
        }

        /// <summary>
        /// 分割Pull2回目以降のPull条件を生成する
        /// </summary>
        /// <param name="baseQuery">ユーザ定義のクエリ</param>
        /// <param name="objects">Pullで取得したオブジェクト一覧</param>
        /// <returns>Pull条件を設定したクエリ</returns>
        /// <remarks>分割Pullの継続不要な場合、返り値はnullとする。</remarks>
        internal virtual NbQuery GetSecondOrLaterQuery(NbQuery baseQuery, IEnumerable<NbObject> objects)
        {
            NbQuery pullQuery = null;
            // where条件の設定
            var objectNumber = objects.Count();

            // 分割数と等しいオブジェクトが取得できた場合、分割Pullを継続する。
            // (fail safe: "より大きい"を含めて判定)
            if (objectNumber >= PullDivideNumber)
            {
                // リスト末尾のオブジェクトのUpdatedAt/Idを取得
                var lastObject = objects.Last();
                var lastUpdatedAt = lastObject[Field.UpdatedAt];
                var lastObjectId = lastObject[Field.Id];

                // 以下の条件を生成(LO=末尾のオブジェクト)
                //  [1]ユーザ定義の条件 && 
                // ([2]UpdatedAtがLOの最終更新日時より大きい || ([3]UpdatedAtがLOと等しい && [4]IdがLOより大きい))
                // 条件[2]を生成
                var greaterUpdatedAtCondition = new NbQuery().GreaterThan(Field.UpdatedAt, lastUpdatedAt);
                // 条件[3]を生成
                var equalsUpdatedAtCondition = new NbQuery().EqualTo(Field.UpdatedAt, lastUpdatedAt);
                // 条件[4]を生成
                var greaterIdCondition = new NbQuery().GreaterThan(Field.Id, lastObjectId);

                // [2]||([3]&&[4])の条件を生成
                var additionalCondition = NbQuery.Or(greaterUpdatedAtCondition, NbQuery.And(equalsUpdatedAtCondition, greaterIdCondition));

                // [1]&&([2]||([3]&&[4]))の条件を生成
                pullQuery = NbQuery.And(baseQuery, additionalCondition);

                // where以外のパラメータに固定値を設定
                SetFixedQueryParameters(pullQuery);
            }

            return pullQuery;
        }

        /// <summary>
        /// Where以外の固定のPull条件を設定する
        /// </summary>
        /// <param name="query">条件設定を行うクエリ</param>
        internal virtual void SetFixedQueryParameters(NbQuery query)
        {
            // where条件以外のプロパティは固定値
            // 各値を明示的に設定
            query.ProjectionValue = null; // projectionは使用しない
            // order
            query.OrderBy(Field.UpdatedAt, Field.Id);
            // limit
            query.Limit(PullDivideNumber);
            // skip
            query.Skip(-1); // パラメータとして不要のため無効値を設定
            // deleteMark
            query.DeleteMark(true);
        }

        /// <summary>
        /// PullUpdateを行う
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="objects">保存対象のオブジェクト一覧</param>
        /// <param name="resolver">リゾルバ</param>
        /// <returns>更新を行ったオブジェクト数</returns>
        internal virtual int PullUpdate(string bucketName, IEnumerable<NbObject> objects, NbObjectConflictResolver.Resolver resolver)
        {
            Debug.WriteLine("[PullUpdate] start : " + objects.Count());

            var database = (NbDatabaseImpl)Service.OfflineService.Database;
            using (var transaction = database.BeginTransaction())
            {
                try
                {
                    var updateCount = PullProcessResults(bucketName, objects, resolver);

                    transaction.Commit();

                    Debug.WriteLine("[PullUpdate] finished : Updated " + updateCount);
                    return updateCount;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 指定タスクの完了を待って、PullUpdateの結果を返却する
        /// </summary>
        /// <param name="task">Wait対象のタスク</param>
        /// <returns>PullUpdateの結果</returns>
        /// <remarks><see paramref="task"/>がnullの場合は何もしない。</remarks>
        internal virtual async Task<int> WaitPullUpdate(Task<int> task)
        {
            if (task == null)
            {
                return 0;
            }

            var result = await task;
            return result;
        }

        /// <summary>
        /// Pull : サーバデータ処理
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="serverObjects">サーバーからの取得したオブジェクト一覧</param>
        /// <param name="resolver">衝突解決リゾルバ</param>
        /// <returns>更新を行ったオブジェクト数</returns>
        internal virtual int PullProcessResults(string bucketName, IEnumerable<NbObject> serverObjects,
            NbObjectConflictResolver.Resolver resolver)
        {
            int updateCount = 0;

            // 取得したオブジェクトを順次書き込み
            foreach (var server in serverObjects)
            {
                var client = _objectCache.FindObject<NbOfflineObject>(bucketName, server.Id);

                if (client == null)
                {
                    // 衝突なし。
                    // サーバ削除データでなければ、ローカル保存。
                    if (!server.Deleted)
                    {
                        _objectCache.InsertObject(server, NbSyncState.Sync);
                        updateCount++;
                    }
                }
                else if (server.Etag == client.Etag)
                {
                    // ETag 変更なし、重複ダウンロード。無視。
                }
                else if (client.SyncState == NbSyncState.Sync)
                {
                    // サーバ変更。上書き。
                    if (!server.Deleted)
                    {
                        _objectCache.UpdateObject(server, NbSyncState.Sync);
                    }
                    else
                    {
                        _objectCache.DeleteObject(client);
                    }
                    updateCount++;
                }
                else
                {
                    // 衝突解決
                    HandleConflict(server, client, resolver);
                    updateCount++;
                }
            }
            return updateCount;
        }
    }
}
