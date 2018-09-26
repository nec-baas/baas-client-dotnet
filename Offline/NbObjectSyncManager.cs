using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// オブジェクトストレージ同期
    /// </summary>
    public partial class NbObjectSyncManager
    {
        internal NbService Service { get; private set; }

        private readonly NbObjectCache _objectCache;
        private readonly ProcessState _processState;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="service">サービス</param>
        /// <exception cref="InvalidOperationException">オフラインサービスが無効</exception>
        public NbObjectSyncManager(NbService service = null)
        {
            Service = service ?? NbService.Singleton;

            if (!Service.IsOfflineEnabled())
            {
                throw new InvalidOperationException("Offline service is not enabled");
            }

            _objectCache = new NbObjectCache(Service);
            _processState = ProcessState.GetInstance();
        }

        /// <summary>
        /// 同期を実行する。
        /// </summary>
        /// <remarks>
        /// 同期範囲を変更した場合、新たな同期範囲でオブジェクトを取得し直す。
        /// </remarks>
        /// <remarks>
        /// 衝突が発生した場合、衝突解決したオブジェクトは次回Pushまでサーバには送信されない。<br/>
        /// <paramref name="resolver"/>がnullの場合、サーバ優先とみなす。<br/>
        /// 同期に失敗したオブジェクトが無い場合、空のリストを返却する。
        /// </remarks>
        /// <param name="bucketName">バケット名</param>
        /// <param name="resolver">衝突解決リゾルバ</param>
        /// <returns>同期に失敗したオブジェクト一覧</returns>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        /// <exception cref="InvalidOperationException">指定バケットの同期範囲が未設定</exception>
        /// <exception cref="NbException">他の同期が処理中</exception>
        public async Task<IList<NbBatchResult>> SyncBucketAsync(string bucketName, NbObjectConflictResolver.Resolver resolver = null)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");

            if (!_processState.TryStartSync()) NbUtil.ThrowLockedException();

            IList<NbBatchResult> result = new List<NbBatchResult>();
            try
            {
                // 同期範囲取得
                var syncScope = GetSyncScope(bucketName);
                NbUtil.NotNullWithInvalidOperation(syncScope, "No syncScope");
                // リゾルバ未指定の場合はサーバ優先とみなす
                resolver = resolver ?? NbObjectConflictResolver.PreferServerResolver;

                await Pull(bucketName, syncScope, resolver);
                result = await Push(bucketName, resolver);
            }
            finally
            {
                _processState.EndSync();
            }

            return result;
        }

        /// <summary>
        /// 衝突解決を実行する。
        /// サーバ選択時は、サーバデータで更新(Sync状態へ)、サーバ削除時はそのままクライアント削除。
        /// クライアント選択時は、ETag のみコピーして Dirty 状態のまま。
        /// </summary>
        /// <param name="server">サーバオブジェクト</param>
        /// <param name="client">クライアントオブジェクト</param>
        /// <param name="resolver">リゾルバ</param>
        /// <returns>選択したオブジェクト</returns>
        internal virtual NbObject HandleConflict(NbObject server, NbObject client, NbObjectConflictResolver.Resolver resolver)
        {
            var resolved = resolver(server, client);
            if (resolved == server)
            {
                // サーバ選択。
                if (!server.Deleted)
                {
                    _objectCache.UpdateObject(resolved, NbSyncState.Sync);
                }
                else
                {
                    _objectCache.DeleteObject(server);
                }
                return server;
            }
            else if (resolved == client)
            {
                // クライアント選択
                // ETag をコピー
                client.Etag = server.Etag;

                _objectCache.UpdateObject(client, NbSyncState.Dirty);
                return client;
            }
            else
            {
                throw new InvalidOperationException("Resolver must returns client or server instance");
            }
        }

        /// <summary>
        /// ObjectBucketCacheDaoを生成する。
        /// </summary>
        /// <param name="context">DBContext</param>
        /// <returns>ObjectBucketCacheDao</returns>
        internal virtual ObjectBucketCacheDao CreateCacheDao(NbManageDbContext context)
        {
            return new ObjectBucketCacheDao(context);
        }
    }
}
