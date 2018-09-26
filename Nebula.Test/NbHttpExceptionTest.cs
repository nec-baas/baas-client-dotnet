using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbHttpExceptionTest
    {
        /**
        * デフォルトコンストラクタ
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// HTTPステータスコード、RESTレスポンスには初期値が格納されていること
        /// </summary>
        [Test]
        public void TestDefaultConstructorNormal()
        {
            var e = new NbHttpException();

            Assert.AreEqual((HttpStatusCode)0, e.StatusCode);
            Assert.IsNull(e.Response);
        }

        /**
        * コンストラクタ 引数がmessage
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// メッセージには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructWithMessageNormal()
        {
            var e = new NbHttpException("message");

            Assert.AreEqual("message", e.Message);
        }

        /// <summary>
        /// コンストラクタテスト（messageがnull）
        /// messageにnullを指定した場合、メッセージには初期値が格納されていること
        /// </summary>
        [Test]
        public void TestConstructWithMessageSubnormalMessageNull()
        {
            var e = new NbHttpException((string)null);

            Assert.IsNotNull(e.Message);
        }

        /**
        * コンストラクタ 引数がmessage、inner
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// メッセージ、内部例外には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructWithMessageAndInnerNormal()
        {
            var inner = new InvalidOperationException();
            var e = new NbHttpException("message", inner);

            Assert.AreEqual("message", e.Message);
            Assert.AreSame(inner, e.InnerException);
        }

        /// <summary>
        /// コンストラクタテスト（messageがnull）
        /// messageにnullを指定した場合、メッセージには初期値が格納されていること
        /// </summary>
        [Test]
        public void TestConstructWithMessageAndInnerSubnormalMessageNull()
        {
            var inner = new InvalidOperationException();
            var e = new NbHttpException(null, inner);

            Assert.IsNotNull(e.Message);
            Assert.AreSame(inner, e.InnerException);
        }

        /// <summary>
        /// コンストラクタテスト（innerがnull）
        /// innerにnullを指定した場合、内部例外には初期値が格納されていること
        /// </summary>
        [Test]
        public void TestConstructWithMessageAndInnerSubnormalInnerNull()
        {
            var e = new NbHttpException("message", null);

            Assert.AreEqual("message", e.Message);
            Assert.IsNull(e.InnerException);
        }

        /**
        * コンストラクタ 引数がstatusCode
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// HTTPステータスコードには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructWithStatusCodeNormal()
        {
            var e = new NbHttpException(HttpStatusCode.BadRequest);

            Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            Assert.IsNull(e.Response);
        }

        // 引数にnullは設定できないため、UT対象外とする

        /**
        * コンストラクタ 引数がstatusCode、message
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// HTTPステータスコード、メッセージには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructWithStatusCodeAndMessageNormal()
        {
            var e = new NbHttpException(HttpStatusCode.BadRequest, "bad request");

            Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            Assert.AreEqual("bad request", e.Message);
            Assert.IsNull(e.Response);
        }

        /// <summary>
        /// コンストラクタテスト（messageがnull）
        /// messageにnullを指定した場合、メッセージには初期値が格納されていること
        /// </summary>
        [Test]
        public void TestConstructWithStatusCodeAndMessageSubnormalMessageNull()
        {
            var e = new NbHttpException(HttpStatusCode.BadRequest, null);

            Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            Assert.IsNotNull(e.Message);
            Assert.IsNull(e.Response);
        }

        // 第一引数にnullは設定できないため、UT対象外とする

        /**
        * コンストラクタ 引数がresponse
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// REST Response、HTTPステータスコード、メッセージには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructWithResponseNormal()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                ReasonPhrase = "test"
            };
            var e = new NbHttpException(response);

            Assert.AreSame(response, e.Response);
            Assert.AreEqual(HttpStatusCode.OK, e.StatusCode);
            Assert.AreEqual(response.ReasonPhrase, e.Message);
        }

        /// <summary>
        /// コンストラクタテスト（responseがnull）
        /// responseにnullを指定した場合、REST Response、HTTPステータスコード、メッセージには初期値が格納されていること
        /// </summary>
        [Test]
        public void TestConstructWithResponseSubnormalResponseNull()
        {
            var e = new NbHttpException((HttpResponseMessage)null);

            Assert.IsNull(e.Response);
            Assert.AreEqual((HttpStatusCode)0, e.StatusCode);
            Assert.IsNotNull(e.Message);
        }
    }
}
