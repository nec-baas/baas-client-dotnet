using NUnit.Framework;
using System.Net;
using System;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbExceptionTest
    {

        /**
         * Constructor
         **/
        /// <summary>
        /// デフォルトコンストラクタテスト（正常）
        /// ステータスコード、メッセージには初期値が格納されていること
        /// 異常終了しないこと
        /// </summary>
        [Test]
        public void TestDefaultConstructorNormal()
        {
            var e = new NbException();
            Assert.AreEqual(0, (int)e.StatusCode);
            Assert.IsNotNull(e.Message);
        }

        /// <summary>
        /// コンストラクタテスト（正常）
        /// ステータスコード、メッセージには指定の値が格納されること
        /// 異常終了しないこと
        /// </summary>
        [Test]
        public void TestConstructWithStatusCodeAndMessageNormal()
        {
            var e = new NbException(NbStatusCode.FailedToDownload, "test");
            Assert.AreEqual(NbStatusCode.FailedToDownload, e.StatusCode);
            Assert.AreEqual("test", e.Message);
        }


        /// <summary>
        /// コンストラクタテスト（準正常）
        /// 未定義のステータスコードを設定しても異常終了しないこと
        /// </summary>
        [Test]
        public void TestConstructWithStatusCodeAndMessageSubnormalWithInvalidID()
        {
            var e = new NbException((NbStatusCode)1 , "test");
            Assert.AreEqual(1, (int) e.StatusCode);
            Assert.AreEqual("test", e.Message);
        }

        /// <summary>
        /// コンストラクタテスト（異常）
        /// メッセージにnullを指定した場合、初期値が格納される
        /// </summary>
        [Test]
        public void TestConstructWithStatusCodeAndMessageSubnormalMessageNull()
        {
            var e = new NbException(NbStatusCode.FailedToDownload, null);
            Assert.AreEqual(NbStatusCode.FailedToDownload, e.StatusCode);
            Assert.IsNotNull(e.Message);
        }
    }
}
