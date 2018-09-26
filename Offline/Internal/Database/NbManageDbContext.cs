using System;
using System.Data.Entity;
using System.Data.SQLite;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// 管理データベース。
    /// EntityFramework で使用する。
    /// なお、オブジェクトテーブルはテーブル名、カラム数などが不定なので、EntityFramework対象外。
    /// </summary>
    internal class NbManageDbContext : DbContext
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="connection">コネクション</param>
        /// <exception cref="ArgumentNullException">コネクションがnull</exception>
        public NbManageDbContext(SQLiteConnection connection)
            : base(connection, false)
        {
            NbUtil.NotNullWithArgument(connection, "connection");
            CreateTables(connection);
        }

        /// <summary>
        /// ログインキャッシュ
        /// </summary>
        public DbSet<LoginCache> LoginCaches { get; set; }

        /// <summary>
        /// オブジェクトバケットキャッシュ
        /// </summary>
        public DbSet<ObjectBucketCache> ObjectBucketCaches { get; set; }

        /// <summary>
        /// テーブルを生成する。
        /// EF6 自体テーブル自動生成機能を持っているが、System.Data.SQLite にはテーブル自動
        /// 生成機能がないため、手動生成する必要がある。
        /// </summary>
        private void CreateTables(SQLiteConnection connection)
        {
            TryCreateTable(connection, LoginCache.CreateTableSql);
            TryCreateTable(connection, ObjectBucketCache.CreateTableSql);
        }

        private void TryCreateTable(SQLiteConnection connection, string sql)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
