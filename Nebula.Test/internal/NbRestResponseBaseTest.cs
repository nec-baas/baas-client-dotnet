using Nec.Nebula.Internal;
using NUnit.Framework;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Nec.Nebula.Test.Internal
{
    [TestFixture]
    public class NbRestResponseBaseTest
    {
        private NbRestResponseBase _sut;
        private HttpResponseMessage _response;
        private byte[] _byteData;

        [SetUp]
        public void Setup()
        {
            _response = new HttpResponseMessage();
            _byteData = System.Text.Encoding.UTF8.GetBytes("TestData");
            _response.Headers.Add("X-Content-Length", _byteData.Length.ToString());
            _response.Headers.Add("X-ACL", "test");
            _response.Content = new ByteArrayContent(_byteData);
            _response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            _sut = new NbRestResponseBase();
            _sut.Response = _response;
            _sut.ContentLength = _byteData.Length;
        }

        /**
        * Constructor
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// インスタンスが生成されること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            Assert.NotNull(_sut);
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
            Assert.AreEqual("X-Content-Length: 8\r\nX-ACL: test\r\n", _sut.Headers.ToString());
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
            Assert.AreEqual("8", _sut.GetHeader("X-Content-Length"));
        }

        /// <summary>
        /// GetHeader（指定したヘッダが存在しない）
        /// nullを取得すること
        /// </summary>
        [Test]
        public void TestGetHeaderSubnormalNotFound()
        {
            Assert.IsNull(_sut.GetHeader("testHoge"));
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

            var res = new NbRestResponseBase();
            res.Response = response;

            Assert.AreEqual("100", res.GetHeader("X-Content-Length"));
        }

        /// <summary>
        /// GetHeader（引数にnullを設定）
        /// nullを取得すること
        /// </summary>
        [Test]
        public void TestGetHeaderSubnormalHeaderNameNull()
        {
            Assert.IsNull(_sut.GetHeader(null));
        }

        // internalクラスとなるため、Headersがnullである場合のUTは省略する

        ///**
        //* RawBytes
        //**/
        ///// <summary>
        ///// RawBytes（正常）
        ///// Bodyデータが取得できること
        ///// </summary>
        //[Test]
        //public void TestRawBytesNormal()
        //{
        //    Assert.AreEqual(_byteData, _sut.RawBytes);
        //}

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
            Assert.AreEqual("text/plain", _sut.ContentType);
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
            Assert.AreEqual(_byteData.Length, _sut.ContentLength);
        }
    }
}
