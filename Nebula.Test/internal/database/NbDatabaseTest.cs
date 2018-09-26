using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class NbDatabaseTest
    {
        private NbDatabaseImpl db;

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();
            db = new NbDatabaseImpl(":memory:", null);
            db.Open();

            db.ExecSql("CREATE TABLE Test (_id INTEGER PRIMARY KEY AUTOINCREMENT, text TEXT)");
        }

        [TearDown]
        public void TearDown()
        {
            db.Close();
            if (File.Exists(GetDbPath()))
            {
                File.Delete(GetDbPath());
            }
        }

        private string GetDbPath()
        {
            var service = NbService.GetInstance();
            var idpass = service.TenantId + "/" + service.AppId;
            var documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fulldbpath = "NEC/Nebula/" + idpass + "/offline.db";
            fulldbpath = Path.Combine(documentFolder, fulldbpath);
            return fulldbpath;
        }

        private void InsertObject(int num = 1)
        {
            using (var transaction = db.BeginTransaction())
            {
                for (int i = 0; i < num; i++)
                {
                    var values = new Dictionary<string, object>
                    {
                        {"text", "value" + i}
                    };
                    db.Insert("Test", values);
                }

                transaction.Commit();
            }
        }


        /**
         * Constructor(NbDatabaseImpl)
         **/

        /// <summary>
        /// コンストラクタ（正常）
        /// 実パス、パスワードを指定しても正常にインスタンスが作成できること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var dir = Path.GetDirectoryName(GetDbPath());
            Directory.CreateDirectory(dir);
            var database = new NbDatabaseImpl(GetDbPath(), "password");
            Assert.IsNotNull(database);
        }

        /// <summary>
        /// コンストラクタ（パスがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionNodbpath()
        {
            var database = new NbDatabaseImpl(null, null);
        }


        /**
         * CreateDbContext
         **/

        /// <summary>
        /// Entity Framework : DbContext を生成する（正常）
        /// DbContextのインスタンスが正常に作成できること
        /// </summary>
        [Test]
        public void TestCreateDbContextNormal()
        {
            Assert.IsNotNull(db.CreateDbContext());
        }


        /**
         * ExecSql
         * 正常系は他試験で実施済みのため割愛
         **/

        /// <summary>
        /// SQL文を実行する(クエリ以外)（SQLがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestExecSqlExceptionNoSql()
        {
            db.ExecSql(null);
        }


        /**
         * Insert
         **/

        /// <summary>
        /// INSERT（正常）
        /// InsertのSQLを発行できること
        /// </summary>
        [Test]
        public void TestInsertNormal()
        {
            InsertObject();
            using (var reader = db.SelectForReader("Test"))
            {
                Assert.IsTrue(reader.Read());
                Assert.AreEqual("value0", reader.GetString(1));
            }
        }

        /// <summary>
        /// INSERT（空のデータ）
        /// InsertのSQLを発行できること
        /// SQLiteExceptionが発行されること（SQL構文エラー）
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestInsertExceptionUnsetValues()
        {
            db.Insert("Test", new Dictionary<string, object>());
        }

        /// <summary>
        /// INSERT（テーブル名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestInsertExceptionNoTable()
        {
            db.Insert(null, new Dictionary<string, object>());
        }

        /// <summary>
        /// INSERT（データがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestInsertExceptionNoValues()
        {
            db.Insert("Test", null);
        }


        /**
         * Update
         **/

        /// <summary>
        /// UPDATE（正常）
        /// UpdateのSQLを発行できること
        /// </summary>
        [Test]
        public void TestUpdateNormal()
        {
            InsertObject();
            var values = new Dictionary<string, object>
            {
                {"text", "value2"}
            };
            db.Update("Test", values, "text = ?", new object[] { "value0" });
            using (var reader = db.SelectForReader("Test"))
            {
                Assert.IsTrue(reader.Read());
                Assert.AreEqual("value2", reader.GetString(1));
            }
        }

        /// <summary>
        /// UPDATE（空のデータ）
        /// UpdateのSQLを発行できること
        /// SQLiteExceptionが発行されること（SQL構文エラー）
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestUpdateExceptionUnsetValues()
        {
            InsertObject();
            db.Update("Test", new Dictionary<string, object>(), null, null);
        }

        /// <summary>
        /// UPDATE（WHERE、WHERE引数ともにnull）
        /// UpdateのSQLを発行できること
        /// </summary>
        [Test]
        public void TestUpdateSubNomalNoWhereWhereArgs()
        {
            InsertObject();
            var values = new Dictionary<string, object>
            {
                {"text", "value2"}
            };
            db.Update("Test", values, null, null);
            using (var reader = db.SelectForReader("Test"))
            {
                Assert.IsTrue(reader.Read());
                Assert.AreEqual("value2", reader.GetString(1));
            }
        }

        /// <summary>
        /// UPDATE（WHEREがnull）
        /// UpdateのSQLを発行できること
        /// </summary>
        [Test]
        public void TestUpdateSubNomalNoWhere()
        {
            InsertObject();
            var values = new Dictionary<string, object>
            {
                {"text", "value2"}
            };
            db.Update("Test", values, null, new object[] { });
            using (var reader = db.SelectForReader("Test"))
            {
                Assert.IsTrue(reader.Read());
                Assert.AreEqual("value2", reader.GetString(1));
            }
        }

        /// <summary>
        /// UPDATE（WHERE引数がnull）
        /// UpdateのSQLを発行できること
        /// SQLiteExceptionが発行されること（SQL構文エラー）
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestUpdateExceptionNoWhereArgs()
        {
            InsertObject();
            var values = new Dictionary<string, object>
            {
                {"text", "value2"}
            };
            db.Update("Test", values, "text = ?", null);
        }

        /// <summary>
        /// UPDATE（WHERE引数が空）
        /// UpdateのSQLを発行できること
        /// SQLiteExceptionが発行されること（SQL構文エラー）
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestUpdateExceptionUnsetWhereArgs()
        {
            InsertObject();
            var values = new Dictionary<string, object>
            {
                {"text", "value2"}
            };
            db.Update("Test", values, "text = ?", new object[] { });
        }

        /// <summary>
        /// UPDATE（テーブル名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestUpdateExceptionNoTable()
        {
            db.Update(null, new Dictionary<string, object>(), null, null);
        }

        /// <summary>
        /// UPDATE（データがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestUpdateExceptionNoValues()
        {
            db.Update("Test", null, null, null);
        }


        /**
         * Delete
         **/

        /// <summary>
        /// 削除する（正常）
        /// DeleteのSQLを発行できること
        /// </summary>
        [Test]
        public void TestDeleteNormal()
        {
            InsertObject();
            db.Delete("Test", "text = ?", new object[] { "value0" });
            using (var reader = db.SelectForReader("Test"))
            {
                Assert.IsFalse(reader.Read());
            }
        }

        /// <summary>
        /// 削除する（WHERE、WHERE引数がnull）
        /// DeleteのSQLを発行できること
        /// </summary>
        [Test]
        public void TestDeleteSubNomalNoWhereWhereArgs()
        {
            InsertObject();
            db.Delete("Test", null, null);
            using (var reader = db.SelectForReader("Test"))
            {
                Assert.IsFalse(reader.Read());
            }
        }

        /// <summary>
        /// 削除する（WHEREがnull）
        /// DeleteのSQLを発行できること
        /// </summary>
        [Test]
        public void TestDeleteSubNomalNoWhere()
        {
            InsertObject();
            db.Delete("Test", null, new object[] { "value0" });
            using (var reader = db.SelectForReader("Test"))
            {
                Assert.IsFalse(reader.Read());
            }
        }

        /// <summary>
        /// 削除する（WHERE引数がnull）
        /// DeleteのSQLを発行できること
        /// SQLiteExceptionが発行されること（SQL構文エラー）
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestDeleteExceptionNoWhereArgs()
        {
            InsertObject();
            db.Delete("Test", "text = ?", null);
        }

        /// <summary>
        /// 削除する（WHERE引数が空）
        /// DeleteのSQLを発行できること
        /// SQLiteExceptionが発行されること（SQL構文エラー）
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestDeleteExceptionUnsetWhereArgs()
        {
            InsertObject();
            db.Delete("Test", "text = ?", new object[] { });
        }

        /// <summary>
        /// 削除する（テーブル名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestDeleteExceptionNoTable()
        {
            db.Delete(null, null, null);
        }


        /**
         * SelectForReader
         **/

        /// <summary>
        /// SELECT を発行する（正常）
        /// SelectのSQLを発行できること
        /// </summary>
        [Test]
        public void TestSelectForReaderNormal()
        {
            InsertObject(5);
            var where = "text = ? or text = ? or text = ? or text = ? or text = ?";
            var whereargs = new object[] { "value0", "value1", "value2", "value3", "value4" };
            using (var reader = db.SelectForReader("Test", new[] { "text" }, where, whereargs, 0, 100))
            {
                int count = 0;
                while (reader.Read())
                {
                    var text = reader.GetString(0);
                    Assert.AreEqual("value" + count, text);
                    count++;
                }
                Assert.AreEqual(5, count);
            }
        }

        /// <summary>
        /// SELECT を発行する（テーブル名以外の引数指定なし）
        /// SelectのSQLを発行できること
        /// </summary>
        [Test]
        public void TestSelectForReaderNormalAllUnset()
        {
            InsertObject(5);
            using (var reader = db.SelectForReader("Test"))
            {
                int count = 0;
                while (reader.Read())
                {
                    var text = reader.GetString(1);
                    Assert.AreEqual("value" + count, text);
                    count++;
                }
                Assert.AreEqual(5, count);
            }
        }

        /// <summary>
        /// SELECT を発行する（WHERE、WHERE引数がnull）
        /// SelectのSQLを発行できること
        /// </summary>
        [Test]
        public void TestSelectForReaderSubNomalUnsetWhereArgs()
        {
            InsertObject(5);
            using (var reader = db.SelectForReader("Test", new[] { "text" }, null, null, 0, 100))
            {
                int count = 0;
                while (reader.Read())
                {
                    var text = reader.GetString(0);
                    Assert.AreEqual("value" + count, text);
                    count++;
                }
                Assert.AreEqual(5, count);
            }
        }

        /// <summary>
        /// SELECT を発行する（テーブル名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSelectForReaderExceptionNoTable()
        {
            db.SelectForReader(null);
        }

        /// <summary>
        /// SELECT を発行する（Limit未設定、オフセット設定あり）
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestSelectForReaderExceptionNoLimitOffset()
        {
            db.SelectForReader("Test", new[] { "text" }, null, null, 10);
        }

        /**
         * RawQuery
         **/

        /// <summary>
        /// Raw Query（正常）
        /// SQL文を発行できること
        /// </summary>
        [Test]
        public void TestRawQueryNormal()
        {
            InsertObject();
            using (var reader = db.RawQuery("select * FROM Test WHERE text = ?", new object[] { "value0" }))
            {
                Assert.IsTrue(reader.Read());
                var text = reader.GetString(1);
                Assert.AreEqual("value0", text);
            }
        }

        /// <summary>
        /// Raw Query（argumentsがnull）
        /// SQL文が発行できること
        /// </summary>
        [Test]
        public void TestRawQuerySubNormalNoArguments()
        {
            InsertObject();
            using (var reader = db.RawQuery("select * FROM Test", null))
            {
                Assert.IsTrue(reader.Read());
                var text = reader.GetString(1);
                Assert.AreEqual("value0", text);
            }
        }

        /// <summary>
        /// Raw Query（argumentsが空）
        /// SQL文が発行できること
        /// SQLiteExceptionが発行されること（SQL構文エラー）
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestRawQueryExceptionUnsetArguments()
        {
            InsertObject();
            var reader = db.RawQuery("select * FROM Test WHERE text = ?", new object[] { });
        }

        /// <summary>
        /// Raw Query（SQL文がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRawQueryExceptionNoSQLe()
        {
            db.RawQuery(null, null);
        }


        /**
         * ChangePassword
         **/

        /// <summary>
        /// Databaseのパスワードを変更する （正常）
        /// 正常に終了すること
        /// </summary>
        [Test]
        public void TestChangePasswordNormal()
        {
            db.ChangePassword("test");
        }

        /// <summary>
        /// Databaseのパスワードを変更する （パスワードがnull）
        /// 正常に終了すること
        /// </summary>
        [Test]
        public void TestChangePasswordNormalNoPassword()
        {
            db.ChangePassword(null);
        }
    }
}
