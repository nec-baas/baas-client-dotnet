
namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// オブジェクトバケットキャッシュエンティティ
    /// </summary>
    public class ObjectBucketCache
    {
        /// <summary>
        /// テーブル生成用 SQL
        /// </summary>
        public const string CreateTableSql =
            "CREATE TABLE IF NOT EXISTS ObjectBucketCaches (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT UNIQUE, LastPullServerTime TEXT, SyncScope TEXT, LastSyncTime TEXT)";

        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// バケット名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 最終Pullサーバ日時
        /// </summary>
        public string LastPullServerTime { get; set; }

        /// <summary>
        /// 同期範囲
        /// </summary>
        public string SyncScope { get; set; }

        /// <summary>
        /// 最終同期完了時刻(クライアント時刻)
        /// </summary>
        public string LastSyncTime { get; set; }
    }
}