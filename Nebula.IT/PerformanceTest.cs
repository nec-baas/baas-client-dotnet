using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class PerformanceTest
    {
        private NbOfflineObjectBucket<NbOfflineObject> mBucket;
        private NbObjectBucket<NbObject> mOnlineBucket;
        private NbFileBucket mFileBucket;
        private Stopwatch mSw;
        private const int DataCount = 10000;

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineUser().Wait();
            ITUtil.InitOnlineObjectStorage().Wait();
            ITUtil.InitOnlineFileStorage().Wait();
            TryDeleteDbFile();

            SwitchOfflineService(true);
            mBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);
            mOnlineBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);
            mFileBucket = new NbFileBucket(ITUtil.FileBucketName);
            mSw = new Stopwatch();
        }

        [TearDown]
        public void TearDown()
        {
            SwitchOfflineService(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            ITUtil.InitOnlineUser().Wait();
            TryDeleteDbFile();
        }

        private void SwitchOfflineService(bool enable)
        {
            if (enable)
            {
                NbOfflineService.EnableOfflineService(NbService.Singleton);
            }
            else
            {
                NbService.Singleton.DisableOffline();
            }
        }


        /**
         * ユーザ管理（性能）
         **/

        /// <summary>
        /// ユーザ管理 性能評価
        /// 最大同時ログインユーザ数が１であること
        /// </summary>
        [Test]
        public void TestUserLoginMaxCount()
        {
            var NewUserName = ITUtil.Username + "2";
            var NewEmail = ITUtil.Email + "2";
            ITUtil.SignUpUser(ITUtil.Username, ITUtil.Email).Wait();
            ITUtil.SignUpUser(NewUserName, NewEmail).Wait();
            var user = NbOfflineUser.LoginWithEmailAsync(ITUtil.Email, ITUtil.Password).Result;
            var caches = GetLoginCach();
            Assert.IsNotEmpty(caches);
            Assert.AreEqual(1, caches.Count());
            Assert.AreEqual(caches[0].UserName, ITUtil.Username);
            Assert.AreEqual(caches[0].Email, ITUtil.Email);

            user = NbOfflineUser.LoginWithEmailAsync(NewEmail, ITUtil.Password).Result;
            caches = GetLoginCach();
            Assert.IsNotEmpty(caches);
            Assert.AreEqual(1, caches.Count());
            Assert.AreEqual(caches[0].UserName, NewUserName);
            Assert.AreEqual(caches[0].Email, NewEmail);

            try
            {
                user = NbOfflineUser.LoginWithEmailAsync(ITUtil.Email, ITUtil.Password, NbUser.LoginMode.Offline).Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
            }
        }

        private List<LoginCache> GetLoginCach()
        {
            if (!NbService.Singleton.IsOfflineEnabled()) return null;
            var db = (NbDatabaseImpl)NbService.Singleton.OfflineService.Database;
            using (var dbContext = db.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);
                var logincaches = dao.FindAll();
                return logincaches.ToList();
            }
        }

        private long elapsedTimesFirst;
        private List<long> elapsedTimes = new List<long>();

        /**
          * ローカルデータベース（性能）
          **/

        private const int TimeSpecQuery = 1100; // ms
        private const int TimeSpec = 300; // ms

        /// <summary>
        /// <para>ローカルデータベース 性能評価</para>
        /// <para>10000件キャッシュされている状態で100件検索する</para>
        /// <para>10000件キャッシュされている状態で1件作成する</para>
        /// <para>10000件キャッシュされている状態で1件更新する</para>
        /// <para>10000件キャッシュされている状態で1件削除する</para>
        /// </summary>
        /// <remarks>
        /// <para>通常はテスト対象外。テストする場合は、[Ignore]を消して、手動実行すること</para>
        /// <para>「TestData_1KB_10000Item.csv」はテスト実行ファイルと同じディレクトリに格納する</para>
        /// </remarks>

        [Ignore]
        [Test]
        public void TestLocalDBPerfomance()
        {
            int loopCount = 5;
            int inCount = DataCount / 100;
            ImportCSV();
            var results = mBucket.QueryAsync(new NbQuery()).Result;
            Assert.AreEqual(DataCount, results.Count());

            var inList = new List<string>();
            for (int i = 0; i < inCount; i++)
            {
                inList.Add(((i + 1) * 100).ToString());
            }

            elapsedTimesFirst = 0;
            var query = new NbQuery().In("NO", inList.ToArray());
            for (int i = 0; i < loopCount; i++)
            {
                mSw.Start();
                var queryRet = mBucket.QueryAsync(query).Result;
                mSw.Stop();
                var time = CollectResult(i);
                Debug.WriteLine("query time : " + time);
                Assert.AreEqual(inCount, queryRet.Count());
            }
            AssertAverage(TimeSpecQuery);

            Debug.WriteLine("finish QueryTest! start CreateTest");

            elapsedTimes = new List<long>();
            var objs = new List<NbOfflineObject>();
            for (int i = 0; i < loopCount; i++)
            {
                var obj = mBucket.NewObject();
                obj["key"] = "value";
                mSw.Start();
                obj = (NbOfflineObject)obj.SaveAsync().Result;
                mSw.Stop();
                objs.Add(obj);
                var time = CollectResult(i);
                Debug.WriteLine("create time : " + time);
            }
            AssertAverage(TimeSpec);

            Debug.WriteLine("finish CreateTest! start UpdateTest");

            elapsedTimes = new List<long>();
            for (int i = 0; i < loopCount; i++)
            {
                objs[i]["key2"] = "value2";
                mSw.Start();
                var result = (NbOfflineObject)objs[i].SaveAsync().Result;
                mSw.Stop();
                var time = CollectResult(i);
                Debug.WriteLine("create time : " + time);
            }
            AssertAverage(TimeSpec);

            Debug.WriteLine("finish UpdateTest! start DeleteTest");

            elapsedTimes = new List<long>();
            for (int i = 0; i < loopCount; i++)
            {
                mSw.Start();
                objs[i].DeleteAsync(false).Wait();
                mSw.Stop();
                var time = CollectResult(i);
                Debug.WriteLine("create time : " + time);
            }
            AssertAverage(TimeSpec);

            Debug.WriteLine("finish DeleteTest!");
        }


        /**
         * オブジェクトストレージ（性能）
         * 
         * ・通常はテスト対象外。テストする場合は、[Ignore]を消して、手動実行すること。
         * ・NW負荷が少ない時に、テストすること。
         * ・CSVファイル"TestData_1KB_10000Item.csv"を以下に格納してから試験を開始すること。
         * 　(Debugビルド)   .\Nebula.IT\bin\Debug\
         * 　(Releaseビルド) .\Nebula.IT\bin\Release\
         */

        // オンラインオブジェクト件数
        private const int GuaranteedOnlineObjectCount = 10000; // 件
        // オンラインオブジェクトサイズ
        private const int GuaranteedOnlineObjectSize = 1024 * 256; // byte
        // オンライン1バケットあたりの最大保証オブジェクトサイズを持つオブジェクト件数
        private const int GuaranteedOnlineLargeObjectCountInBucket = 8; // 件
        // オンライン検索時間の期待値
        private const double TimeSpecOnlineQueryObjects = 0.3 * 1000; // ms
        // オンライン作成時間の期待値
        private const double TimeSpecOnlineCreateObject = 0.3 * 1000; // ms
        // オンライン更新時間の期待値
        private const double TimeSpecOnlineUpdateObject = 0.3 * 1000; // ms
        // オンラインバッチ時間の期待値
        private const double TimeSpecOnlineBatchObjects = 1.0 * 1000; // ms
        // オンライン削除時間の期待値
        private const double TimeSpecOnlineDeleteObject = 0.3 * 1000; // ms
        // オフラインオブジェクト件数
        private const int GuaranteedOfflineObjectCount = 10000; // 件
        // オフラインオブジェクトサイズ
        private const int GuaranteedOfflineObjectSize = 1024 * 256; // byte
        // オンライン1バケットあたりの最大保証オブジェクトサイズを持つオブジェクト件数
        private const int GuaranteedOfflineLargeObjectCountInBucket = 40; // 件
        // オフラインバケット件数
        private const int GuaranteedOfflineObjectBucket = 20; // 件
        // 初回同期完了時間の期待値
        private const int TimeSpecFirstSync = 80 * 1000; // ms
        // 差分同期完了時間の期待値
        private const int TimeSpecDiffrencialSync = 3 * 1000; // ms

        // 耐久試験の試験時間
        private const double TimeSpecDurbility = 48.0; // hour

        private const string TestData1KB = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyzABC";

        /// <summary>
        /// 動作保証値（オンライン）
        /// オブジェクト数
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineObjectCount()
        {
            Debug.WriteLine("-----------[START]TestOnlineObjectCount: " + DateTime.Now);

            for (var l = 1; l <= GuaranteedOnlineObjectCount; l++)
            {
                var id = string.Format("{0:D5}", l);
                var json = new NbJsonObject()
                {
                    {"DATA_ID", id},
                    {"DATA", TestData1KB}
                };
                Assert.AreEqual(1024, json.ToString().Length);
                var obj = new NbObject(ITUtil.ObjectBucketName, json);

                obj.SaveAsync().Wait();
            }

            var result = mOnlineBucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;

            Assert.AreEqual(GuaranteedOnlineObjectCount, result.Count());

            Debug.WriteLine("-----------[END]TestOnlineObjectCount: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オンライン）
        /// オブジェクトサイズ
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineObjectGuaranteedMaxSize()
        {
            Debug.WriteLine("-----------[START]TestOnlineObjectGuaranteedMaxSize: " + DateTime.Now);

            var sb = new StringBuilder();
            string data = null;
            while (true)
            {
                var dataLen = TestData1KB.Length;
                sb.Append(TestData1KB);
                data = sb.ToString();

                if (data.Length > GuaranteedOnlineObjectSize)
                {
                    break;
                }
            }
            var data1 = data.Substring(0, GuaranteedOnlineObjectSize - 29);

            var json = new NbJsonObject()
            {
                {"DATA_ID", "00000"},
                {"DATA", data1}
            };
            Assert.AreEqual(GuaranteedOnlineObjectSize, json.ToString().Length);
            var obj = new NbObject(ITUtil.ObjectBucketName, json);

            var robj = obj.SaveAsync().Result;

            Assert.AreEqual(data1.Length, (robj.Opt<string>("DATA", null).Length));

            Debug.WriteLine("-----------[END]TestOnlineObjectGuaranteedMaxSize: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オンライン）
        /// 最大保証サイズオブジェクトの保持
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineObjectGuaranteedMaxSizeInOneBucketCount()
        {
            Debug.WriteLine("-----------[START]TestOnlineObjectGuaranteedMaxSizeInOneBucketCount: " + DateTime.Now);

            var sb = new StringBuilder();
            string data = null;
            while (true)
            {
                var dataLen = TestData1KB.Length;
                sb.Append(TestData1KB);
                data = sb.ToString();

                if (data.Length > GuaranteedOnlineObjectSize)
                {
                    break;
                }
            }
            var data1 = data.Substring(0, GuaranteedOnlineObjectSize - 29);

            for (var i = 1; i <= GuaranteedOnlineLargeObjectCountInBucket; i++)
            {
                var id = string.Format("{0:D5}", i);

                var json = new NbJsonObject()
                {
                    {"DATA_ID", id},
                    {"DATA", data1}
                };
                Assert.AreEqual(GuaranteedOnlineObjectSize, json.ToString().Length);
                var obj = new NbObject(ITUtil.ObjectBucketName, json);

                obj.SaveAsync().Wait();
            }

            var result = mOnlineBucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;

            Assert.GreaterOrEqual(result.Count(), GuaranteedOnlineLargeObjectCountInBucket);

            Debug.WriteLine("-----------[END]TestOnlineObjectGuaranteedMaxSizeInOneBucketCount: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オンライン）
        /// 検索時間 (100件)
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineQueryObjectsTime()
        {
            Debug.WriteLine("-----------[START]TestOnlineQueryObjectsTime: " + DateTime.Now);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            int loopCount = 5;
            int inCount = DataCount / 100;

            var results = mOnlineBucket.QueryAsync(new NbQuery()).Result;
            Assert.AreEqual(DataCount, results.Count());

            var inList = new List<string>();
            for (int i = 0; i < inCount; i++)
            {
                inList.Add(((i + 1) * 100).ToString());
            }

            // Query
            var query = new NbQuery().In("NO", inList.ToArray());
            elapsedTimes = new List<long>();

            for (int i = 0; i < loopCount; i++)
            {
                mSw.Start();
                var queryRet = mOnlineBucket.QueryAsync(query).Result;
                mSw.Stop();
                var time = CollectResult(i);
                Assert.AreEqual(inCount, queryRet.Count());
            }
            AssertAverage((int)TimeSpecOnlineQueryObjects);

            Debug.WriteLine("-----------[END]TestOnlineQueryObjectsTime: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オンライン）
        /// 作成時間 (1件)
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineCreateObjectTime()
        {
            Debug.WriteLine("-----------[START]TestOnlineCreateObjectTime: " + DateTime.Now);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            int loopCount = 5;

            // Create
            elapsedTimes = new List<long>();
            for (var i = 0; i < loopCount; i++)
            {
                var obj = mOnlineBucket.NewObject();
                obj["key"] = "value";

                mSw.Start();
                var robj = obj.SaveAsync().Result;
                mSw.Stop();

                var time = CollectResult(i);
                Assert.True(robj.HasKey("key"));
            }
            AssertAverage((int)TimeSpecOnlineCreateObject);

            Debug.WriteLine("-----------[END]TestOnlineCreateObjectTime: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オンライン）
        /// 更新時間 (1件)
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineUpdateObjectTime()
        {
            Debug.WriteLine("-----------[START]TestOnlineUpdateObjectTime: " + DateTime.Now);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            int loopCount = 5;

            var query = new NbQuery().Limit(loopCount).OrderBy("DATA_ID");
            var objs = mOnlineBucket.QueryAsync(query).Result.ToList();

            // Update
            elapsedTimes = new List<long>();
            for (var i = 0; i < loopCount; i++)
            {
                objs[i]["key2"] = "value2";
                mSw.Start();
                var robj = objs[i].SaveAsync().Result;
                mSw.Stop();

                var time = CollectResult(i);
                Assert.True(robj.HasKey("key2"));
            }
            AssertAverage((int)TimeSpecOnlineUpdateObject);

            Debug.WriteLine("-----------[END]TestOnlineUpdateObjectTime: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オンライン）
        /// 更新時間 (100件) バッチ処理
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineBatchTime()
        {
            Debug.WriteLine("-----------[START]TestOnlineBatchTime: " + DateTime.Now);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            int loopCount = 5;

            var query = new NbQuery().Limit(100).OrderBy("DATA_ID");

            // Batch
            elapsedTimes = new List<long>();
            for (var i = 0; i < loopCount; i++)
            {
                var objs = mOnlineBucket.QueryAsync(query).Result.ToList();

                var req = new NbBatchRequest();
                foreach (var obj in objs)
                {
                    obj["batchKey"] = "batchValue";
                    req = req.AddUpdateRequest(obj);
                }

                mSw.Start();
                var batchResult = mOnlineBucket.BatchAsync(req).Result;
                mSw.Stop();

                var time = CollectResult(i);
                Assert.AreEqual(100, batchResult.Count);
                for (int j = 0; j < 100; j++)
                {
                    Assert.AreEqual(NbBatchResult.ResultCode.Ok, batchResult[j].Result);
                }
            }
            AssertAverage((int)TimeSpecOnlineBatchObjects);

            Debug.WriteLine("-----------[END]TestOnlineBatchTime: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オンライン）
        /// 削除時間 (1件)
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineDeleteTime()
        {
            Debug.WriteLine("-----------[START]TestOnlineDeleteTime: " + DateTime.Now);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            int loopCount = 5;

            var query = new NbQuery().Limit(loopCount).OrderBy("DATA_ID");
            var objs = mOnlineBucket.QueryAsync(query).Result.ToList();

            // Delete
            elapsedTimes = new List<long>();
            for (var i = 0; i < loopCount; i++)
            {
                mSw.Start();
                objs[i].DeleteAsync(true).Wait();
                mSw.Stop();

                var time = CollectResult(i);
            }
            AssertAverage((int)TimeSpecOnlineDeleteObject);

            Debug.WriteLine("-----------[END]TestOnlineDeleteTime: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オフライン）
        /// オブジェクト数
        /// </summary>
        [Ignore]
        [Test]
        public void TestOfflineObjectCount()
        {
            Debug.WriteLine("-----------[START]TestOfflineObjectCount: " + DateTime.Now);

            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            for (var l = 1; l <= GuaranteedOfflineObjectCount; l++)
            {
                var id = string.Format("{0:D5}", l);
                var json = new NbJsonObject()
                {
                    {"DATA_ID", id},
                    {"DATA", TestData1KB}
                };
                Assert.AreEqual(1024, json.ToString().Length);
                var obj = new NbOfflineObject(ITUtil.ObjectBucketName, json);

                obj.SaveAsync().Wait();
            }

            var result = offlineBucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;

            Assert.AreEqual(GuaranteedOfflineObjectCount, result.Count());

            Debug.WriteLine("-----------[END]TestOfflineObjectCount: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オフライン）
        /// オブジェクトサイズ
        /// </summary>
        [Ignore]
        [Test]
        public void TestOfflineObjectGuaranteedMaxSize()
        {
            Debug.WriteLine("-----------[START]TestOfflineObjectGuaranteedMaxSize: " + DateTime.Now);

            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            var sb = new StringBuilder();
            string data = null;
            while (true)
            {
                var dataLen = TestData1KB.Length;
                sb.Append(TestData1KB);
                data = sb.ToString();

                if (data.Length > GuaranteedOfflineObjectSize)
                {
                    break;
                }
            }
            var data1 = data.Substring(0, GuaranteedOfflineObjectSize - 29);

            var json = new NbJsonObject()
            {
                {"DATA_ID", "00000"},
                {"DATA", data1}
            };
            Assert.AreEqual(GuaranteedOfflineObjectSize, json.ToString().Length);
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, json);

            var robj = obj.SaveAsync().Result;

            Assert.AreEqual(data1.Length, (robj.Opt<string>("DATA", null).Length));

            Debug.WriteLine("-----------[END]TestOfflineObjectGuaranteedMaxSize: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オフライン）
        /// 最大保証サイズオブジェクトの保持
        /// </summary>
        [Ignore]
        [Test]
        public void TestOfflineObjectGuaranteedMaxSizeInOneBucketCount()
        {
            Debug.WriteLine("-----------[START]TestOfflineObjectGuaranteedMaxSizeInOneBucketCount: " + DateTime.Now);

            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            var sb = new StringBuilder();
            string data = null;
            while (true)
            {
                var dataLen = TestData1KB.Length;
                sb.Append(TestData1KB);
                data = sb.ToString();

                if (data.Length > GuaranteedOfflineObjectSize)
                {
                    break;
                }
            }
            var data1 = data.Substring(0, GuaranteedOfflineObjectSize - 29);

            for (var i = 1; i <= GuaranteedOfflineLargeObjectCountInBucket; i++)
            {
                var id = string.Format("{0:D5}", i);

                var json = new NbJsonObject()
                {
                    {"DATA_ID", id},
                    {"DATA", data1}
                };
                Assert.AreEqual(GuaranteedOfflineObjectSize, json.ToString().Length);
                var obj = new NbOfflineObject(ITUtil.ObjectBucketName, json);

                obj.SaveAsync().Wait();
            }

            var result = offlineBucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;

            Assert.GreaterOrEqual(result.Count(), GuaranteedOfflineLargeObjectCountInBucket);

            Debug.WriteLine("-----------[END]TestOfflineObjectGuaranteedMaxSizeInOneBucketCount: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値（オフライン）
        /// バケット数
        /// </summary>
        [Ignore]
        [Test]
        public void TestOfflineObjectBucketCount()
        {
            Debug.WriteLine("-----------[START]TestOfflineObjectBucketCount: " + DateTime.Now);

            for (var i = 1; i <= GuaranteedOfflineObjectBucket; i++)
            {
                var bucketName = string.Format("DotnetITObjectBucket_{0:D2}", i);
                var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(bucketName);

                for (var j = 1; j <= 2000; j++)
                {
                    var id = string.Format("{0:D5}", j);
                    var json = new NbJsonObject()
                    {
                        {"DATA_ID", id},
                        {"DATA", TestData1KB}
                    };
                    Assert.AreEqual(1024, json.ToString().Length);
                    var obj = new NbOfflineObject(bucketName, json);

                    obj.SaveAsync().Wait();
                }

                var result = offlineBucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;

                Assert.AreEqual(2000, result.Count());
            }

            Debug.WriteLine("-----------[END]TestOfflineObjectBucketCount: " + DateTime.Now);
        }

        /// <summary>
        /// 初回同期時間 (10000件)
        /// </summary>
        [Ignore]
        [Test]
        public void TestSyncBucketAsyncNormalMaxFirstSyncTime()
        {
            int loopCount = 5;

            var onlineBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            var syncManager = new NbObjectSyncManager();

            // 10000件のオブジェクト生成
            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            elapsedTimes = new List<long>();
            for (int i = 0; i < loopCount; i++)
            {
                // オフラインオブジェクトを物理削除
                offlineBucket.DeleteAsync(new NbQuery(), false).Wait();

                // 全範囲同期を指定(前回Pull時刻を消去)
                syncManager.SetSyncScope(ITUtil.ObjectBucketName, new NbQuery());

                mSw.Start();
                syncManager.SyncBucketAsync(ITUtil.ObjectBucketName).Wait();
                mSw.Stop();

                var time = CollectResult(i);
                Debug.WriteLine("FirstSyncTime 100000 items: [" + i + "] elapsed Time: " + time);
            }
            AssertAverage(TimeSpecFirstSync);
        }

        /// <summary>
        /// 差分同期時間 (100件)
        /// </summary>
        [Ignore]
        [Test]
        public void TestSyncBucketAsyncNormalMaxDifferentialSyncTime()
        {
            int loopCount = 5;

            var onlineBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            var syncManager = new NbObjectSyncManager();
            // 全範囲同期を指定
            syncManager.SetSyncScope(ITUtil.ObjectBucketName, new NbQuery());

            // 10000件のオブジェクト生成
            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            // 同期済み状態とする
            var results = syncManager.SyncBucketAsync(ITUtil.ObjectBucketName).Result;
            Assert.AreEqual(0, results.Count());

            elapsedTimes = new List<long>();
            for (int i = 0; i < loopCount; i++)
            {
                // 100件を更新
                var updateObjects = onlineBucket.QueryAsync(new NbQuery().Limit(100)).Result;
                ITUtil.UpdateOnlineObjects(onlineBucket, updateObjects).Wait();

                mSw.Start();
                syncManager.SyncBucketAsync(ITUtil.ObjectBucketName).Wait();
                mSw.Stop();

                var time = CollectResult(i);
                Debug.WriteLine("DifferentialSyncTime 100 items: [" + i + "] elapsed Time: " + time);
            }
            AssertAverage(TimeSpecDiffrencialSync);
        }

        /// <summary>
        /// 耐久試験（オンライン）
        /// 連続稼働時間（検索）
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineQueryObjectsDurbility()
        {
            var onlineBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            Debug.WriteLine("-----------[START]TestOnlineQueryObjectsDurbility: " + DateTime.Now);

            var startTime = DateTime.Now;
            var endTime = startTime.AddHours(TimeSpecDurbility);

            var counter = 0;

            while (true)
            {
                var target = "ANTC-CD" + ((counter % 100) * 100).ToString("00000");
                var query = new NbQuery().GreaterThanOrEqual("CD", target).Limit(100);

                counter++;

                var totalMemory = GC.GetTotalMemory(false);
                Console.WriteLine("[MEM]TotalMemory before query [{0}]: {1}", counter, totalMemory);

                // test
                var results = onlineBucket.QueryAsync(query).Result;
                Assert.AreEqual(100, results.Count());

                totalMemory = GC.GetTotalMemory(false);
                Console.WriteLine("[MEM]TotalMemory after query [{0}]: {1}", counter, totalMemory);

                // 時刻経過した場合は終了
                if (DateTime.Now > endTime)
                {
                    break;
                }

                // 2分wait
                Task.Delay(2 * 60 * 1000).Wait();
            }

            Debug.WriteLine("-----------[END]TestOnlineQueryObjectsDurbility: " + DateTime.Now);
        }

        /// <summary>
        /// 耐久試験（オンライン）
        /// 連続稼働時間（更新）
        /// </summary>
        [Ignore]
        [Test]
        public void TestOnlineUpdateObjectDurbility()
        {
            var onlineBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            Debug.WriteLine("-----------[START]TestOnlineUpdateObjectDurbility: " + DateTime.Now);

            var startTime = DateTime.Now;
            var endTime = startTime.AddHours(TimeSpecDurbility);

            var counter = 0;

            while (true)
            {
                var updateObjects = onlineBucket.QueryAsync(new NbQuery().Limit(100).OrderBy("UpdateKey")).Result;
                Assert.AreEqual(100, updateObjects.Count());

                foreach (var obj in updateObjects)
                {
                    counter++;

                    // 値があればそれをインクリメントする
                    var testValue = obj.Opt<int>("UpdateKey", 0);
                    testValue++;
                    obj["UpdateKey"] = testValue;

                    var totalMemory = GC.GetTotalMemory(false);
                    Console.WriteLine("[MEM]TotalMemory before update [{0}]: {1}", counter, totalMemory);

                    // test
                    var saved = obj.SaveAsync().Result;
                    var savedValue = saved.Opt<int>("UpdateKey", 0);
                    Assert.AreEqual(testValue, savedValue);

                    totalMemory = GC.GetTotalMemory(false);
                    Console.WriteLine("[MEM]TotalMemory after update [{0}]: {1}", counter, totalMemory);

                    // 2分wait
                    Task.Delay(2 * 60 * 1000).Wait();
                }

                // 時刻経過した場合は終了
                if (DateTime.Now > endTime)
                {
                    break;
                }
            }

            Debug.WriteLine("-----------[END]TestOnlineUpdateObjectDurbility: " + DateTime.Now);
        }

        /// <summary>
        /// 耐久試験（オフライン）
        /// 連続稼働時間（検索）
        /// </summary>
        [Ignore]
        [Test]
        public void TestOfflineQueryObjectsDurbility()
        {
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(true);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            Debug.WriteLine("-----------[START]TestOfflineQueryObjectsDurbility: " + DateTime.Now);

            var startTime = DateTime.Now;
            var endTime = startTime.AddHours(TimeSpecDurbility);

            var counter = 0;

            while (true)
            {
                var target = "ANTC-CD" + ((counter % 100) * 100).ToString("00000");
                var query = new NbQuery().GreaterThanOrEqual("CD", target).Limit(100);

                counter++;

                var totalMemory = GC.GetTotalMemory(false);
                Console.WriteLine("[MEM]TotalMemory before query [{0}]: {1}", counter, totalMemory);

                // test
                var results = offlineBucket.QueryAsync(query).Result;
                Assert.AreEqual(100, results.Count());

                totalMemory = GC.GetTotalMemory(false);
                Console.WriteLine("[MEM]TotalMemory after query [{0}]: {1}", counter, totalMemory);

                // 時刻経過した場合は終了
                if (DateTime.Now > endTime)
                {
                    break;
                }

                // 2分wait
                Task.Delay(2 * 60 * 1000).Wait();
            }

            Debug.WriteLine("-----------[END]TestOfflineQueryObjectsDurbility: " + DateTime.Now);
        }

        /// <summary>
        /// 耐久試験（オフライン）
        /// 連続稼働時間（更新）
        /// </summary>
        [Ignore]
        [Test]
        public void TestOfflineUpdateObjectDurbility()
        {
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(true);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            Debug.WriteLine("-----------[START]TestOfflineUpdateObjectDurbility: " + DateTime.Now);

            var startTime = DateTime.Now;
            var endTime = startTime.AddHours(TimeSpecDurbility);

            var counter = 0;

            while (true)
            {
                var updateObjects = offlineBucket.QueryAsync(new NbQuery().Limit(100).OrderBy("UpdateKey")).Result;
                Assert.AreEqual(100, updateObjects.Count());

                foreach (var obj in updateObjects)
                {
                    counter++;

                    // 値があればそれをインクリメントする
                    var testValue = obj.Opt<int>("UpdateKey", 0);
                    testValue++;
                    obj["UpdateKey"] = testValue;

                    var totalMemory = GC.GetTotalMemory(false);
                    Console.WriteLine("[MEM]TotalMemory before update [{0}]: {1}", counter, totalMemory);

                    // test
                    var saved = obj.SaveAsync().Result;
                    var savedValue = saved.Opt<int>("UpdateKey", 0);
                    Assert.AreEqual(testValue, savedValue);

                    totalMemory = GC.GetTotalMemory(false);
                    Console.WriteLine("[MEM]TotalMemory after update [{0}]: {1}", counter, totalMemory);

                    // 2分wait
                    Task.Delay(2 * 60 * 1000).Wait();
                }

                // 時刻経過した場合は終了
                if (DateTime.Now > endTime)
                {
                    break;
                }
            }

            Debug.WriteLine("-----------[END]TestOfflineUpdateObjectDurbility: " + DateTime.Now);
        }

        /// <summary>
        /// 耐久試験
        /// 連続稼働時間（差分同期）
        /// </summary>
        [Ignore]
        [Test]
        public void TestSyncBucketAsyncNormalDurbility()
        {
            var onlineBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);
            var offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName);

            var syncManager = new NbObjectSyncManager();
            // 全範囲同期を指定
            syncManager.SetSyncScope(ITUtil.ObjectBucketName, new NbQuery());

            Debug.WriteLine("ImportCSV Start  :" + DateTime.Now);
            ImportCSV(false);
            Debug.WriteLine("ImportCSV Finish :" + DateTime.Now);

            // 全体同期を完了させる
            var results = syncManager.SyncBucketAsync(ITUtil.ObjectBucketName).Result;
            Assert.AreEqual(0, results.Count());

            Debug.WriteLine("-----------[START]SYNC TEST: " + DateTime.Now);

            var startTime = DateTime.Now;
            DateTime endTime = startTime.AddHours(TimeSpecDurbility);

            var counter = 0;

            // 2分毎に100件追加/更新/論理削除と同期を繰り返す
            while (true)
            {
                counter++;

                // 差分データ作成
                // オンライン
                // 論理削除
                var deleteObjects = onlineBucket.QueryAsync(new NbQuery().Limit(33).Skip(100)).Result;
                ITUtil.LogicalDeleteOnlineObjects(onlineBucket, deleteObjects).Wait();
                // 更新
                var updateObjects = onlineBucket.QueryAsync(new NbQuery().Limit(34).Skip(200)).Result;
                ITUtil.UpdateOnlineObjects(onlineBucket, updateObjects).Wait();
                // 新規
                var batch = new NbBatchRequest();
                foreach (var obj in deleteObjects)
                {
                    var insert = onlineBucket.NewObject().FromJson(obj.ToJson());
                    insert.Id = null;
                    insert.Etag = null;
                    insert.Deleted = false;

                    batch.AddInsertRequest(insert);
                }
                onlineBucket.BatchAsync(batch).Wait();

                // オフライン
                // 論理削除
                deleteObjects = offlineBucket.QueryAsync(new NbQuery().Limit(33).Skip(300)).Result;
                foreach (var obj in deleteObjects)
                {
                    obj.DeleteAsync(true).Wait();
                }
                // 更新
                updateObjects = offlineBucket.QueryAsync(new NbQuery().Limit(34).Skip(400)).Result;
                foreach (var obj in updateObjects)
                {
                    obj.SaveAsync().Wait();
                }
                // 新規
                foreach (var obj in deleteObjects)
                {
                    var insert = offlineBucket.NewObject().FromJson(obj.ToJson());
                    insert.Id = null;
                    insert.Etag = null;
                    insert.Deleted = false;

                    insert.SaveAsync().Wait();
                }

                Task.Delay(NbObjectSyncManager.PullTimeOffsetSeconds * 1000).Wait();

                var totalMemory = GC.GetTotalMemory(false);

                Console.WriteLine("[MEM]TotalMemory before Sync [{0}]: {1}", counter, totalMemory);

                // test
                results = syncManager.SyncBucketAsync(ITUtil.ObjectBucketName).Result;
                Assert.AreEqual(0, results.Count());

                totalMemory = GC.GetTotalMemory(false);
                Console.WriteLine("[MEM]TotalMemory  after Sync [{0}]: {1}", counter, totalMemory);

                // 時刻経過した場合は終了
                if (DateTime.Now > endTime)
                {
                    break;
                }

                // 2分wait
                Task.Delay(2 * 60 * 1000).Wait();
            }

            Debug.WriteLine("-----------[END]SYNC TEST: " + DateTime.Now);
        }

        /**
         * ファイルストレージ（性能） 
         * 
         * ★注意★
         * ・通常はテスト対象外。テストする場合は、[Ignore]を消して、手動実行すること。
         * ・NW負荷が少ない時に、テストすること。
         * ・"spec_test_1mb.jpg"、"spec_test_100mb.mp4"を以下に格納してからテストを開始すること。
         * 　(Debugビルド)   .\Nebula.IT\bin\Debug\
         * 　(Releaseビルド) .\Nebula.IT\bin\Release\
         **/

        private const string Image1MbytesFile = "spec_test_1mb.jpg";
        private const string Movie100MbytesFile = "spec_test_100mb.mp4";

        // 新規ファイルアップロード時間の期待値
        private const int TimeSpecUploadNewFile = 2 * 1000; // ms
        // 新規ファイルダウンロード時間の期待値
        private const int TimeSpecDownloadFile = 2 * 1000; // ms
        // 件数
        private const int GuaranteedFileCount = 1000; // 件
        // 1バケットあたりの最大ファイルサイズを持つファイル件数
        private const int GuaranteedLargeFileCountInBucket = 8; // 件
        // 合計サイズ
        private const long GuaranteedTotalFileMaxSize = 1000 * ITUtil.BytesPerMbyte; // byte
        // 連続稼働時間
        private const double TestFileStorageDuration = 48.0; // hour
        // インターバル
        private const int TestFileStorageInternal = 2 * 60 * 1000; // ms

        /// <summary>
        /// 動作保証値
        /// ファイル数/メタファイル数
        /// 1000件分のファイルデータを取得する/1000件分のファイルメタデータを取得する
        /// </summary>
        [Ignore]
        [Test]
        public void TestFileStorageFileCount()
        {
            Debug.WriteLine("-----------[START]TestFileStorageFileMaxCount: " + DateTime.Now);

            // 1バケットあたりに保持できる最大ファイル数。ファイルは画像ファイルとテキストファイルで半々とする
            // 1ファイルのサイズは1MBとする。
            UploadFiles(GuaranteedFileCount);

            // ファイル一覧の取得をする
            var metadataList = mFileBucket.GetFilesAsync().Result.ToList();

            // 1000件であること
            Assert.AreEqual(GuaranteedFileCount, metadataList.Count);

            Debug.WriteLine("-----------[END]TestFileStorageFileMaxCount: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値
        /// ファイルサイズ（動画）
        /// 1ファイルの最大サイズ
        /// </summary>
        [Ignore]
        [Test]
        public void TestFileStorageMovieMaxSize()
        {
            Debug.WriteLine("-----------[START]TestFileStorageMovieMaxSize: " + DateTime.Now);

            // usingは使わずに自分でCloseする
            FileStream fs = new FileStream(Movie100MbytesFile, FileMode.Open, FileAccess.Read);
            var data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();

            string str = null;
            try
            {
                str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ff") + "\t" + "start.";
                Debug.WriteLine(str);

                // アップロード
                var meta = mFileBucket.UploadNewFileAsync(data, "MovieFile_0000.mp4", "video/mp4", NbAcl.CreateAclForAnonymous()).Result;
                Assert.NotNull(meta);

                // ダウンロード
                var downloadData = mFileBucket.DownloadFileAsync(meta.Filename).Result;
                Assert.AreEqual(data, downloadData.RawBytes);

                str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ff") + "\t" + "end.";
                Debug.WriteLine(str);
            }
            catch (Exception)
            {
                Assert.Fail("Upload Fail");
            }

            Debug.WriteLine("-----------[END]TestFileStorageMovieMaxSize: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値
        /// 最大サイズファイルの保持
        /// 最大ファイルサイズを持つファイルの、1バケットの保持数は8ファイルとする
        /// </summary>
        [Ignore]
        [Test]
        public void TestFileStorageTotalMaxSizeInOneBucket()
        {
            Debug.WriteLine("-----------[START]TestFileStorageTotalMaxSizeInOneBucket: " + DateTime.Now);

            var data = ITUtil.GetTextBytes(100 * ITUtil.BytesPerMbyte);

            for (int i = 0; i < GuaranteedLargeFileCountInBucket; i++)
            {
                var fileName = string.Format("TextFile_{0:D4}.txt", i);
                try
                {
                    var meta = mFileBucket.UploadNewFileAsync(data, fileName, "text/plain", NbAcl.CreateAclForAnonymous()).Result;
                    Assert.NotNull(meta);
                }
                catch (Exception)
                {
                    Assert.Fail("Upload Fail");
                }
            }

            // 取得
            var files = mFileBucket.GetFilesAsync().Result.ToList();
            Assert.GreaterOrEqual(files.Count, GuaranteedLargeFileCountInBucket);

            Debug.WriteLine("-----------[END]TestFileStorageTotalMaxSizeInOneBucket: " + DateTime.Now);
        }

        /// <summary>
        /// 動作保証値
        /// 合計サイズ
        /// ファイル数やファイルサイズの組み合わせ評価が発生した場合には、本サイズを保証する
        /// </summary>
        [Ignore]
        [Test]
        public void TestFileStorageTotalGuaranteedSize()
        {
            Debug.WriteLine("-----------[START]TestFileStorageTotalMaxSize: " + DateTime.Now);

            UploadFiles(GuaranteedFileCount);

            var files = mFileBucket.GetFilesAsync().Result.ToList();

            long totalSize = 0;
            foreach (NbFileMetadata file in files)
            {
                totalSize += file.Length;
            }

            // この時点で1000MByteは超えていない
            long restSize = GuaranteedTotalFileMaxSize - totalSize;

            var data = ITUtil.GetTextBytes(restSize);

            try
            {
                var meta = mFileBucket.UploadNewFileAsync(data, "UploadFile_000.txt", "text/plain", NbAcl.CreateAclForAnonymous()).Result;
                Assert.NotNull(meta);
            }
            catch (Exception)
            {
                Assert.Fail("Upload Fail");
            }

            files = mFileBucket.GetFilesAsync().Result.ToList();

            totalSize = 0;
            foreach (NbFileMetadata file in files)
            {
                totalSize += file.Length;
            }

            Assert.GreaterOrEqual(totalSize, GuaranteedTotalFileMaxSize);

            Debug.WriteLine("-----------[END]TestFileStorageTotalMaxSize: " + DateTime.Now);
        }

        /// <summary>
        /// 性能
        /// 新規ファイルアップロード時間/新規ファイルダウンロード時間
        /// 1ファイルのアップロード時間/1ファイルのダウンロード時間
        /// </summary>
        [Ignore]
        [Test]
        public void TestFileStorageUploadDownloadTime()
        {
            // 諸元より
            // 4) ファイルサイズは1MB。サーバ側の登録済みファイル500件とする。

            Debug.WriteLine("-----------[START]TestFileStorageUploadDownloadTime: " + DateTime.Now);

            int loopCount = 10;

            // サーバに500件を事前登録する
            UploadFiles(500);

            // 新規ファイルアップロードする
            elapsedTimes = new List<long>();
            for (int i = 0; i < loopCount; i++)
            {
                if (loopCount % 2 == 0)
                {
                    var data = ITUtil.GetTextBytes(ITUtil.BytesPerMbyte);
                    var fileName = string.Format("UploadFile_{0:D4}.txt", i);
                    mSw.Start();
                    mFileBucket.UploadNewFileAsync(data, fileName, "text/plain", NbAcl.CreateAclForAnonymous()).Wait();
                    mSw.Stop();
                    // 条件が変わるので、削除しておく
                    mFileBucket.DeleteFileAsync(fileName).Wait();
                }
                else
                {
                    var data = Get1MbytesImageBytes();
                    var fileName = string.Format("UploadFile_{0:D4}.jpg", i);
                    mSw.Start();
                    mFileBucket.UploadNewFileAsync(data, fileName, "image/jpeg", NbAcl.CreateAclForAnonymous()).Wait();
                    mSw.Stop();
                    // 条件が変わるので、削除しておく
                    mFileBucket.DeleteFileAsync(fileName).Wait();
                }

                var time = CollectResult(i);
            }
            AssertAverage(TimeSpecUploadNewFile);

            // ダウンロードする
            elapsedTimes = new List<long>();
            for (int i = 0; i < loopCount; i++)
            {
                string fileName;
                if (loopCount % 2 == 0)
                {
                    fileName = string.Format("TextFile_{0:D4}.txt", i);
                }
                else
                {
                    fileName = string.Format("ImageFile_{0:D4}.jpg", i);
                }

                mSw.Start();
                mFileBucket.DownloadFileAsync(fileName).Wait();
                mSw.Stop();

                var time = CollectResult(i);
            }
            AssertAverage(TimeSpecDownloadFile);

            Debug.WriteLine("-----------[END]TestFileStorageUploadDownloadTime: " + DateTime.Now);
        }

        /// <summary>
        /// 耐久試験
        /// ファイルのアップロード/ダウンロードの連続処理時間
        /// </summary>
        [Ignore]
        [Test]
        public void TestFileStorage()
        {
            Debug.WriteLine("-----------[START]TestFileStorage: " + DateTime.Now);

            // 1000件のデータをアップロードする
            UploadFiles(GuaranteedFileCount);

            // ファイル一覧の取得をする
            var metadataList = mFileBucket.GetFilesAsync().Result.ToList();

            var startTime = DateTime.Now;
            DateTime endTime = startTime.AddHours(TestFileStorageDuration);

            double counter = 0;
            int i = 0;

            // ファイルのアップロードとダウンロードを繰り返す
            while (true)
            {
                counter++;

                var totalMemory = GC.GetTotalMemory(false);
                Console.WriteLine("[MEM]TotalMemory before Test [{0:D6}]: {1}", counter, totalMemory);

                // test
                // ファイルのダウンロード
                var downloadData = mFileBucket.DownloadFileAsync(metadataList[i].Filename).Result;
                Assert.Greater(downloadData.RawBytes.Length, 0);

                if (i % 2 == 0)
                {
                    // 更新アップロード
                    byte[] data;
                    if (metadataList[i].ContentType.Equals("text/plain"))
                    {
                        data = ITUtil.GetTextBytes(ITUtil.BytesPerMbyte);
                    }
                    else
                    {
                        data = Get1MbytesImageBytes();
                    }
                    mFileBucket.UploadUpdateFileAsync(data, metadataList[i].Filename, metadataList[i].ContentType).Wait();
                }
                else
                {
                    // 制限にヒットしないように、1件削除しておく
                    mFileBucket.DeleteFileAsync("TextFile_0000.txt").Wait();

                    // 新規アップロード
                    var data = ITUtil.GetTextBytes(ITUtil.BytesPerMbyte);
                    mFileBucket.UploadNewFileAsync(data, "UploadFile_000.txt", "text/plain", NbAcl.CreateAclForAnonymous()).Wait();

                    // 条件が変わるので、元に戻しておく
                    mFileBucket.DeleteFileAsync("UploadFile_000.txt").Wait();
                    mFileBucket.UploadNewFileAsync(data, "TextFile_0000.txt", "text/plain", NbAcl.CreateAclForAnonymous()).Wait();
                }

                totalMemory = GC.GetTotalMemory(false);
                Console.WriteLine("[MEM]TotalMemory after Test [{0:D6}]: {1}", counter, totalMemory);

                // 時刻経過した場合は終了
                if (DateTime.Now > endTime)
                {
                    break;
                }

                if (i < (GuaranteedFileCount - 1))
                {
                    i++;
                }
                else
                {
                    i = 0;
                }

                // 2分wait
                Task.Delay(TestFileStorageInternal).Wait();
            }

            Debug.WriteLine("-----------[END]TestFileStorage: " + DateTime.Now);
        }

        private void ImportCSV(bool isOffline = true, int dataCount = 10000)
        {
            try
            {
                var createList = new List<NbObject>();

                var itemList = 6;
                var count = 0;
                var keyList = new List<string>();
                using (var sr = new StreamReader("TestData_1KB_10000Item.csv"))
                {
                    while (!sr.EndOfStream)
                    {
                        if (count == dataCount + 2)
                        {
                            break;
                        }
                        count++;
                        var line = sr.ReadLine();
                        var values = line.Split(',');
                        var valueList = new List<string>();
                        if (count == 1)
                        {
                            // coutinue;
                        }
                        else if (count == 2)
                        {
                            foreach (var value in values)
                            {
                                keyList.Add(value);
                            }
                        }
                        else
                        {
                            foreach (var value in values)
                            {
                                valueList.Add(value);
                            }

                            var obj = isOffline ? mBucket.NewObject() : mOnlineBucket.NewObject();

                            for (int i = 0; i < itemList; i++)
                            {
                                var key = keyList[i];
                                var value = valueList[i];
                                obj[key] = value;
                            }
                            createList.Add(obj);
                        }
                    }
                }

                if (isOffline)
                {
                    foreach (var obj in createList)
                    {
                        obj.SaveAsync().Wait();
                    }
                }
                else
                {
                    while (createList.Count != 0)
                    {
                        var target = createList.Take(1000).ToList();
                        createList = createList.Skip(1000).ToList();

                        var batch = new NbBatchRequest();
                        foreach (var obj in target)
                        {
                            batch.AddInsertRequest(obj);
                        }
                        mOnlineBucket.BatchAsync(batch).Wait();
                    }
                }
            }
            catch (Exception)
            {
                Assert.Fail("import CSV Error!");
            }
        }

        private long CollectResult(int i)
        {
            var ret = mSw.ElapsedMilliseconds;
            if (i == 0)
            {
                elapsedTimesFirst = ret;
            }
            else
            {
                elapsedTimes.Add(ret);
            }
            mSw.Reset();
            return ret;
        }

        private void AssertAverage(int spec)
        {
            Debug.WriteLine("[Average]first time : " + elapsedTimesFirst);
            Assert.IsTrue(elapsedTimesFirst <= spec);
            long sumSecond = 0;
            foreach (var sec in elapsedTimes)
            {
                sumSecond += sec;
            }
            var averageSecond = (double)sumSecond / elapsedTimes.Count;
            Debug.WriteLine("[Average]second time : " + averageSecond);
            Assert.IsTrue(averageSecond <= spec);
        }

        private void DeleteAllObject()
        {
            var query = new NbQuery().DeleteMark(true);
            var results = mBucket.QueryAsync(query).Result;
            foreach (var result in results)
            {
                result.DeleteAsync(false).Wait();
            }
            results = mBucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;
            Assert.IsEmpty(results);
        }

        private string GetDbPath()
        {
            var service = NbService.GetInstance();
            var idpass = service.TenantId + "/" + service.AppId;
            var documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fulldbpath = "NEC/Nebula/" + idpass + "/offline.db";
            fulldbpath = Path.Combine(documentFolder, fulldbpath);
            return fulldbpath;
        }

        private void TryDeleteDbFile()
        {
            NbOfflineService.SetInMemoryMode(false);
            NbService.Singleton.DisableOffline();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            bool worked = false;
            int tries = 1;
            if (File.Exists(GetDbPath()))
            {
                while ((tries < 5) && (!worked))
                {
                    try
                    {
                        Task.Delay(tries * 100).Wait();
                        File.Delete(GetDbPath());
                        worked = true;
                    }
                    catch (IOException)
                    {
                        tries++;
                    }
                }
                if (!worked)
                    throw new IOException("can not delete file");
            }
        }

        private byte[] Get1MbytesImageBytes()
        {
            // usingは使わずに自分でCloseする
            FileStream fs = new FileStream(Image1MbytesFile, FileMode.Open, FileAccess.Read);
            var data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();

            return data;
        }

        public void Upload1MbytesTextFile(string fileName)
        {
            var data = ITUtil.GetTextBytes(ITUtil.BytesPerMbyte);

            NbFileMetadata meta = null;
            try
            {
                meta = mFileBucket.UploadNewFileAsync(data, fileName, "text/plain", NbAcl.CreateAclForAnonymous()).Result;
                Assert.NotNull(meta);
            }
            catch (Exception)
            {
                Assert.Fail("Upload Fail");
            }
        }

        private void UploadFiles(int count)
        {
            // ファイルは画像ファイルとテキストファイルで半々とする
            for (int i = 0; i < (count / 2); i++)
            {
                var fileName = string.Format("TextFile_{0:D4}.txt", i);
                Upload1MbytesTextFile(fileName);
            }
            for (int i = 0; i < (count / 2); i++)
            {
                var fileName = string.Format("ImageFile_{0:D4}.jpg", i);
                Upload1MbytesImageFile(fileName);
            }
        }

        private void Upload1MbytesImageFile(string fileName)
        {
            var data = Get1MbytesImageBytes();

            NbFileMetadata meta = null;
            try
            {
                meta = mFileBucket.UploadNewFileAsync(data, fileName, "image/jpeg", NbAcl.CreateAclForAnonymous()).Result;
                Assert.NotNull(meta);
            }
            catch (Exception)
            {
                Assert.Fail("Upload Fail");
            }
        }
    }
}
