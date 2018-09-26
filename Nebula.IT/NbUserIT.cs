using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class NbUserIT
    {
        private const string TestUsername = "user1";
        private const string TestEmail = "user1@example.com";
        private const string TestPassword = "Passw0rD";

        private const string UserName = "userTest";
        private const string Email = "userTest@example.com";
        private const string Password = ITUtil.Password;
        private const string NewUserName = "userTest2";
        private const string NewEmail = "userTest2@example.com";
        private const string NewPassword = Password + "2";
        private NbJsonObject Options = new NbJsonObject { { "option1", "testOption1" }, { "option2", "testOption2" } };
        private NbJsonObject NewOptions = new NbJsonObject { { "option101", "testOption101" }, { "option202", "testOption202" } };

        private const string GroupName = "GroupName";

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineGroup().Wait();
            ITUtil.InitOnlineUser().Wait();
        }

        [TearDown]
        public void TearDown()
        {
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            ITUtil.InitOnlineGroup().Wait();
            ITUtil.InitOnlineUser().Wait();
        }

        private NbUser SignUp()
        {
            var user = new NbUser
            {
                Username = TestUsername,
                Email = TestEmail,
                Options = new NbJsonObject()
                {
                    {"realname", "User 1"}
                }
            };
            return user.SignUpAsync(TestPassword).Result;
        }

        [Test]
        public void TestSignUp()
        {
            var user = SignUp();
            Assert.IsNotNull(user.UserId);
        }

        [Test]
        public void TestLoginLogout()
        {
            var user = SignUp();

            Assert.False(NbUser.IsLoggedIn());

            for (int i = 0; i < 2; i++)
            {
                NbUser luser;
                if (i == 0)
                {
                    // username でログイン
                    luser = NbUser.LoginWithUsernameAsync(TestUsername, TestPassword).Result;
                }
                else
                {
                    // Email でログイン
                    luser = NbUser.LoginWithEmailAsync(TestEmail, TestPassword).Result;
                }
                if (user.UserId != luser.UserId)
                {
                    Debug.Print("???");
                }
                Assert.AreEqual(user.UserId, luser.UserId);
                Assert.AreSame(luser, NbUser.CurrentUser());
                Assert.True(NbUser.IsLoggedIn());

                // ログアウト
                NbUser.LogoutAsync().Wait();
                Assert.IsNull(NbUser.CurrentUser());
                Assert.False(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ユーザ取得テスト
        /// </summary>
        [Test]
        public void TestGetUser()
        {
            TestSignUp();

            // ログインしておく
            var user = NbUser.LoginWithUsernameAsync(TestUsername, TestPassword).Result;

            var luser = NbUser.GetUserAsync(user.UserId).Result;
            Assert.AreEqual(user.UserId, luser.UserId);
        }

        /// <summary>
        /// RefreshCurrentUserテスト
        /// </summary>
        [Test]
        public void TestRefreshCurrentUser()
        {
            TestSignUp();

            // 未ログイン状態でテスト
            try
            {
                NbUser.RefreshCurrentUserAsync().Wait();
                Assert.Fail("No expected exception");
            }
            catch (AggregateException e)
            {
                if (!(e.InnerException is InvalidOperationException))
                {
                    Assert.Fail("No expected exception");
                }
            }

            // ログインしておく
            var user = NbUser.LoginWithUsernameAsync(TestUsername, TestPassword).Result;

            var luser = NbUser.RefreshCurrentUserAsync().Result;
            Assert.AreEqual(user.UserId, luser.UserId);
        }

        /// <summary>
        /// ユーザ更新テスト
        /// </summary>
        [Test]
        public void TestUpdateUser()
        {
            TestSignUp();

            // ログインしておく
            var user = NbUser.LoginWithUsernameAsync(TestUsername, TestPassword).Result;

            // ユーザ情報変更
            user.Username = "user2";
            user.Email = "user2@example.com";
            user.Options = new NbJsonObject()
            {
                {"realname", "User 2"}
            };

            // パスワード以外変更
            user = user.SaveAsync().Result;
            Assert.AreEqual("user2", user.Username);
            Assert.AreEqual("user2@example.com", user.Email);
            Assert.AreEqual("User 2", user.Options["realname"]);

            // 変更後の情報でログインできることを確認
            user = NbUser.LoginWithUsernameAsync("user2", TestPassword).Result;

            // パスワード変更
            user = user.SaveAsync("password2").Result;
            Assert.IsNotNull(user);

            // 新パスワードでログインできることを確認
            user = NbUser.LoginWithUsernameAsync("user2", "password2").Result;
            Assert.IsNotNull(user);
        }

        /// <summary>
        /// パスワードリセットテスト。
        /// メールが送信されることは確認しない。REST API 応答のみチェック。
        /// </summary>
        [Test]
        public void TestResetPassword()
        {
            TestSignUp();

            try
            {
                NbUser.ResetPasswordWithUsernameAsync(TestUsername).Wait();
                NbUser.ResetPasswordWithEmailAsync(TestEmail).Wait();

                //NbUser.ResetPasswordWithUsernameAsync("not_exist_user").Wait();
            }
            catch (AggregateException e)
            {
                NbHttpException ne = e.InnerException as NbHttpException;
                if (ne.StatusCode == HttpStatusCode.Forbidden)
                {
                    // パスワードリセットを連続実行すると Forbidden になるため、テストエラーにはしない。
                }
                else
                {
                    throw;
                }
            }
        }


        /**
         * SignUpAsync
         **/

        /// <summary>
        /// サインアップ（正常）
        /// サインアップできること
        /// </summary>
        [Test]
        public void TestSignUpAsyncNormalAll()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = Options;
            var result = user.SignUpAsync(Password).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.UserId);
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, UserName);
            Assert.AreEqual(result.Options, Options);
            Assert.IsNotNull(result.CreatedAt);
            Assert.IsNotNull(result.UpdatedAt);
        }

        /// <summary>
        /// サインアップ（ユーザ名省略）
        /// サインアップできること
        /// </summary>
        [Test]
        public void TestSignUpAsyncNormalNoUsername()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Options = Options;
            var result = user.SignUpAsync(Password).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.UserId);
            Assert.AreEqual(result.Email, Email);
            Assert.IsNotNull(result.Username, UserName);
            Assert.AreEqual(result.Options, Options);
            Assert.IsNotNull(result.CreatedAt);
            Assert.IsNotNull(result.UpdatedAt);
        }

        /// <summary>
        /// サインアップ（オプション情報省略）
        /// サインアップできること
        /// </summary>
        [Test]
        public void TestSignUpAsyncNormalNoOptions()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            var result = user.SignUpAsync(Password).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.UserId);
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, UserName);
            Assert.IsEmpty(result.Options);
            Assert.IsNotNull(result.CreatedAt);
            Assert.IsNotNull(result.UpdatedAt);
        }

        /// <summary>
        /// サインアップ（パラメータ省略）
        /// サインアップできること
        /// </summary>
        [Test]
        public void TestSignUpAsyncNormalNoUsernameAndNoOptions()
        {
            var user = new NbUser();
            user.Email = Email;
            var result = user.SignUpAsync(Password).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.UserId);
            Assert.AreEqual(result.Email, Email);
            Assert.IsNotNull(result.Username, UserName);
            Assert.IsEmpty(result.Options);
            Assert.IsNotNull(result.CreatedAt);
            Assert.IsNotNull(result.UpdatedAt);
        }

        /// <summary>
        /// サインアップ（ユーザ名に@を含む）
        /// サインアップできること
        /// </summary>
        [Test]
        public void TestSignUpAsyncNormalUsernameContainsAtSymbol()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Username = "aaa@bbb";
            user.Options = Options;
            var result = user.SignUpAsync(Password).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.UserId);
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, "aaa@bbb");
            Assert.AreEqual(result.Options, Options);
            Assert.IsNotNull(result.CreatedAt);
            Assert.IsNotNull(result.UpdatedAt);
        }

        /// <summary>
        /// サインアップ（オプション情報が空）
        /// サインアップできること
        /// </summary>
        [Test]
        public void TestSignUpAsyncNormalOptionsEmpty()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = new NbJsonObject();
            var result = user.SignUpAsync(Password).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.UserId);
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, UserName);
            Assert.AreEqual(result.Options, new NbJsonObject());
            Assert.IsNotNull(result.CreatedAt);
            Assert.IsNotNull(result.UpdatedAt);
        }

        /// <summary>
        /// サインアップ（ACL権限なし、マスターキー使用）
        /// サインアップできること
        /// </summary>
        [Test]
        public void TestSignUpAsyncNormalNoACLWithMasterKey()
        {
            ITUtil.UseMasterKey();
            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.C = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = Options;
            var result = user.SignUpAsync(Password).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.UserId);
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, UserName);
            Assert.AreEqual(result.Options, Options);
            Assert.IsNotNull(result.CreatedAt);
            Assert.IsNotNull(result.UpdatedAt);

            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// サインアップ（ACL権限なし）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionNoACL()
        {
            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.C = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = Options;

            try
            {
                var result = user.SignUpAsync(Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                ITUtil.UseMasterKey();
                Assert.IsEmpty(QueryUsers());
                ITUtil.UseNormalKey();
            }
        }

        /// <summary>
        /// サインアップ（ユーザ衝突）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionConflictUsername()
        {
            ITUtil.SignUpUser().Wait();
            var user = new NbUser();
            user.Email = "foo@example.com";
            try
            {
                var result = user.SignUpAsync(Password).Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                ITUtil.UseMasterKey();
                Assert.AreEqual(1, QueryUsers().Count());
                ITUtil.UseNormalKey();
            }
        }

        /// <summary>
        /// サインアップ（テナントID不正）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionInvalidTenantId()
        {
            NbService.Singleton.TenantId = "dummy";

            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = Options;

            try
            {
                var result = user.SignUpAsync(Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                ITUtil.InitNebula();
                ITUtil.UseMasterKey();
                Assert.IsEmpty(QueryUsers());
                ITUtil.UseNormalKey();
            }
        }

        /// <summary>
        /// サインアップ（アプリケーションID不正）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionInvalidAppId()
        {
            NbService.Singleton.AppId = "dummy";

            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = Options;

            try
            {
                var result = user.SignUpAsync(Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                ITUtil.InitNebula();
                ITUtil.UseMasterKey();
                Assert.IsEmpty(QueryUsers());
                ITUtil.UseNormalKey();
            }
        }

        /// <summary>
        /// サインアップ（Emailがnull）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionNoEmail()
        {
            var user = new NbUser();
            user.Username = UserName;
            user.Options = Options;

            try
            {
                var result = user.SignUpAsync(Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
                ITUtil.UseMasterKey();
                Assert.IsEmpty(QueryUsers());
                ITUtil.UseNormalKey();
            }
        }

        /// <summary>
        /// サインアップ（パスワードがnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionNoPassword()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = Options;

            try
            {
                var result = user.SignUpAsync(null).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                ITUtil.UseMasterKey();
                Assert.IsEmpty(QueryUsers());
                ITUtil.UseNormalKey();
            }
        }

        /// <summary>
        /// サインアップ（ユーザ名に2byte文字使用）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionInvalidUsername2Byte()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Username = "あdummy";
            user.Options = Options;

            try
            {
                var result = user.SignUpAsync(Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.BadRequest);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                ITUtil.UseMasterKey();
                Assert.IsEmpty(QueryUsers());
                ITUtil.UseNormalKey();
            }
        }

        /// <summary>
        /// サインアップ（パスワードが7文字）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSignUpAsyncExceptionInvalidPassword()
        {
            var user = new NbUser();
            user.Email = Email;
            user.Username = UserName;
            user.Options = Options;

            try
            {
                var result = user.SignUpAsync("passwor").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.BadRequest);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                ITUtil.UseMasterKey();
                Assert.IsEmpty(QueryUsers());
                ITUtil.UseNormalKey();
            }
        }

        /**
         * LoginWithUsernameAsync
         **/

        /// <summary>
        /// ユーザ名でログイン（正常）
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNormalUsername()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());
            var user = NbUser.LoginWithUsernameAsync(UserName, Password).Result;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.UserId);
            Assert.AreEqual(user.Email, Email);
            Assert.AreEqual(user.Username, UserName);
            Assert.AreEqual(user.Options, Options);
            Assert.IsNotNull(user.CreatedAt);
            Assert.IsNotNull(user.UpdatedAt);

            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreNotEqual(0, expire);
        }

        /// <summary>
        /// ユーザ名でログイン（ユーザ名に@を含む）
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNormalUsernameContainsAtSymbol()
        {
            ITUtil.SignUpUser("aaa@bbb", Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());
            var user = NbUser.LoginWithUsernameAsync("aaa@bbb", Password).Result;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.UserId);
            Assert.AreEqual(user.Email, Email);
            Assert.AreEqual(user.Username, "aaa@bbb");
            Assert.AreEqual(user.Options, Options);
            Assert.IsNotNull(user.CreatedAt);
            Assert.IsNotNull(user.UpdatedAt);

            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreNotEqual(0, expire);
        }

        /// <summary>
        /// ユーザ名でログイン（ログイン済み）
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNormalDuplicateLogIn()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());
            ITUtil.LoginUser(Email).Wait();
            Assert.IsTrue(NbUser.IsLoggedIn());

            var user = NbUser.LoginWithUsernameAsync(UserName, Password).Result;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.UserId);
            Assert.AreEqual(user.Email, Email);
            Assert.AreEqual(user.Username, UserName);
            Assert.AreEqual(user.Options, Options);
            Assert.IsNotNull(user.CreatedAt);
            Assert.IsNotNull(user.UpdatedAt);

            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreNotEqual(0, expire);
        }

        /// <summary>
        /// ユーザ名でログイン（ACL権限なし、マスターキー使用）
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncNormalNoACLWithMasterKey()
        {
            ITUtil.UseMasterKey();
            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.C = new HashSet<string>();
            contentAcl.R = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());

            var user = NbUser.LoginWithUsernameAsync(UserName, Password).Result;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.UserId);
            Assert.AreEqual(user.Email, Email);
            Assert.AreEqual(user.Username, UserName);
            Assert.AreEqual(user.Options, Options);
            Assert.IsNotNull(user.CreatedAt);
            Assert.IsNotNull(user.UpdatedAt);

            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreNotEqual(0, expire);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ名でログイン（アプリケーションキー不正）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionInvalidAppKey()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());

            NbService.Singleton.AppKey = "dummy";

            try
            {
                var user = NbUser.LoginWithUsernameAsync(UserName, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                ITUtil.InitNebula();
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ユーザ名でログイン（パスワードがnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionNoPassword()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            try
            {
                var user = NbUser.LoginWithUsernameAsync(UserName, null).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ユーザ名でログイン（ユーザ名がnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionNoUsername()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            try
            {
                var user = NbUser.LoginWithUsernameAsync(null, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ユーザ名でログイン（未登録ユーザ）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionUnexistedUser()
        {
            try
            {
                var user = NbUser.LoginWithUsernameAsync(UserName, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ユーザ名でログイン（ユーザ名とパスワードが不一致）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithUsernameAsyncExceptionUsernameWithIncorrectPassword()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            try
            {
                var user = NbUser.LoginWithUsernameAsync(UserName, "dummy").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ユーザ名でログイン（成功時ログイン情報確認）
        /// ユーザ情報が保存されていること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterLogInWithUserNameNormal()
        {
            Assert.IsNull(NbUser.CurrentUser());
            TestLoginWithUsernameAsyncNormalUsername();
            var currentUser = NbUser.CurrentUser();
            Assert.AreEqual(currentUser.Email, Email);
            Assert.AreEqual(currentUser.Username, UserName);
            Assert.AreEqual(currentUser.Options, Options);
        }

        /// <summary>
        /// ユーザ名でログイン（失敗時ログイン情報確認）
        /// ユーザ情報が保存されていないこと
        /// </summary>
        [Test]
        public void TestCurrentUserAfterLogInWithUserNameSubnormalLogInFailed()
        {
            Assert.IsNull(NbUser.CurrentUser());
            TestLoginWithUsernameAsyncExceptionUsernameWithIncorrectPassword();
            Assert.IsNull(NbUser.CurrentUser());
        }

        /**
         * LoginWithEmailAsync
         **/

        /// <summary>
        /// Emailでログイン（正常）
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncNormalEmail()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());
            var user = NbUser.LoginWithEmailAsync(Email, Password).Result;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.UserId);
            Assert.AreEqual(user.Email, Email);
            Assert.AreEqual(user.Username, UserName);
            Assert.AreEqual(user.Options, Options);
            Assert.IsNotNull(user.CreatedAt);
            Assert.IsNotNull(user.UpdatedAt);

            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreNotEqual(0, expire);
        }

        /// <summary>
        /// Emailでログイン（ログイン済み）
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncNormalDuplicateLogIn()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());
            ITUtil.LoginUser(Email).Wait();
            Assert.IsTrue(NbUser.IsLoggedIn());

            var user = NbUser.LoginWithEmailAsync(Email, Password).Result;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.UserId);
            Assert.AreEqual(user.Email, Email);
            Assert.AreEqual(user.Username, UserName);
            Assert.AreEqual(user.Options, Options);
            Assert.IsNotNull(user.CreatedAt);
            Assert.IsNotNull(user.UpdatedAt);

            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreNotEqual(0, expire);
        }

        /// <summary>
        /// Emailでログイン（ACL権限なし）
        /// ログインできること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncNormalNoACL()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            Assert.IsFalse(NbUser.IsLoggedIn());

            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.C = new HashSet<string>();
            contentAcl.R = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            var user = NbUser.LoginWithEmailAsync(Email, Password).Result;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.UserId);
            Assert.AreEqual(user.Email, Email);
            Assert.AreEqual(user.Username, UserName);
            Assert.AreEqual(user.Options, Options);
            Assert.IsNotNull(user.CreatedAt);
            Assert.IsNotNull(user.UpdatedAt);

            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreNotEqual(0, expire);
        }

        /// <summary>
        /// Emailでログイン（パスワードがnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncExceptionNoPassword()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            try
            {
                var user = NbUser.LoginWithEmailAsync(Email, null).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// Emailでログイン（Emailがnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncExceptionNoEmail()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            try
            {
                var user = NbUser.LoginWithEmailAsync(null, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// Emailでログイン（未登録ユーザ）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncExceptionUnexistedUser()
        {
            try
            {
                var user = NbUser.LoginWithEmailAsync(Email, Password).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// Emailでログイン（Emailとパスワードが不一致）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLoginWithEmailAsyncExceptionEmailWithIncorrectPassword()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            try
            {
                var user = NbUser.LoginWithEmailAsync(Email, "dummy").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// Emailでログイン（成功時ログイン情報確認）
        /// ユーザ情報が保存されていること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterLogInWithEmailNormal()
        {
            Assert.IsNull(NbUser.CurrentUser());
            TestLoginWithEmailAsyncNormalEmail();
            var currentUser = NbUser.CurrentUser();
            Assert.AreEqual(currentUser.Email, Email);
            Assert.AreEqual(currentUser.Username, UserName);
            Assert.AreEqual(currentUser.Options, Options);
        }

        /// <summary>
        /// Emailでログイン（失敗時ログイン情報確認）
        /// ユーザ情報が保存されていないこと
        /// </summary>
        [Test]
        public void TestCurrentUserAfterLogInWithEmailSubnormalLogInFailed()
        {
            Assert.IsNull(NbUser.CurrentUser());
            TestLoginWithEmailAsyncExceptionEmailWithIncorrectPassword();
            Assert.IsNull(NbUser.CurrentUser());
        }


        /**
         * LogoutAsync
         **/

        /// <summary>
        /// ログアウト（ログイン済み）
        /// ログアウトできること
        /// </summary>
        [Test]
        public void TestLogoutAsyncNormal()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            NbUser.LogoutAsync().Wait();
            Assert.IsNull(NbUser.CurrentUser());
        }

        /// <summary>
        /// ログアウト（ログイン済み、オートモード（オンライン））
        /// ログアウトできること
        /// </summary>
        [Test]
        public void TestLogoutAsyncNormalAuto()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            NbUser.LogoutAsync(NbUser.LoginMode.Auto).Wait();
            Assert.IsNull(NbUser.CurrentUser());
        }

        /// <summary>
        /// ログアウト（未ログイン）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public void TestLogoutAsyncExceptionNotLoggedIn()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            try
            {
                NbUser.LogoutAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
                Assert.IsNull(NbUser.CurrentUser());
            }
        }

        /// <summary>
        /// ログアウト（テナントID不正）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestLogoutAsyncExceptionInvalidTenantId()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());

            NbService.Singleton.TenantId = "dummy";

            try
            {
                NbUser.LogoutAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                Assert.IsNull(NbUser.CurrentUser());
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ログアウト（セッショントークン不正）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestLogoutAsyncExceptionInvalidSessionToken()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());

            ITUtil.LogoutDirect();

            try
            {
                NbUser.LogoutAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                Assert.IsNull(NbUser.CurrentUser());
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ログアウト（成功時ログイン情報確認）
        /// ユーザ情報、セッション情報が破棄されていること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterLogOutNormal()
        {
            TestLogoutAsyncNormal();
            Assert.IsNull(NbUser.CurrentUser());
            Assert.IsNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreEqual(0, expire);
            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// ログアウト（失敗時ログイン情報確認）
        /// ユーザ情報、セッション情報が破棄されていること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterLogOutSubnormalLogOutFailed()
        {
            TestLogoutAsyncExceptionNotLoggedIn();
            Assert.IsNull(NbUser.CurrentUser());
            Assert.IsNull(NbService.Singleton.SessionInfo.SessionToken);
            var expire = NbService.Singleton.SessionInfo.Expire;
            Assert.AreEqual(0, expire);
            Assert.IsFalse(NbUser.IsLoggedIn());
        }


        /**
         * SaveAsync
         **/

        /// <summary>
        /// ユーザ更新（正常）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalAll()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            user.Email = NewEmail;
            user.Username = NewUserName;
            user.Options = NewOptions;
            var result = user.SaveAsync(NewPassword).Result;
            Assert.AreEqual(result.Email, NewEmail);
            Assert.AreEqual(result.Username, NewUserName);
            Assert.AreEqual(result.Options, NewOptions);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, user.UpdatedAt);

            var newUser = NbUser.CurrentUser();
            Assert.AreEqual(result.Email, newUser.Email);
            Assert.AreEqual(result.Username, newUser.Username);
            Assert.AreEqual(result.Options, newUser.Options);
            Assert.AreEqual(result.CreatedAt, newUser.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, newUser.UpdatedAt);

            ITUtil.LoginUser(NewEmail, NewPassword).Wait();
            ITUtil.LoginUserWithUser(NewUserName, NewPassword).Wait();
        }

        /// <summary>
        /// ユーザ更新（ユーザ名更新）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalUsername()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            user.Username = NewUserName;
            var result = user.SaveAsync(null).Result;
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, NewUserName);
            Assert.AreEqual(result.Options, Options);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, user.UpdatedAt);

            ITUtil.LoginUserWithUser(NewUserName).Wait();
        }

        /// <summary>
        /// ユーザ更新（Email更新）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalEmail()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            user.Email = NewEmail;
            var result = user.SaveAsync(null).Result;
            Assert.AreEqual(result.Email, NewEmail);
            Assert.AreEqual(result.Username, UserName);
            Assert.AreEqual(result.Options, Options);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, user.UpdatedAt);

            ITUtil.LoginUser(NewEmail).Wait();
        }

        /// <summary>
        /// ユーザ更新（オプション情報更新）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalOptions()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            user.Options = NewOptions;
            var result = user.SaveAsync(null).Result;
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, UserName);
            Assert.AreEqual(result.Options, NewOptions);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, user.UpdatedAt);

            ITUtil.LoginUser(Email).Wait();
            ITUtil.LoginUserWithUser(UserName).Wait();
        }

        /// <summary>
        /// ユーザ更新（オプション情報が空）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalOptionsEmpty()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            user.Email = NewEmail;
            user.Username = NewUserName;
            user.Options = new NbJsonObject();
            var result = user.SaveAsync(NewPassword).Result;
            Assert.AreEqual(result.Email, NewEmail);
            Assert.AreEqual(result.Username, NewUserName);
            Assert.AreEqual(result.Options, new NbJsonObject());
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, user.UpdatedAt);

            ITUtil.LoginUser(NewEmail, NewPassword).Wait();
            ITUtil.LoginUserWithUser(NewUserName, NewPassword).Wait();
        }

        /// <summary>
        /// ユーザ更新（パスワード更新）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalPassword()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            var result = user.SaveAsync(NewPassword).Result;
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, UserName);
            Assert.AreEqual(result.Options, Options);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, user.UpdatedAt);

            ITUtil.LoginUser(Email, NewPassword).Wait();
            ITUtil.LoginUserWithUser(UserName, NewPassword).Wait();
        }

        /// <summary>
        /// ユーザ更新（ユーザ名に@を含む）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalUsernameContainsAtSymbol()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            user.Username = "aaa@bbb";
            var result = user.SaveAsync(null).Result;
            Assert.AreEqual(result.Email, Email);
            Assert.AreEqual(result.Username, "aaa@bbb");
            Assert.AreEqual(result.Options, Options);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, user.UpdatedAt);

            ITUtil.LoginUserWithUser("aaa@bbb").Wait();
        }

        /// <summary>
        /// ユーザ更新（他ユーザ更新、未ログイン、マスターキー使用）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncOtherNormalAll()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            var user = NbUser.CurrentUser();
            var tempUser = ITUtil.CloneUser(user);
            ITUtil.SignUpUser("otherUser", "other@example.com").Wait();
            ITUtil.LogoutUser().Wait();

            ITUtil.UseMasterKey();
            user.Username = NewUserName;
            user.Email = NewEmail;
            user.Options = NewOptions;
            var result = user.SaveAsync(NewPassword).Result;
            Assert.AreEqual(result.UserId, tempUser.UserId);
            Assert.AreEqual(result.Email, NewEmail);
            Assert.AreEqual(result.Username, NewUserName);
            Assert.AreEqual(result.Options, NewOptions);
            Assert.AreEqual(result.CreatedAt, tempUser.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, tempUser.UpdatedAt);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ更新（未認証ユーザ）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfExceptionNotLoggedIn()
        {
            var user = new NbUser();
            user.Username = UserName;
            user.Email = Email;
            var temp = user.SignUpAsync(Password).Result;

            try
            {
                temp.Username = NewUserName;
                temp.Email = NewEmail;
                temp.Options = NewOptions;
                var result = temp.SaveAsync(NewPassword).Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ更新（他ユーザ更新、マスターキー不使用）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncOtherExceptionNoMasterKey()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            var other = new NbUser();
            other.Username = "otherUser";
            other.Email = "other@example.com";
            var user = other.SignUpAsync(Password).Result;
            var tempUser = ITUtil.CloneUser(user);

            user.Username = NewUserName;
            user.Email = NewEmail;
            user.Options = NewOptions;
            try
            {
                var result = user.SaveAsync(NewPassword).Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                Assert.AreEqual(user.UserId, tempUser.UserId);
                Assert.AreEqual(user.Email, NewEmail);
                Assert.AreEqual(user.Username, NewUserName);
                Assert.AreEqual(user.Options, NewOptions);
                Assert.AreEqual(user.CreatedAt, tempUser.CreatedAt);
                Assert.AreEqual(user.UpdatedAt, tempUser.UpdatedAt);

                var users = QueryUsers();
                Assert.AreEqual(2, users.Count());
                foreach (var ret in users)
                {
                    if (ret.UserId.Equals(tempUser.UserId))
                    {
                        Assert.AreEqual(ret.Username, tempUser.Username);
                        Assert.AreEqual(ret.Email, tempUser.Email);
                    }
                }
                var getUser = GetUser(tempUser.UserId);
                Assert.AreEqual(getUser.Username, tempUser.Username);
                Assert.AreEqual(getUser.Email, tempUser.Email);

                ITUtil.LogoutUser().Wait();
                try
                {
                    ITUtil.LoginUser("other@example.com", NewPassword).Wait();
                    Assert.Fail("No Exception");
                }
                catch (AggregateException)
                {
                }
                try
                {
                    ITUtil.LoginUser("other@example.com", Password).Wait();
                }
                catch (AggregateException)
                {
                    Assert.Fail("Exception accord!");
                }
            }

        }

        /// <summary>
        /// ユーザ更新（Emailが衝突）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfExceptionConflictEmail()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            var currentUser = NbUser.CurrentUser();
            var tempUser = ITUtil.CloneUser(NbUser.CurrentUser());
            var username2 = "other";
            var email2 = "other@example.com";
            ITUtil.SignUpUser(username2, email2).Wait();

            currentUser.Username = NewUserName;
            currentUser.Email = email2;
            currentUser.Options = NewOptions;
            try
            {
                var result = currentUser.SaveAsync(NewPassword).Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response, "reasonCode"));

                Assert.AreEqual(currentUser.UserId, tempUser.UserId);
                Assert.AreEqual(currentUser.CreatedAt, tempUser.CreatedAt);
                Assert.AreEqual(currentUser.UpdatedAt, tempUser.UpdatedAt);

                var users = QueryUsers();
                Assert.AreEqual(2, users.Count());
                foreach (var ret in users)
                {
                    if (ret.UserId.Equals(tempUser.UserId))
                    {
                        Assert.AreEqual(ret.Username, tempUser.Username);
                        Assert.AreEqual(ret.Email, tempUser.Email);
                    }
                }
            }
        }

        /// <summary>
        /// ユーザ更新（未登録ユーザ）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfExceptionUnexistedUserId()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();

            var user = new NbUser();
            user.UserId = "dummy";
            user.Username = NewUserName;
            user.Email = NewEmail;
            user.Options = NewOptions;
            try
            {
                var result = user.SaveAsync(NewPassword).Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ更新（リクエストボディが空）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfNormalEmptyBody()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();
            var tempUser = ITUtil.CloneUser(user);

            user.Email = null;
            user.Username = null;
            user.Options = null;
            var result = user.SaveAsync(null).Result;
            Assert.AreEqual(result.Email, tempUser.Email);
            Assert.AreEqual(result.Username, tempUser.Username);
            Assert.AreEqual(result.Options, tempUser.Options);
            Assert.AreEqual(result.CreatedAt, tempUser.CreatedAt);
            Assert.AreNotEqual(result.UpdatedAt, tempUser.UpdatedAt);
        }

        /// <summary>
        /// ユーザ更新（101文字以上のEmail）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncSelfExceptionTooLongEmail()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();

            user.Email = "aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeffffffffffgggggggggghhhhhhhhhhiiiiiiiiiijjjjjjjjjjk@example.com";
            try
            {
                var result = user.SaveAsync(null).Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.BadRequest);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }

        }

        /// <summary>
        /// ユーザ更新（ユーザIDがnull）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncExceptionUserIdNull()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsNotNull(NbUser.CurrentUser());
            var user = NbUser.CurrentUser();
            user.UserId = null;
            try
            {
                var result = user.SaveAsync(null).Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
            }
        }

        /// <summary>
        /// ユーザ更新（自ユーザ更新成功時ログイン情報確認）
        /// 情報が更新されること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterSaveAsyncNormal()
        {
            Assert.IsNull(NbService.Singleton.SessionInfo.SessionToken);
            Assert.AreEqual(0, NbService.Singleton.SessionInfo.Expire);
            Assert.IsNull(NbUser.CurrentUser());
            TestSaveAsyncSelfNormalAll();
            var sessionInfo = NbService.Singleton.SessionInfo;
            Assert.IsNotNull(sessionInfo.SessionToken);
            Assert.AreNotEqual(0, sessionInfo.Expire);
            Assert.AreEqual(sessionInfo.CurrentUser.Email, NewEmail);
            Assert.AreEqual(sessionInfo.CurrentUser.Username, NewUserName);
            Assert.AreEqual(sessionInfo.CurrentUser.Options, NewOptions);
        }

        /// <summary>
        /// ユーザ更新（他ユーザ更新成功時ログイン情報確認）
        /// 情報が更新されること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterSaveAsyncOtherNormal()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            ITUtil.LoginUser(Email).Wait();
            var currentUesr = NbUser.CurrentUser();
            var sessionInfo = NbService.Singleton.SessionInfo;
            var otherUser = ITUtil.SignUpUser("other", "other@example.com").Result;

            ITUtil.UseMasterKey();
            otherUser.Username = NewUserName;
            otherUser.Email = NewEmail;
            var result = otherUser.SaveAsync(NewPassword).Result;
            var after = NbUser.CurrentUser();
            Assert.AreEqual(currentUesr.Username, after.Username);
            Assert.AreEqual(currentUesr.Email, after.Email);
            Assert.AreEqual(currentUesr.CreatedAt, after.CreatedAt);
            Assert.AreEqual(currentUesr.UpdatedAt, after.UpdatedAt);
            Assert.AreEqual(NbService.Singleton.SessionInfo, sessionInfo);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ更新（自ユーザ更新失敗時ログイン情報確認）
        /// 情報が更新されないこと
        /// </summary>
        [Test]
        public void TestCurrentUserAfterSaveAsyncSubnormalSaveFailed()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var currentUesr = NbUser.CurrentUser();
            var sessionInfo = NbService.Singleton.SessionInfo;
            try
            {
                var result = user.SaveAsync("aaa").Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException)
            {
                var after = NbUser.CurrentUser();
                Assert.AreEqual(currentUesr.Username, after.Username);
                Assert.AreEqual(currentUesr.Email, after.Email);
                Assert.AreEqual(currentUesr.CreatedAt, after.CreatedAt);
                Assert.AreEqual(currentUesr.UpdatedAt, after.UpdatedAt);
                Assert.AreEqual(NbService.Singleton.SessionInfo, sessionInfo);
            }
        }


        /**
         * DeleteAsync
         **/

        /// <summary>
        /// ユーザ削除（自ユーザ削除）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteAsyncSelfNormal()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var users = new List<string>();
            users.Add(user.UserId);
            SaveGroup(GroupName, users, null, null);
            user.DeleteAsync().Wait();
            Assert.IsNull(NbUser.CurrentUser());
            Assert.IsNull(NbService.Singleton.SessionInfo.SessionToken);
            Assert.AreEqual(0, NbService.Singleton.SessionInfo.Expire);

            ITUtil.UseMasterKey();
            var query = QueryUsers();
            Assert.IsEmpty(query);
            var group = GetGroup(GroupName);
            foreach (var id in group.Users)
            {
                if (id.Equals(user.UserId))
                {
                    Assert.Fail("deleted, but belong to parent group!");
                }
            }
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ削除（他ユーザ削除、未ログイン、マスターキー使用）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteAsyncOtherNormal()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var deleteUser = ITUtil.SignUpUser(NewUserName, NewEmail, NewOptions).Result;
            var users = new List<string>();
            users.Add(user.UserId);
            users.Add(deleteUser.UserId);
            SaveGroup(GroupName, users, null, null);
            ITUtil.LogoutUser().Wait();

            ITUtil.UseMasterKey();
            deleteUser.DeleteAsync().Wait();
            var query = QueryUsers();
            Assert.AreEqual(1, query.Count());
            Assert.AreNotEqual(deleteUser.UserId, query.ToList()[0].UserId);

            var group = GetGroup(GroupName);
            Assert.IsNotNull(group);
            foreach (var id in group.Users)
            {
                if (id.Equals(deleteUser.UserId))
                {
                    Assert.Fail("deleted, but belong to parent group!");
                }
            }
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ削除（自ユーザ削除、未ログイン）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncSelfExceptionNotLoggedIn()
        {
            var user = ITUtil.SignUpUser(UserName, Email, Options).Result;
            try
            {
                user.DeleteAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));

                user = ITUtil.LoginUser(Email).Result;
                var query = QueryUsers();
                Assert.AreEqual(1, query.Count());
                var getUser = GetUser(query.ToList()[0].UserId);
                Assert.AreEqual(getUser.UserId, user.UserId);
            }
        }

        /// <summary>
        /// ユーザ削除（他ユーザ削除、マスターキー未使用）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncOtherExceptionNoMasterKey()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var deleteUser = ITUtil.SignUpUser(NewUserName, NewEmail, NewOptions).Result;
            try
            {
                deleteUser.DeleteAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));

                var query = QueryUsers();
                Assert.AreEqual(2, query.Count());
                Assert.IsNotNull(GetUser(deleteUser.UserId));
            }
        }

        /// <summary>
        /// ユーザ削除（削除が衝突）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Ignore]
        public void TestDeleteAsyncSelfExceptionConflict()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var users = new List<string>();
            users.Add(user.UserId);
            user.DeleteAsync().Wait();
            try
            {
                user.DeleteAsync().Wait();
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
            }
        }

        /// <summary>
        /// ユーザ削除（未登録ユーザ）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncSelfExceptionUnexistedUser()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var deleteUser = new NbUser();
            deleteUser.UserId = "dummy";
            try
            {
                deleteUser.DeleteAsync().Wait();
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ削除（セッショントークン無効）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncSelfExceptionInvalidSessionToken()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var clone = ITUtil.CloneUser(user);
            ITUtil.LogoutDirect();
            try
            {
                user.DeleteAsync().Wait();
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));

                NbService.Singleton.SessionInfo.Clear();
                var after = ITUtil.LoginUser(Email).Result;
                Assert.AreEqual(after.UserId, clone.UserId);
            }
        }

        /// <summary>
        /// ユーザ削除（ユーザIDがnull）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncExceptionUserIdNull()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            user.UserId = null;
            try
            {
                user.DeleteAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
            }
        }

        /// <summary>
        /// ユーザ削除（自ユーザ削除成功時ログイン情報確認）
        /// ユーザ情報、セッション情報がクリアされること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterDeleteAsyncNormal()
        {
            TestDeleteAsyncSelfNormal();
            Assert.IsNull(NbUser.CurrentUser());
            Assert.IsNull(NbService.Singleton.SessionInfo.SessionToken);
            Assert.AreEqual(0, NbService.Singleton.SessionInfo.Expire);
        }

        /// <summary>
        /// ユーザ削除（他ユーザ削除成功時ログイン情報確認）
        /// ユーザ情報、セッション情報がクリアされること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterDeleteAsyncOtherNormal()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var deleteUser = ITUtil.SignUpUser(NewUserName, NewEmail, NewOptions).Result;
            var users = new List<string>();
            users.Add(user.UserId);
            users.Add(deleteUser.UserId);
            SaveGroup(GroupName, users, null, null);
            var currentUser = NbUser.CurrentUser();
            var sessionInfo = NbService.Singleton.SessionInfo;

            ITUtil.UseMasterKey();
            deleteUser.DeleteAsync().Wait();

            var afterUser = NbUser.CurrentUser();
            Assert.AreEqual(afterUser.UserId, currentUser.UserId);
            Assert.AreEqual(afterUser.Username, currentUser.Username);
            Assert.AreEqual(afterUser.Email, currentUser.Email);
            var aftersession = NbService.Singleton.SessionInfo;
            Assert.AreEqual(aftersession.SessionToken, sessionInfo.SessionToken);
            Assert.AreEqual(aftersession.Expire, sessionInfo.Expire);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ削除（自ユーザ削除失敗時ログイン情報確認）
        /// ユーザ情報、セッション情報がクリアされること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterDeleteAsyncSubnormalDeleteFailed()
        {
            ITUtil.SignUpUser(UserName, Email, Options).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var currentUser = NbUser.CurrentUser();
            var sessionInfo = NbService.Singleton.SessionInfo;

            user.UserId = null;
            try
            {
                user.DeleteAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
                var afterUser = NbUser.CurrentUser();
                Assert.AreEqual(afterUser.UserId, currentUser.UserId);
                Assert.AreEqual(afterUser.Username, currentUser.Username);
                Assert.AreEqual(afterUser.Email, currentUser.Email);
                var aftersession = NbService.Singleton.SessionInfo;
                Assert.AreEqual(aftersession.SessionToken, sessionInfo.SessionToken);
                Assert.AreEqual(aftersession.Expire, sessionInfo.Expire);
            }
        }


        /**
         * ResetPasswordWithUsernameAsync
         **/

        /// <summary>
        /// ユーザ名指定パスワードリセット（正常）
        /// 正常に終了すること
        /// </summary>
        [Test]
        public void TestResetPasswordWithUsernameAsyncNormalUsername()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            NbUser.ResetPasswordWithUsernameAsync(UserName).Wait();
        }

        /// <summary>
        /// ユーザ名指定パスワードリセット（ユーザ名に@含む）
        /// 正常に終了すること
        /// </summary>
        [Test]
        public void TestResetPasswordWithUsernameAsyncNormalUsernameContainsAtSymbol()
        {
            var user = ITUtil.SignUpUser("aaa@bbb", Email).Result;
            NbUser.ResetPasswordWithUsernameAsync("aaa@bbb").Wait();
        }

        /// <summary>
        /// ユーザ名指定パスワードリセット（リクエストボディが空）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestResetPasswordWithUsernameAsyncExceptionEmptyBody()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            try
            {
                NbUser.ResetPasswordWithUsernameAsync(null).Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
            }
        }

        /// <summary>
        /// ユーザ名指定パスワードリセット（更新が衝突）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Ignore]
        public void TestResetPasswordWithUsernameAsyncExceptionConflict()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            NbUser.ResetPasswordWithUsernameAsync(UserName).Wait();
            try
            {
                NbUser.ResetPasswordWithUsernameAsync(UserName).Wait();
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
            }
        }

        /// <summary>
        /// ユーザ名指定パスワードリセット（未登録ユーザ）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestResetPasswordWithUsernameAsyncExceptionUnexistedUsername()
        {
            try
            {
                NbUser.ResetPasswordWithUsernameAsync("dummy").Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ名指定パスワードリセット（リセット攻撃対策）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestResetPasswordWithUsernameAsyncExceptionOverLimit()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            NbUser.ResetPasswordWithUsernameAsync(UserName).Wait();
            try
            {
                NbUser.ResetPasswordWithUsernameAsync(UserName).Wait();
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ名指定パスワードリセット（不正なユーザ名）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestResetPasswordWithUsernameAsyncExceptionInvalidUserName()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            try
            {
                NbUser.ResetPasswordWithUsernameAsync("").Wait();
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }


        /**
         * ResetPasswordWithEmailAsync
         **/

        /// <summary>
        /// Email指定パスワードリセット（正常）
        /// 正常に終了すること
        /// </summary>
        [Test]
        public void TestResetPasswordWithEmailAsyncNormalEmail()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            NbUser.ResetPasswordWithEmailAsync(Email).Wait();
        }

        /// <summary>
        /// Email指定パスワードリセット（リクエストボディが空）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestResetPasswordWithEmailAsyncExceptionEmptyBody()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            try
            {
                NbUser.ResetPasswordWithEmailAsync(null).Wait();
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
            }
        }

        /// <summary>
        /// Email指定パスワードリセット（未登録Email）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestResetPasswordWithEmailAsyncExceptionUnexistedEmail()
        {
            var user = ITUtil.SignUpUser(UserName, Email).Result;
            try
            {
                NbUser.ResetPasswordWithEmailAsync("dummy").Wait();
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }


        /**
         * QueryUserAsync
         **/

        /// <summary>
        /// ユーザ検索（Email指定）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalEmail()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "1").Wait();
            var users = NbUser.QueryUserAsync(null, Email + "1").Result;
            Assert.AreEqual(1, users.Count());
            Assert.AreEqual(users.ToList()[0].Username, UserName + "1");
            Assert.AreEqual(users.ToList()[0].Email, Email + "1");
        }

        /// <summary>
        /// ユーザ検索（ユーザ名指定）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalUsername()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "2").Wait();
            var users = NbUser.QueryUserAsync(UserName + "3", null).Result;
            Assert.AreEqual(1, users.Count());
            Assert.AreEqual(users.ToList()[0].Username, UserName + "3");
            Assert.AreEqual(users.ToList()[0].Email, Email + "3");
        }

        /// <summary>
        /// ユーザ検索（Email、ユーザ名指定）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalUsernameAndEmail()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "2").Wait();
            var users = NbUser.QueryUserAsync(UserName + "1", Email + "3").Result;
            Assert.AreEqual(1, users.Count());
            Assert.AreEqual(users.ToList()[0].Username, UserName + "1");
            Assert.AreEqual(users.ToList()[0].Email, Email + "1");
        }

        /// <summary>
        /// ユーザ検索（全件検索）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalAll()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "2").Wait();
            var users = NbUser.QueryUserAsync().Result;
            Assert.AreEqual(3, users.Count());
            foreach (var user in users)
            {
                if (!user.Username.Equals(UserName + 1) && !user.Username.Equals(UserName + 2) &&
                    !user.Username.Equals(UserName + 3))
                {
                    Assert.Fail("Not Expect");
                }
            }
        }

        /// <summary>
        /// ユーザ検索（ユーザ名に@を含む）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalUsernameContainsAtSymbol()
        {
            ITUtil.SignUpUser(UserName + "@", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "2").Wait();
            var users = NbUser.QueryUserAsync(UserName + "@").Result;
            Assert.AreEqual(1, users.Count());
            Assert.AreEqual(users.ToList()[0].Username, UserName + "@");
            Assert.AreEqual(users.ToList()[0].Email, Email + "1");
        }

        /// <summary>
        /// ユーザ検索（ACL権限なし、マスターキー使用）
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalNoACLWithMasterKey()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();

            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.R = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            ITUtil.UseMasterKey();
            var users = NbUser.QueryUserAsync().Result;
            Assert.AreEqual(3, users.Count());
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ検索（ACL権限なし、マスターキー使用）、検索対象なし）
        /// 検索できること、ヒットしないこと
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalNoACLWithMasterKeyNoUser()
        {
            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.R = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            ITUtil.UseMasterKey();
            var users = NbUser.QueryUserAsync().Result;
            Assert.IsEmpty(users);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ検索（ACL権限なし）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncExceptionNoACL()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();

            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.R = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            try
            {
                var users = NbUser.QueryUserAsync().Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ検索（未ログイン）
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncExceptionNotLoggedIn()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();

            try
            {
                var users = NbUser.QueryUserAsync(UserName + "1").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ検索（未登録ユーザ）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncExceptionUnexistedUsername()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "1").Wait();
            try
            {
                var users = NbUser.QueryUserAsync(UserName + "4").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ検索（未登録Email）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncExceptionUnexistedEmail()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "1").Wait();
            try
            {
                var users = NbUser.QueryUserAsync(null, Email + "4").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ検索（ユーザ名とEmail不一致（Email不正））
        /// 検索できること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncNormalUsernameAndUnexistedEmail()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "1").Wait();

            var users = NbUser.QueryUserAsync(UserName + "1", Email + "4").Result;
            Assert.AreEqual(1, users.Count());
        }

        /// <summary>
        /// ユーザ検索（ユーザ名とEmail不一致（ユーザ名不正））
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncExceptionUnexistedUsernameAndEmail()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "1").Wait();
            try
            {
                var users = NbUser.QueryUserAsync(UserName + "4", Email + "1").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ検索（ユーザ名が0文字）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestQueryUserAsyncExceptionInvalidUsernameEmpty()
        {
            ITUtil.SignUpUser(UserName + "1", Email + "1").Wait();
            ITUtil.SignUpUser(UserName + "2", Email + "2").Wait();
            ITUtil.SignUpUser(UserName + "3", Email + "3").Wait();
            ITUtil.LoginUser(Email + "1").Wait();
            try
            {
                var users = NbUser.QueryUserAsync("").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }


        /**
         * GetUserAsync
         **/

        /// <summary>
        /// ユーザ取得（ユーザID指定）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestGetUserAsyncNormal()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.SignUpUser(NewUserName, NewEmail).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var result = NbUser.GetUserAsync(user.UserId).Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.Options, user.Options);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);
        }

        /// <summary>
        /// ユーザ取得（ACL権限なし）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestGetUserAsyncNormalNoACLWithMasterKey()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.SignUpUser(NewUserName, NewEmail).Wait();
            var user = ITUtil.LoginUser(Email).Result;

            ITUtil.UseMasterKey();
            var acl = ITUtil.GetDefaltUserAcl();
            var contentAcl = ITUtil.GetDefaultUserContentAcl();
            contentAcl.R = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateUserBucket(acl, contentAcl).Wait();

            var result = NbUser.GetUserAsync(user.UserId).Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.Options, user.Options);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);

            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザ取得（ユーザID不正）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestGetUserAsyncExceptionInvalidUserId()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.SignUpUser(NewUserName, NewEmail).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            try
            {
                var result = NbUser.GetUserAsync("dummy").Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// ユーザ取得（ユーザIDがnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestGetUserAsyncExceptionUserIdNull()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.SignUpUser(NewUserName, NewEmail).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            try
            {
                var result = NbUser.GetUserAsync(null).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
            }
        }


        /**
         * RefreshCurrentUserAsync
         **/

        /// <summary>
        /// カレントユーザ取得（正常）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestRefreshCurrentUserAsyncNormal()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var result = NbUser.RefreshCurrentUserAsync().Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.Options, user.Options);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);
        }

        /// <summary>
        /// カレントユーザ取得（未ログイン）
        /// Exception（InvalidOperationException）が発行されること
        /// </summary>
        [Test]
        public void TestRefreshCurrentUserAsyncExceptionNotLoggedIn()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            try
            {
                var result = NbUser.RefreshCurrentUserAsync().Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is InvalidOperationException);
            }
        }

        /// <summary>
        /// カレントユーザ取得（セッショントークン無効）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestRefreshCurrentUserAsyncExceptionInvalidSessionToken()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.LoginUser(Email).Wait();
            ITUtil.LogoutDirect();
            try
            {
                var result = NbUser.RefreshCurrentUserAsync().Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                NbService.Singleton.SessionInfo.Clear();
            }
        }

        /// <summary>
        /// カレントユーザ取得（成功時ログイン情報確認）
        /// 取得したユーザ情報が保存済みの情報と同じとなること
        /// </summary>
        [Test]
        public void TestCurrentUserAfterRefreshNormal()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.LoginUser(Email).Wait();
            var currentUser = NbUser.CurrentUser();
            var result = NbUser.RefreshCurrentUserAsync().Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(result.UserId, currentUser.UserId);
            Assert.AreEqual(result.Username, currentUser.Username);
            Assert.AreEqual(result.Email, currentUser.Email);
            Assert.AreEqual(result.Options, currentUser.Options);
            Assert.AreEqual(result.CreatedAt, currentUser.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, currentUser.UpdatedAt);
        }

        /// <summary>
        /// カレントユーザ取得（失敗時ログイン情報確認）
        /// 保存済みのユーザ情報に変化がないこと
        /// </summary>
        [Test]
        public void TestCurrentUserAfterRefreshSubnormalRefreshFailed()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.LoginUser(Email).Wait();
            var currentUser = NbUser.CurrentUser();
            var sessionInfo = NbService.Singleton.SessionInfo;

            NbService.Singleton.AppKey = "dummy";
            try
            {
                var result = NbUser.RefreshCurrentUserAsync().Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException)
            {
                var afterUser = NbUser.CurrentUser();
                var afterSession = NbService.Singleton.SessionInfo;
                Assert.AreEqual(currentUser.UserId, afterUser.UserId);
                Assert.AreEqual(currentUser.Username, afterUser.Username);
                Assert.AreEqual(currentUser.Email, afterUser.Email);
                Assert.AreEqual(currentUser.Options, afterUser.Options);
                Assert.AreEqual(currentUser.CreatedAt, afterUser.CreatedAt);
                Assert.AreEqual(currentUser.UpdatedAt, afterUser.UpdatedAt);
                Assert.AreEqual(sessionInfo.SessionToken, afterSession.SessionToken);
                Assert.AreEqual(sessionInfo.Expire, afterSession.Expire);
                ITUtil.UseNormalKey();
            }
        }


        /**
         * CurrentUser
         **/

        /// <summary>
        /// カレントユーザ取得（ログイン済み）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestCurrentUserNormal()
        {
            TestCurrentUserAfterLogInWithUserNameNormal();
        }

        /// <summary>
        /// カレントユーザ取得（未ログイン）
        /// 取得できないこと
        /// </summary>
        [Test]
        public void TestCurrentUserNormalNotLoggedIn()
        {
            Assert.IsNull(NbUser.CurrentUser());
        }


        /**
         * IsLoggedIn
         **/

        /// <summary>
        /// ログイン状態取得（ログイン済み）
        /// trueが返却されること
        /// </summary>
        [Test]
        public void TestIsLoggedInNormal()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            ITUtil.LoginUser(Email).Wait();
            Assert.IsTrue(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// ログイン状態取得（未ログイン）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsLoggedInNormalNotLoggedIn()
        {
            Assert.IsFalse(NbUser.IsLoggedIn());
        }


        /**
         * IsAclAccessibleForRead
         **/

        /// <summary>
        /// ACLアクセス確認（Readアクセス、正常）
        /// trueが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForReadNomal()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var acl = new NbAcl();
            acl.R.Add(NbGroup.GAnonymous);
            Assert.IsTrue(user.IsAclAccessibleForRead(acl));
        }

        /// <summary>
        /// ACLアクセス確認（Readアクセス、権限なし）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForReadSubnomalNoPermission()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var acl = new NbAcl();
            Assert.IsFalse(user.IsAclAccessibleForRead(acl));
        }

        /// <summary>
        /// ACLアクセス確認（Readアクセス、ACLがnull）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForReadSubnomalNoACL()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            Assert.IsFalse(user.IsAclAccessibleForRead(null));

        }

        /// <summary>
        /// ACLアクセス確認（Readアクセス、ユーザがnull）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForReadSubnomalNoUser()
        {
            var acl = new NbAcl();
            acl.R.Add(NbGroup.GAuthenticated);
            Assert.IsFalse(NbUser.IsAclAccessibleForRead(null, acl));
        }


        /**
         * IsAclAccessibleForUpdate
         **/

        /// <summary>
        /// ACLアクセス確認（Updateアクセス、正常）
        /// trueが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForUpdateNomal()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var acl = new NbAcl();
            acl.U.Add(NbGroup.GAnonymous);
            Assert.IsTrue(user.IsAclAccessibleForUpdate(acl));
        }

        /// <summary>
        /// ACLアクセス確認（Updateアクセス、権限なし）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForUpdateSubnomalNoPermission()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var acl = new NbAcl();
            Assert.IsFalse(user.IsAclAccessibleForUpdate(acl));
        }

        /// <summary>
        /// ACLアクセス確認（Updateアクセス、ACLがnull）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForUpdateSubnomalNoACL()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            Assert.IsFalse(user.IsAclAccessibleForUpdate(null));
        }

        /// <summary>
        /// ACLアクセス確認（Updateアクセス、ユーザがnull）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForUpdateSubnomalNoUser()
        {
            var acl = new NbAcl();
            acl.U.Add(NbGroup.GAuthenticated);
            Assert.IsFalse(NbUser.IsAclAccessibleForUpdate(null, acl));
        }


        /**
         * IsAclAccessibleForDelete
         **/

        /// <summary>
        /// ACLアクセス確認（Deleteアクセス、正常）
        /// trueが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForDeleteNomal()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var acl = new NbAcl();
            acl.D.Add(NbGroup.GAnonymous);
            acl.W.Add(NbGroup.GAnonymous);
            Assert.IsTrue(user.IsAclAccessibleForDelete(acl));
        }

        /// <summary>
        /// ACLアクセス確認（Deleteアクセス、権限なし）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForDeleteSubnomalNoPermission()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var acl = new NbAcl();
            Assert.IsFalse(user.IsAclAccessibleForDelete(acl));
        }

        /// <summary>
        /// ACLアクセス確認（Deleteアクセス、ACLがnull）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForDeleteSubnomalNoACL()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            Assert.IsFalse(user.IsAclAccessibleForDelete(null));
        }

        /// <summary>
        /// ACLアクセス確認（Deleteアクセス、ユーザがnull）
        /// falseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForDeleteSubnomalNoUser()
        {
            ITUtil.SignUpUser(UserName, Email).Wait();
            var user = ITUtil.LoginUser(Email).Result;
            var acl = new NbAcl();
            acl.D.Add(NbGroup.GAuthenticated);
            acl.W.Add(NbGroup.GAuthenticated);
            Assert.IsFalse(NbUser.IsAclAccessibleForDelete(null, acl));
        }


        /**
         * プロパティ確認
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
        /// ユーザの作成、取得、削除を3回繰り返す
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestCreateGetDeleteRepeatNormal()
        {
            for (int i = 0; i < RepeatCount; i++)
            {
                ITUtil.SignUpUser(null, Email + i).Wait();
                var user = ITUtil.LoginUser(Email + i).Result;
                var getUser = GetUser(user.UserId);
                Assert.AreEqual(user.Username, getUser.Username);
                Assert.AreEqual(user.Email, getUser.Email);
                user.DeleteAsync().Wait();
            }
            ITUtil.UseMasterKey();
            Assert.IsEmpty(QueryUsers());
            Assert.IsFalse(NbUser.IsLoggedIn());
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザの作成、更新、削除を3回繰り返す
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestCreateUpdateDeleteRepeatNormal()
        {
            for (int i = 0; i < RepeatCount; i++)
            {
                ITUtil.SignUpUser(null, Email + i).Wait();
                var user = ITUtil.LoginUser(Email + i).Result;
                user.Email = NewEmail;
                user.Username = NewUserName;
                var updateUser = user.SaveAsync(NewPassword).Result;
                Assert.AreEqual(NewUserName, updateUser.Username);
                Assert.AreEqual(NewEmail, updateUser.Email);
                user.DeleteAsync().Wait();
            }
            ITUtil.UseMasterKey();
            Assert.IsEmpty(QueryUsers());
            Assert.IsFalse(NbUser.IsLoggedIn());
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// ユーザのログイン、ログアウトを3回繰り返す
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestLoginLogoutRepeatNormal()
        {
            ITUtil.SignUpUser().Wait();
            for (int i = 0; i < RepeatCount; i++)
            {
                ITUtil.LoginUser().Wait();
                ITUtil.LogoutUser().Wait();
            }
            ITUtil.UseMasterKey();
            Assert.AreEqual(1, QueryUsers().Count());
            Assert.IsFalse(NbUser.IsLoggedIn());
            ITUtil.UseNormalKey();
        }


        /**
         * Util for UserIT
         **/

        private IEnumerable<NbUser> QueryUsers(string userName = null, string email = null)
        {
            return NbUser.QueryUserAsync(userName, email).Result;
        }

        private NbUser GetUser(string userId)
        {
            return NbUser.GetUserAsync(userId).Result;
        }

        private NbGroup SaveGroup(string name, List<string> users = null, List<string> groups = null, NbAcl acl = null)
        {
            var group = new NbGroup(name);
            if (users != null) group.Users = new HashSet<string>(from x in users select x as string);
            if (groups != null) group.Groups = new HashSet<string>(from x in groups select x as string);
            group.Acl = acl;
            return group.SaveAsync().Result;
        }

        private NbGroup GetGroup(string gname)
        {
            return NbGroup.GetGroupAsync(gname).Result;
        }
    }
}
