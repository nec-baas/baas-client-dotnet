using System;
using System.Data.SQLite;
using System.Threading;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// スレッドセーフな SQLiteConnection マネージャ。
    /// 
    /// SQLiteConnection はスレッドセーフではない。本クラスでは、SQLiteConnection
    /// をスレッドローカル領域に作成するようにしている。
    /// </summary>
    internal class SQLiteConnectionManager
    {
        private readonly SQLiteConnection _masterConnection;

        private readonly ThreadLocal<SQLiteConnection> _threadLocalConnection;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="connection">コネクション</param>
        /// <param name="clone">コネクションを clone する場合は true (In-memory の場合は false にする)</param>
        /// <exception cref="ArgumentNullException">connectionがnull</exception>
        internal SQLiteConnectionManager(SQLiteConnection connection, bool clone = true)
        {
            NbUtil.NotNullWithArgument(connection, "connection");
            _masterConnection = connection;

            Func<SQLiteConnection> func;
            if (clone)
            {
                func = () => new SQLiteConnection(_masterConnection);
            }
            else
            {
                func = () => _masterConnection;
            }

            _threadLocalConnection = new ThreadLocal<SQLiteConnection>(func);
        }

        /// <summary>
        /// マスターコネクションを返す
        /// </summary>
        /// <returns>SQLiteConnection</returns>
        public SQLiteConnection GetMasterConnection()
        {
            return _masterConnection;
        }

        /// <summary>
        /// コネクション(スレッドローカル)を返す
        /// </summary>
        /// <returns>SQLiteConnection</returns>
        public SQLiteConnection GetConnection()
        {
            return _threadLocalConnection.Value;
        }
    }
}
