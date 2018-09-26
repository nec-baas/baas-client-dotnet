using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    partial class NbObjectSyncManagerTest
    {
        /// <summary>
        /// Pull データ無し
        /// </summary>
        [Test]
        public async void TestPullNormalNoData()
        {
            // test
            await TestPull(0);
        }

        /// <summary>
        /// Pull 1件更新
        /// </summary>
        [Test]
        public async void TestPullNormalOneData()
        {
            // test
            await TestPull(1);
        }

        /// <summary>
        /// Pull 分割数-1更新
        /// </summary>
        [Test]
        public async void TestPullNormalDivideMinusOneData()
        {
            // test
            await TestPull(NbObjectSyncManager.PullDivideNumber - 1);
        }

        /// <summary>
        /// Pull 分割数更新
        /// </summary>
        [Test]
        public async void TestPullNormalDivideData()
        {
            // test
            await TestPull(NbObjectSyncManager.PullDivideNumber);
        }

        /// <summary>
        /// Pull 分割数+1更新
        /// </summary>
        [Test]
        public async void TestPullNormalDividePlusOneData()
        {
            // test
            await TestPull(NbObjectSyncManager.PullDivideNumber + 1);
        }

        /// <summary>
        /// Pull 400 BadRequest発生
        /// </summary>
        [Test]
        public async void TestPullExceptionBadRequest()
        {
            var firstQuery = new NbQuery();

            var errorMessage = new NbJsonObject()
            {
                {"error", "Bad Request"}
            };

            var response = new MockRestResponse(HttpStatusCode.BadRequest, errorMessage.ToString());
            _restExecutor.AddResponse(response);

            // test
            try
            {
                var result = await _manager.Pull(TestBucket, firstQuery, NbObjectConflictResolver.PreferServerResolver);
            }
            catch (NbHttpException ex)
            {
                // 期待動作
                Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
            }
        }

        /// <summary>
        /// GetFirstQuery 前回時刻保存無し
        /// </summary>
        [Test]
        public void TestGetFirstQueryNormalNoDateTime()
        {
            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(MethodNameLastPullServerTime, method);

                }).Returns((string)null);

            var baseQuery = new NbQuery();
            baseQuery.EqualTo("key", "value").Limit(100).Skip(200).OrderBy("abc").Projection("def").DeleteMark(false);

            // test
            var result = _mockManager.Object.GetFirstQuery(TestBucket, baseQuery);

            // base条件がそのまま使用されること
            Assert.AreEqual(baseQuery.Conditions.ToString(), result.Conditions.ToString());
            // base条件に指定した値が無視され、規定値に書き換えられていること
            Assert.AreEqual(NbObjectSyncManager.PullDivideNumber, result.LimitValue);
            Assert.AreEqual(-1, result.SkipValue);
            var order = new string[] { "updatedAt", "_id" };
            Assert.AreEqual(order, result.Order);
            Assert.IsNull(result.ProjectionValue);
            Assert.AreEqual(true, result.DeleteMarkValue);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// GetFirstQuery 前回時刻保存済み<br/>
        /// (指定時刻-3秒)のオフセットを条件に$andで付与
        /// </summary>
        [Test]
        public void TestGetFirstQueryNormalDateTimeSaved()
        {
            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(MethodNameLastPullServerTime, method);

                }).Returns(PullLastTime);

            var baseQuery = new NbQuery();
            baseQuery.EqualTo("key", "value").Limit(100).Skip(200).OrderBy("abc").Projection("def").DeleteMark(false);

            // test
            var result = _mockManager.Object.GetFirstQuery(TestBucket, baseQuery);

            // "オフセットを引いた時刻以降"の条件が$andで加えられていること
            var expectedConditions = NbQuery.And(new NbQuery().GreaterThanOrEqual("updatedAt", PullLastOffsetTime), baseQuery);
            // base条件に指定した値が無視され、規定値に書き換えられていること
            Assert.AreEqual(expectedConditions.Conditions.ToString(), result.Conditions.ToString());
            Assert.AreEqual(NbObjectSyncManager.PullDivideNumber, result.LimitValue);
            Assert.AreEqual(-1, result.SkipValue);
            var order = new string[] { "updatedAt", "_id" };
            Assert.AreEqual(order, result.Order);
            Assert.IsNull(result.ProjectionValue);
            Assert.AreEqual(true, result.DeleteMarkValue);

            _mockManager.VerifyAll();
        }

        // internal virtual NbQuery GetSecondOrLaterQuery(NbQuery baseQuery, IEnumerable<NbObject> objects)

        /// <summary>
        /// GetSecondOrLaterQuery objectが0件
        /// </summary>
        [Test]
        public void TestGetSecondOrLaterQueryNormalNoObject()
        {
            var query = new NbQuery();
            var objects = CreateObjects(0);

            var result = _manager.GetSecondOrLaterQuery(query, objects);
            Assert.IsNull(result);
        }

        /// <summary>
        /// GetSecondOrLaterQuery objectが PullDivideNumber-1件
        /// </summary>
        [Test]
        public void TestGetSecondOrLaterQueryNormalOneObject()
        {
            var query = new NbQuery();
            var objects = CreateObjects(NbObjectSyncManager.PullDivideNumber - 1);

            var result = _manager.GetSecondOrLaterQuery(query, objects);
            Assert.IsNull(result);
        }

        /// <summary>
        /// GetSecondOrLaterQuery objectが PullDivideNumber件
        /// </summary>
        [Test]
        public void TestGetSecondOrLaterQueryNormalPullDivideNumberObjects()
        {
            var query = new NbQuery();
            query.EqualTo("key", "value").Limit(100).Skip(200).OrderBy("abc").Projection("def").DeleteMark(false);

            var objects = CreateObjects(NbObjectSyncManager.PullDivideNumber);

            var result = _manager.GetSecondOrLaterQuery(query, objects);
            Assert.IsNotNull(result);

            var baseCondition = query.Conditions.ToString();
            var lastUpdatedAt = objects.Last().UpdatedAt;
            var lastId = objects.Last().Id;

            // 比較対象の検索条件を生成
            var expectedString = CreateSecondQueryConditionJsonString(baseCondition, lastUpdatedAt, lastId);
            var expectedConditions = NbJsonObject.Parse(expectedString);

            // 検索条件を比較
            Assert.AreEqual(expectedConditions.ToString(), result.Conditions.ToString());

            // base条件に指定した値が無視され、規定値に書き換えられていること
            Assert.AreEqual(NbObjectSyncManager.PullDivideNumber, result.LimitValue);
            Assert.AreEqual(-1, result.SkipValue);
            var order = new string[] { "updatedAt", "_id" };
            Assert.AreEqual(order, result.Order);
            Assert.IsNull(result.ProjectionValue);
            Assert.AreEqual(true, result.DeleteMarkValue);
        }

        /// <summary>
        /// PullUpdate
        /// </summary>
        [Test]
        public void TestPullUpdateNormal()
        {
            var objects = CreateObjects(10);

            _mockManager.Setup(m => m.PullProcessResults(It.IsAny<string>(), It.IsAny<IEnumerable<NbObject>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, IEnumerable<NbObject> serverObjects,
            NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(objects, serverObjects);
                    Assert.AreEqual(NbObjectConflictResolver.PreferClientResolver, resolver);

                }).Returns(objects.Count());


            // test
            var result = _mockManager.Object.PullUpdate(TestBucket, objects, NbObjectConflictResolver.PreferClientResolver);

            Assert.AreEqual(objects.Count(), result);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// PullUpdateの処理中にException発生
        /// </summary>
        [Test]
        public void TestPullUpdateExceptionInvalidOperationException()
        {
            var objects = CreateObjects(10);

            _mockManager.Setup(m => m.PullProcessResults(It.IsAny<string>(), It.IsAny<IEnumerable<NbObject>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, IEnumerable<NbObject> serverObjects,
            NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(objects, serverObjects);
                    Assert.AreEqual(NbObjectConflictResolver.PreferClientResolver, resolver);

                }).Throws(new InvalidOperationException("test exception"));

            // test
            try
            {
                var result = _mockManager.Object.PullUpdate(TestBucket, objects, NbObjectConflictResolver.PreferClientResolver);
            }
            catch (InvalidOperationException)
            {
                // 期待動作
            }
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// WaitPullUpdate 待機タスク無し
        /// </summary>
        [Test]
        public async void TestWaitPullUpdateNormalNoTask()
        {
            var result = await _manager.WaitPullUpdate(null);

            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// WaitPullUpdate 待機タスク有り
        /// </summary>
        [Test]
        public async void TestWaitPullUpdateNormalWithTask()
        {
            var task = Task.FromResult(1);
            var result = await _manager.WaitPullUpdate(task);

            Assert.AreEqual(1, result);
        }

        /// <summary>
        /// PullProcessResult<br/>
        /// サーバ:新規
        /// クライアント: 無し
        /// </summary>
        [Test]
        public void TestPullProcessResultsNormalServerNormalClientNone()
        {
            var serverObjects = CreateObjects(1);
            var serverObj = serverObjects.First();

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            // DBに未保存
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(serverObj.Id, objectId);
                }).Returns((NbOfflineObject)null);

            // DBへの保存実行
            mockObjectCache.Setup(m => m.InsertObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    // サーバオブジェクトの保存を期待
                    Assert.AreEqual(serverObj, obj);
                    Assert.AreEqual(NbSyncState.Sync, state);
                });

            // test
            var result = _manager.PullProcessResults(TestBucket, serverObjects, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(1, result);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PullProcessResult<br/>
        /// サーバ:新規(論理削除済み)
        /// クライアント: 無し
        /// </summary>
        [Test]
        public void TestPullProcessResultsNormalServerDeletedClientNone()
        {
            // 論理削除済み
            var serverObjects = CreateObjects(1);
            var serverObj = serverObjects.First();
            serverObj.Deleted = true;

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            // DBに未保存
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(serverObj.Id, objectId);
                }).Returns((NbOfflineObject)null);

            // DBへの保存は行わない

            // test
            var result = _manager.PullProcessResults(TestBucket, serverObjects, NbObjectConflictResolver.PreferServerResolver);
            // 論理削除済みのオブジェクトは保存しない
            Assert.AreEqual(0, result);
            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PullProcessResult<br/>
        /// サーバ/クライアント: ETag一致
        /// </summary>
        [Test]
        public void TestPullProcessResultsNormalSameEtag()
        {
            var serverObjects = CreateObjects(1);
            var serverObj = serverObjects.First();
            var clientObjects = CreateOfflineObjects(1);
            var clientObj = clientObjects.First();

            // Server,ClientのETag一致
            serverObj.Etag = "sameEtag";
            clientObj.Etag = "sameEtag";

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            // DBに同一ETagのオブジェクト保存済み
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(serverObj.Id, objectId);
                }).Returns(clientObj);

            // test
            var result = _manager.PullProcessResults(TestBucket, serverObjects, NbObjectConflictResolver.PreferServerResolver);
            // ETag一致の場合は更新無しのため保存しない
            Assert.AreEqual(0, result);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PullProcessResult<br/>
        /// サーバ/クライアント: ETag不一致
        /// クライアント: Sync
        /// </summary>
        [Test]
        public void TestPullProcessResultsNormalDiffEtagClientSync()
        {
            var serverObjects = CreateObjects(1);
            var serverObj = serverObjects.First();
            var clientObjects = CreateOfflineObjects(1);
            var clientObj = clientObjects.First();

            // Server,ClientのETag不一致
            serverObj.Etag = "updatedEtag";
            clientObj.Etag = "oldEtag";

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            // DBにETag不一致のオブジェクト保存済み
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(serverObj.Id, objectId);
                }).Returns(clientObj);

            // DBへの更新実行
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    // サーバオブジェクトの保存を期待
                    Assert.AreEqual(serverObj, obj);
                    Assert.AreEqual(NbSyncState.Sync, state);
                });

            // test
            var result = _manager.PullProcessResults(TestBucket, serverObjects, NbObjectConflictResolver.PreferServerResolver);
            // 更新を期待
            Assert.AreEqual(1, result);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PullProcessResult<br/>
        /// サーバ/クライアント: ETag不一致
        /// サーバ: 論理削除
        /// クライアント: Sync
        /// </summary>
        [Test]
        public void TestPullProcessResultsNormalDiffEtagServerDeletedClientSync()
        {
            var serverObjects = CreateObjects(1);
            var serverObj = serverObjects.First();
            var clientObjects = CreateOfflineObjects(1);
            var clientObj = clientObjects.First();

            // Server,ClientのETag不一致
            serverObj.Etag = "updatedEtag";
            clientObj.Etag = "oldEtag";
            // Server論理削除
            serverObj.Deleted = true;

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            // DBにETag不一致のオブジェクト保存済み
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(serverObj.Id, objectId);
                }).Returns(clientObj);

            // DBへの削除実行
            mockObjectCache.Setup(m => m.DeleteObject(It.IsAny<NbObject>()))
                .Callback((NbObject obj) =>
                {
                    // クライアントオブジェクトの削除を期待
                    Assert.AreEqual(clientObj, obj);
                });

            // test
            var result = _manager.PullProcessResults(TestBucket, serverObjects, NbObjectConflictResolver.PreferServerResolver);
            // 更新を期待
            Assert.AreEqual(1, result);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PullProcessResult<br/>
        /// サーバ/クライアント: ETag不一致
        /// クライアント: Dirty
        /// (HandleConflictの動作は別のテストで実施)
        /// </summary>
        [Test]
        public void TestPullProcessResultsNormalDiffEtagClientDirty()
        {
            var serverObjects = CreateObjects(1);
            var serverObj = serverObjects.First();
            var clientObjects = CreateOfflineObjects(1);
            var clientObj = clientObjects.First();

            // Server,ClientのETag不一致
            serverObj.Etag = "updatedEtag";
            clientObj.Etag = "oldEtag";
            // Client Dirty
            clientObj.SyncState = NbSyncState.Dirty;

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            // DBにETag不一致のオブジェクト保存済み
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(serverObj.Id, objectId);
                }).Returns(clientObj);

            // コンフリクト解決によるDBへの更新
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    // サーバオブジェクトの保存を期待
                    Assert.AreEqual(serverObj, obj);
                    Assert.AreEqual(NbSyncState.Sync, state);
                });
            // test
            var result = _manager.PullProcessResults(TestBucket, serverObjects, NbObjectConflictResolver.PreferServerResolver);
            // 更新を期待
            Assert.AreEqual(1, result);

            mockObjectCache.VerifyAll();
        }

        // ---------------------------------------------------------
        // Test Utilities

        private string CreateSecondQueryConditionJsonString(string baseConditions, string updatedAt, string id)
        {
            var builder = new StringBuilder();
            builder.Append("{\"$and\":[");
            {
                // ユーザ指定の条件
                builder.Append(baseConditions);
                builder.Append(",");
                // SDKが付与する追加条件
                {
                    builder.Append("{\"$or\":[");
                    {
                        // 最終オブジェクトのupdatedAtより大きい
                        {
                            builder.Append("{\"updatedAt\":{\"$gt\":\"");
                            builder.Append(updatedAt);
                            builder.Append("\"}}");
                        }

                        builder.Append(",");

                        {
                            builder.Append("{\"$and\":[");
                            {
                                // 最終オブジェクトのupdatedAtが等しい
                                builder.Append("{\"updatedAt\":\"");
                                builder.Append(updatedAt);
                                builder.Append("\"}");

                                builder.Append(",");

                                // 最終オブジェクトの_idより大きい
                                builder.Append("{\"_id\":{\"$gt\":\"");
                                builder.Append(id);
                                builder.Append("\"}}");
                            }
                            builder.Append("]}");
                        }
                    }
                    builder.Append("]}");
                }
            }
            builder.Append("]}");

            return builder.ToString();
        }

        private async Task<int> TestPull(int testObjectsNumber)
        {
            var firstQuery = new NbQuery();
            firstQuery.LessThanOrEqual("test", 100);

            // ObjectBucketの応答生成
            var pullObjectNumber = testObjectsNumber;
            string queryFirstPullTime = null;
            int counter = 0;
            while (pullObjectNumber >= 0)
            {
                var queryResult = CreateQueryAsyncWithOptionResponseJson((pullObjectNumber > NbObjectSyncManager.PullDivideNumber) ? NbObjectSyncManager.PullDivideNumber : pullObjectNumber, counter * 100);
                var response = new MockRestResponse(HttpStatusCode.OK, queryResult.ToString());
                _restExecutor.AddResponse(response);

                if (queryFirstPullTime == null)
                {
                    queryFirstPullTime = (string)queryResult["currentTime"];
                }
                // 分割数分マイナスする
                pullObjectNumber -= NbObjectSyncManager.PullDivideNumber;

                // Pull回数のカウンタ
                counter++;
            }
            // internal methodのモック化
            _mockManager.Setup(m => m.GetFirstQuery(It.IsAny<string>(), It.IsAny<NbQuery>()))
               .Callback((string bucketName, NbQuery baseQuery) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(firstQuery.ToString(), baseQuery.ToString());
               }).Returns(firstQuery);

            _mockManager.Setup(m => m.WaitPullUpdate(It.IsAny<Task<int>>()))
                 .Returns((Task<int> x) => (x == null) ? Task.FromResult(0) : Task.FromResult(x.Result));

            // コールされる回数に応じて返却する結果を変更する
            int pullUpdateCount = testObjectsNumber;
            _mockManager.Setup(m => m.PullUpdate(It.IsAny<string>(), It.IsAny<IEnumerable<NbObject>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, IEnumerable<NbObject> objects, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);

                   int currentCount;
                   if (pullUpdateCount >= NbObjectSyncManager.PullDivideNumber)
                   {
                       currentCount = NbObjectSyncManager.PullDivideNumber;
                   }
                   else
                   {
                       currentCount = pullUpdateCount;
                   }

                   Assert.AreEqual(currentCount, objects.Count());

                   pullUpdateCount -= NbObjectSyncManager.PullDivideNumber;

               }).Returns((string bucketName, IEnumerable<NbObject> objects, NbObjectConflictResolver.Resolver resolver) =>
                    objects.Count()
               );

            // コールされる回数に応じて返却する結果を変更する
            int queryCount = testObjectsNumber;
            _mockManager.Setup(m => m.GetSecondOrLaterQuery(It.IsAny<NbQuery>(), It.IsAny<IEnumerable<NbObject>>()))
               .Callback((NbQuery secondQueryArg, IEnumerable<NbObject> objects) =>
               {
                   Assert.AreEqual(firstQuery.ToString(), secondQueryArg.ToString());

                   int currentCount;
                   if (queryCount >= NbObjectSyncManager.PullDivideNumber)
                   {
                       currentCount = NbObjectSyncManager.PullDivideNumber;
                   }
                   else
                   {
                       currentCount = queryCount;
                   }

                   Assert.AreEqual(currentCount, objects.Count());

                   queryCount -= NbObjectSyncManager.PullDivideNumber;

               }).Returns((NbQuery secondQueryArg, IEnumerable<NbObject> objects) => (objects.Count() < NbObjectSyncManager.PullDivideNumber) ? null : secondQueryArg);

            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .Callback((string bucketName, string method, string value) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(MethodNameLastPullServerTime, method);
                   Assert.AreEqual(queryFirstPullTime, value);
               }).Returns(1);

            // test
            var result = await _mockManager.Object.Pull(TestBucket, firstQuery, NbObjectConflictResolver.PreferServerResolver);
            // 更新件数
            Assert.AreEqual(testObjectsNumber, result);

            _mockManager.VerifyAll();

            return result;
        }

        private static NbJsonObject CreateQueryAsyncWithOptionResponseJson(int number, double offset)
        {

            var array = new NbJsonArray();

            for (int i = 0; i < number; i++)
            {
                var jsonObj = new NbJsonObject()
                {
                    {"_id", i.ToString()},
                    {"etag", "12345"},
                    {"updatedAt", GetOffsetTime(PullObjectUpdatedTime, i)},
                };
                array.Add(jsonObj);
            }
            var json = new NbJsonObject()
            {
                {"results", array},
                {"count", array.Count},
                {"currentTime", GetOffsetTime(PullLastTime, offset)}
            };

            return json;
        }

        private IEnumerable<NbObject> CreateObjects(int objectNum)
        {
            var objects = new List<NbObject>();

            for (int i = 0; i < objectNum; i++)
            {
                var obj = _bucket.NewObject();
                obj.Add("_id", i.ToString());
                obj.Add("etag", "12345");
                obj.Add("updatedAt", GetOffsetTime(PullObjectUpdatedTime, i));

                objects.Add(obj);
            }

            return objects;
        }

        private IEnumerable<NbOfflineObject> CreateOfflineObjects(int objectNum)
        {
            var objects = new List<NbOfflineObject>();

            for (int i = 0; i < objectNum; i++)
            {
                var obj = _offlineBucket.NewObject();
                obj.Add("_id", i.ToString());
                obj.Add("etag", "12345");
                obj.Add("updatedAt", GetOffsetTime(PullObjectUpdatedTime, i));

                objects.Add(obj);
            }

            return objects;
        }

    }
}
