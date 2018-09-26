using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Data.SQLite;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class NbManageDbContextTest
    {
        private SQLiteConnection _connection;

        [SetUp]
        public void SetUp()
        {
            _connection = new SQLiteConnection
            {
                ConnectionString = "Data Source=:memory:;"
            };
            _connection.Open();
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        private int GetTables()
        {
            SQLiteCommand command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM sqlite_master WHERE type='table'";
            int count = 0;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader["name"].ToString().Equals("LoginCaches") ||
                        reader["name"].ToString().Equals("ObjectBucketCaches"))
                        count++;
                }
            }
            return count;
        }

        /**
         * Constructor(NbManageDbContext)
         **/

        /// <summary>
        /// コンストラクタ（正常）
        /// NbManageDbContextのインスタンスが生成できること
        /// ログインキャッシュテーブル、オブジェクトバケットキャッシュテーブルが作成されていること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            Assert.AreEqual(0, GetTables());
            var context = new NbManageDbContext(_connection);
            Assert.IsNotNull(context);
            Assert.AreEqual(2, GetTables());
        }

        /// <summary>
        /// コンストラクタ（connectionがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionNoConnection()
        {
            new NbManageDbContext(null);
        }
    }
}
