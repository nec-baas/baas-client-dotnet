using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

using NUnit.Framework;
using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace Nec.Nebula.IT
{
    [TestFixture]
    public class NbPushIT
    {
        private static string Channel1 = "dotnet_it_channel1";
        private static string Channel2 = "dotnet_it_channel2";

        private static string Password = "p@ssw0rd";

        private NbUser User1;
        private NbUser User2;
        private NbGroup Group1;

        /// <summary>
        /// NbPushIT 開始時に１度だけ実行
        /// </summary>
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Debug.WriteLine("TestFixtureSetUp()");

            ITUtil.InitNebula();
            ITUtil.UseMasterKey();

            // データ削除
            DeleteAllInstallations();
            ITUtil.InitOnlineGroup().Wait();
            ITUtil.InitOnlineUser().Wait();

            // テストユーザ・グループ登録
            RegisterTestUsersAndGroups();

            // テスト用ダミーインスタレーション登録
            RegisterTestInstallations();

            ITUtil.UseNormalKey();
            Debug.WriteLine("TestFixtureSetUp() end");
        }

        /// <summary>
        /// NbPushIT 終了処理
        /// </summary>
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            ITUtil.UseMasterKey();

            DeleteAllInstallations();         // インスタレーション削除
            ITUtil.InitOnlineGroup().Wait();  // グループ削除
            ITUtil.InitOnlineUser().Wait();   // ユーザ削除

            ITUtil.UseNormalKey();
        }


        /// <summary>
        /// 各テストの前処理
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Debug.WriteLine("SetUp()");
            ITUtil.InitNebula();
        }

        /// <summary>
        /// 各テストの後処理
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Debug.WriteLine("TearDown()");
        }

        /// <summary>
        /// インスタレーション登録
        /// </summary>
        /// <param name="installationJson">登録するinstallation情報</param>
        /// <returns>登録したインスタレーション情報</returns>
        public async Task<NbJsonObject> RegisterInstallationAsync(NbJsonObject installationJson)
        {
            var req = NbService.Singleton.RestExecutor.CreateRequest("/push/installations", HttpMethod.Post);
            req.SetJsonBody(installationJson);
            return await NbService.Singleton.RestExecutor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// インスタレーション一覧取得
        /// </summary>
        /// <returns>インスタレーション一覧</returns>
        public async Task<NbJsonObject> GetAllInstallationsAsync()
        {
            var req = NbService.Singleton.RestExecutor.CreateRequest("/push/installations", HttpMethod.Get);
            return await NbService.Singleton.RestExecutor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// インスタレーション削除
        /// </summary>
        public async Task<NbJsonObject> DeleteInstallationAsync(string id)
        {
            var req = NbService.Singleton.RestExecutor.CreateRequest("/push/installations/{id}", HttpMethod.Delete);
            req.SetUrlSegment("id", id);
            return await NbService.Singleton.RestExecutor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// インスタレーション全削除
        /// </summary>
        private void DeleteAllInstallations()
        {
            Debug.WriteLine("DeleteAllInstallations() start");
            var task = GetAllInstallationsAsync();
            task.Wait();
            var response = task.Result;

            var list = response.Get<NbJsonArray>("results");
            Debug.WriteLine("num of installations: " + list.Count);
            foreach (NbJsonObject installation in list)
            {
                DeleteInstallationAsync(installation.Get<string>("_id")).Wait();
            }
            Debug.WriteLine("DeleteAllInstallations() end");
        }

        private void RegisterTestUsersAndGroups()
        {
            var user = new NbUser();
            user.Username = "dotnet_pushIT_user1";
            user.Email = "user1@push.tet.com";
            var task = user.SignUpAsync(Password);
            task.Wait();
            User1 = task.Result;

            user = new NbUser();
            user.Username = "dotnet_pushIT_user2";
            user.Email = "user2@push.tet.com";
            task = user.SignUpAsync(Password);
            task.Wait();
            User2 = task.Result;

            Group1 = new NbGroup("dotnet_pushIT_group1");
            Group1.Users = new HashSet<string>() { User1.UserId };
            Group1.SaveAsync().Wait();
        }

        private void RegisterTestInstallations()
        {
            var inst1 = new NbJsonObject()
            {
                { "_osType", "java"},
                { "_osVersion", "Unknown"},
                { "_deviceToken", "xxxxxxxxxxxxxxxxxxxx"},
                { "_pushType", "sse"},
                { "_channels", new NbJsonArray() {Channel1} },
                { "_appVersionCode", 1},
                { "_appVersionString", "1.0"},
                { "_allowedSenders", new NbJsonArray() {"g:anonymous"} },
                { "description", "Dummy installation of PushIT. (User1)"}
            };

            var inst2 = new NbJsonObject()
            {
                { "_osType", "dotnet"},
                { "_osVersion", "Unknown"},
                { "_deviceToken", "yyyyyyyyyyyyyyyyyyyyyy"},
                { "_pushType", "sse"},
                { "_channels", new NbJsonArray() {Channel2} },
                { "_appVersionCode", 1},
                { "_appVersionString", "1.0"},
                { "_allowedSenders", new NbJsonArray() {"g:anonymous"} },
                { "description", "Dummy installation of PushIT. (User2)"}
            };

            var inst3 = new NbJsonObject()
            {
                { "_osType", "android"},
                { "_osVersion", "6.0"},
                { "_deviceToken", "zzzzzzzzzzzzzzzzzzzzzz"},
                { "_pushType", "gcm"},
                { "_channels", new NbJsonArray() {Channel1, Channel2} },
                { "_appVersionCode", 1},
                { "_appVersionString", "1.0"},
                { "_allowedSenders", new NbJsonArray() {"g:anonymous"} },
                { "description", "Dummy installation of PushIT. (no owner)"}
            };

            // ※TestFixtureSetUp では async/await は非対応な模様

            // User1(Group1所属)で登録
            NbUser.LoginWithUsernameAsync(User1.Username, Password).Wait();
            RegisterInstallationAsync(inst1).Wait();

            // User2で登録
            NbUser.LoginWithUsernameAsync(User2.Username, Password).Wait();
            RegisterInstallationAsync(inst2).Wait();

            // anonymousで登録
            NbUser.LogoutAsync().Wait();
            RegisterInstallationAsync(inst3).Wait();
        }


        /// <summary>
        /// Push送信：オプションなしで送信リクエストできること（受信確認なし）
        /// </summary>
        [Test]
        public async void TestSendWithoutOptions()
        {
            var push = new NbPush();
            push.Query = new NbQuery();
            push.Message = "test message from .NET SDK PushIT";

            var response = await push.SendAsync();

            Assert.AreEqual(3, response.Get<int>("installations"));

            Debug.WriteLine("Test1() end");
        }

        /// <summary>
        /// Push送信：全オプション指定で送信リクエストできること（受信確認なし）
        /// </summary>
        [Test]
        public async void TestSendWithAllOptions()
        {
            var push = new NbPush();
            push.Query = new NbQuery();
            push.Message = "test message from .NET SDK PushIT";

            push.AllowedReceivers = new HashSet<string>() { User1.UserId, User2.UserId, "g:" + Group1.Name };

            var apns = new NbApnsFields();
            apns.Badge = 2;
            apns.Sound = "sound1.aiff";
            apns.ContentAvailable = 1;
            apns.Category = "MESSAGE_CATEGORY";
            push.ApnsFields = apns;

            var gcm = new NbGcmFields();
            gcm.Title = "testTitle";
            gcm.Uri = "http://www.pushtest.test.com";
            push.GcmFields = gcm;

            var sse = new NbSseFields();
            sse.EventId = "testId";
            sse.EventType = "testType";
            push.SseFields = sse;

            var response = await push.SendAsync();

            // 3つのうち ownerがない inst3 のみ除外される
            Assert.AreEqual(2, response.Get<int>("installations"));
        }

        /// <summary>
        /// Push送信：クエリ指定(チャネル)で送信リクエストできること（受信確認なし）
        /// </summary>
        [Test]
        public async void TestSendToChannelWithQuery()
        {
            var push = new NbPush();
            push.Query = new NbQuery().EqualTo("_channels", Channel1);
            push.Message = "test message from .NET SDK PushIT";

            var response = await push.SendAsync();

            Assert.AreEqual(2, response.Get<int>("installations"));
        }

        /// <summary>
        /// allowedReceivers へユーザ指定で送信対象を制限できること
        /// </summary>
        [Test]
        public async void TestSendToUserUsingAllowedReceivers()
        {
            var push = new NbPush();
            push.Query = new NbQuery(); // クエリ：制限なし
            push.Message = "test message from .NET SDK PushIT";

            push.AllowedReceivers = new HashSet<string>() { User1.UserId, User2.UserId };

            var response = await push.SendAsync();
            Assert.AreEqual(2, response.Get<int>("installations"));
        }

        /// <summary>
        /// allowedReceivers へグループ指定で送信対象を制限できること
        /// </summary>
        [Test]
        public async void TestSendToGroupUsingAllowedReceivers()
        {
            var push = new NbPush();
            push.Query = new NbQuery(); // クエリ：制限なし
            push.Message = "test message from .NET SDK PushIT";

            push.AllowedReceivers = new HashSet<string>() { { "g:" + Group1.Name } };

            var response = await push.SendAsync();
            Assert.AreEqual(1, response.Get<int>("installations"));
        }

        /// <summary>
        /// Push送信：クライアントPush禁止の場合、通常権限では403エラーとなること
        /// </summary>
        [Test]
        public async void TestSendErrorForbidden()
        {
            // クライアントPush禁止アプリで試験
            ITUtil.UseAppIDKey(1, true);

            var push = new NbPush();
            push.Query = new NbQuery();
            push.Message = "test message from .NET SDK PushIT";

            try
            {
                var response = await push.SendAsync();
                Assert.Fail("no error occurred");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }
        }

        /// <summary>
        /// Push送信：クライアントPush禁止の場合でもMaster権限であれば送信できること
        /// </summary>
        [Test]
        public async void TestSendWithMasterKey()
        {
            // クライアントPush禁止アプリで試験
            ITUtil.UseAppIDKey(1, false);

            var push = new NbPush();
            push.Query = new NbQuery();
            push.Message = "test message from .NET SDK PushIT";

            var response = await push.SendAsync();
            Assert.AreEqual(0, response.Get<int>("installations"));
        }

        /// <summary>
        /// Push送信：サーバからのエラーを通知できること(400エラー)
        /// </summary>
        [Test]
        public async void TestSendErrorBadRequest()
        {
            var push = new NbPush();
            push.Query = new NbQuery();
            push.Message = "test message from .NET SDK PushIT";
            push.AllowedReceivers = new HashSet<string>() { "g:anonymous" };

            try
            {
                var response = await push.SendAsync();
                Assert.Fail("no error occurred");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }
        }

        /// <summary>
        /// Push送信：Queryが設定されていない場合はInvalidOperationExceptionとなること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestSendErrorNoQuery()
        {
            ITUtil.UseMasterKey();

            var push = new NbPush();
            push.Message = "test message from .NET SDK PushIT";

            var response = await push.SendAsync();
        }

        /// <summary>
        /// Push送信：Queryが設定されていない場合はInvalidOperationExceptionとなること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestSendErrorNoMessage()
        {
            ITUtil.UseMasterKey();

            var push = new NbPush();
            push.Query = new NbQuery();

            var response = await push.SendAsync();
        }
    }
}
