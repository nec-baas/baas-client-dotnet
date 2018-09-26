using NUnit.Framework;
using Nec.Nebula.Internal;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbSseFieldsTest
    {
        private const string testId = "event001";
        private const string testType = "Information";

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
            var sse = new NbSseFields();

            // Assert
            Assert.IsEmpty(sse.Fields);
            Assert.IsNull(sse.EventId);
            Assert.IsNull(sse.EventType);
        }

        /// <summary>
        /// EventIdテスト。
        /// Fieldsに"sseEventId"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestTitleNormal()
        {
            var sse = new NbSseFields();

            // Main
            sse.EventId = testId;

            // Assert
            Assert.AreEqual(testId, sse.EventId);
            Assert.AreEqual(testId, sse.Fields[Field.SseEventId]);
        }

        /// <summary>
        /// EventTypeテスト。
        /// Fieldsに"sseEventType"キーが存在し、その値が正しいこと。
        /// </summary>
        [Test]
        public void TestSoundNormal()
        {
            var sse = new NbSseFields();

            // Main
            sse.EventType = testType;

            // Assert
            Assert.AreEqual(testType, sse.EventType);
            Assert.AreEqual(testType, sse.Fields[Field.SseEventType]);
        }

        /// <summary>
        /// 全てのプロパティテスト。
        /// Fieldsにkeyとvalueが正しく設定されていること。
        /// </summary>
        [Test]
        public void TestAllNormal()
        {
            var sse = new NbSseFields();

            // Main
            sse.EventId = testId;
            sse.EventType = testType;

            // Assert
            Assert.AreEqual(testId, sse.EventId);
            Assert.AreEqual(testType, sse.EventType);
            Assert.AreEqual(testId, sse.Fields[Field.SseEventId]);
            Assert.AreEqual(testType, sse.Fields[Field.SseEventType]);
        }

        /// <summary>
        /// 全てのプロパティテスト。
        /// 値を上書きした場合に、Fieldsにkeyとvalueが正しく設定されていること。
        /// null を指定した場合にフィールドから削除されること
        /// </summary>
        [Test]
        public void TestAllNormalTwice()
        {
            var sse = new NbSseFields();

            // Main
            sse.EventId = testId;
            sse.EventId = null;
            sse.EventType = testType;
            sse.EventType = null;

            // Assert
            Assert.IsNull(sse.EventId);
            Assert.IsNull(sse.EventType);

            Assert.AreEqual(0, sse.Fields.Count);
            Assert.IsFalse(sse.Fields.ContainsKey(Field.SseEventId));
            Assert.IsFalse(sse.Fields.ContainsKey(Field.SseEventType));
        }
    }
}

