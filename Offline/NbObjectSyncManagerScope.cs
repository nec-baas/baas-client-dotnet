using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula
{
    public partial class NbObjectSyncManager
    {
        // メソッド識別用定数
        private const string SyncScope = "SyncScope";
        private const string LastPullServerTime = "LastPullServerTime";
        private const string LastSyncTime = "LastSyncTime";

        /// <summary>
        /// 同期範囲の設定。<br/>
        /// 同期を行う場合、対象のバケットへの同期範囲設定が必要。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="scope">同期範囲</param>
        /// <remarks>
        /// 同期範囲未指定の場合、空のクエリが設定されたとみなす。<br/>
        /// 同期範囲は<see cref="NbQuery"/>の<see cref="NbQuery.Conditions"/>のみ参照する。他のパラメータは無効である。<br/>
        /// 同期範囲を変更した場合、前回同期時刻は破棄する。<br/>
        /// 同期中の同期範囲の設定はしないこと。
        /// </remarks>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public void SetSyncScope(string bucketName, NbQuery scope = null)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");

            // scopeがnullの場合は空のクエリが設定されたとみなす
            scope = scope ?? new NbQuery();

            SetObjectBucketCacheData(bucketName, SyncScope, scope.ToString());
            // 同期範囲変更後は、時刻情報を削除する
            SetObjectBucketCacheData(bucketName, LastPullServerTime, null);
            SetObjectBucketCacheData(bucketName, LastSyncTime, null);
        }

        /// <summary>
        /// 同期範囲の取得。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>同期範囲</returns>
        /// <remarks>
        /// 同期範囲未設定の場合、nullを返却する。
        /// </remarks>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public NbQuery GetSyncScope(string bucketName)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");

            NbQuery query = null;
            var json = GetObjectBucketCacheData(bucketName, SyncScope);
            if (json != null)
            {
                // DBのJSON化したクエリから、NbQueryを復元
                query = NbQuery.FromJSONString(json);
            }
            return query;
        }

        /// <summary>
        /// 同期範囲を削除。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <remarks>
        /// 同期範囲を削除した場合、前回同期時刻は破棄する。<br/>
        /// 同期中の同期範囲の削除はしないこと。
        /// </remarks>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public void RemoveSyncScope(string bucketName)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");

            SetObjectBucketCacheData(bucketName, SyncScope, null);
            // 同期範囲変更後は、時刻情報を削除する
            SetObjectBucketCacheData(bucketName, LastPullServerTime, null);
            SetObjectBucketCacheData(bucketName, LastSyncTime, null);
        }

        /// <summary>
        /// 設定されている同期範囲の一覧を返却。
        /// </summary>
        /// <returns>バケット名と同期範囲のディクショナリ</returns>
        /// <remarks>
        /// 同期範囲が設定されていない場合、空のディクショナリを返却する。
        /// </remarks>
        public Dictionary<string, NbQuery> GetAllSyncScopes()
        {
            var database = (NbDatabaseImpl)Service.OfflineService.Database;
            using (var dbContext = database.CreateDbContext())
            {
                var dao = CreateCacheDao(dbContext);
                // バケットキャッシュ全体を取得
                var bucketCaches = dao.FindAll();
                // 同期範囲設定済みのキャッシュを抽出し、バケット名と、同期範囲をクエリに変換した結果を出力
                return (from x in bucketCaches where x.SyncScope != null select x)
                    .ToDictionary(cache => cache.Name, cache => NbQuery.FromJSONString(cache.SyncScope));
            }
        }

        /// <summary>
        /// 最終同期時刻を返却する。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>最終同期完了時刻</returns>
        /// <remarks>
        /// 同期完了時刻が未保存の場合、nullを返却する。
        /// </remarks>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public DateTime? GetLastSyncTime(string bucketName)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");

            DateTime? time = null;

            var json = GetObjectBucketCacheData(bucketName, LastSyncTime);
            if (json != null)
            {
                time = NbDateUtils.ParseDateTime(json);
            }

            return time;
        }

        /// <summary>
        /// DB保存操作共通処理
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="method">メソッド種別</param>
        /// <param name="value">保存する文字列</param>
        /// <returns>データベースに書き込まれたオブジェクト数</returns>
        internal virtual int SetObjectBucketCacheData(string bucketName, string method, string value)
        {
            int result = 0;
            // ensure cache table
            _objectCache.CreateCacheTable(bucketName);

            var database = (NbDatabaseImpl)Service.OfflineService.Database;
            using (var dbContext = database.CreateDbContext())
            {
                var dao = CreateCacheDao(dbContext);

                using (var transaction = database.BeginTransaction())
                {
                    try
                    {
                        switch (method)
                        {
                            case SyncScope:
                                result = dao.SaveSyncScope(bucketName, value);
                                break;
                            case LastPullServerTime:
                                result = dao.SaveLastPullServerTime(bucketName, value);
                                break;
                            case LastSyncTime:
                                result = dao.SaveLastSyncTime(bucketName, value);
                                break;
                            default:
                                throw new InvalidOperationException("method: " + method + " is undefined.");
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// DB読み出し処理共通処理
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="method">メソッド種別</param>
        /// <returns>読み出した文字列</returns>
        internal virtual string GetObjectBucketCacheData(string bucketName, string method)
        {
            string result = null;
            // ensure cache table
            _objectCache.CreateCacheTable(bucketName);

            var database = (NbDatabaseImpl)Service.OfflineService.Database;
            using (var dbContext = database.CreateDbContext())
            {
                var dao = CreateCacheDao(dbContext);

                switch (method)
                {
                    case SyncScope:
                        result = dao.GetSyncScope(bucketName);
                        break;
                    case LastPullServerTime:
                        result = dao.GetLastPullServerTime(bucketName);
                        break;
                    case LastSyncTime:
                        result = dao.GetLastSyncTime(bucketName);
                        break;
                    default:
                        throw new InvalidOperationException("method: " + method + " is undefined.");
                }
            }

            return result;
        }
    }
}
