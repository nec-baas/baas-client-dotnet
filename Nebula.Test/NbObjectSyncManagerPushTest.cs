using Moq;
using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    partial class NbObjectSyncManagerTest
    {
        /// <summary>
        /// Push 0件
        /// </summary>
        [Test]
        public void TestPushNormalZeroObject()
        {
            _mockManager.Setup(m => m.WaitPushUpdate(It.IsAny<Task<IList<NbBatchResult>>>(), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    Assert.IsNull(task);
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.UpdateLastSyncTime(It.IsAny<string>()))
               .Callback((string bucketName) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
               });

            // test
            var result = _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver).Result;
            // 失敗件数
            Assert.AreEqual(0, result.Count);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Push 1件
        /// </summary>
        [Test]
        public async void TestPushNormalOneObject()
        {
            var objectCount = 1;

            var clientObjects = InsertClientObjects(objectCount);

            // Pushのレスポンス生成・設定
            var responseJson = SetupPushResponse(clientObjects, true);

            NbBatchRequest pushUpdateRequest = null;

            // internal methodのモック化
            _mockManager.Setup(m => m.CreatePushRequest(It.IsAny<IEnumerable<NbOfflineObject>>()))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (pushUpdateRequest = CreatePushUpdateRequest(objects)));

            _mockManager.Setup(m => m.WaitPushUpdate(null, It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.WaitPushUpdate(It.Is<Task<IList<NbBatchResult>>>(i => (i != null)), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    var batchResult = task.Result;
                    Assert.AreEqual(0, batchResult.Count);
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.PushUpdate(It.IsAny<string>(), It.IsAny<NbBatchRequest>(), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, NbBatchRequest batch, IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(pushUpdateRequest.Json.ToString(), batch.Json.ToString());

                   var response = responseJson.First();
                   responseJson.Remove(response);
                   CheckEqualResponseAndBatchResult(response, results);

                   Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);

               }).Returns(new List<NbBatchResult>());

            _mockManager.Setup(m => m.UpdateLastSyncTime(It.IsAny<string>()))
               .Callback((string bucketName) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
               });

            // test
            var result = await _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
            // 失敗件数
            Assert.AreEqual(0, result.Count);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Push PushDivideNumber-1件
        /// </summary>
        [Test]
        public async void TestPushNormalPushDivideNumberMinusOneObjects()
        {
            var objectCount = NbObjectSyncManager.PushDivideNumber - 1;

            var clientObjects = InsertClientObjects(objectCount);

            // Pushのレスポンス生成・設定
            var responseJson = SetupPushResponse(clientObjects, true);

            NbBatchRequest pushUpdateRequest = null;

            // internal methodのモック化
            _mockManager.Setup(m => m.CreatePushRequest(It.IsAny<IEnumerable<NbOfflineObject>>()))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (pushUpdateRequest = CreatePushUpdateRequest(objects)));

            _mockManager.Setup(m => m.WaitPushUpdate(null, It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.WaitPushUpdate(It.Is<Task<IList<NbBatchResult>>>(i => (i != null)), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    var batchResult = task.Result;
                    Assert.AreEqual(0, batchResult.Count);
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.PushUpdate(It.IsAny<string>(), It.IsAny<NbBatchRequest>(), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, NbBatchRequest batch, IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(pushUpdateRequest.Json.ToString(), batch.Json.ToString());

                   var response = responseJson.First();
                   responseJson.Remove(response);
                   CheckEqualResponseAndBatchResult(response, results);

                   Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);

               }).Returns(new List<NbBatchResult>());

            _mockManager.Setup(m => m.UpdateLastSyncTime(It.IsAny<string>()))
               .Callback((string bucketName) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
               });

            // test
            var result = await _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
            // 失敗件数
            Assert.AreEqual(0, result.Count);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Push PushDivideNumber件
        /// </summary>
        [Test]
        public async void TestPushNormalPushDivideNumberObjects()
        {
            var objectCount = NbObjectSyncManager.PushDivideNumber;

            var clientObjects = InsertClientObjects(objectCount);

            // Pushのレスポンス生成・設定
            var responseJson = SetupPushResponse(clientObjects, true);

            NbBatchRequest pushUpdateRequest = null;

            // internal methodのモック化
            _mockManager.Setup(m => m.CreatePushRequest(It.IsAny<IEnumerable<NbOfflineObject>>()))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (pushUpdateRequest = CreatePushUpdateRequest(objects)));

            _mockManager.Setup(m => m.WaitPushUpdate(null, It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.WaitPushUpdate(It.Is<Task<IList<NbBatchResult>>>(i => (i != null)), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    var batchResult = task.Result;
                    Assert.AreEqual(0, batchResult.Count);
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.PushUpdate(It.IsAny<string>(), It.IsAny<NbBatchRequest>(), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, NbBatchRequest batch, IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(pushUpdateRequest.Json.ToString(), batch.Json.ToString());

                   var response = responseJson.First();
                   responseJson.Remove(response);
                   CheckEqualResponseAndBatchResult(response, results);

                   Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);

               }).Returns(new List<NbBatchResult>());

            _mockManager.Setup(m => m.UpdateLastSyncTime(It.IsAny<string>()))
               .Callback((string bucketName) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
               });

            // test
            var result = await _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
            // 失敗件数
            Assert.AreEqual(0, result.Count);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Push PushDivideNumber+1件
        /// </summary>
        [Test]
        public async void TestPushNormalPushDivideNumberPlusOneObjects()
        {
            var objectCount = NbObjectSyncManager.PushDivideNumber + 1;

            var clientObjects = InsertClientObjects(objectCount);
            var clientFirstPushObjects = clientObjects.Take(NbObjectSyncManager.PushDivideNumber);
            var clientSecondPushObjects = clientObjects.Skip(NbObjectSyncManager.PushDivideNumber).Take(NbObjectSyncManager.PushDivideNumber);

            // Pushのレスポンス生成・設定
            var responseJson = SetupPushResponse(clientObjects, true);

            var pushUpdateRequestQueue = new Queue<NbBatchRequest>();

            // internal methodのモック化
            _mockManager.Setup(m => m.CreatePushRequest(It.Is<IEnumerable<NbOfflineObject>>(i => (i.Count() == clientFirstPushObjects.Count()))))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientFirstPushObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (CreatePushUpdateRequest(objects, pushUpdateRequestQueue)));

            _mockManager.Setup(m => m.CreatePushRequest(It.Is<IEnumerable<NbOfflineObject>>(i => (i.Count() == clientSecondPushObjects.Count()))))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientSecondPushObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (CreatePushUpdateRequest(objects, pushUpdateRequestQueue)));

            _mockManager.Setup(m => m.WaitPushUpdate(null, It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.WaitPushUpdate(It.Is<Task<IList<NbBatchResult>>>(i => (i != null)), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    var batchResult = task.Result;
                    Assert.AreEqual(0, batchResult.Count);
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.PushUpdate(It.IsAny<string>(), It.IsAny<NbBatchRequest>(), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, NbBatchRequest batch, IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(pushUpdateRequestQueue.Dequeue().Json.ToString(), batch.Json.ToString());

                   var response = responseJson.First();
                   responseJson.Remove(response);
                   CheckEqualResponseAndBatchResult(response, results);

                   Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);

               }).Returns(new List<NbBatchResult>());

            _mockManager.Setup(m => m.UpdateLastSyncTime(It.IsAny<string>()))
               .Callback((string bucketName) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
               });

            // test
            var result = await _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
            // 失敗件数
            Assert.AreEqual(0, result.Count);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Push PushDivideNumber+1件 処理失敗のデータ有り
        /// </summary>
        [Test]
        public async void TestPushSubnormalPushDivideNumberPlusOneObjectsWithFailedResults()
        {
            var objectCount = NbObjectSyncManager.PushDivideNumber + 1;

            var clientObjects = InsertClientObjects(objectCount);
            var clientFirstPushObjects = clientObjects.Take(NbObjectSyncManager.PushDivideNumber);
            var clientSecondPushObjects = clientObjects.Skip(NbObjectSyncManager.PushDivideNumber).Take(NbObjectSyncManager.PushDivideNumber);

            // Pushのレスポンス生成・設定
            var responseJson = SetupPushResponse(clientObjects, false);

            var pushUpdateRequestQueue = new Queue<NbBatchRequest>();

            // internal methodのモック化
            _mockManager.Setup(m => m.CreatePushRequest(It.Is<IEnumerable<NbOfflineObject>>(i => (i.Count() == clientFirstPushObjects.Count()))))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientFirstPushObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (CreatePushUpdateRequest(objects, pushUpdateRequestQueue)));

            _mockManager.Setup(m => m.CreatePushRequest(It.Is<IEnumerable<NbOfflineObject>>(i => (i.Count() == clientSecondPushObjects.Count()))))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientSecondPushObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (CreatePushUpdateRequest(objects, pushUpdateRequestQueue)));

            _mockManager.Setup(m => m.WaitPushUpdate(null, It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.WaitPushUpdate(It.Is<Task<IList<NbBatchResult>>>(i => (i != null && i.Result.Count == 2)), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    var batchResult = task.Result;
                    Assert.AreEqual(0, failedResults.Count);

                    foreach (var aBatchResult in batchResult)
                    {
                        if (aBatchResult.Result != NbBatchResult.ResultCode.Ok)
                        {
                            failedResults.Add(aBatchResult);
                        }
                    }
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.WaitPushUpdate(It.Is<Task<IList<NbBatchResult>>>(i => (i != null && i.Result.Count == 1)), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    var batchResult = task.Result;
                    Assert.AreEqual(2, failedResults.Count);

                    foreach (var aBatchResult in batchResult)
                    {
                        if (aBatchResult.Result != NbBatchResult.ResultCode.Ok)
                        {
                            failedResults.Add(aBatchResult);
                        }
                    }
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.PushUpdate(It.IsAny<string>(), It.Is<NbBatchRequest>(x => (x.Requests.Count == NbObjectSyncManager.PushDivideNumber)), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, NbBatchRequest batch, IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(pushUpdateRequestQueue.Dequeue().Json.ToString(), batch.Json.ToString());

                   Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);

               }).Returns(CreateFailedBatch(2));

            _mockManager.Setup(m => m.PushUpdate(It.IsAny<string>(), It.Is<NbBatchRequest>(x => (x.Requests.Count == 1)), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, NbBatchRequest batch, IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(pushUpdateRequestQueue.Dequeue().Json.ToString(), batch.Json.ToString());

                   Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);

               }).Returns(CreateFailedBatch(1));

            // test
            var result = await _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
            // 失敗件数
            Assert.AreEqual(3, result.Count);

            _mockManager.Verify(m => m.UpdateLastSyncTime(It.IsAny<string>()), Times.Never);
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Push DBからの読み込み後、実オブジェクトの取得ができない
        /// </summary>
        [Test]
        public async void TestPushSubnormalReadZeroObject()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);

            var objectId = "1234567890";
            var objectIdsFromDb = new List<string>();
            objectIdsFromDb.Add(objectId);

            // DBに1件保存
            mockObjectCache.Setup(m => m.QueryDirtyObjectIds(It.IsAny<string>()))
                .Callback((string bucketName) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                }).Returns(objectIdsFromDb);

            mockObjectCache.Setup(m => m.QueryObjectsWithIds(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Callback((string bucketName, IEnumerable<string> objectIds) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(objectIdsFromDb, objectIds);
                }).Returns(new List<NbOfflineObject>()); // 空のListを返却

            // test
            var result = await _manager.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
            // 失敗件数
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// Push Batch処理で例外発生
        /// </summary>
        [Test]
        public async void TestPushExceptionBadRequest()
        {
            var objectCount = 1;

            var clientObjects = InsertClientObjects(objectCount);

            // Pushのレスポンス生成・設定
            var responseMessage = new NbJsonObject();
            responseMessage.Add("error", "errorMessage");
            var response = new MockRestResponse(HttpStatusCode.BadRequest, responseMessage.ToString());
            _restExecutor.AddResponse(response);

            // internal methodのモック化
            _mockManager.Setup(m => m.CreatePushRequest(It.IsAny<IEnumerable<NbOfflineObject>>()))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (CreatePushUpdateRequest(objects)));

            // test
            try
            {
                var result = await _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
                Assert.Fail("unexpectedly success");
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
            }

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Push 1件 タスクのwait中に例外発生
        /// </summary>
        [Test]
        public async void TestPushExceptionFailedToWaitTask()
        {
            var objectCount = 1;

            var clientObjects = InsertClientObjects(objectCount);

            // Pushのレスポンス生成・設定 
            var responseJson = SetupPushResponse(clientObjects, true);

            NbBatchRequest pushUpdateRequest = null;

            // internal methodのモック化
            _mockManager.Setup(m => m.CreatePushRequest(It.IsAny<IEnumerable<NbOfflineObject>>()))
               .Callback((IEnumerable<NbOfflineObject> objects) =>
               {
                   Assert.AreEqual(clientObjects.Count(), objects.Count());
               }).Returns((IEnumerable<NbOfflineObject> objects) => (pushUpdateRequest = CreatePushUpdateRequest(objects)));

            _mockManager.Setup(m => m.WaitPushUpdate(null, It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    Assert.AreEqual(0, failedResults.Count);
                }).Returns(Task.FromResult(0));

            _mockManager.Setup(m => m.WaitPushUpdate(It.Is<Task<IList<NbBatchResult>>>(i => (i != null)), It.IsAny<List<NbBatchResult>>()))
                .Callback((Task<IList<NbBatchResult>> task, List<NbBatchResult> failedResults) =>
                {
                    var batchResult = task.Result;
                    Assert.AreEqual(0, batchResult.Count);
                    Assert.AreEqual(0, failedResults.Count);
                }).Throws(new InvalidOperationException());

            _mockManager.Setup(m => m.PushUpdate(It.IsAny<string>(), It.IsAny<NbBatchRequest>(), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
               .Callback((string bucketName, NbBatchRequest batch, IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
               {
                   Assert.AreEqual(TestBucket, bucketName);
                   Assert.AreEqual(pushUpdateRequest.Json.ToString(), batch.Json.ToString());

                   var response = responseJson.First();
                   responseJson.Remove(response);
                   CheckEqualResponseAndBatchResult(response, results);

                   Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);

               }).Returns(new List<NbBatchResult>());

            // test
            try
            {
                var result = await _mockManager.Object.Push(TestBucket, NbObjectConflictResolver.PreferServerResolver);
            }
            catch (InvalidOperationException)
            {
                // 期待動作
            }

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// UpdateLastSyncTime
        /// </summary>
        [Test]
        public void TestUpdateLastSyncTimeNormal()
        {
            DateTime time = new DateTime(2016, 1, 2).ToUniversalTime();
            var expectedDate = "2016-01-01T15:00:00.000Z"; // 1/2 00:00 - 9h

            _mockManager.Setup(m => m.GetUtcNow()).Returns(time);

            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method, string value) =>
                    {
                        Assert.AreEqual(TestBucket, bucketName);
                        Assert.AreEqual(MethodNameLastSyncTime, method);
                        Assert.AreEqual(expectedDate, value);
                    })
                .Returns(1);

            // test
            _mockManager.Object.UpdateLastSyncTime(TestBucket);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// GetUtcNow
        /// </summary>
        /// <remarks>
        /// 試験毎に時刻が異なるため、前後の大小比較のみ行う<br/>
        /// また、DateTimeをそのまま比較すると、秒単位での比較となるためTicksで比較する。
        /// </remarks>
        [Test]
        public void TestGetUtcNowNormal()
        {
            var dateBefore = DateTime.UtcNow;
            var date = _manager.GetUtcNow();
            var dateAfter = DateTime.UtcNow;

            Assert.True(dateBefore.Ticks <= date.Ticks);
            Assert.True(date.Ticks <= dateAfter.Ticks);
        }

        /// <summary>
        /// CreatePushRequest
        /// </summary>
        [Test]
        public void TestCreatePushRequestNormal()
        {
            var deleteObject = _offlineBucket.NewObject();
            deleteObject.Id = "123";
            deleteObject.Etag = "abc";
            deleteObject.Deleted = true;
            deleteObject["key"] = "value0";

            var insertObject = _offlineBucket.NewObject();
            insertObject.Id = "456";
            insertObject["key"] = "value1";

            var updateObject = _offlineBucket.NewObject();
            updateObject.Id = "789";
            updateObject.Etag = "def";
            updateObject["key"] = "value2";

            var objects = new List<NbOfflineObject>();
            objects.Add(deleteObject);
            objects.Add(insertObject);
            objects.Add(updateObject);

            // test
            var batchRequest = _manager.CreatePushRequest(objects);

            // expectedData
            var deleteJsonObj = new NbJsonObject();
            deleteJsonObj[NbBatchRequest.KeyOp] = NbBatchRequest.OpDelete;
            deleteJsonObj[NbBatchRequest.KeyId] = deleteObject.Id;
            deleteJsonObj[NbBatchRequest.KeyEtag] = deleteObject.Etag;

            var insertJsonObj = new NbJsonObject();
            insertJsonObj[NbBatchRequest.KeyOp] = NbBatchRequest.OpInsert;
            insertJsonObj[NbBatchRequest.KeyData] = insertObject.ToJson();

            var updateJsonObj = new NbJsonObject();
            updateJsonObj[NbBatchRequest.KeyOp] = NbBatchRequest.OpUpdate;
            updateJsonObj[NbBatchRequest.KeyId] = updateObject.Id;
            var fullUpdate = new NbJsonObject();
            fullUpdate.Add("$full_update", updateObject.ToJson());
            updateJsonObj[NbBatchRequest.KeyData] = fullUpdate;
            updateJsonObj[NbBatchRequest.KeyEtag] = updateObject.Etag;

            var array = new NbJsonArray();
            array.Add(deleteJsonObj);
            array.Add(insertJsonObj);
            array.Add(updateJsonObj);

            var expected = new NbJsonObject();
            expected[NbBatchRequest.KeyRequests] = array;

            Debug.WriteLine(expected.ToString());
            Debug.WriteLine(batchRequest.Json.ToString());

            Assert.AreEqual(expected.ToString(), batchRequest.Json.ToString());
        }

        /// <summary>
        /// WaitPushUpdate 待機タスク無し
        /// </summary>
        [Test]
        public async void TestWaitPushUpdateNormalNoTask()
        {
            var failedResults = new List<NbBatchResult>();

            await _manager.WaitPushUpdate(null, failedResults);

            Assert.AreEqual(0, failedResults.Count);
        }

        /// <summary>
        /// WaitPushUpdate 待機タスク有り
        /// </summary>
        [Test]
        public async void TestWaitPushUpdateNormalWithTask()
        {
            var failedResults = new List<NbBatchResult>();

            IList<NbBatchResult> batchResults = new List<NbBatchResult>();
            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = "000000";
            batchResult.Result = NbBatchResult.ResultCode.Conflict;
            batchResult.Reason = NbBatchResult.ReasonCode.EtagMismatch;
            batchResult.Etag = "abcdef";
            batchResult.UpdatedAt = "2016-01-01T00:00:00.001Z";
            batchResult.Data = new NbJsonObject();

            batchResults.Add(batchResult);

            Task<IList<NbBatchResult>> task = Task<IList<NbBatchResult>>.FromResult(batchResults);
            await _manager.WaitPushUpdate(task, failedResults);

            Assert.AreEqual(1, failedResults.Count);
            Assert.AreEqual(batchResult, failedResults.First());
        }

        /// <summary>
        /// PushUpdate 
        /// </summary>
        [Test]
        public void TestPushUpdateNormal()
        {
            var batchRequest = new NbBatchRequest();
            var batchResultList = new List<NbBatchResult>();
            batchResultList.Add(new NbBatchResult(new NbJsonObject()));
            batchResultList.Add(new NbBatchResult(new NbJsonObject()));

            _mockManager.Setup(m => m.PushProcessResults(It.IsAny<string>(), It.IsAny<NbBatchRequest>(), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
           .Callback((string bucketName, NbBatchRequest batch,
                   IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
           {
               Assert.AreEqual(TestBucket, bucketName);
               Assert.AreEqual(batchRequest, batch);
               Assert.AreEqual(batchResultList, results);
               Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);
           })
           .Returns(new List<NbBatchResult>());

            // test
            var result = _mockManager.Object.PushUpdate(TestBucket, batchRequest, batchResultList, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count());

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// PushUpdate 例外発生
        /// </summary>
        [Test]
        public void TestPushUpdateException()
        {
            var batchRequest = new NbBatchRequest();
            var batchResultList = new List<NbBatchResult>();
            batchResultList.Add(new NbBatchResult(new NbJsonObject()));
            batchResultList.Add(new NbBatchResult(new NbJsonObject()));

            _mockManager.Setup(m => m.PushProcessResults(It.IsAny<string>(), It.IsAny<NbBatchRequest>(), It.IsAny<IList<NbBatchResult>>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
           .Callback((string bucketName, NbBatchRequest batch,
                   IList<NbBatchResult> results, NbObjectConflictResolver.Resolver resolver) =>
           {
               Assert.AreEqual(TestBucket, bucketName);
               Assert.AreEqual(batchRequest, batch);
               Assert.AreEqual(batchResultList, results);
               Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);
           })
           .Throws(new NullReferenceException());

            // test
            try
            {
                var result = _mockManager.Object.PushUpdate(TestBucket, batchRequest, batchResultList, NbObjectConflictResolver.PreferServerResolver);
                Assert.Fail("unexpectedly success");
            }
            catch (NullReferenceException)
            {
                // 期待動作
            }

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Insert成功
        /// </summary>
        [Test]
        public void TestPushProcessResultsNormalOkInsert()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var expectedObjectId = "012345";
            var expectedEtag = "abcde";
            var expectedUpdatedAt = "XYZ";

            var inserObject = _offlineBucket.NewObject();
            inserObject.Id = expectedObjectId;

            var batchRequest = new NbBatchRequest();
            batchRequest.AddInsertRequest(inserObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = expectedObjectId;
            batchResult.Etag = expectedEtag;
            batchResult.UpdatedAt = expectedUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.Ok;

            batchResult.Data = new NbJsonObject();
            batchResult.Data[Field.Id] = expectedObjectId;
            batchResult.Data[Field.Etag] = expectedEtag;
            batchResult.Data[Field.UpdatedAt] = expectedUpdatedAt;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(expectedObjectId, objectId);

                }).Returns((NbOfflineObject)inserObject);

            // DBへの保存実行
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    Assert.AreEqual(expectedObjectId, obj.Id);
                    Assert.AreEqual(expectedEtag, obj.Etag);
                    Assert.AreEqual(expectedUpdatedAt, obj.UpdatedAt);

                    Assert.AreEqual(NbSyncState.Sync, state);
                });

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Update成功
        /// </summary>
        [Test]
        public void TestPushProcessResultsNormalOkUpdate()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var expectedObjectId = "012345";
            var expectedEtag = "abcde";
            var expectedUpdatedAt = "XYZ";

            var updateObject = _offlineBucket.NewObject();
            updateObject.Id = expectedObjectId;

            var batchRequest = new NbBatchRequest();
            batchRequest.AddUpdateRequest(updateObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = expectedObjectId;
            batchResult.Etag = expectedEtag;
            batchResult.UpdatedAt = expectedUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.Ok;

            batchResult.Data = new NbJsonObject();
            batchResult.Data[Field.Id] = expectedObjectId;
            batchResult.Data[Field.Etag] = expectedEtag;
            batchResult.Data[Field.UpdatedAt] = expectedUpdatedAt;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(expectedObjectId, objectId);

                }).Returns((NbOfflineObject)updateObject);

            // DBへの保存実行
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    Assert.AreEqual(expectedObjectId, obj.Id);
                    Assert.AreEqual(expectedEtag, obj.Etag);
                    Assert.AreEqual(expectedUpdatedAt, obj.UpdatedAt);

                    Assert.AreEqual(NbSyncState.Sync, state);
                });

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }


        /// <summary>
        /// PushProcessResults Delete成功
        /// </summary>
        [Test]
        public void TestPushProcessResultsNormalOkDelete()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var expectedObjectId = "012345";
            var newEtag = "abcde";
            var newUpdatedAt = "XYZ";

            var deleteObject = _offlineBucket.NewObject();
            deleteObject.Id = expectedObjectId;
            deleteObject.Etag = "oldETag";
            deleteObject.UpdatedAt = "oldUpdatedAt";

            var batchRequest = new NbBatchRequest();
            batchRequest.AddDeleteRequest(deleteObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = expectedObjectId;
            batchResult.Etag = newEtag;
            batchResult.UpdatedAt = newUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.Ok;

            batchResult.Data = new NbJsonObject();
            batchResult.Data[Field.Id] = expectedObjectId;
            batchResult.Data[Field.Etag] = newEtag;
            batchResult.Data[Field.UpdatedAt] = newUpdatedAt;
            batchResult.Data[Field.Deleted] = true;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(expectedObjectId, objectId);

                }).Returns((NbOfflineObject)deleteObject);

            // DBの削除実行
            mockObjectCache.Setup(m => m.DeleteObject(It.IsAny<NbObject>()))
                .Callback((NbObject obj) =>
                {
                    Assert.AreEqual(deleteObject, obj);
                });

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults 未定義op
        /// </summary>
        [Test]
        public void TestPushProcessResultsSubnormalOkInvalidOp()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var expectedObjectId = "012345";
            var newEtag = "abcde";
            var newUpdatedAt = "XYZ";

            var deleteObject = _offlineBucket.NewObject();
            deleteObject.Id = expectedObjectId;
            deleteObject.Etag = "oldETag";
            deleteObject.UpdatedAt = "oldUpdatedAt";

            var batchRequest = new NbBatchRequest();
            batchRequest.AddDeleteRequest(deleteObject);
            // 不正なopに書き換え
            ((NbJsonObject)batchRequest.Requests[0])["op"] = "invalidOp";

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = expectedObjectId;
            batchResult.Etag = newEtag;
            batchResult.UpdatedAt = newUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.Ok;

            batchResult.Data = new NbJsonObject();
            batchResult.Data[Field.Id] = expectedObjectId;
            batchResult.Data[Field.Etag] = newEtag;
            batchResult.Data[Field.UpdatedAt] = newUpdatedAt;
            batchResult.Data[Field.Deleted] = true;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(expectedObjectId, objectId);

                }).Returns((NbOfflineObject)deleteObject);

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults 対象オブジェクト未保存
        /// </summary>
        [Test]
        public void TestPushProcessResultsSubnormalNoObject()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var expectedObjectId = "012345";
            var newEtag = "abcde";
            var newUpdatedAt = "XYZ";

            var deleteObject = _offlineBucket.NewObject();
            deleteObject.Id = expectedObjectId;
            deleteObject.Etag = "oldETag";
            deleteObject.UpdatedAt = "oldUpdatedAt";

            var batchRequest = new NbBatchRequest();
            batchRequest.AddDeleteRequest(deleteObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = expectedObjectId;
            batchResult.Etag = newEtag;
            batchResult.UpdatedAt = newUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.Ok;

            batchResult.Data = new NbJsonObject();
            batchResult.Data[Field.Id] = expectedObjectId;
            batchResult.Data[Field.Etag] = newEtag;
            batchResult.Data[Field.UpdatedAt] = newUpdatedAt;
            batchResult.Data[Field.Deleted] = true;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(expectedObjectId, objectId);

                }).Returns((NbOfflineObject)null);

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Delete NotFound
        /// </summary>
        [Test]
        public void TestPushProcessResultsSubnormalNotFoundDelete()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var expectedObjectId = "012345";
            //var newEtag = "abcde";
            //var newUpdatedAt = "XYZ";

            var deleteObject = _offlineBucket.NewObject();
            deleteObject.Id = expectedObjectId;
            deleteObject.Etag = "oldETag";
            deleteObject.UpdatedAt = "oldUpdatedAt";

            var batchRequest = new NbBatchRequest();
            batchRequest.AddDeleteRequest(deleteObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = expectedObjectId;
            //batchResult.Etag = newEtag;
            //batchResult.UpdatedAt = newUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.NotFound;

            //batchResult.Data = new NbJsonObject();
            //batchResult.Data[Field.Id] = expectedObjectId;
            //batchResult.Data[Field.Etag] = newEtag;
            //batchResult.Data[Field.UpdatedAt] = newUpdatedAt;
            //batchResult.Data[Field.Deleted] = true;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(expectedObjectId, objectId);

                }).Returns((NbOfflineObject)deleteObject);

            // DBの削除実行
            mockObjectCache.Setup(m => m.DeleteObject(It.IsAny<NbObject>()))
                .Callback((NbObject obj) =>
                {
                    Assert.AreEqual(deleteObject, obj);
                });

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Insert/Update NotFound
        /// </summary>
        [Test]
        public void TestPushProcessResultsSubnormalNotFoundInsertUpdate()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);

            var insertObject = _offlineBucket.NewObject();
            insertObject.Id = "12345";

            var updateObject = _offlineBucket.NewObject();
            updateObject.Id = "6789";
            updateObject.Etag = "oldETag-u";
            updateObject.UpdatedAt = "oldUpdatedAt-u";

            // request
            var batchRequest = new NbBatchRequest();
            batchRequest.AddInsertRequest(insertObject);
            batchRequest.AddUpdateRequest(updateObject);

            // Result
            var batchResultInsert = new NbBatchResult(new NbJsonObject());
            batchResultInsert.Id = insertObject.Id;
            batchResultInsert.Result = NbBatchResult.ResultCode.NotFound;

            var batchResultUpdate = new NbBatchResult(new NbJsonObject());
            batchResultUpdate.Id = updateObject.Id;
            batchResultUpdate.Result = NbBatchResult.ResultCode.NotFound;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResultInsert);
            batchResults.Add(batchResultUpdate);

            // DBのデータ取得(Insert)
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.Is<string>(s => s == batchResultInsert.Id)))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                }).Returns((NbOfflineObject)insertObject);
            // DBのデータ取得(Update)
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.Is<string>(s => s == batchResultUpdate.Id)))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                }).Returns((NbOfflineObject)insertObject);

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(insertObject.Id, result[0].Id);
            Assert.AreEqual(NbBatchResult.ResultCode.NotFound, result[0].Result);
            Assert.AreEqual(updateObject.Id, result[1].Id);
            Assert.AreEqual(NbBatchResult.ResultCode.NotFound, result[1].Result);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Forbidden/BadRequest/ServerError/Unknown
        /// </summary>
        [Test]
        public void TestPushProcessResultsSubnormalVariousErrors()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);

            // objects 
            var objects = new List<NbOfflineObject>();

            // request
            var batchRequest = new NbBatchRequest();
            // result
            var batchResults = new List<NbBatchResult>();

            for (int i = 0; i < 4; i++)
            {
                var insertObject = _offlineBucket.NewObject();
                insertObject.Id = i.ToString();

                objects.Add(insertObject);
                // request
                batchRequest.AddInsertRequest(insertObject);

                // Result
                var batchResultInsert = new NbBatchResult(new NbJsonObject());
                batchResultInsert.Id = insertObject.Id;
                batchResultInsert.Result = NbBatchResult.ResultCode.Ok;

                batchResults.Add(batchResultInsert);
            }

            batchResults[0].Result = NbBatchResult.ResultCode.Forbidden;
            batchResults[1].Result = NbBatchResult.ResultCode.BadRequest;
            batchResults[2].Result = NbBatchResult.ResultCode.ServerError;
            batchResults[3].Result = NbBatchResult.ResultCode.Unknown;

            // DBのデータ取得(Insert)
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                })
                // Idが一致するオブジェクトを返却
                .Returns((string bucketName, string objectId) => (from x in objects where x.Id == objectId select x).First());

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual("0", result[0].Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, result[0].Result);
            Assert.AreEqual("1", result[1].Id);
            Assert.AreEqual(NbBatchResult.ResultCode.BadRequest, result[1].Result);
            Assert.AreEqual("2", result[2].Id);
            Assert.AreEqual(NbBatchResult.ResultCode.ServerError, result[2].Result);
            Assert.AreEqual("3", result[3].Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Unknown, result[3].Result);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Conflict サーバ優先
        /// </summary>
        [Test]
        public void TestPushProcessResultsNormalConflictPreferServer()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var ojectId = "012345";
            var clientEtag = "abcde";
            var clientUpdatedAt = "ABC";

            var serverEtag = "fghij";
            var serverUpdatedAt = "XYZ";

            var clientObject = _offlineBucket.NewObject();
            clientObject.Id = ojectId;
            clientObject.Etag = clientEtag;
            clientObject.UpdatedAt = clientUpdatedAt;


            var batchRequest = new NbBatchRequest();
            batchRequest.AddUpdateRequest(clientObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = ojectId;
            batchResult.Etag = serverEtag;
            batchResult.UpdatedAt = serverUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.Conflict;
            batchResult.Reason = NbBatchResult.ReasonCode.EtagMismatch;

            batchResult.Data = new NbJsonObject();
            batchResult.Data[Field.Id] = ojectId;
            batchResult.Data[Field.Etag] = serverEtag;
            batchResult.Data[Field.UpdatedAt] = serverUpdatedAt;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(ojectId, objectId);

                }).Returns(clientObject);

            // DBへの保存実行
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    Assert.AreEqual(ojectId, obj.Id);
                    Assert.AreEqual(serverEtag, obj.Etag);
                    Assert.AreEqual(serverUpdatedAt, obj.UpdatedAt);

                    Assert.AreEqual(NbSyncState.Sync, state);
                });

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(0, result.Count);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Conflict クライアント優先
        /// </summary>
        [Test]
        public void TestPushProcessResultsNormalConflictPreferClient()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var targetObjectId = "012345";
            var clientEtag = "abcde";
            var clientUpdatedAt = "ABC";

            var serverEtag = "fghij";
            var serverUpdatedAt = "XYZ";

            var clientObject = _offlineBucket.NewObject();
            clientObject.Id = targetObjectId;
            clientObject.Etag = clientEtag;
            clientObject.UpdatedAt = clientUpdatedAt;


            var batchRequest = new NbBatchRequest();
            batchRequest.AddUpdateRequest(clientObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = targetObjectId;
            batchResult.Etag = serverEtag;
            batchResult.UpdatedAt = serverUpdatedAt;
            batchResult.Result = NbBatchResult.ResultCode.Conflict;
            batchResult.Reason = NbBatchResult.ReasonCode.EtagMismatch;

            batchResult.Data = new NbJsonObject();
            batchResult.Data[Field.Id] = targetObjectId;
            batchResult.Data[Field.Etag] = serverEtag;
            batchResult.Data[Field.UpdatedAt] = serverUpdatedAt;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(objectId, objectId);

                }).Returns(clientObject);

            // DBへの保存実行
            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    Assert.AreEqual(targetObjectId, obj.Id);
                    Assert.AreEqual(serverEtag, obj.Etag); // server側のEtagに付け替え
                    Assert.AreEqual(clientUpdatedAt, obj.UpdatedAt);

                    Assert.AreEqual(NbSyncState.Dirty, state);
                });

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, result.Count);

            var failedResult = result.First();
            Assert.AreEqual(targetObjectId, failedResult.Id);
            Assert.AreEqual(serverEtag, failedResult.Etag);
            Assert.AreEqual(serverUpdatedAt, failedResult.UpdatedAt);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults Conflict ObjectId重複 Data通知無し
        /// </summary>
        [Test]
        public void TestPushProcessResultsSubnormalConflictWithNoData()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var targetObjectId = "012345";

            var clientObject = _offlineBucket.NewObject();
            clientObject.Id = targetObjectId;

            var batchRequest = new NbBatchRequest();
            batchRequest.AddInsertRequest(clientObject);

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Result = NbBatchResult.ResultCode.Conflict;
            batchResult.Reason = NbBatchResult.ReasonCode.DuplicateId;
            batchResult.Data = null;

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(objectId, objectId);

                }).Returns(clientObject);

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, result.Count);

            var failedResult = result.First();
            Assert.AreEqual(targetObjectId, failedResult.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, failedResult.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.DuplicateId, failedResult.Reason);
            Assert.IsNull(failedResult.Data);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// PushProcessResults insert失敗によりResultのIdが未採番
        /// </summary>
        [Test]
        public void TestPushProcessResultsSubnormalInsertFailed()
        {
            var mockObjectCache = InjectMockObjectCache(_manager);
            var targetObjectId = "012345";
            string clientEtag = null; // ローカルに保存したのみなので未採番
            string clientUpdatedAt = null; // ローカルに保存したのみなので未採番

            var clientObject = _offlineBucket.NewObject();
            clientObject.Id = targetObjectId; // ローカルでは採番済み
            clientObject.Etag = clientEtag;
            clientObject.UpdatedAt = clientUpdatedAt;

            var batchRequest = new NbBatchRequest();
            batchRequest.AddInsertRequest(clientObject); // 新規作成

            var batchResult = new NbBatchResult(new NbJsonObject());
            batchResult.Id = null; // Inser失敗のためId未採番
            batchResult.Etag = null; // Inser失敗のため未採番
            batchResult.UpdatedAt = null; // Inser失敗のため未採番
            batchResult.Result = NbBatchResult.ResultCode.Forbidden;
            batchResult.Reason = NbBatchResult.ReasonCode.Unknown;
            // Insertに失敗したためDataは無し

            var batchResults = new List<NbBatchResult>();
            batchResults.Add(batchResult);

            // DBのデータ取得
            mockObjectCache.Setup(m => m.FindObject<NbOfflineObject>(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string objectId) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(objectId, objectId);

                }).Returns(clientObject);

            // test
            var result = _manager.PushProcessResults(TestBucket, batchRequest, batchResults, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(1, result.Count);

            var failedResult = result.First();
            Assert.AreEqual(targetObjectId, failedResult.Id); // Requestに登録されたオブジェクトが使用される
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, failedResult.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.Unknown, failedResult.Reason);
            // insert失敗のため、通知されないデータはnullとなる
            Assert.IsNull(failedResult.Etag);
            Assert.IsNull(failedResult.UpdatedAt);
            Assert.IsNull(failedResult.Data);

            mockObjectCache.VerifyAll();
        }

        // バッチリクエストはSDK内で生成し、ユーザが参照できないため、indexの正当性は検証しない
        /// <summary>
        /// GetObjectIdFromBatchRequest Batchリクエスト中にData/Id有り
        /// </summary>
        [Test]
        public void TestGetObjectIdFromBatchRequestNormal()
        {
            var targetObjectId = "012345";

            var clientObject = _offlineBucket.NewObject();
            clientObject.Id = targetObjectId; // ローカルでは採番済み
            clientObject.Etag = null;
            clientObject.UpdatedAt = null;

            var batchRequest = new NbBatchRequest();
            batchRequest.AddInsertRequest(clientObject); // 新規作成

            // test
            var result = NbObjectSyncManager.GetObjectIdFromBatchRequest(0, batchRequest);

            Assert.AreEqual(clientObject.Id, result);
        }

        /// <summary>
        /// GetObjectIdFromBatchRequest Batchリクエスト中にData無し
        /// </summary>
        [Test]
        public void TestGetObjectIdFromBatchRequestSubnormalNoData()
        {
            var targetObjectId = "012345";

            var clientObject = _offlineBucket.NewObject();
            clientObject.Id = targetObjectId; // ローカルでは採番済み
            clientObject.Etag = null;
            clientObject.UpdatedAt = null;

            var batchRequest = new NbBatchRequest();
            batchRequest.AddInsertRequest(clientObject); // 新規作成
            // 入力したDataデータを消去
            ((NbJsonObject)batchRequest.Requests[0])[NbBatchRequest.KeyData] = null;

            // test
            var result = NbObjectSyncManager.GetObjectIdFromBatchRequest(0, batchRequest);

            Assert.IsNull(result);
        }


        // -------------------------------------------------------

        private List<NbJsonObject> SetupPushResponse(IEnumerable<NbOfflineObject> objects, bool successOnly)
        {
            var list = new List<NbJsonObject>();
            var workList = objects.ToList();
            var counter = 0;

            while (workList.Count != 0)
            {
                var pushObjects = workList.Take(NbObjectSyncManager.PushDivideNumber);
                workList = workList.Skip(NbObjectSyncManager.PushDivideNumber).ToList();

                NbJsonObject pushResponse;
                if (successOnly)
                {
                    pushResponse = CreateBatchAsyncResponseJson(pushObjects);
                }
                else
                {
                    pushResponse = CreateBatchAsyncResponseWithFailJson(pushObjects);
                }

                var response = new MockRestResponse(HttpStatusCode.OK, pushResponse.ToString());
                _restExecutor.AddResponse(response);

                list.Add(pushResponse);
                counter++;
            }

            return list;
        }

        private NbJsonObject CreateBatchAsyncResponseJson(IEnumerable<NbOfflineObject> objects)
        {
            var array = new NbJsonArray();
            foreach (NbOfflineObject obj in objects)
            {
                var json = new NbJsonObject();
                json.Add("result", "ok");
                json.Add("reason", "unspecified ");
                json.Add(Field.Id, obj.Id);
                json.Add(Field.Etag, obj.Etag);
                json.Add(Field.UpdatedAt, obj.UpdatedAt);
                json.Add("data", obj.ToJson());

                array.Add(json);
            }

            var baseJson = new NbJsonObject();
            baseJson.Add(Field.Results, array);

            return baseJson;
        }

        private NbJsonObject CreateBatchAsyncResponseWithFailJson(IEnumerable<NbOfflineObject> objects)
        {
            var array = new NbJsonArray();
            var counter = 0;
            foreach (NbOfflineObject obj in objects)
            {
                var json = new NbJsonObject();

                json.Add("result", "ok");
                json.Add("reason", "unspecified ");
                json.Add(Field.Id, obj.Id);
                json.Add(Field.Etag, obj.Etag);
                json.Add(Field.UpdatedAt, obj.UpdatedAt);
                json.Add("data", obj.ToJson());

                switch (counter)
                {
                    case 0:
                        json["result"] = "forbidden";
                        break;
                    case 1:
                        json["result"] = "notFound";
                        break;
                    default:
                        break;
                }

                counter++;
                array.Add(json);
            }

            var baseJson = new NbJsonObject();
            baseJson.Add(Field.Results, array);

            return baseJson;
        }

        private List<NbBatchResult> CreateFailedBatch(int number)
        {
            var list = new List<NbBatchResult>();

            var counter = 0;

            for (int i = 0; i < number; i++)
            {
                var json = new NbJsonObject();

                json.Add("result", "ok");
                json.Add("reason", "unspecified ");
                json.Add(Field.Id, i.ToString());
                json.Add(Field.Etag, i.ToString());
                json.Add(Field.UpdatedAt, i.ToString());
                json.Add("data", new NbJsonObject());

                switch (counter)
                {
                    case 0:
                        json["result"] = "forbidden";
                        break;
                    case 1:
                        json["result"] = "notFound";
                        break;
                    default:
                        break;
                }

                var batch = new NbBatchResult(json);
                list.Add(batch);

                counter++;
            }

            return list;
        }

        private NbBatchRequest CreatePushUpdateRequest(IEnumerable<NbOfflineObject> objects)
        {
            var request = new NbBatchRequest();

            foreach (var obj in objects)
            {
                request.AddUpdateRequest(obj);
            }

            return request;
        }

        private NbBatchRequest CreatePushUpdateRequest(IEnumerable<NbOfflineObject> objects, Queue<NbBatchRequest> queue)
        {
            var request = new NbBatchRequest();

            foreach (var obj in objects)
            {
                request.AddUpdateRequest(obj);
            }

            queue.Enqueue(request);

            return request;
        }

        private IEnumerable<NbOfflineObject> InsertClientObjects(int objectNumber)
        {
            var cache = new NbObjectCache(NbService.Singleton);

            // DBに指定数のオブジェクトを生成
            var clientObjects = CreateOfflineObjects(objectNumber);
            foreach (NbOfflineObject obj in clientObjects)
            {
                cache.InsertObject(obj, NbSyncState.Dirty);
            }

            return clientObjects;
        }

        private void CheckEqualResponseAndBatchResult(NbJsonObject response, IList<NbBatchResult> argument)
        {
            var responseJsonArray = (NbJsonArray)response[Field.Results];
            Assert.AreEqual(responseJsonArray.Count, argument.Count);

            var index = 0;
            foreach (NbJsonObject aJsonObj in responseJsonArray)
            {
                var argumentResult = argument.ElementAt(index);
                Assert.AreEqual(aJsonObj[Field.Id], argumentResult.Id);
                Assert.AreEqual(aJsonObj[Field.Etag], argumentResult.Etag);

                Assert.AreEqual(aJsonObj[Field.UpdatedAt], argumentResult.UpdatedAt);
                Assert.AreEqual(aJsonObj["data"].ToString(), argumentResult.Data.ToString());

                Assert.AreEqual("ok", aJsonObj["result"]);
                Assert.AreEqual(NbBatchResult.ResultCode.Ok, argumentResult.Result);

                Assert.AreEqual("unspecified ", aJsonObj["reason"]);
                Assert.AreEqual(NbBatchResult.ReasonCode.Unknown, argumentResult.Reason);

                index++;
            }
        }


    }
}
