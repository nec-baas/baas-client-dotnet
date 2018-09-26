using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class DatabaseIT
    {
        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            TryDeleteDbFile();
            ITUtil.InitOnlineUser().Wait();
        }

        [TearDown]
        public void TearDown()
        {
            NbService.Singleton.DisableOffline();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            TryDeleteDbFile();
        }

        private const string ObjectBucketName = "ObjectTable";

        /**
         * EnableOfflineService
         **/

        /// <summary>
        /// データベースの初期化（暗号化指定なし）
        /// 初期化できること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormal()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            DatabaseAccessCommon();
            Assert.IsTrue(File.Exists(GetDbPath()));
            AssertAutoVacuum();
        }

        /// <summary>
        /// データベースの初期化（インメモリ指定）
        /// 初期化できること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormalInmemory()
        {
            NbOfflineService.SetInMemoryMode();
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            DatabaseAccessCommon();
            Assert.IsFalse(File.Exists(GetDbPath()));
            NbOfflineService.SetInMemoryMode(false);
        }

        /// <summary>
        /// データベースの初期化（サービスがnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestEnableOfflineServiceExceptionNoService()
        {
            NbOfflineService.EnableOfflineService(null);
        }

        /// <summary>
        /// データベースの初期化（TenantIDがnull）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionNoTenantId()
        {
            NbService.Singleton.TenantId = null;
            NbOfflineService.EnableOfflineService(NbService.Singleton);
        }

        /// <summary>
        /// データベースの初期化（AppIDがnull）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionNoAppId()
        {
            NbService.Singleton.AppId = null;
            NbOfflineService.EnableOfflineService(NbService.Singleton);
        }

        /// <summary>
        /// データベースの初期化（TenantIDが空）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionBlankTenantId()
        {
            NbService.Singleton.TenantId = "";
            NbOfflineService.EnableOfflineService(NbService.Singleton);
        }

        /// <summary>
        /// データベースの初期化（AppIDが空）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionBlankAppId()
        {
            NbService.Singleton.AppId = "";
            NbOfflineService.EnableOfflineService(NbService.Singleton);
        }

        /// <summary>
        /// データベースの初期化（暗号化指定あり）
        /// 初期化できること。暗号化されていること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormalEncryption()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            DatabaseAccessCommon();
            AssertAutoVacuum();
        }

        /// <summary>
        /// データベースの初期化（データベースファイルなし）
        /// 初期化できること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormalNoDatabase()
        {
            TryDeleteDbFile();
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            DatabaseAccessCommon();
            AssertAutoVacuum();
        }

        /// <summary>
        /// データベースの初期化（暗号化なし、オープン済み）
        /// 初期化できること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormalOpened()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            CreateDeleteObjectTable();
            AssertAutoVacuum();
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            DatabaseAccessCommon();
            AssertAutoVacuum();
            CreateDeleteObjectTable(false);
        }

        /// <summary>
        /// データベースの初期化（暗号化あり、未オープン）
        /// 初期化できること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormalNotOpenEncript()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            CreateDeleteObjectTable();
            NbService.Singleton.DisableOffline();
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            DatabaseAccessCommon();
            AssertAutoVacuum();
            NbService.Singleton.DisableOffline();

            try
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton);
                DatabaseAccessCommon();
                Assert.Fail("No Exception!");
            }
            catch (SQLiteException)
            {
            }
        }

        /// <summary>
        /// データベースの初期化（暗号化あり、オープン済み）
        /// 初期化できること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormalOpenedEncript()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            CreateDeleteObjectTable();
            AssertAutoVacuum();
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            DatabaseAccessCommon();
            AssertAutoVacuum();
            try
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton);
                DatabaseAccessCommon();
                Assert.Fail("No Exception!");
            }
            catch (SQLiteException)
            {
            }
        }


        /**
         * ChangeDatabasePassword
         **/

        /// <summary>
        /// パスワード変更（パスワード設定なしから任意のパスワード設定）
        /// 変更できること
        /// </summary>
        [Test]
        public void TestChangeDatabasePasswordNormalSet()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            CreateDeleteObjectTable();
            NbService.Singleton.ChangeDatabasePassword(ITUtil.Password);
            NbService.Singleton.DisableOffline();
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            DatabaseAccessCommon();
            NbService.Singleton.DisableOffline();
            try
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton);
                DatabaseAccessCommon();
                Assert.Fail("No Exception!");
            }
            catch (SQLiteException)
            {
            }
        }

        /// <summary>
        /// パスワード変更（パスワード設定ありから任意のパスワード設定）
        /// 変更できること
        /// </summary>
        [Test]
        public void TestChangeDatabasePasswordNormalChange()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton, "passpass");
            CreateDeleteObjectTable();
            NbService.Singleton.ChangeDatabasePassword(ITUtil.Password);
            NbService.Singleton.DisableOffline();
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            DatabaseAccessCommon();
            NbService.Singleton.DisableOffline();
            try
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton, "passpass");
                DatabaseAccessCommon();
                Assert.Fail("No Exception!");
            }
            catch (SQLiteException)
            {
            }
        }

        /// <summary>
        /// パスワード変更（パスワード設定なしからnullを設定）
        /// 暗号化されていないこと
        /// </summary>
        [Test]
        public void TestChangeDatabasePasswordSubnormalnullSet()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            CreateDeleteObjectTable();
            NbService.Singleton.ChangeDatabasePassword(null);
            NbService.Singleton.DisableOffline();
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            DatabaseAccessCommon();
            NbService.Singleton.DisableOffline();
            try
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton, "passpass");
                DatabaseAccessCommon();
                Assert.Fail("No Exception!");
            }
            catch (SQLiteException)
            {
            }
        }

        /// <summary>
        /// パスワード変更（パスワード設定ありからnullを設定）
        /// 暗号化されていないこと
        /// </summary>
        [Test]
        public void TestChangeDatabasePasswordSubnormalnullChange()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
            CreateDeleteObjectTable();
            NbService.Singleton.ChangeDatabasePassword(null);
            NbService.Singleton.DisableOffline();
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            DatabaseAccessCommon();
            NbService.Singleton.DisableOffline();
            try
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton, ITUtil.Password);
                DatabaseAccessCommon();
                Assert.Fail("No Exception!");
            }
            catch (SQLiteException)
            {
            }
        }

        /// <summary>
        /// パスワード変更（パスワード設定ありから任意のパスワード設定、オフライン機能無効）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestChangeDatabasePasswordExceptionDisabledOffline()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton, "passpass");
            CreateDeleteObjectTable();
            NbService.Singleton.DisableOffline();
            NbService.Singleton.ChangeDatabasePassword(ITUtil.Password);
        }


        /**
         * CloseDatabase
         **/

        /// <summary>
        /// データベースのクローズ（未オープン）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestCloseDatabaseNormalNotOpen()
        {
            NbService.Singleton.DisableOffline();
            Assert.IsFalse(NbService.Singleton.IsOfflineEnabled());
        }

        /// <summary>
        /// データベースのクローズ（オープン済み）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestCloseDatabaseNormalOpened()
        {
            NbOfflineService.EnableOfflineService(NbService.Singleton, "passpass");
            CreateDeleteObjectTable();
            NbService.Singleton.DisableOffline();
            Assert.IsFalse(NbService.Singleton.IsOfflineEnabled());
        }


        /**
         * CRUD操作
         **/

        /// <summary>
        /// オブジェクトキャッシュ作成（オブジェクトキャッシュなし）
        /// 作成できること
        /// </summary>
        [Test]
        public void TestDatabaseCreateNormalNoObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Create);
        }

        /// <summary>
        /// オブジェクトキャッシュ作成（オブジェクトキャッシュあり）
        /// 作成できること
        /// </summary>
        [Test]
        public void TestDatabaseCreateNormalObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Create);
            ObjectCacheTestCommon(CrudMode.Create);
        }

        /// <summary>
        /// ログインキャッシュ作成（ログインキャッシュなし）
        /// 作成できること
        /// </summary>
        [Test]
        public void TestDatabaseCreateNormalNoLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Create);
        }

        /// <summary>
        /// ログインキャッシュ作成（ログインキャッシュあり）
        /// 作成できること
        /// </summary>
        [Test]
        public void TestDatabaseCreateNormalLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Create);
            LoginCacheTestCommon(CrudMode.Create, false);
        }

        /// <summary>
        /// バケットキャッシュ作成（バケットキャッシュなし）
        /// 作成できること
        /// </summary>
        [Test]
        public void TestDatabaseCreateNormalNoBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Create);
        }

        /// <summary>
        /// バケットキャッシュ作成（バケットキャッシュあり）
        /// 作成できること
        /// </summary>
        [Test]
        public void TestDatabaseCreateNormalBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Create);
            BucketCacheTestCommon(CrudMode.Create, false);
        }

        /// <summary>
        /// オブジェクトキャッシュ取得（オブジェクトキャッシュなし）
        /// 取得できないこと
        /// </summary>
        [Test]
        public void TestDatabaseReadNormalNoObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Read, false);
        }

        /// <summary>
        /// オブジェクトキャッシュ取得（オブジェクトキャッシュあり）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestDatabaseReadNormalObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Create);
            ObjectCacheTestCommon(CrudMode.Read);
        }

        /// <summary>
        /// ログインキャッシュ取得（ログインキャッシュなし）
        /// 取得できないこと
        /// </summary>
        [Test]
        public void TestDatabaseReadNormalNoLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Read, false);
        }

        /// <summary>
        /// ログインキャッシュ取得（ログインキャッシュあり）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestDatabaseReadNormalLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Create);
            LoginCacheTestCommon(CrudMode.Read);
        }

        /// <summary>
        /// バケットキャッシュ取得（バケットキャッシュなし）
        /// 取得できないこと
        /// </summary>
        [Test]
        public void TestDatabaseReadNormalNoBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Read, false);
        }

        /// <summary>
        /// バケットキャッシュ取得（バケットキャッシュあり）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestDatabaseReadNormalBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Create);
            BucketCacheTestCommon(CrudMode.Read);
        }

        /// <summary>
        /// オブジェクトキャッシュ更新（オブジェクトキャッシュなし）
        /// 更新できないこと
        /// </summary>
        [Test]
        public void TestDatabaseUpdateNormalNoObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Update, false);
        }

        /// <summary>
        /// オブジェクトキャッシュ更新（オブジェクトキャッシュあり）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestDatabaseUpdateNormalObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Create);
            ObjectCacheTestCommon(CrudMode.Update);
        }

        /// <summary>
        /// ログインキャッシュ更新（ログインキャッシュなし）
        /// 更新できないこと
        /// </summary>
        [Test]
        public void TestDatabaseUpdateNormalNoLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Update, false);
        }

        /// <summary>
        /// ログインキャッシュ更新（ログインキャッシュあり）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestDatabaseUpdateNormalLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Create);
            LoginCacheTestCommon(CrudMode.Update);
        }

        /// <summary>
        /// バケットキャッシュ更新（バケットキャッシュなし）
        /// 更新できないこと
        /// </summary>
        [Test]
        public void TestDatabaseUpdateNormalNoBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Update, false);
        }

        /// <summary>
        /// バケットキャッシュ更新（バケットキャッシュあり）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestDatabaseUpdateNormalBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Create);
            BucketCacheTestCommon(CrudMode.Update);
        }

        /// <summary>
        /// オブジェクトキャッシュ削除（オブジェクトキャッシュなし）
        /// 削除できないこと
        /// </summary>
        [Test]
        public void TestDatabaseDeleteNormalNoObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Delete, false);
        }

        /// <summary>
        /// オブジェクトキャッシュ削除（オブジェクトキャッシュあり）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDatabaseDeleteNormalObjectCache()
        {
            EnsureOffline();
            ObjectCacheTestCommon(CrudMode.Create);
            ObjectCacheTestCommon(CrudMode.Delete);
        }

        /// <summary>
        /// ログインキャッシュ削除（ログインキャッシュなし）
        /// 削除できないこと
        /// </summary>
        [Test]
        public void TestDatabaseDeleteNormalNoLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Delete, false);
        }

        /// <summary>
        /// ログインキャッシュ削除（ログインキャッシュあり）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDatabaseDeleteNormalLoginCache()
        {
            EnsureOffline();
            LoginCacheTestCommon(CrudMode.Create);
            LoginCacheTestCommon(CrudMode.Delete);
        }

        /// <summary>
        /// バケットキャッシュ削除（バケットキャッシュなし）
        /// 削除できないこと
        /// </summary>
        [Test]
        public void TestDatabaseDeleteNormalNoBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Delete, false);
        }

        /// <summary>
        /// バケットキャッシュ削除（バケットキャッシュあり）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDatabaseDeleteNormalBucketCache()
        {
            EnsureOffline();
            BucketCacheTestCommon(CrudMode.Create);
            BucketCacheTestCommon(CrudMode.Delete);
        }


        /**
         * SQL文の発行
         **/

        /// <summary>
        /// オブジェクトテーブルの作成
        /// テーブルが作成できること
        /// </summary>
        [Test]
        public void TestExecSqlNormalCreateObjectTable()
        {
            EnsureOffline();
            CreateDeleteObjectTable();
        }

        /// <summary>
        /// オブジェクトテービルの削除
        /// テーブルが削除できること
        /// </summary>
        [Test]
        public void TestExecSqlNormalDeleteObjectTable()
        {
            EnsureOffline();
            CreateDeleteObjectTable(false);
        }


        /**
         * トランザクションの設定
         **/

        /// <summary>
        /// データ追加中の取得
        /// 追加後のデータを取得できること
        /// </summary>
        [Test]
        public void TestBeginTransactionNormalInsartGet()
        {
            EnsureOffline();
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            var results = bucket.QueryAsync(new NbQuery()).Result;
            Assert.AreEqual(0, results.Count());
            var obj = bucket.NewObject();
            obj["key"] = "value";
            obj.SaveAsync();
            results = bucket.QueryAsync(new NbQuery()).Result;
            Assert.AreEqual(1, results.Count());
        }

        /// <summary>
        /// データ削除中の取得
        /// 削除後のデータを取得できること
        /// </summary>
        [Test]
        public void TestBeginTransactionNormalDeleteGet()
        {
            EnsureOffline();
            CreateObjectCache(2);
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            var results = bucket.QueryAsync(new NbQuery()).Result;
            Assert.AreEqual(2, results.Count());
            bucket.DeleteAsync(new NbQuery());
            results = bucket.QueryAsync(new NbQuery()).Result;
            Assert.AreEqual(0, results.Count());
        }


        /**
         * ローカルデータの一括削除
         **/

        /// <summary>
        /// 一括削除（ログイン済み、オブジェクトキャッシュあり、バケットキャッシュあり）
        /// 一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalObjectBucketCache()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache();
            CreateDatabaseData();
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（未ログイン、オブジェクトキャッシュあり、バケットキャッシュなし）
        /// 一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalObjectCache()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache(false);
            CreateDatabaseData(true, false);
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（ログイン済み、オブジェクトキャッシュなし、バケットキャッシュあり）
        /// 一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalBucketCache()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache();
            CreateDatabaseData(false);
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（未ログイン、オブジェクトキャッシュなし、バケットキャッシュなし）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalNoCache()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache(false);
            CreateDatabaseData(false, false);
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（未ログイン、オブジェクトキャッシュあり、バケットキャッシュあり）
        /// オブジェクト作成後の一括削除
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalCreate()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache(false);
            CreateDatabaseData();
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（未ログイン、オブジェクトキャッシュあり、バケットキャッシュあり）
        /// オブジェクト取得後の一括削除
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalRead()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache(false);
            CreateDatabaseData();
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            var results = bucket.QueryAsync(new NbQuery()).Result;
            foreach (var result in results)
            {
                var obj = bucket.GetAsync(result.Id).Result;
            }
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（未ログイン、オブジェクトキャッシュあり、バケットキャッシュあり）
        /// オブジェクト更新後の一括削除
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalUpdate()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache(false);
            CreateDatabaseData();
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            var results = bucket.QueryAsync(new NbQuery()).Result;
            foreach (var result in results)
            {
                var obj = bucket.GetAsync(result.Id).Result;
                obj.SyncState = NbSyncState.Sync;
                obj.SaveAsync().Wait();
            }
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（未ログイン、オブジェクトキャッシュあり、バケットキャッシュあり）
        /// オブジェクト削除後の一括削除
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllNormalDelete()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache(false);
            CreateDatabaseData();
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            var results = bucket.QueryAsync(new NbQuery()).Result;
            foreach (var result in results)
            {
                var obj = bucket.GetAsync(result.Id).Result;
                obj.DeleteAsync().Wait();
            }
            NbOfflineService.DeleteCacheAll().Wait();
            AssertDatabaseeData();
        }

        /// <summary>
        /// 一括削除（未ログイン、オブジェクトキャッシュあり、バケットキャッシュあり、オフライン機能無効）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllExceptionDisableOffline()
        {
            EnsureOffline();
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            CreateLoginCache(false);
            CreateDatabaseData();
            NbService.Singleton.DisableOffline();
            try
            {
                NbOfflineService.DeleteCacheAll().Wait();
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
                AssertDatabaseeData(false);
            }
        }


        /**
         * オブジェクトの検索
         **/

        /// <summary>
        /// オブジェクトの検索（equals）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereEqual()
        {
            var query = new NbQuery().EqualTo("data1", 120);
            queryCommon(query, createTestDataB(), new int[] { 5 });
        }

        /// <summary>
        /// オブジェクトの検索（equals object array）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereEqualObjectArray()
        {
            var entry = new object[] { 145, 101, 102 };
            var query = new NbQuery().EqualTo("data1", entry);
            queryCommon(query, createTestDataB(), new int[] { 10 });
        }

        /// <summary>
        /// オブジェクトの検索（equals NbJsonArray）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereEqualArray()
        {
            var entry = new NbJsonArray() { 100, 101, 102 };
            var query = new NbQuery().EqualTo("data1", entry);
            queryCommon(query, createTestDataB(), new int[] { 1 });
        }

        /// <summary>
        /// オブジェクトの検索（equals NbJsonArray NbJsonObject）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereEqualNbJsonArray()
        {
            var json = new NbJsonObject() { { "key", "value" } };
            var entry = new NbJsonArray() { json };
            var query = new NbQuery().EqualTo("data1", entry);
            queryCommon(query, createTestDataD(), new int[] { 25 });
        }


        /// <summary>
        /// オブジェクトの検索（equals NbJsonObject）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereEqualNbJsonObject()
        {
            var entry = new NbJsonObject() { { "key", "value" } };
            var query = new NbQuery().EqualTo("data1", entry);
            queryCommon(query, createTestDataD(), new int[] { 18 });
        }

        /// <summary>
        /// オブジェクトの検索（$ne）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNotEqual()
        {
            var query = new NbQuery().NotEquals("data1", 120);
            queryCommon(query, createTestDataB(), new int[] { 1, 2, 3, 4, 6, 7, 8, 9, 10, 11 });
        }

        /// <summary>
        /// オブジェクトの検索（$ne array）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNotEqualArray()
        {
            var entry = new NbJsonArray() { 100, 101, 102 };
            var query = new NbQuery().NotEquals("data1", entry);
            queryCommon(query, createTestDataB(), new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });
        }

        /// <summary>
        /// オブジェクトの検索（$ne null指定）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNotEqualNull()
        {
            var query = new NbQuery().NotEquals("data1", null);
            queryCommon(query, createTestDataD(), new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13,
                                                              14, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25 });
        }

        /// <summary>
        /// オブジェクトの検索（$ne キー違い）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNotEqualNoField()
        {
            var query = new NbQuery().NotEquals("data2", 100);
            queryCommon(query, createTestDataD(), new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13,
                                                              14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 });
        }

        /// <summary>
        /// オブジェクトの検索（$lt）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereLessThan()
        {
            var query = new NbQuery().LessThan("data1", 120);
            queryCommon(query, createTestDataB(), new int[] { 2, 3, 4 });
        }

        /// <summary>
        /// オブジェクトの検索（$lte）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereLessThanOrEqual()
        {
            var query = new NbQuery().LessThanOrEqual("data1", 120);
            queryCommon(query, createTestDataB(), new int[] { 2, 3, 4, 5 });
        }

        /// <summary>
        /// オブジェクトの検索（$gt）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereGreaterThan()
        {
            var query = new NbQuery().GreaterThan("data1", 120);
            queryCommon(query, createTestDataB(), new int[] { 6, 7, 8, 9 });
        }

        /// <summary>
        /// オブジェクトの検索（$gte）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereGreaterThanOrEqual()
        {
            var query = new NbQuery().GreaterThanOrEqual("data1", 120);
            queryCommon(query, createTestDataB(), new int[] { 5, 6, 7, 8, 9 });
        }

        /// <summary>
        /// オブジェクトの検索（$in）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereIn()
        {
            var query = new NbQuery().In("data1", 120, 130, 135, 150);
            queryCommon(query, createTestDataB(), new int[] { 5, 7, 8 });
        }

        /// <summary>
        /// オブジェクトの検索（$in null指定）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereInNull()
        {
            var query = new NbQuery().In("data1", null, 200);
            queryCommon(query, createTestDataD(), new int[] { 19, 24 });
        }

        /// <summary>
        /// オブジェクトの検索（$nin）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNotIn()
        {
            var query = new NbQuery().In("data1", 120, 130, 135, 150).Not("data1");
            queryCommon(query, createTestDataB(), new int[] { 1, 2, 3, 4, 6, 9, 10, 11 });
        }

        /// <summary>
        /// オブジェクトの検索（$all）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereAll()
        {
            var query = new NbQuery().All("data1", 101, 102);
            queryCommon(query, createTestDataB(), new int[] { 1, 10 });
        }

        /// <summary>
        /// オブジェクトの検索（$all null指定）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereAllNull()
        {
            var query = new NbQuery().All("data1", null, 102);
            queryCommon(query, createTestDataD(), new int[] { 24 });
        }

        /// <summary>
        /// オブジェクトの検索（$exists false）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereExistsFalse()
        {
            var query = new NbQuery().NotExists("data1");
            queryCommon(query, createTestDataB(), new int[] { 11 });
        }

        /// <summary>
        /// オブジェクトの検索（$exists true）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereExistsTrue()
        {
            var query = new NbQuery().Exists("data1");
            queryCommon(query, createTestDataB(), new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        /// <summary>
        /// オブジェクトの検索（$not）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNot()
        {
            var query = new NbQuery().Exists("data1").Not("data1");
            queryCommon(query, createTestDataB(), new int[] { 11 });
        }

        /// <summary>
        /// オブジェクトの検索（$or）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereOr()
        {
            var query = NbQuery.Or(new NbQuery().In("data1", 120, 130, 135, 150),
                new NbQuery().All("data1", 101, 102));
            queryCommon(query, createTestDataB(), new int[] { 1, 5, 7, 8, 10 });
        }

        /// <summary>
        /// オブジェクトの検索（$nor）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNotOr()
        {
            var inQuery = new NbQuery().In("data1", 120, 130, 135, 150);
            var allQuery = new NbQuery().All("data1", 101, 102);
            var conditions = new NbJsonArray() { inQuery.Conditions, allQuery.Conditions };
            var query = new NbQuery();
            query.Conditions = new NbJsonObject() { { "$nor", conditions } };
            queryCommon(query, createTestDataB(), new int[] { 2, 3, 4, 6, 9, 11 });
        }

        /// <summary>
        /// オブジェクトの検索（$and）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereAnd()
        {
            var query = NbQuery.And(
                new NbQuery().LessThanOrEqual("data1", 130),
                new NbQuery().GreaterThanOrEqual("data1", 120),
                new NbQuery().In("data1", 120, 130, 135, 150));
            queryCommon(query, createTestDataB(), new int[] { 5, 7 });
        }

        /// <summary>
        /// オブジェクトの検索（$regex IgnoreCase）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereRegexIgnoreCase()
        {
            var query = new NbQuery().Regex("data1", ".*abc.*", "i");
            queryCommon(query, createTestDataC(), new int[] { 1, 2, 4, 5 });
        }

        /// <summary>
        /// オブジェクトの検索（$regex Multline）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereRegexMultiline()
        {
            var query = new NbQuery().Regex("data1", "^_.*", "m");
            queryCommon(query, createTestDataC(), new int[] { 3, 6, 7 });
        }

        /// <summary>
        /// オブジェクトの検索（$regex Singleline）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereRegexSingleline()
        {
            var query = new NbQuery().Regex("data1", ".*5._", "s");
            queryCommon(query, createTestDataC(), new int[] { 6, 7 });
        }

        /// <summary>
        /// オブジェクトの検索（$regex IgnorePatternWhitespace）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereRegexIgnorePatternWhitespace()
        {
            var query = new NbQuery().Regex("data1", ".*5._  # comment", "x");
            queryCommon(query, createTestDataC(), new int[] { 7 });
        }

        /// <summary>
        /// オブジェクトの検索（$regex no option）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereRegexNoOption()
        {
            var query = new NbQuery().Regex("data1", ".*abc.*", null);
            queryCommon(query, createTestDataC(), new int[] { 1, 5 });
        }

        /// <summary>
        /// オブジェクトの検索（組み合わせ、$and, $or）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereAndOr()
        {
            var query = NbQuery.And(
                NbQuery.Or(
                    new NbQuery().In("data1", 120, 130, 135, 150),
                    new NbQuery().All("data1", 101, 102)
                ),
                new NbQuery().GreaterThanOrEqual("data1", 130)
            );
            queryCommon(query, createTestDataD(), new int[] { 7, 8 });
        }

        /// <summary>
        /// オブジェクトの検索（組み合わせ、$not, $lt）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereNotLt()
        {
            var query = new NbQuery().LessThan("data1", 130).Not("data1");
            queryCommon(query, createTestDataD(), new int[] { 1, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 });
        }

        /// <summary>
        /// オブジェクトの検索（入れ子構造　equals）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalNestEquals()
        {
            var query = new NbQuery().EqualTo("data1.dataA", 100);
            queryCommon(query, createTestDataE(), new int[] { 1 });
        }

        /// <summary>
        /// オブジェクトの検索（入れ子構造　$in）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalNestIn()
        {
            var query = new NbQuery().In("data1.dataA", "aaa");
            queryCommon(query, createTestDataE(), new int[] { 2 });
        }

        /// <summary>
        /// オブジェクトの検索（クエリ不正）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestQueryAsyncNormalWhereWrongValue()
        {
            var query = new NbQuery();
            query.Conditions = new NbJsonObject { { "integer", new NbJsonObject { { "$aaa", 12346 } } } };
            queryCommon(query, createTestDataB(), new int[] { });
        }


        /**
         ** Util for LocalDatabaseIT
         **/

        private string GetDbPath()
        {
            var service = NbService.GetInstance();
            var idpass = service.TenantId + "/" + service.AppId;
            var documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fulldbpath = "NEC/Nebula/" + idpass + "/offline.db";
            fulldbpath = Path.Combine(documentFolder, fulldbpath);
            return fulldbpath;
        }

        private void EnsureOffline(string password = null)
        {
            if (!NbService.Singleton.IsOfflineEnabled())
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton, password);
            }
        }

        private void DatabaseAccessCommon()
        {
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            db.ExecSql("CREATE TABLE IF NOT EXISTS Test (_id INTEGER PRIMARY KEY AUTOINCREMENT, text TEXT)");
            var value = new Dictionary<string, object> { { "text", "value" } };
            db.Insert("Test", value);
            db.Delete("Test", null, null);
            db.ExecSql("DROP TABLE IF EXISTS Test");
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

        private void AssertAutoVacuum()
        {
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            var vacuum = db.RawQuery("PRAGMA auto_vacuum", null);
            vacuum.Read();
            Assert.AreEqual(1, vacuum.GetInt32(0));
            vacuum.Dispose();
        }

        private void CreateDeleteObjectTable(bool isCreate = true)
        {
            var objectCache = new NbObjectCache(NbService.Singleton);
            if (isCreate)
            {
                objectCache.CreateCacheTable(ObjectBucketName);
                Assert.IsTrue(objectCache.GetTables().Contains("OBJECT_" + ObjectBucketName));
            }
            else
            {
                objectCache.DeleteCacheTable(ObjectBucketName);
                Assert.IsFalse(objectCache.GetTables().Contains("OBJECT_" + ObjectBucketName));
            }
        }

        private enum CrudMode
        {
            Create,
            Read,
            Update,
            Delete
        }

        private void ObjectCacheTestCommon(CrudMode mode, bool expected = true)
        {
            var offlinebucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            var objectCache = new NbObjectCache(NbService.Singleton);

            switch (mode)
            {
                case CrudMode.Create:
                    var obj = offlinebucket.NewObject();
                    Assert.IsNotNull(obj);
                    var result = obj.SaveAsync().Result;
                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.Id);
                    break;
                case CrudMode.Read:
                    var results = offlinebucket.QueryAsync(new NbQuery()).Result;
                    if (expected)
                        Assert.IsNotEmpty(results);
                    else
                        Assert.IsEmpty(results);
                    break;
                case CrudMode.Update:
                    results = offlinebucket.QueryAsync(new NbQuery()).Result;
                    if (expected)
                    {
                        var updataObj = results.ToList()[0];
                        updataObj.Etag = "dummy";
                        result = updataObj.SaveAsync().Result;
                        Assert.AreEqual(result.Etag, "dummy");
                    }
                    else
                    {
                        // オブジェクトなし状態でのアップデート確認ができないので、直接DBのAPIで確認する
                        try
                        {
                            objectCache.UpdateObject(offlinebucket.NewObject(), NbSyncState.Dirty);
                            Assert.Fail("No Exception!");
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                    break;
                case CrudMode.Delete:
                    results = offlinebucket.QueryAsync(new NbQuery()).Result;
                    if (expected)
                    {
                        var deleteObj = results.ToList()[0];
                        deleteObj.Etag = "dummy";
                        deleteObj.DeleteAsync(false).Wait();
                        Assert.IsEmpty(offlinebucket.QueryAsync(new NbQuery()).Result);
                    }
                    else
                    {
                        // オブジェクトなし状態でのdelete確認ができないので、直接DBのAPIで確認する
                        // 該当オブジェクトなしで正常終了
                        try
                        {
                            objectCache.DeleteObject(offlinebucket.NewObject());
                            Assert.Fail("No Exception!");
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void LoginCacheTestCommon(CrudMode mode, bool expected = true)
        {
            switch (mode)
            {
                case CrudMode.Create:
                    if (expected)
                        ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
                    Assert.IsNotNull(CreateLoginCache());
                    break;
                case CrudMode.Read:
                    if (expected)
                    {
                        NbOfflineUser.LoginWithUsernameAsync(ITUtil.Username, ITUtil.Password, NbUser.LoginMode.Offline).Wait();
                    }
                    else
                    {
                        try
                        {
                            NbOfflineUser.LoginWithUsernameAsync(ITUtil.Username, ITUtil.Password, NbUser.LoginMode.Offline).Wait();
                            Assert.Fail("No Exception!");
                        }
                        catch (AggregateException e)
                        {
                            var ex = e.InnerException as NbHttpException;
                            Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                        }
                    }
                    break;
                case CrudMode.Update:
                    if (expected)
                    {
                        // 別ユーザのログインで更新確認
                        ITUtil.SignUpUser().Wait();
                        var cache = CreateLoginCache(false, "foo");
                        Assert.IsNotNull(cache);
                        Assert.AreEqual(cache.UserName, "foo");
                        Assert.AreEqual(cache.Email, "foo@example.com");
                    }
                    else
                    {
                        // 更新対象のキャッシュなし
                        Assert.IsNull(GetLoginCache());
                    }
                    break;
                case CrudMode.Delete:
                    if (expected)
                    {
                        // 有効期限切れによるキャッシュ削除で確認
                        var cache = CreateLoginCache();
                        var session = NbService.Singleton.SessionInfo;
                        ChangeLoginCacheExpire(cache, session);
                        try
                        {
                            NbOfflineUser.LoginWithUsernameAsync(ITUtil.Username, ITUtil.Password, NbUser.LoginMode.Offline).Wait();
                            Assert.Fail("No Exception!");
                        }
                        catch (AggregateException e)
                        {
                            var ex = e.InnerException as NbHttpException;
                            Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                            Assert.IsNull(GetLoginCache());
                        }
                    }
                    else
                    {
                        // 削除対象のキャッシュなし
                        Assert.IsNull(GetLoginCache());
                    }
                    break;
                default:
                    break;
            }
        }

        private void ChangeLoginCacheExpire(LoginCache cache, NbSessionInfo session)
        {
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                dao.RemoveAll();
                var login = new LoginCache();
                login.SetUser(session.CurrentUser);
                session.Set(session.SessionToken, 1L, session.CurrentUser);
                login.SetSession(session);
                login.Password = ITUtil.Password;

                dao.Add(login);
                dao.SaveChanges();
            }
        }

        private LoginCache CreateLoginCache(bool isLoggedin = true, string username = ITUtil.Username)
        {
            NbOfflineUser.LoginWithUsernameAsync(username, ITUtil.Password).Wait();
            if (!isLoggedin)
                NbOfflineUser.LogoutAsync().Wait();

            var logincache = GetLoginCache(username);
            return logincache;
        }

        private LoginCache GetLoginCache(string username = ITUtil.Username)
        {
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                return dao.FindByUsername(username);
            }
        }

        private void BucketCacheTestCommon(CrudMode mode, bool expected = true)
        {
            switch (mode)
            {
                case CrudMode.Create:
                    if (expected)
                        CreateBucketCache();
                    else
                        CreateBucketCache("second");
                    Assert.IsNotEmpty(GetBucketCaches());
                    break;
                case CrudMode.Read:
                    if (expected)
                    {
                        Assert.IsNotEmpty(GetBucketCaches());
                    }
                    else
                    {
                        Assert.IsEmpty(GetBucketCaches());
                    }
                    break;
                case CrudMode.Update:
                    if (expected)
                    {
                        var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
                        using (var dbContext = db.CreateDbContext())
                        {
                            var dao = new ObjectBucketCacheDao(dbContext);
                            dao.SaveLastPullServerTime("test0", "2016-11-11T00:00:00.000Z");
                            dao.SaveChanges();
                            Assert.AreEqual(dao.GetLastPullServerTime("test0"), "2016-11-11T00:00:00.000Z");
                        }
                    }
                    else
                    {
                        // 更新対象のキャッシュなし
                        Assert.IsEmpty(GetBucketCaches());
                    }
                    break;
                case CrudMode.Delete:
                    if (expected)
                    {
                        var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
                        using (var dbContext = db.CreateDbContext())
                        {
                            var dao = new ObjectBucketCacheDao(dbContext);
                            var results = dao.FindAll();
                            dao.Remove(results.ToList()[0]);
                            Assert.AreEqual(1, dao.SaveChanges());
                        }
                    }
                    else
                    {
                        // 削除対象のキャッシュなし
                        Assert.IsEmpty(GetBucketCaches());
                    }
                    break;
                default:
                    break;
            }
        }

        private List<ObjectBucketCache> GetBucketCaches()
        {
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                return dao.FindAll().ToList();
            }
        }

        private void CreateDatabaseData(bool isObject = true, bool isBucket = true)
        {
            if (isObject)
                CreateObjectCache(3);
            if (isBucket)
                CreateBucketCache("test", 3);
        }

        private void AssertDatabaseeData(bool expected = true)
        {
            EnsureOffline();
            if (expected)
                Assert.IsFalse(IsExistsCache());
            else
                Assert.IsTrue(IsExistsCache());
        }

        private void CreateObjectCache(int cnt = 1)
        {
            var offlinebucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            for (int i = 0; i < cnt; i++)
            {
                var obj = offlinebucket.NewObject();
                obj["key"] = "value" + i;
                var result = obj.SaveAsync().Result;
            }
        }

        private bool IsExistsCache(bool isObject = true, bool isBucket = true)
        {
            bool ret = false;
            if (isObject)
            {
                var objectCache = new NbObjectCache(NbService.Singleton);
                ret = objectCache.IsObjectCacheExists();
            }
            if (isBucket)
            {
                var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
                using (var dbContext = db.CreateDbContext())
                {
                    var dao = new ObjectBucketCacheDao(dbContext);
                    var result = dao.FindAll();
                    if (isObject)
                        ret &= result.Count() != 0;
                    else
                        ret = result.Count() != 0;
                }
            }
            return ret;
        }

        private void CreateBucketCache(string bucketName = "test", int cnt = 1)
        {
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new ObjectBucketCacheDao(dbContext);
                for (int i = 0; i < cnt; i++)
                {
                    using (var transaction = db.BeginTransaction())
                    {
                        var bucket = new ObjectBucketCache();
                        bucket.Name = bucketName + i;
                        bucket.LastPullServerTime = "2015-01-01T00:00:00.00" + i + "Z";
                        dao.Add(bucket);
                        Assert.AreEqual(1, dao.SaveChanges());

                        transaction.Commit();
                    }
                }
            }
        }

        private List<Dictionary<string, object>> createTestDataB()
        {
            var results = new List<Dictionary<string, object>>();
            Dictionary<string, object> data;

            // 1
            data = new Dictionary<string, object>();
            data.Add("data1", new int[] { 100, 101, 102 });
            results.Add(data);

            // 2
            data = new Dictionary<string, object>();
            data.Add("data1", 105);
            results.Add(data);

            // 3
            data = new Dictionary<string, object>();
            data.Add("data1", 110);
            results.Add(data);

            // 4
            data = new Dictionary<string, object>();
            data.Add("data1", 115);
            results.Add(data);

            // 5
            data = new Dictionary<string, object>();
            data.Add("data1", 120);
            results.Add(data);

            // 6
            data = new Dictionary<string, object>();
            data.Add("data1", 125);
            results.Add(data);

            // 7
            data = new Dictionary<string, object>();
            data.Add("data1", 130);
            results.Add(data);

            // 8
            data = new Dictionary<string, object>();
            data.Add("data1", 135);
            results.Add(data);

            // 9
            data = new Dictionary<string, object>();
            data.Add("data1", 140);
            results.Add(data);

            // 10
            data = new Dictionary<string, object>();
            data.Add("data1", new int[] { 145, 101, 102 });
            results.Add(data);

            // 11
            data = new Dictionary<string, object>();
            data.Add("data2", 100);
            results.Add(data);

            return results;
        }

        private List<Dictionary<string, object>> createTestDataC()
        {
            var results = new List<Dictionary<string, object>>();
            Dictionary<string, object> data;

            // 1
            data = new Dictionary<string, object>();
            data.Add("data1", "abcdefg");
            results.Add(data);

            // 2
            data = new Dictionary<string, object>();
            data.Add("data1", "ABCDEFG");
            results.Add(data);

            // 3
            data = new Dictionary<string, object>();
            data.Add("data1", "_012345_");
            results.Add(data);

            // 4
            data = new Dictionary<string, object>();
            data.Add("data1", "0123ABC");
            results.Add(data);

            // 5
            data = new Dictionary<string, object>();
            data.Add("data1", "0123abc");
            results.Add(data);

            // 6
            data = new Dictionary<string, object>();
            data.Add("data1", "012345\n_");
            results.Add(data);

            // 7
            data = new Dictionary<string, object>();
            data.Add("data1", "_0123456_");
            results.Add(data);

            return results;
        }

        private List<Dictionary<string, object>> createTestDataD()
        {
            var results = createTestDataB();
            Dictionary<string, object> data;

            // 12
            data = new Dictionary<string, object>();
            data.Add("data1", 101);
            results.Add(data);

            // 13
            data = new Dictionary<string, object>();
            data.Add("data1", true);
            results.Add(data);

            // 14
            data = new Dictionary<string, object>();
            data.Add("data1", false);
            results.Add(data);

            // 15
            data = new Dictionary<string, object>();
            data.Add("data1", "aaa");
            results.Add(data);

            // 16
            data = new Dictionary<string, object>();
            data.Add("data1", "101");
            results.Add(data);

            // 17
            data = new Dictionary<string, object>();
            data.Add("data1", "true");
            results.Add(data);

            // 18
            data = new Dictionary<string, object>();
            var json = new NbJsonObject();
            json["key"] = "value";
            data.Add("data1", json);
            results.Add(data);

            // 19
            data = new Dictionary<string, object>();
            data.Add("data1", null);
            results.Add(data);

            // 20
            data = new Dictionary<string, object>();
            data.Add("data1", "null");
            results.Add(data);

            // 21
            data = new Dictionary<string, object>();
            data.Add("data1", "120");
            results.Add(data);

            // 22
            data = new Dictionary<string, object>();
            data.Add("data1", "99");
            results.Add(data);

            // 23
            data = new Dictionary<string, object>();
            data.Add("data1", "1000");
            results.Add(data);

            // 24
            data = new Dictionary<string, object>();
            data.Add("data1", new object[] { null, 101, 102 });
            results.Add(data);

            // 25
            data = new Dictionary<string, object>();
            data.Add("data1", new Object[] { json });
            results.Add(data);

            return results;
        }

        private List<Dictionary<string, object>> createTestDataE()
        {
            var results = new List<Dictionary<string, object>>();
            Dictionary<string, object> data;

            // 1
            data = new Dictionary<string, object>();
            var json = new NbJsonObject();
            json["dataA"] = 100;
            data.Add("data1", json);
            results.Add(data);

            // 2
            data = new Dictionary<string, object>();
            json = new NbJsonObject();
            json["dataA"] = "aaa";
            data.Add("data1", json);
            results.Add(data);

            return results;
        }

        private void queryCommon(NbQuery query, List<Dictionary<string, object>> testData, int[] expectedDataIndexes)
        {
            EnsureOffline();
            var expectedData = createObjects(testData);

            // main
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            var results = bucket.QueryAsync(query).Result;

            // assert
            Assert.AreEqual(expectedDataIndexes.Count(), results.Count());
            foreach (var index in expectedDataIndexes)
            {
                var expect = expectedData[index - 1];
                var existed = false;
                foreach (var result in results)
                {
                    if (result.Id.Equals(expect.Id))
                    {
                        existed = true;
                        Assert.AreEqual(result.BucketName, expect.BucketName);
                    }
                }
                Assert.IsTrue(existed);
            }
        }

        private List<NbObject> createObjects(List<Dictionary<string, object>> testData)
        {
            var results = new List<NbObject>();
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName);
            foreach (var data in testData)
            {
                foreach (var pair in data)
                {
                    var obj = bucket.NewObject();
                    obj[pair.Key] = pair.Value;
                    results.Add(obj.SaveAsync().Result);
                }
            }
            return results;
        }
    }
}
