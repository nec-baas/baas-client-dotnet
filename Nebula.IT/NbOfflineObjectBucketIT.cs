using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    /// <summary>
    /// サブクラステスト用
    /// </summary>
    internal class Product : NbOfflineObject
    {
        public Product()
            : base("ObjectBucketIT")
        {
        }

        public string Name
        {
            get { return this["name"] as string; }
            set { this["name"] = value; }
        }
    }

    [TestFixture]
    public class NbOfflineObjectBucketIT
    {
        private const string BucketName = ITUtil.ObjectBucketName;
        private NbService _service;
        private NbOfflineObjectBucket<NbOfflineObject> _offlineBucket;
        private NbOfflineObject _obj;
        private IEnumerable<NbObject> _objs;
        private int _deleteCount;
        private NbObjectSyncManager _syncManager;

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineUser().Wait();
            ITUtil.InitOnlineGroup().Wait();
            // データが残っていると同期に時間がかかってしまうため、オンラインオブジェクトもクリアしておく
            ITUtil.InitOnlineObjectStorage().Wait();

            _service = NbService.Singleton;

            NbOfflineService.EnableOfflineService(_service);
            NbOfflineService.DeleteCacheAll().Wait();

            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(BucketName, _service);
            _obj = null;
            _objs = null;
            _deleteCount = 0;
            _syncManager = new NbObjectSyncManager(_service);
            _syncManager.SetSyncScope(BucketName, new NbQuery());
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクト作成<br/>
        /// 新規作成<br/>
        /// </summary>
        [Test]
        public async void TestCreateObjectNormalLogin()
        {
            var user = await ITUtil.SignUpAndLogin();

            var expectedAcl = new NbAcl();
            expectedAcl.Owner = user.UserId;

            var obj = new NbOfflineObject(BucketName);
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj);

            Assert.AreEqual("testValue", _obj["testKey"]);
            Assert.NotNull(_obj.Id);
            Assert.Null(_obj.CreatedAt);
            Assert.Null(_obj.UpdatedAt);
            Assert.Null(_obj.Etag);
            Assert.True(ITUtil.CompareAcl(expectedAcl, _obj.Acl));
            AssertObject(obj, _obj);
            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// 新規作成<br/>
        /// </summary>
        [Test]
        public async void TestCreateObjectNormalNotLogin()
        {
            var expectedAcl = NbAcl.CreateAclForAnonymous();
            expectedAcl.Admin.Clear();

            var obj = new NbOfflineObject(BucketName);
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj);

            Assert.AreEqual("testValue", _obj["testKey"]);
            Assert.NotNull(_obj.Id);
            Assert.Null(_obj.CreatedAt);
            Assert.Null(_obj.UpdatedAt);
            Assert.Null(_obj.Etag);
            Assert.True(ITUtil.CompareAcl(expectedAcl, _obj.Acl));
            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// 新規作成<br/>
        /// </summary>
        [Test]
        public async void TestCreateObjectNormalAcl()
        {
            var acl = NbAcl.CreateAclForAuthenticated();
            acl.Owner = "TestUser";

            var obj = new NbOfflineObject(BucketName);
            obj.Acl = acl;
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj);

            Assert.True(ITUtil.CompareAcl(acl, _obj.Acl));
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// バケットなし<br/>
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public async void TestCreateObjectExceptionInvalidBucket()
        {
            var obj = new NbOfflineObject("NotExistBucketName");
            obj["testKey"] = "testValue";

            // test
            await obj.SaveAsync();
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// バケット名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestCreateObjectExceptionNoBucketName()
        {
            var obj = new NbOfflineObject(BucketName);
            obj.BucketName = null;

            // test
            await obj.SaveAsync();
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトの読み込み<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectNormalId()
        {
            var obj = await CreateObject();

            // test
            await GetObjectTest(obj.Id);

            Assert.AreEqual("testValue", _obj["testKey"]);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// 該当オブジェクトなし<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionObjectNotExist()
        {
            var obj = await CreateObject();

            // test
            await GetObjectTest("dummy", HttpStatusCode.NotFound);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// 削除済みマークされたオブジェクトの読み込み<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionDeletedObject()
        {
            var obj = await CreateObject();
            await obj.DeleteAsync(true);

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectNormalAclOwner()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id);

            Assert.AreEqual("testValue", _obj["testKey"]);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectNormalAclReadAnonymous()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id);

            Assert.AreEqual("testValue", _obj["testKey"]);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectNormalAclReadAuthenticated()
        {
            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            acl.R.Add("testUser");
            var obj = await CreateObject(acl);

            var user = await ITUtil.SignUpAndLogin();

            // test
            await GetObjectTest(obj.Id);

            Assert.AreEqual("testValue", _obj["testKey"]);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectNormalAclReadUser()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.R.Add(user.UserId);
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id);

            Assert.AreEqual("testValue", _obj["testKey"]);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectNormalAclReadGroup()
        {
            await AddGroupMember();
            await ITUtil.LoginUserWithUser(ITUtil.Username);

            var acl = new NbAcl();
            acl.R.Add("g:testGroup");
            acl.R.Add("testUser");
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionAclReadEmpty()
        {
            var acl = new NbAcl();
            acl.W.Add("g:anonymous");
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionAclOwnerNotLoggedIn()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            var obj = await CreateObject(acl);
            await ITUtil.Logout();

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionAclAuthenticatedNotLoggedIn()
        {
            var acl = new NbAcl();
            acl.R.Add("testUser");
            acl.R.Add("g:authenticated");
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionAclUserMismatch()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.R.Add("mismatchUser");
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionAclGroupMismatch()
        {
            await AddGroupMember();
            await ITUtil.LoginUserWithUser(ITUtil.Username);

            var acl = new NbAcl();
            acl.R.Add("g:mismatchGroup");
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// バケットなし<br/>
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public async void TestGetObjectExceptionBucketNotExsist()
        {
            var obj = await CreateObject();
            await NbOfflineService.DeleteCacheAll();

            // test
            await _offlineBucket.GetAsync(obj.Id);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// オブジェクトIDがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestGetObjectExceptionNoObjectId()
        {
            var obj = await CreateObject();

            // test
            await _offlineBucket.GetAsync(null);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// 全件クエリ、カウントあり<br/>
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public async void TestQueryObjectsWithOptionsExceptionNotSupport()
        {
            // test
            await _offlineBucket.QueryWithOptionsAsync(new NbQuery(), true);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// 全件クエリ<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalBlankBucket()
        {
            // test
            await QueryObjectsTest(new NbQuery());

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormal()
        {
            await ITUtil.CreateObjectsOfDataA(false);

            // 全件クエリ
            // test
            await QueryObjectsTest(new NbQuery());
            Assert.AreEqual(10, _objs.ToList().Count);
            AssertQueryResult(_objs.ToList());

            // リセット
            _objs = null;

            // limit
            // test
            await QueryObjectsTest(new NbQuery().Limit(5));
            Assert.AreEqual(5, _objs.ToList().Count);

            // リセット
            _objs = null;

            // skip
            // test
            await QueryObjectsTest(new NbQuery().Skip(8));
            Assert.AreEqual(2, _objs.ToList().Count);

            // リセット
            _objs = null;

            // skipに負の値を設定
            // test
            await QueryObjectsTest(new NbQuery().Skip(-1));
            Assert.AreEqual(10, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalLimit()
        {
            await ITUtil.CreateObjectsOfLimitTest(false);

            // queryがnull(limit検証)
            // test
            await QueryObjectsTest(null);
            Assert.AreEqual(200, _objs.ToList().Count);

            // リセット
            _objs = null;

            // limit = -1を設定
            // test
            await QueryObjectsTest(new NbQuery().Limit(-1));
            Assert.AreEqual(200, _objs.ToList().Count);

            // リセット
            _objs = null;

            // limitに-1未満の負数を設定
            // test
            await QueryObjectsTest(new NbQuery().Limit(-2));
            Assert.AreEqual(200, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// queryがnull<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalNoQuery()
        {
            var obj = await CreateObject();
            await ITUtil.CreateObjectsOfDataA(false);
            await DeleteObject(obj);

            // test
            await QueryObjectsTest(null);

            Assert.AreEqual(10, _objs.ToList().Count);
            AssertQueryResult(_objs.ToList());
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// order<br/>
        /// 複合キー指定<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalOrderMultiKey()
        {
            for (int i = 1; i <= 3; i++)
            {
                for (int j = 1; j <= 3; j++)
                {
                    var json1 = new NbJsonObject()
                    {
                        {"data1", i},
                        {"data2", j}
                    };
                    await ITUtil.CreateOfflineObject(json1);
                }
            }

            var json2 = new NbJsonObject()
            {
                {"data1", 3},
                {"data2", 4}
            };
            await ITUtil.CreateOfflineObject(json2);

            // test
            var query = new NbQuery().OrderBy("data2", "-data1");
            await QueryObjectsTest(query);

            var objs = _objs.ToList();

            // 1件目
            Assert.AreEqual(3, objs[0].Opt<int>("data1", -1));
            Assert.AreEqual(1, objs[0].Opt<int>("data2", -1));
            // 2件目
            Assert.AreEqual(2, objs[1].Opt<int>("data1", -1));
            Assert.AreEqual(1, objs[1].Opt<int>("data2", -1));
            // 3件目
            Assert.AreEqual(1, objs[2].Opt<int>("data1", -1));
            Assert.AreEqual(1, objs[2].Opt<int>("data2", -1));
            // 4件目
            Assert.AreEqual(3, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(2, objs[3].Opt<int>("data2", -1));
            // 5件目
            Assert.AreEqual(2, objs[4].Opt<int>("data1", -1));
            Assert.AreEqual(2, objs[4].Opt<int>("data2", -1));
            // 6件目
            Assert.AreEqual(1, objs[5].Opt<int>("data1", -1));
            Assert.AreEqual(2, objs[5].Opt<int>("data2", -1));
            // 7件目
            Assert.AreEqual(3, objs[6].Opt<int>("data1", -1));
            Assert.AreEqual(3, objs[6].Opt<int>("data2", -1));
            // 8件目
            Assert.AreEqual(2, objs[7].Opt<int>("data1", -1));
            Assert.AreEqual(3, objs[7].Opt<int>("data2", -1));
            // 9件目
            Assert.AreEqual(1, objs[8].Opt<int>("data1", -1));
            Assert.AreEqual(3, objs[8].Opt<int>("data2", -1));
            // 10件目
            Assert.AreEqual(3, objs[9].Opt<int>("data1", -1));
            Assert.AreEqual(4, objs[9].Opt<int>("data2", -1));
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// limit + skip + order + projection<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalExcludeWhereOption()
        {
            var json = new NbJsonObject()
            {
                {"data1", 121},
                {"data2", 0}
            };
            await ITUtil.CreateOfflineObject(json);
            await ITUtil.CreateObjectsOfDataA(false);

            var query = new NbQuery().Limit(5).Skip(3).OrderBy("-data1").Projection("-data2");
            // test
            await QueryObjectsTest(query);

            var objs = _objs.ToList();

            // 1～5件
            Assert.AreEqual(130, objs[0].Opt<int>("data1", 0));
            Assert.AreEqual(125, objs[1].Opt<int>("data1", 0));
            Assert.AreEqual(121, objs[2].Opt<int>("data1", 0));
            Assert.True(objs[2].HasKey("data2"));
            Assert.AreEqual(120, objs[3].Opt<int>("data1", 0));
            Assert.AreEqual(115, objs[4].Opt<int>("data1", 0));
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// where組み合わせ
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalWhereCombination()
        {
            var jsonData2 = new NbJsonObject()
            {
                {"data3", 3}
            };
            var json1 = new NbJsonObject()
            {
                {"data1", 1},
                {"data2", jsonData2}
            };
            var obj = await ITUtil.CreateOfflineObject(json1);

            var json2 = new NbJsonObject()
            {
                {"data1", 3},
                {"data2", jsonData2}
            };
            await ITUtil.CreateOfflineObject(json2);

            var query = new NbQuery().LessThan("data1", 2).EqualTo("data2", jsonData2);
            // test
            await QueryObjectsTest(query);

            Assert.AreEqual(1, _objs.ToList().Count);
            AssertObject(obj, (NbOfflineObject)_objs.First());
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// deleteMark<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalDeleteMark()
        {
            var obj = await CreateObject();
            await ITUtil.CreateObjectsOfDataA(false);
            await DeleteObject(obj);

            // test
            await QueryObjectsTest(new NbQuery().DeleteMark(true));

            Assert.AreEqual(11, _objs.ToList().Count);

            var v = from x in _objs where x.Id == obj.Id select x;

            Assert.AreEqual(1, v.Count());
            Assert.True(v.First().Deleted);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// projection<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalProjection()
        {
            await ITUtil.CreateObjectsOfDataD(false);

            // test
            await QueryObjectsTest(new NbQuery().Projection("name", "number"));

            var objs = _objs.ToList();
            foreach (var obj in objs)
            {
                Assert.AreEqual(5, obj.Count());
                Assert.True(obj.HasKey("name"));
                Assert.True(obj.HasKey("number"));
                Assert.True(obj.HasKey("telno"));
                Assert.True(obj.HasKey("ACL"));
                Assert.True(obj.HasKey("_id"));
            }
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalAclOwner()
        {
            await ITUtil.CreateObjectsOfDataA(false, new NbAcl());

            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            var obj = await CreateObject(acl);

            // test
            await QueryObjectsTest(new NbQuery());

            Assert.AreEqual(1, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalAclRead()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            acl.R.Add("testUser");
            await ITUtil.CreateObjectsOfDataA(false, acl);
            await CreateObject(new NbAcl());

            // test
            await QueryObjectsTest(new NbQuery());

            Assert.AreEqual(10, _objs.ToList().Count);
            AssertQueryResult(_objs.ToList());
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalAclReadNotLoggedIn()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.R.Add(user.UserId);
            acl.Owner = null;
            await ITUtil.CreateObjectsOfDataA(false, acl);

            await ITUtil.Logout();

            // test
            await QueryObjectsTest(new NbQuery());

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// クエリに不正な演算子設定を行う<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalInvalidQuery()
        {
            await CreateObject();

            var query = new NbQuery().All("testKey", (object[])null);

            // test
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// オブジェクトの更新（部分更新）<br/>
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public async void TestPartUpdateObjectExceptionNotSupport()
        {
            var obj = _offlineBucket.NewObject();
            var json = new NbJsonObject();

            // test
            await obj.PartUpdateAsync(json);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトの更新（完全上書き）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormal()
        {
            var obj = await CreateObject();
            var oldId = obj.Id;
            var oldCreatedAt = obj.CreatedAt;
            var oldUpdatedAt = obj.UpdatedAt;
            var oldEtag = obj.Etag;

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(obj.HasKey("UpdateKey"));
            Assert.AreEqual(oldId, obj.Id);
            Assert.AreEqual(oldCreatedAt, obj.CreatedAt);
            Assert.AreEqual(oldUpdatedAt, obj.UpdatedAt);
            Assert.AreEqual(oldEtag, obj.Etag);
            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            AssertObject(obj, _obj);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 削除済みマークされたオブジェクトの上書き<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalDeletedObject()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            await DeleteObject(obj);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(obj.HasKey("UpdateKey"));
            Assert.True(obj.Deleted);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 該当オブジェクトなし<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionObjectNotExist()
        {
            var obj = await CreateObject();
            await DeleteObject(obj, false);
            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL以外を更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalAclOwner()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(obj.HasKey("UpdateKey"));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL以外を更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalAclUpdateGroup()
        {
            await AddGroupMember();
            await ITUtil.LoginUserWithUser(ITUtil.Username);

            var acl = new NbAcl();
            acl.U.Add("g:testGroup");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(obj.HasKey("UpdateKey"));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL以外を更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalAclWriteAuthenticated()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.W.Add("g:authenticated");
            acl.R.Add("testUser");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(obj.HasKey("UpdateKey"));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL以外を更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionAclUpdateAndWriteEmpty()
        {
            var acl = new NbAcl();
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL以外を更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionAclUpdateUserMismatch()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.U.Add("mismatchUser");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL以外を更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionAclWriteGroupMismatch()
        {
            await AddGroupMember();
            await ITUtil.LoginUserWithUser(ITUtil.Username);

            var acl = new NbAcl();
            acl.W.Add("g:mismatchGroup");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL以外を更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionAclAdminAnonymous()
        {
            var acl = new NbAcl();
            acl.Admin.Add("g:anonymous");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalAclChangeOwner()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";
            obj.Acl = NbAcl.CreateAclForAnonymous();

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(_obj.HasKey("UpdateKey"));
            Assert.True(ITUtil.CompareAcl(NbAcl.CreateAclForAnonymous(), obj.Acl));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalAclChangeAdminAndUpdate()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Admin.Add(user.UserId);
            acl.U.Add("g:anonymous");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";
            obj.Acl.Admin.Clear();

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(_obj.HasKey("UpdateKey"));

            var expectedAcl = new NbAcl();
            expectedAcl.U.Add("g:anonymous");
            Assert.True(ITUtil.CompareAcl(expectedAcl, obj.Acl));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalAclChangeAdminAndWrite()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Admin.Add("g:authenticated");
            acl.W.Add(user.UserId);
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";
            obj.Acl = NbAcl.CreateAclForAnonymous();

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(_obj.HasKey("UpdateKey"));
            Assert.True(ITUtil.CompareAcl(NbAcl.CreateAclForAnonymous(), obj.Acl));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionAclChangeAdminEmpty()
        {
            var acl = new NbAcl();
            acl.U.Add("g:anonymous");
            acl.W.Add("g:anonymous");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";
            obj.Acl = NbAcl.CreateAclForAnonymous();

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オブジェクトACL(ACL更新）<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionAclChangeUpdateAndDeleteEmpty()
        {
            var acl = new NbAcl();
            acl.Admin.Add("g:anonymous");
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";
            obj.Acl = NbAcl.CreateAclForAnonymous();

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 楽観ロック<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalDbEtagNotExist()
        {
            var obj = await CreateObject();
            obj.Etag = "dummy";
            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(_obj.HasKey("UpdateKey"));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 楽観ロック<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalEtagMatch()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);
            Assert.NotNull(robj.Etag);
            robj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(robj);

            Assert.True(_obj.HasKey("UpdateKey"));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 楽観ロック<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionEtagMismatch()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);
            Assert.AreNotEqual(obj.Etag, robj.Etag);
            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Conflict);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// ACL未設定で更新<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionNoAcl()
        {
            var obj = await CreateObject();
            obj.Acl = null;
            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// バケット名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestFullUpdateObjectExceptionNoBucketName()
        {
            var obj = await CreateObject();
            obj["UpdateKey"] = "UpdateValue";
            obj.BucketName = null;

            // test
            await obj.SaveAsync();
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 論理削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalSoftDelete()
        {
            var obj = await CreateObject();
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, true);

            await GetObjectTest(id, HttpStatusCode.NotFound);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(true);
            await QueryObjectsTest(query);

            Assert.AreEqual(1, _objs.ToList().Count);
            Assert.AreEqual(obj.UpdatedAt, _objs.First().UpdatedAt);
            Assert.AreEqual(obj.Etag, _objs.First().Etag);
            Assert.True(obj.Deleted);
            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            AssertObject(obj, (NbOfflineObject)_objs.First());
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 物理削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalHardDelete()
        {
            var obj = await CreateObject();
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, false);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(true);
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 削除の指定なし<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalSoftDeleteUnset()
        {
            var obj = await CreateObject();
            var id = obj.Id;

            // test
            await obj.DeleteAsync();

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(true);
            await QueryObjectsTest(query);

            Assert.AreEqual(1, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 該当オブジェクトなし<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionObjectNotExist()
        {
            var obj = await CreateObject();
            await NbOfflineService.DeleteCacheAll();
            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(BucketName);

            // test
            await DeleteObjectTest(obj, true, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalAclOwner()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            var obj = await CreateObject(acl);
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, true);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(false);
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalAclDeleteAnonymous()
        {
            var acl = new NbAcl();
            acl.D.Add("g:anonymous");
            var obj = await CreateObject(acl);
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, true);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(false);
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalAclWriteAuthenticated()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.W.Add("g:authenticated");
            var obj = await CreateObject(acl);
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, true);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(false);
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalAclDeleteUser()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.D.Add(user.UserId);
            var obj = await CreateObject(acl);
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, true);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(false);
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalAclWriteGroup()
        {
            await AddGroupMember();
            await ITUtil.LoginUserWithUser(ITUtil.Username);

            var acl = new NbAcl();
            acl.W.Add("g:testGroup");
            acl.W.Add("testUser");
            var obj = await CreateObject(acl);
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, true);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(false);
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionAclDeleteAndWriteEmpty()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            var obj = await CreateObject(acl);

            // test
            await DeleteObjectTest(obj, true, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionAclDeleteUserNotLoggedIn()
        {
            var user = await ITUtil.SignUpAndLogin();
            var acl = new NbAcl();
            acl.D.Add(user.UserId);
            var obj = await CreateObject(acl);

            await ITUtil.Logout();

            // test
            await DeleteObjectTest(obj, true, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionAclWriteGroupNotLoggedIn()
        {
            await AddGroupMember();
            await ITUtil.LoginUserWithUser(ITUtil.Username);

            var acl = new NbAcl();
            acl.W.Add("g:testGroup");
            acl.W.Add("testUser");
            var obj = await CreateObject(acl);

            await ITUtil.Logout();

            // test
            await DeleteObjectTest(obj, true, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 楽観ロック<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalDbEtagNotExist()
        {
            var obj = await CreateObject();
            var id = obj.Id;

            // test
            await DeleteObjectTest(obj, true);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(true);
            await QueryObjectsTest(query);

            Assert.AreEqual(1, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 楽観ロック<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectNormalEtagMatch()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);
            Assert.NotNull(robj.Etag);

            // test
            await DeleteObjectTest(robj, true);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 楽観ロック<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionEtagNotExist()
        {
            var obj = await CreateObject();
            var id = obj.Id;

            await _syncManager.SyncBucketAsync(BucketName);
            var robj = await _offlineBucket.GetAsync(id);
            Assert.NotNull(robj.Etag);

            // test
            await DeleteObjectTest(obj, false, HttpStatusCode.Conflict);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 楽観ロック<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionEtagMismatch()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            obj.Etag = "dummy";
            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);
            Assert.AreNotEqual(obj.Etag, robj.Etag);

            // test
            await DeleteObjectTest(obj, true, HttpStatusCode.Conflict);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// オブジェクトIDがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteObjectExceptionNoId()
        {
            var obj = await CreateObject();
            obj.Id = null;

            // test
            await obj.DeleteAsync(true);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// バケット名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteObjectExceptionNoBucketName()
        {
            var obj = await CreateObject();
            obj.BucketName = null;

            // test
            await obj.DeleteAsync(true);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 全件論理削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalSoftDelete()
        {
            var obj = await CreateObject();
            await ITUtil.CreateObjectsOfDataA(false);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(11, _deleteCount);

            await QueryObjectsTest(new NbQuery().DeleteMark(true));
            Assert.AreEqual(11, _objs.ToList().Count);

            var query = new NbQuery().EqualTo("_id", obj.Id).DeleteMark(true);
            await QueryObjectsTest(query);

            Assert.AreEqual(obj.UpdatedAt, _objs.First().UpdatedAt);
            Assert.AreEqual(obj.Etag, _objs.First().Etag);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 全件物理削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalHardDelete()
        {
            await ITUtil.CreateObjectsOfDataA(false);

            // test
            await DeleteAllObjectsTest(new NbQuery(), false);

            Assert.AreEqual(10, _deleteCount);

            await QueryObjectsTest(new NbQuery().DeleteMark(true));
            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 全件、削除の指定なし<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalSoftDeleteUnset()
        {
            await ITUtil.CreateObjectsOfDataA(false);

            // test
            var deleteCount = await _offlineBucket.DeleteAsync(new NbQuery());

            Assert.AreEqual(10, deleteCount);

            await QueryObjectsTest(new NbQuery().DeleteMark(true));
            Assert.AreEqual(10, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 削除条件あり<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalWithWhere()
        {
            await ITUtil.CreateObjectsOfDataA(false);

            // test
            var query = new NbQuery().LessThan("data1", 120).OrderBy("data1");
            await DeleteAllObjectsTest(query, true);

            Assert.AreEqual(4, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 他のクエリパラメータ指定あり<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalWithOtherParam()
        {
            await ITUtil.CreateObjectsOfDataA(false);

            // test
            var query = new NbQuery().Limit(3).OrderBy("data1").Skip(2).Projection("data1");
            await DeleteAllObjectsTest(query, true);

            Assert.AreEqual(10, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 論理削除データの論理削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalDeletedSoftDelete()
        {
            await ITUtil.CreateObjectsOfDataA(false);
            await DeleteAllObjectsTest(new NbQuery(), true);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(0, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 論理削除データの物理削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalDeletedHardDelete()
        {
            await ITUtil.CreateObjectsOfDataA(false);
            await DeleteAllObjectsTest(new NbQuery(), true);

            // test
            await DeleteAllObjectsTest(new NbQuery(), false);

            Assert.AreEqual(0, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalAclReadAndWriteAnonymous()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            acl.W.Add("g:anonymous");
            await ITUtil.CreateObjectsOfDataA(false, acl);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(10, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalAclOwner()
        {
            var user = await ITUtil.SignUpAndLogin();

            var acl = new NbAcl();
            acl.R.Add(user.UserId);
            await ITUtil.CreateObjectsOfDataA(false, acl);

            var acl2 = new NbAcl();
            acl2.Owner = user.UserId;
            await CreateObject(acl2);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(1, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalAclRead()
        {
            var user = await ITUtil.SignUpAndLogin();

            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            acl.D.Add(user.UserId);
            acl.D.Add("testUser");
            await ITUtil.CreateObjectsOfDataA(false, acl);

            var acl2 = new NbAcl();
            acl.D.Add(user.UserId);
            acl.D.Add("testUser");
            await CreateObject(acl2);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(10, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalAclDelete()
        {
            var user = await ITUtil.SignUpAndLogin();

            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            await ITUtil.CreateObjectsOfDataA(false, acl);

            var acl2 = new NbAcl();
            acl2.R.Add("g:authenticated");
            acl2.D.Add("g:authenticated");
            await CreateObject(acl2);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(1, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// オブジェクトACL<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalAclWrite()
        {
            var user = await ITUtil.SignUpAndLogin();

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            await ITUtil.CreateObjectsOfDataA(false, acl);

            var acl2 = new NbAcl();
            acl2.R.Add("g:anonymous");
            acl2.W.Add(user.UserId);
            await CreateObject(acl2);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(1, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// クエリに不正な演算子設定を行う<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalInvalidQuery()
        {
            await CreateObject();

            var query = new NbQuery().All("testKey", (object[])null);

            // test
            await DeleteAllObjectsTest(query, true);

            Assert.AreEqual(0, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// バケット名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteAllObjectsExceptionNoQuery()
        {
            await _offlineBucket.DeleteAsync(null, true);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// バッチオペレーション<br/>
        /// バッチオペレーション<br/>
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public async void TestBatchExceptionNotSupport()
        {
            var obj = _offlineBucket.NewObject();
            var req = new NbBatchRequest().AddInsertRequest(obj);

            // test
            await _offlineBucket.BatchAsync(req);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 集計(Aggregate)<br/>
        /// AggregateAsync<br/>
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public async void TestAggregateExceptionNotSupport()
        {
            var pipeline = new NbJsonArray();

            // test
            await _offlineBucket.AggregateAsync(pipeline);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 初期値<br/>
        /// NbOfflineObject<br/>
        /// </summary>
        [Test]
        public void TestNbOfflineObjectNormal()
        {
            // test
            var obj = new NbOfflineObject(BucketName);

            Assert.AreEqual(BucketName, obj.BucketName);
            Assert.Null(obj.Id);
            Assert.Null(obj.Acl);
            Assert.Null(obj.Etag);
            Assert.Null(obj.CreatedAt);
            Assert.Null(obj.UpdatedAt);
            Assert.False(obj.Deleted);
            Assert.AreEqual(NbSyncState.Sync, obj.SyncState);
        }

        /// <summary>
        /// 初期値<br/>
        /// NbOfflineObjectBucket<br/>
        /// </summary>
        [Test]
        public void TestNbOfflineObjectBucketNormal()
        {
            // test
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(BucketName);

            Assert.AreEqual(BucketName, bucket.BucketName);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// コレクション初期化子<br/>
        /// </summary>
        [Test]
        public void TestCollectionInitializersNormal()
        {
            // test
            var obj = new NbOfflineObject(BucketName)
            {
                {"name", "Taro Nichiden"},
                {"age", 32}
            };

            Assert.AreEqual("Taro Nichiden", obj["name"]);
            Assert.AreEqual(32, obj["age"]);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// インデクサ<br/>
        /// </summary>
        [Test]
        public void TestIndexerNormal()
        {
            var obj = new NbOfflineObject(BucketName);
            // test
            obj["data1"] = "abcde";

            Assert.AreEqual("abcde", obj["data1"]);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// インデクサ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestIndexerExceptionSetInvalidKey()
        {
            var obj = new NbOfflineObject(BucketName);
            // test
            obj["."] = "abcde";
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// LINQ<br/>
        /// </summary>
        [Test]
        public void TestNbOfflineObjectNormalLINQ()
        {
            var obj = new NbOfflineObject(BucketName);
            obj["key1"] = 123456;
            obj["key2"] = "test";

            // test
            var v = from x in obj where x.Key == "key1" select x.Value;

            Assert.AreEqual(1, v.Count());
            Assert.AreEqual(123456, v.First());
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// オブジェクト生成<br/>
        /// </summary>
        [Test]
        public void TestNewObjectNormal()
        {
            // test
            var obj = _offlineBucket.NewObject();

            Assert.NotNull(obj);
            Assert.AreEqual(BucketName, obj.BucketName);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// オブジェクト生成<br/>
        /// </summary>
        [Test]
        public void TestNewObjectNormalSubclass()
        {
            // test
            var obj = new NbOfflineObjectBucket<Product>().NewObject();

            Assert.NotNull(obj);
            Assert.AreEqual("ObjectBucketIT", obj.BucketName);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// サブクラス<br/>
        /// </summary>
        [Test]
        public async void TestSubclassNormal()
        {
            var obj = new Product()
            {
                Name = "TV"
            };
            // 複数のバケットを作成したくなので、バケット名を入れ替える
            obj.BucketName = BucketName;

            // create
            var create = await obj.SaveAsync();

            // query
            var bucket = new NbOfflineObjectBucket<Product>(BucketName);
            var results = await bucket.QueryAsync(new NbQuery());
            Assert.AreEqual(1, results.ToList().Count);
            Assert.AreEqual("TV", results.First().Name);

            // update
            create["Serial"] = "00000";
            var updateObj = await create.SaveAsync();

            // delete
            await updateObj.DeleteAsync();
        }

        /// <summary>
        /// オフラインサービスが無効<br/>
        /// NbOfflineObject<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestOfflineObjectExceptionOfflineIsDisabled()
        {
            _service.DisableOffline();

            // test
            var obj = new NbOfflineObject(BucketName);
        }

        /// <summary>
        /// オフラインサービスが無効<br/>
        /// NbOfflineObjectBucket<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestOfflineObjectBucketExceptionOfflineIsDisabled()
        {
            _service.DisableOffline();

            // test
            var obj = new NbOfflineObjectBucket<NbOfflineObject>(BucketName);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 各種データ型が混在するオブジェクトの作成・クエリ<br/>
        /// </summary>
        [Test]
        public async void TestObjectContainsMultipleDataType()
        {
            var array = new NbJsonArray()
            {
                "Car", "Music"
            };

            var obj = new NbOfflineObject(BucketName)
            {
                {"name", "Taro"},
                {"age", 13},
                {"option", true},
                {"hobby", array},
                {"type", null},
                {"other", new NbJsonObject()
                    {
                        {"fish", "\uD867\uDE3D"},
                        {"mark", new NbJsonObject()
                            {
                                {"mark1", " !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~"},
                                {"mark2", "aaa"}
                            }
                         }
                    }
                }
            };

            var robj = await obj.SaveAsync();
            Assert.True(robj.HasKey("other"));
            Assert.True(robj.HasKey("_id"));
            Assert.AreEqual(array, robj.Opt<NbJsonArray>("hobby", null));
            var id = robj.Id;

            var query = new NbQuery().Exists("other");
            var result = await _offlineBucket.QueryAsync(query);
            Assert.AreEqual(1, result.ToList().Count);
            Assert.AreEqual(id, result.First().Id);

            query = new NbQuery().EqualTo("other.fish", "\uD867\uDE3D");
            result = await _offlineBucket.QueryAsync(query);
            Assert.AreEqual(1, result.ToList().Count);

            query = new NbQuery().EqualTo("other.mark.mark1", " !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~");
            result = await _offlineBucket.QueryAsync(query);
            Assert.AreEqual(1, result.ToList().Count);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 繰り返し評価<br/>
        /// </summary>
        [Test]
        public async void TestCrudRepeatNormal()
        {
            for (int i = 0; i < 3; i++)
            {
                // Create
                var obj = _offlineBucket.NewObject();
                obj["key"] = "value";
                obj = (NbOfflineObject)await obj.SaveAsync();
                Assert.NotNull(obj.Id);

                // Update
                obj["updateKey"] = "updateValue";
                var updateObj = await obj.SaveAsync();
                Assert.True(updateObj.HasKey("updateKey"));

                // Get
                var getObj = await _offlineBucket.GetAsync(updateObj.Id);
                Assert.True(getObj.HasKey("updateKey"));

                // Query
                var queryResult = await _offlineBucket.QueryAsync(new NbQuery().EqualTo("_id", getObj.Id).DeleteMark(true));
                Assert.AreEqual(1, queryResult.ToList().Count);

                // DeleteAllObjecsts
                var deleteResult = await _offlineBucket.DeleteAsync(new NbQuery().DeleteMark(true), true);

                queryResult = await _offlineBucket.QueryAsync(new NbQuery().EqualTo("_id", getObj.Id).DeleteMark(true));
                Assert.AreEqual(1, queryResult.ToList().Count);

                // DeleteObject
                await getObj.DeleteAsync(false);

                queryResult = await _offlineBucket.QueryAsync(new NbQuery().EqualTo("_id", getObj.Id).DeleteMark(true));
                Assert.AreEqual(0, queryResult.ToList().Count);
            }
        }

        /// <summary>
        /// 繰り返し評価<br/>
        /// </summary>
        [Test]
        public async void TestUpdateAndDeleteNormal()
        {
            // Create
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";
            obj = (NbOfflineObject)await obj.SaveAsync();

            Task[] updateTasks = new Task[3];
            Task[] deleteTasks = new Task[3];
            for (int i = 0; i < 3; i++)
            {
                // Update(awaitしない)
                obj["key1"] = "value2";
                updateTasks[i] = obj.SaveAsync();

                // Delete(awaitしない)
                deleteTasks[i] = obj.DeleteAsync(true);
            }

            Task.WaitAll(updateTasks);
            Task.WaitAll(deleteTasks);
        }

        /// <summary>
        /// 繰り返し評価<br/>
        /// </summary>
        [Test]
        public async void TestUpdateAndDeleteNormalWithEtag()
        {
            // Create
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";
            obj = (NbOfflineObject)await obj.SaveAsync();

            // Sync
            await _syncManager.SyncBucketAsync(BucketName);

            var objs = await _offlineBucket.QueryAsync(new NbQuery().EqualTo("_id", obj.Id));
            var robj = objs.First();

            Task[] updateTasks = new Task[3];
            Task[] deleteTasks = new Task[3];
            for (int i = 0; i < 3; i++)
            {
                // Update(awaitしない)
                robj["key1"] = "value2";
                updateTasks[i] = robj.SaveAsync();

                // Delete(awaitしない)
                deleteTasks[i] = robj.DeleteAsync(true);
            }

            Task.WaitAll(updateTasks);
            Task.WaitAll(deleteTasks);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 状態遷移<br/>
        /// 遷移前：データ無、状態-、Etag-<br/>
        /// 操作：C
        /// </summary>
        [Test]
        public async void TestOfflineStateCreate()
        {
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";

            await obj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
        }

        /// <summary>
        /// 状態遷移<br/>
        /// 遷移前：データ有、状態SYNC、Etag有<br/>
        /// 操作：U
        /// </summary>
        [Test]
        public async void TestOfflineStateUpdateWithEtagInSyncState()
        {
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";

            await obj.SaveAsync();

            var id = obj.Id;

            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);

            Assert.AreEqual(NbSyncState.Sync, robj.SyncState);
            Assert.NotNull(robj.Etag);

            // test
            robj["key2"] = "value2";
            await robj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, robj.SyncState);
            Assert.NotNull(robj.Etag);
        }

        /// <summary>
        /// 状態遷移<br/>
        /// 遷移前：データ有、状態SYNC、Etag有<br/>
        /// 操作：D
        /// </summary>
        [Test]
        public async void TestOfflineStateDeleteWithEtagInSyncState()
        {
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";

            await obj.SaveAsync();

            var id = obj.Id;

            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);

            Assert.AreEqual(NbSyncState.Sync, robj.SyncState);
            Assert.NotNull(robj.Etag);

            // test
            await robj.DeleteAsync(true);

            Assert.AreEqual(NbSyncState.Dirty, robj.SyncState);
            Assert.NotNull(robj.Etag);
            Assert.True(robj.Deleted);
        }

        /// <summary>
        /// 状態遷移<br/>
        /// 遷移前：データ有、状態DIRTY、Etag無<br/>
        /// 操作：U
        /// </summary>
        [Test]
        public async void TestOfflineStateUpdateInDirtyState()
        {
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";

            await obj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.Null(obj.Etag);

            // test
            obj["key2"] = "value2";
            await obj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.Null(obj.Etag);
        }

        /// <summary>
        /// 状態遷移<br/>
        /// 遷移前：データ有、状態DIRTY、Etag無<br/>
        /// 操作：D
        /// </summary>
        [Test]
        public async void TestOfflineStateDeleteInDirtyState()
        {
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";

            await obj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.Null(obj.Etag);

            // test
            await obj.DeleteAsync(true);

            Assert.AreEqual(NbSyncState.Dirty, obj.SyncState);
            Assert.Null(obj.Etag);
            Assert.True(obj.Deleted);
        }

        /// <summary>
        /// 状態遷移<br/>
        /// 遷移前：データ有、状態DIRTY、Etag有<br/>
        /// 操作：U
        /// </summary>
        [Test]
        public async void TestOfflineStateUpdateWithEtagInDirtyState()
        {
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";

            await obj.SaveAsync();

            var id = obj.Id;

            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);
            robj["key2"] = "value2";
            await robj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, robj.SyncState);
            Assert.NotNull(robj.Etag);

            // test
            robj["key3"] = "value3";
            await robj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, robj.SyncState);
            Assert.NotNull(robj.Etag);
        }

        /// <summary>
        /// 状態遷移<br/>
        /// 遷移前：データ有、状態DIRTY、Etag有<br/>
        /// 操作：D
        /// </summary>
        [Test]
        public async void TestOfflineStateDeleteWithEtagInDirtyState()
        {
            var obj = _offlineBucket.NewObject();
            obj["key"] = "value";

            await obj.SaveAsync();

            var id = obj.Id;

            await _syncManager.SyncBucketAsync(BucketName);

            var robj = await _offlineBucket.GetAsync(id);
            robj["key2"] = "value2";
            await robj.SaveAsync();

            Assert.AreEqual(NbSyncState.Dirty, robj.SyncState);
            Assert.NotNull(robj.Etag);

            // test
            await robj.DeleteAsync(true);

            Assert.AreEqual(NbSyncState.Dirty, robj.SyncState);
            Assert.NotNull(robj.Etag);
            Assert.True(robj.Deleted);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトを作成する
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <returns>NbOfflineObject</returns>
        private async Task<NbOfflineObject> CreateObject(NbAcl acl = null)
        {
            var obj = new NbOfflineObject(BucketName);

            return await ITUtil.CreateOfflineObject("testKey", "testValue", acl);
        }

        /// <summary>
        /// オブジェクトを削除する
        /// </summary>
        /// <param name="obj">NbOfflineObject</param>
        /// <param name="softDelete">論理削除の場合はtrue</param>
        /// <returns>Task</returns>
        private async Task DeleteObject(NbOfflineObject obj = null, bool softDelete = true)
        {
            await DeleteObjectTest(obj, softDelete);
        }

        /// <summary>
        /// 作成テスト
        /// </summary>
        /// <param name="obj">NbOfflineObject</param> 
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <returns>Task</returns>
        private async Task CreateObjectTest(NbOfflineObject obj, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            try
            {
                _obj = (NbOfflineObject)await obj.SaveAsync();
                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbHttpException e)
            {
                if (expectedStatusCode == HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(expectedStatusCode, e.StatusCode);
                Assert.NotNull(e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 読込テスト
        /// </summary>
        /// <param name="id">オブジェクトID</param> 
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <returns>Task</returns>
        private async Task GetObjectTest(string id, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            try
            {
                _obj = await _offlineBucket.GetAsync(id);
                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbHttpException e)
            {
                if (expectedStatusCode == HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(expectedStatusCode, e.StatusCode);
                Assert.NotNull(e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// クエリテスト
        /// </summary>
        /// <param name="query">クエリ</param> 
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <returns>Task</returns>
        private async Task QueryObjectsTest(NbQuery query, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            try
            {
                _objs = await _offlineBucket.QueryAsync(query);
                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbHttpException e)
            {
                if (expectedStatusCode == HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(expectedStatusCode, e.StatusCode);
                Assert.NotNull(e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 完全上書きテスト
        /// </summary>
        /// <param name="obj">NbOfflineObject</param> 
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <returns>Task</returns>
        private async Task FullUpdateObjectTest(NbOfflineObject obj, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            await CreateObjectTest(obj, expectedStatusCode);
        }

        /// <summary>
        /// 削除テスト
        /// </summary>
        /// <param name="obj">NbOfflineObject</param>
        /// <param name="softDelete">論理削除の場合はtrue</param>
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <returns>Task</returns>
        private async Task DeleteObjectTest(NbOfflineObject obj = null, bool softDelete = true, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            try
            {
                if (obj == null)
                {
                    await _obj.DeleteAsync(softDelete);
                }
                else
                {
                    await obj.DeleteAsync(softDelete);
                }

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbHttpException e)
            {
                if (expectedStatusCode == HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(expectedStatusCode, e.StatusCode);
                Assert.NotNull(e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 一括削除テスト
        /// </summary>
        /// <param name="query">クエリ</param> 
        /// <param name="softDelete">論理削除の場合はtrue</param>
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <returns>Task</returns>
        private async Task DeleteAllObjectsTest(NbQuery query, bool softDelete = true, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            try
            {
                _deleteCount = await _offlineBucket.DeleteAsync(query, softDelete);
                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbHttpException e)
            {
                if (expectedStatusCode == HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(expectedStatusCode, e.StatusCode);
                Assert.NotNull(e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オブジェクトを比較する、一致しない場合はfailとなる
        /// </summary>
        /// <param name="obj1">比較対象</param>
        /// <param name="obj2">比較対象</param>
        private void AssertObject(NbOfflineObject obj1, NbOfflineObject obj2)
        {
            Assert.AreEqual(obj1.ToJson(), obj2.ToJson());
        }

        /// <summary>
        /// クエリ結果を比較する、一致しない場合はfailとなる
        /// </summary>
        /// <param name="softDelete">論理削除の場合はtrue</param>
        private void AssertQueryResult(List<NbObject> objects)
        {
            int[] results = new int[10];
            for (int i = 0; i < 10; i += 1)
            {
                var obj = objects[i] as NbOfflineObject;
                results[i] = obj.Opt<int>("data1", 0);
            }
            Array.Sort(results);

            int[] expectedResults = { 100, 105, 110, 115, 120, 125, 130, 135, 140, 145 };

            for (int i = 0; i < 10; i += 1)
            {
                Assert.AreEqual(expectedResults, results);
            }
        }

        /// <summary>
        /// testGroupというグループに所属する
        /// </summary>
        /// <returns>Task</returns>
        private async Task AddGroupMember()
        {
            var user = await ITUtil.SignUpUser(ITUtil.Username);
            var group = new NbGroup("testGroup");
            group.Acl = NbAcl.CreateAclForAnonymous();
            await group.SaveAsync();

            var users = new List<string>();
            users.Add(user.UserId);
            await group.AddMembersAsync(users, null);
        }
    }
}
