using Moq;
using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Nec.Nebula.Test
{
    [TestFixture]
    partial class NbObjectSyncManagerTest
    {
        /// <summary>
        /// 同期範囲設定
        /// </summary>
        [Test]
        public void TestSetSyncScopeNormal()
        {
            var syncScope = new NbQuery();
            syncScope.Limit(100).Skip(200);

            // 同期範囲設定
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameSyncScope), It.Is<string>(x => x == syncScope.ToString()))).Returns(1);
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameLastSyncTime), It.Is<string>(x => x == null))).Returns(1);
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameLastPullServerTime), It.Is<string>(x => x == null))).Returns(1);

            // test
            _mockManager.Object.SetSyncScope(TestBucket, syncScope);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 同期範囲設定<br/>
        /// クエリ未指定の場合、空のクエリが設定されたとみなす
        /// </summary>
        [Test]
        public void TestSetSyncScopeNormalNoQuery()
        {
            NbQuery syncScope = null;

            var expected = new NbQuery();

            // 同期範囲設定
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameSyncScope), It.Is<string>(x => x == expected.ToString()))).Returns(1);
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameLastSyncTime), It.Is<string>(x => x == null))).Returns(1);
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameLastPullServerTime), It.Is<string>(x => x == null))).Returns(1);

            // test
            _mockManager.Object.SetSyncScope(TestBucket, syncScope);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 同期範囲設定<br/>
        /// バケット名未指定
        /// </summary>
        [Test]
        public void TestSetSyncScopeExceptionBucketNameNull()
        {
            // test
            try
            {
                _manager.SetSyncScope(null);
                Assert.Fail("unexpectedly success");
            }
            catch (ArgumentNullException)
            {
                // 期待動作
            }
        }

        /// <summary>
        /// 同期範囲取得
        /// </summary>
        [Test]
        public void TestGetSyncScopeNormal()
        {
            var syncScope = new NbQuery();
            syncScope.GreaterThan("key", 10).Limit(1000).OrderBy("key2");

            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(MethodNameSyncScope, method);
                }).Returns(syncScope.ToString());

            // test
            var resultScope = _mockManager.Object.GetSyncScope(TestBucket);

            Assert.AreNotSame(syncScope, resultScope);
            Assert.AreEqual(syncScope.ToString(), resultScope.ToString());

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 同期範囲未設定
        /// </summary>
        [Test]
        public void TestGetSyncScopeNormalNoSyncScope()
        {
            // 同期範囲取得
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(MethodNameSyncScope, method);
                }).Returns<object>(null);

            // test
            var resultScope = _mockManager.Object.GetSyncScope(TestBucket);

            Assert.IsNull(resultScope);

            _mockManager.VerifyAll();
        }


        /// <summary>
        /// 同期範囲取得<br/>
        /// バケット名未指定
        /// </summary>
        [Test]
        public void TestGetSyncScopeExceptionBucketNameNull()
        {
            // test
            try
            {
                _manager.GetSyncScope(null);
                Assert.Fail("unexpectedly success");
            }
            catch (ArgumentNullException)
            {
                // 期待動作
            }
        }

        /// <summary>
        /// 同期範囲削除
        /// </summary>
        [Test]
        public void TestRemoveSyncScopeNormal()
        {
            // 同期範囲設定
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameSyncScope), It.Is<string>(x => x == null))).Returns(1);
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameLastSyncTime), It.Is<string>(x => x == null))).Returns(1);
            _mockManager.Setup(m => m.SetObjectBucketCacheData(It.Is<string>(x => x == TestBucket), It.Is<string>(x => x == MethodNameLastPullServerTime), It.Is<string>(x => x == null))).Returns(1);

            // test
            _mockManager.Object.RemoveSyncScope(TestBucket);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 同期範囲削除<br/>
        /// バケット名未指定
        /// </summary>
        [Test]
        public void TestRemoveSyncScopeExceptionBucketNameNull()
        {
            // test
            try
            {
                _manager.RemoveSyncScope(null);
                Assert.Fail("unexpectedly success");
            }
            catch (ArgumentNullException)
            {
                // 期待動作
            }
        }

        /// <summary>
        /// 同期範囲全取得
        /// </summary>
        [Test]
        public void TestGetSyncScopeAllNormal()
        {
            // バケットキャッシュの結果生成
            var objectBucketCacheList = new List<ObjectBucketCache>();
            for (int i = 0; i < 3; i++)
            {
                var cache = new ObjectBucketCache();
                cache.Id = i;
                cache.Name = "BucketName" + i.ToString();
                // i=1のバケットには同期範囲設定無し
                if (i != 1)
                {
                    cache.SyncScope = new NbQuery().Limit(i).ToString();
                }
                objectBucketCacheList.Add(cache);
            }

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.FindAll()).Returns(objectBucketCacheList);

            // test
            var resultList = _mockManager.Object.GetAllSyncScopes();

            // i = 0,2の要素のみ
            Assert.AreEqual(2, resultList.Count);

            var query0 = resultList["BucketName0"];
            Assert.AreEqual(new NbQuery().Limit(0).ToString(), query0.ToString());
            try
            {
                var query1 = resultList["BucketName1"];
                Assert.Fail("unexpectedly success");
            }
            catch (KeyNotFoundException)
            {
                // 期待動作
            }
            var query2 = resultList["BucketName2"];
            Assert.AreEqual(new NbQuery().Limit(2).ToString(), query2.ToString());

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 同期範囲全取得<br/>
        /// 全バケット未設定
        /// </summary>
        [Test]
        public void TestGetSyncScopeAllNormalNoBucketCache()
        {
            // バケットキャッシュの結果生成
            var objectBucketCacheList = new List<ObjectBucketCache>();

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.FindAll()).Returns(objectBucketCacheList);

            // test
            var resultList = _mockManager.Object.GetAllSyncScopes();

            Assert.AreEqual(0, resultList.Count);

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 最終同期時刻取得
        /// </summary>
        [Test]
        public void TestGetLastSyncTimeNormal()
        {
            var lastSyncTime = "2015-12-28T00:00:00.001Z";

            // 同期範囲設定
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(MethodNameLastSyncTime, method);
                }).Returns(lastSyncTime);

            // test
            var resultSyncTime = _mockManager.Object.GetLastSyncTime(TestBucket);

            Assert.AreEqual(lastSyncTime, NbDateUtils.ToString((DateTime)resultSyncTime));

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 最終同期時刻取得<br/>
        /// 同期時刻未設定
        /// </summary>
        [Test]
        public void TestGetLastSyncTimeNormalNoDate()
        {
            string lastSyncTime = null;

            // 同期範囲設定
            _mockManager.Setup(m => m.GetObjectBucketCacheData(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string method) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(MethodNameLastSyncTime, method);
                }).Returns(lastSyncTime);

            // test
            var resultSyncTime = _mockManager.Object.GetLastSyncTime(TestBucket);

            Assert.IsNull(resultSyncTime);

            _mockManager.VerifyAll();
        }

        /// <summary>
        /// 最終同期時刻取得<br/>
        /// バケット名null
        /// </summary>
        [Test]
        public void TestGetLastSyncTimeExceptionBucketNameNull()
        {
            // test
            try
            {
                var resultSyncTime = _manager.GetLastSyncTime(null);
                Assert.Fail("unexpectedly success");
            }
            catch (ArgumentNullException)
            {
                // 期待動作
            }
        }

        /// <summary>
        /// バケットキャッシュ設定<br/>
        /// 同期範囲設定
        /// </summary>
        [Test]
        public void TestSetObjectBucketCacheDataNormalSyncScope()
        {
            var syncScope = new NbQuery().LessThan("key", 10);

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.SaveSyncScope(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string scope) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(syncScope.ToString(), scope);
                }).Returns(1);

            // test
            var result = _mockManager.Object.SetObjectBucketCacheData(TestBucket, MethodNameSyncScope, syncScope.ToString());
            Assert.AreEqual(1, result);

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// バケットキャッシュ設定<br/>
        /// Pull時刻
        /// </summary>
        [Test]
        public void TestSetObjectBucketCacheDataNormalLastPullTime()
        {
            var lastPullTime = "2015-12-28T00:00:00.001Z";

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.SaveLastPullServerTime(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string lastTime) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(lastPullTime, lastTime);
                }).Returns(1);

            // test
            var result = _mockManager.Object.SetObjectBucketCacheData(TestBucket, MethodNameLastPullServerTime, lastPullTime);
            Assert.AreEqual(1, result);

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// バケットキャッシュ設定<br/>
        /// 前回同期時刻
        /// </summary>
        [Test]
        public void TestSetObjectBucketCacheDataNormalLastSyncTime()
        {
            var lastSyncTime = "2015-12-28T00:00:00.001Z";

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.SaveLastSyncTime(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string bucketName, string lastTime) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                    Assert.AreEqual(lastSyncTime, lastTime);
                }).Returns(1);

            // test
            var result = _mockManager.Object.SetObjectBucketCacheData(TestBucket, MethodNameLastSyncTime, lastSyncTime);
            Assert.AreEqual(1, result);

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// バケットキャッシュ設定<br/>
        /// メソッド名不正
        /// </summary>
        [Test]
        public void TestSetObjectBucketCacheDataExceptionInvalidMethod()
        {
            // test
            try
            {
                var result = _manager.SetObjectBucketCacheData(TestBucket, "invalidMethod", "value");
                Assert.Fail("unexpectedly success");
            }
            catch (InvalidOperationException)
            {
                // 期待動作
            }
        }

        /// <summary>
        /// バケットキャッシュ取得<br/>
        /// 同期範囲
        /// </summary>
        [Test]
        public void TestGetObjectBucketCacheDataNormalSyncScope()
        {
            var syncScope = new NbQuery().LessThan("key", 10);

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.GetSyncScope(It.IsAny<string>()))
                .Callback((string bucketName) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                }).Returns(syncScope.ToString());

            // test
            var result = _mockManager.Object.GetObjectBucketCacheData(TestBucket, MethodNameSyncScope);
            Assert.AreEqual(syncScope.ToString(), result);

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// バケットキャッシュ取得<br/>
        /// Pull時刻
        /// </summary>
        [Test]
        public void TestGetObjectBucketCacheDataNormalLastPullTime()
        {
            var lastPullTime = "2015-12-28T00:00:00.001Z";

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.GetLastPullServerTime(It.IsAny<string>()))
                .Callback((string bucketName) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                }).Returns(lastPullTime);

            // test
            var result = _mockManager.Object.GetObjectBucketCacheData(TestBucket, MethodNameLastPullServerTime);
            Assert.AreEqual(lastPullTime, result);

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// バケットキャッシュ取得<br/>
        /// 前回同期時刻
        /// </summary>
        [Test]
        public void TestGetObjectBucketCacheDataNormalLastSyncTime()
        {
            var lastSyncTime = "2015-12-28T00:00:00.001Z";

            // DAOのモック生成
            var mockDao = InjectMockBucketCacheDao(_mockManager);
            mockDao.Setup(m => m.GetLastSyncTime(It.IsAny<string>()))
                .Callback((string bucketName) =>
                {
                    Assert.AreEqual(TestBucket, bucketName);
                }).Returns(lastSyncTime);

            // test
            var result = _mockManager.Object.GetObjectBucketCacheData(TestBucket, MethodNameLastSyncTime);
            Assert.AreEqual(lastSyncTime, result);

            mockDao.VerifyAll();
            _mockManager.VerifyAll();
        }

        /// <summary>
        /// バケットキャッシュ取得<br/>
        /// メソッド名不正
        /// </summary>
        [Test]
        public void TestGretObjectBucketCacheDataExceptionInvalidMethod()
        {
            // test
            try
            {
                var result = _manager.GetObjectBucketCacheData(TestBucket, "invalidMethod");
                Assert.Fail("unexpectedly success");
            }
            catch (InvalidOperationException)
            {
                // 期待動作
            }
        }

        // ---------------------------------

        private Mock<ObjectBucketCacheDao> InjectMockBucketCacheDao(Mock<NbObjectSyncManager> manager)
        {
            using (var dbContext = _db.CreateDbContext())
            {
                var mockDao = new Mock<ObjectBucketCacheDao>(dbContext);
                // モックDAO取得処理
                manager.Setup(m => m.CreateCacheDao(It.IsAny<NbManageDbContext>()))
                    .Callback((NbManageDbContext context) =>
                    {
                        Assert.IsNotNull(context);
                    }).Returns(mockDao.Object);

                return mockDao;
            }
        }

    }
}
