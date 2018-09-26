using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class SQLiteConnectionManagerTest
    {
        private SQLiteConnection _connection;
        private SQLiteConnectionManager _manager;

        private const string DbFilePrefix = "SQLiteConnectionManagerTest";

        [TearDown]
        public void TearDown()
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;

            _manager = null;

            GC.Collect();
        }

        private void OpenConnection(int id, bool clone = true)
        {
            var dbfile = DbFilePrefix + id;

            TryDeleteDbFile(dbfile);

            _connection = new SQLiteConnection("DataSource=" + dbfile + ";");
            _manager = new SQLiteConnectionManager(_connection, clone);

            _connection.Open();
        }

        private void OpenInmemoryConnection()
        {
            _connection = new SQLiteConnection
            {
                ConnectionString = "Data Source=:memory:;"
            };
            _manager = new SQLiteConnectionManager(_connection, false);
            _connection.Open();
        }

        private void TryDeleteDbFile(string dbfile)
        {
            try
            {
                File.Delete(dbfile);
            }
            catch (IOException)
            {
                // ファイルロックされている場合。
                // GCが完全に終わっていないとロックが解放されていないケースがある。
            }
        }

        private void ExecSql(SQLiteConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private SQLiteDataReader ExecSelect(SQLiteConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 1;
            return command.ExecuteReader();
        }

        /// <summary>
        /// マルチスレッドテスト
        /// </summary>
        [Test]
        public void TestMultiThread()
        {
            OpenConnection(1);

            var conn1 = _manager.GetConnection();
            Assert.AreNotSame(_connection, conn1);

            ExecSql(conn1, "DROP TABLE IF EXISTS test1");
            ExecSql(conn1, "CREATE TABLE test1 (c1 INTEGER)");
            ExecSql(conn1, "INSERT INTO test1 VALUES(100)");

            Task.Run(() =>
            {
                var conn2 = _manager.GetConnection();
                Assert.AreNotSame(conn1, conn2);

                var reader = ExecSelect(conn2, "SELECT * FROM test1");

                Assert.True(reader.Read());
                Assert.AreEqual(100, reader.GetInt32(0));
            }).Wait();
        }

        /// <summary>
        /// マルチスレッドテスト (トランザクションあり)
        /// </summary>
        [Test]
        public void TestMultiThreadWithTransaction()
        {
            OpenConnection(2);

            var conn1 = _manager.GetConnection();

            ExecSql(conn1, "DROP TABLE IF EXISTS test1");
            ExecSql(conn1, "CREATE TABLE test1 (c1 INTEGER)");

            using (var transaction = conn1.BeginTransaction(IsolationLevel.Serializable))
            {
                ExecSql(conn1, "INSERT INTO test1 VALUES(100)");

                Task.Run(() =>
                {
                    var conn2 = _manager.GetConnection();
                    var reader = ExecSelect(conn2, "SELECT * FROM test1");

                    Assert.AreEqual(0, reader.StepCount);
                }).Wait();

                transaction.Commit();
            }
        }


        /**
         * Constructor(SQLiteConnectionManager)
         **/

        /// <summary>
        /// コンストラクタ（正常）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            OpenConnection(0);
            Assert.IsNotNull(_manager);
            Assert.IsNotNull(_connection);
            Assert.AreNotEqual(_manager.GetConnection(), _connection);
        }

        /// <summary>
        /// コンストラクタ（cloneをfalse指定）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormalSetClonefalse()
        {
            OpenConnection(0, false);
            Assert.IsNotNull(_manager);
            Assert.IsNotNull(_connection);
            Assert.AreEqual(_manager.GetConnection(), _connection);
        }

        /// <summary>
        /// コンストラクタ（cloneをfalse指定 inmemory）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormalSetClonefalseInmemory()
        {
            OpenInmemoryConnection();
            Assert.IsNotNull(_manager);
            Assert.IsNotNull(_connection);
            Assert.AreEqual(_manager.GetConnection(), _connection);
        }

        /// <summary>
        /// コンストラクタ（connectionがNULL）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionNoConnection()
        {
            OpenInmemoryConnection();
            new SQLiteConnectionManager(null);
        }


        /**
         * GetMasterConnection
         **/

        /// <summary>
        /// マスターコネクションを返す（正常）
        /// マスターコネクションを取得できること
        /// </summary>
        [Test]
        public void TestGetMasterConnectionNormal()
        {
            OpenInmemoryConnection();
            Assert.IsNotNull(_manager.GetMasterConnection());
        }


        /**
         * GetConnection
         **/

        /// <summary>
        /// コネクション(スレッドローカル)を返す（正常）
        /// コネクションを取得できること
        /// </summary>
        [Test]
        public void TestGetConnectionNormal()
        {
            OpenInmemoryConnection();
            Assert.IsNotNull(_manager.GetConnection());
        }
    }
}
