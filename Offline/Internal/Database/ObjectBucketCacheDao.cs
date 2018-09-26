using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// オブジェクトバケットキャッシュ DAO
    /// </summary>
    internal class ObjectBucketCacheDao
    {
        private readonly NbManageDbContext _context;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context">NbManageDbContext</param>
        /// <exception cref="ArgumentNullException">contextがnull</exception>
        public ObjectBucketCacheDao(NbManageDbContext context)
        {
            NbUtil.NotNullWithArgument(context, "context");
            _context = context;
        }

        /// <summary>
        /// バケット名で検索
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns></returns>
        public virtual ObjectBucketCache FindByName(string bucketName)
        {
            var result = from x in _context.ObjectBucketCaches where x.Name == bucketName select x;
            return result.Any() ? result.First() : null;
        }

        /// <summary>
        /// 最終Pullサーバ日時を取得
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>最終Pullサーバ日時</returns>
        public virtual string GetLastPullServerTime(string bucketName)
        {
            var result = from x in _context.ObjectBucketCaches where x.Name == bucketName select x.LastPullServerTime;
            return result.Any() ? result.First() : null;
        }

        /// <summary>
        /// 同期範囲を取得
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>同期範囲</returns>
        public virtual string GetSyncScope(string bucketName)
        {
            var result = from x in _context.ObjectBucketCaches where x.Name == bucketName select x.SyncScope;
            return result.Any() ? result.First() : null;
        }

        /// <summary>
        /// 同期完了時刻を取得
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>同期完了時刻</returns>
        public virtual string GetLastSyncTime(string bucketName)
        {
            var result = from x in _context.ObjectBucketCaches where x.Name == bucketName select x.LastSyncTime;
            return result.Any() ? result.First() : null;
        }

        /// <summary>
        /// 最終Pullサーバ日時を保存
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="lastTime">最終Pullサーバ日時</param>
        /// <returns>データベースに書き込まれたオブジェクト数</returns>
        public virtual int SaveLastPullServerTime(string bucketName, string lastTime)
        {
            var bucketCache = getBucketCache(bucketName);
            bucketCache.LastPullServerTime = lastTime;

            return _context.SaveChanges();
        }

        /// <summary>
        /// 同期範囲を保存する
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="scope">同期範囲</param>
        /// <returns>データベースに書き込まれたオブジェクト数</returns>
        /// <remarks>
        /// 同期範囲保存により、該当バケットの「最終Pullサーバ日時」、「同期完了時刻」は初期化する
        /// </remarks>
        public virtual int SaveSyncScope(string bucketName, string scope)
        {
            var bucketCache = getBucketCache(bucketName);
            bucketCache.SyncScope = scope;
            bucketCache.LastPullServerTime = null;
            bucketCache.LastSyncTime = null;

            return _context.SaveChanges();
        }

        /// <summary>
        /// 同期完了時刻を保存する
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="lastTime">同期完了時刻</param>
        /// <returns>データベースに書き込まれたオブジェクト数</returns>
        public virtual int SaveLastSyncTime(string bucketName, string lastTime)
        {
            var bucketCache = getBucketCache(bucketName);
            bucketCache.LastSyncTime = lastTime;

            return _context.SaveChanges();
        }

        /// <summary>
        /// 保存済みバケットキャッシュを取得する<br/>
        /// 未保存の場合は新規にキャッシュを作成する。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>バケットキャッシュ</returns>
        /// <remarks>バケットキャッシュ取得後に、<see cref="SaveChanges"/>をコールして保存すること。</remarks>
        internal virtual ObjectBucketCache getBucketCache(string bucketName)
        {
            var bucketCache = FindByName(bucketName);
            if (bucketCache == null)
            {
                // キャッシュ未保存の場合、新規キャッシュを生成
                bucketCache = new ObjectBucketCache() { Name = bucketName };
                _context.ObjectBucketCaches.Add(bucketCache);
            }

            return bucketCache;
        }

        /// <summary>
        /// エントリ追加
        /// </summary>
        /// <param name="cache">バケットキャッシュ</param>
        /// <exception cref="ArgumentNullException">バケットキャッシュがnull</exception>
        public virtual void Add(ObjectBucketCache cache)
        {
            NbUtil.NotNullWithArgument(cache, "cache");
            _context.ObjectBucketCaches.Add(cache);
        }

        /// <summary>
        /// エントリ削除
        /// </summary>
        /// <param name="cache">バケットキャッシュ</param>
        /// <exception cref="ArgumentNullException">バケットキャッシュがnull</exception>
        public virtual void Remove(ObjectBucketCache cache)
        {
            NbUtil.NotNullWithArgument(cache, "cache");
            _context.ObjectBucketCaches.Remove(cache);
        }

        /// <summary>
        /// 変更保存
        /// </summary>
        /// <returns></returns>
        public virtual int SaveChanges()
        {
            return _context.SaveChanges();
        }

        /// <summary>
        /// 全オブジェクトバケットキャッシュを検索する
        /// </summary>
        /// <returns>取得したバケットキャッシュ一覧</returns>
        public virtual IEnumerable<ObjectBucketCache> FindAll()
        {
            return from x in _context.ObjectBucketCaches select x;
        }
    }
}
