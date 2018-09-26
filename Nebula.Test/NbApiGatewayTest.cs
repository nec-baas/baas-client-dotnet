using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbApiGatewayTest
    {
        private MockRestExecutor executor;

        private const string appKey = "X-Application-Key";
        private const string appId = "X-Application-Id";
        private const string userAgent = "User-Agent";
        private const string userAgentDefaultValue = "baas dotnet sdk";

        [SetUp]
        public void init()
        {
            TestUtils.Init();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(NbService.Singleton);

            // inject Mock RestExecutor
            executor = new MockRestExecutor();
            NbService.Singleton.RestExecutor = executor;
        }

        private NbJsonObject CreateDummyApiGatewayResultJSON()
        {
            var json = CreateApiGatewayJson();
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            return json;
        }

        private NbJsonObject CreateApiGatewayJson()
        {
            var json = new NbJsonObject()
            {
                {"_id", "12345"},
                {"message", "hello world!"},
            };

            return json;
        }

        private byte[] CreateDummyApiGatewayResultByte()
        {
            var byteArray = new byte[] { 0x41, 0x42, 0x43 }; // ABC
            var response = new MockRestResponse(HttpStatusCode.OK, byteArray, "application/octet-stream");
            executor.AddResponse(response);
            return byteArray;
        }

        /**
        * コンストラクタ
        **/
        /// <summary>
        /// コンストラクタ（正常）
        /// インスタンスが取得できること
        /// プロパティ（正常）初期値が入っていること
        /// Headers, QueryParamsのディクショナリの数が0であること
        /// 指定したServiceが設定されること
        /// </summary>
        [Test]
        public void TestDefaultConstructorNormal()
        {
            var s1 = NbService.Singleton;
            NbService.EnableMultiTenant(true);
            var s2 = NbService.GetInstance();
            Assert.AreNotSame(s1, s2);

            var obj = new NbApiGateway(null, null, null, s2);

            Assert.NotNull(obj);
            Assert.IsNull(obj.ApiName);
            Assert.IsNull(obj.Method);
            Assert.IsNull(obj.SubPath);
            Assert.AreEqual(0, obj.Headers.Count);
            Assert.AreEqual(0, obj.QueryParams.Count);
            Assert.IsNull(obj.ContentType);
            Assert.AreEqual(s2, obj.Service);
            NbService.EnableMultiTenant(false);
        }

        /**
        * API実行
        **/
        /// <summary>
        /// API実行(レスポンス：JSON)
        /// APIが実行できること
        /// ApiName, HttpMethod, SubPathには指定の値が格納されること
        /// サービスはNbService.Singletonであること（service指定無し）
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestExecuteAsyncNormal()
        {
            var json = CreateDummyApiGatewayResultJSON();

            // Main
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Get, "sayHello");
            var result = await obj.ExecuteAsync();

            // Check Value
            Assert.AreEqual("hello", obj.ApiName);
            Assert.AreEqual(System.Net.Http.HttpMethod.Get, obj.Method);
            Assert.AreEqual("sayHello", obj.SubPath);
            Assert.AreEqual(NbService.Singleton, obj.Service);

            // Check Response
            Assert.AreEqual(result.JsonObject["message"], "hello world!");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/hello/sayHello"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.IsTrue(req.Headers.ContainsKey(userAgent));
            Assert.AreEqual(req.Headers[userAgent], userAgentDefaultValue);
        }

        /// <summary>
        /// API実行(レスポンス：バイナリデータ)
        /// APIが実行できること
        /// サービスはNbService.Singletonであること（serviceがnull）
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestExecuteRawAsyncNormal()
        {
            CreateDummyApiGatewayResultByte();

            // Main
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Get, "sayHello", null);
            obj.SetHeader("User-Agent", "User-Agent-TEST");
            var result = await obj.ExecuteRawAsync();

            // Check ServiceObj
            Assert.AreEqual(NbService.Singleton, obj.Service);

            // Check Response
            Assert.AreEqual(result.ContentType, "application/octet-stream");
            Assert.AreEqual(result.ContentLength, 3);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetString(result.RawBytes), "ABC");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/hello/sayHello"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.IsTrue(req.Headers.ContainsKey(userAgent));
            Assert.AreEqual("User-Agent-TEST", req.Headers["User-Agent"]);
        }

        /// <summary>
        /// API実行(レスポンス：JSON)
        /// ApiName未設定の場合、Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncExceptionNoApiName()
        {
            try
            {
                var obj = new NbApiGateway(null, System.Net.Http.HttpMethod.Get, "sayHello");
                var result = await obj.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// API実行(レスポンス：バイナリデータ)
        /// ApiName未設定の場合、Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public async void TestExecuteRawAsyncExceptionNoApiName()
        {
            try
            {
                var obj = new NbApiGateway(null, System.Net.Http.HttpMethod.Get, "sayHello");
                var result = await obj.ExecuteRawAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// API実行(レスポンス：JSON)
        /// bodyが存在し、Content-Type未設定の場合、Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncExceptionNoContentType()
        {
            try
            {
                var json = NbJsonObject.Parse("{'foo': 'bar'}");
                var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Put, "sayHello");
                var result = await obj.ExecuteAsync(json);
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// API実行(レスポンス：JSON)
        /// bodyの型が対象外(JSON形式、byte[])の場合、Exception（ArgumentException）が発行されること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncExceptionNoBodyObject()
        {
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Put, "sayHello");
            obj.ContentType = "text/plain";

            try
            {
                int i = 0;
                var result = await obj.ExecuteAsync(i);
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }

            try
            {
                var result = await obj.ExecuteAsync("");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }

        /// <summary>
        /// API実行
        /// Content-Type設定(Post)
        /// ContentTypeプロパティ、SetHeaderの両方設定（ContentTypeプロパティ優先）
        /// RequestのContent.Headers.ContentTypeが設定値であること（ContentTypeプロパティで指定した値）        
        /// RequestのHeadersにContentTypeが含まれていないこと
        /// Requestのbodyが設定されていること（JSON形式）
        /// NbApiGatewayのContentType値に変更がないこと
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetContentTypePropertyAndHeadersPost()
        {
            var json = CreateDummyApiGatewayResultJSON();

            // Main
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Post, "sayHello");
            obj.ContentType = "text/plain";
            obj.SetHeader("Content-Type", "text/plain2");
            var result = await obj.ExecuteAsync(jsonObj);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(req.Content.Headers.ContentType.ToString(), obj.ContentType);
            Assert.IsTrue(!req.Headers.ContainsKey("Content-Type"));
            var contentBody = NbJsonObject.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(jsonObj, contentBody);

            // Check NbApiGateway Object
            Assert.AreEqual(obj.ContentType, "text/plain");
            Assert.AreEqual(obj.Headers["Content-Type"], "text/plain2");
        }

        /// <summary>
        /// API実行
        /// Content-Type設定(Post)
        /// ContentTypeプロパティのみ設定
        /// RequestのContent.Headers.ContentTypeが設定値であること（ContentTypeプロパティで指定した値）        
        /// RequestのHeadersにContentTypeが含まれていないこと
        /// NbApiGatewayのContentType値に変更がないこと
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetContentTypePost()
        {
            var json = CreateDummyApiGatewayResultJSON();

            // Main
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Post, "sayHello");
            obj.ContentType = "text/plain";
            var result = await obj.ExecuteAsync(jsonObj);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(req.Content.Headers.ContentType.ToString(), obj.ContentType);
            Assert.IsTrue(!req.Headers.ContainsKey("Content-Type"));

            // Check NbApiGateway Object
            Assert.AreEqual(obj.ContentType, "text/plain");
            Assert.IsTrue(!obj.Headers.ContainsKey("Content-Type"));
        }

        /// <summary>
        /// API実行
        /// Content-Type設定(Post)
        /// SetHeaderのみ設定
        /// RequestのContent.Headers.ContentTypeが設定値であること（Headersで指定した値）
        /// RequestのHeadersにContentTypeが含まれていないこと
        /// NbApiGatewayのContentType値に変更がないこと
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetContentTypeHeadersPost()
        {
            var json = CreateDummyApiGatewayResultJSON();

            // Main
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Post, "sayHello");
            obj.SetHeader("Content-Type", "text/plain2");
            var result = await obj.ExecuteAsync(jsonObj);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(req.Content.Headers.ContentType.ToString(), "text/plain2");
            Assert.IsTrue(!req.Headers.ContainsKey("Content-Type"));

            // Check NbApiGateway Object
            Assert.IsNull(obj.ContentType);
            Assert.AreEqual(obj.Headers["Content-Type"], "text/plain2");
        }

        /// <summary>
        /// API実行
        /// Content-Type未設定(Post)
        /// RequestのHeadersにContentTypeが含まれていないこと（req.Content=null）
        /// NbApiGatewayのHeadersのContentType値に変更がないこと
        /// </summary>
        [Test]
        public async void TestExecuteAsyncNoSetContentTypePost()
        {
            var json = CreateDummyApiGatewayResultJSON();

            // Main
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Post, "sayHello");
            var result = await obj.ExecuteAsync();

            // Check Request
            var req = executor.LastRequest;
            Assert.IsTrue(!req.Headers.ContainsKey("Content-Type"));
            Assert.IsNull(req.Content);

            // Check NbApiGateway Object
            Assert.IsNull(obj.ContentType);
            Assert.IsTrue(!obj.Headers.ContainsKey("Content-Type"));
        }

        /// <summary>
        /// API実行
        /// Content-Type設定(Get)
        /// ContentTypeプロパティ、SetHeaderの両方設定（ContentTypeプロパティ優先）
        /// RequestのHeadersにContentTypeが含まれていないこと
        /// NbApiGatewayのContentType値に変更がないこと（値有り）
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetContentTypePropertyAndHeadersGet()
        {
            var json = CreateDummyApiGatewayResultJSON();

            // Main
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Get, "sayHello");
            obj.ContentType = "text/plain";
            obj.SetHeader("Content-Type", "text/plain2");
            var result = await obj.ExecuteAsync(jsonObj);

            // Check Request
            var req = executor.LastRequest;
            Assert.IsNull(req.Content);
            Assert.IsTrue(!req.Headers.ContainsKey("Content-Type"));

            // Check NbApiGateway Object
            Assert.AreEqual(obj.ContentType, "text/plain");
            Assert.AreEqual(obj.Headers["Content-Type"], "text/plain2");
        }

        /// <summary>
        /// API実行
        /// QueryParameter設定
        /// RequestのQueryParamsにパラメータが設定されていること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetQueryParameter()
        {
            // Get Test
            var json = CreateDummyApiGatewayResultJSON();
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Get, "sayHello");
            obj.SetQueryParameter("get1", "get1value");
            var result = await obj.ExecuteAsync();
            var req = executor.LastRequest;
            Assert.AreEqual(req.QueryParams["get1"], "get1value");

            // Delete Test
            json = CreateDummyApiGatewayResultJSON();
            obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Delete, "sayHello");
            obj.SetQueryParameter("delete1", "delete1value");
            result = await obj.ExecuteAsync();
            req = executor.LastRequest;
            Assert.AreEqual(req.QueryParams["delete1"], "delete1value");

            // Post Test
            json = CreateDummyApiGatewayResultJSON();
            obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Post, "sayHello");
            obj.SetQueryParameter("post1", "post1value");
            result = await obj.ExecuteAsync();
            req = executor.LastRequest;
            Assert.AreEqual(req.QueryParams["post1"], "post1value");

            // Put Test
            json = CreateDummyApiGatewayResultJSON();
            obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Put, "sayHello");
            obj.SetQueryParameter("put1", "put1value");
            result = await obj.ExecuteAsync();
            req = executor.LastRequest;
            Assert.AreEqual(req.QueryParams["put1"], "put1value");
        }

        /// <summary>
        /// API実行
        /// Requestのbodyが設定されていること（バイナリデータ）
        /// </summary>
        [Test]
        public async void TestExecuteAsyncByteArray()
        {
            // Post Test
            var json = CreateDummyApiGatewayResultJSON();

            // Main
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Post, "sayHello");
            obj.ContentType = "application/octet-stream";
            var byteArray = new byte[] { 0x41, 0x42, 0x43 }; // ABC
            var result = await obj.ExecuteAsync(byteArray);

            // Check Request
            var req = executor.LastRequest;
            var contentBody = req.Content.ReadAsByteArrayAsync().Result;
            Assert.AreEqual(byteArray, contentBody);
        }

        /**
        * Setter/Getter（カスタマイズしていないプロパティは、UTを実施しない）
        **/
        /**
        * SetHeader/RemoveHeader/ClearHeaders
        **/
        /// <summary>
        /// Header設定
        /// name/valueがNULL、空は登録しないこと
        /// すでに同一名のパラメータがあった場合は上書きされること
        /// ヘッダを削除すること
        /// すべてのヘッダを削除すること
        /// </summary>
        [Test]
        public void TestExecuteAsyncHeader()
        {
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Get, "sayHello");
            obj.SetHeader("", "h1value");
            Assert.AreEqual(obj.Headers.Count, 0);
            obj.SetHeader(null, "h1value");
            Assert.AreEqual(obj.Headers.Count, 0);
            obj.SetHeader("h1", null);
            Assert.AreEqual(obj.Headers.Count, 0);
            obj.SetHeader("h1", "");
            Assert.AreEqual(obj.Headers.Count, 0);

            obj.SetHeader("h1", "h1value1");
            Assert.AreEqual(obj.Headers["h1"], "h1value1");
            obj.SetHeader("h1", "h1value2");
            Assert.AreEqual(obj.Headers["h1"], "h1value2");

            obj.SetHeader("h2", "h2value");
            Assert.AreEqual(obj.Headers["h2"], "h2value");
            obj.RemoveHeader("h2");
            Assert.IsTrue(!obj.Headers.ContainsKey("h2"));

            obj.SetHeader("h2", "h2value");
            Assert.AreEqual(obj.Headers.Count, 2);
            obj.ClearHeaders();
            Assert.AreEqual(obj.Headers.Count, 0);
        }

        /**
        * SetQueryParameter/RemoveQueryParameter/ClearQueryParameters
        **/
        /// <summary>
        /// Query設定
        /// name/valueがNULL、空は登録しないこと
        /// すでに同一名のパラメータがあった場合は上書きされること
        /// クエリを削除すること
        /// すべてのクエリを削除すること
        /// </summary>
        [Test]
        public void TestExecuteAsyncQuery()
        {
            var obj = new NbApiGateway("hello", System.Net.Http.HttpMethod.Get, "sayHello");
            obj.SetQueryParameter("", "q1value");
            Assert.AreEqual(obj.QueryParams.Count, 0);
            obj.SetQueryParameter(null, "q1value");
            Assert.AreEqual(obj.QueryParams.Count, 0);
            obj.SetQueryParameter("q1", null);
            Assert.AreEqual(obj.QueryParams.Count, 0);
            obj.SetQueryParameter("q1", "");
            Assert.AreEqual(obj.QueryParams.Count, 0);

            obj.SetQueryParameter("q1", "q1value1");
            Assert.AreEqual(obj.QueryParams["q1"], "q1value1");
            obj.SetQueryParameter("q1", "q1value2");
            Assert.AreEqual(obj.QueryParams["q1"], "q1value2");

            obj.SetQueryParameter("q2", "q2value");
            Assert.AreEqual(obj.QueryParams["q2"], "q2value");
            obj.RemoveQueryParameter("q2");
            Assert.IsTrue(!obj.QueryParams.ContainsKey("q2"));

            obj.SetQueryParameter("q2", "q2value");
            Assert.AreEqual(obj.QueryParams.Count, 2);
            obj.ClearQueryParameters();
            Assert.AreEqual(obj.QueryParams.Count, 0);
        }
    }
}