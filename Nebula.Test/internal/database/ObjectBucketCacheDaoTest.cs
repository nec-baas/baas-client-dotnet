using Moq;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Data.Entity;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class ObjectBucketCacheDaoTest
    {
        private NbDatabaseImpl _db;

        private Mock<NbManageDbContext> _mockContext;
        private ObjectBucketCacheDao _dao;

        private SQLiteConnection _connection;
        private DbSetBucketCache _dbSet;

        private const string BucketName = "TestBucket";

        class DbSetBucketCache : DbSet<ObjectBucketCache>
        {

        }

        [SetUp]
        public void SetUp()
        {
            _db = new NbDatabaseImpl(":memory:", null);
            _db.Open();


            _connection = new SQLiteConnection
            {
                ConnectionString = "Data Source=:memory:;"
            };
            _connection.Open();


            _mockContext = new Mock<NbManageDbContext>(_connection);
            _dao = new ObjectBucketCacheDao(_mockContext.Object);

            _dbSet = new DbSetBucketCache();
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;

            _db.Close();
            _db = null;
        }

        [Test]
        public void TestConstructorNormal()
        {
            var dao = new ObjectBucketCacheDao(_mockContext.Object);
            FieldInfo context = typeof(ObjectBucketCacheDao).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreSame(_mockContext.Object, context.GetValue(dao));
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionContextNull()
        {
            var dao = new ObjectBucketCacheDao(null);
        }

        // FindByName
        [Test]
        public void TestFindByNameNormal()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = BucketName,
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "differentCache",
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultCache = dao.FindByName(BucketName);

                Assert.AreEqual(cache1.Id, resultCache.Id);
                Assert.AreEqual(cache1.Name, resultCache.Name);
            }
        }

        [Test]
        public void TestFindByNameNormalNoData()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = "abcdefg",
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "xyz",
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultCache = dao.FindByName(BucketName);
                Assert.IsNull(resultCache);
            }
        }

        [Test]
        public void TestFindByNameNormalNoBucketName()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                var resultCache = dao.FindByName(null);
                Assert.IsNull(resultCache);
            }
        }

        // GetLastPullServerTime
        [Test]
        public void TestGetLastPullServerTimeNormal()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = BucketName,
                LastPullServerTime = "aaaa"
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "differentCache",
                LastPullServerTime = "bbbb"
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultPullTime = dao.GetLastPullServerTime(BucketName);

                Assert.AreEqual(cache1.LastPullServerTime, resultPullTime);
            }
        }

        [Test]
        public void TestGetLastPullServerTimeNormalNoMatchData()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = "abcdefg",
                LastPullServerTime = "aaaa"
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "xyz",
                LastPullServerTime = "bbbb"
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultPullTime = dao.GetLastPullServerTime(BucketName);
                Assert.IsNull(resultPullTime);
            }
        }

        [Test]
        public void TestGetLastPullServerTimeNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                var resultPullTime = dao.GetLastPullServerTime(null);
                Assert.IsNull(resultPullTime);
            }
        }

        // GetSyncScope
        [Test]
        public void TestGetSyncScopeNormal()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = "differentCache",
                SyncScope = "aaaa"
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = BucketName,
                SyncScope = "bbbb"
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultSyncScope = dao.GetSyncScope(BucketName);

                Assert.AreEqual(cache2.SyncScope, resultSyncScope);
            }
        }

        [Test]
        public void TestGetSyncScopeNormalNoMatchData()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = "abcdefg",
                SyncScope = "aaaa"
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "xyz",
                SyncScope = "bbbb"
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultSyncScope = dao.GetSyncScope(BucketName);
                Assert.IsNull(resultSyncScope);
            }
        }

        [Test]
        public void TestGetSyncScopeNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                var resultSyncScope = dao.GetSyncScope(null);
                Assert.IsNull(resultSyncScope);
            }
        }

        // GetLastSyncTime
        [Test]
        public void TestGetLastSyncTimeNormal()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = BucketName,
                LastSyncTime = "aaaa"
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "differentCache",
                LastSyncTime = "bbbb"
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultLastSyncTime = dao.GetLastSyncTime(BucketName);

                Assert.AreEqual(cache1.LastSyncTime, resultLastSyncTime);
            }
        }

        [Test]
        public void TestGetLastSyncTimeNormalNoMatchData()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = "abcdefg",
                LastPullServerTime = "aaaa"
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "xyz",
                LastPullServerTime = "bbbb"
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var resultLastSyncTime = dao.GetLastSyncTime(BucketName);
                Assert.IsNull(resultLastSyncTime);
            }
        }

        [Test]
        public void TestGetLastSyncTimeNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                var resultLastSyncTime = dao.GetLastSyncTime(null);
                Assert.IsNull(resultLastSyncTime);
            }
        }

        // SaveLastPullServerTime
        [Test]
        public void TestSaveLastPullServerTimeNormal()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var count = dao.SaveLastPullServerTime(BucketName, "time");
                Assert.AreEqual(1, count);

                var saved = dao.GetLastPullServerTime(BucketName);
                Assert.AreEqual("time", saved);
            }
        }

        [Test]
        public void TestSaveLastPullServerTimeNormalOverwrite()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var count = dao.SaveLastPullServerTime(BucketName, "time");
                Assert.AreEqual(1, count);
                count = dao.SaveLastPullServerTime(BucketName, "time2");
                Assert.AreEqual(1, count);

                var saved = dao.GetLastPullServerTime(BucketName);
                Assert.AreEqual("time2", saved);
            }
        }

        [Test]
        public void TestSaveLastPullServerTimeNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var count = dao.SaveLastPullServerTime(BucketName, null);
                Assert.AreEqual(1, count);

                var saved = dao.GetLastPullServerTime(BucketName);
                Assert.AreEqual(null, saved);
            }
        }

        // SaveSyncScope
        [Test]
        public void TestSaveSyncScopeNormalWithTimeInfos()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = "abcdefg",
                LastPullServerTime = "aaaa",
                LastSyncTime = "xxxx",
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = BucketName,
                LastPullServerTime = "bbbb",
                LastSyncTime = "yyyy",
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                dao.Add(cache1);
                dao.Add(cache2);
                Assert.AreEqual(2, dao.SaveChanges());

                var count = dao.SaveSyncScope(BucketName, "scope");
                Assert.AreEqual(1, count);

                var saved = dao.GetSyncScope(BucketName);
                Assert.AreEqual("scope", saved);
                saved = dao.GetLastPullServerTime(BucketName);
                Assert.IsNull(saved);
                saved = dao.GetLastSyncTime(BucketName);
                Assert.IsNull(saved);
            }
        }

        [Test]
        public void TestSaveSyncScopeNormalOverwrite()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var count = dao.SaveSyncScope(BucketName, "scope");
                Assert.AreEqual(1, count);
                count = dao.SaveSyncScope(BucketName, "scope2");
                Assert.AreEqual(1, count);

                var saved = dao.GetSyncScope(BucketName);
                Assert.AreEqual("scope2", saved);
            }
        }

        [Test]
        public void TestSaveSyncScopeNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var count = dao.SaveSyncScope(BucketName, null);
                Assert.AreEqual(1, count);

                var saved = dao.GetSyncScope(BucketName);
                Assert.AreEqual(null, saved);
            }
        }

        // SaveLastSyncTime
        [Test]
        public void TestSaveLastSyncTimeNormal()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var count = dao.SaveLastSyncTime(BucketName, "time");
                Assert.AreEqual(1, count);

                var resultLastSyncTime = dao.GetLastSyncTime(BucketName);
                Assert.AreEqual("time", resultLastSyncTime);
            }
        }

        [Test]
        public void TestSaveLastSyncTimeNromalOverwrite()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var count = dao.SaveLastSyncTime(BucketName, "time");
                Assert.AreEqual(1, count);
                count = dao.SaveLastSyncTime(BucketName, "time2");
                Assert.AreEqual(1, count);

                var resultLastSyncTime = dao.GetLastSyncTime(BucketName);
                Assert.AreEqual("time2", resultLastSyncTime);
            }
        }

        [Test]
        public void TestSaveLastSyncTimeNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                var count = dao.SaveLastSyncTime(BucketName, null);
                Assert.AreEqual(1, count);

                var resultLastSyncTime = dao.GetLastSyncTime(BucketName);
                Assert.IsNull(resultLastSyncTime);
            }
        }

        // GetBucketCache
        [Test]
        public void TestGetBucketCacheNormalBucketExists()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = BucketName,
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                var cache = dao.getBucketCache(BucketName);

                Assert.AreEqual(cache1.Name, cache.Name);
            }
        }

        [Test]
        public void TestGetBucketCacheNormalNewData()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = "differentBucket",
            };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                dao.Add(cache1);
                Assert.AreEqual(1, dao.SaveChanges());

                var cache = dao.getBucketCache(BucketName);

                Assert.AreEqual(BucketName, cache.Name);
            }
        }

        // Add
        [Test]
        public void TestAddNormal()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = BucketName,
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());

                dao.Add(cache1);
                Assert.AreEqual(1, dao.SaveChanges());
            }
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddExceptionNoBucketCache()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Add(null);
            }
        }

        // Remove
        [Test]
        public void TestRemoveNormal()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = BucketName,
                SyncScope = "exists"
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                Assert.AreEqual(0, dao.SaveChanges());

                dao.Add(cache1);
                Assert.AreEqual(1, dao.SaveChanges());

                dao.Remove(cache1);
                Assert.AreEqual(1, dao.SaveChanges());

                var afterRemove = dao.getBucketCache(BucketName);
                Assert.AreEqual(BucketName, afterRemove.Name);
                Assert.IsNull(afterRemove.SyncScope);
            }
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRemoveExceptionNoBucketCache()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                dao.Remove(null);
            }
        }

        // FindAll
        [Test]
        public void TestFindAllNormalOneData()
        {
            var cache1 = new ObjectBucketCache
           {
               Id = 1,
               Name = BucketName,
           };

            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                dao.Add(cache1);
                dao.SaveChanges();

                var results = dao.FindAll();
                Assert.AreEqual(1, results.Count());

                var cacheA = results.First();
                Assert.AreEqual(cache1.Id, cacheA.Id);
                Assert.AreEqual(cache1.Name, cacheA.Name);
            }
        }

        [Test]
        public void TestFindAllNormalTwoData()
        {
            var cache1 = new ObjectBucketCache
            {
                Id = 1,
                Name = BucketName,
            };
            var cache2 = new ObjectBucketCache
            {
                Id = 2,
                Name = "ABC",
            };
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                dao.Add(cache1);
                dao.Add(cache2);
                dao.SaveChanges();

                var results = dao.FindAll();
                Assert.AreEqual(2, results.Count());

                var cachesA = from x in results where x.Id == cache1.Id select x;
                Assert.AreEqual(1, cachesA.Count());
                var cacheA = cachesA.First();
                Assert.AreEqual(cache1.Name, cacheA.Name);

                var cachesB = from x in results where x.Id == cache2.Id select x;
                Assert.AreEqual(1, cachesB.Count());
                var cacheB = cachesB.First();
                Assert.AreEqual(cache2.Name, cacheB.Name);
            }
        }

        [Test]
        public void TestFindAllNormalNoData()
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);

                var results = dao.FindAll();
                Assert.AreEqual(0, results.Count());
            }
        }

    }
}
