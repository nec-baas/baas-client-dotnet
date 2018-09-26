using NUnit.Framework;
using Nec.Nebula.Internal;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbGcmFieldsTest
    {
        private const string title = "testTitle";
        private const string uri = "http://www.pushtest.test.com";

        /// <summary>
        /// コンストラクタテスト。
        /// Fieldsがnewされること。
        /// Fields以外のプロパティの初期値がnullであること。
        /// 未設定の状態でgetterをコールしても、Exceptionが発生しないこと。
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            // Main
            var gcm = new NbGcmFields();

            // Assert
            Assert.IsEmpty(gcm.Fields);
            Assert.IsNull(gcm.Title);
            Assert.IsNull(gcm.Uri);
        }

        /// <summary>
        /// Titleテスト。
        /// Fieldsに"title"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestTitleNormal()
        {
            var gcm = new NbGcmFields();

            // Main
            gcm.Title = title;

            // Assert
            Assert.AreEqual(title, gcm.Title);
            Assert.AreEqual(title, gcm.Fields[Field.Title]);
        }

        /// <summary>
        /// Uriテスト。
        /// Fieldsに"uri"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestSoundNormal()
        {
            var gcm = new NbGcmFields();

            // Main
            gcm.Uri = uri;

            // Assert
            Assert.AreEqual(uri, gcm.Uri);
            Assert.AreEqual(uri, gcm.Fields[Field.Uri]);
        }

        /// <summary>
        /// 全てのプロパティテスト。
        /// Fieldsにkeyとvalueが正しく設定されていること。
        /// </summary>
        [Test]
        public void TestAllNormal()
        {
            var gcm = new NbGcmFields();

            // Main
            gcm.Title = title;
            gcm.Uri = uri;

            // Assert
            Assert.AreEqual(title, gcm.Title);
            Assert.AreEqual(uri, gcm.Uri);
            Assert.AreEqual(title, gcm.Fields[Field.Title]);
            Assert.AreEqual(uri, gcm.Fields[Field.Uri]);
        }

        /// <summary>
        /// 全てのプロパティテスト。
        /// Fieldsにkeyとvalueが正しく設定されていること。
        /// null を設定した場合にフィールドから削除されること。
        /// </summary>
        [Test]
        public void TestAllNormalTwice()
        {
            var gcm = new NbGcmFields();

            // Main
            gcm.Title = title;
            gcm.Title = null;
            gcm.Uri = uri;
            gcm.Uri = null;

            // Assert
            Assert.IsNull(gcm.Title);
            Assert.IsNull(gcm.Uri);

            Assert.AreEqual(0, gcm.Fields.Count);
            Assert.IsFalse(gcm.Fields.ContainsKey(Field.Title));
            Assert.IsFalse(gcm.Fields.ContainsKey(Field.Uri));
        }
    }
}

