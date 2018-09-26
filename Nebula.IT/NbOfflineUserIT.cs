using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;


namespace Nec.Nebula.IT
{
    [TestFixture]
    class NbOfflineUserIT
    {

        private const string UserName = "userTest";
        private const string Email = "userTest@example.com";
        private const string NewUserName = "userTest2";
        private const string NewEmail = "userTest2@example.com";
        private const string Password = ITUtil.Password;
        private NbJsonObject Options = new NbJsonObject { { "option1", "testOption1" }, { "option2", "testOption2" } };

        private const string ObjectBucetName = "testBucket";

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            SwitchOfflineService(true);
            ITUtil.InitOnlineUser().Wait();
        }

        [TearDown]
        public void TearDown()
        {
            SwitchOfflineService(false);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            ITUtil.InitOnlineUser().Wait();
        }

        private void SwitchOfflineService(bool enable)
        {
            if (enable)
            {
                NbOfflineService.SetInMemoryMode();
                NbOfflineService.EnableOfflineService(NbService.Singleton);
            }
            else
            {
                NbService.Singleton.DisableOffline();
            }
        }

        /**
         * LoginWithUsernameAsync, LoginWithEmailAsync
         **/

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableOnlineSameNotLoggedInUser()
        {
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            var after = GetLoginCach();
            AssertUser(user, after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
            Assert.AreNotEqual(0, afterSession.Expire);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オートモード（ネットワーク接続状態））
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableAutoSameNotLoggedInUser()
        {
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Auto).Result;
            var after = GetLoginCach();
            AssertUser(user, after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
            Assert.AreNotEqual(0, afterSession.Expire);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 同一ユーザ、未ログイン、オブジェクトキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableOnlineSameNotLoggedInObject()
        {
            CreateUserCache(false, false);
            Assert.IsNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            var after = GetLoginCach();
            AssertUser(user, after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
            Assert.AreNotEqual(0, afterSession.Expire);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 同一ユーザ、ログイン済み、ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableOnlineSameLoggedInUser()
        {
            CreateUserCache();
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            var after = GetLoginCach();
            AssertUser(user, after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
            Assert.AreNotEqual(0, afterSession.Expire);
        }

        /// <summary>
        /// Emailでログイン（オフライン機能有効、オンラインモード）
        /// 同一ユーザ、ログイン済み
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncNomarlEnableOnlineSameLoggedIn()
        {
            CreateUserCache(false);
            Assert.IsNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            var after = GetLoginCach();
            AssertUser(user, after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
            Assert.AreNotEqual(0, afterSession.Expire);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 同一ユーザ、パスワードがnull、未ログイン
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOnlineSameNoPasswrod()
        {
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, null).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 異なるユーザ、未ログイン、他ユーザキャッシュあり、オブジェクトキャッシュあり
        /// Exception（UnauthorizedAccessException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOnlineOtherNotLoggedInUserObject()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is UnauthorizedAccessException);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                Assert.AreNotEqual(after.UserName, NewUserName);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 異なるユーザ、未ログイン、他ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableOnlineOtherNotLoggedInUser()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password).Result;
            var after = GetLoginCach();
            Assert.IsNotNull(after);
            Assert.AreEqual(after.UserName, NewUserName);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 異なるユーザ、ログイン済み、他ユーザキャッシュあり、オブジェクトキャッシュあり
        /// Exception（UnauthorizedAccessException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOnlineOtherLoggedInUserObject()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache();
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is UnauthorizedAccessException);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                Assert.AreNotEqual(after.UserName, NewUserName);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNotNull(afterSession.CurrentUser);
                Assert.IsNotNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 異なるユーザ、ログイン済み、オブジェクトキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableOnlineOtherLoggedInObject()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache(false);
            Assert.IsNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.AreEqual(sessionInfo.CurrentUser.Username, UserName);
            Assert.IsNotNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password).Result;
            var after = GetLoginCach();
            Assert.IsNotNull(after);
            Assert.AreEqual(after.UserName, NewUserName);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.AreEqual(afterSession.CurrentUser.Username, NewUserName);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オンラインモード）
        /// 同一ユーザ、ユーザ名がnull、未ログイン
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOnlineSameNoUserName()
        {
            CreateUserCache(false, false);
            Assert.IsNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(null, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                var after = GetLoginCach();
                Assert.IsNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり、オブジェクトキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableOfflineSameNotLoggedInUserObject()
        {
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
            var after = GetLoginCach();
            Assert.IsNotNull(after);
            Assert.AreEqual(after.UserName, UserName);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.AreEqual(afterSession.CurrentUser.Username, UserName);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり（期限切れ）、オブジェクトキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineSameNotLoggedInUserObjectExpired()
        {
            CreateUserCache(true, false);
            var logincache = GetLoginCach();
            Assert.IsNotNull(logincache);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            ChangeLoginCacheExpire(logincache);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var after = GetLoginCach();
                Assert.IsNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlEnableOfflineSameNotLoggedInUser()
        {
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
            var after = GetLoginCach();
            Assert.IsNotNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
            Assert.AreEqual(afterSession.SessionToken, after.SessionToken);
            Assert.AreEqual(afterSession.Expire, after.SessionExpireAt);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、パスワード不正、未ログイン、ユーザキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineSameNotLoggedInUserInvalidPassword()
        {
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, "dummy", NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、未ログイン、オブジェクトキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineSameNotLoggedInObject()
        {
            CreateUserCache(false, false);
            Assert.IsNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var after = GetLoginCach();
                Assert.IsNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、未ログイン
        /// Exception（）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineSameNotLoggedIn()
        {
            CreateUserCache(false, false);
            Assert.IsNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var after = GetLoginCach();
                Assert.IsNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// Emailでログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、ログイン済み、ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncNomarlEnableOfflineSameLoggedInUser()
        {
            CreateUserCache();
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.AreEqual(sessionInfo.CurrentUser.Username, UserName);
            Assert.AreEqual(sessionInfo.CurrentUser.Email, Email);
            Assert.IsNotNull(sessionInfo.SessionToken);
            var user = NbOfflineUser.LoginWithEmailAsync(Email, Password, NbUser.LoginMode.Offline).Result;
            var after = GetLoginCach();
            Assert.IsNotNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、ユーザ名不正、ログイン済み、ユーザキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineSameLoggedInUserInvalidUserName()
        {
            CreateUserCache();
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync("dummy", Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNotNull(afterSession.CurrentUser);
                Assert.IsNotNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// Emailでログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、Email不正、ログイン済み、ユーザキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncExceptionEnableOfflineSameLoggedInUserInvalidEmail()
        {
            CreateUserCache();
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithEmailAsync("dummy", Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNotNull(afterSession.CurrentUser);
                Assert.IsNotNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// Emailでログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、Emailがnull、未ログイン
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncExceptionEnableOfflineSameNoEmail()
        {
            CreateUserCache(true, false);
            Assert.IsNotNull(GetLoginCach());
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithEmailAsync(null, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 異なるユーザ、未ログイン、他ユーザキャッシュあり、オブジェクトキャッシュあり
        /// Exception（UnauthorizedAccessException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineOtherNotLoggedInUserObject()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache(true, false);
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            Assert.AreEqual(login.UserName, UserName);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is UnauthorizedAccessException);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                Assert.AreEqual(after.UserName, UserName);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 異なるユーザ、未ログイン、他ユーザキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineOtherNotLoggedInUser()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache(true, false);
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            Assert.AreEqual(login.UserName, UserName);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                Assert.AreEqual(after.UserName, UserName);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能有効、オフラインモード）
        /// 異なるユーザ、ログイン済み、他ユーザキャッシュあり、オブジェクトキャッシュあり
        /// Exception（）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionEnableOfflineOtherLoggedInUserObject()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache();
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            Assert.AreEqual(login.UserName, UserName);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.AreEqual(sessionInfo.CurrentUser.Username, UserName);
            Assert.IsNotNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is UnauthorizedAccessException);
                var after = GetLoginCach();
                Assert.IsNotNull(after);
                Assert.AreEqual(after.UserName, UserName);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNotNull(afterSession.CurrentUser);
                Assert.IsNotNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// Emailでログイン（オフライン機能有効、オフラインモード）
        /// 同一ユーザ、パスワードがnull、未ログイン
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncExceptionEnableOfflineOtherNoPassword()
        {
            CreateUserCache(false, false);
            var login = GetLoginCach();
            Assert.IsNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                var user = NbOfflineUser.LoginWithEmailAsync(NewEmail, null, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                var after = GetLoginCach();
                Assert.IsNull(after);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オンラインモード）
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlDisableOnlineSameNotLoggedInUser()
        {
            CreateUserCache(true, false);
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            SwitchOfflineService(false);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            SwitchOfflineService(true);
            var after = GetLoginCach();
            Assert.IsNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オンラインモード）
        /// 同一ユーザ、未ログイン
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlDisableOnlineSameNotLoggedIn()
        {
            CreateUserCache(false, false);
            var login = GetLoginCach();
            Assert.IsNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            SwitchOfflineService(false);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            SwitchOfflineService(true);
            var after = GetLoginCach();
            Assert.IsNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オンラインモード）
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり、オブジェクトキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlDisableOnlineSameNotLoggedInUserObject()
        {
            CreateUserCache(true, false);
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            SwitchOfflineService(false);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            SwitchOfflineService(true);
            var after = GetLoginCach();
            Assert.IsNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オンラインモード）
        /// 同一ユーザ、未ログイン、オブジェクトキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlDisableOnlineSameNotLoggedInObject()
        {
            CreateUserCache(false, false);
            var login = GetLoginCach();
            Assert.IsNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            SwitchOfflineService(false);
            var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password).Result;
            SwitchOfflineService(true);
            var after = GetLoginCach();
            Assert.IsNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.IsNotNull(afterSession.SessionToken);

        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オンラインモード）
        /// 異なるユーザ、未ログイン、他ユーザキャッシュあり、オブジェクトキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlDisableOnlineOtherNotLoggedInUserObject()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache(true, false);
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            SwitchOfflineService(false);
            var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password).Result;
            SwitchOfflineService(true);
            var after = GetLoginCach();
            Assert.IsNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.AreEqual(afterSession.CurrentUser.Username, NewUserName);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オンラインモード）
        /// 異なるユーザ、ログイン済み、他ユーザキャッシュあり
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNomarlDisableOnlineOtherLoggedInUser()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache();
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            SwitchOfflineService(false);
            var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password).Result;
            SwitchOfflineService(true);
            var after = GetLoginCach();
            Assert.IsNull(after);
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(afterSession.CurrentUser);
            Assert.AreEqual(afterSession.CurrentUser.Username, NewUserName);
            Assert.IsNotNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オフラインモード）
        /// 同一ユーザ、未ログイン、ユーザキャッシュあり、オブジェクトキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionDisableOfflineSameNotLoggedInUserObject()
        {
            CreateUserCache(true, false);
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            SwitchOfflineService(false);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オフラインモード）
        /// 同一ユーザ、未ログイン、オブジェクトキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionDisableOfflineSameNotLoggedInObject()
        {
            CreateUserCache(false, false);
            var login = GetLoginCach();
            Assert.IsNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            CreateObjcectCache();
            SwitchOfflineService(false);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オフラインモード）
        /// 同一ユーザ、ログイン済み、ユーザキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionDisableOfflineSameLoggedInUser()
        {
            CreateUserCache();
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            SwitchOfflineService(false);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNotNull(afterSession.CurrentUser);
                Assert.IsNotNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オフラインモード）
        /// 同一ユーザ、ログイン済み
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionDisableOfflineSameLoggedIn()
        {
            CreateUserCache(false);
            var login = GetLoginCach();
            Assert.IsNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.IsNotNull(sessionInfo.SessionToken);
            SwitchOfflineService(false);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNotNull(afterSession.CurrentUser);
                Assert.IsNotNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オフラインモード）
        /// 異なるユーザ、未ログイン、他ユーザキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionDisableOfflineOtherNotLoggedInUser()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache(true, false);
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            SwitchOfflineService(false);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }

        /// <summary>
        /// ユーザ名でログイン（オフライン機能無効、オフラインモード）
        /// 異なるユーザ、ログイン済み、他ユーザキャッシュあり、オブジェクトキャッシュあり
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionDisableOfflineOtherLoggedInUserObject()
        {
            ITUtil.SignUpUser(NewUserName).Wait();
            CreateUserCache();
            var login = GetLoginCach();
            Assert.IsNotNull(login);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.AreEqual(sessionInfo.CurrentUser.Username, UserName);
            Assert.IsNotNull(sessionInfo.SessionToken);
            SwitchOfflineService(false);
            try
            {
                var user = NbOfflineUser.LoginWithUsernameAsync(NewUserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNotNull(afterSession.CurrentUser);
                Assert.AreEqual(afterSession.CurrentUser.Username, UserName);
                Assert.IsNotNull(afterSession.SessionToken);
            }
        }


        /**
         * LogoutAsync
         **/

        /// <summary>
        /// ログアウト（正常、オフラインモード、ログイン済み）
        /// ログアウトできること
        /// </summary>
        [Test]
        public void TestLogoutAsyncNormalOfflineMode()
        {
            CreateUserCache();
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.CurrentUser);
            Assert.AreEqual(sessionInfo.CurrentUser.Username, UserName);
            Assert.IsNotNull(sessionInfo.SessionToken);
            NbOfflineUser.LogoutAsync(NbUser.LoginMode.Offline).Wait();
            var afterSession = NbService.Singleton.SessionInfo;
            Assert.IsNull(afterSession.CurrentUser);
            Assert.IsNull(afterSession.SessionToken);
        }

        /// <summary>
        /// ログアウト（正常、オフラインモード、未ログイン）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public void TestLogoutAsyncExceptionOfflineModeNotLoggedIn()
        {
            CreateUserCache(true, false);
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNull(sessionInfo.CurrentUser);
            Assert.IsNull(sessionInfo.SessionToken);
            try
            {
                NbOfflineUser.LogoutAsync(NbUser.LoginMode.Offline).Wait();
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.IsNull(afterSession.CurrentUser);
                Assert.IsNull(afterSession.SessionToken);
            }
        }


        /**
         * IsLoggedIn
         **/

        /// <summary>
        /// ログイン状態確認（ログイン済み）
        /// trueが返却されること
        /// </summary>
        [Test]
        public void TestIsLoggedInNormalOfflineMode()
        {
            CreateUserCache();
            Assert.IsTrue(NbOfflineUser.IsLoggedIn());
        }

        /// <summary>
        /// ログイン状態確認（未ログイン）
        /// trueが返却されること
        /// </summary>
        [Test]
        public void TestIsLoggedInNormalOfflineModeNotLoggedIn()
        {
            CreateUserCache(true, false);
            Assert.IsFalse(NbOfflineUser.IsLoggedIn());
        }


        /**
         * プロパティ
         **/

        /// <summary>
        /// ユーザIDの設定・取得
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestSetGetUserIdNormal()
        {
            var user = new NbUser();
            user.UserId = "12345";
            Assert.AreEqual(user.UserId, "12345");
        }

        /// <summary>
        /// ユーザIDの設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetUserIdNormalInit()
        {
            var user = new NbUser();
            Assert.IsNull(user.UserId);
        }

        /// <summary>
        /// ユーザIDの設定・取得
        /// nullの設定、取得ができること
        /// </summary>
        [Test]
        public void TestSetGetUserIdSubnormalNull()
        {
            var user = new NbUser();
            user.UserId = "12345";
            Assert.AreEqual(user.UserId, "12345");
            user.UserId = null;
            Assert.IsNull(user.UserId);
        }

        /// <summary>
        /// ユーザ名の設定・取得
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestSetGetUserNameNormal()
        {
            var user = new NbUser();
            user.Username = UserName;
            Assert.AreEqual(user.Username, UserName);
        }

        /// <summary>
        /// ユーザ名の設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetUserNameNormalInit()
        {
            var user = new NbUser();
            Assert.IsNull(user.Username);
        }

        /// <summary>
        /// ユーザ名の設定・取得
        /// nullの設定、取得ができること
        /// </summary>
        [Test]
        public void TestSetGetUserNameSubnormalNull()
        {
            var user = new NbUser();
            user.Username = UserName;
            Assert.AreEqual(user.Username, UserName);
            user.Username = null;
            Assert.IsNull(user.Username);
        }

        /// <summary>
        /// Emailの設定・取得
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestSetGetEmailNormal()
        {
            var user = new NbUser();
            user.Email = Email;
            Assert.AreEqual(user.Email, Email);
        }

        /// <summary>
        /// Emailの設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetEmailNormalInit()
        {
            var user = new NbUser();
            user.Email = Email;
            Assert.AreEqual(user.Email, Email);
        }

        /// <summary>
        /// Emailの設定・取得
        /// nullの設定、取得ができること
        /// </summary>
        [Test]
        public void TestSetGetEmailSubnormalNull()
        {
            var user = new NbUser();
            user.Email = Email;
            Assert.AreEqual(user.Email, Email);
            user.Email = null;
            Assert.IsNull(user.Email);
        }

        /// <summary>
        /// オプション情報の設定・取得
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestSetGetOptionsNormal()
        {
            var user = new NbUser();
            user.Options = Options;
            Assert.AreEqual(user.Options, Options);
            user.Options = null;
            Assert.IsNull(user.Options);
        }

        /// <summary>
        /// オプション情報の設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetOptionsNormalInit()
        {
            var user = new NbUser();
            Assert.IsNull(user.Options);
        }

        /// <summary>
        /// オプション情報の設定・取得
        /// nullの設定、取得ができること
        /// </summary>
        [Test]
        public void TestSetGetOptionsSubnormalNull()
        {
            var user = new NbUser();
            user.Options = Options;
            Assert.AreEqual(user.Options, Options);
            user.Options = null;
            Assert.IsNull(user.Options);
        }

        /// <summary>
        /// 所属グループ一覧の設定・取得
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupsNormal()
        {
            var user = new NbUser();
            user.Groups = new List<string>();
            user.Groups.Add("testGroup");
            Assert.IsNotEmpty(user.Groups);
            user.Groups = null;
            Assert.IsNull(user.Groups);
        }

        /// <summary>
        /// 所属グループ一覧の設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupsNormalInit()
        {
            var user = new NbUser();
            Assert.IsNull(user.Groups);
        }

        /// <summary>
        /// 所属グループ一覧の設定・取得
        /// nullの設定、取得ができること
        /// </summary>
        [Test]
        public void TestSetGetGroupsSubnormalNull()
        {
            var user = new NbUser();
            user.Groups = new List<string>();
            user.Groups.Add("testGroup");
            Assert.IsNotEmpty(user.Groups);
            user.Groups = null;
            Assert.IsNull(user.Groups);
        }

        /// <summary>
        /// 作成日時の設定・取得
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestSetGetCreatedTimeNormal()
        {
            var user = new NbUser();
            user.CreatedAt = "12345";
            Assert.AreEqual(user.CreatedAt, "12345");
        }

        /// <summary>
        /// 作成日時の設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetCreatedTimeNormalInit()
        {
            var user = new NbUser();
            Assert.IsNull(user.CreatedAt);
        }

        /// <summary>
        /// 作成日時の設定・取得
        /// nullの設定、取得ができること
        /// </summary>
        [Test]
        public void TestSetGetCreatedTimeSubnormalNull()
        {
            var user = new NbUser();
            user.CreatedAt = "12345";
            Assert.AreEqual(user.CreatedAt, "12345");
            user.CreatedAt = null;
            Assert.IsNull(user.CreatedAt);
        }

        /// <summary>
        /// 更新日時の設定・取得
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestSetGetUpdatedTimeNormal()
        {
            var user = new NbUser();
            user.UpdatedAt = "12345";
            Assert.AreEqual(user.UpdatedAt, "12345");
            user.UpdatedAt = null;
            Assert.IsNull(user.UpdatedAt);
        }

        /// <summary>
        /// 更新日時の設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetUpdatedTimeNormalInit()
        {
            var user = new NbUser();
            Assert.IsNull(user.UpdatedAt);
        }

        /// <summary>
        /// 更新日時の設定・取得
        /// nullの設定、取得ができること
        /// </summary>
        [Test]
        public void TestSetGetUpdatedTimeSubnormalNull()
        {
            var user = new NbUser();
            user.UpdatedAt = "12345";
            Assert.AreEqual(user.UpdatedAt, "12345");
            user.UpdatedAt = null;
            Assert.IsNull(user.UpdatedAt);
        }


        /**
         * 繰り返し評価
         **/

        private const int RepeatCount = 3;

        /// <summary>
        /// ユーザのログイン、ログアウトを3回繰り返す
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestLoginLogoutRepeatNormal()
        {
            CreateUserCache(true, false);
            for (int i = 0; i < RepeatCount; i++)
            {
                var result = NbOfflineUser.LoginWithUsernameAsync(UserName, Password, NbUser.LoginMode.Offline).Result;
                Assert.IsTrue(NbOfflineUser.IsLoggedIn());
                NbOfflineUser.LogoutAsync(NbUser.LoginMode.Offline).Wait();
                Assert.IsFalse(NbOfflineUser.IsLoggedIn());
            }

            for (int i = 0; i < RepeatCount; i++)
            {
                var result = NbOfflineUser.LoginWithEmailAsync(Email, Password, NbUser.LoginMode.Offline).Result;
                Assert.IsTrue(NbOfflineUser.IsLoggedIn());
                NbOfflineUser.LogoutAsync(NbUser.LoginMode.Offline).Wait();
                Assert.IsFalse(NbOfflineUser.IsLoggedIn());
            }
        }


        /**
         * Util for OfflineUserIT
         **/

        private void CreateUserCache(bool isCache = true, bool isLoggedin = true)
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            if (isCache)
            {
                var user = NbOfflineUser.LoginWithEmailAsync(Email, Password).Result;
                if (!isLoggedin)
                    NbOfflineUser.LogoutAsync().Wait();
            }
            else
            {
                var user = NbUser.LoginWithEmailAsync(Email, Password).Result;
                if (!isLoggedin)
                    NbUser.LogoutAsync().Wait();
            }
        }

        private void CreateObjcectCache()
        {
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(ObjectBucetName);
            var obj = bucket.NewObject();
            var result = obj.SaveAsync().Result;
        }

        private LoginCache GetLoginCach()
        {
            if (!NbService.Singleton.IsOfflineEnabled()) return null;
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                var logincaches = dao.FindAll();
                if (logincaches.Count() == 0) return null;
                else return logincaches.ToList()[0];
            }
        }

        private void AssertUser(NbUser user, LoginCache cache)
        {
            Assert.AreEqual(user.UserId, cache.UserId);
            Assert.AreEqual(user.Username, cache.UserName);
            Assert.AreEqual(user.Email, cache.Email);
            Assert.AreEqual(user.Options, cache.Options);
            Assert.AreEqual(user.CreatedAt, cache.CreatedAt);
            Assert.AreEqual(user.UpdatedAt, cache.UpdatedAt);
        }

        private void ChangeLoginCacheExpire(LoginCache cache, NbSessionInfo session = null)
        {
            if (!NbService.Singleton.IsOfflineEnabled()) return;

            NbUser user = null;
            NbSessionInfo sessionInfo = null;
            if (session == null)
            {
                user = new NbUser();
                user.UserId = cache.UserId;
                user.Username = cache.UserName;
                user.Email = cache.Email;
                user.Options = cache.Options;
                user.Groups = cache.Groups;
                user.CreatedAt = cache.CreatedAt;
                user.UpdatedAt = cache.UpdatedAt;
                sessionInfo = new NbSessionInfo();
                sessionInfo.Set(cache.SessionToken, cache.SessionExpireAt, user);
            }
            else
            {
                user = session.CurrentUser;
                sessionInfo = session;
            }

            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                dao.RemoveAll();
                var login = new LoginCache();
                login.SetUser(user);
                sessionInfo.Set(sessionInfo.SessionToken, 1L, user);
                login.SetSession(sessionInfo);
                login.Password = Password;

                dao.Add(login);
                dao.SaveChanges();
            }
        }
    }
}
