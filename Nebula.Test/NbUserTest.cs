using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbUserTest
    {
        private MockRestExecutor executor;

        private const string appKey = "X-Application-Key";
        private const string appId = "X-Application-Id";
        private const string session = "X-Session-Token";

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();

            // inject Mock RestExecutor
            executor = new MockRestExecutor();
            NbService.Singleton.RestExecutor = executor;
        }

        private NbJsonObject CreateUserJson(NbJsonObject options = null)
        {
            var json = new NbJsonObject()
            {
                {"_id", "12345"},
                {"username", "foo"},
                {"email", "foo@example.com"},
                {"createdAt", "CREATEDAT"},
                {"updatedAt", "UPDATEDAT"}
            };
            if (options != null)
            {
                json["options"] = options;
            }
            return json;
        }

        private async Task<NbUser> DoSignUpWithMock(NbService service = null)
        {
            service = service ?? NbService.Singleton;
            // create dummy response
            var json = CreateUserJson();

            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var user = new NbUser(service);
            user.Username = "foo";
            user.Email = "foo@example.com";

            var result = await user.SignUpAsync("password");

            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);

            return result;
        }

        private async Task<NbUser> DoLoginOnlineWithMock(NbService service = null)
        {
            service = service ?? NbService.Singleton;
            // create dummy response
            var json = CreateUserJson();
            json["sessionToken"] = "1234567890";
            json["expire"] = 999999999999L;

            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var user = await NbUser.LoginWithUsernameAsync("foo", "password", service);

            Assert.AreEqual("foo", user.Username);
            Assert.IsTrue(NbUser.IsLoggedIn(service));

            return user;
        }

        private async Task DoLogoutWithMock(NbService service = null)
        {
            service = service ?? NbService.Singleton;
            var json = new NbJsonObject()
            {
                {"_id", "12345"}
            };

            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            await NbUser.LogoutAsync(NbUser.LoginMode.Online, service);
            Assert.IsFalse(NbUser.IsLoggedIn(service));
        }


        /**
         * プロパティ確認
         **/

        /// <summary>
        /// プロパティ（正常）
        /// 初期値が入っていること
        /// </summary>
        [Test]
        public void TestInitNomal()
        {
            var user = new NbUser();

            Assert.IsNull(user.UserId);
            Assert.IsNull(user.Username);
            Assert.IsNull(user.Email);
            Assert.IsNull(user.Options);
            Assert.IsNull(user.Groups);
            Assert.IsNull(user.CreatedAt);
            Assert.IsNull(user.UpdatedAt);
        }


        /**
         * Constructor(NbUser)
         **/

        /// <summary>
        /// コンストラクタ（正常）
        /// Serviceが設定されること。
        /// Service以外のプロパティが初期値であること。
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            // Main
            var user = new NbUser();

            // Assert
            Assert.AreEqual(NbService.Singleton, user.Service);
            Assert.IsNull(user.UserId);
            Assert.IsNull(user.Username);
            Assert.IsNull(user.Email);
            Assert.IsNull(user.Options);
            Assert.IsNull(user.Groups);
            Assert.IsNull(user.CreatedAt);
            Assert.IsNull(user.UpdatedAt);
        }

        /// <summary>
        /// コンストラクタ（サービス指定）（正常）
        /// 指定したServiceが設定されること。
        /// Service以外のプロパティが初期値であること。
        /// </summary>
        [Test]
        public void TestConstructorWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();

            // Main
            var user = new NbUser(service);

            // Assert
            Assert.AreEqual(service, user.Service);
            Assert.IsNull(user.UserId);
            Assert.IsNull(user.Username);
            Assert.IsNull(user.Email);
            Assert.IsNull(user.Options);
            Assert.IsNull(user.Groups);
            Assert.IsNull(user.CreatedAt);
            Assert.IsNull(user.UpdatedAt);

            NbService.EnableMultiTenant(false);
        }


        /**
         * SignUpAsync
         **/

        /// <summary>
        /// サインアップ（正常）
        /// サインアップできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestSignUpAsyncNormal()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var response = new MockRestResponse(HttpStatusCode.OK, CreateUserJson(options).ToString());
            executor.AddResponse(response);

            // Main
            var user = new NbUser();
            user.Username = "foo";
            user.Email = "foo@example.com";
            user.Options = options;

            var result = await user.SignUpAsync("password");

            // Check Response
            Assert.AreEqual(result.UserId, "12345");
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.Options, user.Options);
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson[Field.Username], user.Username);
            Assert.AreEqual(reqJson[Field.Email], user.Email);
            Assert.AreEqual(reqJson[Field.Password], "password");
            Assert.AreEqual(reqJson[Field.Options], user.Options);
        }

        /// <summary>
        /// サインアップ（ユーザ情報にEmail設定なし）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestSignUpAsyncExceptionNoEmail()
        {
            var response = new MockRestResponse(HttpStatusCode.OK, CreateUserJson().ToString());
            executor.AddResponse(response);

            // Main
            var user = new NbUser();

            var result = await user.SignUpAsync("password");
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// サインアップ（パスワードがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestSignUpAsyncExceptionNoPassword()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var response = new MockRestResponse(HttpStatusCode.OK, CreateUserJson(options).ToString());
            executor.AddResponse(response);

            // Main
            var user = new NbUser();
            user.Username = "foo";
            user.Email = "foo@example.com";
            user.Options = options;

            var result = await user.SignUpAsync(null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// サインアップ（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestSignUpAsyncExceptionFailer()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            var user = new NbUser();
            user.Username = "foo";
            user.Email = "foo@example.com";
            user.Options = options;

            try
            {
                var result = await user.SignUpAsync("password");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * LoginWithUsernameAsync
         **/

        /// <summary>
        /// ユーザ名でオンラインログインする（正常）
        /// ユーザ名でログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncNormal()
        {
            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithUsernameAsync("foo", "password");

            // Check Response
            VerifyLoginResult(result, json);
            Assert.IsTrue(NbUser.IsLoggedIn());

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson[Field.Username], "foo");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock();
        }

        private NbJsonObject CreateDummyLoginResult()
        {
            var options = new NbJsonObject() {{"key1", "value1"}};
            var groups = new List<string>() {"g1", "g2", "g3"};
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            return json;
        }

        private static void VerifyLoginResult(NbUser user, NbJsonObject userJson)
        {
            Assert.AreEqual(user.UserId, userJson[Field.Id]);
            Assert.AreEqual(user.Username, userJson[Field.Username]);
            Assert.AreEqual(user.Email, userJson[Field.Email]);
            Assert.AreEqual(user.Options, userJson[Field.Options]);
            Assert.AreEqual(user.Groups, userJson[Field.Groups]);
            Assert.AreEqual(user.CreatedAt, userJson[Field.CreatedAt]);
            Assert.AreEqual(user.UpdatedAt, userJson[Field.UpdatedAt]);
        }

        /// <summary>
        /// ユーザ名でオンラインログインする（サービス指定）（正常）
        /// ユーザ名でログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithUsernameAsync("foo", "password", service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            VerifyLoginResult(result, json);
            Assert.IsTrue(NbUser.IsLoggedIn(service));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Username], "foo");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock(service);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// ユーザ名でオンラインログインする（ユーザ名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestLoginWithUsernameAsyncExceptionNoUsername()
        {
            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithUsernameAsync(null, "password");
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ユーザ名でオンラインログインする（パスワードがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestLoginWithUsernameAsyncExceptionNoPassword()
        {
            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithUsernameAsync("foo", null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ユーザ名でオンラインログインする（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionFailer()
        {
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var result = await NbUser.LoginWithUsernameAsync("foo", "password");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }


        /**
         * LoginWithEmailAsync
         **/

        /// <summary>
        /// Email でオンラインログインする（正常）
        /// Emailでログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncNormal()
        {
            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithEmailAsync("foo@example.com", "password");

            // Check Response
            VerifyLoginResult(result, json);
            Assert.IsTrue(NbUser.IsLoggedIn());

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson[Field.Email], "foo@example.com");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// Email でオンラインログインする（サービス指定）（正常）
        /// Emailでログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithEmailAsync("foo@example.com", "password", service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            VerifyLoginResult(result, json);
            Assert.IsTrue(NbUser.IsLoggedIn(service));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Email], "foo@example.com");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock(service);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// Email でオンラインログインする。（Emailがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestLoginWithEmailAsyncExceptionNoEmail()
        {
            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithEmailAsync(null, "password");
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// Email でオンラインログインする。（パスワードがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestLoginWithEmailAsyncExceptionNoPassword()
        {
            var json = CreateDummyLoginResult();

            // Main
            var result = await NbUser.LoginWithEmailAsync("foo@example.com", null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// Email でオンラインログインする。（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionFailer()
        {
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var result = await NbUser.LoginWithEmailAsync("foo@example.com", "password");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsFalse(NbUser.IsLoggedIn());
            }
        }

        /// <summary>
        /// ワンタイムトークンでオンラインログインする（サービス指定）（正常）
        /// ワンタイムトークンでログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithTokenNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var json = CreateDummyLoginResult();

            // Main
            var param = new NbUser.LoginParam()
            {
                Token = "TOKEN"
            };
            var result = await NbUser.LoginAsync(param, service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            VerifyLoginResult(result, json);
            Assert.IsTrue(NbUser.IsLoggedIn(service));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.OneTimeToken], "TOKEN");

            // after
            await DoLogoutWithMock(service);

            NbService.EnableMultiTenant(false);
        }

        /**
         * CurrentUser
         **/

        /// <summary>
        /// 現在ログイン中のユーザを返す（正常）
        /// ログイン中ユーザ情報を取得できること
        /// </summary>
        [Test]
        public async void TestCurrentUserNormal()
        {
            await DoLoginOnlineWithMock();

            // Main
            Assert.IsNotNull(NbUser.CurrentUser());

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// 現在ログイン中のユーザを返す（サービス指定）（正常）
        /// ログイン中ユーザ情報を取得できること
        /// </summary>
        [Test]
        public async void TestCurrentUserWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            await DoLoginOnlineWithMock(service);

            // Main
            Assert.IsNotNull(NbUser.CurrentUser(service));

            // after
            await DoLogoutWithMock(service);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// 現在ログイン中のユーザを返す（未ログイン）
        /// ログイン中ユーザがいない場合はnullが返却されること
        /// </summary>
        [Test]
        public void TestCurrentUserSubNormalNoLoggedIn()
        {
            // Main
            Assert.IsNull(NbUser.CurrentUser());
        }

        /// <summary>
        /// 現在ログイン中のユーザを返す（期限切れ）
        /// 有効期限切れの場合はnullが返却されること
        /// </summary>
        [Test]
        public async void TestCurrentUserSubNormalExpired()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 1L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            await NbUser.LoginWithUsernameAsync("foo", "password");

            // Main
            Assert.IsNull(NbUser.CurrentUser());
        }


        /**
         * LogoutAsync
         **/

        /// <summary>
        /// ログアウトする（正常）
        /// ログアウトできること
        /// リクエストの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLogoutAsyncNormal()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            await NbUser.LoginWithEmailAsync("foo@example.com", "password");
            Assert.IsTrue(NbUser.IsLoggedIn());

            response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.LogoutAsync();
            Assert.IsFalse(NbUser.IsLoggedIn());

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Delete, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(4, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.IsTrue(req.Headers.ContainsKey(session));
        }

        /// <summary>
        /// ログアウトする（サービス指定）（正常）
        /// ログアウトできること
        /// リクエストの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLogoutAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            await NbUser.LoginWithEmailAsync("foo@example.com", "password", service);
            Assert.IsTrue(NbUser.IsLoggedIn(service));

            response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.LogoutAsync(NbUser.LoginMode.Online, service);
            Assert.IsFalse(NbUser.IsLoggedIn(service));

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Delete, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// ログアウトする（正常、オフライン）
        /// オフラインモードを指定してログアウトできること
        /// </summary>
        [Test]
        public async void TestLogoutAsyncModeOfflineNormal()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            await NbUser.LoginWithEmailAsync("foo@example.com", "password");
            Assert.IsTrue(NbUser.IsLoggedIn());

            // Main
            await NbUser.LogoutAsync(NbUser.LoginMode.Offline);
            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// ログアウトする（正常、オート）
        /// オートモードを指定してログアウトできること
        /// </summary>
        [Test]
        public async void TestLogoutAsyncModeAutoOnlineNormal()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            await NbUser.LoginWithEmailAsync("foo@example.com", "password");
            Assert.IsTrue(NbUser.IsLoggedIn());

            response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.LogoutAsync(NbUser.LoginMode.Auto);
            Assert.IsFalse(NbUser.IsLoggedIn());

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Delete, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(4, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.IsTrue(req.Headers.ContainsKey(session));
        }

        /// <summary>
        /// ログアウトする（未ログイン）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestLogoutAsyncExceptionNoLoggedIn()
        {
            var response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.LogoutAsync();
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ログアウトする（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLogoutAsyncExceptionFailer()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);
            await NbUser.LoginWithEmailAsync("foo@example.com", "password");
            Assert.IsTrue(NbUser.IsLoggedIn());

            response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                await NbUser.LogoutAsync();
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * SaveAsync
         **/

        /// <summary>
        /// ユーザ情報を更新する（正常）
        /// ユーザの情報を更新できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormal()
        {
            var user = await DoSignUpWithMock();
            var options = new NbJsonObject() { { "key1", "value1" } };
            user.Options = options;
            var json = new NbJsonObject()
            {
                {Field.Id, user.UserId},
                {Field.Username, user.Username},
                {Field.Email, user.Email},
                {Field.Options, options},
                {Field.CreatedAt, user.CreatedAt},
                {Field.UpdatedAt, user.UpdatedAt},
            };

            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var result = await user.SaveAsync();

            // Check Response
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users/" + user.UserId));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson[Field.Username], json[Field.Username]);
            Assert.AreEqual(reqJson[Field.Email], json[Field.Email]);
            Assert.AreEqual(reqJson[Field.Options], json[Field.Options]);
            Assert.IsFalse(reqJson.ContainsKey(Field.Password));
        }

        /// <summary>
        /// ユーザ情報を更新する（パスワード指定あり）（正常）
        /// ユーザの情報を更新できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestSaveAsyncWithPasswordNormal()
        {
            var user = await DoSignUpWithMock();
            var options = new NbJsonObject() { { "key1", "value1" } };
            user.Options = options;
            var json = new NbJsonObject()
            {
                {Field.Id, user.UserId},
                {Field.Username, user.Username},
                {Field.Email, user.Email},
                {Field.Options, options},
                {Field.CreatedAt, user.CreatedAt},
                {Field.UpdatedAt, user.UpdatedAt},
            };

            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var result = await user.SaveAsync("password");

            // Check Response
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users/" + user.UserId));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson[Field.Username], json[Field.Username]);
            Assert.AreEqual(reqJson[Field.Email], json[Field.Email]);
            Assert.AreEqual(reqJson[Field.Options], json[Field.Options]);
            Assert.AreEqual(reqJson[Field.Password], "password");
        }

        /// <summary>
        /// ユーザ情報を更新する（ユーザID未設定）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestSaveAsyncExceptionNoUserId()
        {
            var user = new NbUser();

            var result = await user.SaveAsync();
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ユーザ情報を更新する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionFailer()
        {
            var user = await DoSignUpWithMock();
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            try
            {
                var result = await user.SaveAsync();
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * DeleteAsync
         **/

        /// <summary>
        /// ユーザを削除する（正常）
        /// ユーザを削除できること
        /// リクエストの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormal()
        {
            var user = await DoLoginOnlineWithMock();
            var response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await user.DeleteAsync();

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Delete, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users/" + user.UserId));
            Assert.AreEqual(4, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.IsTrue(req.Headers.ContainsKey(session));
        }

        /// <summary>
        /// ユーザを削除する（ユーザID未設定）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteAsyncExceptionNoUserId()
        {
            var user = new NbUser();

            // Main
            await user.DeleteAsync();
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ユーザを削除する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionFailer()
        {
            var user = await DoSignUpWithMock();
            var json = new NbJsonObject()
            {
                {"error", "DeleteAsync Error"}
            };
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                await user.DeleteAsync();
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * ResetPasswordWithUsername
         **/

        /// <summary>
        /// パスワードリセット (username指定)（正常）
        /// ユーザ名を指定してパスワードリセットができること
        /// リクエスト情報が正しいこと
        /// </summary>
        [Test]
        public async void TestResetPasswordWithUsernameAsyncNormal()
        {
            var response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.ResetPasswordWithUsernameAsync("foo");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/request_password_reset"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
        }

        /// <summary>
        /// パスワードリセット (username指定)（サービス指定）（正常）
        /// ユーザ名を指定してパスワードリセットができること
        /// リクエスト情報が正しいこと
        /// </summary>
        [Test]
        public async void TestResetPasswordWithUsernameAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.ResetPasswordWithUsernameAsync("foo", service);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/request_password_reset"));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// パスワードリセット (username指定)（ユーザ名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestResetPasswordWithUsernameAsyncExceptionNoUsername()
        {
            // Main
            await NbUser.ResetPasswordWithUsernameAsync(null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// パスワードリセット (username指定)（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestResetPasswordWithUsernameAsyncExceptionFailer()
        {
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                await NbUser.ResetPasswordWithUsernameAsync("foo");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * ResetPasswordWithEmail
         **/

        /// <summary>
        /// パスワードリセット (email指定)（正常）
        /// E-mailを指定してパスワードリセットができること
        /// リクエスト情報が正しいこと
        /// </summary>
        [Test]
        public async void TestResetPasswordWithEmailAsyncNormal()
        {
            var response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.ResetPasswordWithEmailAsync("foo@example.com");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/request_password_reset"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
        }

        /// <summary>
        /// パスワードリセット (email指定)（サービス指定）（正常）
        /// E-mailを指定してパスワードリセットができること
        /// リクエスト情報が正しいこと
        /// </summary>
        [Test]
        public async void TestResetPasswordWithEmailAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var response = new MockRestResponse(HttpStatusCode.OK);
            executor.AddResponse(response);

            // Main
            await NbUser.ResetPasswordWithEmailAsync("foo@example.com", service);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/request_password_reset"));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// パスワードリセット (email指定)（Emailがnull）
        /// ArgumentNullExceptionを発行すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestResetPasswordWithEmailAsyncExceptionNoEmail()
        {
            // Main
            await NbUser.ResetPasswordWithEmailAsync(null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// パスワードリセット (email指定)（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestResetPasswordWithEmailAsyncExceptionFailer()
        {
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                await NbUser.ResetPasswordWithEmailAsync("foo@example.com");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * QueryUserAsync
         **/

        /// <summary>
        /// ユーザ情報一覧を取得する（正常）
        /// ユーザ情報一覧を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryUserAsyncNormal()
        {
            var json = new NbJsonArray();
            json.Add(CreateUserJson());
            var responseJson = new NbJsonObject()
            {
                {"results", json}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, responseJson.ToString());
            executor.AddResponse(response);

            var results = await NbUser.QueryUserAsync("foo", "foo@example.com");

            foreach (var result in results)
            {
                // Check Response
                Assert.AreEqual(result.UserId, "12345");
                Assert.AreEqual(result.Username, "foo");
                Assert.AreEqual(result.Email, "foo@example.com");
                Assert.AreEqual(result.CreatedAt, "CREATEDAT");
                Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(req.QueryParams[Field.Username], "foo");
            Assert.AreEqual(req.QueryParams[Field.Email], "foo@example.com");
        }

        /// <summary>
        /// ユーザ情報一覧を取得する（正常、ユーザ名指定）
        /// ユーザ情報一覧を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryUserAsyncNormalUserName()
        {
            var json = new NbJsonArray();
            json.Add(CreateUserJson());
            var responseJson = new NbJsonObject()
            {
                {"results", json}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, responseJson.ToString());
            executor.AddResponse(response);

            var results = await NbUser.QueryUserAsync("foo");

            foreach (var result in results)
            {
                // Check Response
                Assert.AreEqual(result.UserId, "12345");
                Assert.AreEqual(result.Username, "foo");
                Assert.AreEqual(result.Email, "foo@example.com");
                Assert.AreEqual(result.CreatedAt, "CREATEDAT");
                Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(req.QueryParams[Field.Username], "foo");
        }

        /// <summary>
        /// ユーザ情報一覧を取得する（正常、Email指定）
        /// ユーザ情報一覧を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryUserAsyncNormalEmail()
        {
            var json = new NbJsonArray();
            json.Add(CreateUserJson());
            var responseJson = new NbJsonObject()
            {
                {"results", json}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, responseJson.ToString());
            executor.AddResponse(response);

            var results = await NbUser.QueryUserAsync(null, "foo@example.com");

            foreach (var result in results)
            {
                // Check Response
                Assert.AreEqual(result.UserId, "12345");
                Assert.AreEqual(result.Username, "foo");
                Assert.AreEqual(result.Email, "foo@example.com");
                Assert.AreEqual(result.CreatedAt, "CREATEDAT");
                Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(req.QueryParams[Field.Email], "foo@example.com");
        }

        /// <summary>
        /// ユーザ情報一覧を取得する（サービス指定）（正常）
        /// ユーザ情報一覧を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryUserAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var json = new NbJsonArray();
            json.Add(CreateUserJson());
            var responseJson = new NbJsonObject()
            {
                {"results", json}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, responseJson.ToString());
            executor.AddResponse(response);

            var results = await NbUser.QueryUserAsync("foo", "foo@example.com", service);

            foreach (var result in results)
            {
                // Check Response
                Assert.AreEqual(result.Service, service);
                Assert.AreEqual(result.UserId, "12345");
                Assert.AreEqual(result.Username, "foo");
                Assert.AreEqual(result.Email, "foo@example.com");
                Assert.AreEqual(result.CreatedAt, "CREATEDAT");
                Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users"));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// ユーザ情報一覧を取得する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestQueryUserAsyncExceptionFailer()
        {
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            try
            {
                var result = await NbUser.QueryUserAsync();
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * GetUserAsync
         **/

        /// <summary>
        /// ユーザ情報を取得する（正常）
        /// ユーザ情報を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestGetUserAsyncNormal()
        {
            var json = CreateUserJson();
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var result = await NbUser.GetUserAsync("12345");

            // Check Response
            Assert.AreEqual(result.UserId, "12345");
            Assert.AreEqual(result.Username, "foo");
            Assert.AreEqual(result.Email, "foo@example.com");
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users/12345"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
        }

        /// <summary>
        /// ユーザ情報を取得する（サービス指定）（正常）
        /// ユーザ情報を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestGetUserAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var json = CreateUserJson();
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var result = await NbUser.GetUserAsync("12345", service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            Assert.AreEqual(result.UserId, "12345");
            Assert.AreEqual(result.Username, "foo");
            Assert.AreEqual(result.Email, "foo@example.com");
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users/12345"));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// ユーザ情報を取得する（ユーザIDがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestGetUserAsyncExceptionNoUserId()
        {
            var result = await NbUser.GetUserAsync(null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ユーザ情報を取得する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestGetUserAsyncExceptionFailer()
        {
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            try
            {
                var result = await NbUser.GetUserAsync("12345");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }


        /**
         * RefreshCurrentUserAsync
         **/

        /// <summary>
        /// ログイン中ユーザの情報を取得する（正常）
        /// ログイン中ユーザの情報を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestRefreshCurrentUserAsyncNormal()
        {
            var user = await DoLoginOnlineWithMock();
            var json = CreateUserJson();
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbUser.RefreshCurrentUserAsync();

            // Check Response
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users/current"));
            Assert.AreEqual(4, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.IsTrue(req.Headers.ContainsKey(session));

            //after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// ログイン中ユーザの情報を取得する（サービス指定）（正常）
        /// ログイン中ユーザの情報を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestRefreshCurrentUserAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            var user = await DoLoginOnlineWithMock(service);
            var json = CreateUserJson();
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbUser.RefreshCurrentUserAsync(service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/users/current"));

            //after
            await DoLogoutWithMock(service);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// ログイン中ユーザの情報を取得する（未ログイン）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestRefreshCurrentUserAsyncExceptionNoLoggedin()
        {
            var result = await NbUser.RefreshCurrentUserAsync();
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ログイン中ユーザの情報を取得する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestRefreshCurrentUserAsyncExceptionFailer()
        {
            var user = await DoLoginOnlineWithMock();
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            try
            {
                var result = await NbUser.RefreshCurrentUserAsync();
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
            await DoLogoutWithMock();
        }


        /**
         * IsAclAccessibleForRead
         **/

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// オーナーとマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForReadNormalOwnerMatch()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            Assert.IsTrue(NbUser.IsAclAccessibleForRead(user, acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// Read権限に"g:anonymous"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForReadNormalAnonymous()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            Assert.IsTrue(NbUser.IsAclAccessibleForRead(user, acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// Read権限に"g:authenticated"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForReadNormalAuthenticated()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            Assert.IsTrue(NbUser.IsAclAccessibleForRead(user, acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// Read権限にユーザIDが設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForReadNormalUserId()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.R.Add(user.UserId);
            Assert.IsTrue(NbUser.IsAclAccessibleForRead(user, acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// Read権限がグループ情報でマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForReadNormalGroup()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.R.Add("g:g1");
            Assert.IsTrue(NbUser.IsAclAccessibleForRead(user, acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// マッチするものがない場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForReadSubNormalNoMatch()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.R.Add("test");
            acl.R.Add("g:r1");
            Assert.IsFalse(NbUser.IsAclAccessibleForRead(user, acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// ユーザ情報の指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForReadSubNormalNoUser()
        {
            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForRead(null, acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// ユーザ情報内のユーザIDがnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForReadSubNormalNoUserId()
        {
            var acl = new NbAcl();
            acl.R.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForRead(new NbUser(), acl));
        }

        /// <summary>
        /// ユーザ acl に read アクセス可能か調べる
        /// ACLの指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForReadSubNormalNoAcl()
        {
            var user = await DoSignUpWithMock();
            Assert.IsFalse(NbUser.IsAclAccessibleForRead(user, null));
        }


        /**
         * IsAclAccessibleForUpdate
         **/

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// オーナーとマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalOwnerMatch()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Write権限に"g:anonymous"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalWriteAnonymous()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.W.Add("g:anonymous");
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Write権限に"g:authenticated"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalWriteAuthenticated()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.W.Add("g:authenticated");
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Write権限にユーザIDが設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalWriteUserId()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.W.Add(user.UserId);
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Write権限がグループ情報でマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalWriteGroup()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.W.Add("g:g1");
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Update権限に"g:anonymous"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalAnonymous()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.U.Add("g:anonymous");
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Update権限に"g:authenticated"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalAuthenticated()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.U.Add("g:authenticated");
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Update権限にユーザIDが設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalUserId()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.U.Add(user.UserId);
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// Update権限がグループ情報でマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateNormalGroup()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.U.Add("g:g1");
            Assert.IsTrue(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// マッチするものがない場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateSubNormalNoMatch()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.U.Add("test");
            acl.U.Add("g:u1");
            Assert.IsFalse(NbUser.IsAclAccessibleForUpdate(user, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// ユーザ情報の指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForUpdateSubNormalNoUser()
        {
            var acl = new NbAcl();
            acl.U.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForUpdate(null, acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// ユーザ情報内のユーザIDがnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForUpdateSubNormalNoUserId()
        {
            var acl = new NbAcl();
            acl.U.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForUpdate(new NbUser(), acl));
        }

        /// <summary>
        /// ユーザ acl に update アクセス可能か調べる
        /// ACLの指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForUpdateSubNormalNoAcl()
        {
            var user = await DoSignUpWithMock();
            Assert.IsFalse(NbUser.IsAclAccessibleForUpdate(user, null));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// オーナーとマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalOwnerMatch()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }


        /**
         * IsAclAccessibleForDelete
         **/

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Write権限に"g:anonymous"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalWriteAnonymous()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.W.Add("g:anonymous");
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Write権限に"g:authenticated"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalWriteAuthenticated()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.W.Add("g:authenticated");
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Write権限にユーザIDが設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalWriteUserId()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.W.Add(user.UserId);
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Write権限がグループ情報でマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalWriteGroup()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.W.Add("g:g1");
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }
        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Delete権限に"g:anonymous"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalAnonymous()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.D.Add("g:anonymous");
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }
        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Delete権限に"g:authenticated"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalAuthenticated()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.D.Add("g:authenticated");
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }
        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Delete権限にユーザIDが設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalUserId()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.D.Add(user.UserId);
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }
        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// Delete権限がグループ情報でマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteNormalGroup()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.D.Add("g:g1");
            Assert.IsTrue(NbUser.IsAclAccessibleForDelete(user, acl));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// マッチするものがない場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteSubNormalNoMatch()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.D.Add("test");
            acl.D.Add("g:d1");
            Assert.IsFalse(NbUser.IsAclAccessibleForDelete(user, acl));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// ユーザ情報の指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForDeleteSubNormalNoUser()
        {
            var acl = new NbAcl();
            acl.D.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForDelete(null, acl));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// ユーザ情報内のユーザIDがnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForDeleteSubNormalNoUserId()
        {
            var acl = new NbAcl();
            acl.D.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForDelete(new NbUser(), acl));
        }

        /// <summary>
        /// ユーザ acl に delete アクセス可能か調べる
        /// ACLの指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForDeleteSubNormalNoAcl()
        {
            var user = await DoSignUpWithMock();
            Assert.IsFalse(NbUser.IsAclAccessibleForDelete(user, null));
        }


        /**
         * IsAclAccessibleForAdmin
         **/

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// Adminに"g:anonymous"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForAdminNormalAnonymous()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.Admin.Add("g:anonymous");
            Assert.IsTrue(NbUser.IsAclAccessibleForAdmin(user, acl));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// Adminに"g:authenticated"が設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForAdminNormalAuthenticated()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.Admin.Add("g:authenticated");
            Assert.IsTrue(NbUser.IsAclAccessibleForAdmin(user, acl));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// AdminにユーザIDが設定されている場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForAdminNormalUserId()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.Admin.Add(user.UserId);
            Assert.IsTrue(NbUser.IsAclAccessibleForAdmin(user, acl));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// Adminがグループ情報でマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForAdminNormalWriteGroup()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.Admin.Add("g:g1");
            Assert.IsTrue(NbUser.IsAclAccessibleForAdmin(user, acl));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// マッチするものがない場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForAdminSubNormalNoMatch()
        {
            var user = await DoSignUpWithMock();
            var groups = new List<string>() { "g1", "g2", "g3" };
            user.Groups = groups;
            var acl = new NbAcl();
            acl.Admin.Add("test");
            acl.Admin.Add("g:d1");
            Assert.IsFalse(NbUser.IsAclAccessibleForAdmin(user, acl));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// ユーザ情報の指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForAdminSubNormalNoUser()
        {
            var acl = new NbAcl();
            acl.Admin.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForAdmin(null, acl));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// ユーザ情報内のユーザIDがnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public void TestIsAclAccessibleForAdminSubNormalNoUserId()
        {
            var acl = new NbAcl();
            acl.Admin.Add("g:authenticated");
            Assert.IsFalse(NbUser.IsAclAccessibleForAdmin(new NbUser(), acl));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// ACLの指定がnullの場合はfalseが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForAdminSubNormalNoAcl()
        {
            var user = await DoSignUpWithMock();
            Assert.IsFalse(NbUser.IsAclAccessibleForAdmin(user, null));
        }

        /// <summary>
        /// ユーザ acl に Admin権限が含まれるかを調べる
        /// オーナーとマッチした場合はtrueが返却されること
        /// </summary>
        [Test]
        public async void TestIsAclAccessibleForAdminNormalOwnerMatch()
        {
            var user = await DoSignUpWithMock();
            var acl = new NbAcl();
            acl.Owner = user.UserId;
            Assert.IsTrue(NbUser.IsAclAccessibleForAdmin(user, acl));
        }
    }
}
