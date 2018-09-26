using Nec.Nebula.Internal;
using NUnit.Framework;

namespace Nec.Nebula.Test.Internal
{
    [TestFixture]
    class NbSessionInfoTest
    {
        private NbSessionInfo _session;

        [SetUp]
        public void Setup()
        {
            _session = new NbSessionInfo();
        }

        [TearDown]
        public void TearDown()
        {
            _session = null;
        }


        /**
         * Set
         **/

        /// <summary>
        /// セッション情報をセットする（正常）
        /// セッション情報をセットできること
        /// </summary>
        [Test]
        public void TestSetNomal()
        {
            var user = new NbUser();

            _session.Set("token", 12345L, user);
            Assert.AreEqual("token", _session.SessionToken);
            Assert.AreEqual(12345L, _session.Expire);
            Assert.AreSame(user, _session.CurrentUser);
        }

        /// <summary>
        /// セッション情報をセットする（準正常）
        /// セットされる情報がnull（または0）でもExceptionが発行されないこと
        /// </summary>
        [Test]
        public void TestSetSubNomalNoSessionInfo()
        {
            _session.Set(null, 0, null);
            Assert.IsNull(_session.SessionToken);
            Assert.AreEqual(0, _session.Expire);
            Assert.IsNull(_session.CurrentUser);
        }


        /**
         * Clear
         **/

        /// <summary>
        /// セッションをクリアする（正常）
        /// セッション情報をクリアできること
        /// </summary>
        [Test]
        public void TestClearNomal()
        {
            var user = new NbUser();

            _session.Set("token", 12345L, user);
            Assert.AreEqual("token", _session.SessionToken);
            Assert.AreEqual(12345L, _session.Expire);
            Assert.AreSame(user, _session.CurrentUser);

            _session.Clear();
            Assert.IsNull(_session.SessionToken);
            Assert.AreEqual(0, _session.Expire);
            Assert.IsNull(_session.CurrentUser);
        }

        /// <summary>
        /// セッションをクリアする（準正常）
        /// セッション情報が設定されていない状態でクリアしてもExceptionが発行されないこと
        /// </summary>
        [Test]
        public void TestClearSubNomalNoSet()
        {
            _session.Clear();
            Assert.IsNull(_session.SessionToken);
            Assert.AreEqual(0, _session.Expire);
            Assert.IsNull(_session.CurrentUser);
        }


        /**
         * IsAvailable
         **/

        /// <summary>
        /// セッションが有効か調べる（有効）
        /// trueが返却されること
        /// </summary>
        [Test]
        public void TestIsAvailableNomaltrue()
        {
            _session.Set("token", 999999999999L, null);
            Assert.IsTrue(_session.IsAvailable());
        }

        /// <summary>
        /// セッションが有効か調べる（セッショントークンなし）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAvailableSubNomalfalseNoSessionToken()
        {
            Assert.IsFalse(_session.IsAvailable());
        }

        /// <summary>
        /// セッションが有効か調べる（有効期限切れ）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAvailableSubNomalfalseExpired()
        {
            _session.Set("token", 1L, null);
            Assert.IsFalse(_session.IsAvailable());
        }
    }
}
