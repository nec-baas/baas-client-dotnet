using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbGroupTest
    {
        private NbGroup group;
        private MockRestExecutor executor;

        private const string appKey = "X-Application-Key";
        private const string appId = "X-Application-Id";
        private const string session = "X-Session-Token";

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();
            group = new NbGroup("group1");
            executor = new MockRestExecutor();
            NbService.Singleton.RestExecutor = executor;
        }

        private NbJsonObject CreateGroupJson()
        {
            var json = new NbJsonObject()
            {
                {"_id", "12345"},
                {"name", "test"},
                {"users", new HashSet<string>(){"user1"}},
                {"groups", new HashSet<string>(){"group1"}},
                {"ACL", new NbAcl()},
                {"createdAt", "CREATEDAT"},
                {"updatedAt", "UPDATEDAT"},
                {"etag", "ETAG"}
            };

            return json;
        }

        [Test]
        public void TestInit()
        {
            Assert.IsEmpty(group.Users);
            Assert.IsEmpty(group.Groups);
            Assert.IsNull(group.Acl);

            var json = group.ToJson().ToString();

            group.Users.Add("u1");
            group.Groups.Add("g1");
            group.Acl = new NbAcl();
            json = group.ToJson().ToString();
        }

        [Test]
        public void TestFromJson()
        {
            var json = new NbJsonObject
            {
                {"_id", "ID"},
                {"name", "GroupName"},
                {"users", new NbJsonArray {"u1"}},
                {"groups", new NbJsonArray {"g1"}},
                {"ACL", new NbAcl().ToJson()},
                {"createdAt", "CREATEDAT"},
                {"updatedAt", "UPDATEDAT"},
                {"etag", "ETAG"}
            };

            group = NbGroup.FromJson(json);

            Assert.AreEqual("ID", group.GroupId);
            Assert.AreEqual("GroupName", group.Name);
            Assert.True(group.Users.Contains("u1"));
            Assert.True(group.Groups.Contains("g1"));
            Assert.AreEqual("CREATEDAT", group.CreatedAt);
            Assert.AreEqual("UPDATEDAT", group.UpdatedAt);
            Assert.AreEqual("ETAG", group.Etag);
        }

        /**
         * Constructor(NbGroup)
         **/

        /// <summary>
        /// コンストラクタテスト（正常）
        /// Serviceが設定されること。
        /// グループ名が設定されること。
        /// Service、グループ名以外のプロパティが初期値であること。
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            // Main
            var test = new NbGroup("test");

            // Assert
            Assert.AreEqual(NbService.Singleton, test.Service);
            Assert.AreEqual(test.Name, "test");
            Assert.IsNull(test.GroupId);
            Assert.IsEmpty(test.Users);
            Assert.IsEmpty(test.Groups);
            Assert.IsNull(test.Acl);
            Assert.IsNull(test.CreatedAt);
            Assert.IsNull(test.UpdatedAt);
            Assert.IsNull(test.Etag);
        }

        /// <summary>
        /// コンストラクタテスト（サービス指定）（正常）
        /// Serviceが設定されること。
        /// グループ名が設定されること。
        /// Service、グループ名以外のプロパティが初期値であること。
        /// </summary>
        [Test]
        public void TestConstructorWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();

            // Main
            var test = new NbGroup("test", service);

            // Assert
            Assert.AreEqual(service, test.Service);
            Assert.AreEqual(test.Name, "test");
            Assert.IsNull(test.GroupId);
            Assert.IsEmpty(test.Users);
            Assert.IsEmpty(test.Groups);
            Assert.IsNull(test.Acl);
            Assert.IsNull(test.CreatedAt);
            Assert.IsNull(test.UpdatedAt);
            Assert.IsNull(test.Etag);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// コンストラクタテスト（グループ名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionNoName()
        {
            // Main
            var test = new NbGroup(null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// コンストラクタテスト（サービス指定）（グループ名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorWithServiceExceptionNoName() 
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();

            // Main
            try
            {
                var test = new NbGroup(null, service);
                Assert.Fail("No Exception");
            }
            finally
            {
                NbService.EnableMultiTenant(false);
            }
        }


        /**
         * SaveAsync
         **/

        /// <summary>
        /// グループ保存テスト（正常）
        /// グループ情報が保存できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormal()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            test.Users.Add("user1");
            test.Groups.Add("group1");
            test.Acl = new NbAcl();

            // Main
            var result = await test.SaveAsync();

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.AreEqual(result.Users, test.Users);
            Assert.AreEqual(result.Groups, test.Groups);
            Assert.AreEqual(result.Acl, test.Acl);
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson["users"], test.Users);
            Assert.AreEqual(reqJson["groups"], test.Groups);
            Assert.AreEqual(reqJson["ACL"], test.Acl.ToJson());
        }

        /// <summary>
        /// グループ保存テスト（正常）
        /// Etag付きでグループ情報が保存できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestSaveAsyncWithEtagNormal()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            test.Users.Add("user1");
            test.Groups.Add("group1");
            test.Acl = new NbAcl();
            test.Etag = "ETAG";

            // Main
            var result = await test.SaveAsync();

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.AreEqual(result.Users, test.Users);
            Assert.AreEqual(result.Groups, test.Groups);
            Assert.AreEqual(result.Acl, test.Acl);
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson["users"], test.Users);
            Assert.AreEqual(reqJson["groups"], test.Groups);
            Assert.AreEqual(reqJson["ACL"], test.Acl.ToJson());
            Assert.AreEqual(req.QueryParams["etag"], test.Etag);
        }

        /// <summary>
        /// グループ保存テスト（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionFailer()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            test.Users.Add("user1");
            test.Groups.Add("group1");
            test.Acl = new NbAcl();

            // Main
            try
            {
                await test.SaveAsync();
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }

            // Check Request
            var req = executor.LastRequest;
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(reqJson["users"], test.Users);
            Assert.AreEqual(reqJson["groups"], test.Groups);
            Assert.AreEqual(reqJson["ACL"], test.Acl.ToJson());
        }


        /**
         * ToJson
         **/

        /// <summary>
        /// JSON Object変換テスト（正常）
        /// JSON Objectに変換後の情報が正しいこと
        /// </summary>
        [Test]
        public void TestToJsonNormal()
        {
            // Main
            var test = new NbGroup("test");
            test.Users.Add("user1");
            test.Groups.Add("group1");
            test.Acl = new NbAcl();

            var json = test.ToJson();

            Assert.AreEqual(json["users"], test.Users);
            Assert.AreEqual(json["groups"], test.Groups);
            Assert.AreEqual(json["ACL"], test.Acl.ToJson());
        }

        /// <summary>
        /// JSON Object変換テスト（正常）
        /// 空のグループ情報をJSON Objectに変換できること
        /// </summary>
        [Test]
        public void TestToJsonNormalAllUnset()
        {
            // Main
            var test = new NbGroup("test");

            var json = test.ToJson();

            Assert.IsEmpty((ISet<string>)json[Field.Users]);
            Assert.IsEmpty((ISet<string>)json[Field.Groups]);
            Assert.IsFalse(json.ContainsKey(Field.Acl));
        }


        /**
         * FromJson
         **/

        /// <summary>
        /// JSON Object から NbGroup へ変換（正常）
        /// NbGroup へ変換後の情報が正しいこと
        /// </summary>
        [Test]
        public void TestFromJsonNormal()
        {
            NbJsonObject json = new NbJsonObject
            {
                {Field.Id, "12345"},
                {Field.Name, "test"},
                {Field.Users, new NbJsonArray {"user1"}},
                {Field.Groups, new NbJsonArray {"group1"}},
                {Field.Acl, new NbAcl().ToJson()},
                {Field.CreatedAt, "CREATEDAT"},
                {Field.UpdatedAt, "UPDATEDAT"},
                {Field.Etag, "ETAG"}
            };

            var group = NbGroup.FromJson(json);

            Assert.AreEqual(group.GroupId, json[Field.Id]);
            Assert.AreEqual(group.Name, json[Field.Name]);
            Assert.AreEqual(group.Users, json[Field.Users]);
            Assert.AreEqual(group.Groups, json[Field.Groups]);
            Assert.AreEqual(group.Acl.ToString(), new NbAcl(json.GetJsonObject(Field.Acl)).ToString());
            Assert.AreEqual(group.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(group.UpdatedAt, json[Field.UpdatedAt]);
            Assert.AreEqual(group.Etag, json[Field.Etag]);
        }

        /// <summary>
        /// JSON Object から NbGroup へ変換（サービス指定）（正常）
        /// NbGroupへ変換後の情報が正しいこと
        /// </summary>
        [Test]
        public void TestFromJsonWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();

            NbJsonObject json = new NbJsonObject
            {
                {Field.Id, "12345"},
                {Field.Name, "test"},
                {Field.Users, new NbJsonArray {"user1"}},
                {Field.Groups, new NbJsonArray {"group1"}},
                {Field.Acl, new NbAcl().ToJson()},
                {Field.CreatedAt, "CREATEDAT"},
                {Field.UpdatedAt, "UPDATEDAT"},
                {Field.Etag, "ETAG"}
            };

            var group = NbGroup.FromJson(json, service);

            Assert.AreEqual(group.GroupId, json[Field.Id]);
            Assert.AreEqual(group.Name, json[Field.Name]);
            Assert.AreEqual(group.Users, json[Field.Users]);
            Assert.AreEqual(group.Groups, json[Field.Groups]);
            Assert.AreEqual(group.Acl.ToString(), new NbAcl(json.GetJsonObject(Field.Acl)).ToString());
            Assert.AreEqual(group.CreatedAt, json[Field.CreatedAt]);
            Assert.AreEqual(group.UpdatedAt, json[Field.UpdatedAt]);
            Assert.AreEqual(group.Etag, json[Field.Etag]);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// JSON Object から NbGroup へ変換（空の（必要なKeyを含まない）jsonオブジェクト）
        /// KeyNotFoundExceptionが発行される。
        /// </summary>
        [Test, ExpectedException(typeof(KeyNotFoundException))]
        public void TestFromJsonExceptionAllUnset()
        {
            NbJsonObject json = new NbJsonObject();

            var group = NbGroup.FromJson(json);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// JSON Object から NbGroup へ変換（Jsonがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestFromJsonExceptionNoJson()
        {
            var group = NbGroup.FromJson(null);
            Assert.Fail("No Exception");
        }


        /**
         * QueryGroupsAsync
         **/

        /// <summary>
        /// グループ一覧を取得する（正常）
        /// グループ一覧を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryGroupsAsyncNormal()
        {
            // Set Dummy Response
            var results = new NbJsonArray();
            results.Add(CreateGroupJson());
            var responseJson = new NbJsonObject()
            {
                {Field.Results, results}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, responseJson.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbGroup.QueryGroupsAsync();

            // Check Response
            foreach (var grp in result)
            {
                Assert.AreEqual(grp.GroupId, "12345");
                Assert.AreEqual(grp.Name, "test");
                Assert.IsTrue(grp.Users.Contains("user1"));
                Assert.IsTrue(grp.Groups.Contains("group1"));
                Assert.AreEqual(grp.Acl.ToString(), new NbAcl().ToString());
                Assert.AreEqual(grp.CreatedAt, "CREATEDAT");
                Assert.AreEqual(grp.UpdatedAt, "UPDATEDAT");
                Assert.AreEqual(grp.Etag, "ETAG");
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
        }

        /// <summary>
        /// グループ一覧を取得する（サービス指定）（正常）
        /// グループ一覧を取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryGroupsAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            // Set Dummy Response
            var results = new NbJsonArray();
            results.Add(CreateGroupJson());
            var responseJson = new NbJsonObject()
            {
                {Field.Results, results}
            };
            var response = new MockRestResponse(HttpStatusCode.OK, responseJson.ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbGroup.QueryGroupsAsync(service);

            // Check Response
            foreach (var grp in result)
            {
                Assert.AreEqual(grp.GroupId, "12345");
                Assert.AreEqual(grp.Name, "test");
                Assert.IsTrue(grp.Users.Contains("user1"));
                Assert.IsTrue(grp.Groups.Contains("group1"));
                Assert.AreEqual(grp.Acl.ToString(), new NbAcl().ToString());
                Assert.AreEqual(grp.CreatedAt, "CREATEDAT");
                Assert.AreEqual(grp.UpdatedAt, "UPDATEDAT");
                Assert.AreEqual(grp.Etag, "ETAG");
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// グループ一覧を取得する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestQueryGroupsAsyncExceptionFailer()
        {
            // Set Dummy Response
            var results = new NbJsonArray();
            results.Add(CreateGroupJson());
            var responseJson = new NbJsonObject()
            {
                {Field.Results, results}
            };
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var result = await NbGroup.QueryGroupsAsync();
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }
        }


        /**
         * GetGroupAsync
         **/

        /// <summary>
        /// グループを取得する（正常）
        /// 指定したグループ情報が取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestGetGroupAsyncNormal()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbGroup.GetGroupAsync("test");

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.IsTrue(result.Users.Contains("user1"));
            Assert.IsTrue(result.Groups.Contains("group1"));
            Assert.AreEqual(result.Acl.ToString(), new NbAcl().ToString());
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
        }

        /// <summary>
        /// グループを取得する（サービス指定）（正常）
        /// 指定したグループ情報が取得できること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestGetGroupAsyncWithServiceNormal()
        {
            NbService.EnableMultiTenant(true);
            var service = NbService.GetInstance();
            service.RestExecutor = executor;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            var result = await NbGroup.GetGroupAsync("test", service);

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.IsTrue(result.Users.Contains("user1"));
            Assert.IsTrue(result.Groups.Contains("group1"));
            Assert.AreEqual(result.Acl.ToString(), new NbAcl().ToString());
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Get, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// グループを取得する（グループ名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestGetGroupAsyncExceptionNoGroupName()
        {
            // Main
            var result = await NbGroup.GetGroupAsync(null);
            Assert.Fail("No Exception");
        }

        /// <summary>
        /// グループを取得する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestGetGroupAsyncExceptionFailer()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var result = await NbGroup.GetGroupAsync("test");
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
        /// グループを削除する（正常）
        /// グループの削除ができること
        /// リクエストの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormal()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            await test.DeleteAsync();

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Delete, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
        }

        /// <summary>
        /// グループを削除する（正常）
        /// Etag付きでグループの削除ができること
        /// リクエストの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestDeleteAsyncWithEtagNormal()
        {
            var test = new NbGroup("test");
            test.Etag = "ETAG";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            await test.DeleteAsync();

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Delete, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(req.QueryParams["etag"], test.Etag);
        }

        /// <summary>
        /// グループを削除する（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionFailer()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                await test.DeleteAsync();
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Delete, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
        }


        /**
         * AddMembersAsync
         **/

        /// <summary>
        /// グループメンバ追加（正常）
        /// グループメンバの追加ができること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestAddMembersAsyncNormal()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            var users = new List<string>(){"u1", "u2", "u3"};
            var groups = new List<string>(){"g1", "g2", "g3"};
            var result = await test.AddMembersAsync(users, groups);

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.IsTrue(result.Users.Contains("user1"));
            Assert.IsTrue(result.Groups.Contains("group1"));
            Assert.AreEqual(result.Acl.ToString(), new NbAcl().ToString());
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test/addMembers"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(reqJson["users"], users);
            Assert.AreEqual(reqJson["groups"], groups);
        }

        /// <summary>
        /// グループメンバ追加（設定情報がすべてnull）
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestAddMembersAsyncNormalAllNull()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            var result = await test.AddMembersAsync(null, null);

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.IsTrue(result.Users.Contains("user1"));
            Assert.IsTrue(result.Groups.Contains("group1"));
            Assert.AreEqual(result.Acl.ToString(), new NbAcl().ToString());
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test/addMembers"));
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.IsFalse(reqJson.ContainsKey("users"));
            Assert.IsFalse(reqJson.ContainsKey("groups"));
        }

        /// <summary>
        /// グループメンバ追加（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestAddMembersAsyncExceptionFailer()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var users = new List<string>() { "u1", "u2", "u3" };
                var groups = new List<string>() { "g1", "g2", "g3" };
                var result = await test.AddMembersAsync(users, groups);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }

        /// <summary>
        /// グループメンバ削除（正常）
        /// グループメンバの削除ができること
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestDeleteMembersAsyncNormal()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            var users = new List<string>() { "u1", "u2", "u3" };
            var groups = new List<string>() { "g1", "g2", "g3" };
            var result = await test.DeleteMembersAsync(users, groups);

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.IsTrue(result.Users.Contains("user1"));
            Assert.IsTrue(result.Groups.Contains("group1"));
            Assert.AreEqual(result.Acl.ToString(), new NbAcl().ToString());
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test/removeMembers"));
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(reqJson["users"], users);
            Assert.AreEqual(reqJson["groups"], groups);
        }

        // <summary>
        /// グループメンバ削除（設定情報がすべてnull）
        /// リクエスト、レスポンスの情報が正しいこと
        /// </summary>
        [Test]
        public async void TestDeleteMembersAsyncNormalAllNull()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGroupJson().ToString());
            executor.AddResponse(response);

            // Main
            var result = await test.DeleteMembersAsync(null, null);

            // Check Response
            Assert.AreEqual(result.GroupId, "12345");
            Assert.AreEqual(result.Name, "test");
            Assert.IsTrue(result.Users.Contains("user1"));
            Assert.IsTrue(result.Groups.Contains("group1"));
            Assert.AreEqual(result.Acl.ToString(), new NbAcl().ToString());
            Assert.AreEqual(result.CreatedAt, "CREATEDAT");
            Assert.AreEqual(result.UpdatedAt, "UPDATEDAT");
            Assert.AreEqual(result.Etag, "ETAG");

            // Check Request
            var req = executor.LastRequest;
            Assert.AreEqual(HttpMethod.Put, req.Method);
            Assert.IsTrue(req.Uri.EndsWith("/groups/test/removeMembers"));
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.IsFalse(reqJson.ContainsKey("users"));
            Assert.IsFalse(reqJson.ContainsKey("groups"));
        }

        // <summary>
        /// グループメンバ削除（異常）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDeleteMembersAsyncExceptionFailer()
        {
            var test = new NbGroup("test");

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.Forbidden);
            executor.AddResponse(response);

            // Main
            try
            {
                var users = new List<string>() { "u1", "u2", "u3" };
                var groups = new List<string>() { "g1", "g2", "g3" };
                var result = await test.DeleteMembersAsync(users, groups);
                Assert.Fail("No Exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(e.StatusCode, HttpStatusCode.Forbidden);
            }
        }
    }
}
