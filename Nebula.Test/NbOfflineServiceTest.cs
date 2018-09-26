using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbOfflineServiceTest
    {
        private ProcessState _processState;

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();
            _processState = ProcessState.GetInstance();
            TryDeleteDbFile();
        }

        [TearDown]
        public void TearDown()
        {
            NbOfflineService.SetInMemoryMode(false);
            FieldInfo context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            context.SetValue(null, null);
            NbService.GetInstance().DisableOffline();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            TryDeleteDbFile();
        }

        private bool isDbFileExists()
        {
            return File.Exists(GetDbPath());
        }

        private string GetDbPath()
        {
            var service = NbService.GetInstance();
            var idpath = service.TenantId + "/" + service.AppId;
            var documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fulldbpath = "NEC/Nebula/" + idpath + "/offline.db";
            return Path.Combine(documentFolder, fulldbpath);
        }

        /**
         * Constructor(NbOfflineService)
         **/

        /// <summary>
        /// コンストラクタ（正常）
        /// インスタンスを生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            Assert.IsNotNull(new NbOfflineService());
        }


        /**
         * EnableOfflineService
         **/

        /// <summary>
        /// オフラインサービスを有効にする（正常）
        /// オフラインサービスを有効にできること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormal()
        {
            FieldInfo context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNull((string)context.GetValue(null));
            Assert.IsNull(NbService.GetInstance().OfflineService);
            Assert.IsFalse(NbOfflineService.MemoryMode);
            Assert.IsFalse(isDbFileExists());

            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull((string)context.GetValue(null));
            Assert.IsNotNull(NbService.GetInstance().OfflineService);
            Assert.IsFalse(NbOfflineService.MemoryMode);
            Assert.IsTrue(isDbFileExists());

            AssertAutoVacuum();
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>("test");
            NbOfflineService.EnableOfflineService(NbService.GetInstance());
            AssertAutoVacuum();
        }

        /// <summary>
        /// オフラインサービスを有効にする（パスワード設定あり）
        /// オフラインサービスを有効にできること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceWithPasswordNormal()
        {
            FieldInfo context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNull((string)context.GetValue(null));
            Assert.IsNull(NbService.GetInstance().OfflineService);
            Assert.IsFalse(NbOfflineService.MemoryMode);
            Assert.IsFalse(isDbFileExists());

            NbOfflineService.EnableOfflineService(NbService.GetInstance(), "password");

            context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull((string)context.GetValue(null));
            Assert.IsNotNull(NbService.GetInstance().OfflineService);
            Assert.IsNotNull(NbService.GetInstance().OfflineService.Database);
            Assert.IsFalse(NbOfflineService.MemoryMode);
            Assert.IsTrue(isDbFileExists());

            AssertAutoVacuum();
        }

        /// <summary>
        /// オフラインサービスを有効にする（インメモリモード）
        /// オフラインサービスを有効にできること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceWithNormalInMemory()
        {
            FieldInfo context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNull((string)context.GetValue(null));
            Assert.IsNull(NbService.GetInstance().OfflineService);
            Assert.IsFalse(NbOfflineService.MemoryMode);
            Assert.IsFalse(isDbFileExists());

            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull((string)context.GetValue(null));
            Assert.IsNotNull(NbService.GetInstance().OfflineService);
            Assert.IsNotNull(NbService.GetInstance().OfflineService.Database);
            Assert.IsTrue(NbOfflineService.MemoryMode);
            Assert.IsFalse(isDbFileExists());
        }

        /// <summary>
        /// オフラインサービスを有効にする（サービスがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestEnableOfflineServiceExceptionNoService()
        {
            NbOfflineService.EnableOfflineService(null);
        }

        /// <summary>
        /// オフラインサービスを有効にする（テナントIDがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionNoTenantID()
        {
            var service = NbService.GetInstance();
            service.TenantId = null;

            NbOfflineService.EnableOfflineService(service);
        }

        /// <summary>
        /// オフラインサービスを有効にする（テナントIDが空文字）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionBlankTenantID()
        {
            var service = NbService.GetInstance();
            service.TenantId = "";

            NbOfflineService.EnableOfflineService(service);
        }

        /// <summary>
        /// オフラインサービスを有効にする（AppIDがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionNoAppID()
        {
            var service = NbService.GetInstance();
            service.AppId = null;

            NbOfflineService.EnableOfflineService(service);
        }

        /// <summary>
        /// オフラインサービスを有効にする（AppIDが空文字）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionBlankAppID()
        {
            var service = NbService.GetInstance();
            service.AppId = "";

            NbOfflineService.EnableOfflineService(service);
        }

        /// <summary>
        /// オフラインサービスを有効にする（テナントIDにフォルダ名に使用できない文字）
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestEnableOfflineServiceExceptionBadTenantID()
        {
            var service = NbService.GetInstance();
            service.TenantId = "***";

            NbOfflineService.EnableOfflineService(service);
        }

        /// <summary>
        /// オフラインサービスを有効にする（AppIDにフォルダ名に使用できない文字）
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestEnableOfflineServiceExceptionBadAppID()
        {
            var service = NbService.GetInstance();
            service.AppId = "***";

            NbOfflineService.EnableOfflineService(service);
        }


        /**
         * CloseDatabase
         **/

        /// <summary>
        /// データベースをクローズする（正常）
        /// Databeseプロパティがnullとなること
        /// </summary>
        [Test]
        public void TestCloseDatabaseNormal()
        {
            NbOfflineService.EnableOfflineService(NbService.GetInstance());
            Assert.IsNotNull(NbService.GetInstance().OfflineService.Database);
            NbService.GetInstance().OfflineService.CloseDatabase();
            Assert.IsNull(NbService.GetInstance().OfflineService.Database);
        }

        /// <summary>
        /// データベースをクローズする（データベースなし）
        /// Databeseプロパティがnullとなること
        /// </summary>
        [Test]
        public void TestCloseDatabaseNormalNoDatabase()
        {
            var service = new NbOfflineService();
            service.CloseDatabase();
            Assert.IsNull(service.Database);
        }


        /**
         * DeleteOfflineDatabase
         **/

        /// <summary>
        /// データベースファイルを強制削除する（正常）
        /// データベースファイルが削除されていること
        /// </summary>
        [Test]
        public void TestDeleteOfflineDatabaseNormal()
        {
            File.Create(GetDbPath()).Close(); // ダミーファイル作成
            Assert.IsTrue(isDbFileExists());
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var service = NbService.GetInstance();
            var idpath = service.TenantId + "/" + service.AppId;
            var offline = new NbOfflineService();
            FieldInfo context = typeof(NbOfflineService).GetField("_sIdpath", BindingFlags.NonPublic | BindingFlags.Static);
            context.SetValue((object)offline, idpath);
            
            offline.DeleteOfflineDatabase();
            Assert.IsFalse(isDbFileExists());
        }

        // <summary>
        /// データベースファイルを強制削除する（データベースなし）
        /// Exceptionが発生しないこと
        /// </summary>
        [Test]
        public void TestDeleteOfflineDatabaseNormalNoDatabase()
        {
            var offline = new NbOfflineService();
            offline.DeleteOfflineDatabase();
        }


        /**
         * ChangeOfflineDatabasePassword
         **/

        /// <summary>
        /// オフラインデータベースのパスワードを変更する（正常）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestChangeOfflineDatabasePasswordNormal()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            NbService.Singleton.OfflineService.ChangeOfflineDatabasePassword("test");
        }

        /// <summary>
        /// オフラインデータベースのパスワードを変更する（オフラインサービスが無効）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestChangeOfflineDatabasePasswordSubnormalDisableOffline()
        {
            var offline = new NbOfflineService();
            offline.ChangeOfflineDatabasePassword("test");
        }

        /// <summary>
        /// オフラインデータベースのパスワードを変更する（新パスワードがnull）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestChangeOfflineDatabasePasswordNormalNoNewPassword()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            NbService.Singleton.OfflineService.ChangeOfflineDatabasePassword(null);
        }


        /**
         * SetInMemoryMode
         **/

        /// <summary>
        /// テスト用にインメモリDBを使用する（true）
        /// </summary>
        [Test]
        public void TestSetInMemoryModeNormalModetrue()
        {
            NbOfflineService.SetInMemoryMode();
            Assert.IsTrue(NbOfflineService.MemoryMode);
        }

        /// <summary>
        /// テスト用にインメモリDBを使用する（false）
        /// </summary>
        [Test]
        public void TestSetInMemoryModeNormalModefalse()
        {
            NbOfflineService.SetInMemoryMode(false);
            Assert.IsFalse(NbOfflineService.MemoryMode);
        }


        /**
         * DeleteAll
         **/

        private void CreateObjectCaches()
        {
            var _objectCache = new NbObjectCache(NbService.GetInstance());
            _objectCache.CreateCacheTable("test");

            for (int i = 0; i < 2; i++)
            {
                var obj = new NbOfflineObject("test");
                obj["key"] = i;
                var ret1 = obj.SaveAsync().Result;
            }
        }

        private void CreateObjectBucketCaches()
        {
            var db = (NbDatabaseImpl)NbService.GetInstance().OfflineService.Database;
            using (var transaction = db.BeginTransaction())
            {
                using (var dbContext = db.CreateDbContext())
                {
                    var dao = new ObjectBucketCacheDao(dbContext);
                    for (int i = 0; i < 2; i++)
                    {
                        var bucket = new ObjectBucketCache
                        {
                            Name = "test" + i,
                            LastPullServerTime = "2015-01-01T00:00:00.000Z"
                        };
                        dao.Add(bucket);
                        dao.SaveChanges();
                    }
                }
                transaction.Commit();
            }
        }

        private bool isObjectCacheExists()
        {
            var _objectCache = new NbObjectCache(NbService.GetInstance());
            var db = (NbDatabaseImpl)NbService.GetInstance().OfflineService.Database;
            var tables = _objectCache.GetTables();
            foreach (var table in tables)
            {
                using (var reader = db.SelectForReader(table, new[] { "json", "state" }, null, null, 0, 1))
                {
                    if (reader.Read()) return true;
                }
            }
            return false;
        }

        private bool isBucketCacheExists()
        {
            var db = (NbDatabaseImpl)NbService.GetInstance().OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                var found = dao.FindAll();
                return found.Count() != 0 ? true : false;
            }
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする（正常）
        /// すべてクリアできること
        /// </summary>
        [Test]
        public async void TestDeleteCacheAllNormal()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            CreateObjectCaches();
            Assert.IsTrue(isObjectCacheExists());
            CreateObjectBucketCaches();
            Assert.IsTrue(isBucketCacheExists());

            await NbOfflineService.DeleteCacheAll();

            Assert.IsFalse(isObjectCacheExists());
            Assert.IsFalse(isBucketCacheExists());

            // 状態確認
            Assert.IsFalse(_processState.Crud);
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする（オブジェクトキャッシュなし）
        /// バケットキャッシュをクリアできること
        /// </summary>
        [Test]
        public async void TestDeleteCacheAllNormalNoObjectCache()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            CreateObjectBucketCaches();
            Assert.IsFalse(isObjectCacheExists());
            Assert.IsTrue(isBucketCacheExists());

            await NbOfflineService.DeleteCacheAll();

            Assert.IsFalse(isObjectCacheExists());
            Assert.IsFalse(isBucketCacheExists());

            // 状態確認
            Assert.IsFalse(_processState.Crud);
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする（バケットキャッシュなし）
        /// オブジェクトキャッシュをクリアできること
        /// </summary>
        [Test]
        public async void TestDeleteCacheAllNormalNoBucketCache()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            CreateObjectCaches();
            Assert.IsTrue(isObjectCacheExists());
            Assert.IsFalse(isBucketCacheExists());

            await NbOfflineService.DeleteCacheAll();
            Assert.IsFalse(isObjectCacheExists());
            Assert.IsFalse(isBucketCacheExists());

            // 状態確認
            Assert.IsFalse(_processState.Crud);
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする（同期中）
        /// NbExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestDeleteCacheAllExceptionWhileSyncing()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            CreateObjectCaches();
            Assert.IsTrue(isObjectCacheExists());
            CreateObjectBucketCaches();
            Assert.IsTrue(isBucketCacheExists());

            // 擬似的に同期状態を作る
            _processState.TryStartSync();

            try
            {
                await NbOfflineService.DeleteCacheAll();
                Assert.Fail("No exception");
            }
            catch (NbException ex)
            {
                Assert.AreEqual(NbStatusCode.Locked, ex.StatusCode);
                Assert.AreEqual("Locked.", ex.Message);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

            Assert.IsTrue(isObjectCacheExists());
            Assert.IsTrue(isBucketCacheExists());

            // 状態確認
            Assert.IsFalse(_processState.Crud);

            // 後始末
            _processState.EndSync();
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする（同期終了後）
        /// すべてクリアできること
        /// </summary>
        [Test]
        public async void TestDeleteCacheAllNormalAfterSyncing()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            CreateObjectCaches();
            Assert.IsTrue(isObjectCacheExists());
            CreateObjectBucketCaches();
            Assert.IsTrue(isBucketCacheExists());

            // 擬似的に同期状態を作る
            _processState.TryStartSync();
            // 同期を終了させる
            _processState.EndSync();

            await NbOfflineService.DeleteCacheAll();

            Assert.IsFalse(isObjectCacheExists());
            Assert.IsFalse(isBucketCacheExists());

            // 状態確認
            Assert.IsFalse(_processState.Crud);
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする（CRUD中）
        /// CRUD終了まで待ち合わせた後ですべてクリアできること
        /// </summary>
        [Test]
        public async void TestDeleteCacheAllNormalWhileCrud()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.GetInstance());

            CreateObjectCaches();
            Assert.IsTrue(isObjectCacheExists());
            CreateObjectBucketCaches();
            Assert.IsTrue(isBucketCacheExists());

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            Task task = new Task(() =>
            {
                Thread.Sleep(500);
                _processState.EndCrud();
            });
            task.Start();

            await NbOfflineService.DeleteCacheAll();

            Assert.IsFalse(isObjectCacheExists());
            Assert.IsFalse(isBucketCacheExists());

            // 状態確認
            Assert.IsFalse(_processState.Crud);
        }

        private void AssertAutoVacuum()
        {
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            var vacuum = db.RawQuery("PRAGMA auto_vacuum", null);
            vacuum.Read();
            Assert.AreEqual(1, vacuum.GetInt32(0));
            vacuum.Dispose();
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする（オフライン無効）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteCacheAllExceptionOfflineDisabled()
        {
            await NbOfflineService.DeleteCacheAll();
        }

        private void TryDeleteDbFile()
        {
            NbOfflineService.SetInMemoryMode(false);
            NbService.Singleton.DisableOffline();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            bool worked = false;
            int tries = 1;
            if (File.Exists(GetDbPath()))
            {
                while ((tries < 5) && (!worked))
                {
                    try
                    {
                        Task.Delay(tries * 100).Wait();
                        File.Delete(GetDbPath());
                        worked = true;
                    }
                    catch (IOException)
                    {
                        tries++;
                    }
                }
                if (!worked)
                    throw new IOException("can not delete file");
            }
        }
    }
}
