using Moq;
using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbRestRequestTest
    {
        private const string BaseUrl = "http://www.nebula.test.com";
        private const string Path = "/hoge";
        private NbService _service;

        [SetUp]
        public void Setup()
        {
            TestUtils.Init();
            _service = NbService.Singleton;
        }

        /**
        * Constructor
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// HTTP メソッド、URIには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, Path, HttpMethod.Post);

            Assert.IsEmpty(request.Headers);
            Assert.IsEmpty(request.QueryParams);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            // privateフィールドとなるため、Reflectionを使用
            FieldInfo client = request.GetType().GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(client.GetValue(request));
            Assert.AreEqual(BaseUrl + Path, request.Uri);
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する

        /**
        * SendAsync 引数はmessage
        **/
        // HttpClient.SendAsync()はMock化できないため、UT対象外とする

        /**
        * SetRequestBody
        **/
        /// <summary>
        /// SetRequestBody（正常）
        /// コンテンツには指定の値が格納されること
        /// </summary>
        [Test]
        public async void TestSetRequestBodyNormal()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, Path, HttpMethod.Post);
            var byteData = System.Text.Encoding.UTF8.GetBytes("TestData");
            request.SetRequestBody("text/plain", byteData);

            Assert.IsNotNull(request.Content);
            var data = await request.Content.ReadAsByteArrayAsync();
            Assert.AreEqual(byteData, data);
            Assert.AreEqual("text/plain", request.Content.Headers.ContentType.ToString());
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
        // なお、いずれかの引数にnullが設定されると、例外が発生する

        /**
        * SetJsonBody
        **/
        /// <summary>
        /// SetJsonBody（正常）
        /// コンテンツには指定の値が格納されること
        /// </summary>
        [Test]
        public async void TestSetJsonBodyNormal()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, Path, HttpMethod.Put);
            var json = new NbJsonObject
            {
                {"testKey", "testValue"}
            };
            request.SetJsonBody(json);

            Assert.IsNotNull(request.Content);
            var data = await request.Content.ReadAsStringAsync();
            Assert.AreEqual(json, NbJsonObject.Parse(data));
            Assert.AreEqual("application/json; charset=utf-8", request.Content.Headers.ContentType.ToString());
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
        // なお、引数にnullが設定されると、例外が発生する

        /**
        * SetHeader
        **/
        /// <summary>
        /// SetHeader（正常）
        /// ヘッダには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestSetHeaderNormal()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, Path, HttpMethod.Post);
            request.SetHeader("X-Application-Id", "testAppId");
            request.SetHeader("X-Application-Key", "testAppKey");

            var headers = request.Headers;
            Assert.AreEqual(2, request.Headers.Count);
            Assert.AreEqual("testAppId", request.Headers["X-Application-Id"]);
            Assert.AreEqual("testAppKey", request.Headers["X-Application-Key"]);
        }

        /// <summary>
        /// SetHeader（同一名ヘッダあり）
        /// ヘッダには指定の値が格納されること
        /// 同一名のヘッダは上書きされること
        /// </summary>
        [Test]
        public void TestSetHeaderSubnormalOverwrite()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, Path, HttpMethod.Post);
            request.SetHeader("X-Application-Id", "testAppId");
            request.SetHeader("X-Application-Key", "testAppKey");
            request.SetHeader("X-Application-Id", "newTestAppId");

            var headers = request.Headers;
            Assert.AreEqual(2, request.Headers.Count);
            Assert.AreEqual("newTestAppId", request.Headers["X-Application-Id"]);
            Assert.AreEqual("testAppKey", request.Headers["X-Application-Key"]);
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
        // なお、第一引数にnullが設定されると、例外が発生する

        /**
        * SetUrlSegment
        **/
        /// <summary>
        /// SetUrlSegment（正常）
        /// URIには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNormal()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegment("test", "abcd");

            Assert.AreEqual(BaseUrl + "/hoge/abcd", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（URIエンコーディングあり）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていること
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNormalEncoding()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegment("test", "あいうえお");

            Assert.AreEqual(BaseUrl + "/hoge/%E3%81%82%E3%81%84%E3%81%86%E3%81%88%E3%81%8A", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（RFC3986 非予約文字）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていないこと
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNormalUnreservedCharacters()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegment("test", "a0-._~");

            Assert.AreEqual(BaseUrl + "/hoge/a0-._~", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（RFC3986 予約文字）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていること
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNormalReservedCharacters()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegment("test", ":/?#[]@!$&'()*+,;=");

            Assert.AreEqual(BaseUrl + "/hoge/%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（その他）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていること
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNormalOther()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegment("test", " \"%^|{}<>\\あア漢");

            Assert.AreEqual(BaseUrl + "/hoge/%20%22%25%5E%7C%7B%7D%3C%3E%5C%E3%81%82%E3%82%A2%E6%BC%A2", request.Uri);
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
        // なお、いずれかの引数にnullが設定されると、例外が発生する

        /**
        * SetUrlSegmentNoEscape
        **/
        /// <summary>
        /// SetUrlSegment（正常）
        /// URIには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestSetUrlSegmenNoEscapeNormal()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegmentNoEscape("test", "abcd");

            Assert.AreEqual(BaseUrl + "/hoge/abcd", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（URIエンコーディングあり）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていないこと
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNoEscapeNormalEncoding()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegmentNoEscape("test", "あいうえお");

            Assert.AreEqual(BaseUrl + "/hoge/あいうえお", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（RFC3986 非予約文字）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていないこと
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNoEscapeNormalUnreservedCharacters()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegmentNoEscape("test", "a0-._~");

            Assert.AreEqual(BaseUrl + "/hoge/a0-._~", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（RFC3986 予約文字）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていないこと
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNoEscapeNormalReservedCharacters()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegmentNoEscape("test", ":/?#[]@!$&'()*+,;=");

            Assert.AreEqual(BaseUrl + "/hoge/:/?#[]@!$&'()*+,;=", request.Uri);
        }

        /// <summary>
        /// SetUrlSegment（その他）
        /// URIには指定の値が格納されること
        /// URIエンコーディングされていないこと
        /// </summary>
        [Test]
        public void TestSetUrlSegmentNoEscapeNormalOther()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, "/hoge/{test}", HttpMethod.Put);
            request.SetUrlSegmentNoEscape("test", " \"%^|{}<>\\あア漢");

            Assert.AreEqual(BaseUrl + "/hoge/ \"%^|{}<>\\あア漢", request.Uri);
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
        // なお、いずれかの引数にnullが設定されると、例外が発生する

        /**
        * SetQueryParameter
        **/
        /// <summary>
        /// SetQueryParameter（正常）
        /// クエリパラメータには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestSetQueryParameterNormal()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, Path, HttpMethod.Delete);
            request.SetQueryParameter("testKey1", "testValue1");
            request.SetQueryParameter("testKey2", "1");

            Assert.AreEqual(2, request.QueryParams.Count);
            Assert.AreEqual("testValue1", request.QueryParams["testKey1"]);
            Assert.AreEqual("1", request.QueryParams["testKey2"]);
        }

        /// <summary>
        /// SetQueryParameter（同一名のパラメータ）
        /// クエリパラメータには指定の値が格納されること
        /// 同一名のパラメータは上書きされること
        /// </summary>
        [Test]
        public void TestSetQueryParameterSubnormalOverwrite()
        {
            var request = new NbRestRequest(new HttpClient(), BaseUrl, Path, HttpMethod.Delete);
            request.SetQueryParameter("testKey1", "testValue1");
            request.SetQueryParameter("testKey2", "1");
            request.SetQueryParameter("testKey1", "testValue2");

            Assert.AreEqual(2, request.QueryParams.Count);
            Assert.AreEqual("testValue2", request.QueryParams["testKey1"]);
            Assert.AreEqual("1", request.QueryParams["testKey2"]);
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
        // なお、第一引数にnullが設定されると、例外が発生する

        /**
        * LogRequest
        **/
        // Debug.WriteLine()がstaticメソッドでMock化できないため、UT対象外とする

        /**
        * LogResponse
        **/
        // Debug.WriteLine()がstaticメソッドでMock化できないため、UT対象外とする

        /**
        * SendAsync
        **/
        /// <summary>
        /// SendAsync（ヘッダなし、クエリパラメータなし、リクエストボディなし）
        /// 生成されたリクエストメッセージが正しいこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalNoAll()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Get) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            var json = new NbJsonObject()
            {
                {"testKey", "testValue"}
            };
            response.Content = new StringContent(json.ToString());

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Get;
            var uriBuilder = new UriBuilder(BaseUrl + Path);
            request.RequestUri = uriBuilder.Uri;

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);

            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            var bodyString = await result.Content.ReadAsStringAsync();
            Assert.AreEqual(json, NbJsonObject.Parse(bodyString));
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータなし、リクエストボディなし）
        /// 生成されたリクエストメッセージが正しいこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalHeader()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Delete) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.InternalServerError;
            var json = new NbJsonObject()
            {
                {"testKey", "testValue"}
            };
            response.Content = new StringContent(json.ToString());

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Delete;
            var uriBuilder = new UriBuilder(BaseUrl + Path);
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            var bodyString = await result.Content.ReadAsStringAsync();
            Assert.AreEqual(json, NbJsonObject.Parse(bodyString));
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータあり、リクエストボディなし）
        /// 生成されたリクエストメッセージが正しいこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalHeaderAndQuery()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Get) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Get;
            var uriBuilder = new UriBuilder(BaseUrl + Path + "?testKey1=testValue1&testKey2=testValue2");
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            mock.Object.SetQueryParameter("testKey1", "testValue1");
            mock.Object.SetQueryParameter("testKey2", "testValue2");
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータあり、リクエストボディなし）
        /// 生成されたリクエストメッセージが正しいこと
        /// クエリパラメータ部分がURIエンコーディングされていること
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalHeaderAndQueryEncoding()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Get) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Get;
            var uriBuilder = new UriBuilder(BaseUrl + Path + "?testKey1=testValue1&testKey2=%E3%81%82%E3%81%84%E3%81%86%E3%81%88%E3%81%8A");
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            mock.Object.SetQueryParameter("testKey1", "testValue1");
            mock.Object.SetQueryParameter("testKey2", "あいうえお");
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータあり、リクエストボディなし）
        /// 生成されたリクエストメッセージが正しいこと
        /// クエリパラメータ部分がURIエンコーディングされていないこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalHeaderAndQueryUnreservedCharacters()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Get) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Get;
            var uriBuilder = new UriBuilder(BaseUrl + Path + "?testKey1=a0-._~&testKey2=a0-._~");
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            mock.Object.SetQueryParameter("testKey1", "a0-._~");
            mock.Object.SetQueryParameter("testKey2", "a0-._~");
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータあり、リクエストボディなし）
        /// 生成されたリクエストメッセージが正しいこと
        /// クエリパラメータ部分がURIエンコーディングされていること
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalHeaderAndQueryReservedCharacters()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Get) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Get;
            var uriBuilder = new UriBuilder(BaseUrl + Path + "?testKey1=%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D&testKey2=%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D");
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            mock.Object.SetQueryParameter("testKey1", ":/?#[]@!$&'()*+,;=");
            mock.Object.SetQueryParameter("testKey2", ":/?#[]@!$&'()*+,;=");
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータあり、リクエストボディなし）
        /// 生成されたリクエストメッセージが正しいこと
        /// クエリパラメータ部分がURIエンコーディングされていること
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalHeaderAndQueryOther()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Get) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Get;
            var uriBuilder = new UriBuilder(BaseUrl + Path + "?testKey1=%20%22%25%5E%7C%7B%7D%3C%3E%5C%E3%81%82%E3%82%A2%E6%BC%A2&testKey2=%20%22%25%5E%7C%7B%7D%3C%3E%5C%E3%81%82%E3%82%A2%E6%BC%A2");
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            mock.Object.SetQueryParameter("testKey1", " \"%^|{}<>\\あア漢");
            mock.Object.SetQueryParameter("testKey2", " \"%^|{}<>\\あア漢");
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータなし、リクエストボディあり）
        /// 生成されたリクエストメッセージが正しいこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalHeaderAndContent()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Post) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var json = new NbJsonObject()
            {
                {"testKey", "testValue"}
            };
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Post;
            var uriBuilder = new UriBuilder(BaseUrl + Path);
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");
            request.Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            mock.Object.SetJsonBody(json);
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mock.VerifyAll();
        }

        /// <summary>
        /// SendAsync（ヘッダあり、クエリパラメータあり、リクエストボディあり）
        /// 生成されたリクエストメッセージが正しいこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNormalAll()
        {
            // Set Mock
            var mock = new Mock<NbRestRequest>(new HttpClient(), BaseUrl, Path, HttpMethod.Get) { CallBase = true };

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            // SendAsync(message)の引数messageが正しいものかを検証（末尾のVerfiyAll()で検証される）
            var json = new NbJsonObject()
            {
                {"testKey", "testValue"}
            };
            var request = new DummyRequestMesasge();
            request.Method = HttpMethod.Get;
            var uriBuilder = new UriBuilder(BaseUrl + Path + "?testKey1=testValue1&testKey2=testValue2");
            request.RequestUri = uriBuilder.Uri;
            request.Headers.Add("X-Application-Id", "appId");
            request.Headers.Add("X-Application-Key", "appKey");
            request.Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

            mock.Setup(x => x.SendAsync(request)).ReturnsAsync(response);
            mock.Object.SetHeader("X-Application-Id", "appId");
            mock.Object.SetHeader("X-Application-Key", "appKey");
            mock.Object.SetQueryParameter("testKey1", "testValue1");
            mock.Object.SetQueryParameter("testKey2", "testValue2");
            mock.Object.SetJsonBody(json);
            var result = await mock.Object.SendAsync();

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mock.VerifyAll();
        }

        /// <summary>
        /// EscapeUriStringとEscapeDataStringの動作
        /// </summary>
        [Test]
        public void TestEscapeNormal()
        {
            // 非予約文字はどちらを使用しても、エンコードしない
            var str = "a0-._~";
            var encodeUri = Uri.EscapeUriString(str);
            Assert.False(encodeUri.Contains("%"));
            Assert.AreEqual(str, encodeUri);
            var encodeData = Uri.EscapeDataString(str);
            Assert.False(encodeData.Contains("%"));
            Assert.AreEqual(str, encodeData);

            // 予約文字はEscapeUriString()はエンコードしない、EscapeDataString()はエンコードする
            str = ":/?#[]@!$&'()*+,;=";
            encodeUri = Uri.EscapeUriString(str);
            Assert.False(encodeUri.Contains("%"));
            Assert.AreEqual(str, encodeUri);
            encodeData = Uri.EscapeDataString(str);
            Assert.AreEqual("%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D", encodeData);

            // その他はどちらを使用しても、エンコードする(\はエスケープのために入っているので、
            // 実際の文字は "%^|{}<>\あア漢 である
            str = " \"%^|{}<>\\あア漢";
            encodeUri = Uri.EscapeUriString(str);
            Assert.AreEqual("%20%22%25%5E%7C%7B%7D%3C%3E%5C%E3%81%82%E3%82%A2%E6%BC%A2", encodeUri);
            encodeData = Uri.EscapeDataString(str);
            Assert.AreEqual("%20%22%25%5E%7C%7B%7D%3C%3E%5C%E3%81%82%E3%82%A2%E6%BC%A2", encodeData);
        }

        // 引数チェックのためのダミークラス（簡易実装）
        class DummyRequestMesasge : HttpRequestMessage
        {
            public override bool Equals(object obj)
            {
                var comparedObj = obj as HttpRequestMessage;

                if (comparedObj == null) return false;

                if (!isEquals(Method, comparedObj.Method)) return false;
                if (!isEquals(RequestUri, comparedObj.RequestUri)) return false;
                if (!isEquals(Headers.ToString(), comparedObj.Headers.ToString())) return false;
                if (Content != null && comparedObj != null & !isEquals(Content.ToString(), comparedObj.Content.ToString())) return false;

                return true;
            }

            private bool isEquals(object c1, object c2)
            {
                if (c1 == null || c2 == null)
                {
                    if (c1 == c2)
                        return true;
                    else
                        return false;
                }

                return c1.Equals(c2);
            }

            public override int GetHashCode()
            {
                // テスト用なので、ここは適当
                return base.GetHashCode();
            }
        }
    }

}
