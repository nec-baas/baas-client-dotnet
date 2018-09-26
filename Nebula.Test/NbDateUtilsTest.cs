using NUnit.Framework;
using System;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbDateUtilsTest
    {
        [Test]
        public void TestParseDateTimeNormal()
        {
            var dt = NbDateUtils.ParseDateTime("2015-01-02T01:23:45.678Z");
            Assert.NotNull(dt);
            Assert.AreEqual(DateTimeKind.Utc, dt.Kind);
            Assert.AreEqual(2015, dt.Year);
            Assert.AreEqual(1, dt.Month);
            Assert.AreEqual(2, dt.Day);
            Assert.AreEqual(1, dt.Hour);
            Assert.AreEqual(23, dt.Minute);
            Assert.AreEqual(45, dt.Second);
            Assert.AreEqual(678, dt.Millisecond);
        }

        [Test]
        public void TestParseDateTimeExceptionNullArgument()
        {
            try
            {
                NbDateUtils.ParseDateTime(null);
            }
            catch (ArgumentNullException)
            {
                // 期待動作
            }
        }

        [Test]
        public void TestParseDateTimeExceptionInvalidFormat()
        {
            try
            {
                var dt = NbDateUtils.ParseDateTime("2015-01-02T01:23:45.678ZZ");
            }
            catch (FormatException)
            {
                // 期待動作
            }
        }


        [Test]
        public void TestToStringNormal()
        {
            var dt = new DateTime(2015, 1, 2, 1, 23, 45, DateTimeKind.Utc).AddMilliseconds(678).ToLocalTime();

            Assert.AreEqual("2015-01-02T01:23:45.678Z", NbDateUtils.ToString(dt));
        }

        /// <summary>
        /// ToStringの引数は構造体のためnull設定不可
        /// </summary>
        [Test]
        public void TestToStringNormalEmptyDate()
        {
            var dt = new DateTime();
            Assert.AreEqual("0001-01-01T00:00:00.000Z", NbDateUtils.ToString(dt));
        }
    }
}
