using Moq;
using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbOfflineObjectBucketTest
    {
        private NbOfflineObjectBucket<NbOfflineObject> _bucket;
        private ProcessState _processState;

        [SetUp]
        public void SetUp()
        {
            NbService.DisposeSingleton();
            TestUtils.Init();
            SetOfflineService(true);

            _bucket = new NbOfflineObjectBucket<NbOfflineObject>("test");
            _processState = ProcessState.GetInstance();
        }

        private void SetMockObjectCache(NbOfflineObjectBucket<NbOfflineObject> bucket, NbObjectCache cache)
        {
            FieldInfo objectCache = bucket.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            objectCache.SetValue(bucket, cache);
        }

        private void SetMockSessionInfo(NbService service, NbSessionInfo mockSI = null)
        {
            if (mockSI == null)
            {
                var user = new NbUser();
                user.UserId = "testUser";
                var expire = NbUtil.CurrentUnixTime() + 10000;
                mockSI = new NbSessionInfo();
                mockSI.Set("SessionToken", expire, user);
            }

            PropertyInfo propertyInfo = service.GetType().GetProperty("SessionInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            propertyInfo.SetValue(service, mockSI);
        }

        private void SetOfflineService(bool enable)
        {
            if (enable)
            {
                NbOfflineService.SetInMemoryMode(true);
                NbOfflineService.EnableOfflineService(NbService.Singleton);
            }
            else
            {
                NbService.Singleton.DisableOffline();
            }
        }

        private NbQuery CreateTestQuery(NbJsonObject conditions)
        {
            var query = new NbQuery();
            query.DeleteMarkValue = true;
            query.LimitValue = 100;
            query.SkipValue = 50;
            query.Conditions = conditions;

            return query;
        }

        private NbOfflineObject CreateTestObject()
        {
            var obj = new NbOfflineObject(_bucket.BucketName);
            obj.Id = "TestId";
            obj.Acl = NbAcl.CreateAclForAuthenticated();
            obj.Etag = "cacheETag";
            return obj;
        }

        /**
        * コンストラクタ
        **/
        /// <summary>
        /// コンストラクタ（NbOfflineObject、サービス指定あり）
        /// サービス、バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var s1 = NbService.Singleton;
            NbService.EnableMultiTenant(true);
            
            var s2 = NbService.GetInstance();
            s2.TenantId = "s2_tenantid";
            s2.AppId = "s2_appid";
            s2.AppKey = "s2_appkey"; ;
            s2.EndpointUrl = "s2_epurl";

            NbOfflineService.EnableOfflineService(s2);
            Assert.AreNotSame(s1, s2);

            var bucket = new NbOfflineObjectBucket<NbOfflineObject>("test", s2);

            Assert.AreEqual("test", bucket.BucketName);
            Assert.AreEqual(s2, bucket.Service);
            FieldInfo objectCache = bucket.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(bucket));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// コンストラクタ（NbOfflineObject、サービス指定なし）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceUnset()
        {
            Assert.AreEqual("test", _bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, _bucket.Service);
            FieldInfo objectCache = _bucket.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(_bucket));
        }

        /// <summary>
        /// コンストラクタ（NbOfflineObject、serviceがnull）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceNull()
        {
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>("test", null);

            Assert.AreEqual("test", bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, bucket.Service);
            FieldInfo objectCache = bucket.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(bucket));
        }

        private class SubObject : NbOfflineObject
        {
            public SubObject()
                : base("subTest")
            {
            }
        }

        private class SubNoBucketObject : NbOfflineObject
        {
            public SubNoBucketObject()
                : base()
            {
            }
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名あり、bucketName指定無し）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalSubclassBucketNameUnset()
        {
            var bucket = new NbOfflineObjectBucket<SubObject>();

            Assert.AreEqual("subTest", bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, bucket.Service);
            FieldInfo objectCache = bucket.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(bucket));
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名あり、bucketNameがnull）
        /// バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormalSubclassBucketNameNull()
        {
            var bucket = new NbOfflineObjectBucket<SubObject>(null);

            Assert.AreEqual("subTest", bucket.BucketName);
            Assert.AreEqual(NbService.Singleton, bucket.Service);
            FieldInfo objectCache = bucket.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(objectCache.GetValue(bucket));
        }

        /// <summary>
        /// コンストラクタ（NbObject、bucketName指定なし）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionBucketNameUnset()
        {
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>();
        }

        /// <summary>
        /// コンストラクタ（NbObject、bucketNameがnull）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionBucketNameNull()
        {
            var bucket = new NbOfflineObjectBucket<NbOfflineObject>(null);
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名なし、bucketName指定なし）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionSubclassBucketNameUnset()
        {
            var bucket = new NbOfflineObjectBucket<SubNoBucketObject>();
        }

        /// <summary>
        /// コンストラクタ（サブクラスにバケット名なし、bucketNameがnull）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorExceptionSubclassBucketNameNotExists()
        {
            var bucket = new NbOfflineObjectBucket<SubNoBucketObject>(null);
        }

        /// <summary>
        /// コンストラクタ（NbOfflineObject、オフラインサービス無効）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public void TestConstructorExceptionOfflineDisabled()
        {
            var s1 = NbService.Singleton;
            NbService.EnableMultiTenant(true);
            var s2 = NbService.GetInstance();
            Assert.AreNotSame(s1, s2);

            try
            {
                var bucket = new NbOfflineObjectBucket<NbOfflineObject>("test", s2);
                Assert.Fail("no exception");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
            finally
            {
                NbService.EnableMultiTenant(false);
            }
        }

        /**
        * NewObject
        **/
        /// <summary>
        /// NewObject（NbOfflineObject、正常）
        /// オブジェクトが生成できること
        /// オブジェクトのサービス、バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestNewObjectNormal()
        {
            var obj = _bucket.NewObject();

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
        }

        /// <summary>
        /// NewObject（サブクラス）
        /// オブジェクトが生成できること
        /// オブジェクトのサービス、バケット名には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestNewObjectNormalSubclass()
        {
            var bucket = new NbOfflineObjectBucket<SubObject>();
            var obj = bucket.NewObject();

            Assert.AreEqual("subTest", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
        }

        private NbObject AddTestData()
        {
            var obj = _bucket.NewObject();
            obj["name"] = "Taro Nichiden";
            return obj.SaveAsync().Result;
        }

        private NbObject AddTestData(int i)
        {
            var obj = _bucket.NewObject();
            obj["name"] = "Taro Nichiden";
            obj["no"] = i;
            return obj.SaveAsync().Result;
        }

        private NbObject AddSubclassTestData()
        {
            var bucket = new NbOfflineObjectBucket<SubObject>();
            var obj = bucket.NewObject();
            obj["name"] = "Hanako";
            return obj.SaveAsync().Result;
        }

        /**
        * QueryAsync
        **/
        /// <summary>
        /// QueryAsync（1件）
        /// 検索結果が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalOne()
        {
            // データ保存
            AddTestData();

            var objects = await _bucket.QueryAsync(new NbQuery());

            Assert.AreEqual(1, objects.Count());
            var robj = objects.First();
            Assert.AreEqual("Taro Nichiden", robj["name"]);
        }

        /// <summary>
        /// QueryAsync（全件）
        /// 検索結果が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalAll()
        {
            // データ保存
            for (int i = 1; i <= 5; i++)
            {
                AddTestData(i);
            }

            var objects = await _bucket.QueryAsync(new NbQuery());

            Assert.AreEqual(5, objects.Count());

            int count = 1;
            foreach (var robj in objects)
            {
                Assert.AreEqual(count, robj["no"]);
                Assert.AreEqual("Taro Nichiden", robj["name"]);
                count++;
            }
        }

        /// <summary>
        /// QueryAsync（クエリ演算子）
        /// 検索結果が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalWithConditions()
        {
            // データ保存
            for (int i = 1; i <= 5; i++)
            {
                AddTestData(i);
            }

            var objects = await _bucket.QueryAsync(new NbQuery().LessThan("no", 3));

            Assert.AreEqual(2, objects.Count());

            int count = 1;
            foreach (var robj in objects)
            {
                Assert.AreEqual(count, robj["no"]);
                Assert.AreEqual("Taro Nichiden", robj["name"]);
                count++;
            }
        }

        /// <summary>
        /// QueryAsync（サブクラス）
        /// 検索結果が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalSubclass()
        {
            // データ保存
            AddTestData();
            AddSubclassTestData();

            var bucket = new NbOfflineObjectBucket<SubObject>();
            var objects = await bucket.QueryAsync(new NbQuery());

            Assert.AreEqual(1, objects.Count());
            var robj = objects.First();
            Assert.AreEqual("Hanako", robj["name"]);
        }

        // 他のMongoDBのクエリ演算子については、他のクラスのUTで実施される認識のため、
        // 1パターンのみ確認する。

        /// <summary>
        /// QueryAsync（queryがnull）
        /// 検索結果が正しいこと
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalQueryNull()
        {
            // データ保存
            for (int i = 1; i <= 5; i++)
            {
                AddTestData(i);
            }

            var objects = await _bucket.QueryAsync(null);

            Assert.AreEqual(5, objects.Count());

            int count = 1;
            foreach (var robj in objects)
            {
                Assert.AreEqual(count, robj["no"]);
                Assert.AreEqual("Taro Nichiden", robj["name"]);
                count++;
            }
        }

        /// <summary>
        /// QueryAsync（プロジェクションあり）
        /// 検索結果にプロジェクションは関係がないこと
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalWithProjection()
        {
            // データ保存
            for (int i = 1; i <= 5; i++)
            {
                AddTestData(i);
            }

            var objects = await _bucket.QueryAsync(new NbQuery().Projection("-no"));

            Assert.AreEqual(5, objects.Count());

            int count = 1;
            foreach (var robj in objects)
            {
                Assert.AreEqual(count, robj["no"]);
                Assert.AreEqual("Taro Nichiden", robj["name"]);
                count++;
            }
        }

        /// <summary>
        /// QueryAsync（検索ヒットなし）
        /// カウントが0であること
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalEmpty()
        {
            var objects = await _bucket.QueryAsync(new NbQuery());
            Assert.AreEqual(0, objects.Count());
        }

        /// <summary>
        /// QueryAsync
        /// MongoQueryObjects()には、自インスタンスのバケット名と設定したNbQueryのConditionが設定されること
        /// </summary>
        [Test]
        public async void TestQueryAsyncNormalUsingMock()
        {
            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var q = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var cacheObj = CreateTestObject();

            var list = new List<NbOfflineObject>();
            list.Add(cacheObj);
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Callback((string bucketName, NbQuery query, bool aclCheck, NbUser currentUser) =>
                {
                    Assert.AreEqual(_bucket.BucketName, bucketName);
                    Assert.AreEqual(q.DeleteMarkValue, query.DeleteMarkValue);
                    Assert.AreEqual(q.LimitValue, query.LimitValue);
                    Assert.AreEqual(q.SkipValue, query.SkipValue);
                    Assert.AreEqual(conditions.ToString(), query.Conditions.ToString());

                    Assert.True(aclCheck);

                    Assert.AreEqual(user.ToString(), currentUser.ToString());
                })
                .Returns(results);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            var objs = await _bucket.QueryAsync(q);

            Assert.AreEqual(results, objs);
        }

        /**
         * QueryWithOptionsAsync
         */
        /// <summary>
        /// QueryWithOptionsAsync
        /// NotSupportedExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestQueryWithOptionsAsyncExceptionNotImplemented()
        {
            _bucket.QueryWithOptionsAsync(new NbQuery());
        }

        /**
        * GetAsync
        **/
        /// <summary>
        /// GetAsync（正常）
        /// オブジェクトを取得できること
        /// </summary>
        [Test]
        public async void TestGetAsyncNormal()
        {
            // データ保存
            var obj = AddTestData();

            var obj2 = await _bucket.GetAsync(obj.Id);

            Assert.NotNull(obj2);
            Assert.AreEqual(obj.Id, obj2.Id);
            Assert.AreEqual("Taro Nichiden", obj2["name"]);
        }

        /// <summary>
        /// GetAsync（サブクラス）
        /// オブジェクトを取得できること
        /// </summary>
        [Test]
        public async void TestGetAsyncNormalSubclass()
        {
            // データ保存
            var bucket = new NbOfflineObjectBucket<SubObject>();
            var obj = bucket.NewObject();
            obj["name"] = "Taro Nichiden";
            var obj1 = await obj.SaveAsync();

            var obj2 = await bucket.GetAsync(obj1.Id);

            Assert.NotNull(obj2);
            Assert.AreEqual(obj1.Id, obj2.Id);
            Assert.AreEqual("Taro Nichiden", obj2["name"]);
        }

        /// <summary>
        /// GetAsync（取得対象のオブジェクトが存在しない）
        /// NbHttpExceptionが発生すること、そのステータスコードがNotFoundであること
        /// </summary>
        [Test]
        public async void TestGetAsyncExceptionObjectNotFound()
        {
            // データ保存
            AddTestData();

            try
            {
                var obj = await _bucket.GetAsync("not_exist_id");
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
        }

        /// <summary>
        /// GetAsync（論理削除済オブジェクト）
        /// NbHttpExceptionが発生すること、そのステータスコードがNotFoundであること
        /// </summary>
        [Test]
        public async void TestGetAsyncExceptionDeleted()
        {
            // データ保存
            var obj = AddTestData();
            string objId = obj.Id;

            // 論理削除
            await obj.DeleteAsync();

            try
            {
                var obj2 = await _bucket.GetAsync(objId);
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
        }

        // オブジェクトが保存されていない、もしくは論理削除済みオブジェクトの場合は
        // 同じ結果となるが、正しく判断されているかを確認するため、
        // Mockで確認する

        /// <summary>
        /// GetAsync（更新対象のオブジェクトが存在しない）
        /// NbHttpExceptionが発生すること、そのステータスコードがNotFoundであること
        /// </summary>
        [Test]
        public async void TestGetAsyncExceptionNotFoundUsingMock()
        {
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(_bucket.BucketName, bucketName);
                    Assert.AreEqual("testObjectId", objectId);
                })
                .Returns((NbOfflineObject)null);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            try
            {
                var obj2 = await _bucket.GetAsync("testObjectId");
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
        }

        /// <summary>
        /// GetAsync（論理削除済オブジェクト）
        /// NbHttpExceptionが発生すること、そのステータスコードがNotFoundであること
        /// </summary>
        [Test]
        public async void TestGetAsyncExceptionDeletedUsingMock()
        {
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            var cacheObj = CreateTestObject();
            cacheObj.Deleted = true;

            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(_bucket.BucketName, bucketName);
                    Assert.AreEqual("testObjectId", objectId);
                })
                .Returns(cacheObj);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            try
            {
                var obj2 = await _bucket.GetAsync("testObjectId");
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
        }

        /// <summary>
        /// GetAsync（権限がない）
        /// NbHttpExceptionが発生すること、そのステータスコードがForbiddenであること
        /// </summary>
        [Test]
        public async void TestGetAsyncExceptionNoPermission()
        {
            // データ保存
            var obj = _bucket.NewObject();
            obj["name"] = "Taro Nichiden";
            var acl = NbAcl.CreateAclFor(new HashSet<string>());
            obj.Acl = acl;
            var obj1 = await obj.SaveAsync();

            try
            {
                var obj2 = await _bucket.GetAsync(obj1.Id);
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
            }
        }

        /// <summary>
        /// GetAsync（オブジェクトIDがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetAsyncExceptionObjectIdNull()
        {
            _bucket.GetAsync(null);
        }

        /**
         * DeleteAsync
         */
        /// <summary>
        /// DeleteAsync（softDelete未設定）
        /// 論理削除が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteUnsetUsingInmemory()
        {
            AddTestData();

            var result = await _bucket.DeleteAsync(new NbQuery());
            Assert.AreEqual(1, result);
            // 状態確認
            Assert.False(_processState.Crud);
        }

        /// <summary>
        /// DeleteAsync（softDelete=true）
        /// 論理削除が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteUsingInmemory()
        {
            AddTestData();

            // soft delete
            var result = await _bucket.DeleteAsync(new NbQuery(), true);
            Assert.AreEqual(1, result);
            // 状態確認
            Assert.False(_processState.Crud);

            // 追加
            // hard delete
            // DeleteMark指定はwhere条件に入らない。論理削除されたデータは含まれないため、削除されない
            result = await _bucket.DeleteAsync(new NbQuery().DeleteMark(true), false);
            Assert.AreEqual(0, result);
            // 状態確認
            Assert.False(_processState.Crud);
        }

        /// <summary>
        /// DeleteAsync（softDelete=false）
        /// 物理削除が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalHardDeleteUsingInmemory()
        {
            AddTestData();

            // hard delete
            var result = await _bucket.DeleteAsync(new NbQuery(), false);
            Assert.AreEqual(1, result);
            // 状態確認
            Assert.False(_processState.Crud);
        }

        /// <summary>
        /// DeleteAsync（サブクラス）
        /// 削除が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSubclass()
        {
            AddSubclassTestData();

            // hard delete
            var bucket = new NbOfflineObjectBucket<SubObject>();
            var result = await bucket.DeleteAsync(new NbQuery());
            Assert.AreEqual(1, result);
            // 状態確認
            Assert.False(_processState.Crud);
        }

        /// <summary>
        /// DeleteAsync
        /// 物理削除
        /// ・物理削除となること（NbObjectCache#DeleteObject()が実行されること）
        /// ・DeleteObject()には、取得したオブジェクトを渡すこと
        /// ・MongoQueryObjects()には、自インスタンスのバケット名と設定したNbQueryのConditionが設定されること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalHardDelete()
        {

            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var cacheObj = CreateTestObject();

            var list = new List<NbOfflineObject>();
            list.Add(cacheObj);
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Callback((string bucketName, NbQuery customQuery, bool aclCheck, NbUser currentUser) =>
                {
                    var expectQuery = new NbQuery();
                    Assert.AreEqual(_bucket.BucketName, bucketName);

                    Assert.AreEqual(expectQuery.DeleteMarkValue, customQuery.DeleteMarkValue);
                    Assert.AreEqual(expectQuery.LimitValue, customQuery.LimitValue);
                    Assert.AreEqual(expectQuery.SkipValue, customQuery.SkipValue);
                    Assert.AreEqual(conditions.ToString(), customQuery.Conditions.ToString());

                    Assert.IsTrue(aclCheck);

                    Assert.AreEqual(user.ToString(), currentUser.ToString());
                })
                .Returns(results);

            mockObjectCache.Setup(m => m.DeleteObject(It.IsAny<NbObject>()))
                .Callback((NbObject actualObj) =>
                {
                    Assert.AreEqual(cacheObj, (NbOfflineObject)actualObj);
                });

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            await _bucket.DeleteAsync(query, false);

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.DeleteObject(It.IsAny<NbObject>()), Times.Once());

        }

        /// <summary>
        /// DeleteAsync
        /// 論理削除
        /// ・論理削除となること（NbObjectCache#UpdateObject()が実行されること）
        /// ・DeleteObject()には、自インスタンスとNbSyncState.Dirtyを渡すこと
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDelete()
        {

            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            SetMockSessionInfo(NbService.Singleton);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var cacheObj = CreateTestObject();

            var list = new List<NbOfflineObject>();
            list.Add(cacheObj);
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Returns(results);

            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject actualObj, NbSyncState actualState) =>
                {
                    Assert.AreEqual(cacheObj, (NbOfflineObject)actualObj);
                    Assert.AreEqual(NbSyncState.Dirty, actualState);
                });

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            await _bucket.DeleteAsync(query, true);

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// DeleteAsync
        /// 削除方法未指定
        /// ・引数SoftDelete未指定時は論理削除となること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalAuto()
        {

            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            SetMockSessionInfo(NbService.Singleton);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var cacheObj = CreateTestObject();

            var list = new List<NbOfflineObject>();
            list.Add(cacheObj);
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Returns(results);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            await _bucket.DeleteAsync(query);

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());

        }

        /// <summary>
        /// DeleteAsync
        /// クエリ未指定
        /// ・NbQueryにNullが指定された場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteAsyncExceptionNoQuery()
        {

            await _bucket.DeleteAsync(null, false);

        }

        /// <summary>
        /// DeleteAsync
        /// 該当オブジェクト0件
        /// ・クエリ結果0件時は処理を行わないこと
        /// ・エラーとならないこと
        /// </summary>
        [Test]
        public async void TestDeleteAsyncSubnormalObjectNotExist()
        {

            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            SetMockSessionInfo(NbService.Singleton);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var list = new List<NbOfflineObject>();
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Returns(results);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            await _bucket.DeleteAsync(query, false);

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.DeleteObject(It.IsAny<NbObject>()), Times.Never());
        }

        /// <summary>
        /// DeleteAsync
        /// ACL権限なし
        /// ・対象オブジェクトに削除権限がない場合は処理を行わないこと
        /// ・エラーとならないこと
        /// </summary>
        [Test]
        public async void TestDeleteAsyncSubnormalNoPermission()
        {

            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            SetMockSessionInfo(NbService.Singleton);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var cacheObj = CreateTestObject();
            cacheObj.Acl = null;

            var list = new List<NbOfflineObject>();
            list.Add(cacheObj);
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Returns(results);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            await _bucket.DeleteAsync(query, false);

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.DeleteObject(It.IsAny<NbObject>()), Times.Never());

        }

        /// <summary>
        /// DeleteAsync
        /// 同期中
        /// ・NbExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionWhileSyncing()
        {
            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();

            int result;
            try
            {
                result = await _bucket.DeleteAsync(query);
                Assert.Fail("no exception");
            }
            catch (NbException ex)
            {
                Assert.AreEqual(NbStatusCode.Locked, ex.StatusCode);
                Assert.AreEqual("Locked.", ex.Message);
            }

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()), Times.Never());

            // 後始末
            _processState.EndSync();
        }

        /// <summary>
        /// DeleteAsync
        /// 同期終了後
        /// ・削除処理が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalAfterSyncing()
        {
            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            SetMockSessionInfo(NbService.Singleton);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var cacheObj = CreateTestObject();

            var list = new List<NbOfflineObject>();
            list.Add(cacheObj);
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Returns(results);

            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();
            // 同期を終了させる
            _processState.EndSync();

            await _bucket.DeleteAsync(query);

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()), Times.Once());
        }

        /// <summary>
        /// DeleteAsync
        /// CRUD中
        /// ・CRUD終了まで待ち合わせた後で削除処理が成功すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalWhileCrud()
        {
            // クエリ
            var conditions = new NbJsonObject();
            conditions.Add("Test", "Query");
            var query = CreateTestQuery(conditions);

            // currentUser ユーザ取得成功
            var user = new NbUser();
            user.UserId = "testUser";
            var expire = NbUtil.CurrentUnixTime() + 10000;
            var mockSI = new NbSessionInfo();
            mockSI.Set("SessionToken", expire, user);
            SetMockSessionInfo(NbService.Singleton, mockSI);

            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);

            // MongoQueryObjects ユーザ取得1件
            var cacheObj = CreateTestObject();

            var list = new List<NbOfflineObject>();
            list.Add(cacheObj);
            IEnumerable<NbOfflineObject> results = list;

            mockObjectCache.Setup(m => m.MongoQueryObjects<NbOfflineObject>(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<bool>(), It.IsAny<NbUser>()))
                .Returns(results);


            // Mockセット
            SetMockObjectCache(_bucket, mockObjectCache.Object);

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            Task task = new Task(() =>
            {
                Thread.Sleep(500);
                _processState.EndCrud();
            });
            task.Start();

            await _bucket.DeleteAsync(query, false);

            // 状態確認
            Assert.False(_processState.Crud);

            mockObjectCache.Verify(m => m.DeleteObject(It.IsAny<NbObject>()), Times.Once());
        }

        /**
         * AggregateAsync
         */

        /// <summary>
        /// 集計(Aggregation)
        /// オフラインでは Aggregation は行えないこと
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestAggrergateAsyncExceptionNotImplemented()
        {
            _bucket.AggregateAsync(new NbJsonArray());
        }

        /**
         * BatchAsync
         */

        /// <summary>
        /// バッチ処理
        /// オフラインではバッチ処理は行えないこと
        /// </summary>
        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestBatchAsyncExceptionNotImplemented()
        {
            _bucket.BatchAsync(new NbBatchRequest());
        }
    }
}
