using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;

namespace Nec.Nebula.Test.Internal
{
    [TestFixture]
    public class NbRestExecutorTest
    {
        NbService _service;
        string _path;

        [SetUp]
        public void Setup()
        {
            TestUtils.Init();
            _service = NbService.Singleton;

            _path = _service.EndpointUrl + "1/" + _service.TenantId + "/test";
        }

        /**
        * Constructor
        **/
        /// <summary>
        /// コンストラクタテスト（serviceなし）
        /// 各フィールドには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var executor = new NbRestExecutor();

            // privateフィールドとなるため、Reflectionを使用
            FieldInfo service = executor.GetType().GetField("_service", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo sessionInfo = executor.GetType().GetField("_sessionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo httpClient = executor.GetType().GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.AreEqual(_service, service.GetValue(executor));
            Assert.AreEqual(_service.SessionInfo, sessionInfo.GetValue(executor));
            var client = httpClient.GetValue(executor) as HttpClient;
            Assert.IsNotNull(client);
            Assert.AreEqual(Timeout.InfiniteTimeSpan, client.Timeout);
        }

        /// <summary>
        /// コンストラクタテスト（serviceあり）
        /// 各フィールドには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorWithServiceNormal()
        {
            // SetUp
            NbService.EnableMultiTenant(true);
            var nbService = NbService.GetInstance();

            var executor = new NbRestExecutor(nbService);

            // privateフィールドとなるため、Reflectionを使用
            FieldInfo service = executor.GetType().GetField("_service", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo sessionInfo = executor.GetType().GetField("_sessionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo httpClient = executor.GetType().GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.AreEqual(nbService, service.GetValue(executor));
            Assert.AreEqual(nbService.SessionInfo, sessionInfo.GetValue(executor));
            var client = httpClient.GetValue(executor) as HttpClient;
            Assert.IsNotNull(client);
            Assert.AreEqual(Timeout.InfiniteTimeSpan, client.Timeout);

            // TearDown
            NbService.EnableMultiTenant(false);
        }

        /**
        * CreateRequest
        **/
        /// <summary>
        /// CreateRequest（セッショントークン有効）
        /// 生成されたリクエストの内容が正しいこと
        /// "X-Session-Token"ヘッダが存在すること
        /// </summary>
        [Test]
        public void TestCreateRequestNormalSessionTokenIsAvailable()
        {
            var executor = new NbRestExecutor();
            // sessionInfoを差し替える
            FieldInfo sessionInfo = executor.GetType().GetField("_sessionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            NbSessionInfo mockSessionInfo = new NbSessionInfo();
            var user = new NbUser();
            mockSessionInfo.Set("token", NbUtil.CurrentUnixTime() + 3, user);
            sessionInfo.SetValue(executor, mockSessionInfo);

            var request = executor.CreateRequest("/test", HttpMethod.Get);

            Assert.AreEqual(request.Method, HttpMethod.Get);
            Assert.AreEqual(request.Uri, _path);
            Assert.AreEqual(4, request.Headers.Count);
            Assert.AreEqual(_service.AppId, request.Headers["X-Application-Id"]);
            Assert.AreEqual(_service.AppKey, request.Headers["X-Application-Key"]);
            Assert.AreEqual(Header.UserAgentDefaultValue, request.Headers["User-Agent"]);
            Assert.AreEqual("token", request.Headers["X-Session-Token"]);
        }

        /// <summary>
        /// CreateRequest（セッショントークン無効）
        /// 生成されたリクエストの内容が正しいこと
        /// "X-Session-Token"ヘッダが存在しないこと
        /// </summary>
        [Test]
        public void TestCreateRequestNormalSessionTokenIsNotAvailable()
        {
            var executor = new NbRestExecutor();
            // sessionInfoを差し替える 
            FieldInfo sessionInfo = executor.GetType().GetField("_sessionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            NbSessionInfo mockSessionInfo = new NbSessionInfo();
            sessionInfo.SetValue(executor, mockSessionInfo);

            var request = executor.CreateRequest("/test", HttpMethod.Post);

            Assert.AreEqual(request.Method, HttpMethod.Post);
            Assert.AreEqual(request.Uri, _path);
            Assert.AreEqual(3, request.Headers.Count);
            Assert.AreEqual(_service.AppId, request.Headers["X-Application-Id"]);
            Assert.AreEqual(_service.AppKey, request.Headers["X-Application-Key"]);
            Assert.AreEqual(Header.UserAgentDefaultValue, request.Headers["User-Agent"]);
        }

        /// <summary>
        /// CreateRequest（セッショントークン有効期限切れ）
        /// 生成されたリクエストの内容が正しいこと
        /// "X-Session-Token"ヘッダが存在しないこと
        /// </summary>
        [Test]
        public void TestCreateRequestSubnormalSessionTokenExpired()
        {
            var executor = new NbRestExecutor();
            // sessionInfoを差し替える 
            FieldInfo sessionInfo = executor.GetType().GetField("_sessionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            NbSessionInfo mockSessionInfo = new NbSessionInfo();
            var user = new NbUser();
            mockSessionInfo.Set("token", NbUtil.CurrentUnixTime() - 1, user);
            sessionInfo.SetValue(executor, mockSessionInfo);

            var request = executor.CreateRequest("/test", HttpMethod.Put);

            Assert.AreEqual(request.Method, HttpMethod.Put);
            Assert.AreEqual(request.Uri, _path);
            Assert.AreEqual(3, request.Headers.Count);
            Assert.AreEqual(_service.AppId, request.Headers["X-Application-Id"]);
            Assert.AreEqual(_service.AppKey, request.Headers["X-Application-Key"]);
            Assert.AreEqual(Header.UserAgentDefaultValue, request.Headers["User-Agent"]);
        }

        /// <summary>
        /// CreateRequest（EndpointUrlがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test]
        public void TestCreateRequestExceptionEndpointUrlNull()
        {
            // SetUp
            NbService.EnableMultiTenant(true);
            var nbService = NbService.GetInstance();

            var executor = new NbRestExecutor(nbService);

            try
            {
                var request = executor.CreateRequest("/test", HttpMethod.Get);
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            finally
            {
                // TearDown
                NbService.EnableMultiTenant(false);
            }
        }

        /// <summary>
        /// CreateRequest（TenantIdがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestCreateRequestExceptionTenantIdNull()
        {
            _service.TenantId = null;

            var executor = new NbRestExecutor();
            var request = executor.CreateRequest("/test", HttpMethod.Get);
        }

        /// <summary>
        /// CreateRequest（AppIdがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestCreateRequestExceptionAppIdNull()
        {
            _service.AppId = null;

            var executor = new NbRestExecutor();
            var request = executor.CreateRequest("/test", HttpMethod.Get);
        }

        /// <summary>
        /// CreateRequest（AppKeyがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestCreateRequestExceptionAppKeyNull()
        {
            _service.AppKey = null;

            var executor = new NbRestExecutor();
            var request = executor.CreateRequest("/test", HttpMethod.Get);
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する

        /**
        * _ExecuteRequest
        **/
        // NbRestRequest.SendAsync()をvirtualにする必要があること、
        // 分岐処理がないことから、UT対象外とする

        // ExecuteRequest()、ExecuteRequestForJson()には、
        // それを継承したクラスMockRestExecutorを使って検証する
        /**
        * ExecuteRequest
        **/
        /// <summary>
        /// ExecuteRequest（正常）
        /// レスポンスを取得できること
        /// </summary>
        [Test]
        public async void TestExecuteRequestNormal()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK);
            var byteData = System.Text.Encoding.UTF8.GetBytes("TestData");
            response.Content = new ByteArrayContent(byteData);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            response.Headers.Add("X-Content-Length", byteData.Length.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);
            var result = await executor.ExecuteRequest(request);

            Assert.AreEqual(response.Headers, result.Headers);
            Assert.AreEqual(byteData, result.RawBytes);
            Assert.AreEqual("text/plain", result.ContentType);
            Assert.AreEqual(byteData.Length, result.ContentLength);
        }

        /// <summary>
        /// ExecuteRequest（データサイズが0）
        /// レスポンスを取得できること、例外が発生しないこと
        /// </summary>
        [Test]
        public async void TestExecuteRequestNormalEmpty()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK);
            var byteData = new byte[0];
            response.Content = new ByteArrayContent(byteData);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            response.Headers.Add("X-Content-Length", byteData.Length.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);
            var result = await executor.ExecuteRequest(request);

            Assert.AreEqual(response.Headers, result.Headers);
            Assert.AreEqual(byteData, result.RawBytes);
            Assert.AreEqual("text/plain", result.ContentType);
            Assert.AreEqual(byteData.Length, result.ContentLength);
        }

        /// <summary>
        /// ExecuteRequest（ステータスコードが101）
        /// NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestExecuteRequestExceptionSwitchingProtocols()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.SwitchingProtocols, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);

            try
            {
                var result = await executor.ExecuteRequest(request);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.SwitchingProtocols, e.StatusCode);
                Assert.AreEqual(response, e.Response);
            }
        }

        /// <summary>
        /// ExecuteRequest（正常:201 Created）
        /// レスポンスを取得できること
        /// </summary>
        [Test]
        public async void TestExecuteRequestNormalCreated()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.Created, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);
            var result = await executor.ExecuteRequest(request);
        }

