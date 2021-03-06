﻿using NUnit.Framework;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbRestJsonResponseTest
    {
        private NbRestJsonResponse _sujsont;
        private NbRestResponse _surawbyte;
        private HttpResponseMessage _response;
        private string _jsonData;
        private byte[] _byteData;

        [SetUp]
        public void Setup()
        {
            _response = new HttpResponseMessage();
            _jsonData = "{'foo': 'bar'}";
            _byteData = System.Text.Encoding.UTF8.GetBytes(_jsonData);
            _response.Headers.Add("X-Content-Length", _byteData.Length.ToString());
            _response.Headers.Add("X-ACL", "test");
            _response.Content = new ByteArrayContent(_byteData);
            _response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            _surawbyte = new NbRestResponse(_response, _byteData);
            _sujsont = new NbRestJsonResponse(_surawbyte);
        }

        /**
        * Constructor
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// レスポンス、データ、Content-Lengthには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            Assert.AreEqual(_response, _sujsont.Response);
            Assert.AreEqual(NbJsonObject.Parse(_jsonData), _sujsont.JsonObject);
            Assert.AreEqual(_byteData.Length, _sujsont.ContentLength);
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する

        /**
        * Headers
        **/
        /// <summary>
        /// Headers（正常）
        /// Response.Headersの値が取得できること
        /// </summary>
        [Test]
        public void TestHeadersNormal()
        {
            Assert.AreEqual("X-Content-Length: 14\r\nX-ACL: test\r\n", _sujsont.Headers.ToString());
        }

        // internalクラスとなるため、Responseがnullである場合のUTは省略する

        /**
        * GetHeader
        **/
        /// <summary>
        /// GetHeader（正常）
        /// 指定したヘッダの値を取得できること
        /// </summary>
        [Test]
        public void TestGetHeaderNormal()
        {
            Assert.AreEqual("14", _sujsont.GetHeader("X-Content-Length"));
        }

        /// <summary>
        /// GetHeader（指定したヘッダが存在しない）
        /// nullを取得すること
        /// </summary>
        [Test]
        public void TestGetHeaderSubnormalNotFound()
        {
            Assert.IsNull(_sujsont.GetHeader("testHoge"));
        }

        /// <summary>
        /// GetHeader（重複）
        /// 先頭の値を取得できること
        /// </summary>
        [Test]
        public void TestGetHeaderSubnormalOverwrite()
        {
            var response = new HttpResponseMessage();
            var byteData = System.Text.Encoding.UTF8.GetBytes("TestData");
            response.Headers.Add("X-Content-Length", "100");
            response.Headers.Add("X-Content-Length", byteData.Length.ToString());
            response.Content = new ByteArrayContent(byteData);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            var res = new NbRestResponse(response, byteData);

            Assert.AreEqual("100", res.GetHeader("X-Content-Length"));
        }

        /// <summary>
        /// GetHeader（引数にnullを設定）
        /// nullを取得すること
        /// </summary>
        [Test]
        public void TestGetHeaderSubnormalHeaderNameNull()
        {
            Assert.IsNull(_sujsont.GetHeader(null));
        }

        // internalクラスとなるため、Headersがnullである場合のUTは省略する

        /**
        * JsonObject
        **/
        /// <summary>
        /// JsonObject（正常）
        /// JsonObjectデータが取得できること
        /// </summary>
        [Test]
        public void TestJsonObjectNormal()
        {
            Assert.AreEqual( NbJsonObject.Parse(_jsonData), _sujsont.JsonObject);
        }

        /**
        * ContentType
        **/
        /// <summary>
        /// ContentType（正常）
        /// Content-Typeが取得できること
        /// </summary>
        [Test]
        public void TestContentTypeNormal()
        {
            Assert.AreEqual("text/plain", _sujsont.ContentType);
        }

        // internalクラスとなるため、Response,Content,Headersがnullである場合のUTは省略する
        // "Content-Type"ヘッダが見つからない場合のUTは省略する

        /**
        * ContentLength
        **/
        /// <summary>
        /// ContentLength（正常）
        /// Content-Lengthが取得できること
        /// </summary>
        [Test]
        public void TestContentLengthNormal()
        {
            Assert.AreEqual(_byteData.Length, _sujsont.ContentLength);
        }
    }
}
