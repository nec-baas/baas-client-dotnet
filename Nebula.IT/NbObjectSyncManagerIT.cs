using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    [TestFixture]
    public class NbObjectSyncManagerIT
    {
        private const string ObjectBucketName = ITUtil.ObjectBucketName;

        private NbObjectSyncManager _syncManager;

        private NbService _service;
        private NbObjectBucketBase<NbObject> _onlineBucket;
        private NbObjectBucketBase<NbOfflineObject> _offlineBucket;

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();

            _service = NbService.Singleton;
            //NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            NbOfflineService.DeleteCacheAll().Wait();

            _syncManager = new NbObjectSyncManager(_service);

            // バケットとオブジェクトの初期化
            ITUtil.InitOnlineObjectStorage().Wait();

            ITUtil.InitOnlineUser().Wait();

            ITUtil.Logout().Wait();

            ITUtil.UseNormalKey();

            // 全範囲同期を指定
            _syncManager.SetSyncScope(ObjectBucketName, new NbQuery());

            _onlineBucket = new NbObjectBucket<NbObject>(ObjectBucketName, _service);
            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucketName, _service);
        }

        // パラメータ関連 --------------------------------------------------

        /// <summary>
        /// オフラインサービス無効
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestConstructorExceptionParamsOfflineServiceDisabled()
        {
            // OfflineService無効化
            _service.DisableOffline();

            // test
            var syncManager = new NbObjectSyncManager();
        }

        /// <summary>
        /// バケット名null
        /// </summary>
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async void TestSyncBucketAsyncExceptionParamsBucketNameNull()
        {
            // test
            var result = await _syncManager.SyncBucketAsync(null, NbObjectConflictResolver.PreferServerResolver);
        }

        /// <summary>
        /// リゾルバ指定なし
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalParamsNoResolver()
        {
            await CreateConflictObject("server", "client");

            var onlineObj = (await GetOnlineObjects()).First();

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            var offlineObj = (await GetOfflineObjects()).First();

            CompareObjects(offlineObj, onlineObj);
            Assert.AreEqual("server", offlineObj["key"]);
        }

        /// <summary>
        /// 同期範囲未指定
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async void TestSyncBucketAsyncExceptionParamsNoSyncScope()
        {
            // 同期範囲を削除
            _syncManager.RemoveSyncScope(ObjectBucketName);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
        }

        /// <summary>
        /// ユーザ定義リゾルバ(サーバ)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalParamsUserDefinedResolverServer()
        {
            // "key"が大きいオブジェクトを採用するリゾルバ
            NbObjectConflictResolver.Resolver userDefinedResolver = (server, client) => (Convert.ToInt32(server["key"]) >= Convert.ToInt32(client["key"])) ? server : client;

            await CreateConflictObject(1, -1);

            var onlineObj = (await GetOnlineObjects()).First();

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName, userDefinedResolver);

            var offlineObj = (await GetOfflineObjects()).First();

            CompareObjects(offlineObj, onlineObj);
            // サーバを採用
            Assert.AreEqual(1, offlineObj["key"]);
        }

        /// <summary>
        /// ユーザ定義リゾルバ(クライアント)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalParamsUserDefinedResolverClient()
        {
            // "key"が大きいオブジェクトを採用するリゾルバ
            NbObjectConflictResolver.Resolver userDefinedResolver = (server, client) => (Convert.ToInt32(server["key"]) >= Convert.ToInt32(client["key"])) ? server : client;

            await CreateConflictObject(-1, 1);

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName, userDefinedResolver);

            var onlineObj = (await GetOnlineObjects()).First();
            var offlineObj = (await GetOfflineObjects()).First();

            CompareObjects(offlineObj, onlineObj);
            // クライアントを採用
            Assert.AreEqual(1, offlineObj["key"]);
        }

        /// <summary>
        /// ユーザ定義リゾルバ不正
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async void TestSyncBucketAsyncExceptionParamsUserDefinedResolverInvalid()
        {
            // 新規オブジェクトを返却するリゾルバ
            NbObjectConflictResolver.Resolver invalidResolver = (server, client) => new NbObject();

            var obj = _onlineBucket.NewObject();
            var serverObj = await obj.SaveAsync();

            await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            // サーバオブジェクトの更新
            serverObj["key"] = 1; // value
            var reSavedServerObj = await serverObj.SaveAsync();

            // クライアントオブジェクトの更新
            var clientObjects = await _offlineBucket.QueryAsync(new NbQuery());
            var clientObj = clientObjects.First();
            clientObj["key"] = -1;  // value
            var reSavedClientObj = await clientObj.SaveAsync();

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName, invalidResolver);
        }

        // Pull処理 --------------------------------------------------

        /// <summary>
        /// ContentACL(Read)権限あり
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullContentAclReadable()
        {
            var onlineCreatedObj = await CreateOnlineObject();

            // contentAclにRead権限付与
            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            // contentAclにRead権限付与
            contentAcl.R.Add("g:anonymous");

            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗なし
            Assert.AreEqual(0, result.Count);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());
        }

        /// <summary>
        /// ContentACL(Read)権限無し(MasterKey)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullContentAclUnreadableMasterKey()
        {
            var onlineCreatedObj = await CreateOnlineObject();

            // contentAclにRead権限無し
            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            ITUtil.UseMasterKey();
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            ITUtil.UseNormalKey();
            // 失敗なし
            Assert.AreEqual(0, result.Count);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());
        }

        /// <summary>
        /// ContentACL(Read)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPullContentAclUnreadable()
        {
            var onlineCreatedObj = await CreateOnlineObject();

            // contentAclにRead権限無し
            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            try
            {
                var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
                Assert.Fail("unexpectedly success");
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, ex.StatusCode);
            }
        }

        /// <summary>
        /// ACL(Read)権限有り
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullAclReadable()
        {
            var obj = _onlineBucket.NewObject();
            obj.Acl = NbAcl.CreateAclForAnonymous();
            var saved = await obj.SaveAsync();

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗なし
            Assert.AreEqual(0, result.Count);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());
        }

        /// <summary>
        /// ACL(Read)権限無し(MasterKey)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullAclUnreadableMasterKey()
        {
            await ITUtil.InitOnlineUser();

            // ログイン状態でオブジェクトを作成
            var user = await ITUtil.SignUpAndLogin();

            // anonymous userにread権限無し
            var obj = _onlineBucket.NewObject();
            var saved = await obj.SaveAsync();

            // test
            // 権限のない状態で同期を行う
            await NbUser.LogoutAsync();
            ITUtil.UseMasterKey();
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            ITUtil.UseNormalKey();
            // 失敗なし
            Assert.AreEqual(0, result.Count);

            // ログアウト状態で確認
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            await NbUser.LoginWithEmailAsync(ITUtil.Email, ITUtil.Password);

            // ログイン状態で確認
            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());

            await NbUser.LogoutAsync();
        }

        /// <summary>
        /// ACL(Read)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullAclUnreadable()
        {
            await ITUtil.InitOnlineUser();

            // ログイン状態でオブジェクトを作成
            var user = await ITUtil.SignUpAndLogin();

            // anonymous userにread権限無し
            var obj = _onlineBucket.NewObject();
            var saved = await obj.SaveAsync();

            // test
            // 権限のない状態で同期を行う
            await NbUser.LogoutAsync();
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗なし
            Assert.AreEqual(0, result.Count);

            // ログアウト状態で確認
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            await NbUser.LoginWithEmailAsync(ITUtil.Email, ITUtil.Password);

            // ログイン状態で確認
            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            await NbUser.LogoutAsync();
        }

        /// <summary>
        /// パス不正(テナントID不正)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPullPathInvalidTenantId()
        {
            // 不正なテナントIDに変更
            var service = NbService.GetInstance();
            service.TenantId = "12345ABCDE";

            // test
            try
            {
                await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
            }
        }

        /// <summary>
        /// パス不正(バケット無し)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPullPathNoBucket()
        {
            // バケット削除
            await DeleteObjectBucket(ObjectBucketName);

            // test
            try
            {
                await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
            }
        }

        /// <summary>
        /// AppId不正
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPullHttpHeaderInvalidAppId()
        {
            // AppId不正
            _service.AppId = "1234567abcde";

            // test
            try
            {
                await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, ex.StatusCode);
            }
        }

        // オブジェクト検索条件 --------------------------------------------------

        /// <summary>
        /// 初回同期: 初期オブジェクト無し
        /// 2回目同期: 追加オブジェクト無し
        /// 3回目同期: オブジェクト変化なし(同期範囲再設定)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullSearchFirstNoObjectSecodNoObject()
        {
            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // 同期範囲再設定
            _syncManager.SetSyncScope(ObjectBucketName, new NbQuery());

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// 初回同期: 初期オブジェクト無し
        /// 2回目同期: 追加オブジェクト有り
        /// 3回目同期: オブジェクト変化なし(同期範囲再設定)
        /// オブジェクトは同期範囲外とする
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullSearchFirstNoObjectSecodAddObjectsOutOfScope()
        {
            var query = new NbQuery();
            query.EqualTo("key", "In");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // オブジェクトの追加
            var onlineCreatedObj = await CreateOnlineObject("key", "Out");

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // 同期範囲再設定
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// 初回同期: 初期オブジェクト無し
        /// 2回目同期: 追加オブジェクト有り
        /// 3回目同期: オブジェクト変化なし(同期範囲再設定)
        /// オブジェクトは同期範囲内とする
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullSearchFirstNoObjectSecodAddObjectsInScope()
        {
            var query = new NbQuery();
            query.EqualTo("key", "In");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // オブジェクトの追加
            var onlineCreatedObj = await CreateOnlineObject("key", "In");

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());

            // 物理削除
            await _offlineBucket.DeleteAsync(new NbQuery(), false);

            // 同期範囲再設定
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());
        }

        /// <summary>
        /// 初回同期: 初期オブジェクト有り
        /// 2回目同期: 追加オブジェクト無し
        /// 3回目同期: オブジェクト変化なし(同期範囲再設定)
        /// オブジェクトは同期範囲外とする
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullSearchFirstAddObjectsSecodNoObjectOutOfScope()
        {
            var query = new NbQuery();
            query.EqualTo("key", "In");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オブジェクトの追加
            var onlineCreatedObj = await CreateOnlineObject("key", "Out");

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // 同期範囲再設定
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// 初回同期: 初期オブジェクト有り
        /// 2回目同期: 追加オブジェクト無し
        /// 3回目同期: オブジェクト変化なし(同期範囲再設定)
        /// オブジェクトは同期範囲内とする
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullSearchFirstAddObjectsSecodNoObjectInScope()
        {
            var query = new NbQuery();
            query.EqualTo("key", "In");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オブジェクトの追加
            var onlineCreatedObj = await CreateOnlineObject("key", "In");

            // 一定時間待機
            await Task.Delay(NbObjectSyncManager.PullTimeOffsetSeconds * 1000);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());

            // 物理削除
            await _offlineBucket.DeleteAsync(new NbQuery(), false);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // 同期範囲再設定
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());
        }

        /// <summary>
        /// 初回同期: 初期オブジェクト有り
        /// 2回目同期: 追加オブジェクト有り
        /// 3回目同期: オブジェクト変化なし(同期範囲再設定)
        /// オブジェクトは同期範囲外とする
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullSearchFirstAddObjectsSecodAddObjectsOutOfScope()
        {
            var query = new NbQuery();
            query.EqualTo("key", "In");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オブジェクトの追加
            var onlineCreatedObj = await CreateOnlineObject("key", "Out");

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // オブジェクトの追加
            var onlineCreatedObj2 = await CreateOnlineObject("key", "Out");

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());

            // 同期範囲再設定
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// 初回同期: 初期オブジェクト有り
        /// 2回目同期: 追加オブジェクト有り
        /// 3回目同期: オブジェクト変化なし(同期範囲再設定)
        /// オブジェクトは同期範囲内とする
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullSearchFirstAddObjectsSecodAddObjectsInScope()
        {
            var query = new NbQuery();
            query.EqualTo("key", "In");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オブジェクトの追加
            var onlineCreatedObj = await CreateOnlineObject("key", "In");

            // 一定時間待機
            await Task.Delay(NbObjectSyncManager.PullTimeOffsetSeconds * 1000);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());

            // 物理削除
            await _offlineBucket.DeleteAsync(new NbQuery(), false);

            // オブジェクトの追加
            var onlineCreatedObj2 = await CreateOnlineObject("key", "In");

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            offlineObjects = await GetOfflineObjects();
            var secondSyncObj = offlineObjects.ElementAt(0);
            // 2回目同期分のオブジェクトのみ取得できること
            Assert.AreEqual(1, offlineObjects.Count());

            CompareObjects(secondSyncObj, onlineCreatedObj2);

            // 同期範囲再設定
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            // 更新時刻順で取得
            offlineObjects = await GetOfflineObjects(new NbQuery().OrderBy("updatedAt"));
            Assert.AreEqual(2, offlineObjects.Count());

            var firstOfflineObj = offlineObjects.ElementAt(0);
            CompareObjects(firstOfflineObj, onlineCreatedObj);

            var secondOfflineObj = offlineObjects.ElementAt(1);
            CompareObjects(secondOfflineObj, onlineCreatedObj2);
        }

        // Pullオブジェクト数 --------------------------------------------------

        /// <summary>
        /// Pullオブジェクト数: 0件
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberZero()
        {

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// Pullオブジェクト数: 1件
        /// 新規オブジェクトをサーバに保存すること
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberOneInsert()
        {
            var objects = await ITUtil.CreateOnlineObjects(_onlineBucket, 1);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());

            CompareObjectLists(offlineObjects, objects);
        }

        /// <summary>
        /// Pullオブジェクト数: 分割数-1件
        /// 更新オブジェクトをサーバに保存すること
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberDivideNumberMinusOneUpdate()
        {
            var createdObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, NbObjectSyncManager.PullDivideNumber - 1);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var objects = await ITUtil.UpdateOnlineObjects(_onlineBucket, createdObjects);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(NbObjectSyncManager.PullDivideNumber - 1, offlineObjects.Count());
            CompareObjectLists(offlineObjects, objects);
        }

        /// <summary>
        /// Pullオブジェクト数: 分割数件
        /// 論理削除オブジェクトをサーバに保存すること
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberDivideNumberDelete()
        {
            var createdObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, NbObjectSyncManager.PullDivideNumber);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var objects = await ITUtil.LogicalDeleteOnlineObjects(_onlineBucket, createdObjects);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(NbObjectSyncManager.PullDivideNumber, objects.Count());

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// Pullオブジェクト数: 分割数件+1件
        /// 新規オブジェクトをサーバに保存すること
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberDivideNumberPlusOneInsert()
        {
            var objects = await ITUtil.CreateOnlineObjects(_onlineBucket, NbObjectSyncManager.PullDivideNumber + 1);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(NbObjectSyncManager.PullDivideNumber + 1, offlineObjects.Count());

            CompareObjectLists(offlineObjects, objects);
        }

        /// <summary>
        /// Pullオブジェクト数: 2*分割数 -1件
        /// 更新オブジェクトをサーバに保存すること
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberDoubleDivideNumberMinusOneUpdate()
        {
            var createdObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, 2 * NbObjectSyncManager.PullDivideNumber - 1);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var objects = await ITUtil.UpdateOnlineObjects(_onlineBucket, createdObjects);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(2 * NbObjectSyncManager.PullDivideNumber - 1, offlineObjects.Count());
            CompareObjectLists(offlineObjects, objects);
        }

        /// <summary>
        /// Pullオブジェクト数: 2*分割数件
        /// 論理削除オブジェクトをサーバに保存すること
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberDoubleDivideNumberDelete()
        {
            var createdObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, 2 * NbObjectSyncManager.PullDivideNumber);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var objects = await ITUtil.LogicalDeleteOnlineObjects(_onlineBucket, createdObjects);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(2 * NbObjectSyncManager.PullDivideNumber, objects.Count());

            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// Pullオブジェクト数: 2*分割数 +1件
        /// 新規/更新/論理削除オブジェクトをサーバに保存すること
        /// </summary>
        /// <remarks>分割数は3以上であること</remarks>
        [Test]
        public async void TestSyncBucketAsyncNormalPullNumberDoubleDivideNumberPlusMixed()
        {
            // 総数が2*分割数+1件
            // 更新、論理削除が1/3、残りが新規とする
            var baseNumber = 2 * NbObjectSyncManager.PullDivideNumber + 1;
            var updateNumber = baseNumber / 3;
            var deleteNumber = baseNumber / 3;
            var insertNumber = baseNumber - (updateNumber + deleteNumber);

            // 更新向けオブジェクト生成
            var objectsForUpdate = await ITUtil.CreateOnlineObjects(_onlineBucket, updateNumber);
            var objectsForDelete = await ITUtil.CreateOnlineObjects(_onlineBucket, deleteNumber);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            // 更新、論理削除、新規オブジェクトを生成
            var updatedObjects = await ITUtil.UpdateOnlineObjects(_onlineBucket, objectsForUpdate);
            var deletedObjects = await ITUtil.LogicalDeleteOnlineObjects(_onlineBucket, objectsForDelete);
            var insertedObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, insertNumber);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            // 新規、更新オブジェクトのみが保存されていること
            Assert.AreEqual(insertNumber + updateNumber, offlineObjects.Count());
            CompareObjectLists(offlineObjects, updatedObjects.Concat(insertedObjects));
        }

        // Push オブジェクト検索 --------------------------------------------------

        /// <summary>
        /// 同期状態: Sync
        /// ACL(Read)権限: 有り 
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushSearchStateSyncACLReadable()
        {
            var offlineObj = _offlineBucket.NewObject();
            var saved = await offlineObj.SaveAsync();
            // 同期を行いSync状態とする
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバのオブジェクトの物理削除を行い、Pushが行われた場合失敗とする
            var onlineObj = (await GetOnlineObjects()).First();
            await onlineObj.DeleteAsync(false);

            // ACL.Rに権限有り、Sync状態
            offlineObj = (await GetOfflineObjects()).First();
            Assert.AreEqual(NbSyncState.Sync, offlineObj.SyncState);
            Assert.AreEqual("g:anonymous", offlineObj.Acl.R.First());

            // test
            result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// 同期状態: Sync
        /// ACL(Read)権限: 無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushSearchStateSyncACLUnreadable()
        {
            var user = await ITUtil.SignUpAndLogin();
            Assert.NotNull(user.UserId);

            var offlineObj = _offlineBucket.NewObject();
            // 認証済みユーザのみRead許可するオブジェクトを生成
            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            acl.W.Add("g:anonymous");
            acl.Admin.Add("g:anonymous");
            offlineObj.Acl = acl;
            var saved = await offlineObj.SaveAsync();
            // 同期を行いSync状態とする
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // ACL.Rに権限無し、Sync状態
            offlineObj = (await GetOfflineObjects()).First();
            Assert.AreEqual(NbSyncState.Sync, offlineObj.SyncState);
            Assert.AreEqual("g:authenticated", offlineObj.Acl.R.First());

            // サーバのオブジェクトの物理削除を行い、Pushが行われた場合失敗とする
            var onlineObj = (await GetOnlineObjects()).First();
            await onlineObj.DeleteAsync(false);

            // ログアウトしてRead権限無しとする
            await ITUtil.Logout();

            // test
            result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count);

            // write、Admin権限はあるので、DBにオブジェクトが存在していればConflictが発生する
            try
            {
                await saved.SaveAsync();
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, ex.StatusCode);
            }
        }

        /// <summary>
        /// 同期状態: Dirty
        /// ACL(Read)権限: 有り 
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushSearchStateDirtyACLReadable()
        {
            var offlineObj = _offlineBucket.NewObject();
            var saved = await offlineObj.SaveAsync();

            // ACL.Rに権限有り、Dirty状態
            offlineObj = (await GetOfflineObjects()).First();
            Assert.AreEqual(NbSyncState.Dirty, offlineObj.SyncState);
            Assert.AreEqual("g:anonymous", offlineObj.Acl.R.First());

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count);

            // サーバにオブジェクトが格納されていること
            var onlineObjects = await GetOnlineObjects();
            Assert.AreEqual(1, onlineObjects.Count());
        }

        /// <summary>
        /// 同期状態: Dirty
        /// ACL(Read)権限: 無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushSearchStateDirtyACLUnreadable()
        {
            var user = await ITUtil.SignUpAndLogin();
            Assert.NotNull(user.UserId);

            var offlineObj = _offlineBucket.NewObject();
            // 認証済みユーザのみRead許可するオブジェクトを生成
            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            acl.W.Add("g:anonymous");
            acl.Admin.Add("g:anonymous");
            offlineObj.Acl = acl;
            var saved = await offlineObj.SaveAsync();

            // ACL.Rに権限無し、Dirty状態
            offlineObj = (await GetOfflineObjects()).First();
            Assert.AreEqual(NbSyncState.Dirty, offlineObj.SyncState);
            Assert.AreEqual("g:authenticated", offlineObj.Acl.R.First());

            // ログアウトしてRead権限無しとする
            await ITUtil.Logout();

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count);

            // write、Admin権限はあるので、DBにオブジェクトが存在していればConflictが発生する
            try
            {
                await saved.SaveAsync();
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, ex.StatusCode);
            }

            // サーバにオブジェクトが格納されること(ANTC Redmine #3047)
            ITUtil.UseMasterKey();
            var onlineObjects = await GetOnlineObjects();
            Assert.AreEqual(1, onlineObjects.Count());
            ITUtil.UseNormalKey();
        }

        // Pushオブジェクト数 --------------------------------------------------

        /// <summary>
        /// Pushオブジェクト数: 0件
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberZero()
        {
            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjects = await GetOnlineObjects();
            Assert.AreEqual(0, onlineObjects.Count());
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// Pushオブジェクト数: 1件
        /// 新規オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberOneInsert()
        {
            var offlineObj = await CreateOfflineObject();

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, onlineObjects.Count());

            CompareObjectLists(offlineObjects, onlineObjects);
        }

        /// <summary>
        /// Pushオブジェクト数: 分割数-1件
        /// 更新オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberDivideNumberMinusOneUpdate()
        {
            var onlineObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, NbObjectSyncManager.PushDivideNumber - 1);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            // DBのオブジェクトを更新
            var offlineObjects = await GetOfflineObjects();
            foreach (var obj in offlineObjects)
            {
                await obj.SaveAsync();
            }

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineUpdatedObjects = await GetOnlineObjects();
            var offlineUpdatedObjects = await GetOfflineObjects();

            Assert.AreEqual(NbObjectSyncManager.PushDivideNumber - 1, onlineUpdatedObjects.Count());

            CompareObjectLists(offlineUpdatedObjects, onlineUpdatedObjects);
        }

        /// <summary>
        /// Pushオブジェクト数: 分割数件
        /// 論理削除オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberDivideNumberDelete()
        {
            var onlineObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, NbObjectSyncManager.PushDivideNumber);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            // DBのオブジェクトを論理削除
            var offlineObjects = await GetOfflineObjects();
            foreach (var obj in offlineObjects)
            {
                await obj.DeleteAsync(true);
            }

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineDeletedObjects = await GetOnlineObjects();
            Assert.AreEqual(NbObjectSyncManager.PushDivideNumber, onlineDeletedObjects.Count());
            foreach (var obj in onlineDeletedObjects)
            {
                //　全て論理削除されていること
                Assert.True(obj.Deleted);
            }

            // 論理削除したオブジェクトがDBから削除されていること
            var offlineDeletedObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineDeletedObjects.Count());
        }

        /// <summary>
        /// Pushオブジェクト数: 分割数件+1 件
        /// 新規オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberDivideNumberPlusOneInsert()
        {
            var offlineObjects = await CreateOfflineObjects(_offlineBucket, NbObjectSyncManager.PushDivideNumber + 1);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineInsertedObjects = await GetOnlineObjects();
            var offlineInsertedObjects = await GetOfflineObjects();
            Assert.AreEqual(NbObjectSyncManager.PushDivideNumber + 1, onlineInsertedObjects.Count());

            CompareObjectLists(offlineInsertedObjects, onlineInsertedObjects);
        }

        /// <summary>
        /// Pushオブジェクト数: 2*分割数-1件
        /// 更新オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberDoubleDivideNumberMinusOneUpdate()
        {
            var onlineObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, 2 * NbObjectSyncManager.PushDivideNumber - 1);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            // DBのオブジェクトを更新
            var offlineObjects = await GetOfflineObjects();
            foreach (var obj in offlineObjects)
            {
                await obj.SaveAsync();
            }

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineUpdatedObjects = await GetOnlineObjects();
            var offlineUpdatedObjects = await GetOfflineObjects();
            Assert.AreEqual(2 * NbObjectSyncManager.PushDivideNumber - 1, onlineUpdatedObjects.Count());

            CompareObjectLists(offlineUpdatedObjects, onlineUpdatedObjects);
        }

        /// <summary>
        /// Pushオブジェクト数: 2*分割数件
        /// 論理削除オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberDoubleDivideNumberDelete()
        {
            var onlineObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, 2 * NbObjectSyncManager.PushDivideNumber);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            // DBのオブジェクトを論理削除
            var offlineObjects = await GetOfflineObjects();
            foreach (var obj in offlineObjects)
            {
                await obj.DeleteAsync(true);
            }

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineDeletedObjects = await GetOnlineObjects();
            Assert.AreEqual(2 * NbObjectSyncManager.PushDivideNumber, onlineDeletedObjects.Count());
            foreach (var obj in onlineDeletedObjects)
            {
                //　全て論理削除されていること
                Assert.True(obj.Deleted);
            }

            // 論理削除したオブジェクトがDBから削除されていること
            var offlineDeletedObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineDeletedObjects.Count());
        }

        /// <summary>
        /// Pushオブジェクト数: 2*分割数+1 件
        /// 新規/更新/論理削除オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushNumberDoubleDivideNumberPlusMixed()
        {
            // 総数が2*分割数+1件
            // 更新、論理削除が1/3、残りが新規とする
            var totalNumber = 2 * NbObjectSyncManager.PushDivideNumber + 1;
            var updateNumber = totalNumber / 3;
            var deleteNumber = totalNumber / 3;
            var insertNumber = totalNumber - (updateNumber + deleteNumber);

            // 更新、論理削除向けオブジェクト生成
            var objectsForUpdate = await CreateOfflineObjects(_offlineBucket, updateNumber);
            var objectsForDelete = await CreateOfflineObjects(_offlineBucket, deleteNumber);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjectsBeforeSync = await GetOfflineObjects();

            // LocalDBに更新、論理削除、新規オブジェクトを生成
            var updateObject = GetOfflineObjectsMatchId(offlineObjectsBeforeSync, objectsForUpdate);
            var updatedObjects = await UpdateOfflineObjects(_offlineBucket, updateObject);

            var deleteObject = (GetOfflineObjectsMatchId(offlineObjectsBeforeSync, objectsForUpdate)).ToList();
            var deletedObjects = (await LogicalDeleteOfflineObjects(_offlineBucket, deleteObject)).ToList();

            var insertedObjects = (await CreateOfflineObjects(_offlineBucket, insertNumber)).ToList();

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjectsAfterSync = (await GetOnlineObjects()).ToList();
            var offlineObjectsAfterSync = (await GetOfflineObjects()).ToList();

            // online
            // 新規、更新、論理削除オブジェクトが保存されていること
            Assert.AreEqual(totalNumber, onlineObjectsAfterSync.Count());

            // offline
            // 新規、更新オブジェクトのみが保存されていること
            Assert.AreEqual(insertNumber + updateNumber, offlineObjectsAfterSync.Count());
        }


        // Push処理 --------------------------------------------------

        /// <summary>
        /// ContentACL(Write)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushContentAclUnwritable()
        {
            await ITUtil.CreateOnlineObjects(_onlineBucket, 2);

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();

            var inserted = await CreateOfflineObject(); // 新規
            var updated = await offlineObjects.ElementAt(0).SaveAsync(); // 更新
            await offlineObjects.ElementAt(1).DeleteAsync(true); //論理削除
            var deleted = offlineObjects.ElementAt(1); // 論理削除済み

            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");
            // contentAclのWrite権限削除
            contentAcl.W.Add("g:authenticated");

            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗発生
            Assert.AreEqual(3, result.Count);
            var insertResult = from x in result where x.Id == inserted.Id select x;
            Assert.AreEqual(1, insertResult.Count());
            var updateResult = from x in result where x.Id == updated.Id select x;
            Assert.AreEqual(1, updateResult.Count());
            var deleteResult = from x in result where x.Id == deleted.Id select x;
            Assert.AreEqual(1, deleteResult.Count());

            foreach (var batchResult in result)
            {
                Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, batchResult.Result);
            }
        }

        /// <summary>
        /// ContentACL(Create)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushContentAclUncreatable()
        {
            var inserted = await CreateOfflineObject(); // 新規

            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");
            // contentAclのCreate権限無し

            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗発生
            Assert.AreEqual(1, result.Count);
            var insertResult = from x in result where x.Id == inserted.Id select x;
            Assert.AreEqual(1, insertResult.Count());
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, result[0].Result);
        }

        /// <summary>
        /// ContentACL(Update)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushContentAclUnupdatable()
        {
            await ITUtil.CreateOnlineObjects(_onlineBucket, 1);
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            var updated = await offlineObjects.ElementAt(0).SaveAsync(); // 更新

            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");
            // contentAclのUpdate権限削除
            contentAcl.U.Add("g:authenticated");

            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗発生
            Assert.AreEqual(1, result.Count);
            var updateResult = from x in result where x.Id == updated.Id select x;
            Assert.AreEqual(1, updateResult.Count());
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, result[0].Result);
        }

        /// <summary>
        /// ContentACL(Delete)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushContentAclUndeletable()
        {
            await ITUtil.CreateOnlineObjects(_onlineBucket, 1);
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offlineObjects = await GetOfflineObjects();
            await offlineObjects.ElementAt(0).DeleteAsync(true); //論理削除
            var deleted = offlineObjects.ElementAt(0); // 論理削除済み

            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");
            // contentAclのDelete権限削除
            contentAcl.D.Add("g:authenticated");

            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗発生
            Assert.AreEqual(1, result.Count);
            var deleteResult = from x in result where x.Id == deleted.Id select x;
            Assert.AreEqual(1, deleteResult.Count());
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, result[0].Result);
        }

        /// <summary>
        /// ContentACL(Write)権限無し MasterKey
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushContentAclMasterKey()
        {
            await ITUtil.CreateOnlineObjects(_onlineBucket, 2);
            await _syncManager.SyncBucketAsync(ObjectBucketName);
            var offlineObjects = await GetOfflineObjects();

            var inserted = await CreateOfflineObject(); // 新規
            var updated = await offlineObjects.ElementAt(0).SaveAsync(); // 更新
            await offlineObjects.ElementAt(1).DeleteAsync(true); //論理削除
            var deleted = offlineObjects.ElementAt(1); // 論理削除済み

            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");
            // contentAclのWrite権限削除
            contentAcl.W.Add("g:authenticated");

            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            // test
            ITUtil.UseMasterKey();
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            ITUtil.UseNormalKey();
            // 失敗発生
            Assert.AreEqual(0, result.Count);

            var onlineObjects = await GetOnlineObjects();
            var insertedObjecst = from x in onlineObjects where x.Id == inserted.Id select x;
            Assert.AreEqual(1, insertedObjecst.Count());
            var updatedObjects = from x in onlineObjects where x.Id == updated.Id select x;
            Assert.AreEqual(1, updatedObjects.Count());
            var deletedObjects = from x in onlineObjects where x.Id == deleted.Id select x;
            Assert.AreEqual(1, deletedObjects.Count());
        }

        /// <summary>
        /// ACL(Create)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushAclUncreatable()
        {
            var insert = _offlineBucket.NewObject();
            var acl = new NbAcl();
            // Create権限無し
            acl.R.Add("g:anonymous");
            insert.Acl = acl;
            var saved = await insert.SaveAsync();

            // test
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗無し
            Assert.AreEqual(0, result.Count);

            // 新規作成成功
            var objects = await GetOnlineObjects();
            Assert.AreEqual(1, objects.Count());
            var obj = objects.First();
            Assert.AreEqual(insert.Id, obj.Id);
        }

        /// <summary>
        /// ACL(Update)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushAclUnupdatable()
        {
            var insert = _offlineBucket.NewObject();
            var acl = new NbAcl();
            // Update権限ありの状態でオブジェクトを生成
            acl.R.Add("g:anonymous");
            acl.U.Add("g:anonymous");
            acl.Admin.Add("g:anonymous");
            insert.Acl = acl;
            var saved = await insert.SaveAsync();

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗無し
            Assert.AreEqual(0, results.Count);

            // ローカル側を更新
            var offlineObjects = await GetOfflineObjects();
            var offlineObj = offlineObjects.First();
            offlineObj["key"] = "updated";
            var offlineUpdated = await offlineObj.SaveAsync();

            // オンライン側のACLはUpdate禁止
            var onlineObjects = await GetOnlineObjects();
            var onlineObj = onlineObjects.First();
            onlineObj["key"] = "updateForbidden";
            onlineObj.Acl.U.Remove("g:anonymous");

            var onlineUpdated = await onlineObj.SaveAsync();

            // 衝突はローカル側を優先
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            // 失敗有り(コンフリクト)
            Assert.AreEqual(1, results.Count);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, results.Count);
            var result = results.First();
            Assert.AreEqual(offlineUpdated.Id, result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, result.Result);
        }

        /// <summary>
        /// ACL(Delete)権限無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushAclUndeletable()
        {
            var insert = _offlineBucket.NewObject();
            var acl = new NbAcl();
            // Delete権限ありの状態でオブジェクトを生成
            acl.R.Add("g:anonymous");
            acl.U.Add("g:anonymous");
            acl.D.Add("g:anonymous");
            acl.Admin.Add("g:anonymous");
            insert.Acl = acl;
            var saved = await insert.SaveAsync();

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            // 失敗無し
            Assert.AreEqual(0, results.Count);

            // ローカル側を論理削除
            var offlineObjects = await GetOfflineObjects();
            var offlineObj = offlineObjects.First();
            offlineObj["key"] = "updated";
            await offlineObj.DeleteAsync(true);

            // オンライン側のACLはDelete禁止
            var onlineObjects = await GetOnlineObjects();
            var onlineObj = onlineObjects.First();
            onlineObj["key"] = "deleteForbidden";
            onlineObj.Acl.D.Remove("g:anonymous");
            var onlineUpdated = await onlineObj.SaveAsync();

            // 衝突はローカル側を優先
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            // 失敗有り(コンフリクト)
            Assert.AreEqual(1, results.Count);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, results.Count);
            var result = results.First();
            Assert.AreEqual(offlineObj.Id, result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, result.Result);
        }

        /// <summary>
        /// ACL(Write)権限無し MasterKey
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushAclMasterKey()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            acl.W.Add("g:anonymous");
            acl.Admin.Add("g:anonymous");

            // Delete
            var delete = _offlineBucket.NewObject();
            delete.Acl = acl;
            delete = (NbOfflineObject)await delete.SaveAsync();

            // Update
            var update = _offlineBucket.NewObject();
            update.Acl = acl;
            update = (NbOfflineObject)await update.SaveAsync();

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, results.Count());

            // offline側
            var forDeleteOffline = await _offlineBucket.GetAsync(delete.Id);
            await forDeleteOffline.DeleteAsync();

            var forUpdateOffline = await _offlineBucket.GetAsync(update.Id);
            await forUpdateOffline.SaveAsync();

            // online側
            // W権限を削除
            var forDeleteOnline = await _onlineBucket.GetAsync(delete.Id);
            forDeleteOnline.Acl.W.Remove("g:anonymous");
            await forDeleteOnline.SaveAsync();

            var forUpdateOnline = await _onlineBucket.GetAsync(update.Id);
            forUpdateOnline.Acl.W.Remove("g:anonymous");
            await forUpdateOnline.SaveAsync();

            // conflict発生(クライアント優先)
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(2, results.Count());

            var insert = _offlineBucket.NewObject();
            acl = new NbAcl();
            // Create権限無し
            acl.R.Add("g:anonymous");
            insert.Acl = acl;
            var inserted = await insert.SaveAsync();

            // test
            ITUtil.UseMasterKey();
            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            ITUtil.UseNormalKey();
            // 全て成功
            Assert.AreEqual(0, result.Count);

            var onlineObjects = await GetOnlineObjects();

            var insertedObjecst = from x in onlineObjects where x.Id == inserted.Id select x;
            Assert.AreEqual(1, insertedObjecst.Count());
            var updatedObjects = from x in onlineObjects where x.Id == forUpdateOffline.Id select x;
            Assert.AreEqual(1, updatedObjects.Count());
            var deletedObjects = from x in onlineObjects where x.Id == forDeleteOffline.Id select x;
            Assert.AreEqual(1, deletedObjects.Count());
        }

        /// <summary>
        /// パス不正(テナントID不正)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPushPathInvalidTenantId()
        {
            var service = NbService.GetInstance();

            var onlineObject = await CreateOnlineObject();

            var offlineObjects = await CreateOfflineObjects(_offlineBucket, NbObjectSyncManager.PushDivideNumber * 2 + 1);

            // test
            // awaitで待機しない
            var syncTask = _syncManager.SyncBucketAsync(ObjectBucketName);

            while (true)
            {
                try
                {
                    var pulled = await _offlineBucket.GetAsync(onlineObject.Id);
                    break;
                }
                catch (NbHttpException ex)
                {
                    // Pull完了まで処理継続
                    Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
                }
                // 一定時間待機
                await Task.Delay(100);
            }
            // 不正なTenantIdに変更
            service.TenantId = "12345ZZZZ";

            try
            {
                var result = await syncTask;
                Assert.Fail("unexpectedly success");
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
            }
        }

        /// <summary>
        /// AppKey不正
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPushHttpHeaderInvalidAppId()
        {
            var service = NbService.GetInstance();

            var onlineObject = await CreateOnlineObject();

            var offlineObjects = await CreateOfflineObjects(_offlineBucket, NbObjectSyncManager.PushDivideNumber * 2 + 1);

            // test
            // awaitで待機しない
            var syncTask = _syncManager.SyncBucketAsync(ObjectBucketName);

            while (true)
            {
                try
                {
                    var pulled = await _offlineBucket.GetAsync(onlineObject.Id);
                    break;
                }
                catch (NbHttpException ex)
                {
                    // Pull完了まで処理継続
                    Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
                }
                // 一定時間待機
                await Task.Delay(100);
            }
            // 不正なAppKeyに変更
            service.AppKey = "12345ZZZZ";

            try
            {
                var result = await syncTask;
                Assert.Fail("unexpectedly success");
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, ex.StatusCode);
            }
        }

        // 最終同期時刻 更新 --------------------------------------------------

        /// <summary>
        /// 初回同期: オブジェクト無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushLastSyncTimeFirstSyncNoObject()
        {

            var date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);

            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count());

            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.NotNull(date);

            _syncManager.SetSyncScope(ObjectBucketName);
            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);
        }

        /// <summary>
        /// 初回同期: オブジェクト有り
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushLastSyncTimeFirstSyncObjectExists()
        {

            await CreateOfflineObject();
            await CreateOnlineObject();

            var date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);

            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count());

            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.NotNull(date);

            _syncManager.SetSyncScope(ObjectBucketName);
            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);
        }

        /// <summary>
        /// 初回同期: オブジェクト失敗有り
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushLastSyncTimeFirstSyncFailedObjectExists()
        {
            await CreateOfflineObject();
            var onlineObject = await CreateOnlineObject();

            var acl = NbAcl.CreateAclForAnonymous();
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");
            // contentAclのWrite権限削除
            contentAcl.W.Add("g:authenticated");

            await UpdateObjectBucket(ObjectBucketName, acl, contentAcl);

            var date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);

            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(1, result.Count());

            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);

            _syncManager.SetSyncScope(ObjectBucketName);
            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);
        }

        /// <summary>
        /// 2回目同期: オブジェクト無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushLastSyncTimeSecondSyncNoObject()
        {
            var date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);

            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count());

            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.NotNull(date);

            result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count());

            var dateAfterSync = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.AreNotEqual(date, dateAfterSync);

            _syncManager.SetSyncScope(ObjectBucketName);
            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);
        }

        /// <summary>
        /// 2回目同期: オブジェクト有り
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushLastSyncTimeSecodSyncObjectExists()
        {
            await CreateOfflineObject();
            await CreateOnlineObject();

            var date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);

            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count());

            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.NotNull(date);

            await CreateOfflineObject();
            await CreateOnlineObject();

            result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count());

            var dateAfterSync = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.AreNotEqual(date, dateAfterSync);

            _syncManager.SetSyncScope(ObjectBucketName);
            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);
        }

        /// <summary>
        /// 2回目同期: オブジェクト失敗有り
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushLastSyncTimeSecondSyncFailedObjectExists()
        {
            var offlineObject = await CreateOfflineObject("key", "value");
            var onlineObject = await CreateOnlineObject("key", "value");

            _syncManager.SetSyncScope(ObjectBucketName, new NbQuery().EqualTo("key", "value2"));

            var date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);

            var result = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, result.Count());

            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNotNull(date);

            // 競合条件生成
            // online側更新
            var savedOnlineObject = await _onlineBucket.GetAsync(offlineObject.Id);
            await savedOnlineObject.DeleteAsync();
            // offline側更新
            var savedOfflineObject = await _offlineBucket.GetAsync(offlineObject.Id);
            await savedOfflineObject.SaveAsync();

            await CreateOfflineObject();
            await CreateOnlineObject();

            result = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, result.Count());

            var dateAfterSync = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.AreEqual(date, dateAfterSync);

            _syncManager.SetSyncScope(ObjectBucketName);
            date = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(date);
        }

        // Push同期範囲 --------------------------------------------------

        /// <summary>
        /// 同期範囲外オブジェクト
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushSyncScopeOutOfScope()
        {
            var obj = _offlineBucket.NewObject();
            var saved = await obj.SaveAsync();

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, results.Count());

            var query = new NbQuery().EqualTo("key", "value");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var syncedOffline = await _offlineBucket.GetAsync(saved.Id);
            syncedOffline["key"] = "value1";
            syncedOffline["key1"] = "client";
            var syncedOfflineSaved = await syncedOffline.SaveAsync();

            var syncedOnline = await _onlineBucket.GetAsync(saved.Id);
            syncedOnline["key"] = "value1";
            syncedOnline["key1"] = "server";
            var syncedOnlineSaved = await syncedOnline.SaveAsync();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            Assert.AreEqual(1, results.Count());
            var result = results.First();
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, result.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.EtagMismatch, result.Reason);
            // Pushでコンフリクト発生し、次回
            syncedOnline = await _onlineBucket.GetAsync(saved.Id);
            syncedOffline = await _offlineBucket.GetAsync(saved.Id);
            Assert.AreEqual(syncedOnline.Etag, syncedOffline.Etag);
            Assert.AreEqual(syncedOnline["key"], syncedOffline["key"]);
            Assert.AreNotEqual(syncedOnline["key1"], syncedOffline["key1"]);
            Assert.AreEqual("client", syncedOffline["key1"]);

            // 再度同期でPushが行われClientデータで更新されること
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            Assert.AreEqual(0, results.Count());
            syncedOnline = await _onlineBucket.GetAsync(saved.Id);
            syncedOffline = await _offlineBucket.GetAsync(saved.Id);
            Assert.AreEqual(syncedOnline.Etag, syncedOffline.Etag);
            Assert.AreEqual(syncedOnline["key"], syncedOffline["key"]);
            Assert.AreEqual(syncedOnline["key1"], syncedOffline["key1"]);
            Assert.AreEqual("client", syncedOffline["key1"]);
        }

        // 同期範囲設定 --------------------------------------------------

        /// <summary>
        /// バケット名null
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetSyncScopeExceptionNoBucketName()
        {
            _syncManager.SetSyncScope(null, new NbQuery());
        }

        /// <summary>
        /// クエリ指定有り
        /// 同期範囲設定済み
        /// </summary>
        [Test]
        public void TestSetSyncScopeNormalSyncScopeExists()
        {
            _syncManager.SetSyncScope(ObjectBucketName, new NbQuery());

            var query = new NbQuery().EqualTo("key", "value");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.AreEqual(query.ToString(), resultQuery.ToString());
        }

        /// <summary>
        /// クエリ指定有り
        /// 同期範囲未設定
        /// バケットキャッシュ無し
        /// </summary>
        [Test]
        public void TestSetSyncScopeNormalNoBucketCache()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            var query = new NbQuery().EqualTo("key", "value");

            _syncManager.SetSyncScope(ObjectBucketName, query);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);

            Assert.AreEqual(query.ToString(), resultQuery.ToString());
        }

        /// <summary>
        /// クエリ指定無し
        /// 同期範囲設定済み
        /// </summary>
        [Test]
        public void TestSetSyncScopeNormalNoQueryArgumentSyncScopeExists()
        {
            var query = new NbQuery().EqualTo("key", "value");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            _syncManager.SetSyncScope(ObjectBucketName);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.AreEqual(new NbQuery().ToString(), resultQuery.ToString());
        }

        /// <summary>
        /// クエリ指定無し
        /// 同期範囲未設定
        /// バケットキャッシュ無し
        /// </summary>
        [Test]
        public void TestSetSyncScopeNormalNoQueryArgumentNoSyncScope()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            _syncManager.SetSyncScope(ObjectBucketName);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);

            Assert.AreEqual(new NbQuery().ToString(), resultQuery.ToString());
        }

        /// <summary>
        /// クエリ指定null
        /// 同期範囲設定済み
        /// </summary>
        [Test]
        public void TestSetSyncScopeNormalNullQueryArgumentSyncScopeExists()
        {
            var query = new NbQuery().EqualTo("key", "value");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            _syncManager.SetSyncScope(ObjectBucketName, null);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.AreEqual(new NbQuery().ToString(), resultQuery.ToString());
        }

        // 同期範囲取得 --------------------------------------------------

        /// <summary>
        /// バケット名null
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetSyncScopeExceptionNoBucketName()
        {
            _syncManager.GetSyncScope(null);
        }

        /// <summary>
        /// 同期範囲設定済み
        /// </summary>
        [Test]
        public void TestGetSyncScopeNormalSyncScopeExists()
        {
            var query = new NbQuery().EqualTo("key", "value");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);

            Assert.AreEqual(query.ToString(), resultQuery.ToString());
        }

        /// <summary>
        /// 同期範囲未設定
        /// バケットキャッシュ有
        /// </summary>
        [Test]
        public void TestGetSyncScopeNormalNoSyncScope()
        {
            NbOfflineService.DeleteCacheAll().Wait();
            // 別バケットのキャッシュを生成
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>("bucket");

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.IsNull(resultQuery);
        }

        /// <summary>
        /// 同期範囲未設定
        /// バケットキャッシュ無
        /// </summary>
        [Test]
        public void TestGetSyncScopeNormalNoSyncScopeNoBucketCache()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.IsNull(resultQuery);
        }

        // 同期範囲削除 --------------------------------------------------

        /// <summary>
        /// バケット名null
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRemoveSyncScopeExceptionNoBucketName()
        {
            _syncManager.RemoveSyncScope(null);
        }

        /// <summary>
        /// 同期範囲設定済み
        /// </summary>
        [Test]
        public void TestRemoveSyncScopeNormalSyncScopeExists()
        {
            var query = new NbQuery().EqualTo("key", "value");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            _syncManager.RemoveSyncScope(ObjectBucketName);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.IsNull(resultQuery);
        }

        /// <summary>
        /// 同期範囲未設定
        /// バケットキャッシュ有
        /// </summary>
        [Test]
        public void TestRemoveSyncScopeNormalNoBucketNameSyncScopeExist()
        {
            NbOfflineService.DeleteCacheAll().Wait();
            // 別バケットのキャッシュを生成
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>("bucket");

            _syncManager.RemoveSyncScope(ObjectBucketName);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.IsNull(resultQuery);
        }

        /// <summary>
        /// 同期範囲未設定
        /// バケットキャッシュ無
        /// </summary>
        [Test]
        public void TestRemoveSyncScopeNormalNoSyncScopeNoBucketCache()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            _syncManager.RemoveSyncScope(ObjectBucketName);

            var resultQuery = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.IsNull(resultQuery);
        }

        // 同期範囲一括取得 --------------------------------------------------

        /// <summary>
        /// 同期設定件数(2件)
        /// </summary>
        [Test]
        public void TestGetAllSyncScopesNormalTwoSyncScopes()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            var bucketA = "bucketA";
            var bucketB = "bucketB";

            var queryA = new NbQuery().LessThan("key", 1);
            var queryB = new NbQuery().GreaterThan("key", 2);

            _syncManager.SetSyncScope(bucketA, queryA);
            _syncManager.SetSyncScope(bucketB, queryB);

            var queries = _syncManager.GetAllSyncScopes();
            Assert.AreEqual(2, queries.Count);

            var resultA = queries[bucketA];
            Assert.AreEqual(queryA.ToString(), resultA.ToString());
            var resultB = queries[bucketB];
            Assert.AreEqual(queryB.ToString(), resultB.ToString());
        }

        /// <summary>
        /// 同期設定件数(1件)
        /// </summary>
        [Test]
        public void TestGetAllSyncScopesNormalASyncScope()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            var query = new NbQuery().EqualTo("key", "value");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var queries = _syncManager.GetAllSyncScopes();
            Assert.AreEqual(1, queries.Count);

            var result = queries[ObjectBucketName];
            Assert.AreEqual(query.ToString(), result.ToString());
        }

        /// <summary>
        /// 同期設定件数　無し
        /// バケットキャッシュ有
        /// </summary>
        [Test]
        public void TestGetAllSyncScopesNormalNoSyncScope()
        {
            NbOfflineService.DeleteCacheAll().Wait();
            // 別バケットのキャッシュを生成
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>("bucket");

            var queries = _syncManager.GetAllSyncScopes();
            Assert.AreEqual(0, queries.Count);
        }

        /// <summary>
        /// 同期設定件数　無し
        /// バケットキャッシュ無
        /// </summary>
        [Test]
        public void TestGetAllSyncScopesNormalNoBucketCache()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            var queries = _syncManager.GetAllSyncScopes();
            Assert.AreEqual(0, queries.Count);
        }

        // 最終同期時刻 --------------------------------------------------

        /// <summary>
        /// バケット名null
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetLastSyncTimeExceptionNoBucketName()
        {
            _syncManager.GetLastSyncTime(null);
        }

        /// <summary>
        /// 時刻情報保存済み
        /// </summary>
        [Test]
        public async void TestGetLastSyncTimeNormalTimeExists()
        {
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var result = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.NotNull(result);
        }

        /// <summary>
        /// 時刻情報未保存
        /// バケットキャッシュ有り
        /// </summary>
        [Test]
        public void TestGetLastSyncTimeNormalNoTimeSaved()
        {
            NbOfflineService.DeleteCacheAll().Wait();
            // 別バケットのキャッシュを生成
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>("bucket");

            var result = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(result);
        }

        /// <summary>
        /// 時刻情報未保存
        /// バケットキャッシュ無し
        /// </summary>
        [Test]
        public void TestGetLastSyncTimeNormalNoBucketCache()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            var result = _syncManager.GetLastSyncTime(ObjectBucketName);
            Assert.IsNull(result);
        }

        // バケットキャッシュ削除 --------------------------------------------------

        /// <summary>
        /// 同期範囲設定有り
        /// </summary>
        [Test]
        public void TestGetSyncScopeNormalBucketCacheDeleteSyncScopeExsists()
        {
            _syncManager.SetSyncScope(ObjectBucketName, new NbQuery());

            NbOfflineService.DeleteCacheAll().Wait();

            var result = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.IsNull(result);
        }

        /// <summary>
        /// 同期範囲設定無し
        /// </summary>
        [Test]
        public void TestGetSyncScopeNormalBucketCacheDeleteNoSyncScope()
        {
            NbOfflineService.DeleteCacheAll().Wait();

            var result = _syncManager.GetSyncScope(ObjectBucketName);
            Assert.IsNull(result);
        }

        // Pull状態遷移 --------------------------------------------------

        /// <summary>
        /// サーバオブジェクト無し
        /// クライアントオブジェクト無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateNoServerNoClient()
        {
            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();

            Assert.AreEqual(0, onlineObjects.Count());
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// 新規
        /// クライアントオブジェクト無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateInsertNoClient()
        {
            var createObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, 1);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();

            Assert.AreEqual(1, onlineObjects.Count());
            Assert.AreEqual(1, offlineObjects.Count());

            var onlineObj = createObjects.First();
            var offlineObj = offlineObjects.First();

            CompareObjects(offlineObj, onlineObj);
            Assert.AreEqual(NbSyncState.Sync, offlineObj.SyncState);
        }

        /// <summary>
        /// Pull重複
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateInsertSameEtag()
        {
            var createObjects = await ITUtil.CreateOnlineObjects(_onlineBucket, 1);
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();

            var onlineObj = onlineObjects.First();
            var offlineObj = offlineObjects.First();
            CompareObjectLists(onlineObjects, offlineObjects);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjectsAfterSync = await GetOnlineObjects();
            var offlineObjectsAfterSync = await GetOfflineObjects();

            Assert.AreEqual(1, onlineObjects.Count());
            Assert.AreEqual(1, offlineObjects.Count());

            var onlineObjAfterSync = onlineObjectsAfterSync.First();
            var offlineObjAfterSync = offlineObjectsAfterSync.First();

            // 同期の前後で変化が無いこと
            CompareObjects(offlineObjAfterSync, offlineObj);
            CompareObjects(offlineObjAfterSync, onlineObjAfterSync);
            Assert.AreEqual(NbSyncState.Sync, offlineObjAfterSync.SyncState);
        }

        /// <summary>
        /// 更新-新規衝突(サーバ優先)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateInsertUpdateInsertConflictPreferServer()
        {
            var objectId = await CreateSameIdObject();

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            var offline = await _offlineBucket.GetAsync(objectId);
            var online = await _onlineBucket.GetAsync(objectId);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Sync, offline.SyncState);
            Assert.AreEqual(online.Etag, offline.Etag);
            Assert.AreEqual("server", offline["key"]);
        }

        /// <summary>
        /// 更新-新規衝突(クライアント優先)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateInsertUpdateInsertConflictPreferClient()
        {
            var objectId = await CreateSameIdObject();

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            var offline = await _offlineBucket.GetAsync(objectId);
            var online = await _onlineBucket.GetAsync(objectId);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Dirty, offline.SyncState);
            Assert.AreEqual(online.Etag, offline.Etag);
            Assert.AreEqual("client", offline["key"]);
        }

        /// <summary>
        /// Pull重複(ETag同一のためクライアント変更せず)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateInsertSameEtagClientDirty()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // クライアント側更新
            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client";
            await offline.SaveAsync();

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            offline = await _offlineBucket.GetAsync(online.Id);
            online = await _onlineBucket.GetAsync(online.Id);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Dirty, offline.SyncState);
            Assert.AreEqual(online.Etag, offline.Etag);
            Assert.AreEqual("client", offline["key"]);
        }

        /// <summary>
        /// Pull重複(ETag同一のためクライアント変更せず)
        /// 論理削除
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateInsertSameEtagClientDirtyLogicalDelete()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // オフライン側論理削除
            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client";
            offline = (NbOfflineObject)await offline.SaveAsync();
            await offline.DeleteAsync(true); // 論理削除

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            var offlineObjects = await GetOfflineObjects();
            offline = offlineObjects.First();
            online = await _onlineBucket.GetAsync(online.Id);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Dirty, offline.SyncState);
            Assert.AreEqual(online.Etag, offline.Etag);
            Assert.AreEqual("client", offline["key"]);
            Assert.AreEqual(true, offline.Deleted);
        }

        /// <summary>
        /// Pull (UPDATE)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateUpdate()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側を更新
            online["key"] = "server2";
            await online.SaveAsync();

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            var offline = await _offlineBucket.GetAsync(online.Id);
            var onlineUpdated = await _onlineBucket.GetAsync(online.Id);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Sync, offline.SyncState);
            Assert.AreNotEqual(online.Etag, offline.Etag);
            Assert.AreEqual(onlineUpdated.Etag, offline.Etag);
            Assert.AreEqual("server2", offline["key"]);
        }

        /// <summary>
        /// 更新衝突
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateUpdateUpdateUpdateConflictPreferServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側更新
            online["key"] = "server2";
            await online.SaveAsync();

            // クライアント側更新
            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client2";
            await offline.SaveAsync();

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            var offlineUpdated = await _offlineBucket.GetAsync(online.Id);
            var onlineUpdated = await _onlineBucket.GetAsync(online.Id);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Sync, offlineUpdated.SyncState);
            Assert.AreNotEqual(online.Etag, offlineUpdated.Etag);
            Assert.AreEqual(onlineUpdated.Etag, offlineUpdated.Etag);
            Assert.AreEqual("server2", offlineUpdated["key"]);
        }

        /// <summary>
        /// 更新衝突
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateUpdateUpdateUpdateConflictPreferClient()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側を更新
            online["key"] = "server2";
            await online.SaveAsync();

            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client2";
            await offline.SaveAsync();

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            var offlineUpdated = await _offlineBucket.GetAsync(online.Id);
            var onlineUpdated = await _onlineBucket.GetAsync(online.Id);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Dirty, offlineUpdated.SyncState);
            Assert.AreNotEqual(online.Etag, offlineUpdated.Etag);
            Assert.AreEqual(onlineUpdated.Etag, offlineUpdated.Etag);
            Assert.AreEqual("client2", offlineUpdated["key"]);
        }

        /// <summary>
        /// 更新-削除衝突
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateUpdateUpdateLogicalDeleteConflictPreferServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側更新
            online["key"] = "server2";
            await online.SaveAsync();

            // クライアント側更新
            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client2";
            offline = (NbOfflineObject)await offline.SaveAsync();
            await offline.DeleteAsync(true); // 論理削除

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            var offlineUpdated = await _offlineBucket.GetAsync(online.Id);
            var onlineUpdated = await _onlineBucket.GetAsync(online.Id);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Sync, offlineUpdated.SyncState);
            Assert.AreNotEqual(online.Etag, offlineUpdated.Etag);
            Assert.AreEqual(onlineUpdated.Etag, offlineUpdated.Etag);
            Assert.AreEqual("server2", offlineUpdated["key"]);
            Assert.AreEqual(false, offlineUpdated.Deleted);
        }

        /// <summary>
        /// 更新-削除衝突
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateUpdateUpdateLogicalDeleteConflictPreferClient()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側を更新
            online["key"] = "server2";
            await online.SaveAsync();

            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client2";
            offline = (NbOfflineObject)await offline.SaveAsync();
            await offline.DeleteAsync(true); // 論理削除

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            var offlineObjects = await GetOfflineObjects();
            var offlineUpdated = offlineObjects.First();
            var onlineUpdated = await _onlineBucket.GetAsync(online.Id);

            // オフラインオブジェクト
            Assert.AreEqual(NbSyncState.Dirty, offlineUpdated.SyncState);
            Assert.AreNotEqual(online.Etag, offlineUpdated.Etag);
            Assert.AreEqual(onlineUpdated.Etag, offlineUpdated.Etag);
            Assert.AreEqual("client2", offlineUpdated["key"]);
            Assert.AreEqual(true, offlineUpdated.Deleted);
        }

        /// <summary>
        /// Pull (DELETE)
        /// クライアントデータ無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateDeleteNoClient()
        {
            var online = await CreateOnlineObject("key", "server");

            // サーバ側更新
            await online.DeleteAsync(true); // 論理削除

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            var offlineObjects = await GetOfflineObjects();
            // オフラインオブジェクト
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// Pull (DELETE)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateDelete()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側論理削除
            await online.DeleteAsync(true);

            // クライアント側更新無し
            var offline = await _offlineBucket.GetAsync(online.Id);

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // オフラインオブジェクト
            var offlineObjects = await _offlineBucket.QueryAsync(new NbQuery().DeleteMark(true));
            // 物理削除されていること
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// 削除-更新衝突
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateDeleteLogicalDeleteUpdateConflictPreferServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側論理削除
            await online.DeleteAsync(true);

            // クライアント側更
            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client";
            offline = (NbOfflineObject)await offline.SaveAsync();

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            // オフラインオブジェクト
            var offlineObjects = await GetOfflineObjects();
            // 物理削除されていること
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// 削除-更新衝突
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateDeleteLogicalDeleteUpdateConflictPreferClient()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側論理削除
            await online.DeleteAsync(true);

            // クライアント側論理削除
            var offline = await _offlineBucket.GetAsync(online.Id);
            offline["key"] = "client";
            offline = (NbOfflineObject)await offline.SaveAsync();

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            // オフラインオブジェクト
            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, offlineObjects.Count());

            var onlineDeleted = onlineObjects.First();
            var offlineUpdated = offlineObjects.First();

            Assert.AreEqual(NbSyncState.Dirty, offlineUpdated.SyncState);
            Assert.AreEqual(onlineDeleted.Etag, offlineUpdated.Etag);
            Assert.AreEqual("client", offlineUpdated["key"]);
            Assert.AreEqual(false, offlineUpdated.Deleted);
        }

        /// <summary>
        /// 削除-削除衝突
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPullStateDeleteLogicalDeleteLogicalDeleteConflict()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバ側論理削除
            await online.DeleteAsync(true);

            // クライアント側論理削除
            var offline = await _offlineBucket.GetAsync(online.Id);
            await offline.DeleteAsync(true);

            // Pull時の状態を保存するためReadOnlyとする
            await UpdateContentAclReadOnly(ObjectBucketName);

            // test
            results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // オフラインオブジェクト
            var offlineObjects = await GetOfflineObjects();

            // 物理削除されていること
            Assert.AreEqual(0, offlineObjects.Count());
        }

        // Push状態遷移 --------------------------------------------------

        /// <summary>
        /// サーバ Create
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateInsertNoServer()
        {
            var offline = await CreateOfflineObject("key", "client");

            await _syncManager.SyncBucketAsync(ObjectBucketName);

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, onlineObjects.Count());

            var online = onlineObjects.First();
            offline = offlineObjects.First();

            Assert.AreEqual(NbSyncState.Sync, offline.SyncState);
            Assert.AreEqual(online.Etag, offline.Etag);
            Assert.AreEqual("client", online["key"]);
        }

        /// <summary>
        /// ObjectID衝突
        /// クライアント: データ有 dirty
        /// サーバ: データ有
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateInsertConflictPreferServer()
        {
            var objectId = await CreateSameIdObject();
            var onlineBeforeSync = await _onlineBucket.GetAsync(objectId);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(1, results.Count());
            var result = results.First();
            Assert.AreEqual(objectId, result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, result.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.DuplicateId, result.Reason);

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, onlineObjects.Count());

            var online = onlineObjects.First();
            var offline = offlineObjects.First();

            Assert.AreEqual(NbSyncState.Dirty, offline.SyncState);
            Assert.IsNull(offline.Etag);
            Assert.AreEqual("client", offline["key"]);

            Assert.AreEqual(onlineBeforeSync.Etag, online.Etag);
            Assert.AreEqual("server", online["key"]);
        }

        /// <summary>
        /// ObjectID衝突
        /// クライアント: データ有 dirty
        /// サーバ: データ有
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateInsertConflictPreferClient()
        {
            var objectId = await CreateSameIdObject();
            var onlineBeforeSync = await _onlineBucket.GetAsync(objectId);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, results.Count());

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, onlineObjects.Count());

            var online = onlineObjects.First();
            var offline = offlineObjects.First();

            Assert.AreEqual(NbSyncState.Dirty, offline.SyncState);
            Assert.IsNull(offline.Etag);
            Assert.AreEqual("client", offline["key"]);

            Assert.AreEqual(onlineBeforeSync.Etag, online.Etag);
            Assert.AreEqual("server", online["key"]);
        }

        /// <summary>
        /// ObjectID衝突
        /// クライアント: データ有 dirty
        /// サーバ: データ有(論理削除)
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateInsertLogicalDeletedConflictPreferServer()
        {
            var objectId = await CreateSameIdObject();

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // サーバのオブジェクトを論理削除する
            var onlineDelete = await _onlineBucket.GetAsync(objectId);
            await onlineDelete.DeleteAsync(true);
            var onlineBeforeSync = (await GetOnlineObjects()).First();

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(1, results.Count());

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, onlineObjects.Count());

            var online = onlineObjects.First();
            var offline = offlineObjects.First();

            Assert.AreEqual(NbSyncState.Dirty, offline.SyncState);
            Assert.IsNull(offline.Etag);
            Assert.AreEqual("client", offline["key"]);

            Assert.AreEqual(onlineBeforeSync.Etag, online.Etag);
            Assert.AreEqual("server", online["key"]);
        }

        /// <summary>
        /// ObjectID衝突
        /// クライアント: データ有 dirty
        /// サーバ: データ有(論理削除)
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateInsertLogicalDeletedConflictPreferClient()
        {
            var objectId = await CreateSameIdObject();

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // サーバのオブジェクトを論理削除する
            var onlineDelete = await _onlineBucket.GetAsync(objectId);
            await onlineDelete.DeleteAsync(true);
            var onlineBeforeSync = (await GetOnlineObjects()).First();

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, results.Count());

            var onlineObjects = await GetOnlineObjects();
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(1, onlineObjects.Count());

            var online = onlineObjects.First();
            var offline = offlineObjects.First();

            Assert.AreEqual(NbSyncState.Dirty, offline.SyncState);
            Assert.IsNull(offline.Etag);
            Assert.AreEqual("client", offline["key"]);

            Assert.AreEqual(onlineBeforeSync.Etag, online.Etag);
            Assert.AreEqual("server", online["key"]);
        }

        /// <summary>
        /// サーバ消失 
        /// クライアント: 更新あり
        /// サーバ: データ無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateUpdateNoServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // 物理削除
            var onlineObject = (await GetOnlineObjects()).First();
            await onlineObject.DeleteAsync(false);

            // 更新
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(1, results.Count());

            // 失敗として扱うこと(ANTC Redmine #2996)
            var result = results.First();
            Assert.AreEqual(online.Id, result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.NotFound, result.Result);

            // 更新が発生しないこと(ANTC Redmine #2996)
            Assert.AreEqual(NbSyncState.Dirty, offlineObject.SyncState);
            Assert.AreEqual(onlineObject.Etag, offlineObject.Etag);
            Assert.AreEqual("client", offlineObject["key"]);
        }

        /// <summary>
        /// サーバ Update
        /// クライアント: 更新あり
        /// サーバ: 更新なし
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateUpdate()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // 更新
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, results.Count());

            var onlineSynced = (await GetOnlineObjects()).First();
            var offlineSynced = (await GetOfflineObjects()).First();

            // クライアント側オブジェクトで更新
            Assert.AreEqual(NbSyncState.Sync, offlineSynced.SyncState);
            Assert.AreNotEqual(offlineObject.Etag, offlineSynced.Etag);
            Assert.AreEqual(offlineSynced.Etag, onlineSynced.Etag);
            Assert.AreEqual("client", onlineSynced["key"]);
        }

        /// <summary>
        /// 更新-更新衝突
        /// クライアント: 更新あり
        /// サーバ: 更新あり
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateUpdatePreferServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();

            // オンライン更新
            var onlineObject = (await GetOnlineObjects()).First();
            onlineObject["key"] = "server2";
            onlineObject = await onlineObject.SaveAsync();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, results.Count());

            var onlineSynced = (await GetOnlineObjects()).First();
            var offlineSynced = (await GetOfflineObjects()).First();

            // サーバ側オブジェクトで更新
            Assert.AreEqual(NbSyncState.Sync, offlineSynced.SyncState);
            Assert.AreNotEqual(offlineObject.Etag, offlineSynced.Etag);
            Assert.AreEqual(offlineSynced.Etag, onlineSynced.Etag);
            Assert.AreEqual("server2", onlineSynced["key"]);
        }

        /// <summary>
        /// 更新-更新衝突
        /// クライアント: 更新あり
        /// サーバ: 更新あり
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateUpdatePreferClient()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();

            // オンライン更新
            var onlineObject = (await GetOnlineObjects()).First();
            onlineObject["key"] = "server2";
            onlineObject = await onlineObject.SaveAsync();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, results.Count());
            var result = results.First();
            Assert.AreEqual(online.Id, result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, result.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.EtagMismatch, result.Reason);

            var onlineSynced = (await GetOnlineObjects()).First();
            var offlineSynced = (await GetOfflineObjects()).First();

            // クライアント側オブジェクトで更新
            Assert.AreEqual(NbSyncState.Dirty, offlineSynced.SyncState);
            Assert.AreNotEqual(offlineObject.Etag, offlineSynced.Etag);
            Assert.AreEqual(offlineSynced.Etag, onlineSynced.Etag);
            Assert.AreEqual("server2", onlineSynced["key"]);
            Assert.AreEqual("client", offlineSynced["key"]);
        }

        /// <summary>
        /// 削除復帰
        /// クライアント: 更新無し
        /// サーバ: 更新無し(論理削除)
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateUpdateLogicalDelete()
        {
            var offline = _offlineBucket.NewObject();
            offline["key"] = "client";
            offline = (NbOfflineObject)await offline.SaveAsync();

            // オフラインのIDでサーバにオブジェクトを生成
            var json = new NbJsonObject();
            json[Field.Id] = offline.Id;
            json["key"] = "server";
            var req = CreateObjectRequest(ObjectBucketName, HttpMethod.Post);
            req.SetJsonBody(json);
            var rjson = await _service.RestExecutor.ExecuteRequestForJson(req);
            var online = new NbObject(ObjectBucketName, rjson);

            // オンライン更新(論理削除)
            var onlineObject = (await GetOnlineObjects()).First();
            onlineObject["key"] = "server2";
            await onlineObject.DeleteAsync(true);
            var onlineObjectDeleted = (await GetOnlineObjects()).First();

            // オフラインオブジェクトのEtagを論理削除済みオブジェクトと一致させる
            offline.Etag = onlineObjectDeleted.Etag; // internalプロパティを使用
            offline = (NbOfflineObject)await offline.SaveAsync();

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, results.Count());

            var onlineSynced = (await GetOnlineObjects()).First();
            var offlineSynced = (await GetOfflineObjects()).First();

            // クライアント側オブジェクトで更新
            Assert.AreEqual(NbSyncState.Sync, offlineSynced.SyncState);
            Assert.AreNotEqual(offline.Etag, offlineSynced.Etag);
            Assert.AreEqual(offlineSynced.Etag, onlineSynced.Etag);
            Assert.AreEqual("client", onlineSynced["key"]);
            Assert.AreEqual("client", offlineSynced["key"]);
            Assert.AreEqual(false, onlineSynced.Deleted);
            Assert.AreEqual(false, offlineSynced.Deleted);
        }

        /// <summary>
        /// 更新-削除衝突
        /// クライアント: 更新あり
        /// サーバ: 更新あり(論理削除)
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateUpdateLogicalDeletePreferServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();

            // オンライン更新(論理削除)
            var onlineObject = (await GetOnlineObjects()).First();
            await onlineObject.DeleteAsync(true);
            onlineObject = (await GetOnlineObjects()).First();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, results.Count());

            var onlineSynced = (await GetOnlineObjects()).First();
            // 削除されていること
            var offlineSynced = await GetOfflineObjects();
            Assert.AreEqual(0, offlineSynced.Count());

            // サーバ側のオブジェクトが更新されていないこと
            Assert.AreEqual(onlineObject.Etag, onlineSynced.Etag);
            Assert.AreEqual("server", onlineSynced["key"]);
            Assert.AreEqual(true, onlineSynced.Deleted);
        }

        /// <summary>
        /// 更新-削除衝突
        /// クライアント: 更新あり
        /// サーバ: 更新あり(論理削除)
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateUpdateLogicalDeletePreferClient()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();

            // オンライン更新(論理削除)
            var onlineObject = (await GetOnlineObjects()).First();
            await onlineObject.DeleteAsync(true);
            onlineObject = (await GetOnlineObjects()).First();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, results.Count());
            var result = results.First();
            Assert.AreEqual(online.Id, result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, result.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.EtagMismatch, result.Reason);

            var onlineSynced = (await GetOnlineObjects()).First();
            var offlineSynced = (await GetOfflineObjects()).First();

            // クライアント側オブジェクトで更新
            Assert.AreEqual(NbSyncState.Dirty, offlineSynced.SyncState);
            Assert.AreNotEqual(offlineObject.Etag, offlineSynced.Etag);
            Assert.AreEqual(offlineSynced.Etag, onlineSynced.Etag);
            Assert.AreEqual("server", onlineSynced["key"]);
            Assert.AreEqual("client", offlineSynced["key"]);
            Assert.AreEqual(true, onlineSynced.Deleted);
            Assert.AreEqual(false, offlineSynced.Deleted);
        }

        /// <summary>
        /// 削除 (サーバで物理削除済み)
        /// クライアント: 更新あり(論理削除)
        /// サーバ: 物理削除
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateLogicalDeleteNoServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新(論理削除)
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            await offlineObject.DeleteAsync(true);
            offlineObject = (await GetOfflineObjects()).First();
            // オンライン更新(物理削除)
            var onlineObject = (await GetOnlineObjects()).First();
            await onlineObject.DeleteAsync(false);

            results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, results.Count());

            // サーバ,クライアント共に物理削除されていること
            var onlineObjects = await GetOnlineObjects();
            Assert.AreEqual(0, onlineObjects.Count());
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        /// <summary>
        /// サーバ Delete
        /// クライアント: 更新あり(論理削除)
        /// サーバ: 更新無し
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateLogicalDelete()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新(論理削除)
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            await offlineObject.DeleteAsync(true);
            offlineObject = (await GetOfflineObjects()).First();

            var onlineObject = (await GetOnlineObjects()).First();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, results.Count());

            // サーバ側は論理削除されること
            var onlineObjectSynced = (await GetOnlineObjects()).First();
            Assert.AreNotEqual(onlineObject.Etag, onlineObjectSynced.Etag); // Etagが更新されること
            Assert.AreEqual(true, onlineObjectSynced.Deleted);

            // クライアント側は物理削除されていること
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }


        /// <summary>
        /// 削除-更新衝突
        /// クライアント: 更新あり(論理削除)
        /// サーバ: 更新有り
        /// サーバ優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateLogicalDeleteServerUpdatePreferServer()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新(論理削除)
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            await offlineObject.DeleteAsync(true);
            offlineObject = (await GetOfflineObjects()).First();

            // オンライン更新
            var onlineObject = (await GetOnlineObjects()).First();
            onlineObject["key"] = "server2";
            onlineObject = await onlineObject.SaveAsync();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, results.Count());

            // サーバ側は更新が無いこと
            var onlineObjectSynced = (await GetOnlineObjects()).First();
            Assert.AreEqual(onlineObject.Etag, onlineObjectSynced.Etag);
            Assert.AreEqual(false, onlineObjectSynced.Deleted);
            Assert.AreEqual("server2", onlineObjectSynced["key"]);

            // クライアント側はサーバ情報で上書きされること
            var offlineObjectSynced = (await GetOfflineObjects()).First();
            Assert.AreEqual(NbSyncState.Sync, offlineObjectSynced.SyncState);
            Assert.AreEqual(onlineObjectSynced.Etag, offlineObjectSynced.Etag);
            Assert.AreEqual(false, offlineObjectSynced.Deleted);
            Assert.AreEqual("server2", offlineObjectSynced["key"]);
        }

        /// <summary>
        /// 削除-更新衝突
        /// クライアント: 更新あり(論理削除)
        /// サーバ: 更新有り
        /// クライアント優先
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateLogicalDeleteServerUpdatePreferClient()
        {
            var online = await CreateOnlineObject("key", "server");
            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            // オフライン更新(論理削除)
            var offlineObject = (await GetOfflineObjects()).First();
            offlineObject["key"] = "client";
            await offlineObject.DeleteAsync(true);
            offlineObject = (await GetOfflineObjects()).First();

            // オンライン更新
            var onlineObject = (await GetOnlineObjects()).First();
            onlineObject["key"] = "server2";
            onlineObject = await onlineObject.SaveAsync();

            results = await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, results.Count());
            var result = results.First();
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, result.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.EtagMismatch, result.Reason);

            // サーバ側は更新が無いこと
            var onlineObjectSynced = (await GetOnlineObjects()).First();
            Assert.AreEqual(onlineObject.Etag, onlineObjectSynced.Etag);
            Assert.AreEqual(false, onlineObjectSynced.Deleted);
            Assert.AreEqual("server2", onlineObjectSynced["key"]);

            // クライアント側は論理削除されていること
            var offlineObjectSynced = (await GetOfflineObjects()).First();
            Assert.AreEqual(NbSyncState.Dirty, offlineObjectSynced.SyncState);
            Assert.AreEqual(onlineObjectSynced.Etag, offlineObjectSynced.Etag);
            Assert.AreEqual(true, offlineObjectSynced.Deleted);
            Assert.AreEqual("client", offlineObjectSynced["key"]);
        }

        /// <summary>
        /// 削除-削除衝突
        /// クライアント: 論理削除
        /// サーバ: 論理削除
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalPushStateLogicalDeleteServerLogicalDelete()
        {
            var offlineObject = _offlineBucket.NewObject();
            offlineObject["key"] = "client";
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();

            // サーバに同一IDのオブジェクトを生成
            // 論理削除する
            var onlineObject = await CreateObjectWithId(offlineObject.Id);
            onlineObject["key"] = "server";
            onlineObject = await onlineObject.SaveAsync();
            await onlineObject.DeleteAsync(true);
            onlineObject = (await GetOnlineObjects()).First();

            // ETagを一致させたオフラインオブジェクトを生成
            // 論理削除する
            offlineObject.Etag = onlineObject.Etag; // internalプロパティ
            offlineObject = (NbOfflineObject)await offlineObject.SaveAsync();
            await offlineObject.DeleteAsync(true);

            // サーバに生成したオブジェクトはPullしない
            var query = new NbQuery().EqualTo("key", "client");
            _syncManager.SetSyncScope(ObjectBucketName, query);

            var results = await _syncManager.SyncBucketAsync(ObjectBucketName);
            Assert.AreEqual(0, results.Count());

            // サーバ側は論理削除のままEtagが更新されること
            var onlineObjectSynced = (await GetOnlineObjects()).First();
            Assert.AreNotEqual(onlineObject.Etag, onlineObjectSynced.Etag);
            Assert.AreEqual(true, onlineObjectSynced.Deleted);
            Assert.AreEqual("server", onlineObjectSynced["key"]);

            // クライアント側は物理削除されていること
            var offlineObjects = await GetOfflineObjects();
            Assert.AreEqual(0, offlineObjects.Count());
        }

        // ----------------------------------------------------------------------
        // オフライン同期特有のユーティリティ
        // ----------------------------------------------------------------------

        private async Task CreateConflictObject(object serverValue, object clientValue)
        {
            var obj = _onlineBucket.NewObject();
            var serverObj = await obj.SaveAsync();

            // 保存済みオブジェクトを同期
            await _syncManager.SyncBucketAsync(ObjectBucketName, NbObjectConflictResolver.PreferServerResolver);

            // サーバオブジェクトの更新
            serverObj["key"] = serverValue;
            var reSavedServerObj = await serverObj.SaveAsync();

            // クライアントオブジェクトの更新
            var clientObjects = await _offlineBucket.QueryAsync(new NbQuery());
            var clientObj = clientObjects.First();
            clientObj["key"] = clientValue;
            var reSavedClientObj = await clientObj.SaveAsync();
        }

        private async Task<IEnumerable<NbObject>> GetOnlineObjects(NbQuery query = null)
        {
            var q = query ?? new NbQuery().DeleteMark(true);
            return await _onlineBucket.QueryAsync(q);
        }

        private async Task<IEnumerable<NbOfflineObject>> GetOfflineObjects(NbQuery query = null)
        {
            var q = query ?? new NbQuery().DeleteMark(true);
            return await _offlineBucket.QueryAsync(q);
        }

        private async Task<NbObject> CreateOnlineObject(string key = null, object value = null)
        {
            var obj = _onlineBucket.NewObject();

            if (key != null && value != null)
            {
                obj[key] = value;
            }

            var serverObj = await obj.SaveAsync();

            return serverObj;
        }

        private async Task<NbOfflineObject> CreateOfflineObject(string key = null, object value = null)
        {
            var obj = _offlineBucket.NewObject();
            if (key != null && value != null)
            {
                obj[key] = value;
            }

            var clientObj = (NbOfflineObject)await obj.SaveAsync();

            return clientObj;
        }

        private void CompareObjectLists(IEnumerable<NbObject> listA, IEnumerable<NbObject> listB)
        {
            Assert.AreEqual(listA.Count(), listB.Count());

            foreach (var obj in listA)
            {
                // Idが一致するオブジェクトを取得
                var compareObjects = from x in listB where x.Id == obj.Id select x;
                Assert.AreEqual(1, compareObjects.Count());

                var compareObj = compareObjects.First();

                CompareObjects(obj, compareObj);
            }
        }

        /// <summary>
        /// Objectの基本要素の比較を行う
        /// </summary>
        /// <param name="client">クライアントオブジェクト</param>
        /// <param name="server">サーバオブジェクト</param>
        /// <remarks>ACL,ユーザ定義の情報は比較しない</remarks>
        private void CompareObjects(NbObject client, NbObject server)
        {
            Assert.AreEqual(client.Id, server.Id);
            Assert.AreEqual(client.Etag, server.Etag);
            Assert.AreEqual(client.CreatedAt, server.CreatedAt);
            Assert.AreEqual(client.UpdatedAt, server.UpdatedAt);
            Assert.AreEqual(client.Deleted, server.Deleted);
        }

        private static async Task UpdateObjectBucket(string bucketName, NbAcl acl = null, NbContentAcl contentAcl = null, string description = null)
        {
            ITUtil.UseMasterKey();

            acl = acl ?? new NbAcl();
            contentAcl = contentAcl ?? new NbContentAcl();
            description = description ?? "bucket update";

            var service = NbService.GetInstance();

            var bodyJson = new NbJsonObject();
            bodyJson.Add("ACL", acl.ToJson());
            bodyJson.Add("contentACL", contentAcl.ToJson());
            bodyJson.Add("description", description);

            var req = CreateBucketRequest(bucketName, HttpMethod.Put);
            req.SetJsonBody(bodyJson);

            var rjson = await service.RestExecutor.ExecuteRequestForJson(req);

            ITUtil.UseNormalKey();
        }

        private async Task UpdateContentAclReadOnly(string bucketName)
        {
            // Pull終了時の状態を保存するため、バケットのWrite権限無しとする
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");
            contentAcl.W.Add("g:authenticated");
            await UpdateObjectBucket(bucketName, null, contentAcl, "readonly");
        }

        private static async Task DeleteObjectBucket(string bucketName)
        {
            ITUtil.UseMasterKey();

            var service = NbService.GetInstance();

            var req = CreateBucketRequest(bucketName, HttpMethod.Delete);

            var rjson = await service.RestExecutor.ExecuteRequestForJson(req);

            ITUtil.UseNormalKey();
        }

        private static NbRestRequest CreateBucketRequest(string bucketName, HttpMethod method)
        {
            var service = NbService.GetInstance();

            var req = service.RestExecutor.CreateRequest("/buckets/object/{bucket}", method);
            req.SetUrlSegment("bucket", bucketName);
            return req;
        }

        private static NbRestRequest CreateObjectRequest(string bucketName, HttpMethod method)
        {
            var service = NbService.GetInstance();

            var req = service.RestExecutor.CreateRequest("/objects/{bucket}", method);
            req.SetUrlSegment("bucket", bucketName);
            return req;
        }


        /// <summary>
        /// DBに指定件数のオブジェクトを生成する
        /// </summary>
        /// <param name="bucket">バケット</param>
        /// <param name="number">生成するオブジェクト数</param>
        /// <returns>作成したオブジェクト一覧</returns>
        private static async Task<IEnumerable<NbOfflineObject>> CreateOfflineObjects(NbObjectBucketBase<NbOfflineObject> bucket, int number)
        {
            var list = new List<NbOfflineObject>();
            for (int i = 0; i < number; i++)
            {
                var obj = bucket.NewObject();
                var saved = (NbOfflineObject)await obj.SaveAsync();
                list.Add(saved);
            }

            return list;
        }

        /// <summary>
        /// DBの指定オブジェクトを更新する
        /// </summary>
        /// <param name="bucket">バケット</param>
        /// <param name="number">更新するオブジェクト一覧</param>
        /// <returns>更新したオブジェクト一覧</returns>
        private static async Task<IEnumerable<NbOfflineObject>> UpdateOfflineObjects(NbObjectBucketBase<NbOfflineObject> bucket, IEnumerable<NbOfflineObject> target)
        {
            var list = new List<NbOfflineObject>();

            foreach (var obj in target)
            {
                var saved = (NbOfflineObject)await obj.SaveAsync();
                list.Add(saved);
            }

            return list;
        }

        /// <summary>
        /// DBの指定オブジェクトを論理削除する
        /// </summary>
        /// <param name="bucket">バケット</param>
        /// <param name="number">論理削除するオブジェクト一覧</param>
        /// <returns>論理削除したオブジェクト一覧</returns>
        private static async Task<IEnumerable<NbOfflineObject>> LogicalDeleteOfflineObjects(NbObjectBucketBase<NbOfflineObject> bucket, IEnumerable<NbOfflineObject> target)
        {
            var backetName = target.First().BucketName;

            foreach (var obj in target)
            {
                await obj.DeleteAsync(true);
            }

            var localBucket = new NbOfflineObjectBucket<NbOfflineObject>(backetName);

            var offlineObjects = await localBucket.QueryAsync(new NbQuery().DeleteMark(true));

            // Idが一致するオブジェクトを取得
            var result = GetOfflineObjectsMatchId(offlineObjects, target);

            return result;
        }

        private static IEnumerable<NbObject> GetObjectsMatchId(IEnumerable<NbObject> source, IEnumerable<NbObject> matcher)
        {
            var targetIds = from x in matcher select x.Id;

            // Idが一致するオブジェクトを取得
            var result = from x in source where targetIds.Contains(x.Id) select x;
            return result;
        }

        private static IEnumerable<NbOfflineObject> GetOfflineObjectsMatchId(IEnumerable<NbOfflineObject> source, IEnumerable<NbObject> matcher)
        {
            var targetIds = from x in matcher select x.Id;

            // Idが一致するオブジェクトを取得
            var result = from x in source where targetIds.Contains(x.Id) select x;
            return result;
        }

        private async Task<NbObject> CreateObjectWithId(string objectId)
        {
            var json = new NbJsonObject();
            json[Field.Id] = objectId;
            json["key"] = "server";
            var req = CreateObjectRequest(ObjectBucketName, HttpMethod.Post);
            req.SetJsonBody(json);
            var rjson = await _service.RestExecutor.ExecuteRequestForJson(req);
            var online = new NbObject(ObjectBucketName, rjson);

            return online;
        }

        private async Task<string> CreateSameIdObject()
        {
            var offlineObj = await CreateOfflineObject("key", "client");

            // REST APIでIdが同一のObjectをサーバに生成
            await CreateObjectWithId(offlineObj.Id);

            return offlineObj.Id;
        }


    }

}