        /// <summary>
        /// ExecuteRequest（ステータスコードが300）
        /// NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestExecuteRequestExceptionMultipleChoices()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.MultipleChoices, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);

            try
            {
                var result = await executor.ExecuteRequest(request);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.MultipleChoices, e.StatusCode);
                Assert.AreEqual(response, e.Response);
            }
        }

        /// <summary>
        /// ExecuteRequest（ステータスコードが500）
        /// NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestExecuteRequestException()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.InternalServerError, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);

            try
            {
                var result = await executor.ExecuteRequest(request);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.InternalServerError, e.StatusCode);
                Assert.AreEqual(response, e.Response);
            }
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する

        /**
        * ExecuteRequestForJson
        **/
        /// <summary>
        /// ExecuteRequestForJson（正常）
        /// レスポンスを取得できること
        /// </summary>
        [Test]
        public async void TestExecuteRequestForJsonNormal()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject()
            {
                {"_id", "12345"},
                {"username", "foo"},
                {"email", "foo@example.com"},
                {"createdAt", ""},
                {"updatedAt", ""}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);
            var result = await executor.ExecuteRequestForJson(request);

            Assert.AreEqual(json, result);
        }

        /// <summary>
        /// ExecuteRequestForJson（レスポンスのJSONが空）
        /// 空のJSONObjectを取得できること、例外が発生しないこと
        /// </summary>
        [Test]
        public async void TestExecuteRequestForJsonSubnormalEmpty()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);
            var result = await executor.ExecuteRequestForJson(request);

            Assert.AreEqual(json, result);
        }

        /// <summary>
        /// ExecuteRequestForJson（レスポンスボディが存在しない）
        /// 空のJSONObjectを取得できること、例外が発生しないこと
        /// </summary>
        [Test]
        public async void TestExecuteRequestForJsonSubnormalContentNull()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.OK);
            response.Content = null;
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);
            var result = await executor.ExecuteRequestForJson(request);

            Assert.AreEqual(json, result);
        }

        /// <summary>
        /// ExecuteRequestForJson（ステータスコードが101）
        /// NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestExecuteRequestForJsonExceptionSwitchingProtocols()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.SwitchingProtocols, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);

            try
            {
                var result = await executor.ExecuteRequestForJson(request);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.SwitchingProtocols, e.StatusCode);
                Assert.AreEqual(response, e.Response);
            }
        }

        /// <summary>
        /// ExecuteRequestForJson（正常:201 Created）
        /// レスポンスを取得できること
        /// </summary>
        [Test]
        public async void TestExecuteRequestForJsonNormalCreated()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.Created, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);
            var result = await executor.ExecuteRequestForJson(request);
        }

        /// <summary>
        /// ExecuteRequestForJson（ステータスコードが300）
        /// NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestExecuteRequestForJsonExceptionMultipleChoices()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.MultipleChoices, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);

            try
            {
                var result = await executor.ExecuteRequestForJson(request);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.MultipleChoices, e.StatusCode);
                Assert.AreEqual(response, e.Response);
            }
        }

        /// <summary>
        /// ExecuteRequestForJson（ステータスコードが404）
        /// NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestExecuteRequestForJsonException()
        {
            var executor = new MockRestExecutor();

            // inject rest executor
            _service.RestExecutor = executor;

            // Set Dummy Response
            var json = new NbJsonObject();
            var response = new MockRestResponse(HttpStatusCode.NotFound, json.ToString());
            executor.AddResponse(response);

            var request = executor.CreateRequest("/test", HttpMethod.Get);

            try
            {
                var result = await executor.ExecuteRequestForJson(request);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
                Assert.AreEqual(response, e.Response);
            }
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
    }
}
