﻿using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// オフラインバケット
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    /// <typeparam name="T">NbOfflineObject及びそのサブクラス</typeparam>
    public class NbOfflineObjectBucket<T> : NbObjectBucketBase<T> where T : NbOfflineObject, new()
    {
        private readonly NbObjectCache _cache;
        private readonly ProcessState _processState;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="service">サービス</param>
        /// <exception cref="ArgumentException">バケット名が特定できない</exception>
        /// <exception cref="InvalidOperationException">オフラインサービスが無効</exception>
        public NbOfflineObjectBucket(string bucketName = null, NbService service = null)
            : base(bucketName, service)
        {
            if (!Service.IsOfflineEnabled())
            {
                throw new InvalidOperationException("Offline service is not enabled");
            }
            _cache = new NbObjectCache(Service);
            _cache.CreateCacheTable(BucketName);
            _processState = ProcessState.GetInstance();
        }

        /// <summary>
        /// オブジェクトを生成する
        /// </summary>
        /// <returns>新規オブジェクト</returns>
        public override T NewObject()
        {
            var obj = new T();
            obj.Init(BucketName, Service);
            return obj;
        }

        /// <summary>
        /// オフラインオブジェクトを検索する
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <returns>オブジェクト検索結果</returns>
        /// <remarks>
        /// <paramref name="query"/>が未指定の場合、空のクエリが指定されたとみなす。<br/>
        /// オフラインオブジェクトのクエリでは、プロジェクションをサポートしない。
        /// </remarks>
        public override Task<IEnumerable<T>> QueryAsync(NbQuery query)
        {
            // パラメータ未設定の場合は、空のクエリが指定されたとみなす
            if (query == null)
            {
                query = new NbQuery();
            }

            var objects = _cache.MongoQueryObjects<T>(BucketName, query, true, NbUser.CurrentUser(Service));
            return Task.FromResult(objects);
        }

        /// <summary>
        /// オフラインバケットに対する オブジェクト検索(カウント付き)はサポートしない。
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <param name="queryCount">ヒット件数を取得する場合はtrue</param>
        /// <returns>オブジェクト検索結果</returns>
        /// <exception cref="NotSupportedException">未サポート</exception>
        public override Task<NbObjectQueryResult<T>> QueryWithOptionsAsync(NbQuery query, bool queryCount = false)
        {
            throw new NotSupportedException("QueryWithOptionsAsync for offline bucket is not supported.");
        }

        /// <summary>
        /// オブジェクトID検索
        /// </summary>
        /// <param name="objectId">オブジェクトID</param>
        /// <returns>オブジェクト</returns>
        /// <exception cref="ArgumentNullException">オブジェクトIDがnull</exception>
        /// <exception cref="NbHttpException">
        /// <list type="table">
        ///   <item>
        ///     <term><see cref="HttpStatusCode.Forbidden"/></term>
        ///     <description>ACLの権限不正</description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpStatusCode.NotFound"/></term>
        ///     <description>取得対象のオブジェクトが存在しない、または削除済み</description>
        ///   </item>
        /// </list>
        /// </exception>
        public override Task<T> GetAsync(string objectId)
        {
            NbUtil.NotNullWithArgument(objectId, "objectId");

            var obj = _cache.FindObject<T>(BucketName, objectId);

            // DBにオブジェクトが保存されていない、もしくは論理削除済みオブジェクトの場合
            // オンラインと同様に、ACLのチェック前に、Deletedの判定を行う
            if (obj == null || obj.Deleted)
            {
                throw new NbHttpException(HttpStatusCode.NotFound, "No such object");
            }
            // ACLチェック
            if (!NbUser.IsAclAccessibleForRead(NbUser.CurrentUser(Service), obj.Acl))
            {
                throw new NbHttpException(HttpStatusCode.Forbidden, "No ACL");
            }

            return Task.FromResult(obj);
        }

        /// <summary>
        /// オブジェクトの一括削除。
        /// 読み込み・削除権限がないオブジェクトは削除されない。
        /// </summary>
        /// <param name="query">削除条件。クエリのConditionを適用する。</param>
        /// <param name="softDelete">論理削除する場合は true (デフォルトは true)</param>
        /// <returns>削除した件数</returns>
        /// <exception cref="ArgumentNullException">クエリがnull</exception>
        /// <exception cref="NbException">同期処理中</exception>
        public override Task<int> DeleteAsync(NbQuery query, bool softDelete = true)
        {
            NbUtil.NotNullWithArgument(query, "query");

            // Where条件のみを抽出したクエリを生成
            var conditionsQuery = new NbQuery();
            conditionsQuery.Conditions = query.Conditions;

            var database = (NbDatabaseImpl)Service.OfflineService.Database;
            int result = 0;

            if (!_processState.TryStartCrud()) NbUtil.ThrowLockedException();

            try
            {
                using (var transaction = database.BeginTransaction())
                {

                    var currentUser = NbUser.CurrentUser(Service);
                    var objects = _cache.MongoQueryObjects<NbOfflineObject>(BucketName, conditionsQuery, true, currentUser);

                    foreach (var obj in objects)
                    {
                        if (!NbUser.IsAclAccessibleForDelete(currentUser, obj.Acl))
                        {
                            // No ACL
                            continue;
                        }

                        if (softDelete)
                        {
                            obj.Deleted = true;
                            result += _cache.UpdateObject(obj, NbSyncState.Dirty);
                        }
                        else
                        {
                            result += _cache.DeleteObject(obj);
                        }
                    }
                    // Commit
                    transaction.Commit();

                }
            }
            finally
            {
                _processState.EndCrud();
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// オフラインバケットに対するAggregationはサポートしない。
        /// </summary>
        /// <param name="pipeline">Aggregation Pipeline JSON配列</param>
        /// <param name="options">オプション</param>
        /// <returns>Aggregation 実行結果</returns>
        /// <exception cref="NotSupportedException">未サポート</exception>
        public override Task<NbJsonArray> AggregateAsync(NbJsonArray pipeline, NbJsonObject options = null)
        {
            throw new NotSupportedException("AggregateAsync for offline bucket is not supported.");
        }

        /// <summary>
        /// オフラインバケットに対するバッチリクエストはサポートしない。
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <param name="softDelete">論理削除を行う場合は true (デフォルトは true)</param>
        /// <returns>バッチ応答のリスト</returns>
        /// <exception cref="NotSupportedException">未サポート</exception>
        public override Task<IList<NbBatchResult>> BatchAsync(NbBatchRequest request, bool softDelete = true)
        {
            throw new NotSupportedException("BatchAsync for offline bucket is not supported.");
        }

        internal static void DeleteAll()
        {
            var database = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            var processState = ProcessState.GetInstance();

            if (!processState.TryStartCrud()) NbUtil.ThrowLockedException();

            try
            {
                using (var transaction = database.BeginTransaction())
                {
                    using (var dbContext = database.CreateDbContext())
                    {
                        // Delete All Object Cache tables
                        var _cache = new NbObjectCache(NbService.Singleton);
                        var tables = _cache.GetTables();
                        foreach (var table in tables)
                        {
                            _cache.DeleteCacheTable(table.Substring("OBJECT_".Length));
                        }

                        // Delete All Bucket Caches
                        var bucketdao = new ObjectBucketCacheDao(dbContext);
                        foreach (var bucketcache in bucketdao.FindAll())
                        {
                            bucketdao.Remove(bucketcache);
                        }
                        bucketdao.SaveChanges();
                    }
                    transaction.Commit();
                }
            }
            finally
            {
                processState.EndCrud();
            }
        }
    }
}
