using NUnit.Framework;
using Nec.Nebula.Internal;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbApnsFieldsTest
    {
        private const string sound = "sound1.aiff";
        private const string category = "MESSAGE_CATEGORY";

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
            var apns = new NbApnsFields();

            // Assert
            Assert.IsEmpty(apns.Fields);
            Assert.IsNull(apns.Badge);
            Assert.IsNull(apns.Sound);
            Assert.IsNull(apns.ContentAvailable);
            Assert.IsNull(apns.Category);
        }

        /// <summary>
        /// Badgeテスト。
        /// Fieldsに"badge"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestBadgeNormal()
        {
            var apns = new NbApnsFields();

            // Main
            apns.Badge = 2;

            // Assert
            Assert.AreEqual(2, apns.Badge);
            Assert.AreEqual(2, apns.Fields[Field.Badge]);
        }

        /// <summary>
        /// Soundテスト。
        /// Fieldsに"sound"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestSoundNormal()
        {
            var apns = new NbApnsFields();

            // Main            
            apns.Sound = sound;

            // Assert
            Assert.AreEqual(sound, apns.Sound);
            Assert.AreEqual(sound, apns.Fields[Field.Sound]);
        }

        /// <summary>
        /// ContentAvailableテスト。
        /// Fieldsに"content-available"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestContentAvailableNormal()
        {
            var apns = new NbApnsFields();

            // Main
            apns.ContentAvailable = 1;

            // Assert
            Assert.AreEqual(1, apns.ContentAvailable);
            Assert.AreEqual(1, apns.Fields[Field.ContentAvailable]);
        }

        /// <summary>
        /// Categoryテスト。
        /// Fieldsに"category"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestCategoryNormal()
        {
            var apns = new NbApnsFields();

            // Main
            apns.Category = category;

            // Assert
            Assert.AreEqual(category, apns.Category);
            Assert.AreEqual(category, apns.Fields[Field.Category]);
        }

        /// <summary>
        /// 全てのプロパティテスト。
        /// Fieldsにkeyとvalueが正しく設定されていること。
        /// </summary>
        [Test]
        public void TestAllNormal()
        {
            var apns = new NbApnsFields();

            // Main
            apns.Badge = 2;
            apns.Sound = sound;
            apns.ContentAvailable = 1;
            apns.Category = category;

            // Assert
            Assert.AreEqual(2, apns.Badge);
            Assert.AreEqual(sound, apns.Sound);
            Assert.AreEqual(1, apns.ContentAvailable);
            Assert.AreEqual(category, apns.Category);
            Assert.AreEqual(2, apns.Fields[Field.Badge]);
            Assert.AreEqual(sound, apns.Fields[Field.Sound]);
            Assert.AreEqual(1, apns.Fields[Field.ContentAvailable]);
            Assert.AreEqual(category, apns.Fields[Field.Category]);
        }

        /// <summary>
        /// 全てのプロパティテスト。
        /// null を指定した場合にフィールドから削除されること
        /// </summary>
        [Test]
        public void TestAllNormalTwice()
        {
            var apns = new NbApnsFields();

            // Main
            apns.Badge = 2;
            apns.Badge = null;
            apns.Sound = sound;
            apns.Sound = null;
            apns.ContentAvailable = 1;
            apns.ContentAvailable = null;
            apns.Category = category;
            apns.Category = null;

            // Assert
            Assert.IsNull(apns.Badge);
            Assert.IsNull(apns.Sound);
            Assert.IsNull(apns.ContentAvailable);
            Assert.IsNull(apns.Category);

            Assert.AreEqual(0, apns.Fields.Count);
            Assert.IsFalse(apns.Fields.ContainsKey(Field.Badge));
            Assert.IsFalse(apns.Fields.ContainsKey(Field.Sound));
            Assert.IsFalse(apns.Fields.ContainsKey(Field.ContentAvailable));
            Assert.IsFalse(apns.Fields.ContainsKey(Field.Category));
        }

    }
}
