using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// データベースアクセスハンドル
    /// </summary>
    public class NbDatabaseImpl : NbDatabase
    {
        /// <summary>
        /// データベースコネクション。
        /// スレッドセーフではないので注意。
        /// 別スレッドに引き渡す場合は clone しなければならない。
        /// </summary>
        private readonly SQLiteConnectionManager _connectionManager;

        /// <summary>
        /// コンストラクタ。_connectionManager と DbContext が新規生成される。
        /// </summary>
        /// <param name="dbpath">データベースファイルパス。':memory:' を指定するとインメモリDBとなる。</param>
        /// <param name="password">暗号化パスワード</param>
        /// <exception cref="ArgumentNullException">データベースファイルパスがnull</exception>
        public NbDatabaseImpl(string dbpath, string password)
        {
            NbUtil.NotNullWithArgument(dbpath, "dbpath");
            var connection = new SQLiteConnection
            {
                ConnectionString = "Data Source=" + dbpath + ";"
            };
            if (password != null)
            {
                connection.ConnectionString += "Password=" + password + ";";
            }

            bool isInMemory = (dbpath == ":memory:");
            _connectionManager = new SQLiteConnectionManager(connection, !isInMemory);
        }

        /// <summary>
        /// コネクションを取得する（スレッドセーフ)。
        /// DB生成時のスレッドとは別のスレッドから呼ばれた場合は、毎回コネクションを生成する。
        /// (ただし、In-Memory DB の場合は生成しない)
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection GetConnection()
        {
            return _connectionManager.GetConnection();
        }

        /// <summary>
        /// データベースをオープンする
        /// </summary>
        public void Open()
        {
            _connectionManager.GetMasterConnection().Open();
        }

        /// <summary>
        /// データベースをクローズする
        /// </summary>
        public void Close()
        {
            _connectionManager.GetMasterConnection().Close();
        }

        /// <summary>
        /// Entity Framework : DbContext を生成する
        /// </summary>
        /// <returns>DbContext</returns>
        internal NbManageDbContext CreateDbContext()
        {
            return new NbManageDbContext(GetConnection());
        }

        /// <summary>
        /// SQL文を実行する(クエリ以外)
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <exception cref="ArgumentNullException">SQL文がnull</exception>
        public void ExecSql(string sql)
        {
            NbUtil.NotNullWithArgument(sql, "sql");
            var command = GetConnection().CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// INSERT
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <param name="values">INSERTするデータ</param>
        /// <returns>INSERT行数</returns>
        /// <exception cref="ArgumentNullException">テーブル名、INSERTするデータがnull</exception>
        public int Insert(string table, Dictionary<string, object> values)
        {
            NbUtil.NotNullWithArgument(table, "table");
            NbUtil.NotNullWithArgument(values, "values");
            var sql = new StringBuilder();
            sql.Append("INSERT INTO ").Append(table).Append(" (");
            sql.Append(string.Join(",", values.Keys));
            sql.Append(") VALUES (");

            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) sql.Append(",");
                sql.Append("?");
            }
            sql.Append(")");

            var command = GetConnection().CreateCommand();
            command.CommandText = sql.ToString();

            foreach (var pair in values)
            {
                command.Parameters.AddWithValue(null, pair.Value);
            }
            int n = command.ExecuteNonQuery();
            return n;
        }

        /// <summary>
        /// UPDATE
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <param name="values">UPDATEするデータ</param>
        /// <param name="where">WHERE</param>
        /// <param name="whereArgs">WHERE引数</param>
        /// <returns>更新された行数</returns>
        /// <exception cref="ArgumentNullException">テーブル名、UPDATEするデータがnull</exception>
        public int Update(string table, Dictionary<string, object> values, string where, object[] whereArgs)
        {
            NbUtil.NotNullWithArgument(table, "table");
            NbUtil.NotNullWithArgument(values, "values");
            var sql = new StringBuilder();
            sql.Append("UPDATE ").Append(table).Append(" SET ");

            int i = 0;
            foreach (var key in values.Keys)
            {
                if (i > 0) sql.Append(",");
                sql.Append(key).Append("=?");
                i++;
            }

            if (where != null)
            {
                sql.Append(" WHERE ").Append(where);
            }

            var command = GetConnection().CreateCommand();
            command.CommandText = sql.ToString();

            AddArguments(command, values.Values); // 値
            if (where != null)
            {
                AddArguments(command, whereArgs); // Where 引数
            }

            var reader = command.ExecuteReader();
            return reader.RecordsAffected;
        }

        private void AddArguments(SQLiteCommand command, IEnumerable<object> arguments)
        {
            if (arguments != null)
            {
                foreach (var arg in arguments)
                {
                    command.Parameters.AddWithValue(null, arg);
                }
            }
        }

        /// <summary>
        /// 削除する
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <param name="where">WHERE</param>
        /// <param name="whereArgs">WHERE引数</param>
        /// <returns>削除された行数</returns>
        /// <exception cref="ArgumentNullException">テーブル名がnull</exception>
        public int Delete(string table, string where, object[] whereArgs)
        {
            NbUtil.NotNullWithArgument(table, "table");
            var sql = new StringBuilder();
            sql.Append("DELETE FROM ").Append(table);
            if (where != null)
            {
                sql.Append(" WHERE ").Append(where);
            }

            var command = GetConnection().CreateCommand();
            command.CommandText = sql.ToString();

            if (where != null)
            {
                AddArguments(command, whereArgs);
            }

            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// SELECT を発行する
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <param name="projection">Projection</param>
        /// <param name="where">WHERE</param>
        /// <param name="whereArgs">WHERE引数</param>
        /// <param name="offset">Offset</param>
        /// <param name="limit">Limit</param>
        /// <returns>Reader</returns>
        /// <exception cref="ArgumentNullException">テーブル名がnull</exception>
        public SQLiteDataReader SelectForReader(string table, string[] projection = null, string where = null, object[] whereArgs = null,
            int offset = -1, int limit = -1)
        {
            NbUtil.NotNullWithArgument(table, "table");
            var sql = new StringBuilder();
            sql.Append("SELECT ");
            sql.Append(projection != null ? string.Join(",", projection) : "*");
            sql.Append(" FROM ").Append(table);

            if (where != null)
            {
                sql.Append(" WHERE ").Append(where);
            }
            if (limit >= 0)
            {
                if (offset <= 0) offset = 0;
                sql.Append(" LIMIT ").Append(offset).Append(",").Append(limit);
            }
            else if (offset > 0)
            {
                throw new ArgumentException("offset without limit");
            }

            var command = GetConnection().CreateCommand();
            command.CommandText = sql.ToString();

            if (where != null)
            {
                AddArguments(command, whereArgs);
            }

            return command.ExecuteReader();
        }

        /// <summary>
        /// Raw Query
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="arguments">引数</param>
        /// <returns>Reader</returns>
        /// <exception cref="ArgumentNullException">SQL文がnull</exception>
        public SQLiteDataReader RawQuery(string sql, object[] arguments)
        {
            NbUtil.NotNullWithArgument(sql, "sql");
            var command = GetConnection().CreateCommand();
            command.CommandText = sql;
            AddArguments(command, arguments);
            return command.ExecuteReader();
        }

        /// <summary>
        /// トランザクションを開始する
        /// </summary>
        /// <returns>トランザクション</returns>
        public SQLiteTransaction BeginTransaction()
        {
            return GetConnection().BeginTransaction(IsolationLevel.Serializable);
        }

        /// <summary>
        /// Databaseのパスワードを変更する
        /// </summary>
        /// <param name="newPassword">新しいパスワード</param>
        public void ChangePassword(string newPassword)
        {
            GetConnection().ChangePassword(newPassword);
        }
    }
}
