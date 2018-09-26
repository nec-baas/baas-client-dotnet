using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Reflection;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class ObjectBucketCacheTest
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
        public void TestBucketCache()
        {
            // INSERT
            var bucket = new ObjectBucketCache
            {
                Name = "test1",
                LastPullServerTime = "2015-01-01T00:00:00.000Z"
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(bucket);
                Assert.AreEqual(1, dao.SaveChanges());

                // QUERY
                var found = dao.FindByName("test1");
                Assert.AreEqual("test1", found.Name);
                Assert.AreEqual("2015-01-01T00:00:00.000Z", found.LastPullServerTime);
            }
        }

        [Test]
        public void TestLastUpdatedAt()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                Assert.IsNull(dao.GetLastPullServerTime("test1"));

                // INSERT
                var ts = "2015-01-01T00:00:00.000Z";
                Assert.AreEqual(1, dao.SaveLastPullServerTime("test1", ts));

                // QUERY
                Assert.AreEqual(ts, dao.GetLastPullServerTime("test1"));

                // UPDATE
                var ts2 = "2015-02-01T00:00:00.000Z";
                Assert.AreEqual(1, dao.SaveLastPullServerTime("test1", ts2));

                // QUERY
                Assert.AreEqual(ts2, dao.GetLastPullServerTime("test1"));
            }
        }


        /**
         * Constructor(ObjectBucketCacheDao)
         **/

        /// <summary>
        /// コンストラクタ（正常）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.IsNotNull(dao);
                FieldInfo context = typeof(ObjectBucketCacheDao).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.AreEqual(dbContext, context.GetValue(dao));
            }
        }

        /// <summary>
        /// コンストラクタ（contextがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionNoContext()
        {
            new ObjectBucketCacheDao(null);
        }


        /**
         * FindByName
         **/

        /// <summary>
        /// バケット名で検索（正常）
        /// バケット名で検索できること
        /// </summary>
        [Test]
        public void TestFindByNameNormal()
        {
            var bucket = new ObjectBucketCache
            {
                Name = "test",
                LastPullServerTime = "2015-01-01T00:00:00.000Z"
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(bucket);
                Assert.AreEqual(1, dao.SaveChanges());

                var found = dao.FindByName("test");
                Assert.AreEqual(bucket.Name, found.Name);
                Assert.AreEqual(bucket.LastPullServerTime, found.LastPullServerTime);

                Assert.AreEqual(0, dao.SaveChanges());
                dao.Remove(bucket);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        /// <summary>
        /// バケット名で検索（バケットキャッシュなし）
        /// ヒットしないこと
        /// </summary>
        [Test]
        public void TestFindByNameNormalNoHit()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.IsNull(dao.FindByName("test"));
            }
        }

        /// <summary>
        /// バケット名で検索（バケット名がnull）
        /// ヒットしないこと
        /// </summary>
        [Test]
        public void TestFindByNameSubNormalNoBuckeName()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.IsNull(dao.FindByName(null));
            }
        }


        /**
         * GetLastPullServerTime
         **/

        /// <summary>
        /// 最終Pullサーバ日時を取得（正常）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestGetLastPullServerTimeNormal()
        {
            var bucket = new ObjectBucketCache
            {
                Name = "test",
                LastPullServerTime = "2015-01-01T00:00:00.000Z"
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(bucket);
                Assert.AreEqual(1, dao.SaveChanges());

                var found = dao.GetLastPullServerTime("test");
                Assert.AreEqual(bucket.LastPullServerTime, found);

                Assert.AreEqual(0, dao.SaveChanges());
                dao.Remove(bucket);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        /// <summary>
        /// 最終Pullサーバ日時を取得（保存なし）
        /// ヒットしないこと
        /// </summary>
        [Test]
        public void TestGetLastPullServerTimeNormalNoHit()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.IsNull(dao.GetLastPullServerTime("test"));
            }
        }

        /// <summary>
        /// 最終Pullサーバ日時を取得（バケット名がnull）
        /// ヒットしないこと
        /// </summary>
        [Test]
        public void TestGetLastPullServerTimeSubNormalNoBuckeName()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.IsNull(dao.GetLastPullServerTime(null));
            }
        }


        /**
         * SaveLastPullServerTime
         **/

        /// <summary>
        /// 最終Pullサーバ日時を保存（正常）
        /// 保存（更新）できること
        /// </summary>
        [Test]
        public void TestSaveLastPullServerTimeNormal()
        {
            var bucket = new ObjectBucketCache
            {
                Name = "test",
                LastPullServerTime = "2015-01-01T00:00:00.000Z"
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(bucket);
                Assert.AreEqual(1, dao.SaveChanges());

                var UpdatePullServerTime = "2015-12-12T12:12:12.000Z";
                Assert.AreEqual(1, dao.SaveLastPullServerTime("test", UpdatePullServerTime));
                Assert.AreEqual(UpdatePullServerTime, dao.GetLastPullServerTime("test"));

                Assert.AreEqual(0, dao.SaveChanges());
                dao.Remove(bucket);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        /// <summary>
        /// 最終Pullサーバ日時を保存（指定バケット名のキャッシュなし）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveLastPullServerTimeNormalNoHit()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var UpdatePullServerTime = "2015-12-12T12:12:12.000Z";
                Assert.AreEqual(1, dao.SaveLastPullServerTime("test", UpdatePullServerTime));
                Assert.AreEqual(UpdatePullServerTime, dao.GetLastPullServerTime("test"));
            }
        }

        /// <summary>
        /// 最終Pullサーバ日時を保存（バケット名がnull）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveLastPullServerTimeSubNormalNoBacketName()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var UpdatePullServerTime = "2015-12-12T12:12:12.000Z";
                Assert.AreEqual(1, dao.SaveLastPullServerTime(null, UpdatePullServerTime));
                Assert.AreEqual(UpdatePullServerTime, dao.GetLastPullServerTime(null));
            }
        }

        /// <summary>
        /// 最終Pullサーバ日時を保存（lastTimeがnull）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveLastPullServerTimeSubNormalNoLastTime()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                Assert.AreEqual(1, dao.SaveLastPullServerTime("test", null));
                Assert.AreEqual(null, dao.GetLastPullServerTime("test"));
            }
        }


        /**
         * Add
         **/

        /// <summary>
        /// エントリ追加（cacheがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddExceptionNoCache()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                dao.Add(null);
            }
        }


        /**
         * Remove
         **/

        /// <summary>
        /// エントリ削除（データなし）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestRemoveNormalNoData()
        {
            var bucket = new ObjectBucketCache
            {
                Name = "test",
                LastPullServerTime = "2015-01-01T00:00:00.000Z"
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                dao.Remove(bucket);
            }
        }

        /// <summary>
        /// エントリ削除（catchがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRemoveExceptionNoCache()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                dao.Remove(null);
            }
        }


        /**
         * FindAll
         **/

        /// <summary>
        /// 全オブジェクトバケットキャッシュを検索する（正常）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestFindAllNormal()
        {
            var bucket = new ObjectBucketCache
            {
                Name = "test",
                LastPullServerTime = "2015-01-01T00:00:00.000Z"
            };
            var bucket2 = new ObjectBucketCache
            {
                Name = "test2",
                LastPullServerTime = "2015-02-02T00:00:00.000Z"
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Add(bucket);
                dao.Add(bucket2);
                Assert.AreEqual(2, dao.SaveChanges());

                var results = dao.FindAll();
                foreach (var found in results)
                {
                    if (found.Name.Equals(bucket.Name))
                    {
                        Assert.AreEqual(bucket.Name, found.Name);
                        Assert.AreEqual(bucket.LastPullServerTime, found.LastPullServerTime);
                    }
                    else
                    {
                        Assert.AreEqual(bucket2.Name, found.Name);
                        Assert.AreEqual(bucket2.LastPullServerTime, found.LastPullServerTime);
                    }
                }
                Assert.AreEqual(0, dao.SaveChanges());
                dao.Remove(bucket);
                dao.Remove(bucket2);
                Assert.AreEqual(2, dao.SaveChanges());
            }
        }

        /// <summary>
        /// 全オブジェクトバケットキャッシュを検索する（データなし）
        /// ヒットしないこと
        /// </summary>
        [Test]
        public void TestFindAllNormalNoHit()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.IsEmpty(dao.FindAll());
            }
        }
    }
}
