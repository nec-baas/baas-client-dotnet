using System.Collections.Generic;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// データベースハンドル。
    /// SQLite に依存する部分は記述しない。
    /// </summary>
    public interface NbDatabase
    {
        /// <summary>
        /// データベースをオープンする
        /// </summary>
        void Open();

        /// <summary>
        /// データベースをクローズする
        /// </summary>
        void Close();

        /// <summary>
        /// SQL文を実行する(クエリ以外)
        /// </summary>
        /// <param name="sql">SQL文</param>
        void ExecSql(string sql);

        /// <summary>
        /// INSERT
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <param name="values">INSERTするデータ</param>
        /// <returns>INSERT行数</returns>
        int Insert(string table, Dictionary<string, object> values);

        /// <summary>
        /// UPDATE
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <param name="values">UPDATEするデータ</param>
        /// <param name="where">WHERE</param>
        /// <param name="whereArgs">WHERE引数</param>
        /// <returns>更新された行数</returns>
        int Update(string table, Dictionary<string, object> values, string where, object[] whereArgs);

        /// <summary>
        /// 削除する
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <param name="where">WHERE</param>
        /// <param name="whereArgs">WHERE引数</param>
        /// <returns>削除された行数</returns>
        int Delete(string table, string where, object[] whereArgs);

        /// <summary>
        /// Databaseのパスワードを変更する
        /// </summary>
        /// <param name="newPassword">新しいパスワード</param>
        void ChangePassword(string newPassword);
    }
}