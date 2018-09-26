using Moq;
using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbOfflineObjectTest
    {
        private NbObjectBucketBase<NbOfflineObject> _bucket;
        private ProcessState _processState;

        [SetUp]
        public void Setup()
        {
            NbService.DisposeSingleton();
            TestUtils.Init();
            SetOfflineService(true);

            _bucket = new NbOfflineObjectBucket<NbOfflineObject>("test1");
            _processState = ProcessState.GetInstance();
        }

        private void SetMockObjectCache(NbOfflineObject obj, NbObjectCache cache)
        {
            FieldInfo objectCache = obj.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            objectCache.SetValue(obj, cache);
        }

        private void SetMockSessionInfo(NbService service, NbSessionInfo si)
        {
            PropertyInfo propertyInfo = service.GetType().GetProperty("SessionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            propertyInfo.SetValue(service, si);
        }

        private void SetOfflineService(bool enable)
        {
            if (enable)
            {
                NbOfflineService.SetInMemoryMode(true);
                NbOfflineService.EnableOfflineService(NbService.Singleton);
            }
            else
            {
                NbService.Singleton.DisableOffline();
            }
        }

        /**
        * デフォルトコンストラクタ
        **/
        /// <summary>
        /// デフォルトコンストラクタ（正常）
        /// インスタンスが取得できること
        /// </summary>
        /// 
        [Test]
        public void TestDefaultConstructorNormal()
        {
            Assert.NotNull(new NbOfflineObject());
        }

        /**
        * コンストラクタ 引数はbucketName, service
        **/
        /// <summary>
        /// コンストラクタ（service指定あり）
        /// サービス、バケット名には指定の値が格納されること
        /// オブジェクトID、ACL、ETag、作成日時、更新日時がnullであること 
        /// 削除マークがFalseであること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var s1 = NbService.Singleton;
            NbService.EnableMultiTenant(true);
            var s2 = NbService.GetInstance();
            s2.TenantId = "s2_tenantid";
            s2.AppId = "s2_appid";
            s2.AppKey = "s2_appkey"; ;
            s2.EndpointUrl = "s2_epurl";

            NbOfflineService.EnableOfflineService(s2);
            Assert.AreNotSame(s1, s2);

            var obj = new NbOfflineObject("test", s2);

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(s2, obj.Service);
            Assert.IsNull(obj.Id);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsNull(obj.UpdatedAt);
            Assert.IsNull(obj.Acl);
            Assert.IsNull(obj.Etag);
            Assert.False(obj.Deleted);
            Assert.AreEqual(0, obj.Count());
            FieldInfo objectCache = obj.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(obj));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// コンストラクタ（service指定なし）
        /// バケット名には指定の値が格納されること
        /// オブジェクトID、ACL、ETag、作成日時、更新日時がnullであること 
        /// 削除マークがFalseであること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceUnset()
        {
            var obj = new NbOfflineObject("test");

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
            Assert.IsNull(obj.Id);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsNull(obj.UpdatedAt);
            Assert.IsNull(obj.Acl);
            Assert.IsNull(obj.Etag);
            Assert.False(obj.Deleted);
            Assert.AreEqual(0, obj.Count());
            FieldInfo objectCache = obj.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(obj));
        }

        /// <summary>
        /// コンストラクタ（serviceがnull）
        /// バケット名には指定の値が格納されること
        /// オブジェクトID、ACL、ETag、作成日時、更新日時がnullであること 
        /// 削除マークがFalseであること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceNull()
        {
            var obj = new NbOfflineObject("test", null);

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
            FieldInfo objectCache = obj.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(obj));
        }

        /// <summary>
        /// コンストラクタ（bucketNameがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionBucketNameNull()
        {
            var obj = new NbOfflineObject(null);
        }

        /// <summary>
        /// コンストラクタ（NbOfflineObject、オフラインサービス無効）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public void TestConstructorExceptionOfflineDisabled()
        {
            var s1 = NbService.Singleton;
            NbService.EnableMultiTenant(true);
            var s2 = NbService.GetInstance();
            Assert.AreNotSame(s1, s2);

            try
            {
                var obj = new NbOfflineObject("test", s2);
                Assert.Fail("no exception");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
            finally
            {
                NbService.EnableMultiTenant(false);
            }
        }

        /**
        * コンストラクタ 引数はbucketName, json, service
        **/
        /// <summary>
        /// コンストラクタ（正常）
        /// 各プロパティ、フィールドには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonNormal()
        {
            const string objId = "0123456789abcdef";
            const string acl = "{owner:'o', r:[], w:[], c:[], u:[], d:[], admin:[]}";
            const string createdAt = "2015-01-01T00:00:00.000Z";
            const string updatedAt = "2015-02-02T00:00:00.000Z";
            const string etag = "43488373-3499-49d4-ab21-5e44f1149310";
            string jsonString = string.Format("{{_id:'{0}', test_key:'test_value', ACL:{1}, createdAt:'{2}', updatedAt:'{3}', etag:'{4}', _deleted:true}}", objId, acl, createdAt, updatedAt, etag);

            var obj = new NbOfflineObject("test", NbJsonObject.Parse(jsonString));

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
            Assert.AreEqual(objId, obj.Id);
            Assert.AreEqual(createdAt, obj.CreatedAt);
            Assert.AreEqual(updatedAt, obj.UpdatedAt);
            Assert.AreEqual("o", obj.Acl.Owner);
            Assert.IsEmpty(obj.Acl.R);
            Assert.IsEmpty(obj.Acl.W);
            Assert.IsEmpty(obj.Acl.C);
            Assert.IsEmpty(obj.Acl.U);
            Assert.IsEmpty(obj.Acl.D);
            Assert.IsEmpty(obj.Acl.Admin);
            Assert.AreEqual(etag, obj.Etag);
            Assert.True(obj.Deleted);
            Assert.AreEqual("test_value", obj["test_key"]);
            Assert.AreEqual(7, obj.Count()); // 任意のkey-valueだけでなく、他のkeyも含まれるため
        }

        /// <summary>
        /// コンストラクタ（jsonが空）
        /// オブジェクトID、ACL、ETag、作成日時、更新日時がnullであること
        /// 削除マークがFalseであること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonNormalJsonEmpty()
        {
            var obj = new NbOfflineObject("test", new NbJsonObject());

            Assert.AreEqual("test", obj.BucketName);
            Assert.IsNull(obj.Id);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsNull(obj.UpdatedAt);
            Assert.IsNull(obj.Acl);
            Assert.IsNull(obj.Etag);
            Assert.False(obj.Deleted);
            Assert.AreEqual(0, obj.Count());
        }

        /// <summary>
        /// コンストラクタ（jsonがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorWithJsonExceptionJsonNull()
        {
            var obj = new NbOfflineObject("test", (NbJsonObject)null);
        }

        /// <summary>
        /// コンストラクタ（json内に不正フィールドを含む）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorWithJsonExceptionInvalidField()
        {
            var json = new NbJsonObject()
            {
                {".test", "value"}
            };
            var obj = new NbOfflineObject("test", json);
        }

        /**
        * Init
        **/
        // 引数がbucketName, serviceのコンストラクタの方で検証したので、UTは省略する

        /**
         * SaveAsync 
         */

        /// <summary>
        /// SaveAsync（正常）
        /// 新規追加/更新ができること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormal()
        {
            var obj = _bucket.NewObject();
            obj["key"] = "abc";

            // INSERT
            var result = await obj.SaveAsync();
            var id = obj.Id;
            Assert.AreEqual("abc", result["key"]);
            Assert.AreSame(obj, result);
            Assert.NotNull(id);

            // UPDATE
            obj["key"] = "cde";
            result = await obj.SaveAsync();
            Assert.AreEqual("cde", result["key"]);
            Assert.AreSame(obj, result);
            Assert.AreEqual(id, obj.Id);
        }

        /// <summary>
        /// SaveAsync
        /// 新規追加 正常系 ACLなし、未ログイン
        /// ・新規追加となること（NbObjectCache#InsertObject()をコール）
        /// ・InsertObject()には、自インスタンスとNbSyncState.Dirtyを渡すこと
        /// ・Acl自動補間処理が呼ばれること（処理後に自動補間されること）
        /// ・自インスタンスのSyncStateはDirtyになること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalInsertNoAclNotLoggedIn()
        {

            var obj = _bucket.NewObject();

            var aclValue = new HashSet<string>();
            aclValue.Add("g:anonymous");

            // InsertObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject actualObj, NbSyncState actualState) =>
                {
                    Assert.AreEqual(obj, (NbOfflineObject)actualObj);
                    Assert.AreEqual(aclValue, actualObj.Acl.R);
                    Assert.AreEqual(aclValue, actualObj.Acl.W);
                    Assert.IsEmpty(actualObj.Acl.U);
                    Assert.IsEmpty(actualObj.Acl.C);
                    Assert.IsEmpty(actualObj.Acl.D);
                    Assert.IsEmpty(actualObj.Acl.Admin);
                    Assert.IsNull(actualObj.Acl.Owner);
                    Assert.AreEqual(NbSyncState.Dirty, actualState);
                });

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            Assert.NotNull(ret.Acl);
            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());
        }

        /// <summary>
        /// SaveAsync
        /// 新規追加 正常系 ACLなし、ログイン済
        /// ・新規追加となること（NbObjectCache#InsertObject()をコール）
        /// ・InsertObject()には、自インスタンスとNbSyncState.Dirtyを渡すこと
        /// ・Acl自動補間処理が呼ばれること（処理後に自動補間されること）
        /// ・自インスタンスのSyncStateはDirtyになること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalInsertNoAclLoggedIn()
        {
            var obj = _bucket.NewObject();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            // InsertObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject actualObj, NbSyncState actualState) =>
                {
                    Assert.AreEqual(obj, (NbOfflineObject)actualObj);
                    Assert.IsEmpty(actualObj.Acl.R);
                    Assert.IsEmpty(actualObj.Acl.W);
                    Assert.IsEmpty(actualObj.Acl.U);
                    Assert.IsEmpty(actualObj.Acl.C);
                    Assert.IsEmpty(actualObj.Acl.D);
                    Assert.IsEmpty(actualObj.Acl.Admin);
                    Assert.AreEqual("testUser", actualObj.Acl.Owner);
                    Assert.AreEqual(NbSyncState.Dirty, actualState);
                });

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            Assert.NotNull(ret.Acl);
            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());
        }

        /// <summary>
        /// SaveAsync
        /// 新規追加 正常系 ACLあり
        /// ・新規追加となること（NbObjectCache#InsertObject()をコール）
        /// ・InsertObject()には、自インスタンスとNbSyncState.Dirtyを渡すこと
        /// ・自インスタンスのSyncStateはDirtyになること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalInsertWithAcl()
        {
            var obj = _bucket.NewObject();
            var aclValue = new HashSet<string>();
            aclValue.Add("g:anonymous");
            var acl = new NbAcl();
            acl.C = aclValue;
            acl.D = aclValue;
            acl.Owner = "testUser";
            obj.Acl = acl;

            // InsertObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject actualObj, NbSyncState actualState) =>
                {
                    Assert.AreEqual(obj, (NbOfflineObject)actualObj);
                    Assert.IsEmpty(actualObj.Acl.R);
                    Assert.IsEmpty(actualObj.Acl.W);
                    Assert.IsEmpty(actualObj.Acl.U);
                    Assert.AreEqual(aclValue, actualObj.Acl.C);
                    Assert.AreEqual(aclValue, actualObj.Acl.D);
                    Assert.IsEmpty(actualObj.Acl.Admin);
                    Assert.AreEqual("testUser", actualObj.Acl.Owner);
                    Assert.AreEqual(NbSyncState.Dirty, actualState);
                });

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());
        }

        /// <summary>
        /// SaveAsync
        /// 更新 正常系（ACL更新なし）
        /// ・更新となること（NbObjectCache#UpdateObject()をコール）
        /// ・UpdateObject()には、自インスタンスとNbSyncState.Dirtyを渡すこと
        /// ・自インスタンスのSyncStateはDirtyになること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalUpdate()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            // UpdateObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject actualObj, NbSyncState actualState) =>
                {
                    Assert.AreEqual(obj, (NbOfflineObject)actualObj);
                    Assert.AreEqual(NbSyncState.Dirty, actualState);
                });

            // 対象オブジェクトあり
            // ACLに変更なし
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(obj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// SaveAsync
        /// 更新 正常系（ACL更新あり）
        /// ・Admin権限を有する場合はUpdate可能なこと
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalUpdateWithAclUpdate()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            json.Add(Field.Acl, NbAcl.CreateAclForAuthenticated().ToJson());
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            // UpdateObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject actualObj, NbSyncState actualState) =>
                {
                    Assert.AreEqual(obj, (NbOfflineObject)actualObj);
                    Assert.AreEqual(NbSyncState.Dirty, actualState);
                });

            // 対象オブジェクトあり
            // ACLに変更あり
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// SaveAsync
        /// 例外（バケット名未設定）
        /// ・バケット名が未設定の場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestSaveAsyncExceptionNoBucketName()
        {
            _bucket.BucketName = null;
            var obj = _bucket.NewObject();

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

        }

        /// <summary>
        /// SaveAsync
        /// 更新 例外（オブジェクトなし）
        /// ・対象オブジェクトがない場合はエラーとなること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionUpdateObjectNotExist()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            json.Add(Field.Acl, NbAcl.CreateAclForAuthenticated().ToJson());
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトなし
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((NbOfflineObject)null);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                var ret = (NbOfflineObject)await obj.SaveAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }

        /// <summary>
        /// SaveAsync
        /// 更新 例外（更新するオブジェクトのACL未設定）
        /// ・エラーとなること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionUpdateNoAclUpdate()
        {

            // ACL無しのオブジェクトに更新
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = "cacheETag";
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                var ret = (NbOfflineObject)await obj.SaveAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }

        /// <summary>
        /// SaveAsync
        /// 更新 例外（ETag不正）
        /// ・ETag不正時はエラーとなること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionUpdateEtagMismatch()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            json.Add(Field.Acl, NbAcl.CreateAclForAuthenticated().ToJson());
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ETag不一致
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = "cacheETag";
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                var ret = (NbOfflineObject)await obj.SaveAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }

        /// <summary>
        /// SaveAsync
        /// 更新 例外（ACL不正：Update）
        /// ・ACL不正時はエラーとなること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionUpdateNoUpdatePermission()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            json.Add(Field.Acl, NbAcl.CreateAclForAuthenticated().ToJson());
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL Updateなし
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Acl.W = new HashSet<string>();
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                var ret = (NbOfflineObject)await obj.SaveAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }

        /// <summary>
        /// SaveAsync
        /// 更新 例外（ACL不正：Admin）
        /// ・ACL不正時はエラーとなること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionUpdateNoAdminPermission()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            json.Add(Field.Acl, NbAcl.CreateAclForAuthenticated().ToJson());
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL Adminなし
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Acl.Admin = new HashSet<string>();
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                var ret = (NbOfflineObject)await obj.SaveAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }

        /// <summary>
        /// SaveAsync
        /// 新規追加 同期中
        /// ・新規追加処理が成功すること
        /// ・NbExceptionが発生しないこと
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalInsertWhileSyncing()
        {
            var obj = _bucket.NewObject();
            var aclValue = new HashSet<string>();
            aclValue.Add("g:anonymous");
            var acl = new NbAcl();
            acl.C = aclValue;
            acl.D = aclValue;
            acl.Owner = "testUser";
            obj.Acl = acl;

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

            // 後始末
            _processState.EndSync();
        }

        /// <summary>
        /// SaveAsync
        /// 更新 同期中
        /// ・NbExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionUpdateWhileSyncing()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();

            // Test
            try
            {
                var ret = (NbOfflineObject)await obj.SaveAsync();
                Assert.Fail("no exception");
            }
            catch (NbException ex)
            {
                Assert.AreEqual(NbStatusCode.Locked, ex.StatusCode);
                Assert.AreEqual("Locked.", ex.Message);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            // 後始末
            _processState.EndSync();
        }

        /// <summary>
        /// SaveAsync
        /// 更新 同期終了後
        /// ・更新処理が成功すること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalUpdateAfterSyncing()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACLに変更なし
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(obj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();
            // 同期を終了させる
            _processState.EndSync();

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());
        }

        /// <summary>
        /// SaveAsync
        /// 更新 CRUD中
        /// ・CRUD終了まで待ち合わせた後で更新処理が成功すること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalUpdateWhileCrud()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            json.Add(Field.Acl, NbAcl.CreateAclForAuthenticated().ToJson());
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            // UpdateObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACLに変更あり
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            Task task = new Task(() =>
            {
                Thread.Sleep(500);
                _processState.EndCrud();
            });
            task.Start();

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, ret.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

            // 念の為後始末
            _processState.EndCrud();
        }

        /**
        * EnsureOfflineAcl
        **/
        // SaveAsync()の方で検証したので、UTは省略する

        /**
         * PartUpdateAsync
         */
        /// <summary>
        /// PartUpdateAsync
        /// NotSupportedExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public async void TestPartUpdateAsyncExceptionNotImplemented()
        {
            var obj = _bucket.NewObject();
            await obj.PartUpdateAsync(new NbJsonObject());
        }

        /**
         * DeleteAsync 
         */

        /// <summary>
        /// DeleteAsync
        /// 物理削除 正常系
        /// ・物理削除となること（NbObjectCache#DeleteObject()をコール）
        /// ・DeleteObject()には、自インスタンスを渡すこと
        /// ・自インスタンスのSyncStateはDirtyになること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalHardDelete()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();
            obj.Acl.D.Add("g:authenticated");

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);


            // DeleteObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.DeleteObject(It.IsAny<NbObject>()))
                .Callback((NbObject actualObj) =>
                {
                    Assert.AreEqual(obj, (NbOfflineObject)actualObj);
                });

            // 対象オブジェクトあり
            // ACL権限あり
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(obj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            await obj.DeleteAsync(false);

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.IsFalse(obj.Deleted);
            mockObjectCache.Verify(m => m.DeleteObject(It.IsAny<NbObject>()), Times.Once());

        }

        /// <summary>
        /// DeleteAsync（物理削除）
        /// 物理削除が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalHardDeleteUsingInmemory()
        {
            // save
            var obj = _bucket.NewObject();
            obj["key"] = "abc";
            await obj.SaveAsync();

            // hard delete
            await obj.DeleteAsync(false);

            // 状態確認
            Assert.False(_processState.Crud);

            // query
            var query = new NbQuery().DeleteMark(true);
            var found = await _bucket.QueryAsync(query);
            Assert.False(found.Any());
        }

        /// <summary>
        /// DeleteAsync
        /// 論理削除 正常系
        /// ・論理削除となること（NbObjectCache#UpdateObject()をコール）
        /// ・UpdateObject()には、自インスタンスとNbSyncState.Dirtyを渡すこと
        /// ・自インスタンスのSyncStateはDirtyになること
        /// ・自インスタンスのDeletedはTrueとなること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDelete()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            // UpdateObjectが呼ばれること
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject actualObj, NbSyncState actualState) =>
                {
                    Assert.AreEqual(obj, (NbOfflineObject)actualObj);
                    Assert.AreEqual(NbSyncState.Dirty, actualState);
                });

            // 対象オブジェクトあり
            // ACL権限あり
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(obj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            await obj.DeleteAsync();

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.IsTrue(obj.Deleted);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// DeleteAsync（論理削除）
        /// 論理削除が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteUsingInmemory()
        {
            // save
            var obj = _bucket.NewObject();
            obj["key"] = "abc";
            await obj.SaveAsync();

            // soft delete
            await obj.DeleteAsync();

            // 状態確認
            Assert.False(_processState.Crud);

            // query
            var query = new NbQuery().DeleteMark(true);
            var found = await _bucket.QueryAsync(query);
            Assert.AreEqual(obj.Id, found.First().Id);
            Assert.True(found.First().Deleted);
        }

        /// <summary>
        /// DeleteAsync
        /// 削除 例外（バケット名未設定）
        /// ・バケット名が未設定の場合エラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteAsyncExceptionNoBucketName()
        {
            _bucket.BucketName = null;
            var obj = _bucket.NewObject();
            obj.Id = "testObjectID";

            // Test
            await obj.DeleteAsync();

        }

        /// <summary>
        /// DeleteAsync
        /// 削除 例外（オブジェクトID未設定）
        /// ・オブジェクトIDが未設定の場合エラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteAsyncExceptionNoObjectID()
        {
            var obj = _bucket.NewObject();

            // Test
            await obj.DeleteAsync();

        }

        /// <summary>
        /// DeleteAsync
        /// 削除 例外（オブジェクトなし）
        /// ・対象オブジェクトがない場合はエラーとなること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionObjectNotExist()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトなし
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((NbOfflineObject)null);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                await obj.DeleteAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }

        /// <summary>
        /// DeleteAsync
        /// 削除 例外（オブジェクトなし）
        /// ・対象オブジェクトがない場合はエラーとなること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionObjectNotExistUsingInmemory()
        {
            // save
            var obj = _bucket.NewObject();
            obj["key"] = "abc";
            await obj.SaveAsync();

            await obj.DeleteAsync(false);

            try
            {
                await obj.DeleteAsync();
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

        }

        /// <summary>
        /// DeleteAsync
        /// 削除 例外（ACL不正：Delete）
        /// ・ACL不正時はエラーとなること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionNoDeletePermission()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL Updateなし
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Acl.W = new HashSet<string>();
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                await obj.DeleteAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }

        /// <summary>
        /// DeleteAsync
        /// 削除 例外（ACL不正：Delete）
        /// ・ACL不正時はエラーとなること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionNoDeletePermissionUsingInmemory()
        {
            // save
            var obj = _bucket.NewObject();
            obj["key"] = "abc";
            var acl = new NbAcl();
            obj.Acl = acl;
            await obj.SaveAsync();

            try
            {
                await obj.DeleteAsync();
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);
        }

        /// <summary>
        /// DeleteAsync
        /// 削除 例外（ETag不正）
        /// ・ETag不正時はエラーとなること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionEtagMismatch()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ETag不一致
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = "cacheETag";
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            try
            {
                await obj.DeleteAsync();
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Never());

        }


        /// <summary>
        /// DeleteAsync
        /// 同期中
        /// ・NbExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionWhileSyncing()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();

            // Test
            try
            {
                await obj.DeleteAsync();
                Assert.Fail("no exception");
            }
            catch (NbException ex)
            {
                Assert.AreEqual(NbStatusCode.Locked, ex.StatusCode);
                Assert.AreEqual("Locked.", ex.Message);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            // 後始末
            _processState.EndSync();
        }

        /// <summary>
        /// DeleteAsync
        /// 同期終了後
        /// ・削除処理が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalAfterSyncing()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL権限あり
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(obj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();
            // 同期を終了させる
            _processState.EndSync();

            // Test
            await obj.DeleteAsync();

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.IsTrue(obj.Deleted);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());
        }

        /// <summary>
        /// DeleteAsync
        /// CRUD中
        /// ・CRUD終了まで待ち合わせた後で削除処理が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalWhileCrud()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();
            obj.Acl.D.Add("g:authenticated");

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL権限あり
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(obj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            Task task = new Task(() =>
            {
                Thread.Sleep(500);
                _processState.EndCrud();
            });
            task.Start();

            // Test
            await obj.DeleteAsync(false);

            // 状態確認
            Assert.False(_processState.Crud);

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.IsFalse(obj.Deleted);
            mockObjectCache.Verify(m => m.DeleteObject(It.IsAny<NbObject>()), Times.Once());
        }

        /**
         * IsConflict
         */
        /// <summary>
        /// IsConflict
        /// Etagが一致
        /// ・衝突しないこと
        /// </summary>
        [Test]
        public async void TestIsConflictNormal()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Etag = "testEtag";

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ETag一致
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = obj.Etag;
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            await obj.DeleteAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// IsConflict
        /// キャッシュEtagがNull
        /// ・衝突しないこと
        /// </summary>
        [Test]
        public async void TestIsConflictNormalCacheEtagNotExist()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Etag = "testEtag";

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ETag一致
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = null;
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            await obj.DeleteAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// IsConflict
        /// Etag未設定
        /// ・衝突しないこと
        /// </summary>
        [Test]
        public async void TestIsConflictNormalEtagNotExist()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Etag = null;

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ETag一致
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = null;
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            await obj.DeleteAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// IsConflict
        /// 更新Etag未設定
        /// ・衝突扱いとなりエラーが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(NbHttpException))]
        public async void TestIsConflictExceptionUpdateEtagNotExist()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Etag = null;

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ETag一致
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = "cacheETag";
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            await obj.DeleteAsync();

        }

        /// <summary>
        /// IsConflict
        /// Etag不一致
        /// ・衝突扱いとなりエラーが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(NbHttpException))]
        public async void TestIsConflictExceptionEtagMismatch()
        {
            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Etag = "testCache";

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ETag一致
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Etag = "cacheETag";
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            await obj.DeleteAsync();

        }

        /**
         * IsUpdateAcl
         */

        /// <summary>
        /// IsUpdateAcl
        /// ACL未変更
        /// ・ACLに変更がない場合は更新なしとすること
        /// </summary>
        [Test]
        public async void TestIsUpdateAclNormalSameAcl()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();
            obj.Acl.Admin = new HashSet<string>();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL Updateなし
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = obj.Acl;
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// IsUpdateAcl
        /// キャッシュのACLがNULL
        /// ・ACLに変更がある場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(NbHttpException))]
        public async void TestIsUpdateAclExceptionCacheAclNotExist()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAuthenticated();
            obj.Acl.Admin = new HashSet<string>();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL Updateなし
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = null;
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

        }

        /// <summary>
        /// IsUpdateAcl
        /// 変更後のACLがNULL
        /// ・ACLに変更がある場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(NbHttpException))]
        public async void TestIsUpdateAclExceptionUpdateAclToNull()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL Updateなし
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Acl.Admin = new HashSet<string>();
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

        }

        /// <summary>
        /// IsUpdateAcl
        /// 複数の権限に変更有り
        /// ・ACLに変更がある場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(NbHttpException))]
        public async void TestIsUpdateAcExceptionUpdatePermissions()
        {

            var json = new NbJsonObject();
            json.Add("_id", "TestId");
            var obj = new NbOfflineObject(_bucket.BucketName, json);
            obj.Acl = NbAcl.CreateAclForAnonymous();

            // User取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            user.Groups = new List<string>();
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // 対象オブジェクトあり
            // ACL Updateなし
            var cacheObj = new NbOfflineObject(_bucket.BucketName, json);
            cacheObj.Acl = NbAcl.CreateAclForAuthenticated();
            cacheObj.Acl.Admin = new HashSet<string>();
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(obj, mockObjectCache.Object);

            // Test
            var ret = (NbOfflineObject)await obj.SaveAsync();

        }
    }
}
