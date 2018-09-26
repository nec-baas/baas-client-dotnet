using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbObjectBucketTest
    {
        private NbObjectBucket<NbObject> bucket;
        private MockRestExecutor restExecutor;

        private const string appKey = "X-Application-Key";
        private const string appId = "X-Application-Id";

        [SetUp]
        public void Init()
        {
            TestUtils.Init();

            bucket = new NbObjectBucket<NbObject>("test");
            restExecutor = new MockRestExecutor();

            // inject rest executor
            NbService.Singleton.RestExecutor = restExecutor;
        }

        /**
        * コンストラクタ
        **/
        /// <summary>
        /// コンストラクタ（NbObject、service指定あり）
        /// サービス、バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var s1 = NbService.Singleton;
            NbService.EnableMultiTenant(true);
            var s2 = NbService.GetInstance();
            Assert.AreNotSame(s1, s2);

            var bucket = new NbObjectBucket<NbObject>("test", s2);

            Assert.AreEqual("test", bucket.BucketName);
            Assert.AreEqual(s2, bucket.Service);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// コンストラクタ（NbObject、service指定なし）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceUnset()
        {
            Assert.AreEqual("test", bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, bucket.Service);
        }

        /// <summary>
        /// コンストラクタ（NbObject、serviceがnull）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceNull()
        {
            var bucket = new NbObjectBucket<NbObject>("test", null);

            Assert.AreEqual("test", bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, bucket.Service);
        }

        private class SubObject : NbObject
        {
            public SubObject()
                : base("subTest")
            {
            }
        }

        private class SubNoBucketObject : NbObject
        {
            public SubNoBucketObject()
                : base()
            {
            }
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名あり、bucketName指定無し）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalSubclassBucketNameUnset()
        {
            var bucket = new NbObjectBucket<SubObject>();

            Assert.AreEqual("subTest", bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, bucket.Service);
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名あり、bucketNameがnull）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalSubclassBucketNameNull()
        {
            var bucket = new NbObjectBucket<SubObject>(null);

            Assert.AreEqual("subTest", bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, bucket.Service);
        }

        /// <summary>
        /// コンストラクタ（NbObject、bucketName指定なし）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionBucketNameUnset()
        {
            var bucket = new NbObjectBucket<NbObject>();
        }

        /// <summary>
        /// コンストラクタ（NbObject、bucketNameがnull）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionBucketNameNull()
        {
            var bucket = new NbObjectBucket<NbObject>(null);
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名なし、bucketName指定なし）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionSubclassBucketNameUnset()
        {
            var bucket = new NbObjectBucket<SubNoBucketObject>();
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名なし、bucketNameがnull）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionSubclassBucketNameNull()
        {
            var bucket = new NbObjectBucket<SubNoBucketObject>(null);
        }

        /**
        * NewObject
        **/
        /// <summary>
        /// NewObject（NbObject、正常）
        /// オブジェクトが生成できること
        /// オブジェクトのサービス、バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestNewObjectNormal()
        {
            var obj = bucket.NewObject();

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
        }

        /// <summary>
        /// NewObject（サブクラス）
        /// オブジェクトが生成できること
        /// オブジェクトのサービス、バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestNewObjectNormalSubclass()
        {
            var bucket = new NbObjectBucket<SubObject>();
            var obj = bucket.NewObject();

            Assert.AreEqual("subTest", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
        }

        /**
        * GetAsync
        **/
        /// <summary>
        /// GetAsync（NbObject、正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestGetAsyncNormal()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGetAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.GetAsync("abcdef");

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            CheckResponse(result);
        }

        /// <summary>
        /// GetAsync（サブクラス、正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestGetAsyncNormalSubclass()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGetAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var bucket = new NbObjectBucket<SubObject>();
            var result = await bucket.GetAsync("abcdef");

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "subTest/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            CheckResponse(result, "subTest");
        }

        /// <summary>
        /// GetAsync（オブジェクトIDがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestGetAsyncExceptionObjectIdNull()
        {
            var result = await bucket.GetAsync(null);
        }

        /// <summary>
        /// GetAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestGetAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            try
            {
                var result = await bucket.GetAsync("abcdef");
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
        * QueryAsync
        **/
        /// <summary>
        /// QueryAsync（全ての条件の指定あり）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// query.SkipValue=0,orderが一つとなるQueryで検証
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalAll()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var query = new NbQuery().Exists("testKey");
            // order指定あり
            query.OrderBy("key1");
            // skip指定あり
            query.Skip(0);
            // limit指定あり
            query.Limit(5);
            // deleteMark指定あり
            query.DeleteMark(true);
            // projection指定あり
            query.Projection("name");

            var result = await bucket.QueryAsync(query);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(6, req.QueryParams.Count);
            // where
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(true, where.GetJsonObject("testKey")["$exists"]);
            // order
            Assert.AreEqual("key1", req.QueryParams["order"]);
            // skip
            Assert.AreEqual("0", req.QueryParams["skip"]);
            // limit
            Assert.AreEqual("5", req.QueryParams["limit"]);
            // deleteMark
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);
            // projection
            var projection = NbJsonObject.Parse(req.QueryParams["projection"]);
            Assert.AreEqual(1, projection["name"]);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Count());
            foreach (var obj in result)
            {
                CheckResponse(obj);
            }
        }

        /// <summary>
        /// QueryAsync（NbQueryが空）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalQueryEmpty()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.QueryAsync(new NbQuery());

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            // whereは空文字になる
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Count());
            foreach (var obj in result)
            {
                CheckResponse(obj);
            }
        }

        /// <summary>
        /// QueryAsync（orderの指定が複数）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalMultipleOrder()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var query = new NbQuery().All("testKey", 4, 5, 6);
            // order指定あり
            query.OrderBy("key1", "-key2");

            var result = await bucket.QueryAsync(query);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(3, req.QueryParams.Count);
            // where
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(new object[] { 4, 5, 6 }, where.GetJsonObject("testKey")["$all"]);
            // order
            Assert.AreEqual("key1,-key2", req.QueryParams["order"]);
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Count());
            foreach (var obj in result)
            {
                CheckResponse(obj);
            }
        }

        /// <summary>
        /// QueryAsync（orderのサイズが0、limitが範囲外）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryAsyncSubnormalIllegalParam()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var query = new NbQuery().In("testKey", 1, 2, 3);
            // order指定あり
            query.OrderBy(new string[0]);
            // limit指定あり
            query.Limit(-2);
            var result = await bucket.QueryAsync(query);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            // where
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(new object[] { 1, 2, 3 }, where.GetJsonObject("testKey")["$in"]);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは設定なし
            Assert.False(req.QueryParams.ContainsKey("limit"));
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Count());
            foreach (var obj in result)
            {
                CheckResponse(obj);
            }
        }

        /// <summary>
        /// QueryAsync（queryがnull）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalQueryNull()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.QueryAsync(null);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            // whereは空文字になる
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Count());
            foreach (var obj in result)
            {
                CheckResponse(obj);
            }
        }

        /// <summary>
        /// QueryAsync（検索結果が空）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 空が返ること
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalResponseEmpty()
        {
            // Set Dummy Response
            var json = new NbJsonObject()
            {
                {"results", new NbJsonArray()}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.QueryAsync(new NbQuery());

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            // whereは空文字になる
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.IsEmpty(result);
        }

        /// <summary>
        /// QueryAsync（サブクラス）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalSubclass()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var bucket = new NbObjectBucket<SubObject>();
            var result = await bucket.QueryAsync(new NbQuery());

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "subTest"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            // whereは空文字になる
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Count());
            foreach (var obj in result)
            {
                Assert.True(obj is SubObject);
                CheckResponse(obj, "subTest");
            }
        }

        /// <summary>
        /// QueryAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestQueryAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.Forbidden, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            try
            {
                var result = await bucket.QueryAsync(new NbQuery());
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
        * QueryWithOptionsAsync
        **/
        /// <summary>
        /// QueryWithOptionsAsync（queryCount未設定）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryWithOptionsAsyncNormalQueryCountUnset()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.QueryWithOptionsAsync(new NbQuery());

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            // whereは空文字になる
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Objects.Count());
            foreach (var obj in result.Objects)
            {
                CheckResponse(obj);
            }
            Assert.AreEqual(-1, result.Count);
            Assert.AreEqual("2013-09-01T12:34:56.000Z", result.CurrentTime);
        }

        /// <summary>
        /// QueryWithOptionsAsync（queryCountがfalse）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryWithOptionsAsyncNormalQueryCountFalse()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryeAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.QueryWithOptionsAsync(new NbQuery().LessThanOrEqual("testKey", 100), false);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            // where
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(100, where.GetJsonObject("testKey")["$lte"]);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Objects.Count());
            foreach (var obj in result.Objects)
            {
                CheckResponse(obj);
            }
            Assert.AreEqual(-1, result.Count);
            Assert.AreEqual("2013-09-01T12:34:56.000Z", result.CurrentTime);
        }

        /// <summary>
        /// QueryWithOptionsAsync（queryCountがtrue）
        /// リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryWithOptionsAsyncNormalQueryCountTrue()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryWithOptionsAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.QueryWithOptionsAsync(new NbQuery().NotEquals("testKey", 1), true);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(3, req.QueryParams.Count);
            // where
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(1, where.GetJsonObject("testKey")["$ne"]);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));
            // count
            Assert.AreEqual("1", req.QueryParams["count"]);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Objects.Count());
            foreach (var obj in result.Objects)
            {
                CheckResponse(obj);
            }
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("2013-09-01T12:34:56.000Z", result.CurrentTime);
        }

        /// <summary>
        /// QueryWithOptionsAsync（queryがnull）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryWithOptionsAsyncNormalQueryNull()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryWithOptionsAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.QueryWithOptionsAsync(null, true);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(3, req.QueryParams.Count);
            // whereは空文字になる
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));
            // count
            Assert.AreEqual("1", req.QueryParams["count"]);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Objects.Count());
            foreach (var obj in result.Objects)
            {
                CheckResponse(obj);
            }
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("2013-09-01T12:34:56.000Z", result.CurrentTime);
        }

        /// <summary>
        /// QueryWithOptionsAsync（サブクラス）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestQueryWithOptionsAsyncNormalSubclass()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateQueryWithOptionsAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var bucket = new NbObjectBucket<SubObject>();
            var result = await bucket.QueryWithOptionsAsync(new NbQuery(), true);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "subTest"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(3, req.QueryParams.Count);
            // whereは空文字になる
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);
            // orderは設定なし
            Assert.False(req.QueryParams.ContainsKey("order"));
            // skipは設定なし
            Assert.False(req.QueryParams.ContainsKey("skip"));
            // limitは-1
            Assert.AreEqual("-1", req.QueryParams["limit"]);
            // deleteMarkは設定なし
            Assert.False(req.QueryParams.ContainsKey("deleteMark"));
            // projectionは設定なし
            Assert.False(req.QueryParams.ContainsKey("projection"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(2, result.Objects.Count());
            foreach (var obj in result.Objects)
            {
                Assert.True(obj is SubObject);
                CheckResponse(obj, "subTest");
            }
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("2013-09-01T12:34:56.000Z", result.CurrentTime);
        }

        /// <summary>
        /// QueryWithOptionsAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestQueryWithOptionsAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            try
            {
                var result = await bucket.QueryWithOptionsAsync(new NbQuery());
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        // SetQueryParameters()、JarrayToObjects()については、QueryAsync()、QueryWithOptionsAsync()
        // のUTで検証済
        // 引数がnullの場合については、呼び元から引数がnullの状態で呼ばれることがないため、
        // UT対象外とする

        /**
        * DeleteAsync
        **/
        /// <summary>
        /// DeleteAsync（NbObject、softDelete未設定）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteUnset()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateDeleteAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.DeleteAsync(new NbQuery().Exists("testKey"));

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(true, where.GetJsonObject("testKey")["$exists"]);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(3, result);
        }

        /// <summary>
        /// DeleteAsync（NbObject、softDeleteがtrue）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteTrue()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateDeleteAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.DeleteAsync(new NbQuery().GreaterThan("testKey", 2), true);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(2, where.GetJsonObject("testKey")["$gt"]);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(3, result);
        }

        /// <summary>
        /// DeleteAsync（NbObject、softDeleteがfalse）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteFalse()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateDeleteAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.DeleteAsync(new NbQuery().EqualTo("testKey", 0), false);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(0, where.Get<int>("testKey"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(3, result);
        }

        /// <summary>
        /// DeleteAsync（クエリが空）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalQueryEmpty()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateDeleteAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.DeleteAsync(new NbQuery());

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.IsEmpty(where);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(3, result);
        }

        /// <summary>
        /// DeleteAsync（サブクラス）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSubclass()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateDeleteAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var bucket = new NbObjectBucket<SubObject>();
            var result = await bucket.DeleteAsync(new NbQuery().GreaterThan("testKey", 100).LessThan("testKey", 200));

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.True(req.Uri.EndsWith("/objects/" + "subTest"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);
            var where = NbJsonObject.Parse(req.QueryParams["where"]);
            Assert.AreEqual(100, where.GetJsonObject("testKey")["$gt"]);
            Assert.AreEqual(200, where.GetJsonObject("testKey")["$lt"]);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(3, result);
        }

        /// <summary>
        /// DeleteAsync（queryがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteAsyncExceptionQueryNull()
        {
            var result = await bucket.DeleteAsync(null);
        }

        /// <summary>
        /// DeleteAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.InternalServerError, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            try
            {
                var result = await bucket.DeleteAsync(new NbQuery());
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.InternalServerError, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
         * AggregateAsync
         */

        /// <summary>
        /// AggregateAsync
        /// 設定しているメソッド、パス、ヘッダ、パラメータ、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestAggregateAsyncNormal()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateAggregateResultJson().ToString());
            restExecutor.AddResponse(response);

            var pipeline = new NbJsonArray();
            var limit = new NbJsonObject()
            {
                {"$limit", 100}
            };
            pipeline.Add(limit);

            var opResults = await bucket.AggregateAsync(pipeline);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Post, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + bucket.BucketName + "/_aggregate"));

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // Request Parameter
            Assert.IsEmpty(req.QueryParams);

            // Request Body
            var requestBody = NbJsonObject.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(requestBody.ContainsKey("pipeline"));
            Assert.IsFalse(requestBody.ContainsKey("options"));
            var reqPipeline = requestBody.GetArray("pipeline");
            Assert.AreEqual(1, reqPipeline.Count());
            Assert.AreEqual(limit, reqPipeline.Get<NbJsonObject>(0));

            // Response
            Assert.AreEqual(2, opResults.Count());
        }

        /// <summary>
        /// AggregateAsync
        /// リクエストにオプションが含まれること
        /// </summary>
        [Test]
        public async void TestAggregateAsyncNormalWithOptions()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, "{\"results\":[]}");
            restExecutor.AddResponse(response);

            var pipeline = new NbJsonArray();
            var options = new NbJsonObject()
            {
                {"allowDiskUse", true},
                {"maxTimeMS", 1000},
                {"batchSize", 10}
            };

            var opResults = await bucket.AggregateAsync(pipeline, options);

            // Request Check
            var req = restExecutor.LastRequest;

            // Request Body
            var requestBody = NbJsonObject.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(requestBody.ContainsKey("pipeline"));
            Assert.IsTrue(requestBody.ContainsKey("options"));
            var reqPipeline = requestBody.GetArray("pipeline");
            Assert.IsEmpty(reqPipeline);
            Assert.AreEqual(options, requestBody.GetJsonObject("options"));

            // Response
            Assert.IsEmpty(opResults);
        }

        /// <summary>
        /// AggregateAsync
        /// pipelineが未設定の場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestAggregateAsyncExceptionPipelineNull()
        {
            var result = await bucket.AggregateAsync(null);
        }

        /// <summary>
        /// AggregateAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestAggregateAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.InternalServerError, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var pipeline = new NbJsonArray();

            try
            {
                var result = await bucket.AggregateAsync(pipeline);
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.InternalServerError, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
         * BatchAsync
         */

        /// <summary>
        /// BatchAsync
        /// 設定しているメソッド、パス、ヘッダ、パラメータ、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestBatchAsyncNormal()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateBatchResultJson().ToString());
            restExecutor.AddResponse(response);

            var obj = new NbObject(bucket.BucketName);
            var requests = new NbBatchRequest();
            requests.AddInsertRequest(obj);

            var result = await bucket.BatchAsync(requests, true);
            var opResults = result.ToArray();

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Post, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + bucket.BucketName + "/_batch"));

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // Request Parameter
            Assert.AreEqual(requests.RequestToken, req.QueryParams["requestToken"]);
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);

            // Request Body
            var requestBody = NbJsonObject.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(requests.Json, requestBody);

            // Response
            Assert.AreEqual(3, opResults.Count());

        }

        /// <summary>
        /// BatchAsync
        /// リクエストパラメータに仮削除マークを含まないこと
        /// </summary>
        [Test]
        public async void TestBatchAsyncSubnormalNoDeleteMark()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateBatchResultJson().ToString());
            restExecutor.AddResponse(response);

            var obj = new NbObject(bucket.BucketName);
            var requests = new NbBatchRequest();
            requests.AddInsertRequest(obj);

            var result = await bucket.BatchAsync(requests, false);
            var opResults = result.ToArray();

            // Request Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.IsFalse(req.QueryParams.ContainsKey("deleteMark"));
        }

        /// <summary>
        /// BatchAsync（サブクラス）
        /// 設定しているパスが正しいこと
        /// </summary>
        [Test]
        public async void TestBatchAsyncNormalSubclass()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateBatchResultJson().ToString());
            restExecutor.AddResponse(response);

            var obj = new SubObject();
            var requests = new NbBatchRequest();
            requests.AddInsertRequest(obj);

            var bucket = new NbObjectBucket<SubObject>();
            var result = await bucket.BatchAsync(requests, true);
            var opResults = result.ToArray();

            // Request Check
            var req = restExecutor.LastRequest;

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "subTest" + "/_batch"));
        }

        /// <summary>
        /// BatchAsync
        /// リクエストが未設定の場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestBatchAsyncExceptionRequestNull()
        {
            var result = await bucket.BatchAsync(null);
        }

        /// <summary>
        /// BatchAsync
        /// オペレーションが1つもない場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestBatchAsyncExceptionRequestEmpty()
        {

            var result = await bucket.BatchAsync(new NbBatchRequest(), false);
        }

        /// <summary>
        /// BatchAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestBatchAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.InternalServerError, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var obj = new NbObject(bucket.BucketName);
            var requests = new NbBatchRequest();
            requests.AddInsertRequest(obj);

            try
            {
                var result = await bucket.BatchAsync(requests);
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.InternalServerError, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
         * Test Utility
         */
        private NbJsonObject CreateGetAsyncResponseJson()
        {
            return NbObjectTest.CreateSaveAsyncResponseJson();
        }

        private NbJsonObject CreateDeleteAsyncResponseJson()
        {
            var json = new NbJsonObject()
            {
                {"result", "ok"},
                {"deletedObjects",3}
            };

            return json;
        }

        private NbJsonObject CreateQueryeAsyncResponseJson()
        {
            var array = new NbJsonArray();
            array.Add(CreateGetAsyncResponseJson());
            array.Add(CreateGetAsyncResponseJson());

            var json = new NbJsonObject()
            {
                {"results", array},
                {"currentTime", "2013-09-01T12:34:56.000Z"}
            };

            return json;
        }

        private NbJsonObject CreateQueryWithOptionsAsyncResponseJson()
        {
            var array = new NbJsonArray();
            array.Add(CreateGetAsyncResponseJson());
            array.Add(CreateGetAsyncResponseJson());

            var json = new NbJsonObject()
            {
                {"results", array},
                {"count", 2},
                {"currentTime", "2013-09-01T12:34:56.000Z"}
            };

            return json;
        }

        private NbJsonObject CreateAggregateResultJson()
        {
            var json = new NbJsonObject();
            var results = new NbJsonArray();

            var obj1 = new NbJsonObject();
            obj1["_id"] = "id_1";
            obj1["value"] = "aaa";
            results.Add(obj1);

            var obj2 = new NbJsonObject();
            obj2["_id"] = "id_2";
            obj2["value"] = "bbb";
            results.Add(obj2);

            json["results"] = results;

            return json;
        }

        private NbJsonObject CreateBatchResultJson()
        {

            var ret = new NbJsonObject();
            var opeArray = new NbJsonArray();
            var obj = new NbObject(bucket.BucketName);

            // insert
            var insert = new NbJsonObject();
            insert["result"] = "ok";
            insert["_id"] = "insertId";
            insert["etag"] = "insertEtag";
            insert["updatedAt"] = "insertUpdateTime";
            insert["data"] = obj.ToJson();

            opeArray.Add(insert);

            // update
            var update = new NbJsonObject();
            update["result"] = "conflict";
            update["reasonCode"] = "duplicate_id";
            update["_id"] = "updateId";
            update["etag"] = "updateEtag";
            update["updatedAt"] = "updateUpdateTime";
            update["data"] = obj.ToJson();

            opeArray.Add(update);

            // delete
            var delete = new NbJsonObject();
            delete["result"] = "forbidden";
            delete["_id"] = "deleteId";
            delete["etag"] = "deleteEtag";
            delete["updatedAt"] = "deletetUpdateTime";
            delete["data"] = obj.ToJson();

            opeArray.Add(delete);

            ret["results"] = opeArray;

            return ret;
        }

        private void CheckResponse(NbObject obj, string bucketName = "test")
        {
            NbObjectTest.CheckResponse(obj, bucketName);
        }
    }

}
