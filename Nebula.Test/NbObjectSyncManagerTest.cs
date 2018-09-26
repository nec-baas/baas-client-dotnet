using Moq;
using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    partial class NbObjectSyncManagerTest
    {
        private const string TestBucket = "TestBucket";

        // メソッド識別用定数
        private const string MethodNameSyncScope = "SyncScope";
        private const string MethodNameLastPullServerTime = "LastPullServerTime";
        private const string MethodNameLastSyncTime = "LastSyncTime";

        private const string PullObjectUpdatedTime = "2015-02-02T00:00:00.000Z";

        private const string PullLastTime = "2016-09-01T12:34:56.000Z";
        private const string PullLastOffsetTime = "2016-09-01T12:34:53.000Z";
        private const string PushCurrentTime = "2016-09-01T12:34:56.000Z";

        private NbObjectSyncManager _manager;
        private Mock<NbObjectSyncManager> _mockManager;

        private MockRestExecutor _restExecutor;

        private NbObjectBucket<NbObject> _bucket;
        private NbOfflineObjectBucket<NbOfflineObject> _offlineBucket;

        private NbDatabaseImpl _db;

        private ProcessState _processState;

        [SetUp]
        public void Setup()
        {
            TestUtils.Init();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(NbService.Singleton);
            _manager = new NbObjectSyncManager(NbService.Singleton);
            _mockManager = new Mock<NbObjectSyncManager>(NbService.Singleton) { CallBase = true };

            _restExecutor = new MockRestExecutor();

            // inject rest executor
            NbService.Singleton.RestExecutor = _restExecutor;

            _bucket = new NbObjectBucket<NbObject>(TestBucket);
            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(TestBucket);

            _db = new NbDatabaseImpl(":memory:", null);
            _db.Open();

            _processState = ProcessState.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            _db.Close();
            _db = null;
        }

        /// <summary>
        /// コンストラクタ<br/>
        /// 引数未設定の場合は、Singletonのサービスを参照する。
        /// </summary>
        [Test]
        public void TestConstractorNormalNoServiceArgument()
        {
            // test
            var localManager = new NbObjectSyncManager();

            Assert.AreSame(_manager.Service, localManager.Service);

            // ObjectCacheへのservice設定
            var type = localManager.GetType();
            var field = type.GetField("_objectCache", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            var cache = (NbObjectCache)field.GetValue(localManager);

            var cacheType = cache.GetType();
            var cacheField = cacheType.GetField("_service", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            var cacheService = (NbService)cacheField.GetValue(cache);

            Assert.AreSame(localManager.Service, cacheService);
        }

        /// <summary>
        /// コンストラクタ<br/>
        /// オフラインサービス無効の場合例外とする。
        /// </summary>
        [Test]
        public void TestConstractorExceptionOfflineServiceNotEnabled()
        {
            NbService.Singleton.OfflineService = null;
            try
            {
                // test
                _manager = new NbObjectSyncManager(NbService.Singleton);
            }
            catch (InvalidOperationException)
            {
                // 期待動作
            }
        }

        /// <summary>
        /// SyncBucketAsyncの正常動作
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormal()
        {
            var syncScope = new NbQuery();

            var batchResult = CreateBatchResult();

            IList<NbBatchResult> pushResult = new List<NbBatchResult>();
            pushResult.Add(batchResult);

            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);

                }).Returns(syncScope.ToString());
            // Pull
            _mockManager.Setup(m => m.Pull(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, NbQuery query, NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(syncScope.ToString(), query.ToString());
                    Assert.AreEqual(NbObjectConflictResolver.PreferClientResolver, resolver);
                }).Returns(Task.FromResult(1));
            // Push
            _mockManager.Setup(m => m.Push(It.IsAny<string>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(NbObjectConflictResolver.PreferClientResolver, resolver);
                }).Returns(Task.FromResult(pushResult));

            // test
            var result = await _mockManager.Object.SyncBucketAsync(TestBucket, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(pushResult, result);

            // 状態確認
            Assert.False(_processState.Syncing);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// SyncBucketAsyncの正常動作<br/>
        /// 衝突解決リゾルバが未指定の場合、Server優先とみなす
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalNoResolver()
        {
            var syncScope = new NbQuery();

            var batchResult = CreateBatchResult();

            IList<NbBatchResult> pushResult = new List<NbBatchResult>();
            pushResult.Add(batchResult);

            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);

                }).Returns(syncScope.ToString());
            // Pull
            _mockManager.Setup(m => m.Pull(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, NbQuery query, NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(syncScope.ToString(), query.ToString());
                    Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);
                }).Returns(Task.FromResult(1));
            // Push
            _mockManager.Setup(m => m.Push(It.IsAny<string>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);
                }).Returns(Task.FromResult(pushResult));

            // test
            var result = await _mockManager.Object.SyncBucketAsync(TestBucket);
            Assert.AreEqual(pushResult, result);

            // 状態確認
            Assert.False(_processState.Syncing);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// バケット名がnull
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionArgumentNullException()
        {
            try
            {
                // test
                var result = await _manager.SyncBucketAsync(null, NbObjectConflictResolver.PreferServerResolver);
                Assert.Fail("unexpectedly success");
            }
            catch (ArgumentNullException)
            {
                //期待動作
            }
        }

        /// <summary>
        /// 同期範囲未指定
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionNoSyncScope()
        {
            try
            {
                // test
                var result = await _manager.SyncBucketAsync(TestBucket);
                Assert.Fail("unexpectedly success");
            }
            catch (InvalidOperationException)
            {
                // 期待動作
            }

            // 状態確認
            Assert.False(_processState.Syncing);
        }

        /// <summary>
        /// Pullに失敗
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPullFailed()
        {
            var syncScope = new NbQuery();
            var exceptionMessage = "failed";
            var badRequestException = new NbHttpException(HttpStatusCode.BadRequest, exceptionMessage);

            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);

                }).Returns(syncScope.ToString());
            // Pull
            _mockManager.Setup(m => m.Pull(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, NbQuery query, NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(syncScope.ToString(), query.ToString());
                    Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);
                }).Throws(badRequestException);

            try
            {
                // test
                var result = await _mockManager.Object.SyncBucketAsync(TestBucket);
                Assert.Fail("unexpectedly success");
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
                Assert.AreEqual(exceptionMessage, ex.Message);
            }

            // 状態確認
            Assert.False(_processState.Syncing);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// Pushに失敗
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionPushFailed()
        {
            var exceptionMessage = "failed";
            var badRequestException = new NbHttpException(HttpStatusCode.BadRequest, exceptionMessage);

            var syncScope = new NbQuery();

            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);

                }).Returns(syncScope.ToString());
            // Pull
            _mockManager.Setup(m => m.Pull(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, NbQuery query, NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(syncScope.ToString(), query.ToString());
                    Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);
                }).Returns(Task.FromResult(1));
            // Push
            _mockManager.Setup(m => m.Push(It.IsAny<string>(), It.IsAny<NbObjectConflictResolver.Resolver>()))
                .Callback((string bucketName, NbObjectConflictResolver.Resolver resolver) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(NbObjectConflictResolver.PreferServerResolver, resolver);
                }).Throws(badRequestException);

            try
            {
                // test
                var result = await _mockManager.Object.SyncBucketAsync(TestBucket);
                Assert.Fail("unexpectedly success");
            }
            catch (NbHttpException ex)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
                Assert.AreEqual(exceptionMessage, ex.Message);
            }

            // 状態確認
            Assert.False(_processState.Syncing);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 同期中の同期
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncExceptionWhileSyncing()
        {
            var syncScope = new NbQuery();

            var batchResult = CreateBatchResult();

            IList<NbBatchResult> pushResult = new List<NbBatchResult>();
            pushResult.Add(batchResult);

            // 擬似的に同期状態を作る
            _processState.TryStartSync();

            try
            {
                // test
                var result = await _mockManager.Object.SyncBucketAsync(TestBucket);
                Assert.Fail("unexpectedly success");
            }
            catch (NbException ex)
            {
                Assert.AreEqual(NbStatusCode.Locked, ex.StatusCode);
                Assert.AreEqual("Locked.", ex.Message);
            }
            _mockManager.Verify(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            // 後始末
            _processState.EndSync();
        }

        /// <summary>
        /// 同期終了後の同期
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalAfterSyncing()
        {
            var syncScope = new NbQuery();

            var batchResult = CreateBatchResult();

            IList<NbBatchResult> pushResult = new List<NbBatchResult>();
            pushResult.Add(batchResult);

            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>())).Returns(syncScope.ToString());
            // Pull
            _mockManager.Setup(m => m.Pull(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<NbObjectConflictResolver.Resolver>())).Returns(Task.FromResult(1));
            // Push
            _mockManager.Setup(m => m.Push(It.IsAny<string>(), It.IsAny<NbObjectConflictResolver.Resolver>())).Returns(Task.FromResult(pushResult));

            // 擬似的に同期状態を作る
            _processState.TryStartSync();
            // 同期を終了させる
            _processState.EndSync();

            // test
            var result = await _mockManager.Object.SyncBucketAsync(TestBucket, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(pushResult, result);

            // 状態確認
            Assert.False(_processState.Syncing);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// CRUD中の同期
        /// </summary>
        [Test]
        public async void TestSyncBucketAsyncNormalWhileCrud()
        {
            var syncScope = new NbQuery();

            var batchResult = CreateBatchResult();

            IList<NbBatchResult> pushResult = new List<NbBatchResult>();
            pushResult.Add(batchResult);

            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>())).Returns(syncScope.ToString());
            // Pull
            _mockManager.Setup(m => m.Pull(It.IsAny<string>(), It.IsAny<NbQuery>(), It.IsAny<NbObjectConflictResolver.Resolver>())).Returns(Task.FromResult(1));
            // Push
            _mockManager.Setup(m => m.Push(It.IsAny<string>(), It.IsAny<NbObjectConflictResolver.Resolver>())).Returns(Task.FromResult(pushResult));

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            Task task = new Task(() =>
            {
                Thread.Sleep(500);
                _processState.EndCrud();
            });
            task.Start();

            // test
            // ブレークポイントをかけたら、EndCrud()より以下の方が先に実行されている
            var result = await _mockManager.Object.SyncBucketAsync(TestBucket, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(pushResult, result);

            // 状態確認
            Assert.False(_processState.Syncing);

            _mockManager.VerifyAll();

            // 念の為後始末
            _processState.EndCrud();
        }

        /// <summary>
        /// HandleConflict正常
        /// サーバ優先
        /// </summary>
        [Test]
        public void TestHandleConflictNormalPreferServer()
        {
            var serverObj = CreateDummyServerObject(TestBucket);
            var clientObj = CreateDummyClientObject(TestBucket);

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    // サーバオブジェクトの保存を期待
                    Assert.AreEqual(serverObj, obj);
                    Assert.AreEqual(NbSyncState.Sync, state);
                }).Returns(1);

            // test
            var resultObj = _manager.HandleConflict(serverObj, clientObj, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(serverObj, resultObj);
            Assert.AreEqual(ServerEtag, serverObj.Etag);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// HandleConflict正常
        /// サーバ優先、論理削除済み
        /// </summary>
        [Test]
        public void TestHandleConflictNormalPreferServerDeleted()
        {
            var serverObj = CreateDummyServerObject(TestBucket);
            var clientObj = CreateDummyClientObject(TestBucket);

            // サーバデータは論理削除済みとする
            serverObj.Deleted = true;

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            mockObjectCache.Setup(m => m.DeleteObject(It.IsAny<NbObject>()))
                .Callback((NbObject obj) =>
                {
                    // サーバオブジェクトの保存を期待
                    Assert.AreEqual(serverObj, obj);
                }).Returns(1);

            // test
            var resultObj = _manager.HandleConflict(serverObj, clientObj, NbObjectConflictResolver.PreferServerResolver);
            Assert.AreEqual(serverObj, resultObj);
            Assert.AreEqual(ServerEtag, serverObj.Etag);

            mockObjectCache.VerifyAll();
        }

        /// <summary>
        /// HandleConflict正常
        /// クライアント優先
        /// </summary>
        [Test]
        public void TestHandleConflictNormalPreferClient()
        {
            var serverObj = CreateDummyServerObject(TestBucket);
            var clientObj = CreateDummyClientObject(TestBucket);
            var dummyObj = new NbObject(TestBucket);

            // NbObjectCacheのモック設定
            var mockObjectCache = InjectMockObjectCache(_manager);

            mockObjectCache.Setup(m => m.UpdateObject(It.IsAny<NbObject>(), It.IsAny<NbSyncState>()))
                .Callback((NbObject obj, NbSyncState state) =>
                {
                    // クライアントの保存を期待
                    Assert.AreEqual(clientObj, obj);
                    Assert.AreEqual(NbSyncState.Dirty, state);
                }).Returns(1);

            // test
            var resultObj = _manager.HandleConflict(serverObj, clientObj, NbObjectConflictResolver.PreferClientResolver);
            Assert.AreEqual(clientObj, resultObj);
            // クライアントのEtagがサーバのEtagに置き換わっていること
            Assert.AreEqual(ServerEtag, clientObj.Etag);

            mockObjectCache.VerifyAll();
        }

        [Test]
        public void TestHandleConflictExceptionInvalidObject()
        {
            var serverObj = CreateDummyServerObject(TestBucket);
            var clientObj = CreateDummyClientObject(TestBucket);

            var dummyObj = new NbObject(TestBucket);

            // test
            // 不正なObjectを返却するリゾルバを実装
            try
            {
                var resultObj = _manager.HandleConflict(serverObj, clientObj, (server, client) => dummyObj);
            }
            catch (InvalidOperationException)
            {
                // 期待動作
            }
        }

        // ------------------------------------------------------------

        private NbBatchResult CreateBatchResult(
            string id = "000000000000000000000000",
            NbBatchResult.ResultCode result = NbBatchResult.ResultCode.Ok,
            NbBatchResult.ReasonCode reasonCode = NbBatchResult.ReasonCode.Unspecified,
            string etag = "00000000-0000-0000-0000-000000000000",
            NbJsonObject data = null)
        {
            var json = new NbJsonObject();
            json["_id"] = id;
            json["result"] = result.ToString();
            json["reasonCode"] = reasonCode.ToString();
            json["etag"] = etag;
            json["updatedAt"] = "2015-01-02T01:23:45.678Z";
            var localData = data ?? new NbJsonObject();
            json["data"] = localData;

            localData["_id"] = id;
            localData["etag"] = etag;
            localData["updatedAt"] = "2015-01-02T01:23:45.678Z"; ;

            var batchResult = new NbBatchResult(json);
            return batchResult;
        }

        // Id共通で別ETagを付与
        private const string ObjectId = "000000000000000000000001";
        private const string ServerEtag = "11111111-1111-1111-1111-111111111111";
        private const string ClientEtag = "00000000-0000-0000-0000-000000000000";

        private NbObject CreateDummyServerObject(string bucketName)
        {
            var serverObj = new NbObject(bucketName);
            serverObj.Id = ObjectId;
            serverObj.Etag = ServerEtag;

            return serverObj;
        }

        private NbOfflineObject CreateDummyClientObject(string bucketName)
        {
            var clientObj = new NbOfflineObject(bucketName);
            clientObj.Id = ObjectId;
            clientObj.Etag = ClientEtag;

            return clientObj;
        }

        private Mock<NbObjectCache> InjectMockObjectCache(NbObjectSyncManager manager)
        {
            var mockObjectCache = new Mock<NbObjectCache>(NbService.Singleton);
            var type = manager.GetType();
            var field = type.GetField("_objectCache", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);
            field.SetValue(manager, mockObjectCache.Object);

            return mockObjectCache;
        }

        /// <summary>
        /// 時刻の文字列にオフセットを加えた値を返却する
        /// </summary>
        /// <param name="currentTime">ベースとなる時刻</param>
        /// <param name="offsetInMilliSec">オフセット</param>
        /// <returns>オフセットを加えた時刻</returns>
        private static string GetOffsetTime(string currentTime, double offsetInMilliSec)
        {
            var currentDateTime = NbDateUtils.ParseDateTime(currentTime);
            var offsetTime = currentDateTime.AddMilliseconds(offsetInMilliSec);

            return NbDateUtils.ToString(offsetTime);
        }
    }
}
