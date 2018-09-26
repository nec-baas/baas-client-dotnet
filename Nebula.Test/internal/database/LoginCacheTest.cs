using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class LoginCacheTest
    {
        private NbDatabaseImpl _db;

        [SetUp]
        public void SetUp()
        {
            _db = new NbDatabaseImpl(":memory:", null);
            _db.Open();
        }

        [TearDown]
        public void TearDown()
        {
            _db.Close();
            _db = null;
        }

        [Test]
        public void TestLoginCache()
        {
            // INSERT
            var login = new LoginCache
            {
                UserName = "foo",
                Email = "foo@example.com",
                Groups = new List<string> { "g1", "g2", "g3" },
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);

                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                // QUERY
                var found = dao.FindByUsername("foo");
                Assert.AreEqual("foo", found.UserName);
                Assert.AreEqual("foo@example.com", found.Email);

                var groups = found.Groups;
                Assert.AreEqual(3, groups.Count);

                found = dao.FindByEmail("foo@example.com");
                Assert.AreEqual("foo", found.UserName);
                Assert.AreEqual("foo@example.com", found.Email);

                // UPDATE
                found.Email = "bar@example.com";
                Assert.AreEqual(1, dbContext.SaveChanges());

                // QUERY
                found = dao.FindByUsername("foo");
                Assert.AreEqual("bar@example.com", found.Email);

                // DELETE
                dao.Remove(found);
                Assert.AreEqual(1, dbContext.SaveChanges());

                // QUERY
                found = dao.FindByUsername("foo");
                Assert.IsNull(found);
            }
        }

        [Test]
        public void TestCalcHash()
        {
            var cache = new LoginCache();

            cache.Password = "aaa";
            var hash = cache.PasswordHash;
            Assert.AreEqual(64, hash.Length);

            Assert.True(cache.IsValidPassword("aaa"));
            Assert.False(cache.IsValidPassword("aab"));

            cache.Password = "aab";
            var hash2 = cache.PasswordHash;

            cache.Password = "aaa";
            var hash3 = cache.PasswordHash;

            Assert.AreNotEqual(hash, hash2);
            Assert.AreEqual(hash, hash3);
        }

        [Test]
        public void TestGroupList()
        {
            // initial
            var cache = new LoginCache();
            Assert.AreEqual("[]", cache.GroupsJson);
            Assert.IsNull(cache.Groups);

            // set empty string
            cache.GroupsJson = "";
            Assert.IsEmpty(cache.Groups);

            // set string
            cache.GroupsJson = "[\"1\",\"2\",\"3\"]";
            Assert.AreEqual(3, cache.Groups.Count);

            // set List
            cache.Groups = new List<string>() { "a", "b", "c" };
            Assert.AreEqual("[\"a\",\"b\",\"c\"]", cache.GroupsJson);

            // set bad json
            cache.GroupsJson = "[";
            Assert.IsEmpty(cache.Groups);
        }

        private LoginCache CreateLoginCache()
        {
            var options = new NbJsonObject() { { "key", "val" } };

            var login = new LoginCache
            {
                Id = 12345,
                UserId = "12345",
                UserName = "foo",
                Email = "foo@example.com",
                Options = options,
                GroupsJson = "[\"1\",\"2\",\"3\"]",
                Password = "password",
                CreatedAt = "CREATEDAT",
                UpdatedAt = "UPDATEDAT",
                SessionToken = "1234567890",
                SessionExpireAt = 999999999999L
            };

            return login;
        }


        /**
         * プロパティ確認
        **/

        /// <summary>
        /// プロパティ確認
        /// 設定した値が読み出せること
        /// </summary>
        [Test]
        public void TestLoginCacheNormal()
        {
            var cache = CreateLoginCache();

            Assert.AreEqual(cache.Id, 12345);
            Assert.AreEqual(cache.UserId, "12345");
            Assert.AreEqual(cache.UserName, "foo");
            Assert.AreEqual(cache.Email, "foo@example.com");
            Assert.AreEqual(cache.Options, new NbJsonObject() { { "key", "val" } });
            Assert.AreEqual(cache.Groups, new List<string>() { "1", "2", "3" });
            Assert.IsTrue(cache.IsValidPassword("password"));
            Assert.AreEqual(cache.CreatedAt, "CREATEDAT");
            Assert.AreEqual(cache.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(cache.SessionToken, "1234567890");
            Assert.AreEqual(cache.SessionExpireAt, 999999999999L);
        }

        /// <summary>
        /// プロパティ確認
        /// 未設定の場合は初期値が読み出せること
        /// </summary>
        [Test]
        public void TestLoginCacheSubNormalUnset()
        {
            var cache = new LoginCache();

            Assert.AreEqual(cache.Id, 0);
            Assert.IsNull(cache.UserId);
            Assert.IsNull(cache.UserName);
            Assert.IsNull(cache.Email);
            Assert.IsNull(cache.Options);
            Assert.IsNull(cache.Groups);
            Assert.IsNull(cache.PasswordHash);
            Assert.IsNull(cache.CreatedAt);
            Assert.IsNull(cache.UpdatedAt);
            Assert.IsNull(cache.SessionToken);
            Assert.AreEqual(cache.SessionExpireAt, 0);
        }


        /**
         * SetUser
        **/

        /// <summary>
        /// ユーザ情報をセットする（正常）
        /// ユーザ情報をセットできること。読み出せること
        /// </summary>
        [Test]
        public void TestSetUserNormal()
        {
            var user = new NbUser();
            user.UserId = "12345";
            user.Username = "foo";
            user.Email = "foo@example.com";
            var options = new NbJsonObject() { { "key", "val" } };
            user.Options = options;
            var groups = new List<string>() { "1", "2", "3" };
            user.Groups = groups;
            user.CreatedAt = "CREATEDAT";
            user.UpdatedAt = "UPDATEDAT";

            var cache = new LoginCache();
            cache.SetUser(user);

            Assert.AreEqual(cache.UserId, user.UserId);
            Assert.AreEqual(cache.UserName, user.Username);
            Assert.AreEqual(cache.Email, user.Email);
            Assert.AreEqual(cache.Options, options);
            Assert.AreEqual(cache.Groups, groups);
            Assert.AreEqual(cache.CreatedAt, user.CreatedAt);
            Assert.AreEqual(cache.UpdatedAt, user.UpdatedAt);
        }

        /// <summary>
        /// ユーザ情報をセットする（ユーザ情報がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetUserExceptionNoUser()
        {
            var cache = new LoginCache();
            cache.SetUser(null);
        }


        /**
         * SetSession
        **/

        /// <summary>
        /// セッション情報をセットする（正常）
        /// セッション情報をセットできること。読み出せること
        /// </summary>
        [Test]
        public void TestSetSessionNormal()
        {
            var user = new NbUser();
            user.UserId = "12345";
            user.Username = "foo";
            user.Email = "foo@example.com";
            var options = new NbJsonObject() { { "key", "val" } };
            user.Options = options;
            var groups = new List<string>() { "1", "2", "3" };
            user.Groups = groups;
            user.CreatedAt = "CREATEDAT";
            user.UpdatedAt = "UPDATEDAT";

            var session = new NbSessionInfo();
            session.Set("1234567890", 999999999999L, user);

            var cache = new LoginCache();
            cache.SetSession(session);

            Assert.AreEqual(cache.SessionToken, "1234567890");
            Assert.AreEqual(cache.SessionExpireAt, 999999999999L);
        }

        /// <summary>
        /// セッション情報をセットする（準正常）
        /// 空のセッション情報をセットしてもExceptionが発行されないこと
        /// </summary>
        [Test]
        public void TestSetSessionSubNomalUnset()
        {
            var session = new NbSessionInfo();

            var cache = new LoginCache();
            cache.SetSession(session);

            Assert.IsNull(cache.SessionToken);
            Assert.AreEqual(cache.SessionExpireAt, 0);
        }

        /// <summary>
        /// セッション情報をセットする（セッション情報がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetSessionExceptionNoSession()
        {
            var cache = new LoginCache();
            cache.SetSession(null);
        }


        /**
         * IsValidPassword
         **/

        /// <summary>
        /// パスワードを検証する（正常）
        /// 設定したパスワードと同じパスワードを指定した場合はtrueが返却されること
        /// </summary>
        [Test]
        public void TestIsValidPasswordNormal()
        {
            var cache = new LoginCache();
            cache.Password = "password";

            Assert.IsTrue(cache.IsValidPassword("password"));
        }

        /// <summary>
        /// パスワードを検証する（準正常）
        /// 設定したパスワードと異なるパスワードを指定した場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsValidPasswordSubNormalPasswordError()
        {
            var cache = new LoginCache();
            cache.Password = "password";

            Assert.IsFalse(cache.IsValidPassword("test"));
        }

        /// <summary>
        /// パスワードを検証する（パスワードがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestIsValidPasswordExceptionNoPassword()
        {
            var cache = new LoginCache();
            cache.Password = "password";

            var result = cache.IsValidPassword(null);
        }


        /**
         * Constructor(LoginCacheDao)
         **/

        /// <summary>
        /// コンストラクタ (LoginCacheDao)（正常）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var dao = new LoginCacheDao(_db.CreateDbContext());
            Assert.IsNotNull(dao);
            FieldInfo context = typeof(LoginCacheDao).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(context.GetValue(dao));
        }

        /// <summary>
        /// コンストラクタ (LoginCacheDao)（contextがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionNoContext()
        {
            var dao = new LoginCacheDao(null);
        }


        /**
         * FindByUsername
         **/

        /// <summary>
        /// ユーザ名でログインキャッシュを検索する（正常）
        /// キャッシュ登録済みのユーザ名でキャッシュを検索できること
        /// </summary>
        [Test]
        public void TestFindByUsernameNormal()
        {
            var login = CreateLoginCache();

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                var found = dao.FindByUsername(login.UserName);
                Assert.AreEqual(login.UserName, found.UserName);
                Assert.AreEqual(login.Email, found.Email);
                Assert.IsTrue(found.IsValidPassword("password"));

                dao.Remove(found);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        /// <summary>
        /// ユーザ名でログインキャッシュを検索する（正常）
        /// キャッシュ登録済みのユーザ名と異なるユーザ名で検索しヒットしないこと
        /// </summary>
        [Test]
        public void TestFindByUsernameNormalNoHit()
        {
            var login = CreateLoginCache();
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                Assert.IsNull(dao.FindByUsername("bar"));

                dao.Remove(login);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        /// <summary>
        /// ユーザ名でログインキャッシュを検索する（ユーザ名がnull）
        /// ヒットしないこと
        /// </summary>
        [Test]
        public void TestFindByUsernameSubNormalNoUsername()
        {
            var login = CreateLoginCache();
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                Assert.IsNull(dao.FindByUsername(null));

                dao.Remove(login);
                Assert.AreEqual(1, dao.SaveChanges());

            }
        }


        /**
         * FindByEmail
         **/

        /// <summary>
        /// E-mail でログインキャッシュを検索する（正常）
        /// キャッシュ登録済みのEmailでキャッシュを検索できること
        /// </summary>
        [Test]
        public void TestFindByEmailNormal()
        {
            var login = CreateLoginCache();
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                var found = dao.FindByEmail(login.Email);
                Assert.AreEqual(login.UserName, found.UserName);
                Assert.AreEqual(login.Email, found.Email);
                Assert.IsTrue(found.IsValidPassword("password"));

                dao.Remove(found);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        /// <summary>
        /// E-mail でログインキャッシュを検索する（正常）
        /// キャッシュ登録済みのEmailと異なるEmailで検索しヒットしないこと
        /// </summary>
        [Test]
        public void TestFindByEmailNormalNoHit()
        {
            var login = CreateLoginCache();
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                Assert.IsNull(dao.FindByEmail("bar@example.com"));

                dao.Remove(login);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        /// <summary>
        /// E-mail でログインキャッシュを検索する
        /// 検索するEmailがnullの場合はヒットしないこと
        /// </summary>
        [Test]
        public void TestFindByEmailSubNormalNoEmail()
        {
            var login = CreateLoginCache();
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                Assert.IsNull(dao.FindByEmail(null));
                dao.Remove(login);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }


        /**
         * FindAll
         **/

        /// <summary>
        /// 全ログインキャッシュを検索する（正常）
        /// 登録済みのキャッシュを全検索できること
        /// </summary>
        [Test]
        public void TestFindAllNormal()
        {
            var login = CreateLoginCache();
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                var results = dao.FindAll();
                foreach (var found in results)
                {
                    Assert.AreEqual(login.UserName, found.UserName);
                    Assert.AreEqual(login.Email, found.Email);
                    Assert.IsTrue(found.IsValidPassword("password"));

                    dao.Remove(found);
                    Assert.AreEqual(1, dao.SaveChanges());
                }
            }
        }

        /// <summary>
        /// 全ログインキャッシュを検索する（正常）
        /// 登録済みキャッシュがない場合はヒットしないこと
        /// </summary>
        [Test]
        public void TestFindAllNormalNoHit()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.IsEmpty(dao.FindAll());
            }
        }


        /**
         * Add
         * 正常系は検索系の試験で実施済みのため割愛
         **/

        /// <summary>
        /// エントリを追加する（エントリがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test]
        public void TestAddExceptionNoCache()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                try
                {
                    dao.Add(null);
                }
                catch (ArgumentNullException)
                {
                    Assert.AreEqual(0, dao.SaveChanges());
                }
            }
        }


        /**
         * Remove
         * 正常系は検索系の試験で実施済みのため割愛
         **/

        /// <summary>
        /// エントリを削除する（エントリが存在しない）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test]
        public void TestRemoveExceptionNoHit()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                try
                {
                    var login = CreateLoginCache();
                    dao.Remove(login);
                }
                catch (InvalidOperationException)
                {
                    Assert.AreEqual(0, dao.SaveChanges());
                }
            }
        }

        /// <summary>
        /// エントリを削除する（エントリがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test]
        public void TestRemoveExceptionNoCache()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                try
                {
                    dao.Remove(null);
                }
                catch (ArgumentNullException)
                {
                    Assert.AreEqual(0, dao.SaveChanges());
                }
            }
        }


        /**
         * RemoveAll
         **/

        /// <summary>
        /// 全エントリを削除する（正常）
        /// 登録済みキャッシュを全削除できること
        /// </summary>
        [Test]
        public void TestRemoveAllNormal()
        {
            var login = CreateLoginCache();
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(login);
                Assert.AreEqual(1, dao.SaveChanges());

                dao.RemoveAll();
                Assert.AreEqual(1, dao.SaveChanges());
                Assert.IsEmpty(dao.FindAll());
            }
        }

        /// <summary>
        /// 全エントリを削除する（登録済みキャッシュなし）
        /// Exceptionが発行されないこと
        /// </summary>
        [Test]
        public void TestRemoveAllNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                dao.RemoveAll();
                Assert.AreEqual(0, dao.SaveChanges());
                Assert.IsEmpty(dao.FindAll());
            }
        }
    }
}
