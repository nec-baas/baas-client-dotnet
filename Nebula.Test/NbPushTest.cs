using Moq;
using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbPushTest
    {
        private MockRestExecutor executor;

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();

            // inject Mock RestExecutor
            executor = new MockRestExecutor();
            NbService.Singleton.RestExecutor = executor;
        }

        private NbJsonObject CreateResponseJson(bool isSuccess)
        {
            var json = new NbJsonObject();

            if (isSuccess)
            {
                json["result"] = "ok";
                json["installations"] = 17;
            }
            else
            {
                json["error"] = "error messages...";
            }

            return json;
        }

        /// <summary>
        /// コンストラクタテスト。
        /// Serviceに正しい値が設定されること。
        /// Service以外のプロパティの初期値がnullであること。
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            // Main
            var push = new NbPush();

            // Assert
            Assert.AreEqual(NbService.Singleton, push.Service);
            Assert.IsNull(push.Query);
            Assert.IsNull(push.Message);
            Assert.IsNull(push.AllowedReceivers);
            Assert.IsNull(push.ApnsFields);
            Assert.IsNull(push.GcmFields);
            Assert.IsNull(push.SseFields);
        }

        /// <summary>
        /// コンストラクタテスト。
        /// Serviceに正しい値が設定されること。
        /// Service以外のプロパティの初期値がnullであること。
        /// </summary>
        [Test]
        public void TestConstructorWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();

            // Main
            var push = new NbPush(service);

            // Assert
            Assert.AreEqual(service, push.Service);
            Assert.IsNull(push.Query);
            Assert.IsNull(push.Message);
            Assert.IsNull(push.AllowedReceivers);
            Assert.IsNull(push.ApnsFields);
            Assert.IsNull(push.GcmFields);
            Assert.IsNull(push.SseFields);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// SendAsyncテスト。全パラメータ指定
        /// 設定しているメソッド、パス、リクエストボディが正しいこと。
        /// </summary>
        [Test]
        public async void TestSendAsyncFullOptions()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateResponseJson(true).ToString());
            executor.AddResponse(response);

            var push = new NbPush();
            push.Query = new NbQuery().Exists("NbPushTestKey");
            push.Message = "testMessage";

            var allowedReceivers = new HashSet<string>();
            allowedReceivers.Add("testUser");
            allowedReceivers.Add("g:group1");
            push.AllowedReceivers = allowedReceivers;

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

            // Main
            var result = await push.SendAsync();

            // Check Response
            Assert.AreEqual("ok", result["result"]);
            Assert.AreEqual(17, result["installations"]);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/push/notifications"));

            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            var queryJson = reqJson[Field.Query] as NbJsonObject;
            Assert.AreEqual(true, queryJson.GetJsonObject("NbPushTestKey")["$exists"]);
            Assert.AreEqual("testMessage", reqJson[Field.Message]);
            Assert.AreEqual(new NbJsonArray(allowedReceivers), reqJson[Field.AllowedReceivers]);

            Assert.AreEqual(2, reqJson[Field.Badge]);
            Assert.AreEqual("sound1.aiff", reqJson[Field.Sound]);
            Assert.AreEqual(1, reqJson[Field.ContentAvailable]);
            Assert.AreEqual("MESSAGE_CATEGORY", reqJson[Field.Category]);

            Assert.AreEqual("testTitle", reqJson[Field.Title]);
            Assert.AreEqual("http://www.pushtest.test.com", reqJson[Field.Uri]);

            Assert.AreEqual("testId", reqJson[Field.SseEventId]);
            Assert.AreEqual("testType", reqJson[Field.SseEventType]);
        }

        /// <summary>
        /// SendAsyncテスト
        /// オプション無しの場合、リクエストに含まれないこと
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// </summary>
        [Test]
        public async void TestSendAsyncWithoutOptions()
        {
            var push = new NbPush();

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateResponseJson(true).ToString());
            executor.AddResponse(response);

            push.Query = new NbQuery().Exists("NbPushTestKey");

            push.Message = "testMessage";

            // Main
            var result = await push.SendAsync();

            // Check Response
            Assert.AreEqual("ok", result["result"]);
            Assert.AreEqual(17, result["installations"]);

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/push/notifications"));
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            var queryJson = reqJson[Field.Query] as NbJsonObject;
            Assert.AreEqual(true, queryJson.GetJsonObject("NbPushTestKey")["$exists"]);

            Assert.IsFalse(reqJson.ContainsKey(Field.AllowedReceivers));

            Assert.IsFalse(reqJson.ContainsKey(Field.Badge));
            Assert.IsFalse(reqJson.ContainsKey(Field.Sound));
            Assert.IsFalse(reqJson.ContainsKey(Field.ContentAvailable));
            Assert.IsFalse(reqJson.ContainsKey(Field.Category));

            Assert.IsFalse(reqJson.ContainsKey(Field.Title));
            Assert.IsFalse(reqJson.ContainsKey(Field.Uri));

            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventId));
            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventType));
        }

        /// <summary>
        /// SendAsyncテスト
        /// nullを指定した値はリクエストボディに含まれないこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNullValue()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateResponseJson(true).ToString());
            executor.AddResponse(response);

            var push = new NbPush();
            push.Query = new NbQuery();
            push.Message = "testMessage";

            push.AllowedReceivers = null;

            var apns = new NbApnsFields();
            apns.Badge = 1;
            apns.Sound = null;
            apns.ContentAvailable = null;
            apns.Category = null;
            push.ApnsFields = apns;

            var gcm = new NbGcmFields();
            gcm.Title = null;
            gcm.Uri = null;
            push.GcmFields = gcm;

            var sse = new NbSseFields();
            sse.EventId = "testId";
            sse.EventType = null;
            push.SseFields = sse;

            // Main
            await push.SendAsync();

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            var queryJson = reqJson[Field.Query] as NbJsonObject;
            Assert.AreEqual(new NbJsonObject(), queryJson);
            Assert.AreEqual("testMessage", reqJson[Field.Message]);

            Assert.IsFalse(reqJson.ContainsKey(Field.AllowedReceivers));

            Assert.AreEqual(1, reqJson[Field.Badge]);
            Assert.IsFalse(reqJson.ContainsKey(Field.Sound));
            Assert.IsFalse(reqJson.ContainsKey(Field.ContentAvailable));
            Assert.IsFalse(reqJson.ContainsKey(Field.Category));

            Assert.IsFalse(reqJson.ContainsKey(Field.Title));
            Assert.IsFalse(reqJson.ContainsKey(Field.Uri));

            Assert.AreEqual("testId", reqJson[Field.SseEventId]);
            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventType));
        }

        /// <summary>
        /// SendAsyncテスト
        /// 固有値がnullの場合はリクエストボディに含まれないこと
        /// </summary>
        [Test]
        public async void TestSendAsyncNullFields()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateResponseJson(true).ToString());
            executor.AddResponse(response);

            var push = new NbPush();
            push.Query = new NbQuery().EqualTo("_channels", "channel1");
            push.Message = "testMessage";

            var allowedReceivers = new HashSet<string>();
            allowedReceivers.Add("testUser");
            allowedReceivers.Add("g:group1");
            allowedReceivers.Add("g:group2");
            push.AllowedReceivers = allowedReceivers;

            push.ApnsFields = null;

            push.GcmFields = null;

            var sse = new NbSseFields();
            sse.EventId = null;
            sse.EventType = "testSseType";
            push.SseFields = sse;

            // Main
            await push.SendAsync();

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            var queryJson = reqJson[Field.Query] as NbJsonObject;
            Assert.AreEqual("channel1", queryJson["_channels"]);
            Assert.AreEqual("testMessage", reqJson[Field.Message]);

            Assert.AreEqual(allowedReceivers, reqJson[Field.AllowedReceivers]);

            Assert.IsFalse(reqJson.ContainsKey(Field.Badge));
            Assert.IsFalse(reqJson.ContainsKey(Field.Sound));
            Assert.IsFalse(reqJson.ContainsKey(Field.ContentAvailable));
            Assert.IsFalse(reqJson.ContainsKey(Field.Category));

            Assert.IsFalse(reqJson.ContainsKey(Field.Title));
            Assert.IsFalse(reqJson.ContainsKey(Field.Uri));

            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventId));
            Assert.AreEqual("testSseType", reqJson[Field.SseEventType]);
        }

        /// <summary>
        /// SendAsyncテスト
        /// 未設定の値はリクエストに含まれないこと
        /// </summary>
        [Test]
        public async void TestSendAsyncUnset()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateResponseJson(true).ToString());
            executor.AddResponse(response);

            var push = new NbPush();
            push.Query = new NbQuery().In("_channels", new string[] { "channel1", "channel2" });
            push.Message = "testMessage";

            var gcm = new NbGcmFields();
            gcm.Title = "testTitle";
            gcm.Uri = null;
            push.GcmFields = gcm;

            // Main
            await push.SendAsync();

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            var queryJson = reqJson[Field.Query] as NbJsonObject;
            Assert.AreEqual(NbJsonObject.Parse("{'_channels':{'$in':['channel1','channel2']}}"), queryJson);
            Assert.AreEqual("testMessage", reqJson[Field.Message]);

            Assert.IsFalse(reqJson.ContainsKey(Field.AllowedReceivers));

            Assert.IsFalse(reqJson.ContainsKey(Field.Badge));
            Assert.IsFalse(reqJson.ContainsKey(Field.Sound));
            Assert.IsFalse(reqJson.ContainsKey(Field.ContentAvailable));
            Assert.IsFalse(reqJson.ContainsKey(Field.Category));

            Assert.AreEqual("testTitle", reqJson[Field.Title]);
            Assert.IsFalse(reqJson.ContainsKey(Field.Uri));

            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventId));
            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventType));
        }

        /// <summary>
        /// SendAsyncテスト。
        /// サーバのレスポンスが200OK以外の場合にNbHttpExceptionがスローされること。
        /// </summary>
        [Test]
        public async void TestSendAsyncExceptionFailure()
        {
            var push = new NbPush();

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.InternalServerError, CreateResponseJson(false).ToString());
            executor.AddResponse(response);

            push.Query = new NbQuery().Exists("NbPushTestKey");
            push.Message = "testMessage";
            var gcm = new NbGcmFields();
            gcm.Title = "testTitle";
            gcm.Uri = "http://www.pushtest.test.com";
            push.GcmFields = gcm;

            // Main
            try
            {
                await push.SendAsync();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.InternalServerError, e.StatusCode);
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/push/notifications"));
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            var queryJson = reqJson[Field.Query] as NbJsonObject;
            Assert.AreEqual(true, queryJson.GetJsonObject("NbPushTestKey")["$exists"]);
            Assert.AreEqual("testMessage", reqJson[Field.Message]);
            Assert.IsFalse(reqJson.ContainsKey(Field.AllowedReceivers));
            Assert.IsFalse(reqJson.ContainsKey(Field.Badge));
            Assert.IsFalse(reqJson.ContainsKey(Field.Sound));
            Assert.IsFalse(reqJson.ContainsKey(Field.ContentAvailable));
            Assert.IsFalse(reqJson.ContainsKey(Field.Category));
            Assert.AreEqual("testTitle", reqJson[Field.Title]);
            Assert.AreEqual("http://www.pushtest.test.com", reqJson[Field.Uri]);
            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventId));
            Assert.IsFalse(reqJson.ContainsKey(Field.SseEventType));
        }

        /// <summary>
        /// SendAsyncテスト。
        /// Queryがnullの時、InvalidOperationExceptionがスローされること。
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async void TestSendAsyncExceptionNoQuery()
        {
            var push = new NbPush();
            push.Message = "testMessage";

            // Main
            await push.SendAsync();
        }

        /// <summary>
        /// SendAsyncテスト。
        /// Messageがnullの時、InvalidOperationExceptionがスローされること。
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async void TestSendAsyncExceptionNoMessage()
        {
            var push = new NbPush();
            push.Query = new NbQuery();

            // Main
            await push.SendAsync();
        }
    }
}

