using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbOfflineUserTest
    {
        private MockRestExecutor executor;

        private const string appKey = "X-Application-Key";
        private const string appId = "X-Application-Id";

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();

            // inject Mock RestExecutor
            executor = new MockRestExecutor();
            NbService.Singleton.RestExecutor = executor;
            SwitchOfflineService(true);
        }

        [TearDown]
        public void TearDown()
        {
            SwitchOfflineService(false);
        }

        private void SwitchOfflineService(bool enable, NbService service = null)
        {
            service = service ?? NbService.Singleton;
            if (enable)
            {
                NbOfflineService.SetInMemoryMode();
                NbOfflineService.EnableOfflineService(service);
            }
            else
            {
                service.DisableOffline();
            }
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

        private async Task<NbUser> DoLoginOnlineWithMock(NbService service = null, bool expired = true)
        {
            service = service ?? NbService.Singleton;
            // create dummy response
            var json = CreateUserJson();
            json["sessionToken"] = "1234567890";
            if (expired) json["expire"] = 999999999999L;
            else json["expire"] = 1L;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            var user = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Online, service);
            Assert.AreEqual("foo", user.Username);
            if (expired) Assert.IsTrue(NbUser.IsLoggedIn(service));
            else Assert.IsFalse(NbUser.IsLoggedIn(service));

            return user;
        }

        private async Task DoLogoutWithMock(NbService service = null)
        {
            var json = new NbJsonObject()
            {
                {"_id", "12345"}
            };
            await NbUser.LogoutAsync(NbUser.LoginMode.Offline, service);
        }

        private bool CheckLoginCache(string username, string email, NbService service = null)
        {
            service = service ?? NbService.Singleton;
            if (!service.IsOfflineEnabled()) return false;

            var database = (NbDatabaseImpl)service.OfflineService.Database;
            using (var dbContext = database.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                LoginCache login = null;
                if (username != null) login = dao.FindByUsername(username);
                else if (email != null) login = dao.FindByEmail(email);
                if (login == null) return false;

                dao.RemoveAll();
                dao.SaveChanges();
            }

            return true;
        }

        private void CreateObjcectCache(NbService service = null)
        {
            service = service ?? NbService.Singleton;

            var bucket = new NbOfflineObjectBucket<NbOfflineObject>("testBucket");
            var obj = bucket.NewObject();
            var result = obj.SaveAsync().Result;
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
            var user = new NbOfflineUser();

            Assert.IsNull(user.UserId);
            Assert.IsNull(user.Username);
            Assert.IsNull(user.Email);
            Assert.IsNull(user.Options);
            Assert.IsNull(user.Groups);
            Assert.IsNull(user.CreatedAt);
            Assert.IsNull(user.UpdatedAt);
        }


        /**
         * LoginWithUsernameAsync
        **/

        /// <summary>
        /// ユーザ名でログインする（オンラインモード）（正常）
        /// ユーザ名でログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncNormalOnline()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password");

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsTrue(CheckLoginCache("foo", null));

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

        /// <summary>
        /// ユーザ名でログインする（オートモード。オンライン）（正常）
        /// ユーザ名でログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncNormalAutoOnline()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Auto);

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsTrue(CheckLoginCache("foo", null));

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

        /// <summary>
        /// ユーザ名でログインする（オンラインモード）（サービス指定）（正常）
        /// ユーザ名でログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncWithServiceNormalOnline()
        {
            SwitchOfflineService(false);
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;
            service.AppId = "appid";
            service.TenantId = "tenantid";

            SwitchOfflineService(true, service);

            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Online, service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn(service));
            Assert.IsTrue(CheckLoginCache("foo", null, service));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Username], "foo");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock(service);
            SwitchOfflineService(false, service);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// ユーザ名でログインする（オンラインモード）（オフライン機能無効）（正常）
        /// ユーザ名でログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されないこと
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncSubNormalOnlineOfflineDisabled()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            SwitchOfflineService(false);

            // Main
            var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password");

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsFalse(CheckLoginCache("foo", null));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Username], "foo");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// ユーザ名でログインする（オンラインモード）（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionOnlineFailer()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（正常）
        /// ユーザ名でログインできること
        /// レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncNormalOffline()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Offline);

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);
            Assert.IsTrue(NbUser.IsLoggedIn());

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（サービス指定）（正常）
        /// ユーザ名でログインできること
        /// レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncWithServiceNormalOffline()
        {
            SwitchOfflineService(false);
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;
            service.AppId = "appid";
            service.TenantId = "tenantid";

            SwitchOfflineService(true, service);

            var user = await DoLoginOnlineWithMock(service);
            await DoLogoutWithMock(service);

            // Main
            var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Offline, service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);
            Assert.IsTrue(NbUser.IsLoggedIn(service));

            // after
            await DoLogoutWithMock(service);
            SwitchOfflineService(false, service);
            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（オフライン機能無効）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionOfflineDisabled()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            SwitchOfflineService(false);

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（前回ログインユーザと異なるユーザでのログイン）
        /// UnauthorizedAccessExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(UnauthorizedAccessException))]
        public async void TestLoginWithUsernameAsyncExceptionOfflineCheckLoginUser()
        {
            var user = await DoLoginOnlineWithMock();
            CreateObjcectCache();
            await DoLogoutWithMock();

            // Main
            var result = await NbOfflineUser.LoginWithUsernameAsync("bar", "password", NbUser.LoginMode.Offline);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（ログインキャッシュなし）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionOfflineNoLoginCache()
        {
            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（ログインキャッシュなし、オブジェクトキャッシュあり）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionOfflineNoLoginCacheExistObject()
        {
            CreateObjcectCache();
            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（有効期限切れ）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionOfflineExpired()
        {
            var user = await DoLoginOnlineWithMock(NbService.Singleton, false);
            var session = NbService.Singleton.SessionInfo;
            session.Set("1234567890", 999999999999L, user);
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
            Assert.IsFalse(CheckLoginCache(user.Username, user.Email));
        }

        /// <summary>
        /// ユーザ名でログインする（オフラインモード）（パスワード不正）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionOfflineInvalidPassword()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync("foo", "pass", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// ユーザ名でログインする（ユーザ名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionNoUsername()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync(null, "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (ArgumentNullException)
            {
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// ユーザ名でログインする（パスワードがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithUsernameAsyncExceptionNoPassword()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithUsernameAsync("foo", null, NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (ArgumentNullException)
            {
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }


        /**
         * LoginWithEmailAsync
         **/

        /// <summary>
        /// Email でログインする（オンラインモード）（正常）
        /// Emailでログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncNormalOnline()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password");

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsTrue(CheckLoginCache(null, "foo@example.com"));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Email], "foo@example.com");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// Email でログインする（オートモード。オンライン）（正常）
        /// Emailでログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncNormalAutoOnline()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Auto);

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsTrue(CheckLoginCache(null, "foo@example.com"));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Email], "foo@example.com");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// Email でログインする（オンラインモード）（サービス指定）（正常）
        /// Emailでログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncWithServiceNormalOnline()
        {
            SwitchOfflineService(false);
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;
            service.AppId = "appid";
            service.TenantId = "tenantid";

            SwitchOfflineService(true, service);

            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Online, service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn(service));
            Assert.IsTrue(CheckLoginCache(null, "foo@example.com", service));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Email], "foo@example.com");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock(service);
            SwitchOfflineService(false, service);
            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// Email でログインする（オンラインモード）（オフライン機能無効）
        /// Emailでログインできること
        /// リクエスト、レスポンスの情報が正しいこと
        /// ログインキャッシュが保存されないこと
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncNormalOnlineOfflineDisabled()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            executor.AddResponse(response);

            SwitchOfflineService(false);

            // Main
            var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password");

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, json[Field.Id]);
            Assert.AreEqual(result.Username, json[Field.Username]);
            Assert.AreEqual(result.Email, json[Field.Email]);
            Assert.AreEqual(result.Options, json[Field.Options]);
            Assert.AreEqual(result.Groups, json[Field.Groups]);
            Assert.AreEqual(result.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(result.UpdatedAt, json[Field.UpdatedAt]);
            Assert.IsTrue(NbUser.IsLoggedIn());
            Assert.IsFalse(CheckLoginCache(null, "foo@example.com"));

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/login"));
            Assert.AreEqual(reqJson[Field.Email], "foo@example.com");
            Assert.AreEqual(reqJson[Field.Password], "password");

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// Email でログインする（オンラインモード）（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionOnlineFailer()
        {
            var options = new NbJsonObject() { { "key1", "value1" } };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var json = CreateUserJson(options);
            json[Field.SessionToken] = "1234567890";
            json[Field.Expire] = 999999999999L;
            json[Field.Groups] = groups;
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password");
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }

        /// <summary>
        /// Email でログインする（オフラインモード）（正常）
        /// Emailでログインできること
        /// レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncNormalOffline()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Offline);

            // Check Response
            Assert.AreEqual(result.Service, NbService.Singleton);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);
            Assert.IsTrue(NbUser.IsLoggedIn());

            // after
            await DoLogoutWithMock();
        }

        /// <summary>
        /// Email でログインする（オフラインモード）（サービス指定）（正常）
        /// Emailでログインできること
        /// レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncWithServiceNormalOffline()
        {
            SwitchOfflineService(false);
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;
            service.AppId = "appid";
            service.TenantId = "tenantid";

            SwitchOfflineService(true, service);

            var user = await DoLoginOnlineWithMock(service);
            await DoLogoutWithMock(service);

            // Main
            var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Offline, service);

            // Check Response
            Assert.AreEqual(result.Service, service);
            Assert.AreEqual(result.UserId, user.UserId);
            Assert.AreEqual(result.Username, user.Username);
            Assert.AreEqual(result.Email, user.Email);
            Assert.AreEqual(result.CreatedAt, user.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, user.UpdatedAt);
            Assert.IsTrue(NbUser.IsLoggedIn(service));

            // after
            await DoLogoutWithMock(service);
            SwitchOfflineService(false, service);
            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// Email でログインする（オフラインモード）（オフライン機能無効）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionOfflineDisabled()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            SwitchOfflineService(false);

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Email でログインする（前回ログインユーザと異なるユーザでのログイン）
        /// UnauthorizedAccessExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(UnauthorizedAccessException))]
        public async void TestLoginWithEmailAsyncExceptionOfflineCheckLoginUser()
        {
            var user = await DoLoginOnlineWithMock();
            CreateObjcectCache();
            await DoLogoutWithMock();

            // Main
            var result = await NbOfflineUser.LoginWithEmailAsync("bar", "password", NbUser.LoginMode.Offline);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// Email でログインする（オフラインモード）（ログインキャッシュなし）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionOfflineNoLoginCache()
        {
            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// Email でログインする（オフラインモード）（ログインキャッシュなし、オブジェクトキャッシュあり）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionOfflineNoLoginCacheExistObject()
        {
            CreateObjcectCache();
            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// Email でログインする（オフラインモード）（有効期限切れ）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionOfflineExpired()
        {
            var user = await DoLoginOnlineWithMock(NbService.Singleton, false);
            var session = NbService.Singleton.SessionInfo;
            session.Set("1234567890", 999999999999L, user);
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
            Assert.IsFalse(CheckLoginCache(user.Username, user.Email));
        }

        /// <summary>
        /// Email でログインする（オフラインモード）（パスワード不正）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionOfflineInvalidPassword()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", "pass", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Unauthorized);
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// Email でログインする（Emailがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionNoEmail()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync(null, "password", NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (ArgumentNullException)
            {
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }

        /// <summary>
        /// Email でログインする（パスワードがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestLoginWithEmailAsyncExceptionNoPassword()
        {
            var user = await DoLoginOnlineWithMock();
            await DoLogoutWithMock();

            // Main
            try
            {
                var result = await NbOfflineUser.LoginWithEmailAsync("foo@example.com", null, NbUser.LoginMode.Offline);
                Assert.Fail("No Exception");
            }
            catch (ArgumentNullException)
            {
            }

            Assert.IsFalse(NbUser.IsLoggedIn());
        }
    }
}
