using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    /// <summary>
    /// サブクラステスト用
    /// </summary>
    internal class Person : NbObject
    {
        public Person()
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
    public class NbObjectBucketIT
    {
        private const string BucketName = ITUtil.ObjectBucketName;
        private NbService _service;
        private NbObjectBucket<NbObject> _onlineBucket;
        private NbObject _obj;
        private IEnumerable<NbObject> _objs;
        private int _deleteCount;
        private NbObjectBucket<NbObject>.NbObjectQueryResult<NbObject> _queryResult;
        private IList<NbBatchResult> _batchResult;

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineUser().Wait();
            ITUtil.InitOnlineObjectStorage().Wait();

            _service = NbService.Singleton;

            _onlineBucket = new NbObjectBucket<NbObject>(BucketName);
            _obj = null;
            _objs = null;
            _deleteCount = 0;
            _queryResult = new NbObjectBucket<NbObject>.NbObjectQueryResult<NbObject>();
            _batchResult = new List<NbBatchResult>();
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

            var obj = new NbObject(BucketName);
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj);

            Assert.AreEqual("testValue", _obj["testKey"]);
            Assert.NotNull(_obj.Id);
            Assert.NotNull(_obj.CreatedAt);
            Assert.NotNull(_obj.UpdatedAt);
            Assert.NotNull(_obj.Etag);
            Assert.True(ITUtil.CompareAcl(expectedAcl, _obj.Acl));
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

            var obj = new NbObject(BucketName);
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj);

            Assert.AreEqual("testValue", _obj["testKey"]);
            Assert.NotNull(_obj.Id);
            Assert.NotNull(_obj.CreatedAt);
            Assert.NotNull(_obj.UpdatedAt);
            Assert.NotNull(_obj.Etag);
            Assert.True(ITUtil.CompareAcl(expectedAcl, _obj.Acl));
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

            var obj = new NbObject(BucketName);
            obj.Acl = acl;
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj);

            Assert.True(ITUtil.CompareAcl(acl, _obj.Acl));
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// マスターキー<br/>
        /// </summary>
        [Test]
        public async void TestCreateObjectSubnormalMasterKey()
        {
            var contentAcl = CreateContentAclRAnonymous();

            await ITUtil.CreateObjectBucket(null, contentAcl);

            ITUtil.UseMasterKey();

            var obj = new NbObject(BucketName);
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj);

            Assert.AreEqual("testValue", _obj["testKey"]);
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// 権限エラー<br/>
        /// </summary>
        [Test]
        public async void TestCreateObjectExceptionPermissionCreate()
        {
            var contentAcl = CreateContentAclRAnonymous();

            await ITUtil.CreateObjectBucket(null, contentAcl);

            var obj = new NbObject(BucketName);
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// パス不正<br/>
        /// </summary>
        [Test]
        public async void TestCreateObjectExceptionWrongPath()
        {
            var obj = new NbObject("NotExistBucketName");
            obj["testKey"] = "testValue";

            // test
            await CreateObjectTest(obj, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// HTTPヘッダパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestCreateObjectExceptionWrongHttpHeader()
        {
            var obj = new NbObject(BucketName);
            obj["testKey"] = "testValue";
            _service.AppId = "dummy";

            // test
            await CreateObjectTest(obj, HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// オブジェクト作成<br/>
        /// バケット名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestCreateObjectExceptionNoBucketName()
        {
            var obj = new NbObject(BucketName);
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
        /// マスターキー<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectSubnormalMasterKey()
        {
            var acl = new NbAcl();
            var obj = await CreateObject(acl);

            await ITUtil.CreateObjectBucket(null, new NbContentAcl());

            ITUtil.UseMasterKey();

            // test
            await GetObjectTest(obj.Id);

            Assert.AreEqual("testValue", _obj["testKey"]);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// 権限エラー(バケット)<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionPermissionBucketRead()
        {
            var obj = await CreateObject();

            await ITUtil.CreateObjectBucket(null, new NbContentAcl());

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// 権限エラー(オブジェクト)<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionPermissionRead()
        {
            var acl = new NbAcl();
            var obj = await CreateObject(acl);

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// パス不正<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionWrongPath()
        {
            var obj = await CreateObject();
            var bucket = new NbObjectBucket<NbObject>("");

            try
            {
                // test
                await bucket.GetAsync(obj.Id);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オブジェクト読み込み<br/>
        /// HTTPヘッダパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestGetObjectExceptionWrongHttpHeader()
        {
            var obj = await CreateObject();
            _service.AppId = "";

            // test
            await GetObjectTest(obj.Id, HttpStatusCode.Unauthorized);
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
            await _onlineBucket.GetAsync(null);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// 全件クエリ、カウントあり<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalBlankBucket()
        {
            // test
            await QueryObjectsWithOptionsTest(new NbQuery(), true);

            Assert.IsEmpty(_queryResult.Objects);
            Assert.AreEqual(0, _queryResult.Count);
            Assert.NotNull(_queryResult.CurrentTime);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormal()
        {
            await ITUtil.CreateObjectsOfDataA();

            // 全件クエリ、カウントあり
            // test
            await QueryObjectsWithOptionsTest(new NbQuery(), true);
            Assert.AreEqual(10, _queryResult.Objects.ToList().Count);
            Assert.AreEqual(10, _queryResult.Count);
            Assert.NotNull(_queryResult.CurrentTime);
            AssertQueryResult(_queryResult.Objects.ToList());

            // リセット
            _queryResult = new NbObjectBucket<NbObject>.NbObjectQueryResult<NbObject>();

            // 全件クエリ、カウントなし
            // test
            await QueryObjectsWithOptionsTest(new NbQuery(), false);
            Assert.AreEqual(10, _queryResult.Objects.ToList().Count);
            Assert.AreEqual(-1, _queryResult.Count);
            Assert.NotNull(_queryResult.CurrentTime);
            AssertQueryResult(_queryResult.Objects.ToList());

            // リセット
            _queryResult = new NbObjectBucket<NbObject>.NbObjectQueryResult<NbObject>();

            // 全件クエリ、カウント指定なし
            // test
            _queryResult = await _onlineBucket.QueryWithOptionsAsync(new NbQuery());
            Assert.AreEqual(10, _queryResult.Objects.ToList().Count);
            Assert.AreEqual(-1, _queryResult.Count);
            Assert.NotNull(_queryResult.CurrentTime);
            AssertQueryResult(_queryResult.Objects.ToList());

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

            // リセット
            _objs = null;

            // order 単体キー指定
            // test
            await QueryObjectsTest(new NbQuery().OrderBy("-data1"));
            Assert.AreEqual(10, _objs.ToList().Count);
            var objs = _objs.ToList();
            for (int i = 145; i <= 100; i -= 5)
            {
                Assert.AreEqual(i, objs[i].Opt<int>("data1", 0));
            }
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalLimit()
        {
            await ITUtil.CreateObjectsOfLimitTest();

            // queryがnull(limit検証)
            // test
            await QueryObjectsTest(null);
            Assert.AreEqual(200, _objs.ToList().Count);

            // リセット
            _objs = null;

            // limit未設定時の上限
            // test
            await QueryObjectsTest(new NbQuery());
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
            Assert.AreEqual(100, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// queryがnull<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalNoQuery()
        {
            var obj = await CreateObject();
            await ITUtil.CreateObjectsOfDataA();
            await DeleteObject(obj);

            // test
            await QueryObjectsTest(null);

            Assert.AreEqual(10, _objs.ToList().Count);
            AssertQueryResult(_objs.ToList());
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// ID検索<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalId()
        {
            var obj = await CreateObject();
            await ITUtil.CreateObjectsOfDataA();

            var query = new NbQuery().EqualTo("_id", obj.Id);
            // test
            await QueryObjectsTest(query);

            Assert.AreEqual(1, _objs.ToList().Count);
            AssertObject(obj, (_objs.ToList())[0]);
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
                    await ITUtil.CreateOnlineObject(json1);
                }
            }

            var json2 = new NbJsonObject()
            {
                {"data1", 3},
                {"data2", 4}
            };
            await ITUtil.CreateOnlineObject(json2);

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
        /// limit + skip + order + count + projection<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalExcludeWhereOption()
        {
            var json = new NbJsonObject()
            {
                {"data1", 121},
                {"data2", 0}
            };
            await ITUtil.CreateOnlineObject(json);
            await ITUtil.CreateObjectsOfDataA();

            var query = new NbQuery().Limit(5).Skip(3).OrderBy("-data1").Projection("-data2");
            // test
            await QueryObjectsWithOptionsTest(query);

            Assert.AreEqual(5, _queryResult.Objects.ToList().Count);
            Assert.AreEqual(11, _queryResult.Count);

            var objs = _queryResult.Objects.ToList();

            // 1～5件
            Assert.AreEqual(130, objs[0].Opt<int>("data1", 0));
            Assert.AreEqual(125, objs[1].Opt<int>("data1", 0));
            Assert.AreEqual(121, objs[2].Opt<int>("data1", 0));
            Assert.False(objs[2].HasKey("data2"));
            Assert.AreEqual(120, objs[3].Opt<int>("data1", 0));
            Assert.AreEqual(115, objs[4].Opt<int>("data1", 0));
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalWhere()
        {
            await CreateObjectsOfDataB();

            var expectedJsonArray1 = new NbJsonArray()
            {
                100, 101, 102
            };
            var expectedJsonArray2 = new NbJsonArray()
            {
                145, 101, 102
            };
            NbJsonArray dataArray;

            // where - （equals)
            // test
            await QueryObjectsTest(new NbQuery().EqualTo("data1", 120));
            Assert.AreEqual(1, _objs.ToList().Count);
            Assert.AreEqual(120, _objs.First().Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $ne
            // test
            await QueryObjectsTest(new NbQuery().NotEquals("data1", 120).OrderBy("data1"));
            var objs = _objs.ToList();
            Assert.AreEqual(10, objs.Count);
            // 1件目
            Assert.AreEqual(100, objs[0].Opt<int>("data2", -1));
            // 2件目
            dataArray = objs[1].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray1, dataArray);
            // 3件目
            dataArray = objs[2].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray2, dataArray);
            // 4件目～10件目
            Assert.AreEqual(105, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(110, objs[4].Opt<int>("data1", -1));
            Assert.AreEqual(115, objs[5].Opt<int>("data1", -1));
            Assert.AreEqual(125, objs[6].Opt<int>("data1", -1));
            Assert.AreEqual(130, objs[7].Opt<int>("data1", -1));
            Assert.AreEqual(135, objs[8].Opt<int>("data1", -1));
            Assert.AreEqual(140, objs[9].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $lt
            // test
            await QueryObjectsTest(new NbQuery().LessThan("data1", 120).OrderBy("data1"));
            objs = _objs.ToList();
            Assert.AreEqual(5, objs.Count);
            // 1件目
            dataArray = objs[0].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray1, dataArray);
            // 2件目
            dataArray = objs[1].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray2, dataArray);
            // 3件目～5件目
            Assert.AreEqual(105, objs[2].Opt<int>("data1", -1));
            Assert.AreEqual(110, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(115, objs[4].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $lte
            // test
            await QueryObjectsTest(new NbQuery().LessThanOrEqual("data1", 120).OrderBy("data1"));
            objs = _objs.ToList();
            Assert.AreEqual(6, objs.Count);
            // 1件目
            dataArray = objs[0].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray1, dataArray);
            // 2件目
            dataArray = objs[1].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray2, dataArray);
            // 3件目～6件目
            Assert.AreEqual(105, objs[2].Opt<int>("data1", -1));
            Assert.AreEqual(110, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(115, objs[4].Opt<int>("data1", -1));
            Assert.AreEqual(120, objs[5].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $gt
            // test
            await QueryObjectsTest(new NbQuery().GreaterThan("data1", 120).OrderBy("data1"));
            objs = _objs.ToList();
            Assert.AreEqual(5, objs.Count);
            // 1件目
            dataArray = objs[0].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray2, dataArray);
            // 2～5件目
            Assert.AreEqual(125, objs[1].Opt<int>("data1", -1));
            Assert.AreEqual(130, objs[2].Opt<int>("data1", -1));
            Assert.AreEqual(135, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(140, objs[4].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $gte
            // test
            await QueryObjectsTest(new NbQuery().GreaterThanOrEqual("data1", 120).OrderBy("data1"));
            objs = _objs.ToList();
            Assert.AreEqual(6, objs.Count);
            // 1件目
            dataArray = objs[0].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray2, dataArray);
            // 2～6件目
            Assert.AreEqual(120, objs[1].Opt<int>("data1", -1));
            Assert.AreEqual(125, objs[2].Opt<int>("data1", -1));
            Assert.AreEqual(130, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(135, objs[4].Opt<int>("data1", -1));
            Assert.AreEqual(140, objs[5].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $in
            // test
            var query = new NbQuery().In("data1", 120, 130, 135, 150).OrderBy("data1");
            await QueryObjectsTest(query);
            objs = _objs.ToList();
            Assert.AreEqual(3, objs.Count);
            // 1～3件目
            Assert.AreEqual(120, objs[0].Opt<int>("data1", -1));
            Assert.AreEqual(130, objs[1].Opt<int>("data1", -1));
            Assert.AreEqual(135, objs[2].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $all
            // test
            query = new NbQuery().All("data1", 101, 102).OrderBy("data1");
            await QueryObjectsTest(query);
            objs = _objs.ToList();
            Assert.AreEqual(2, objs.Count);
            dataArray = objs[0].Opt<NbJsonArray>("data1", null);
            if (dataArray[0].Equals((long)100))
            {
                // 1件目
                Assert.AreEqual(101, dataArray[1]);
                Assert.AreEqual(102, dataArray[2]);
                // 2件目
                dataArray = objs[1].Opt<NbJsonArray>("data1", null);
                Assert.AreEqual(145, dataArray[0]);
                Assert.AreEqual(101, dataArray[1]);
                Assert.AreEqual(102, dataArray[2]);
            }
            else
            {
                // 1件目
                Assert.AreEqual(145, dataArray[0]);
                Assert.AreEqual(101, dataArray[1]);
                Assert.AreEqual(102, dataArray[2]);
                // 2件目
                dataArray = objs[1].Opt<NbJsonArray>("data1", null);
                Assert.AreEqual(145, dataArray[0]);
                Assert.AreEqual(101, dataArray[1]);
                Assert.AreEqual(102, dataArray[2]);
            }

            // リセット
            _objs = null;

            // where - $exists (false)
            // test
            await QueryObjectsTest(new NbQuery().NotExists("data1").OrderBy("data1"));
            objs = _objs.ToList();
            Assert.AreEqual(1, objs.Count);
            // 1件目
            Assert.AreEqual(100, objs[0].Opt<int>("data2", -1));

            // リセット
            _objs = null;

            // where - $exists (true)
            // test
            await QueryObjectsTest(new NbQuery().Exists("data1").OrderBy("data1"));
            objs = _objs.ToList();
            Assert.AreEqual(10, objs.Count);
            // 1件目
            dataArray = objs[0].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray1, dataArray);
            // 2件目
            dataArray = objs[1].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray2, dataArray);
            // 3件目～10件目
            Assert.AreEqual(105, objs[2].Opt<int>("data1", -1));
            Assert.AreEqual(110, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(115, objs[4].Opt<int>("data1", -1));
            Assert.AreEqual(120, objs[5].Opt<int>("data1", -1));
            Assert.AreEqual(125, objs[6].Opt<int>("data1", -1));
            Assert.AreEqual(130, objs[7].Opt<int>("data1", -1));
            Assert.AreEqual(135, objs[8].Opt<int>("data1", -1));
            Assert.AreEqual(140, objs[9].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $not
            // test
            query = new NbQuery().Exists("data1").Not("data1").OrderBy("data1");
            await QueryObjectsTest(query);
            objs = _objs.ToList();
            Assert.AreEqual(1, objs.Count);
            // 1件目
            Assert.AreEqual(100, objs[0].Opt<int>("data2", -1));

            // リセット
            _objs = null;

            // where - $or
            // test
            var queryIn = new NbQuery().In("data1", 120, 130, 135, 150);
            var queryAll = new NbQuery().All("data1", 101, 102);
            query = NbQuery.Or(queryIn, queryAll).OrderBy("data1");
            await QueryObjectsTest(query);
            objs = _objs.ToList();
            Assert.AreEqual(5, objs.Count);
            // 1件目
            dataArray = objs[0].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray1, dataArray);
            // 2件目
            dataArray = objs[1].Opt<NbJsonArray>("data1", null);
            Assert.AreEqual(expectedJsonArray2, dataArray);
            // 3件目～5件目
            Assert.AreEqual(120, objs[2].Opt<int>("data1", -1));
            Assert.AreEqual(130, objs[3].Opt<int>("data1", -1));
            Assert.AreEqual(135, objs[4].Opt<int>("data1", -1));

            // リセット
            _objs = null;

            // where - $and
            // test
            var queryLte = new NbQuery().LessThanOrEqual("data1", 120);
            var queryGte = new NbQuery().GreaterThanOrEqual("data1", 120);
            queryIn = new NbQuery().In("data1", 120, 130, 135, 150);
            query = NbQuery.And(queryLte, queryGte, queryIn).OrderBy("data1");
            await QueryObjectsTest(query);
            objs = _objs.ToList();
            Assert.AreEqual(1, objs.Count);
            // 1件目
            Assert.AreEqual(120, objs[0].Opt<int>("data1", -1));
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// where - （$eq)
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalWhereEqualJsonObject()
        {
            var jsonData2 = new NbJsonObject()
            {
                {"data3", 3}
            };
            var json = new NbJsonObject()
            {
                {"data1", 1},
                {"data2", jsonData2}
            };
            var obj = await ITUtil.CreateOnlineObject(json);

            var query = new NbQuery().EqualTo("data2", jsonData2);
            // test
            await QueryObjectsTest(query);

            Assert.AreEqual(1, _objs.ToList().Count);
            AssertObject(obj, _objs.First());
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalWhereRegex()
        {
            await CreateObjectsOfDataC();

            // where - $regex(INSENSITIVITY)
            // test
            var query = new NbQuery().Regex("data1", ".*abc.*", "i").OrderBy("data1");
            await QueryObjectsTest(query);
            var objs = _objs.ToList();
            Assert.AreEqual(4, objs.Count);
            // 1～4件目
            Assert.AreEqual("0123ABC", objs[0].Opt<string>("data1", null));
            Assert.AreEqual("0123abc", objs[1].Opt<string>("data1", null));
            Assert.AreEqual("ABCDEFG", objs[2].Opt<string>("data1", null));
            Assert.AreEqual("abcdefg", objs[3].Opt<string>("data1", null));

            // リセット
            _objs = null;

            // where - $regex(MULTILINE)
            // test
            query = new NbQuery().Regex("data1", "^_.*", "m").OrderBy("data1");
            await QueryObjectsTest(query);
            objs = _objs.ToList();
            Assert.AreEqual(2, objs.Count);
            // 1～2件目
            Assert.AreEqual("012345" + '\n' + "_", objs[0].Opt<string>("data1", null));
            Assert.AreEqual("_012345_", objs[1].Opt<string>("data1", null));
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// where - $regex(DOT_MULTILINE)<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalWhereRegexDotMultiline()
        {
            await CreateObjectsOfDataC();
            await ITUtil.CreateOnlineObject("data1", "_0123456_");

            // test
            var query = new NbQuery().Regex("data1", ".*5._", "s").OrderBy("data1");
            await QueryObjectsTest(query);

            var objs = _objs.ToList();
            Assert.AreEqual(2, objs.Count);

            // 1～2件目
            Assert.AreEqual("012345" + '\n' + "_", objs[0].Opt<string>("data1", null));
            Assert.AreEqual("_0123456_", objs[1].Opt<string>("data1", null));
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// where - $regex(EXTENDED)<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalWhereRegexExtended()
        {
            await CreateObjectsOfDataC();
            await ITUtil.CreateOnlineObject("data1", "_0123456_");

            // test
            var query = new NbQuery().Regex("data1", ".*5._# comment\n", "x").OrderBy("data1");
            await QueryObjectsTest(query);

            var objs = _objs.ToList();
            Assert.AreEqual(1, objs.Count);

            // 1件目
            Assert.AreEqual("_0123456_", objs[0].Opt<string>("data1", null));
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
            var obj = await ITUtil.CreateOnlineObject(json1);

            var json2 = new NbJsonObject()
            {
                {"data1", 3},
                {"data2", jsonData2}
            };
            await ITUtil.CreateOnlineObject(json2);

            // test
            var query = new NbQuery().LessThan("data1", 2).EqualTo("data2", jsonData2);
            await QueryObjectsTest(query);

            Assert.AreEqual(1, _objs.ToList().Count);
            AssertObject(obj, _objs.First());
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// deleteMark<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsNormalDeleteMark()
        {
            var obj = await CreateObject();
            await ITUtil.CreateObjectsOfDataA();
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
        public async void TestQueryObjectsNormalProjection()
        {
            await ITUtil.CreateObjectsOfDataD();

            // test
            await QueryObjectsTest(new NbQuery().Projection("name", "number"));

            var objs = _objs.ToList();
            foreach (var obj in objs)
            {
                Assert.AreEqual(3, obj.Count());
                Assert.True(obj.HasKey("name"));
                Assert.True(obj.HasKey("number"));
                Assert.True(obj.HasKey("_id"));
            }

            _objs = null;

            // test
            await QueryObjectsTest(new NbQuery().Projection("-telno"));

            objs = _objs.ToList();
            foreach (var obj in objs)
            {
                Assert.AreEqual(7, obj.Count());
                Assert.True(obj.HasKey("name"));
                Assert.True(obj.HasKey("number"));
                Assert.True(obj.HasKey("_id"));
                Assert.True(obj.HasKey("ACL"));
                Assert.True(obj.HasKey("createdAt"));
                Assert.True(obj.HasKey("updatedAt"));
                Assert.True(obj.HasKey("etag"));
                Assert.False(obj.HasKey("-telno"));
            }

            _objs = null;

            // test
            await QueryObjectsTest(new NbQuery().Projection("-_id"));

            objs = _objs.ToList();
            foreach (var obj in objs)
            {
                Assert.AreEqual(7, obj.Count());
                Assert.True(obj.HasKey("name"));
                Assert.True(obj.HasKey("number"));
                Assert.True(obj.HasKey("telno"));
                Assert.True(obj.HasKey("ACL"));
                Assert.True(obj.HasKey("createdAt"));
                Assert.True(obj.HasKey("updatedAt"));
                Assert.True(obj.HasKey("etag"));
                Assert.False(obj.HasKey("_id"));
            }

            _objs = null;

            // test
            await QueryObjectsTest(new NbQuery().Projection("telno.home"));

            objs = _objs.ToList();
            foreach (var obj in objs)
            {
                Assert.AreEqual(2, obj.Count());
                Assert.True(obj.HasKey("_id"));
                Assert.True(obj.HasKey("telno"));
                var telno = obj.Opt<NbJsonObject>("telno", null);
                Assert.True(telno.ContainsKey("home"));
                Assert.False(telno.ContainsKey("mobile"));
            }

            _objs = null;

            // test
            await QueryObjectsTest(new NbQuery().Projection("name", "-number"), HttpStatusCode.InternalServerError);

            _objs = null;

            // test
            await QueryObjectsTest(new NbQuery().Projection("name", "-_id"));

            objs = _objs.ToList();
            foreach (var obj in objs)
            {
                Assert.AreEqual(1, obj.Count());
                Assert.True(obj.HasKey("name"));
                Assert.False(obj.HasKey("_id"));
            }

            _objs = null;

            // test
            await QueryObjectsTest(new NbQuery().Projection("name", "aaa"));
            objs = _objs.ToList();
            foreach (var obj in objs)
            {
                Assert.AreEqual(2, obj.Count());
                Assert.True(obj.HasKey("name"));
                Assert.True(obj.HasKey("_id"));
            }
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// マスターキー<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalMasterKey()
        {
            var acl = new NbAcl();
            await ITUtil.CreateObjectsOfDataA(true, acl);
            await ITUtil.CreateObjectBucket(null, new NbContentAcl());

            ITUtil.UseMasterKey();

            // test
            await QueryObjectsTest(new NbQuery());

            Assert.AreEqual(10, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// 権限エラー(1オブジェクト)<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalPermissionRead()
        {
            var acl = new NbAcl();
            await CreateObject(acl);
            await ITUtil.CreateObjectsOfDataA();

            // test
            await QueryObjectsTest(new NbQuery());

            Assert.AreEqual(10, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// 権限エラー(全てのオブジェクト)<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsSubnormalAllPermissionRead()
        {
            var acl = new NbAcl();
            await ITUtil.CreateObjectsOfDataA(true, acl);

            // test

            await QueryObjectsTest(new NbQuery());

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// パス不正<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsExceptionWrongPath()
        {
            await CreateObject();

            _service.TenantId = "dummy";

            // test
            await QueryObjectsTest(new NbQuery(), HttpStatusCode.NotFound);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// HTTPヘッダパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsExceptionWrongHttpHeader()
        {
            await CreateObject();

            _service.AppKey = "dummy";

            // test
            await QueryObjectsTest(new NbQuery(), HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// オブジェクトのクエリ<br/>
        /// リクエストパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestQueryObjectsExceptionWrongHttpParam()
        {
            await CreateObject();

            // test
            var query = new NbQuery().All("testKey", (object[])null);
            await QueryObjectsTest(query, HttpStatusCode.InternalServerError);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// オブジェクトの更新（部分更新）<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormal()
        {
            var obj = await CreateObject();
            var acl = new NbAcl();
            acl.W.Add("testUser");

            var json = new NbJsonObject()
            {
                {"UpdateKey", "UpdateValue"},
                {"ACL", acl.ToJson()}
            };

            // test
            await PartUpdateObjectTest(json, obj);

            Assert.True(_obj.HasKey("UpdateKey"));
            Assert.AreEqual(obj.Id, _obj.Id);
            Assert.AreEqual(obj.CreatedAt, _obj.CreatedAt);
            Assert.AreNotEqual(obj.UpdatedAt, _obj.UpdatedAt);
            Assert.AreNotEqual(obj.Etag, _obj.Etag);
            Assert.True(ITUtil.CompareAcl(acl, _obj.Acl));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// 削除済みマークされたオブジェクトの部分更新<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalDeletedObject()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            await DeleteObject(obj);

            // クエリをかける
            await QueryObjectsTest(new NbQuery().EqualTo("_id", id).DeleteMark(true));

            var deletedObj = _objs.First();
            var time = DateTime.Now.AddHours(-3);
            var updateCreateAt = NbDateUtils.ToString(time);

            var json = new NbJsonObject()
            {
                {"UpdateKey", "UpdateValue"},
                {"createdAt", updateCreateAt}
            };

            // test
            await PartUpdateObjectTest(json, deletedObj);

            Assert.True(_obj.HasKey("UpdateKey"));
            Assert.True(ITUtil.CompareAcl(obj.Acl, _obj.Acl));
            Assert.AreEqual(updateCreateAt, _obj.CreatedAt);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// オプションパラメータ未設定<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectSubnormalNoOptionParams()
        {
            var obj = await CreateObject();
            obj.Etag = null;

            var json = new NbJsonObject()
            {
                {"UpdateKey", "UpdateValue"}
            };

            // test
            await PartUpdateObjectTest(json, obj);

            Assert.True(_obj.HasKey("UpdateKey"));
            Assert.AreEqual(obj.Id, _obj.Id);
            Assert.AreEqual(obj.CreatedAt, _obj.CreatedAt);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $inc<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalInc()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = 1;
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$inc", new NbJsonObject()
                    {
                        {"key1", 5}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            Assert.AreEqual(6, _obj.Opt<int>("key1", -1));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $rename<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalRename()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = 2;
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$rename", new NbJsonObject()
                    {
                        {"key1", "key1_1"}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            Assert.True(_obj.HasKey("key1_1"));
            Assert.False(_obj.HasKey("key1"));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $set<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalSet()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = 2;
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$set", new NbJsonObject()
                    {
                        {"key1", 5}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            Assert.AreEqual(5, _obj.Opt<int>("key1", -1));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $unset<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalUnset()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = 2;
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$unset", new NbJsonObject()
                    {
                        {"key1", ""}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            Assert.False(_obj.HasKey("key1"));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $addToSet<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalAddToSet()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                "electronics", "camera"
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$addToSet", new NbJsonObject()
                    {
                        {"key1", "accessories"}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                "electronics", "camera", "accessories"
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $pop<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalPop()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                8, 9, 10
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$pop", new NbJsonObject()
                    {
                        {"key1", -1}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                9, 10
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $pullAll<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalPullAll()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                0, 2, 5, 5, 1, 0
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$pullAll", new NbJsonObject()
                    {
                        {"key1", new NbJsonArray()
                            { 0, 5 }
                         }
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                2, 1
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $pull<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalPull()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                "vme", "de", "msr", "tsc", "pse", "msr"
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$pull", new NbJsonObject()
                    {
                        {"key1", "msr"}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                "vme", "de", "tsc", "pse"
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $push<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalPush()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                90, 91
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$push", new NbJsonObject()
                    {
                        {"key1", 89}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                90, 91, 89
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $each<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalEach()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                "electronics", "supplies"
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$addToSet", new NbJsonObject()
                    {
                        {"key1", new NbJsonObject()
                            {
                                {"$each", new NbJsonArray()
                                    {"camera", "electronics", "accessory"}
                                }
                            }
                        }
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                "electronics", "supplies", "camera", "accessory"
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $slice<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalSlice()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                80, 90
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$push", new NbJsonObject()
                    {
                        {"key1", new NbJsonObject()
                            {
                                {"$slice", -3},
                                {"$each", new NbJsonArray()
                                    {100, 20}
                                }
                            }
                        }
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                90, 100, 20
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $sort<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalSort()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = new NbJsonArray()
            {
                89, 70, 89, 50
            };
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$push", new NbJsonObject()
                    {
                        {"key1", new NbJsonObject()
                            {
                                {"$sort", -1},
                                {"$each", new NbJsonArray()
                                    {40, 60}
                                }
                            }
                        }
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            var expectedResult = new NbJsonArray()
            {
                89, 89, 70, 60, 50, 40
            };

            Assert.AreEqual(expectedResult, _obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// $bit<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectNormalBit()
        {
            var obj = _onlineBucket.NewObject();
            obj["key1"] = 13;
            obj = await obj.SaveAsync();

            var json = new NbJsonObject()
            {
                {"$bit", new NbJsonObject()
                    {
                        {"key1", new NbJsonObject()
                            {
                                {"and", 10}
                            }
                        }
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj);

            Assert.AreEqual(8, _obj.Opt<int>("key1", -1));
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// パス不正<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectExceptionWrongPath()
        {
            var obj = await CreateObject();

            _service.TenantId = "";

            var json = new NbJsonObject()
            {
                {"UpdateKey", "UpdateValue"}
            };

            // test
            await PartUpdateObjectTest(json, obj, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// HTTPヘッダパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectExceptionWrongHttpHeader()
        {
            var obj = await CreateObject();
            _service.AppKey = "";

            var json = new NbJsonObject()
            {
                {"UpdateKey", "UpdateValue"}
            };

            // test
            await PartUpdateObjectTest(json, obj, HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        ///リクエストパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectExceptionHttpParamWrongEtag()
        {
            var obj = await CreateObject();
            obj.Etag = "dummy";

            var json = new NbJsonObject()
            {
                {"UpdateKey", "UpdateValue"}
            };

            // test
            await PartUpdateObjectTest(json, obj, HttpStatusCode.Conflict, ITUtil.ReasonCodeEtagMismatch);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// リクエストボディデータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectExceptionRequestBodyWithWrongValue()
        {
            var obj = await CreateObject();

            var json = new NbJsonObject()
            {
                {"$t", new NbJsonObject()
                    {
                        {"key1", 5}
                    }
                }
            };

            // test
            await PartUpdateObjectTest(json, obj, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// IDを変更<br/>
        /// </summary>
        [Test]
        public async void TestPartUpdateObjectExceptionRequestBodyWithChangedId()
        {
            var obj = await CreateObject();
            var id = MongoObjectIdGenerator.CreateObjectId();

            var json = new NbJsonObject()
            {
                {"UpdateKey", "UpdateValue"},
                {"_id", id},
            };

            // test
            await PartUpdateObjectTest(json, obj, HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// 部分更新用 JSONがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestPartUpdateObjectExceptionNoJson()
        {
            var obj = await CreateObject();

            // test
            await obj.PartUpdateAsync(null);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// オブジェクトIDがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestPartUpdateObjectExceptionNoObjectId()
        {
            var obj = await CreateObject();
            obj.Id = null;

            var json = new NbJsonObject();

            // test
            await obj.PartUpdateAsync(json);
        }

        /// <summary>
        /// オブジェクトの更新（部分更新）<br/>
        /// バケット名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestPartUpdateObjectExceptionNoBucketName()
        {
            var obj = await CreateObject();
            obj.BucketName = null;

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

            var oldCreatedAt = obj.CreatedAt;

            obj["UpdateKey"] = "UpdateValue";
            var time = DateTime.Now.AddHours(-3);
            var UpdateCreatedAt = NbDateUtils.ToString(time);
            // 作成日時も更新
            obj.CreatedAt = UpdateCreatedAt;

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(_obj.HasKey("UpdateKey"));
            Assert.AreEqual(obj.Id, _obj.Id);
            Assert.AreEqual(UpdateCreatedAt, _obj.CreatedAt);
            Assert.AreNotEqual(obj.UpdatedAt, _obj.UpdatedAt);
            Assert.AreNotEqual(obj.Etag, _obj.Etag);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// データの内容に変更なし<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectNormalNoChange()
        {
            var obj = await CreateObject();

            // test
            await FullUpdateObjectTest(obj);

            Assert.AreNotEqual(obj.UpdatedAt, _obj.UpdatedAt);
            Assert.AreNotEqual(obj.Etag, _obj.Etag);
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

            // クエリをかける
            await QueryObjectsTest(new NbQuery().EqualTo("_id", id).DeleteMark(true));

            var deletedObj = _objs.First();
            deletedObj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(deletedObj);

            Assert.True(_obj.HasKey("UpdateKey"));
            Assert.True(_obj.Deleted);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// オプションパラメータ未設定<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectSubnormalNoOptionParams()
        {
            var obj = await CreateObject();
            obj.Etag = null;
            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj);

            Assert.True(_obj.HasKey("UpdateKey"));
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// マスターキー<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectSubnormalMasterKey()
        {
            var acl = CreateAclRAnonymous();
            var obj = await CreateObject(acl);

            var contentAcl = CreateContentAclRAnonymous();
            await ITUtil.CreateObjectBucket(null, contentAcl);

            obj["UpdateKey"] = "UpdateValue";
            obj.Acl = NbAcl.CreateAclForAnonymous();

            ITUtil.UseMasterKey();

            // test
            await FullUpdateObjectTest(obj);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 権限エラー(バケット)<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionPermissionBucketUpdate()
        {
            var obj = await CreateObject();

            var contentAcl = CreateContentAclRAnonymous();
            await ITUtil.CreateObjectBucket(null, contentAcl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 権限エラー(オブジェクト：admin)<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionPermissionAdmin()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            acl.Admin.Clear();
            var obj = await CreateObject(acl);

            obj.Acl = NbAcl.CreateAclForAuthenticated();

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// 権限エラー(オブジェクト：update)<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionPermissionUpdate()
        {
            var acl = CreateAclRAnonymous();
            var obj = await CreateObject(acl);

            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの更新（完全上書き）<br/>
        /// リクエストボディデータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestFullUpdateObjectExceptionRequestBodyWithWrongValue()
        {
            var obj = await CreateObject();
            obj.Acl = null;
            obj["UpdateKey"] = "UpdateValue";

            // test
            await FullUpdateObjectTest(obj, HttpStatusCode.BadRequest);
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
            Assert.True(_objs.First().Deleted);
            Assert.AreNotEqual(obj.UpdatedAt, _objs.First().UpdatedAt);
            Assert.AreNotEqual(obj.Etag, _objs.First().Etag);
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
        /// オプションパラメータ未設定<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectSubnormalNoOptionParams()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            obj.Etag = null;

            // test
            await DeleteObjectTest(obj, false);

            var query = new NbQuery().EqualTo("_id", id).DeleteMark(true);
            await QueryObjectsTest(query);

            Assert.AreEqual(0, _objs.ToList().Count);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// マスターキー<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectSubnormalMasterKey()
        {
            var acl = CreateAclRAnonymous();
            var obj = await CreateObject(acl);

            var contentAcl = CreateContentAclRAnonymous();
            await ITUtil.CreateObjectBucket(null, contentAcl);

            ITUtil.UseMasterKey();

            // test
            await DeleteObjectTest(obj, true);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// 権限エラー(オブジェクト)<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionPermissionDelete()
        {
            var acl = CreateAclRAnonymous();
            var obj = await CreateObject(acl);

            // test
            await DeleteObjectTest(obj, true, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの削除<br/>
        /// リクエストパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestDeleteObjectExceptionWrongHttpParamEtag()
        {
            var obj = await CreateObject();
            obj.Etag = "dummy";

            // test
            await DeleteObjectTest(obj, true, HttpStatusCode.Conflict, ITUtil.ReasonCodeEtagMismatch);
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
            await ITUtil.CreateObjectsOfDataA();

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(11, _deleteCount);

            await QueryObjectsTest(new NbQuery().DeleteMark(true));
            Assert.AreEqual(11, _objs.ToList().Count);

            var query = new NbQuery().EqualTo("_id", obj.Id).DeleteMark(true);
            await QueryObjectsTest(query);

            Assert.AreNotEqual(obj.UpdatedAt, _objs.First().UpdatedAt);
            Assert.AreNotEqual(obj.Etag, _objs.First().Etag);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 全件物理削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsNormalHardDelete()
        {
            await ITUtil.CreateObjectsOfDataA();

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
            await ITUtil.CreateObjectsOfDataA();

            // test
            var deleteCount = await _onlineBucket.DeleteAsync(new NbQuery());

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
            await ITUtil.CreateObjectsOfDataA();

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
            await ITUtil.CreateObjectsOfDataA();

            // test
            var query = new NbQuery().Limit(3);
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
            await ITUtil.CreateObjectsOfDataA();
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
            await ITUtil.CreateObjectsOfDataA();
            await DeleteAllObjectsTest(new NbQuery(), true);

            // test
            await DeleteAllObjectsTest(new NbQuery(), false);

            Assert.AreEqual(10, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// マスターキー<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalMasterKey()
        {
            var acl = CreateAclRAnonymous();
            await ITUtil.CreateObjectsOfDataA(true, acl);

            var contentAcl = CreateContentAclRAnonymous();
            await ITUtil.CreateObjectBucket(null, contentAcl);

            ITUtil.UseMasterKey();

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(10, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 権限エラー(バケット)<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsExceptionPermissionBucketDelete()
        {
            await ITUtil.CreateObjectsOfDataA();

            var contentAcl = CreateContentAclRAnonymous();
            await ITUtil.CreateObjectBucket(null, contentAcl);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 権限エラー(1オブジェクト)<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalPermissionDelete()
        {
            var acl = CreateAclRAnonymous();
            await CreateObject(acl);
            await ITUtil.CreateObjectsOfDataA();

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(10, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// 権限エラー(全てのオブジェクト)<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsSubnormalAllPermissionDelete()
        {
            var acl = CreateAclRAnonymous();
            await ITUtil.CreateObjectsOfDataA(true, acl);

            // test
            await DeleteAllObjectsTest(new NbQuery(), true);

            Assert.AreEqual(0, _deleteCount);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// パス不正<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsExceptionWrongPath()
        {
            await CreateObject();

            var onlineBucket = new NbObjectBucket<NbObject>("NotExistBucket");
            try
            {
                // test
                await onlineBucket.DeleteAsync(new NbQuery(), true);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// HTTPヘッダパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsExceptionWrongHttpHeader()
        {
            await CreateObject();

            _service.AppId = "dummy";

            // test
            await DeleteAllObjectsTest(new NbQuery(), true, HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// リクエストパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestDeleteAllObjectsExceptionWrongHttpParam()
        {
            await CreateObject();

            var query = new NbQuery().All("testKey", (object[])null);

            // test
            await DeleteAllObjectsTest(query, true, HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// オブジェクトの一括削除<br/>
        /// バケット名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteAllObjectsExceptionNoQuery()
        {
            await _onlineBucket.DeleteAsync(null, true);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 追加要求<br/>
        /// </summary>
        [Test]
        public void TestBatchAddInsertRequestNormal()
        {
            var obj = new NbObject(BucketName);
            var acl = NbAcl.CreateAclForAnonymous();
            obj["key"] = "value";
            obj.Acl = acl;

            // test
            var req = new NbBatchRequest().AddInsertRequest(obj);

            var expectedReq = new NbJsonObject()
            {
                {"requests", new NbJsonArray()
                    {
                        new NbJsonObject()
                        {
                            {"op", "insert"},
                            {"data", new NbJsonObject()
                                {
                                    {"key", "value"},
                                    {"ACL", acl.ToJson()}
                                }
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(1, req.Requests.Count);
            Assert.AreEqual(expectedReq, req.Json);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 追加要求<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestBatchAddInsertRequestExceptionNoObject()
        {
            // test
            var req = new NbBatchRequest().AddInsertRequest(null);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 追加要求<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestBatchAddInsertRequestExceptionContainsEtag()
        {
            var obj = new NbObject(BucketName);
            obj.Etag = "dummy";

            // test
            var req = new NbBatchRequest().AddInsertRequest(obj);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 更新要求<br/>
        /// </summary>
        [Test]
        public async void TestBatchAddUpdateRequestNormalContainsEtag()
        {
            var obj = await CreateObject();
            obj["updateKey"] = 1;
            var acl = NbAcl.CreateAclForAnonymous();
            obj.Acl = acl;
            var id = obj.Id;
            var etag = obj.Etag;
            var data = obj.ToJson();

            // test
            var req = new NbBatchRequest().AddUpdateRequest(obj);

            var expectedReq = new NbJsonObject()
            {
                {"requests", new NbJsonArray()
                    {
                        new NbJsonObject()
                        {
                            {"op", "update"},
                            {"_id", id},
                            {"etag", etag},
                            {"data", new NbJsonObject()
                                {
                                    {"$full_update", data}
                                }
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(1, req.Requests.Count);
            Assert.AreEqual(expectedReq, req.Json);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 更新要求<br/>
        /// </summary>
        [Test]
        public async void TestBatchAddUpdateRequestNormal()
        {
            var obj = await CreateObject();
            obj["updateKey"] = 1;
            var acl = NbAcl.CreateAclForAnonymous();
            obj.Acl = acl;
            obj.Etag = null;
            var id = obj.Id;
            var data = obj.ToJson();

            // test
            var req = new NbBatchRequest().AddUpdateRequest(obj);

            var expectedReq = new NbJsonObject()
            {
                {"requests", new NbJsonArray()
                    {
                        new NbJsonObject() 
                        {
                            {"op", "update"},
                            {"_id", id},
                            {"data", new NbJsonObject()
                                {
                                    {"$full_update", data}
                                }
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(1, req.Requests.Count);
            Assert.AreEqual(expectedReq, req.Json);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 更新要求<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestBatchAddUpdateRequestExceptionNoObject()
        {
            // test
            var req = new NbBatchRequest().AddUpdateRequest(null);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 更新要求<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestBatchAddUpdateRequestExceptionNoObjectId()
        {
            var obj = new NbObject(BucketName);

            // test
            var req = new NbBatchRequest().AddUpdateRequest(obj);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 削除要求<br/>
        /// </summary>
        [Test]
        public async void TestBatchAddDeleteRequestNormalContainsEtag()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            var etag = obj.Etag;

            // test
            var req = new NbBatchRequest().AddDeleteRequest(obj);

            var expectedReq = new NbJsonObject()
            {
                {"requests", new NbJsonArray()
                    {
                        new NbJsonObject()
                        {
                            {"op", "delete"},
                            {"_id", id},
                            {"etag", etag}
                        }
                    }
                }
            };

            Assert.AreEqual(1, req.Requests.Count);
            Assert.AreEqual(expectedReq, req.Json);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 削除要求<br/>
        /// </summary>
        [Test]
        public async void TestBatchAddDeleteRequestNormal()
        {
            var obj = await CreateObject();
            obj.Etag = null;
            var id = obj.Id;

            // test
            var req = new NbBatchRequest().AddDeleteRequest(obj);

            var expectedReq = new NbJsonObject()
            {
                {"requests", new NbJsonArray()
                    {
                        new NbJsonObject()
                        {
                            {"op", "delete"},
                            {"_id", id},
                        }
                    }
                }
            };

            Assert.AreEqual(1, req.Requests.Count);
            Assert.AreEqual(expectedReq, req.Json);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 削除要求<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestBatchAddDeleteRequestExceptionNoObject()
        {
            // test
            var req = new NbBatchRequest().AddDeleteRequest(null);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// 削除要求<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestBatchAddDeleteRequestExceptionNoObjectId()
        {
            var obj = new NbObject(BucketName);

            // test
            var req = new NbBatchRequest().AddDeleteRequest(obj);
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// バッチリクエスト作成<br/>
        /// </summary>
        [Test]
        public async void TestBatchRequestNormal()
        {
            // create
            var creteObj = new NbObject(BucketName);
            var acl = NbAcl.CreateAclForAnonymous();
            creteObj["key"] = "value";
            creteObj.Acl = acl;

            // Update
            var updateObj = await CreateObject();
            updateObj["updateKey"] = 1;
            updateObj.Acl = acl;
            var updateId = updateObj.Id;
            var updateEtag = updateObj.Etag;
            var data = updateObj.ToJson();

            // Delete
            var deleteObj = await CreateObject();
            var deleteId = deleteObj.Id;
            var deleteEtag = deleteObj.Etag;

            // test
            var req = new NbBatchRequest().AddInsertRequest(creteObj);
            req.AddUpdateRequest(updateObj);
            req.AddDeleteRequest(deleteObj);

            var expectedCreateJson = new NbJsonObject()
            {
                {"op", "insert"},
                {"data", new NbJsonObject()
                    {
                        {"key", "value"},
                        {"ACL", acl.ToJson()}
                    }
                }
            };

            var expectedUpdateJson = new NbJsonObject()
            {
                {"op", "update"},
                {"_id", updateId},
                {"etag", updateEtag},
                {"data", new NbJsonObject()
                    {
                        {"$full_update", data}
                    }
                }
            };

            var expectedDeleteJson = new NbJsonObject()
            {
                {"op", "delete"},
                {"_id", deleteId},
                {"etag", deleteEtag}
            };

            var expectedReq = new NbJsonObject()
            {
                {"requests", new NbJsonArray()
                    {
                        expectedCreateJson, expectedUpdateJson, expectedDeleteJson
                    }
                }
            };

            Assert.AreEqual(3, req.Requests.Count);
            Assert.AreEqual(expectedReq, req.Json);

            Assert.AreEqual("insert", req.GetOp(0));
            Assert.AreEqual("update", req.GetOp(1));
            Assert.AreEqual("delete", req.GetOp(2));
        }

        /// <summary>
        /// バッチリクエスト作成<br/>
        /// オペレーション取得<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetOpExceptionOutOfRange()
        {
            var obj = new NbObject(BucketName);
            var acl = NbAcl.CreateAclForAnonymous();
            obj["key"] = "value";
            obj.Acl = acl;
            obj.Id = "id";

            var req = new NbBatchRequest().AddInsertRequest(obj);
            req.AddUpdateRequest(obj);
            req.AddDeleteRequest(obj);

            // test
            req.GetOp(4);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// バッチオペレーション<br/>
        /// バッチオペレーション<br/>
        /// </summary>
        [Test]
        public async void TestBatchNormal()
        {
            var req = await CreateBatchRequest();

            // test
            await BatchTest(req, true);

            Assert.AreEqual(3, _batchResult.Count);

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(NbBatchResult.ResultCode.Ok, _batchResult[i].Result);
                Assert.AreEqual(NbBatchResult.ReasonCode.Unknown, _batchResult[i].Reason);

                var obj = new NbObject(BucketName).FromJson(_batchResult[i].Data);

                Assert.AreEqual(obj.Etag, _batchResult[i].Etag);
                Assert.AreEqual(obj.UpdatedAt, _batchResult[i].UpdatedAt);

                if (i == 0)
                {
                    Assert.True(obj.HasKey("create"));
                }
                else if (i == 1)
                {
                    Assert.True(obj.HasKey("update"));
                }
                else if (i == 2)
                {
                    Assert.True(obj.Deleted);
                }
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// 論理削除<br/>
        /// </summary>
        [Test]
        public async void TestBatchNormalSoftDelete()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            var req = new NbBatchRequest().AddDeleteRequest(obj);

            // test
            await BatchTest(req, true);

            Assert.AreEqual(NbBatchResult.ResultCode.Ok, _batchResult[0].Result);

            var queryResult = await _onlineBucket.QueryAsync(new NbQuery().EqualTo("_id", id).DeleteMark(true));
            Assert.AreEqual(1, queryResult.ToList().Count);
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// 物理削除<br/>
        /// </summary>
        [Test]
        public async void TestBatchNormalHardDelete()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            var req = new NbBatchRequest().AddDeleteRequest(obj);

            // test
            await BatchTest(req, false);

            Assert.AreEqual(NbBatchResult.ResultCode.Ok, _batchResult[0].Result);

            var queryResult = await _onlineBucket.QueryAsync(new NbQuery().EqualTo("_id", id).DeleteMark(true));
            Assert.AreEqual(0, queryResult.ToList().Count);
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// 削除の指定なし<br/>
        /// </summary>
        [Test]
        public async void TestBatchNormalSoftDeleteUnset()
        {
            var obj = await CreateObject();
            var id = obj.Id;
            var req = new NbBatchRequest().AddDeleteRequest(obj);

            try
            {
                await _onlineBucket.BatchAsync(req);
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }

            var queryResult = await _onlineBucket.QueryAsync(new NbQuery().EqualTo("_id", id).DeleteMark(true));

            Assert.AreEqual(1, queryResult.ToList().Count);
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// 同じリクエストトークン<br/>
        /// </summary>
        [Test]
        public async void TestBatchNormalSameRequestToken()
        {
            var obj = await CreateObject();
            obj["update"] = 1;
            var req = new NbBatchRequest().AddUpdateRequest(obj);

            try
            {
                await _onlineBucket.BatchAsync(req);
                await _onlineBucket.BatchAsync(req);
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// マスターキー<br/>
        /// </summary>
        [Test]
        public async void TestBatchSubnormalMasterKey()
        {
            var req = await CreateBatchRequest();

            var contentAcl = CreateContentAclRAnonymous();
            await ITUtil.CreateObjectBucket(null, contentAcl);

            ITUtil.UseMasterKey();

            // test
            await BatchTest(req, true);

            Assert.AreEqual(3, _batchResult.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(NbBatchResult.ResultCode.Ok, _batchResult[i].Result);
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// 権限エラー(バケット)<br/>
        /// </summary>
        [Test]
        public async void TestBatchSubnormalPermissionBucket()
        {
            var req = await CreateBatchRequest();

            var contentAcl = CreateContentAclRAnonymous();
            await ITUtil.CreateObjectBucket(null, contentAcl);

            // test
            await BatchTest(req, true);

            Assert.AreEqual(3, _batchResult.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, _batchResult[i].Result);
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// 権限エラー(オブジェクト)<br/>
        /// </summary>
        [Test]
        public async void TestBatchSubnormalPermissionObject()
        {
            var acl = CreateAclRAnonymous();

            // Update
            var updateObj = await CreateObject(acl);
            updateObj["update"] = 1;
            updateObj.Acl = NbAcl.CreateAclForAnonymous();

            // Delete
            var deleteObj = await CreateObject(acl);

            var req = new NbBatchRequest().AddUpdateRequest(updateObj);
            req.AddDeleteRequest(deleteObj);

            // test
            await BatchTest(req, true);

            Assert.AreEqual(2, _batchResult.Count);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, _batchResult[i].Result);
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// パス不正<br/>
        /// </summary>
        [Test]
        public async void TestBatchSubnormalWrongPath()
        {
            var req = await CreateBatchRequest();
            var bucket = new NbObjectBucket<NbObject>("NotExistBucket");

            // test
            try
            {
                var batchResult = await bucket.BatchAsync(req);
                Assert.AreEqual(3, batchResult.Count);
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(NbBatchResult.ResultCode.NotFound, batchResult[i].Result);
                }
            }
            catch (Exception)
            {
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// HTTPヘッダパラメータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestBatchExceptionWrongHttpHeader()
        {
            var req = await CreateBatchRequest();

            _service.AppKey = "dummy";

            // test
            await BatchTest(req, true, HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// リクエストボディデータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestBatchSubnormalRequestBodyWithWrongValueDuplicateId()
        {
            var obj = await CreateObject();
            obj.Etag = null;
            var req = new NbBatchRequest().AddInsertRequest(obj);

            // test
            await BatchTest(req, true);

            Assert.AreEqual(1, _batchResult.Count);
            for (int i = 0; i < 1; i++)
            {
                Assert.AreEqual(NbBatchResult.ResultCode.Conflict, _batchResult[i].Result);
                Assert.AreEqual(NbBatchResult.ReasonCode.DuplicateId, _batchResult[i].Reason);
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// リクエストボディデータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestBatchSubnormalRequestBodyWithWrongValueEtag()
        {
            var obj = await CreateObject();

            // Update
            obj["update"] = 1;

            var req = new NbBatchRequest().AddUpdateRequest(obj);
            req.AddDeleteRequest(obj);

            // test
            await BatchTest(req, true);

            Assert.AreEqual(2, _batchResult.Count);

            Assert.AreEqual(NbBatchResult.ResultCode.Ok, _batchResult[0].Result);
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, _batchResult[1].Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.EtagMismatch, _batchResult[1].Reason);
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// リクエストボディデータ誤り<br/>
        /// </summary>
        [Test]
        public async void TestBatchSubnormalRequestBodyWithWrongValueUpdateNoAcl()
        {
            var obj = await CreateObject();
            obj.Acl = null;

            var req = new NbBatchRequest().AddUpdateRequest(obj);

            // test
            await BatchTest(req, true);

            Assert.AreEqual(1, _batchResult.Count);
            for (int i = 0; i < 1; i++)
            {
                Assert.AreEqual(NbBatchResult.ResultCode.BadRequest, _batchResult[i].Result);
            }
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// バッチリクエストがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestBatchExceptionNoRequest()
        {
            // test
            await _onlineBucket.BatchAsync(null);
        }

        /// <summary>
        /// バッチオペレーション<br/>
        /// バッチ処理対象のデータが含まれない<br/>
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestBatchExceptionEmptyRequest()
        {
            var req = new NbBatchRequest();

            // test
            await _onlineBucket.BatchAsync(req);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 初期値<br/>
        /// NbObject<br/>
        /// </summary>
        [Test]
        public void TestNbObjectNormal()
        {
            // test
            var obj = new NbObject(BucketName);

            Assert.AreEqual(BucketName, obj.BucketName);
            Assert.Null(obj.Id);
            Assert.Null(obj.Acl);
            Assert.Null(obj.Etag);
            Assert.Null(obj.CreatedAt);
            Assert.Null(obj.UpdatedAt);
            Assert.False(obj.Deleted);
        }

        /// <summary>
        /// 初期値<br/>
        /// NbObjectBucket<br/>
        /// </summary>
        [Test]
        public void TestNbObjectBucketNormal()
        {
            // test
            var bucket = new NbObjectBucket<NbObject>(BucketName);

            Assert.AreEqual(BucketName, bucket.BucketName);
        }

        /// <summary>
        /// 初期値<br/>
        /// NbQuery<br/>
        /// </summary>
        [Test]
        public void TestNbQueryNormal()
        {
            // test
            var query = new NbQuery();

            Assert.NotNull(query.Conditions);
            Assert.Null(query.ProjectionValue);
            Assert.Null(query.Order);
            Assert.AreEqual(-1, query.LimitValue);
            Assert.AreEqual(-1, query.SkipValue);
            Assert.False(query.DeleteMarkValue);
        }

        /// <summary>
        /// 初期値<br/>
        /// NbBatchRequest<br/>
        /// </summary>
        [Test]
        public void TestNbBatchRequestNormal()
        {
            // test
            var req = new NbBatchRequest();

            Assert.NotNull(req.Json);
            Assert.NotNull(req.Requests);
            Assert.NotNull(req.RequestToken);
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
            var obj = new NbObject(BucketName)
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
            var obj = new NbObject(BucketName);
            // test
            obj["data1"] = "abcde";

            Assert.AreEqual("abcde", obj["data1"]);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// インデクサ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestIndexerExceptionGetKeyNull()
        {
            var obj = new NbObject(BucketName);
            // test
            var v = obj[null];
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// インデクサ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestIndexerExceptionSetKeyNull()
        {
            var obj = new NbObject(BucketName);
            // test
            obj[null] = "abcde";
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// インデクサ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestIndexerExceptionSetInvalidKey()
        {
            var obj = new NbObject(BucketName);
            // test
            obj[""] = "abcde";
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// foreach<br/>
        /// </summary>
        [Test]
        public void TestNbObjectNormalForeach()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;
            obj["key2"] = 123456;

            // test
            var count = 0;
            foreach (var key_value in obj)
            {
                count++;
            }
            Assert.AreEqual(2, count);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// LINQ<br/>
        /// </summary>
        [Test]
        public void TestNbObjectNormalLINQ()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;
            obj["key2"] = "test";

            // test
            var v = from x in obj where x.Key == "key1" select x.Value;

            Assert.AreEqual(1, v.Count());
            Assert.AreEqual(123456, v.First());
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// HasKey<br/>
        /// </summary>
        [Test]
        public void TestHasKeyNormal()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            Assert.True(obj.HasKey("key1"));
            Assert.False(obj.HasKey("key2"));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// HasKey<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestHasKeyExceptionKeyNull()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            obj.HasKey(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Get<T><br/>
        /// </summary>
        [Test]
        public void TestGetNormal()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 120L;

            // test
            Assert.AreEqual(120L, obj.Get<long>("key1"));
            Assert.AreEqual(120, obj.Get<int>("key1"));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Get<T><br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetExceptionKeyNull()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            obj.Get<int>(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Get<T><br/>
        /// </summary>
        [Test, ExpectedException(typeof(KeyNotFoundException))]
        public void TestGetExceptionKeyNotFound()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            obj.Get<float>("key2");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// TryGet<T><br/>
        /// </summary>
        [Test]
        public void TestTryGetNormal()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = false;

            // test
            bool value;
            Assert.True(obj.TryGet<bool>("key1", out value));
            Assert.False(value);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// TryGet<T><br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestTryGetExceptionKeyNull()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            string value;
            obj.TryGet<string>(null, out value);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// TryGet<T><br/>
        /// </summary>
        [Test]
        public void TestTryGetSubnormalKeyNotFound()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            string value;
            Assert.False(obj.TryGet<string>("key2", out value));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// TryGet<T><br/>
        /// </summary>
        [Test]
        public void TestTryGetSubnormalInvalidType()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            string value;
            Assert.False(obj.TryGet<string>("key1", out value));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Opt<T><br/>
        /// </summary>
        [Test]
        public void TestOptNormal()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            Assert.AreEqual(123456, obj.Opt<int>("key1", 0));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Opt<T><br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOptExceptionKeyNull()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            obj.Opt<long>(null, -1);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Opt<T><br/>
        /// </summary>
        [Test]
        public void TestOptSubnormalKeyNotFound()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            Assert.AreEqual(-1, obj.Opt<int>("key2", -1));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Set<br/>
        /// </summary>
        [Test]
        public void TestSetNormal()
        {
            var obj = new NbObject(BucketName);
            var json = new NbJsonObject()
            {
                {"key2", "abcdeg"}
            };
            // test
            obj.Set("key1", json);

            Assert.AreEqual(json, obj.Opt<NbJsonObject>("key1", null));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Set<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetExceptionKeyNull()
        {
            var obj = new NbObject(BucketName);

            // test
            obj.Set(null, "abcde");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Set<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestSetExceptionInvalidKey()
        {
            var obj = new NbObject(BucketName);

            // test
            obj.Set("abc.", 12345);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Remove<br/>
        /// </summary>
        [Test]
        public void TestRemoveNormal()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = true;

            Assert.True(obj.HasKey("key1"));

            // test
            Assert.True(obj.Remove("key1"));

            Assert.False(obj.HasKey("key1"));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Remove<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRemoveExceptionKeyNull()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = 123456;

            // test
            obj.Remove(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Remove<br/>
        /// </summary>
        [Test]
        public void TestRemoveSubnormalKeyNotFound()
        {
            var obj = new NbObject(BucketName);
            obj["key1"] = true;

            // test
            Assert.False(obj.Remove("key2"));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Add<br/>
        /// </summary>
        [Test]
        public void TestAddNormal()
        {
            var obj = new NbObject(BucketName);
            var jsonArray = new NbJsonArray()
            {
                1, 2, 3
            };
            // test
            obj.Add("key1", jsonArray);

            Assert.AreEqual(jsonArray, obj.Opt<NbJsonArray>("key1", null));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Add<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddExceptionKeyNull()
        {
            var obj = new NbObject(BucketName);

            // test
            obj.Add(null, "abcde");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// Add<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestAddExceptionInvalidKey()
        {
            var obj = new NbObject(BucketName);

            // test
            obj.Set("$test", 12345);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// JSON出力<br/>
        /// </summary>
        [Test]
        public async void TestToJsonNormal()
        {
            // test
            var obj = await ITUtil.CreateOnlineObject("key", "value");

            var json = obj.ToJson();

            Assert.AreEqual(6, json.Count);
            AssertObject(obj, new NbObject(BucketName, json));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// JSON出力<br/>
        /// </summary>
        [Test]
        public void TestToJsonNormalEmpty()
        {
            // test
            var obj = new NbObject(BucketName);

            var json = obj.ToJson();

            Assert.IsEmpty(json);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// オブジェクト生成<br/>
        /// </summary>
        [Test]
        public void TestNewObjectNormal()
        {
            // test
            var obj = _onlineBucket.NewObject();

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
            var obj = new NbObjectBucket<Person>().NewObject();

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
            var obj = new Person()
            {
                Name = "Taro Nichiden"
            };
            // 複数のバケットを作成したくなので、バケット名を入れ替える
            obj.BucketName = BucketName;

            // create
            var create = await obj.SaveAsync();

            // query
            var bucket = new NbObjectBucket<Person>(BucketName);
            var results = await bucket.QueryAsync(new NbQuery());
            Assert.AreEqual(1, results.ToList().Count);
            Assert.AreEqual("Taro Nichiden", results.First().Name);

            // update
            create["Age"] = 22;
            var updateObj = await create.SaveAsync();

            // delete
            await updateObj.DeleteAsync();
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestProjectionExceptionProjectionNull()
        {
            // test
            var query = new NbQuery().Projection(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestProjectionExceptionContainsNull()
        {
            // test
            var query = new NbQuery().Projection("a", null, "b");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOrderByExceptionOrderNull()
        {
            // test
            var query = new NbQuery().OrderBy(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestOrderByExceptionContainsNull()
        {
            // test
            var query = new NbQuery().OrderBy("a", null, "-b");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConditionsExceptionNull()
        {
            var query = new NbQuery();

            // test
            query.Conditions = null;
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestEqualToExceptionKeyNull()
        {
            // test
            var query = new NbQuery().EqualTo(null, 1);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotEqualsExceptionKeyNull()
        {
            // test
            var query = new NbQuery().NotEquals(null, "abc");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestLessThanExceptionKeyNull()
        {
            // test
            var query = new NbQuery().LessThan(null, 1);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestLessThanOrEqualExceptionKeyNull()
        {
            // test
            var query = new NbQuery().LessThanOrEqual(null, 2);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGreaterThanExceptionKeyNull()
        {
            // test
            var query = new NbQuery().GreaterThan(null, 2);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGreaterThanOrEqualExceptionKeyNull()
        {
            // test
            var query = new NbQuery().GreaterThanOrEqual(null, 1);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestInExceptionKeyNull()
        {
            // test
            var query = new NbQuery().In(null, "a", "b");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAllExceptionKeyNull()
        {
            // test
            var query = new NbQuery().All(null, "a", "b");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestExistsExceptionKeyNull()
        {
            // test
            var query = new NbQuery().Exists(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotExistsExceptionKeyNull()
        {
            // test
            var query = new NbQuery().NotExists(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRegexExceptionKeyNull()
        {
            // test
            var query = new NbQuery().Regex(null, "abc");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRegexExceptionRegexpNull()
        {
            // test
            var query = new NbQuery().Regex("data1", null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOrExceptionQueriesNull()
        {
            // test
            var query = NbQuery.Or(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestOrExceptionContainsNull()
        {
            // test
            var query = NbQuery.Or(new NbQuery().EqualTo("a", 1), null, new NbQuery().EqualTo("b", 2));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAndExceptionQueriesNull()
        {
            // test
            var query = NbQuery.And(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestAndExceptionContainsNull()
        {
            // test
            var query = NbQuery.And(new NbQuery().EqualTo("a", 1), null, new NbQuery().EqualTo("b", 2));
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotExceptionKeyNull()
        {
            // test
            var query = new NbQuery().Not(null);
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestNotExceptionKeyNotFound()
        {
            // test
            var query = new NbQuery().GreaterThan("key1", 100).Not("key2");
        }

        /// <summary>
        /// 基本動作・パラメータ関連<br/>
        /// クエリ<br/>
        /// </summary>
        [Test]
        public void TestNbQueryToStringNormal()
        {
            var query = new NbQuery().EqualTo("a", 1).Projection("b", "c").OrderBy("d", "-e").Limit(100).Skip(200).DeleteMark(true);

            // test
            var jsonString = query.ToString();

            var json = NbJsonObject.Parse(jsonString);

            Assert.AreEqual(1, json.GetJsonObject("where")["a"]);
            Assert.AreEqual(new NbJsonArray() { "b", "c" }, json.GetArray("projection"));
            Assert.AreEqual(new NbJsonArray() { "d", "-e" }, json.GetArray("order"));
            Assert.AreEqual(100, json["limit"]);
            Assert.AreEqual(200, json["skip"]);
            Assert.True(json.Get<bool>("deleteMark"));
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

            var obj = new NbObject(BucketName)
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
            var result = await _onlineBucket.QueryAsync(query);
            Assert.AreEqual(1, result.ToList().Count);
            Assert.AreEqual(id, result.First().Id);

            query = new NbQuery().EqualTo("other.fish", "\uD867\uDE3D");
            result = await _onlineBucket.QueryAsync(query);
            Assert.AreEqual(1, result.ToList().Count);

            query = new NbQuery().EqualTo("other.mark.mark1", " !\"#$%&'()*+-.,/:;<=>?@[]^_`{|}~");
            result = await _onlineBucket.QueryAsync(query);
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
                var obj = _onlineBucket.NewObject();
                obj["key"] = "value";
                obj = await obj.SaveAsync();
                Assert.NotNull(obj.Id);

                // Update
                obj["updateKey"] = "updateValue";
                var updateObj = await obj.SaveAsync();
                Assert.True(updateObj.HasKey("updateKey"));

                // Get
                var getObj = await _onlineBucket.GetAsync(updateObj.Id);
                Assert.True(getObj.HasKey("updateKey"));

                // Query
                var queryResult = await _onlineBucket.QueryAsync(new NbQuery().EqualTo("_id", getObj.Id).DeleteMark(true));
                Assert.AreEqual(1, queryResult.ToList().Count);

                // DeleteObject
                await getObj.DeleteAsync(true);
            }

            for (int i = 0; i < 3; i++)
            {
                var obj1 = _onlineBucket.NewObject();
                obj1["batchKey1"] = true;
                var obj2 = _onlineBucket.NewObject();
                obj2["batchKey2"] = false;

                var batchRequest = new NbBatchRequest().AddInsertRequest(obj1);
                batchRequest.AddInsertRequest(obj2);

                // Batch
                await _onlineBucket.BatchAsync(batchRequest);

                // DeleteAllObjecsts
                var deleteResult = await _onlineBucket.DeleteAsync(new NbQuery().DeleteMark(true), false);
                Assert.GreaterOrEqual(deleteResult, 2);
            }
        }

        /// <summary>
        /// 繰り返し評価<br/>
        /// </summary>
        [Test]
        public async void TestUpdateAndDeleteExceptionEtagMismatch()
        {
            // Create
            var obj = _onlineBucket.NewObject();
            obj["key"] = "value";
            obj = await obj.SaveAsync();

            // Update(awaitしない)
            obj["key1"] = "value2";
            var t1 = obj.SaveAsync();

            // Delete
            try
            {
                await obj.DeleteAsync();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
                Assert.AreEqual(ITUtil.ReasonCodeEtagMismatch, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
            }

            await t1;
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// オブジェクトを作成する
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <returns>NbObject</returns>
        private async Task<NbObject> CreateObject(NbAcl acl = null)
        {
            var obj = new NbObject(BucketName);

            return await ITUtil.CreateOnlineObject("testKey", "testValue", acl);
        }

        /// <summary>
        /// オブジェクトを削除する
        /// </summary>
        /// <param name="obj">NbObject</param>
        /// <param name="softDelete">論理削除の場合はtrue</param>
        /// <returns>Task</returns>
        private async Task DeleteObject(NbObject obj = null, bool softDelete = true)
        {
            await DeleteObjectTest(obj, softDelete);
        }

        /// <summary>
        /// 作成テスト
        /// </summary>
        /// <param name="obj">NbObject</param> 
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task CreateObjectTest(NbObject obj, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            try
            {
                _obj = await obj.SaveAsync();
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
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
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task GetObjectTest(string id, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            try
            {
                _obj = await _onlineBucket.GetAsync(id);
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
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
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task QueryObjectsTest(NbQuery query, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            try
            {
                _objs = await _onlineBucket.QueryAsync(query);
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// クエリテスト(カウント付き)
        /// </summary>
        /// <param name="query">クエリ</param> 
        /// <param name="queryCount">ヒット件数を取得する場合はtrue</param>
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task QueryObjectsWithOptionsTest(NbQuery query, bool queryCount = true, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            try
            {
                _queryResult = await _onlineBucket.QueryWithOptionsAsync(query, queryCount);
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 部分更新テスト
        /// </summary>
        /// <param name="obj">NbObject</param>
        /// <param name="json">部分更新用 JSON</param>
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task PartUpdateObjectTest(NbJsonObject json, NbObject obj = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            try
            {
                if (obj == null)
                {
                    _obj = await _obj.PartUpdateAsync(json);
                }
                else
                {
                    _obj = await obj.PartUpdateAsync(json);
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
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
        /// <param name="obj">NbObject</param> 
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task FullUpdateObjectTest(NbObject obj, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            await CreateObjectTest(obj, expectedStatusCode, expectedReasonCode);
        }

        /// <summary>
        /// 削除テスト
        /// </summary>
        /// <param name="obj">NbObject</param>
        /// <param name="softDelete">論理削除の場合はtrue</param>
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task DeleteObjectTest(NbObject obj = null, bool softDelete = true, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
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
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task DeleteAllObjectsTest(NbQuery query, bool softDelete = true, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            try
            {
                _deleteCount = await _onlineBucket.DeleteAsync(query, softDelete);
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// バッチテスト
        /// </summary>
        /// <param name="req">バッチリクエスト</param>
        /// <param name="softDelete">論理削除の場合はtrue</param>
        /// <param name="expectedStatusCode">期待するstatusCode</param>
        /// <param name="expectedReasonCode">期待するreasonCode</param> 
        /// <returns>Task</returns>
        private async Task BatchTest(NbBatchRequest req, bool softDelete = true, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, string expectedReasonCode = null)
        {
            try
            {
                _batchResult = await _onlineBucket.BatchAsync(req, softDelete);
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

                if (expectedReasonCode != null)
                {
                    Assert.AreEqual(expectedReasonCode, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    if (expectedReasonCode == ITUtil.ReasonCodeEtagMismatch)
                    {
                        Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                    else
                    {
                        Assert.Null(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                    }
                }
                else
                {
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// R権限がAnonymousのACLを生成する
        /// </summary>
        /// <returns>ACL</returns>
        private NbAcl CreateAclRAnonymous()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");

            return acl;
        }

        /// <summary>
        /// R権限がAnonymousのContentACLを生成する
        /// </summary>
        /// <returns>ContentAcl</returns>
        private NbContentAcl CreateContentAclRAnonymous()
        {
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            return contentAcl;
        }

        /// <summary>
        /// データBのオブジェクトを登録する
        /// </summary>
        /// <returns>Task</returns>
        private async Task CreateObjectsOfDataB()
        {
            var req = new NbBatchRequest();

            var obj = await ITUtil.CreateOnlineObject("data1", new int[] { 100, 101, 102 }, null, false);
            req.AddInsertRequest(obj);

            for (int i = 105; i <= 140; i += 5)
            {
                obj = await ITUtil.CreateOnlineObject("data1", i, null, false);
                req.AddInsertRequest(obj);
            }

            obj = await ITUtil.CreateOnlineObject("data1", new int[] { 145, 101, 102 }, null, false);
            req.AddInsertRequest(obj);
            obj = await ITUtil.CreateOnlineObject("data2", 100, null, false);
            req.AddInsertRequest(obj);

            await _onlineBucket.BatchAsync(req);
        }

        /// <summary>
        /// データCのオブジェクトを登録する
        /// </summary>
        /// <returns>Task</returns>
        private async Task CreateObjectsOfDataC()
        {
            // データ量がそれ程多くないので、Batchは使わない
            await ITUtil.CreateOnlineObject("data1", "abcdefg");
            await ITUtil.CreateOnlineObject("data1", "ABCDEFG");
            await ITUtil.CreateOnlineObject("data1", "_012345_");
            await ITUtil.CreateOnlineObject("data1", "0123ABC");
            await ITUtil.CreateOnlineObject("data1", "0123abc");
            await ITUtil.CreateOnlineObject("data1", "012345" + '\n' + "_");
        }

        /// <summary>
        /// オブジェクトを比較する、一致しない場合はfailとなる
        /// </summary>
        /// <param name="obj1">比較対象</param>
        /// <param name="obj2">比較対象</param>
        private void AssertObject(NbObject obj1, NbObject obj2)
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
                results[i] = objects[i].Opt<int>("data1", 0);
            }
            Array.Sort(results);

            int[] expectedResults = { 100, 105, 110, 115, 120, 125, 130, 135, 140, 145 };

            for (int i = 0; i < 10; i += 1)
            {
                Assert.AreEqual(expectedResults, results);
            }
        }

        /// <summary>
        /// 追加・更新・削除を含むバッチリクエストを生成する
        /// </summary>
        /// <returns>バッチリクエスト</returns>
        private async Task<NbBatchRequest> CreateBatchRequest()
        {
            // create
            var creteObj = new NbObject(BucketName);
            creteObj["create"] = "value";

            // Update
            var updateObj = await CreateObject();
            updateObj["update"] = 1;

            // Delete
            var deleteObj = await CreateObject();

            var req = new NbBatchRequest().AddInsertRequest(creteObj);
            req.AddUpdateRequest(updateObj);
            req.AddDeleteRequest(deleteObj);

            return req;
        }
    }
}
