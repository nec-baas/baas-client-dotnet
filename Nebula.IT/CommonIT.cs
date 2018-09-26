using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class CommonIT
    {
        private const string Username = "DotnetITUser2";
        private const string Email = "DotnetITUser2@example.com";

        private static bool _isServiceEnabled = true;

        [SetUp]
        public void SetUp()
        {
            // NbServiceの確認を行うため、Disposeしておく
            DisposeService();
        }

        [TearDown]
        public void TearDown()
        {
            NbService.EnableMultiTenant(false);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            // 他機能のITでInitNebula()を呼べば、値は設定されるので、
            // ここではDisponseしたインスタンスを再生成するのみ
            if (!_isServiceEnabled)
            {
                GetServiceInstance();
            }
        }

        #region ■設定・初期化
        /// <summary>
        /// テナントID - 設定
        /// テナントIDが正しく設定されていること
        /// </summary>
        [Test]
        public void TestTenantIdNormal()
        {
            var service = GetServiceInstance();
            service.TenantId = "dummyTenantId";

            Assert.AreEqual("dummyTenantId", service.TenantId);
        }

        /// <summary>
        /// テナントID - 初期値
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestTenantIdNormalInit()
        {
            var service = GetServiceInstance();

            Assert.IsNull(service.TenantId);
        }

        /// <summary>
        /// アプリケーションID - 設定
        /// アプリケーションIDが正しく設定されていること
        /// </summary>
        [Test]
        public void TestAppIdNormal()
        {
            var service = GetServiceInstance();
            service.AppId = "dummyAppId";

            Assert.AreEqual("dummyAppId", service.AppId);
        }

        /// <summary>
        /// アプリケーションID - 初期値
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestAppIdNormalInit()
        {
            var service = GetServiceInstance();

            Assert.IsNull(service.AppId);
        }

        /// <summary>
        /// アプリケーションキー - 設定
        /// アプリケーションキーが正しく設定されていること
        /// </summary>
        [Test]
        public void TestAppKeyNormal()
        {
            var service = GetServiceInstance();
            service.AppKey = "dummyAppKey";

            Assert.AreEqual("dummyAppKey", service.AppKey);
        }

        /// <summary>
        /// アプリケーションキー - 初期値
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestAppKeyNormalInit()
        {
            var service = GetServiceInstance();

            Assert.IsNull(service.AppKey);
        }

        /// <summary>
        /// REST APIエンドポイント URL - 設定：末尾が"/"の場合
        /// エンドポイントURIが正しく設定されていること
        /// </summary>
        [Test]
        public void TestEndPointUriNormalEndsWithSlash()
        {
            var service = GetServiceInstance();
            service.EndpointUrl = "http://api.example.com/";

            Assert.AreEqual("http://api.example.com/", service.EndpointUrl);
        }

        /// <summary>
        /// REST APIエンドポイント URL - 設定：末尾が"/"以外の場合
        /// "/"が自動補完されること
        /// </summary>
        [Test]
        public void TestEndPointUriNormalEndsWithNotSlash()
        {
            var service = GetServiceInstance();
            service.EndpointUrl = "http://api.example.com";

            Assert.AreEqual("http://api.example.com/", service.EndpointUrl);
        }

        /// <summary>
        /// REST APIエンドポイント URL - 設定：nullの場合
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestEndPointUriExceptionSetNull()
        {
            var service = GetServiceInstance();
            service.EndpointUrl = null;
        }

        /// <summary>
        /// REST APIエンドポイント URL - 初期値
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestEndPointUriNormalInit()
        {
            var service = GetServiceInstance();

            Assert.IsNull(service.EndpointUrl);
        }

        /// <summary>
        /// マルチテナント機能 - 有効
        /// 異なるサービスインスタンスが生成されること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalEnabled()
        {
            NbService.EnableMultiTenant(true);

            var service1 = GetServiceInstance();
            var service2 = GetServiceInstance();

            Assert.AreNotEqual(service1, service2);
        }

        /// <summary>
        /// マルチテナント機能 - 無効
        /// 同じサービスインスタンスが生成されること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalDisabled()
        {
            NbService.EnableMultiTenant(false);

            var service1 = GetServiceInstance();
            var service2 = GetServiceInstance();

            Assert.AreEqual(service1, service2);
        }

        /// <summary>
        /// マルチテナント機能 - 有効時にサービスを省略した場合
        /// オブジェクト生成時、InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestOmitServiceExceptionMultiTenantEnabled()
        {
            NbService.EnableMultiTenant(true);

            var obj = new NbObject(ITUtil.ObjectBucketName);
        }

        /// <summary>
        /// マルチテナント機能 - 無効時にサービスを省略した場合
        /// 最後に生成したサービスが使用されること
        /// </summary>
        [Test]
        public async void TestOmitServiceNormalMultiTenantDisabled()
        {
            NbService.EnableMultiTenant(false);

            var service1 = GetServiceInstance();
            service1.TenantId = "dummy";
            service1.AppId = "dummy";
            service1.AppKey = "dummy";
            service1.EndpointUrl = "dummy";

            var service2 = GetServiceInstance();
            service2.TenantId = ITUtil.TenantId;
            service2.AppId = ITUtil.AppId;
            service2.AppKey = ITUtil.AppKey;
            service2.EndpointUrl = ITUtil.EndpointUrl;

            // バケット作成＆データクリア
            await ITUtil.InitOnlineObjectStorage();

            // オブジェクト登録が成功すること
            var obj = CreateObjectInstance();
            try
            {
                var result = await obj.SaveAsync();
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オフライン機能 - 有効
        /// オフラインバケットが生成できること
        /// </summary>
        [Test]
        public void TestEnableOfflineServiceNormalEnabled()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(service);

            try
            {
                var bucket = new NbOfflineObjectBucket<NbOfflineObject>("CommonIT", service);
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オフライン機能 - 有効
        /// trueが取得できること
        /// </summary>
        [Test]
        public void TestIsOfflineEnabledNormalEnabled()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(service);

            Assert.True(service.IsOfflineEnabled());
        }

        /// <summary>
        /// オフライン機能 - 無効
        /// オフラインバケット生成時、InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestEnableOfflineServiceExceptionDisabled()
        {
            var service = GetServiceInstance();

            var bucket = new NbOfflineObjectBucket<NbOfflineObject>("CommonIT", service);
        }

        /// <summary>
        /// オフライン機能 - 初期値
        /// falseが取得できること
        /// </summary>
        [Test]
        public void TestOfflineSerivceNormalInit()
        {
            var service = GetServiceInstance();

            Assert.False(service.IsOfflineEnabled());
        }
        #endregion

        #region ■ACL
        /// <summary>
        /// ACL設定 - 初期値
        /// Readable ユーザID/グループ名が空のHashSetであること
        /// Writable ユーザID/グループ名が空のHashSetであること
        /// Updatable ユーザID/グループ名が空のHashSetであること
        /// Creatable ユーザID/グループ名が空のHashSetであること
        /// Deletable ユーザID/グループ名が空のHashSetであること
        /// オーナユーザIDがnullであること
        /// Admin ユーザID/グループ名が空のHashSetであること
        /// </summary>
        [Test]
        public void TestAclNormalInit()
        {
            var acl = new NbAcl();

            Assert.IsEmpty(acl.R);
            Assert.IsEmpty(acl.W);
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.IsNull(acl.Owner);
            Assert.IsEmpty(acl.Admin);
        }

        /// <summary>
        /// ACL設定 - 設定・取得
        /// 設定した値が取得できること
        /// </summary>
        [Test]
        public void TestSetAclNormal()
        {
            var acl = new NbAcl();
            acl.R.Add("r1");
            acl.W.Add("w1");
            acl.U.Add("u1");
            acl.C.Add("c1");
            acl.D.Add("d1");
            acl.Admin.Add("a1");
            acl.Owner = "o1";

            Assert.AreEqual(1, acl.R.Count);
            Assert.True(acl.R.Contains("r1"));
            Assert.AreEqual(1, acl.W.Count);
            Assert.True(acl.W.Contains("w1"));
            Assert.AreEqual(1, acl.U.Count);
            Assert.True(acl.U.Contains("u1"));
            Assert.AreEqual(1, acl.C.Count);
            Assert.True(acl.C.Contains("c1"));
            Assert.AreEqual(1, acl.D.Count);
            Assert.True(acl.D.Contains("d1"));
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.True(acl.Admin.Contains("a1"));
            Assert.AreEqual("o1", acl.Owner);
        }

        /// <summary>
        /// ACL設定 - nullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetAclRExceptionNull()
        {
            var acl = new NbAcl();
            acl.R = null;
        }

        /// <summary>
        /// ACL設定 - nullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetAclWExceptionNull()
        {
            var acl = new NbAcl();
            acl.W = null;
        }

        /// <summary>
        /// ACL設定 - nullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetAclUExceptionNull()
        {
            var acl = new NbAcl();
            acl.U = null;
        }

        /// <summary>
        /// ACL設定 - nullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetAclCExceptionNull()
        {
            var acl = new NbAcl();
            acl.C = null;
        }

        /// <summary>
        /// ACL設定 - nullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetAclDExceptionNull()
        {
            var acl = new NbAcl();
            acl.D = null;
        }

        /// <summary>
        /// ACL設定 - nullを設定
        /// オーナユーザIDがnullであること
        /// </summary>
        [Test]
        public void TestSetOwnerNormalNull()
        {
            var acl = new NbAcl();
            acl.Owner = null;

            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// ACL設定 - nullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetAdminExceptionNull()
        {
            var acl = new NbAcl();
            acl.Admin = null;
        }

        /// <summary>
        /// ACL設定 - JSON Object へ変換
        /// 生成されたJsonObjectの内容が正しいこと
        /// </summary>
        [Test]
        public void TestToJsonNormal()
        {
            var acl = new NbAcl();
            acl.R.Add("r1");
            acl.W.Add("w1");
            acl.U.Add("u1");
            acl.C.Add("c1");
            acl.D.Add("d1");
            acl.Admin.Add("a1");
            acl.Owner = "o1";

            var json = acl.ToJson();

            Assert.True(json.GetArray("r").Contains("r1"));
            Assert.True(json.GetArray("w").Contains("w1"));
            Assert.True(json.GetArray("u").Contains("u1"));
            Assert.True(json.GetArray("c").Contains("c1"));
            Assert.True(json.GetArray("d").Contains("d1"));
            Assert.True(json.GetArray("admin").Contains("a1"));
            Assert.AreEqual("o1", json.Get<string>("owner"));

            // 逆変換も試しておく
            var acl1 = new NbAcl(json);
            ITUtil.CompareAcl(acl, acl1);
        }

        /// <summary>
        /// ACL生成 - ACL生成
        /// Anonymousアクセス(R/W/Admin)可能な ACL を生成すること
        /// </summary>
        [Test]
        public void TestCreateAclForAnonymousNormal()
        {
            var acl = NbAcl.CreateAclForAnonymous();

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("g:anonymous", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("g:anonymous", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("g:anonymous", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// ACL生成 - ACL生成
        /// Authenticatedアクセス(R/W/Admin)可能な ACL を生成すること
        /// </summary>
        [Test]
        public void TestCreateAclForAuthenticatedNormal()
        {
            var acl = NbAcl.CreateAclForAuthenticated();

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("g:authenticated", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("g:authenticated", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("g:authenticated", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// ACL生成 - ACL生成
        /// 特定ユーザのみがアクセス可能な ACL を生成すること
        /// Owner も同時に設定されること
        /// </summary>
        [Test]
        public void TestCreateAclForUserNormal()
        {
            var user = new NbUser();
            user.UserId = "u1";
            var acl = NbAcl.CreateAclForUser(user);

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("u1", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("u1", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("u1", acl.Admin.First());
            Assert.AreEqual("u1", acl.Owner);
        }

        /// <summary>
        /// ACL生成 - ACL生成
        /// R/W/Adminが同一の ACL を生成すること
        /// </summary>
        [Test]
        public void TestCreateAclForWithEntryNormal()
        {
            var acl = NbAcl.CreateAclFor("u1");

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("u1", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("u1", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("u1", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// ACL生成 - ACL生成
        /// R/W/Adminが同一の ACL を生成すること
        /// </summary>
        [Test]
        public void TestCreateAclForWithEntriesNormal()
        {
            var acl = NbAcl.CreateAclFor(new[] { "u1" });

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("u1", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("u1", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("u1", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// ACL生成 - userにnullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForUserExceptionUserNull()
        {
            var acl = NbAcl.CreateAclForUser(null);
        }

        /// <summary>
        /// ACL生成 - UserIdにnullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForUserExceptionUserIdNull()
        {
            var user = new NbUser();
            user.UserId = null;
            var acl = NbAcl.CreateAclForUser(user);
        }

        /// <summary>
        /// ACL生成 - entryにnullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>    
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForWithEntryExceptionEntryNull()
        {
            var acl = NbAcl.CreateAclFor((string)null);
        }

        /// <summary>
        /// ACL生成 - entriesにnullを設定
        /// ArgumentNullExceptionが発行されること
        /// </summary>     
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForWithEntriesExceptionEntriesNull()
        {
            var acl = NbAcl.CreateAclFor((IEnumerable<string>)null);
        }

        /// <summary>
        /// オブジェクト作成 - 権限なし - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>     
        [Test]
        public async void TestCreateObjectNormalNoACL()
        {
            await CreateObjectTest(new NbAcl());
        }

        /// <summary>
        /// オブジェクト作成 - r,w,c,u,dにグループ名を設定 - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>     
        [Test]
        public async void TestCreateObjectNormalGroup()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            acl.W.Add("g:authenticated");
            acl.C.Add("g:anonymous");
            acl.U.Add("g:authenticated");
            acl.D.Add("g:anonymous");

            await CreateObjectTest(acl);
        }

        /// <summary>
        /// オブジェクト作成 - r,w,c,u,dにユーザIDを設定 - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>   
        [Test]
        public async void TestCreateObjectNormalUserId()
        {
            string userId = await GetLoggedInUserId();
            // ログアウトする
            await ITUtil.Logout();

            var acl = new NbAcl();
            acl.R.Add(userId);
            acl.W.Add(userId);
            acl.C.Add(userId);
            acl.U.Add(userId);
            acl.D.Add(userId);

            await CreateObjectTest(acl);
        }

        /// <summary>
        /// オブジェクト作成 - r,w,c,u,dに複数設定 - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>   
        [Test]
        public async void TestCreateObjectNormalMultiple()
        {
            string userId = await GetLoggedInUserId();
            // ログアウトする
            await ITUtil.Logout();

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            acl.R.Add("g:test");
            acl.W.Add("g:authenticated");
            acl.W.Add(userId);
            acl.C.Add(userId);
            acl.C.Add("g:anonymous");
            acl.U.Add("g:authenticated");
            acl.U.Add("g:test");
            acl.D.Add("g:anonymous");
            acl.D.Add(userId);

            await CreateObjectTest(acl);
        }

        /// <summary>
        /// オブジェクト作成 - ownerにユーザIDを設定 - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>   
        [Test]
        public async void TestCreateObjectNormalOwner()
        {
            string userId = await GetLoggedInUserId();
            // ログアウトする
            await ITUtil.Logout();

            var acl = new NbAcl();
            acl.Owner = userId;

            await CreateObjectTest(acl);
        }

        /// <summary>
        /// オブジェクト作成 - adminにグループ名を設定 - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>   
        [Test]
        public async void TestCreateObjectNormalAdminGroup()
        {
            var acl = new NbAcl();
            acl.Admin.Add("g:anonymous");

            await CreateObjectTest(acl);
        }

        /// <summary>
        /// オブジェクト作成 - adminにユーザIDを設定 - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>
        [Test]
        public async void TestCreateObjectNormalAdminUserId()
        {
            string userId = await GetLoggedInUserId();
            // ログアウトする
            await ITUtil.Logout();

            var acl = new NbAcl();
            acl.Admin.Add(userId);

            await CreateObjectTest(acl);
        }

        /// <summary>
        /// オブジェクト作成 - adminに複数設定 - 未ログイン
        /// 左記のACL設定でオブジェクトを登録できること
        /// </summary>
        [Test]
        public async void TestCreateObjectNormalAdminMultiple()
        {
            var acl = new NbAcl();
            acl.Admin.Add("g:authenticated");
            acl.Admin.Add("g:test");

            await CreateObjectTest(acl);
        }

        /// <summary>
        /// オブジェクト read権限 - 権限なし
        /// オブジェクトを参照できないこと
        /// </summary>
        [Test]
        public async void TestReadObjectNormalNoACL()
        {
            await ReadObjectTest(new NbAcl(), false);
        }

        /// <summary>
        /// オブジェクト read権限 - 誰でも参照可能 - ログイン済み
        /// オブジェクトを参照できること
        /// </summary>
        [Test]
        public async void TestReadObjectNormalAnonymousLogin()
        {
            string userId = await GetLoggedInUserId();

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");

            await ReadObjectTest(acl, true);
        }

        /// <summary>
        /// オブジェクト read権限 - 誰でも参照可能 - 未ログイン
        /// オブジェクトを参照できること
        /// </summary>
        [Test]
        public async void TestReadObjectNormalAnonymousNoLogin()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");

            await ReadObjectTest(acl, true);
        }

        /// <summary>
        /// オブジェクト update権限 - 権限なし
        /// オブジェクトを更新できないこと
        /// </summary>
        [Test]
        public async void TestUpdateObjectNormalNoACL()
        {
            await UpdateObjectTest(new NbAcl(), false);
        }

        /// <summary>
        /// オブジェクト update権限 - 認証ユーザのみ更新可能 - ログイン済み
        /// オブジェクトを更新できること
        /// </summary>
        [Test]
        public async void TestUpdateObjectNormalAuthenticatedLogin()
        {
            string userId = await GetLoggedInUserId();

            var acl = new NbAcl();
            acl.U.Add("g:authenticated");

            await UpdateObjectTest(acl, true);
        }

        /// <summary>
        /// オブジェクト update権限 - 認証ユーザのみ更新可能 - 未ログイン
        /// オブジェクトを更新できないこと
        /// </summary>
        [Test]
        public async void TestUpdateObjectNormalAuthenticatedNoLogin()
        {
            var acl = new NbAcl();
            acl.U.Add("g:authenticated");

            await UpdateObjectTest(acl, false);
        }

        /// <summary>
        /// オブジェクト delete権限 - 権限なし
        /// オブジェクトを削除できないこと
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalNoACL()
        {
            await DeleteObjectTest(new NbAcl(), false);
        }

        /// <summary>
        /// オブジェクト delete権限 - ユーザのみ削除可能 - ログイン済み
        /// オブジェクトを削除できること
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalUserIDLogin()
        {
            string userId = await GetLoggedInUserId();

            var acl = new NbAcl();
            acl.D.Add(userId);

            await DeleteObjectTest(acl, true);
        }

        /// <summary>
        /// オブジェクト delete権限 - ユーザのみ削除可能 - 未ログイン
        /// オブジェクトを削除できないこと
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalUserIDNoLogin()
        {
            string userId = await GetLoggedInUserId();
            // ログアウトする
            await ITUtil.Logout();

            var acl = new NbAcl();
            acl.D.Add(userId);

            await DeleteObjectTest(acl, false);
        }

        /// <summary>
        /// オブジェクト admin権限 - 権限なし
        /// ACL変更できないこと
        /// </summary>
        [Test]
        public async void TestAdminNormalNoACL()
        {
            var acl = new NbAcl();
            acl.W.Add("g:anonymous");

            await UpdateObjectTestWithAdmin(acl, false);
        }

        /// <summary>
        /// オブジェクト admin権限 - 誰でもACL変更可能 - ログイン済み
        /// ACL変更できること
        /// </summary>
        [Test]
        public async void TestAdminNormalAnonymousLogin()
        {
            string userId = await GetLoggedInUserId();

            var acl = new NbAcl();
            acl.W.Add("g:anonymous");
            acl.Admin.Add("g:anonymous");

            await UpdateObjectTestWithAdmin(acl, true);
        }

        /// <summary>
        /// オブジェクト admin権限 - 誰でもACL変更可能 - 未ログイン
        /// ACL変更できること
        /// </summary>
        [Test]
        public async void TestAdminNormalAnonymousNoLogin()
        {
            var acl = new NbAcl();
            acl.W.Add("g:anonymous");
            acl.Admin.Add("g:anonymous");

            await UpdateObjectTestWithAdmin(acl, true);
        }

        /// <summary>
        /// オブジェクト owner情報 - オブジェクト読み込み(readable)
        /// オーナユーザでログイン時、オブジェクトを参照できること
        /// 他ユーザでログイン時、オブジェクトを参照できないこと
        /// </summary>
        [Test]
        public async void TestOwnerNormalReadable()
        {
            await ReadObjectTestWithOwner();
        }

        /// <summary>
        /// オブジェクト owner情報 - オブジェクト更新(updatable)
        /// オーナユーザでログイン時、オブジェクトを更新できること
        /// 他ユーザでログイン時、オブジェクトを更新できないこと
        /// </summary>
        [Test]
        public async void TestOwnerNormalUpdatable()
        {
            await UpdateObjectTestWithOwner();
        }

        /// <summary>
        /// オブジェクト owner情報 - オブジェクト削除(deletable)
        /// オーナユーザでログイン時、オブジェクトを削除できること
        /// 他ユーザでログイン時、オブジェクトを削除できないこと
        /// </summary>
        [Test]
        public async void TestOwnerNormalDeletable()
        {
            await DeleteObjectTestWithOwner();
        }
        #endregion

        #region ■通信
        /// <summary>
        /// 通信（基本） - URLエンコード（パス）
        /// ファイルをアップロードできること
        /// </summary>
        [Test]
        public async void TestUrlEncodingNormalPath()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();

            // バケット作成＆データクリア
            await ITUtil.InitOnlineFileStorage();

            var bucket = new NbFileBucket(ITUtil.FileBucketName);
            // v6.0よりシングルコーテーション(')は使用不可のため修正
            var fileName = "a0-._~#[]@!$&()+,;= %^{}あア漢.txt";
            try
            {
                await bucket.UploadNewFileAsync(Encoding.UTF8.GetBytes("test"), fileName, "text/plain", NbAcl.CreateAclForAnonymous());
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }

            var meta = await bucket.GetFileMetadataAsync(fileName);
            Assert.AreEqual(fileName, meta.Filename);
        }

        /// <summary>
        /// 通信（基本） - URLエンコード（クエリパラメータ）
        /// オブジェクトを検索して、ヒットすること
        /// </summary>
        [Test]
        public async void TestUrlEncodingNormalQuery()
        {
            await InitObjectTest();

            var obj = new NbObject(ITUtil.ObjectBucketName);
            var value = "a0-._~:/?#[]@!$&'()*+,;= \"%^|{}<>\\あア漢";
            obj["key"] = value;
            try
            {
                var result = await obj.SaveAsync();
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }

            var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);
            var query = new NbQuery().EqualTo("key", value);
            var objects = await bucket.QueryAsync(query);

            Assert.GreaterOrEqual(objects.ToList().Count, 1);
        }

        /// <summary>
        /// 通信（基本） - テナントIDがnull
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestTenantIdExceptionNull()
        {
            var service = GetServiceInstance();
            service.AppId = ITUtil.AppId;
            service.AppKey = ITUtil.AppKey;
            service.EndpointUrl = ITUtil.EndpointUrl;

            var obj = CreateObjectInstance();

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /// <summary>
        /// 通信（基本） - アプリケーションIDがnull
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestAppIdExceptionNull()
        {
            var service = GetServiceInstance();
            service.TenantId = ITUtil.TenantId;
            service.AppKey = ITUtil.AppKey;
            service.EndpointUrl = ITUtil.EndpointUrl;

            var obj = CreateObjectInstance();

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /// <summary>
        /// 通信（基本） - アプリケーションキーがnull
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestAppKeyExceptionNull()
        {
            var service = GetServiceInstance();
            service.TenantId = ITUtil.TenantId;
            service.AppId = ITUtil.AppId;
            service.EndpointUrl = ITUtil.EndpointUrl;

            var obj = CreateObjectInstance();

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /// <summary>
        /// 通信（基本） - エンドポイントURLがnull
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestEndpointUrlExceptionNull()
        {
            var service = GetServiceInstance();
            service.TenantId = ITUtil.TenantId;
            service.AppId = ITUtil.AppId;
            service.AppKey = ITUtil.AppKey;

            var obj = CreateObjectInstance();

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /// <summary>
        /// 通信（基本） - テナントIDがサーバに存在しない
        /// NbHttpExceptionが発生すること
        /// HTTPステータスコードが"404 NotFound"であること
        /// </summary>
        [Test]
        public async void TestTenantIdExceptionNotExist()
        {
            var service = GetServiceInstance();
            service.TenantId = "dummyTenantId";
            service.AppId = ITUtil.AppId;
            service.AppKey = ITUtil.AppKey;
            service.EndpointUrl = ITUtil.EndpointUrl;

            var obj = CreateObjectInstance();

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
        }

        /// <summary>
        /// 通信（基本） - アプリケーションIDがサーバに存在しない
        /// NbHttpExceptionが発生すること
        /// HTTPステータスコードが"401 Unauthorized"であること
        /// </summary>
        [Test]
        public async void TestAppIdExceptionNotExist()
        {
            var service = GetServiceInstance();
            service.TenantId = ITUtil.TenantId;
            service.AppId = "dummyAppId";
            service.AppKey = ITUtil.AppKey;
            service.EndpointUrl = ITUtil.EndpointUrl;

            var obj = CreateObjectInstance();

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, e.StatusCode);
            }
        }

        /// <summary>
        /// 通信（基本） - アプリケーションキーがサーバに存在しない
        /// NbHttpExceptionが発生すること
        /// HTTPステータスコードが"401 Unauthorized"であること
        /// </summary>
        [Test]
        public async void TestAppKeyExceptionNotExist()
        {
            var service = GetServiceInstance();
            service.TenantId = ITUtil.TenantId;
            service.AppId = ITUtil.AppId;
            service.AppKey = "dummyAppKey";
            service.EndpointUrl = ITUtil.EndpointUrl;

            var obj = CreateObjectInstance();

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, e.StatusCode);
            }
        }

        /// <summary>
        /// 通信（基本） - 通信成功
        /// オブジェクトの新規登録が成功し、その結果が正しいこと
        /// </summary>
        [Test]
        public async void TestCreateObjectNormal()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();

            var result = await CreateObject(new NbAcl());

            Assert.True(result.HasKey("key"));
            Assert.AreEqual("value", result.Get<string>("key"));
        }

        /// <summary>
        /// 通信（基本） - サーバからエラー返却
        /// NbHttpExceptionが発生すること
        /// HTTPステータスコードが正しいこと
        /// レスポンスボディが格納されていること
        /// </summary>
        [Test]
        public async void TestCreateObjectException()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();

            var obj = new NbObject("NotExistBucket");
            obj["key"] = "value";

            try
            {
                await obj.SaveAsync();
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
                Assert.IsNotNull(e.Response);
            }
        }
        #endregion

        #region ■RESTAPI共通
        /// <summary>
        /// 非同期通信 - オブジェクト作成
        /// API呼び出し時のスレッドでコールバックを受けること
        /// </summary>
        [Test]
        public async void TestActionThreadNormal()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();

            // バケット作成＆データクリア
            await ITUtil.InitOnlineObjectStorage();

            var obj = CreateObjectInstance();

            var thread1 = Thread.CurrentThread.ManagedThreadId;

            await obj.SaveAsync();

            var thread2 = Thread.CurrentThread.ManagedThreadId;

            Assert.AreEqual(thread1, thread2);
        }
        #endregion

        #region ■JSON
        /// <summary>
        /// valueの型
        /// string型のvalueを格納・取得できること
        /// number型のvalueを格納・取得できること
        /// bool型のvalueを格納・取得できること
        /// nullのvalueを格納・取得できること
        /// NbJsonObject型のvalueを格納・取得できること
        /// IEnumerable型のvalueを格納・取得できること
        /// NbJsonArray型のvalueを格納・取得できること
        /// </summary>
        [Test]
        public void TestJsonNormal()
        {
            var json = new NbJsonObject();
            json["KeyString"] = "ValueString";
            json["KeyStringNumber"] = "123";
            json["KeyNumberInt"] = (int)1;
            json["KeyNumberFloat"] = (float)3.141592;
            json["KeyBool"] = true;
            json["KeyNull"] = null;

            var jsonObjectValue = new NbJsonObject
            {
                {"a", -1.2}
            };
            json["KeyJsonObject"] = jsonObjectValue;

            var enumerableValue = new HashSet<string>();
            enumerableValue.Add("c");
            enumerableValue.Add("d");
            json["KeyEnumerable"] = enumerableValue;

            var jsonArrayValue = new NbJsonArray
            {
                1, "test", 3
            };
            json["KeyJsonArray"] = jsonArrayValue;

            Assert.AreEqual("ValueString", json["KeyString"]);
            Assert.AreEqual("ValueString", json.Get<string>("KeyString"));
            Assert.AreEqual("ValueString", json.Opt<string>("KeyString", null));

            Assert.AreEqual("123", json["KeyStringNumber"]);
            Assert.AreEqual("123", json.Get<string>("KeyStringNumber"));
            Assert.AreEqual("123", json.Opt<string>("KeyStringNumber", null));

            Assert.AreEqual(1, json["KeyNumberInt"]);
            Assert.AreEqual(1, json.Get<int>("KeyNumberInt"));
            Assert.AreEqual(1, json.Opt<int>("KeyNumberInt", 0));

            Assert.AreEqual(3.141592f, json["KeyNumberFloat"]);
            Assert.AreEqual(3.141592f, json.Get<float>("KeyNumberFloat"));
            Assert.AreEqual(3.141592f, json.Opt<float>("KeyNumberFloat", 0));

            Assert.AreEqual(true, json["KeyBool"]);
            Assert.True(json.Get<bool>("KeyBool"));
            Assert.True(json.Opt<bool>("KeyBool", false));

            Assert.AreEqual(null, json["KeyNull"]);
            Assert.AreEqual(null, json.Get<string>("KeyNull"));
            Assert.AreEqual(0, json.Get<int>("KeyNull"));
            Assert.AreEqual(null, json.Opt<string>("KeyNull", "dummy"));
            Assert.AreEqual(0, json.Opt<int>("KeyNull", 1));

            Assert.AreEqual(jsonObjectValue, json["KeyJsonObject"]);
            Assert.AreEqual(jsonObjectValue, json.Get<NbJsonObject>("KeyJsonObject"));
            Assert.AreEqual(jsonObjectValue, json.Opt<NbJsonObject>("KeyJsonObject", null));
            Assert.AreEqual(jsonObjectValue, json.GetJsonObject("KeyJsonObject"));

            Assert.AreEqual(enumerableValue, json["KeyEnumerable"]);
            Assert.AreEqual(enumerableValue, json.Get<IEnumerable<object>>("KeyEnumerable"));
            Assert.AreEqual(enumerableValue, json.Opt<IEnumerable<object>>("KeyEnumerable", null));
            Assert.AreEqual(enumerableValue, json.GetEnumerable("KeyEnumerable"));

            Assert.AreEqual(jsonArrayValue, json["KeyJsonArray"]);
            Assert.AreEqual(jsonArrayValue, json.Get<NbJsonArray>("KeyJsonArray"));
            Assert.AreEqual(jsonArrayValue, json.Opt<NbJsonArray>("KeyJsonArray", null));
            Assert.AreEqual(jsonArrayValue, json.GetArray("KeyJsonArray"));
        }

        /// <summary>
        /// JSONオブジェクト - keyが存在しない
        /// KeyNotFoundExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(KeyNotFoundException))]
        public void TestGetExceptionKeyNotFound()
        {
            var o = new NbJsonObject()
            {
                {"a", "b"}
            };

            var v = o.Get<int>("c");
        }

        /// <summary>
        /// JSONオブジェクト - keyが存在しない
        /// デフォルト値を取得できること
        /// </summary>
        [Test]
        public void TestOptSubnormalKeyNotFound()
        {
            var o = new NbJsonObject()
            {
                {"a", "test"}
            };

            var v = o.Opt<int>("c", 1);
            Assert.AreEqual(1, v);
        }

        /// <summary>
        /// JSONオブジェクト - keyが存在しない
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestGetJsonObjectSubnormalKeyNotFound()
        {
            var aJson = new NbJsonObject()
            {
                {"b", 1},
                {"c", "test"}
            };
            var o = new NbJsonObject()
            {
                {"a", aJson}
            };

            var v = o.GetJsonObject("b");

            Assert.IsNull(v);
        }

        /// <summary>
        /// JSONオブジェクト - keyが存在しない
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestGetEnumerableSubnormalKeyNotFound()
        {
            var aValue = new HashSet<string>();
            aValue.Add("b");
            aValue.Add("c");

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetEnumerable("d");

            Assert.IsNull(v);
        }

        /// <summary>
        /// JSONオブジェクト - keyが存在しない
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestGetArraySubnormalKeyNotFound()
        {
            var aValue = new NbJsonArray
            {
                new NbJsonObject {{"b", "test"}},
                new NbJsonObject {{"c", 1}}
            };

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetArray("d");

            Assert.IsNull(v);
        }

        /// <summary>
        /// JSONオブジェクト - keyがnull
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetExceptionKeyNull()
        {
            var o = new NbJsonObject()
            {
                {"a", "test"}
            };

            var v = o.Get<string>(null);
        }

        /// <summary>
        /// JSONオブジェクト - keyがnull
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOptExceptionKeyNull()
        {
            var o = new NbJsonObject()
            {
                {"a", "test"}
            };

            var value = o.Opt<int>(null, 1);
        }

        /// <summary>
        /// JSONオブジェクト - keyがnull
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetJsonObjectExceptionKeyNull()
        {
            var aJson = new NbJsonObject()
            {
                {"b", 1},
                {"c", "test"}
            };
            var o = new NbJsonObject()
            {
                {"a", aJson}
            };

            var v = o.GetJsonObject(null);
        }

        /// <summary>
        /// JSONオブジェクト - keyがnull
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetEnumerableExceptionKeyNull()
        {
            var aValue = new HashSet<string>();
            aValue.Add("b");
            aValue.Add("c");

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetEnumerable(null);
        }

        /// <summary>
        /// JSONオブジェクト - keyがnull
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetArrayExceptionKeyNull()
        {
            var aValue = new NbJsonArray
            {
                new NbJsonObject {{"b", "test"}},
                new NbJsonObject {{"c", 1}}
            };

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetArray(null);
        }

        /// <summary>
        /// JSON配列 - インデックス指定による取得
        /// 指定した位置の値を取得できること
        /// 指定した位置の値(NbJsonObject)を取得できること
        /// 指定した位置の値(NbJsonArray)を取得できること
        /// </summary>
        [Test]
        public void TestGetNormal()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add("1");
            var json = new NbJsonObject()
            {
                {"a", "b"}
            };
            jsonArray.Add(json);
            var innerJsonArray = new NbJsonArray();
            innerJsonArray.Add(1);
            innerJsonArray.Add(2);
            jsonArray.Add(innerJsonArray);

            Assert.AreEqual("1", jsonArray.Get<string>(0));
            Assert.AreEqual(json, jsonArray.GetJsonObject(1));
            Assert.AreEqual(innerJsonArray, jsonArray.GetArray(2));
        }

        /// <summary>
        /// JSON配列 - インデックスが範囲外
        /// ArgumentOutOfRangeExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetExceptionIndexOutOfRange()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.Get<int>(4);
        }

        /// <summary>
        /// JSON配列 - インデックスが範囲外
        /// ArgumentOutOfRangeExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetJsonObjectExceptionIndexOutOfRange()
        {
            var jsonArray = new NbJsonArray();
            var json = new NbJsonObject()
            {
                {"a", "b"}
            };
            jsonArray.Add(json);

            var v = jsonArray.GetJsonObject(1);
        }

        /// <summary>
        /// JSON配列 - インデックスが範囲外
        /// ArgumentOutOfRangeExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetArrayExceptionIndexOutOfRange()
        {
            var json = new NbJsonArray();
            json.Add(1);
            json.Add(2);
            json.Add(3);
            var jsonArray = new NbJsonArray();
            jsonArray.Add(json);

            var v = jsonArray.GetArray(-1);
        }

        /// <summary>
        /// JSON配列 - リスト取得
        /// リスト型の型変換を行えること
        /// </summary>
        [Test]
        public void TestToListNormal()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.ToList<int>();

            Assert.True(v is IList<int>);
            Assert.AreEqual(3, v.Count);
            Assert.AreEqual(1, v[0]);
            Assert.AreEqual(2, v[1]);
            Assert.AreEqual(3, v[2]);
        }

        /// <summary>
        /// JSON配列 - リスト取得時の型が異なる
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestToListExceptionInvalidType()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.ToList<string>();
        }

        /// <summary>
        /// 型変換(JSONオブジェクト)
        /// int型のvalueをlong型で取得できること
        /// long型のvalueをdouble型で取得できること
        /// long型のvalueをint型で取得できること
        /// double型のvalueをlong型で取得できること
        /// </summary>
        [Test]
        public void TestJsonObjectTypeConversionNormal()
        {
            var json = new NbJsonObject
            {
                {"int", 100},
                {"long", 100L},
                {"double", 100.123456},
            };

            Assert.AreEqual(100L, json.Get<long>("int"));
            Assert.AreEqual(100.0, json.Get<double>("long"));
            Assert.AreEqual(100L, json.Opt<long>("int", 0L));
            Assert.AreEqual(100.0, json.Opt<double>("long", 0.0));

            Assert.AreEqual(100, json.Get<int>("long"));
            Assert.AreEqual(100L, json.Get<long>("double"));
            Assert.AreEqual(100, json.Opt<int>("long", 0));
            Assert.AreEqual(100L, json.Opt<long>("double", 0L));
        }

        /// <summary>
        /// 型変換(JSONオブジェクト)
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test]
        public void TestJsonObjectTypeConversionException()
        {
            var json = new NbJsonObject
            {
                {"bool", true},
                {"string", "100"}
            };

            Assert.True(json.Get<bool>("bool"));
            Assert.AreEqual("100", json.Get<string>("string"));

            try
            {
                json.Get<int>("string");
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.Opt<int>("string", 0);
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.Get<long>("bool");
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.Opt<long>("bool", 0L);
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.GetJsonObject("string");
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.GetEnumerable("string");
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.GetArray("string");
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }
        }

        /// <summary>
        /// 型変換(JSON配列)
        /// int型のvalueをlong型で取得できること
        /// long型のvalueをdouble型で取得できること
        /// long型のvalueをint型で取得できること
        /// double型のvalueをlong型で取得できること
        /// </summary>
        [Test]
        public void TestJsonArrayTypeConversionNormal()
        {
            var json = new NbJsonArray
            {
                100,
                100L,
                100.123456,
            };

            Assert.AreEqual(100L, json.Get<long>(0));
            Assert.AreEqual(100.0, json.Get<double>(1));
            Assert.AreEqual(100, json.Get<int>(1));
            Assert.AreEqual(100L, json.Get<long>(2));
        }

        /// <summary>
        /// 型変換(JSON配列)
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test]
        public void TestJsonArrayTypeConversionException()
        {
            var json = new NbJsonArray
            {
                100,
                100L
            };

            Assert.AreEqual(100, json.Get<int>(0));
            Assert.AreEqual(100L, json.Get<long>(1));

            try
            {
                json.Get<string>(0);
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.Get<bool>(1);
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.GetJsonObject(0);
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.GetArray(0);
                Assert.Fail("No exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }
        }

        /// <summary>
        /// パース・変換 - JSON文字列→JSONオブジェクト
        /// 生成されたJSONオブジェクトが正しいこと
        /// </summary>
        [Test]
        public void TestJsonObjectParseNormal()
        {
            var jsonString1 = "{a:1, b:'txt', c:true, d:{d1:1, d2:2, d3:[1,2,3]}, e:['e1','e2','e3'], f:null, g:'\uD842\uDF9Fる', '\uD867\uDE3D':'好きです', h:' !\\\"#$%&\\'()*+-.,/:;<=>?@[]^_`{|}~', ' !\\\"#$%&\\'()*+-.,/:;<=>?@[]^_`{|}~':'記号'}";
            var jsonString2 = "{\"a\":1,\"b\":\"txt\",\"c\":true,\"d\":{\"d1\":1,\"d2\":2,\"d3\":[1,2,3]},\"e\":[\"e1\",\"e2\",\"e3\"],\"f\":null,\"g\":\"\uD842\uDF9Fる\",\"\uD867\uDE3D\":\"好きです\",\"h\":\" !\\\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~\",\" !\\\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~\":\"記号\"}";

            var json1 = NbJsonObject.Parse(jsonString1);
            var json2 = NbJsonObject.Parse(jsonString2);

            var expectedJson = new NbJsonObject
            {
                {"a", 1},
                {"b", "txt"},
                {"c", true},
                {"d", new NbJsonObject {
                    {"d1", 1},
                    {"d2", 2},
                    {"d3", new int[] {1, 2, 3}}
                }},
                {"e", new NbJsonArray {
                    "e1", "e2", "e3"
                }},
                {"f", null},
                {"g", "\uD842\uDF9Fる"},
                {"\uD867\uDE3D", "好きです"},
                {"h", " !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~"},
                {" !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~", "記号"}
            };

            Assert.AreEqual(expectedJson, json1);
            Assert.AreEqual(json1, json2);
        }

        /// <summary>
        /// パース・変換 - JSONオブジェクト→JSON文字列
        /// 生成されたJSON文字列が正しいこと
        /// </summary>
        [Test]
        public void TestJsonObjectToStringNormal()
        {
            var json = new NbJsonObject
            {
                {"a", 1},
                {"b", "txt"},
                {"c", true},
                {"d", new NbJsonObject {
                    {"d1", 1},
                    {"d2", 2},
                    {"d3", new int[] {1, 2, 3}}
                }},
                {"e", new NbJsonArray {
                    "e1", "e2", "e3"
                }},
                {"f", null},
                {"g", "\uD842\uDF9Fる"},
                {"\uD867\uDE3D", "好きです"},
                {"h", " !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~"},
                {" !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~", "記号"}
            };

            var s = json.ToString();

            Assert.AreEqual("{\"a\":1,\"b\":\"txt\",\"c\":true,\"d\":{\"d1\":1,\"d2\":2,\"d3\":[1,2,3]},\"e\":[\"e1\",\"e2\",\"e3\"],\"f\":null,\"g\":\"\uD842\uDF9Fる\",\"\uD867\uDE3D\":\"好きです\",\"h\":\" !\\\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~\",\" !\\\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~\":\"記号\"}", s);
        }

        /// <summary>
        /// パース・変換 - JSON配列文字列→JSON配列
        /// 生成されたJSON配列が正しいこと
        /// </summary>
        [Test]
        public void TestJsonArrayParseNormal()
        {
            var jsonString = "[1,\"string\",false,{\"a\":\"b\"},[1,2,3],null,\"\uD842\uDF9Fる\",\" !\\\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~\"]";
            var jsonArray = NbJsonArray.Parse(jsonString);

            Assert.AreEqual(8, jsonArray.Count);
            Assert.AreEqual(1, jsonArray[0]);
            Assert.AreEqual("string", jsonArray[1]);
            Assert.AreEqual(false, jsonArray[2]);
            var value1 = new NbJsonObject()
            {
                {"a", "b"}
            };
            Assert.AreEqual(value1, jsonArray[3]);
            var value2 = new NbJsonArray()
            {
                1, 2, 3
            };
            Assert.AreEqual(value2, jsonArray[4]);
            Assert.AreEqual(null, jsonArray[5]);
            Assert.AreEqual("\uD842\uDF9Fる", jsonArray[6]);
            Assert.AreEqual(" !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~", jsonArray[7]);
        }

        /// <summary>
        /// パース・変換 - JSON配列→JSON配列文字列
        /// 生成されたJSON配列文字列が正しいこと
        /// </summary>
        [Test]
        public void TestJsonArrayToStringNormal()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add("string");
            jsonArray.Add(false);
            var json = new NbJsonObject()
            {
                {"a", "b"}
            };
            jsonArray.Add(json);
            var innerArray = new NbJsonArray()
            {
                1, 2, 3
            };
            jsonArray.Add(innerArray);
            jsonArray.Add(null);
            jsonArray.Add("\uD842\uDF9Fる");
            jsonArray.Add(" !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~");

            Assert.AreEqual("[1,\"string\",false,{\"a\":\"b\"},[1,2,3],null,\"\uD842\uDF9Fる\",\" !\\\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~\"]", jsonArray.ToString());
        }

        /// <summary>
        /// パース・変換 - JSON文字列パース失敗
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestJsonObjectParseException()
        {
            NbJsonObject.Parse(";l;:sdflg:s;dfgl");
        }

        /// <summary>
        /// パース・変換 - JSON配列文字列パース失敗
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestJsonArrayParseException()
        {
            NbJsonArray.Parse("");
        }
        #endregion

        /// <summary>
        /// サービスにテナントID等を設定。
        /// オブジェクトバケット作成、バケット内全オブジェクト削除を行う。
        /// </summary>
        private async Task InitObjectTest()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();

            // バケット作成＆データクリア
            await ITUtil.InitOnlineObjectStorage();
        }

        /// <summary>
        /// オブジェクトインスタンスを生成する。
        /// Nebulaサーバへの登録は行わない。
        /// </summary>
        /// <returns>生成したオブジェクトインスタンス</returns> 
        private NbObject CreateObjectInstance()
        {
            var obj = new NbObject(ITUtil.ObjectBucketName);
            obj["key"] = "value";

            return obj;
        }

        /// <summary>
        /// オブジェクトインスタンスを生成する。
        /// Nebulaサーバへの登録も行う。
        /// </summary>
        /// <returns>生成したオブジェクトインスタンス</returns>
        private async Task<NbObject> CreateObject(NbAcl acl)
        {
            await InitObjectTest();

            var obj = CreateObjectInstance();
            obj.Acl = acl;

            var result = await obj.SaveAsync();

            return result;
        }

        /// <summary>
        /// オブジェクト作成テスト
        /// 登録後のACLと一致しない場合はFailする。
        /// </summary>
        /// <param name="acl">ACL</param>
        private async Task CreateObjectTest(NbAcl acl)
        {
            var result = await CreateObject(acl);

            Assert.True(ITUtil.CompareAcl(acl, result.Acl));
        }

        /// <summary>
        /// オブジェクト取得
        /// isSuccessに従った結果でない場合はFailする。
        /// </summary>
        /// <param name="id">オブジェクトID</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        private async Task ReadObject(string id, bool isSuccess)
        {
            var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);

            try
            {
                await bucket.GetAsync(id);
                if (!isSuccess)
                {
                    Assert.Fail("No exception");
                }
            }
            catch (NbHttpException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// オブジェクト取得テスト
        /// isSuccessに従った結果でない場合はFailする。
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        private async Task ReadObjectTest(NbAcl acl, bool isSuccess)
        {
            var obj = await CreateObject(acl);

            await ReadObject(obj.Id, isSuccess);
        }

        /// <summary>
        /// オブジェクト取得テスト(Owner)
        /// 期待通りでなければ、Failする。
        /// </summary>
        private async Task ReadObjectTestWithOwner()
        {
            string userId = await GetLoggedInUserId();
            var acl = new NbAcl();
            acl.Owner = userId;
            var obj = await CreateObject(acl);

            await ReadObject(obj.Id, true);

            // ログアウト
            await ITUtil.Logout();

            // 別のユーザでログイン
            var otherUser = new NbUser()
            {
                Username = Username,
                Email = Email
            };
            await ITUtil.SignUpAndLogin(otherUser);

            await ReadObject(obj.Id, false);
        }

        /// <summary>
        /// オブジェクト更新
        /// isSuccessに従った結果でない場合はFailする。
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        private async Task UpdateObject(NbObject obj, bool isSuccess)
        {
            try
            {
                await obj.SaveAsync();
                if (!isSuccess)
                {
                    Assert.Fail("No exception");
                }
            }
            catch (NbHttpException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /// <summary>
        /// オブジェクト更新テスト
        /// isSuccessに従った結果でない場合はFailする。
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        private async Task UpdateObjectTest(NbAcl acl, bool isSuccess)
        {
            var obj = await CreateObject(acl);

            await UpdateObject(obj, isSuccess);
        }

        /// <summary>
        /// オブジェクト更新テスト(Admin)
        /// isSuccessに従った結果でない場合はFailする。
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        private async Task UpdateObjectTestWithAdmin(NbAcl acl, bool isSuccess)
        {
            var obj = await CreateObject(acl);
            var updateAcl = new NbAcl();
            updateAcl.D.Add("g:anonymous");
            obj.Acl = updateAcl;

            await UpdateObject(obj, isSuccess);
        }

        /// <summary>
        /// オブジェクト更新テスト(Owner)
        /// 期待通りでなければ、Failする。
        /// </summary>
        private async Task UpdateObjectTestWithOwner()
        {
            string userId = await GetLoggedInUserId();
            var acl = new NbAcl();
            acl.Owner = userId;
            var obj = await CreateObject(acl);

            await UpdateObject(obj, true);

            // ログアウト
            await ITUtil.Logout();

            // 別のユーザでログイン
            var otherUser = new NbUser()
            {
                Username = Username,
                Email = Email
            };
            await ITUtil.SignUpAndLogin(otherUser);

            await UpdateObject(obj, false);
        }

        /// <summary>
        /// オブジェクト削除
        /// isSuccessに従った結果でない場合はFailする。
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        private async Task DeleteObject(NbObject obj, bool isSuccess)
        {
            try
            {
                await obj.DeleteAsync();
                if (!isSuccess)
                {
                    Assert.Fail("No exception");
                }
            }
            catch (NbHttpException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /// <summary>
        /// オブジェクト削除テスト
        /// isSuccessに従った結果でない場合はFailする。
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        private async Task DeleteObjectTest(NbAcl acl, bool isSuccess)
        {
            var obj = await CreateObject(acl);

            await DeleteObject(obj, isSuccess);
        }

        /// <summary>
        /// オブジェクト削除テスト(Owner)
        /// 期待通りでなければ、Failする。
        /// </summary>
        private async Task DeleteObjectTestWithOwner()
        {
            string userId = await GetLoggedInUserId();

            var acl = new NbAcl();
            acl.Owner = userId;
            var obj = await CreateObject(acl);

            await ITUtil.Logout();

            // 別のユーザでログイン
            var otherUser = new NbUser()
            {
                Username = Username,
                Email = Email
            };
            await ITUtil.SignUpAndLogin(otherUser);

            await DeleteObject(obj, false);

            // ログアウト
            await ITUtil.Logout();
            // オーナーユーザでログイン
            await NbUser.LoginWithUsernameAsync(ITUtil.Username, ITUtil.Password);

            await DeleteObject(obj, true);
        }

        /// <summary>
        /// ログインユーザIDを取得する。
        /// </summary>
        /// <returns>ログインユーザID</returns>
        private async Task<string> GetLoggedInUserId()
        {
            var service = GetServiceInstance();
            ITUtil.InitNebula();

            await ITUtil.InitOnlineUser();
            var loggedInUser = await ITUtil.SignUpAndLogin();

            return loggedInUser.UserId;
        }

        /// <summary>
        /// NbServiceをDisposeする。
        /// </summary>
        private void DisposeService()
        {
            _isServiceEnabled = false;
            NbService.DisposeSingleton();
        }

        /// <summary>
        /// NbServiceのインスタンスを取得する。
        /// </summary>
        /// <returns>NbService</returns>
        private NbService GetServiceInstance()
        {
            _isServiceEnabled = true;
            return NbService.GetInstance();
        }
    }
}
