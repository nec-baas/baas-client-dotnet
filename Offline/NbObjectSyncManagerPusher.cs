using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    // オブジェクト同期: Push
    public partial class NbObjectSyncManager
    {
        /// <summary>
        /// Push分割数 (999以下であること)
        /// </summary>
        internal static readonly int PushDivideNumber = 500;

        /// <summary>
        /// Push。
        /// </summary>
        /// <remarks>
        /// 衝突が発生した場合、衝突解決したオブジェクトは次回Pushまでサーバには送信されない。
        /// </remarks>
        /// <param name="bucketName">バケット名</param>
        /// <param name="resolver">衝突解決リゾルバ</param>
        /// <returns>失敗したバッチ結果の一覧を返す</returns>
        /// <remarks>引数はコール元でチェックすること</remarks>
        internal virtual async Task<IList<NbBatchResult>> Push(string bucketName, NbObjectConflictResolver.Resolver resolver)
        {
            // ensure cache table
            _objectCache.CreateCacheTable(bucketName);

            // Push対象となるオブジェクトのId一覧を取得する
            IEnumerable<string> wholeObjectsIds = _objectCache.QueryDirtyObjectIds(bucketName);

            // PushUpdate待機用Task
            Task<IList<NbBatchResult>> pushUpdateTask = null;
            // PushUpdate失敗結果一覧
            var failedResults = new List<NbBatchResult>();

            Debug.WriteLine("[--Push Start--] " + bucketName + " " + wholeObjectsIds.Count());

            // 分割Push,PushUpdate開始
            while (wholeObjectsIds.Count() > 0)
            {
                // 全Push対象オブジェクトから、分割数のオブジェクトIdを取得
                var targetObjectIds = wholeObjectsIds.Take(PushDivideNumber);
                // 管理リストから取得済みのObjectIdを削除
                wholeObjectsIds = wholeObjectsIds.Skip(PushDivideNumber);

                var targetObjects = _objectCache.QueryObjectsWithIds(bucketName, targetObjectIds);

                if (targetObjects.Count == 0)
                {
                    // fail safe 分割同期処理中のDelete(物理削除)操作により発生しうるが、マルチスレッド制御により禁止される
                    // 指定のオブジェクトが全く取得できない場合はskip
                    // 一部でも取得できればPushを行う
                    continue;
                }

                // バッチリクエスト実行
                var batch = CreatePushRequest(targetObjects);
                var bucket = GetObjectBucket(bucketName);
                Debug.WriteLine("[Push] start : " + targetObjects.Count);
                var batchResults = await bucket.BatchAsync(batch, true);
                Debug.WriteLine("[Push] finished : " + batchResults.Count + " Left: " + wholeObjectsIds.Count());

                // 初回を除き、PushUpdateのタスク完了を待機
                await WaitPushUpdate(pushUpdateTask, failedResults);

                // PushUpdateを非同期実行開始。待機せず次のPush処理に移行。
                // 最終のPushUpdateは、待機せずにループを抜ける
                pushUpdateTask = Task.Run(() => PushUpdate(bucketName, batch, batchResults, resolver));
            }
            // 最終Pushに対するPushUpdate完了を待機
            await WaitPushUpdate(pushUpdateTask, failedResults);

            // Pushに失敗が無かった場合、同期完了日時を更新
            if (failedResults.Count == 0)
            {
                UpdateLastSyncTime(bucketName);
            }

            Debug.WriteLine("[--Push finished--] TotalFailed: " + failedResults.Count);

            return failedResults;
        }

        /// <summary>
        /// 最終同期完了日時を現在時刻に更新
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        internal virtual void UpdateLastSyncTime(string bucketName)
        {
            var currentUtcTime = GetUtcNow();
            var utcTimeString = NbDateUtils.ToString(currentUtcTime);
            SetObjectBucketCacheData(bucketName, LastSyncTime, utcTimeString);
        }

        /// <summary>
        /// 現在のUTC時刻取得のラッパー
        /// </summary>
        /// <returns>UTCの現在時刻</returns>
        internal virtual DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// バッチリクエスト生成処理
        /// </summary>
        /// <param name="objects">Push対象のオブジェクト一覧</param>
        /// <returns>リクエスト<br/>オブジェクトが指定されない場合、空のリクエストを返却する</returns>
        internal virtual NbBatchRequest CreatePushRequest(IEnumerable<NbOfflineObject> objects)
        {
            var request = new NbBatchRequest();
            foreach (var obj in objects)
            {
                if (obj.Deleted)
                {
                    // DELETE
                    request.AddDeleteRequest(obj);
                }
                else if (obj.Etag == null)
                {
                    // INSERT
                    request.AddInsertRequest(obj);
                }
                else
                {
                    // UPDATE
                    request.AddUpdateRequest(obj);
                }
            }

            return request;
        }

        internal virtual NbObjectBucket<NbObject> GetObjectBucket(string bucketName)
        {
            return new NbObjectBucket<NbObject>(bucketName, Service);
        }

        /// <summary>
        /// 指定タスクの完了を待って、PushUpdateの結果をリストに格納する
        /// </summary>
        /// <param name="task">Wait対象のタスク</param>
        /// <param name="failedResults">PushUpdateの処理結果</param>
        /// <remarks><see paramref="task"/>がnullの場合は何もしない。</remarks>
        internal virtual async Task WaitPushUpdate(Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults)
        {
            if (task == null)
            {
                return;
            }

            var result = await task;
            failedResults.AddRange(result);
        }

        /// <summary>
        /// Pushバッチ結果処理反映のラッパー<br/>
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="batch">バッチリクエスト</param>
        /// <param name="results">バッチ処理結果</param>
        /// <param name="resolver">衝突解決リゾルバ</param>
        /// <returns>失敗結果一覧</returns>
        internal virtual IList<NbBatchResult> PushUpdate(string bucketName, NbBatchRequest batch,
            IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver)
        {
            Debug.WriteLine("[PushUpdate] start : " + results.Count);
            var database = (NbDatabaseImpl)Service.OfflineService.Database;
            using (var transaction = database.BeginTransaction())
            {
                try
                {
                    var failedResults = PushProcessResults(bucketName, batch, results, resolver);
                    transaction.Commit();
                    Debug.WriteLine("[PushUpdate] finished : " + failedResults.Count + " failed.");
                    return failedResults;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Pushバッチ結果処理
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="batch">バッチリクエスト</param>
        /// <param name="results">バッチ処理結果</param>
        /// <param name="resolver">衝突解決リゾルバ</param>
        /// <returns>失敗結果一覧</returns>
        internal virtual IList<NbBatchResult> PushProcessResults(string bucketName, NbBatchRequest batch,
            IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver)
        {
            int i = -1;
            var failedResults = new List<NbBatchResult>();

            foreach (var result in results)
            {
                i++;
                var op = batch.GetOp(i);

                // fail safe
                // 通常はserverが通知したIdを使用する
                // insertに失敗したケース等Id未採番の場合、reuqestからIdを復元する
                if (result.Id == null)
                {
                    result.Id = GetObjectIdFromBatchRequest(i, batch);
                }

                var obj = _objectCache.FindObject<NbOfflineObject>(bucketName, result.Id);
                if (obj == null)
                {
                    // fail safe
                    // 同期中のDelete(物理削除)操作により発生しうるが、マルチスレッド制御により禁止される
                    continue;
                }

                switch (result.Result)
                {
                    case NbBatchResult.ResultCode.Ok:
                        switch (op)
                        {
                            case NbBatchRequest.OpInsert:
                            case NbBatchRequest.OpUpdate:
                                obj.FromJson(result.Data);
                                obj.Etag = result.Etag;
                                obj.UpdatedAt = result.UpdatedAt;
                                _objectCache.UpdateObject(obj, NbSyncState.Sync);
                                break;

                            case NbBatchRequest.OpDelete:
                                _objectCache.DeleteObject(obj);
                                break;
                            default:
                                // fail safe: SDK内でリクエストを生成するため発生しない
                                // 無視する
                                break;
                        }
                        break;

                    case NbBatchResult.ResultCode.NotFound:
                        if (op == NbBatchRequest.OpDelete)
                        {
                            // すでにサーバデータ削除済み
                            _objectCache.DeleteObject(obj);
                        }
                        else
                        {
                            failedResults.Add(result);
                        }
                        break;

                    case NbBatchResult.ResultCode.Conflict:
                        // サーバからデータが通知されないケースでは競合解決不可のため失敗扱いとする
                        if (result.Data == null)
                        {
                            failedResults.Add(result);
                            break;
                        }
                        // Clientオブジェクト
                        var client = obj;
                        // Serverオブジェクト復元
                        var server = new NbObject(bucketName, Service);
                        server.FromJson(result.Data);
                        server.Etag = result.Etag;
                        server.UpdatedAt = result.UpdatedAt;
                        // 衝突解決処理
                        if (HandleConflict(server, client, resolver) == client)
                        {
                            // Push時にクライアント優先で解決した場合は、アプリ側に衝突通知する。
                            // これは未同期オブジェクト(dirty状態)がまだ残っていることを通知する必要があるため。
                            failedResults.Add(result);
                        }
                        break;

                    default: // forbidden, badRequest, serverError
                        failedResults.Add(result);
                        break;
                }
            }
            return failedResults;
        }

        /// <summary>
        /// バッチ要求から、ObjectIdを取得する
        /// </summary>
        /// <param name="index">リクエストのindex</param>
        /// <param name="request">バッチ要求</param>
        /// <returns>取得したId</returns>
        /// <remarks>取得に失敗した場合はnullを返却する</remarks>
        internal static string GetObjectIdFromBatchRequest(int index, NbBatchRequest request)
        {
            string result = null;

            var jsonObject = (NbJsonObject)request.Requests[index];
            var jsonData = jsonObject.Opt<NbJsonObject>(NbBatchRequest.KeyData, null);
            if (jsonData != null)
            {
                result = jsonData.Opt<string>(Field.Id, null);
            }
            return result;
        }

    }
}
