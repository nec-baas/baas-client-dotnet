using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class ObjectMultiThreadIT
    {
        // 組み合わせ試験の回数
        // テスト仕様書上はCrudLoopCount=1000、SyncLoopCount=100となっているが、
        // TP走行時間の都合上、以下とする（ANTC Redmine #3220）。
        private static int CrudLoopCount = 100;
        private static int SyncLoopCount = 10;

        private NbService _service;
        private NbObjectBucketBase<NbObject> _onlineBucket;
        private NbObjectBucketBase<NbOfflineObject> _offlineBucket;
        private NbObjectSyncManager _syncManager;
        private NbOfflineObject _testObj;
        private ProcessState _processState;

        // 以下の初期化は必要なテストで行う
        private int _updateSuccessCount = 0;
        private int _updateErrCount = 0;
        private int _deleteSuccessCount = 0;
        private int _deleteErrCount = 0;
        private int _sync1SuccessCount = 0;
        private int _sync1ErrCount = 0;
        private int _sync2SuccessCount = 0;
        private int _sync2ErrCount = 0;

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineObjectStorage().Wait();

            _service = NbService.Singleton;
            NbOfflineService.EnableOfflineService(_service);

            // ローカルデータ一括削除
            NbOfflineService.DeleteCacheAll().Wait();

            _syncManager = new NbObjectSyncManager(_service);
            _onlineBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName, _service);
            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName, _service);
            _processState = ProcessState.GetInstance();

            // 同期範囲を設定
            _syncManager.SetSyncScope(ITUtil.ObjectBucketName, new NbQuery());
        }

        [TearDown]
        public void TearDown()
        {
            // 同期状態、CRUD状態が各テスト完了時にFalseになっているべき
            Assert.False(_processState.Syncing);
            Assert.False(_processState.Crud);
        }

        /// <summary>
        /// 同期排他（同期中の同期）
        /// 同期排他（同期中のCRUD）
        /// 
        /// バケット同期中の以下を確認
        ///  ・バケット同期できない
        ///  ・作成・読込できる
        ///  ・更新・削除・オブジェクト一括削除・ローカルデータ一括削除できない
        ///  また、バケット同期終了後に上記処理ができること
        /// </summary>
        [Test]
        public void TestLockedWhileBucketSyncing()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 500; i++)
            {
                CreateTestObject().Wait();
            }

            // バケット同期開始
            var syncProc = _syncManager.SyncBucketAsync(ITUtil.ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);

            // バケット同期中のバケット同期はできない
            SyncTest(false).Wait();

            // 同期中は作成できる
            CreateObjectTest(true).Wait();

            // 同期中は読込できる
            ReadObjectTest(true).Wait();

            // 同期中は更新できない
            UpdateObjectTest(false).Wait();

            // 同期中は削除できない
            DeleteObjectTest(false).Wait();

            // 同期中はオブジェクト一括削除できない
            DeleteAllObjectsTest(false).Wait();

            // 同期中はローカルデータ一括削除できない
            DeleteCacheAllTest(false).Wait();

            // 同期修了
            syncProc.Wait();

            // 同期終了後は同期できる
            SyncTest(true).Wait();

            // 同期終了後は作成できる
            CreateObjectTest(true).Wait();

            // 同期終了後は読込できる
            ReadObjectTest(true).Wait();

            // 同期終了後は更新できる
            UpdateObjectTest(true).Wait();

            // 同期終了後は削除できる
            DeleteObjectTest(true).Wait();

            // 同期終了後はオブジェクト一括削除できる
            DeleteAllObjectsTest(true, false).Wait();

            // 同期終了後はローカルデータ一括削除できる
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 同期排他（同期中の同期） - バケット同期中のバケット同期
        /// 
        /// バケット同期中にバケット同期できないこと
        /// </summary>
        [Test]
        public void TestLockedWhileBucketSyncingWithParallelSync()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // バケット同期中にバケット同期できないこと
            Parallel.Invoke(new Action[]
            {
                () => SyncTest(true).Wait(),
                () => SyncTest(false, 5).Wait(),
            });
        }

        /// <summary>
        /// 同期排他（同期中の同期）
        /// 同期排他（同期中のCRUD）
        /// 
        /// バケット同期エラー後に同期およびCRUDできること
        /// </summary>
        [Test]
        public void TestLockedAfterBucketSyncingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 10; i++)
            {
                CreateTestObject().Wait();
            }

            // テスト用にAppIdを不正なものにする
            _service.AppId = "dummyAppId";

            // バケット同期でエラーが発生
            try
            {
                _syncManager.SyncBucketAsync(ITUtil.ObjectBucketName, NbObjectConflictResolver.PreferClientResolver).Wait();
            }
            catch (Exception)
            {
                // ok
            }

            // AppIdを元に戻す
            _service.AppId = ITUtil.AppId;

            // 同期エラー発生後は同期できる
            SyncTest(true).Wait();

            // 同期エラー発生後は作成できる
            CreateObjectTest(true).Wait();

            // 同期エラー発生後は読込できる
            ReadObjectTest(true).Wait();

            // 同期エラー発生後は更新できる
            UpdateObjectTest(true).Wait();

            // 同期エラー発生後は削除できる
            DeleteObjectTest(true).Wait();

            // 同期エラー発生後はオブジェクト一括削除できる
            DeleteAllObjectsTest(true, false).Wait();

            // 同期エラー発生後はローカルデータ一括削除できる
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 同期排他（同期中のCRUD） - バケット同期中の更新
        /// 
        /// バケット同期中に更新できないこと
        /// </summary>
        [Test]
        public void TestLockedWhileBucketSyncingWithParallelUpdate()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 10; i++)
            {
                CreateTestObject().Wait();
            }

            // バケット同期中に更新できないこと
            Parallel.Invoke(new Action[]
            {
                () => SyncTest(true).Wait(),
                () => UpdateObjectTest(false, 5).Wait(),
            });
        }

        /// <summary>
        /// 同期排他（同期中のCRUD） - バケット同期中の削除
        /// 
        /// バケット同期中に削除できないこと
        /// </summary>
        [Test]
        public void TestLockedWhileBucketSyncingWithParallelDelete()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 10; i++)
            {
                CreateTestObject().Wait();
            }

            // バケット同期中に削除できないこと
            Parallel.Invoke(new Action[]
            {
                () => SyncTest(true).Wait(),
                () => DeleteObjectTest(false, true, 5).Wait(),
            });
        }

        /// <summary>
        /// 同期排他（同期中のCRUD） - バケット同期中のオブジェクト一括削除
        /// 
        /// バケット同期中にオブジェクト一括削除できないこと
        /// </summary>
        [Test]
        public void TestLockedWhileBucketSyncingWithParallelDeleteAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 10; i++)
            {
                CreateTestObject().Wait();
            }

            // バケット同期中にオブジェクト一括削除できないこと
            Parallel.Invoke(new Action[]
            {
                () => SyncTest(true).Wait(),
                () => DeleteAllObjectsTest(false, true, 5).Wait(),
            });
        }

        /// <summary>
        /// 同期排他（同期中のCRUD） - バケット同期中のローカルデータ一括削除
        /// 
        /// バケット同期中にローカルデータ一括削除できないこと
        /// </summary>
        [Test]
        public void TestLockedWhileBucketSyncingWithParallelDeleteCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // バケット同期中にローカルデータ一括削除できないこと
            Parallel.Invoke(new Action[]
            {
                () => SyncTest(true).Wait(),
                () => DeleteCacheAllTest(false, 5).Wait(),
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中の同期） - 更新中のバケット同期
        /// 
        /// 待ち合わせを行うこと
        /// 更新終了後に同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileUpdating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 更新終了後に同期できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectTest(true).Wait(),
                () => SyncTest(true, 5).Wait(),
            });

            // サーバにクエリをかける
            var query = new NbQuery().EqualTo("testKey1", "testValue1");
            var results = _onlineBucket.QueryAsync(query).Result.ToList();
            Assert.GreaterOrEqual(results.Count, 1);
        }

        /// <summary>
        /// 更新・削除排他（更新中の同期） - 更新中のバケット同期(更新エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 更新エラー終了後に同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileUpdatingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 更新エラー終了後に同期できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectError(_testObj).Wait(),
                () => SyncTest(true, 100).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中の同期） - 更新終了後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterUpdating()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 更新を実施
            UpdateObjectTest(true).Wait();

            // 同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中の同期） - 更新エラー終了後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterUpdatingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 更新エラー
            UpdateObjectError(_testObj).Wait();

            // 同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedWhileUpdating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 作成できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectTest(true).Wait(),
                () => CreateObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の作成(更新エラー終了)
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedWhileUpdatingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 作成できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectError(_testObj).Wait(),
                () => CreateObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedWhileUpdating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 読込できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectTest(true).Wait(),
                () => ReadObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の読込(更新エラー終了)
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedWhileUpdatingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 読込できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectError(_testObj).Wait(),  
                () => ReadObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の更新
        /// 
        /// 待ち合わせを行うこと
        /// 更新終了後に更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedWhileUpdating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 更新終了後に更新できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectTest(true).Wait(),
                () => UpdateObjectTest2(true, 5).Wait()
            });

            // ローカルにクエリをかける
            var query = new NbQuery().EqualTo("_id", _testObj.Id).DeleteMark(true);
            var result = _offlineBucket.QueryAsync(query).Result.ToList();
            Assert.True(result[0].HasKey("testKey1"));
            Assert.True(result[0].HasKey("testKey2"));
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の更新(更新エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 更新エラー終了後に更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedWhileUpdatingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, _testObj.ToJson(), _service);

            // ETagを変更する
            var etag = obj.Etag;
            obj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 更新エラー終了後に更新できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectError(obj).Wait(),  
                () => UpdateObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の削除
        /// 
        /// 待ち合わせを行うこと
        /// 更新終了後に削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedWhileUpdating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 更新終了後に削除できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectTest(true).Wait(),
                () => DeleteObjectTest(true, true, 2).Wait(),
            });

            // ローカルにクエリをかける
            var query = new NbQuery().EqualTo("_id", _testObj.Id).DeleteMark(true);
            var result = _offlineBucket.QueryAsync(query).Result.ToList();
            Assert.True(result[0].HasKey("testKey1"));
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中の削除(更新エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 更新エラー終了後に削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedWhileUpdatingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, _testObj.ToJson(), _service);

            // ETagを変更する
            var etag = obj.Etag;
            obj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 更新エラー終了後に削除できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectError(obj).Wait(), 
                () => DeleteObjectTest(true, true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中のオブジェクト一括削除
        /// 
        /// 待ち合わせを行うこと
        /// 更新終了後にオブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedWhileUpdating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 更新終了後にオブジェクト一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectTest(true).Wait(),
                () => DeleteAllObjectsTest(true, true, 1).Wait(),
            });

            // ローカルにクエリをかける
            var query = new NbQuery().EqualTo("_id", _testObj.Id).DeleteMark(true);
            var result = _offlineBucket.QueryAsync(query).Result.ToList();
            Assert.True(result[0].HasKey("testKey1"));
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中のオブジェクト一括削除(更新エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 更新エラー終了後にオブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedWhileUpdatingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 更新エラー終了後にオブジェクト一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectError(_testObj).Wait(), 
                () => DeleteAllObjectsTest(true, true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中のローカルデータ一括削除
        /// 
        /// 待ち合わせを行うこと
        /// 更新終了後にローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedWhileUpdating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 更新終了後にローカルデータ一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectTest(true).Wait(),
                () => DeleteCacheAllTest(true, 1).Wait(),
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新中のローカルデータ一括削除(更新エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 更新エラー終了後にローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedWhileUpdatingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 更新エラー終了後にローカルデータ一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => UpdateObjectError(_testObj).Wait(),  
                () => DeleteCacheAllTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新終了後の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedAfterUpdating()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 更新を実施
            UpdateObjectTest(true).Wait();

            // 作成できること
            CreateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新終了後の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedAfterUpdating()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 更新を実施
            UpdateObjectTest(true).Wait();

            // 読込できること
            ReadObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新終了後の更新
        /// 
        /// 更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedAfterUpdating()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 更新を実施
            UpdateObjectTest(true).Wait();

            // 更新できること
            UpdateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新終了後の削除
        /// 
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedAfterUpdating()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 更新を実施
            UpdateObjectTest(true).Wait();

            // 削除できること
            DeleteObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新終了後のオブジェクト一括削除
        /// 
        /// オブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedAfterUpdating()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 更新を実施
            UpdateObjectTest(true).Wait();

            // オブジェクト一括削除できること
            DeleteAllObjectsTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新終了後のローカルデータ一括削除
        /// 
        /// ローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedAfterUpdating()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 更新を実施
            UpdateObjectTest(true).Wait();

            // ローカルデータ一括削除できること
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新エラー終了後の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedAfterUpdatingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 更新エラー
            UpdateObjectError(_testObj).Wait();

            // 作成できること
            CreateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新エラー終了後の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedAfterUpdatingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 更新エラー
            UpdateObjectError(_testObj).Wait();

            // 読込できること
            ReadObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新エラー終了後の更新
        /// 
        /// 更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedAfterUpdatingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            var etag = _testObj.Etag;
            _testObj.Etag = "dummy";

            // 更新エラー
            UpdateObjectError(_testObj).Wait();

            // 更新できること
            _testObj.Etag = etag;
            UpdateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新エラー終了後の削除
        /// 
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedAfterUpdatingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            var etag = _testObj.Etag;
            _testObj.Etag = "dummy";

            // 更新エラー
            UpdateObjectError(_testObj).Wait();

            // 削除できること
            _testObj.Etag = etag;
            DeleteObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新エラー終了後のオブジェクト一括削除
        /// 
        /// オブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedAfterUpdatingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 更新エラー
            UpdateObjectError(_testObj).Wait();

            // オブジェクト一括削除できること
            DeleteAllObjectsTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（更新中のCRUD） - 更新エラー終了後のローカルデータ一括削除
        /// 
        /// ローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedAfterUpdatingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 更新エラー
            UpdateObjectError(_testObj).Wait();

            // ローカルデータ一括削除できること
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中の同期） - 削除中のバケット同期
        /// 
        /// 待ち合わせを行うこと
        /// 削除終了後に同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileDeleting()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 削除終了後に同期できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectTest(true).Wait(),
                () => SyncTest(true, 5).Wait()
            });

            // サーバにクエリをかける
            var results = _onlineBucket.QueryAsync(new NbQuery()).Result.ToList();
            Assert.AreEqual(99, results.Count);
        }

        /// <summary>
        /// 更新・削除排他（削除中の同期） - 削除中のバケット同期(削除エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 削除エラー終了後に同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileDeletingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 削除エラー終了後に同期できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectError(_testObj).Wait(),  
                () => SyncTest(true, 100).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中の同期） - 削除終了後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterDeleting()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteObjectTest(true).Wait();

            // 同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中の同期） - 削除エラー終了後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterDeletingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 削除エラー
            DeleteObjectError(_testObj).Wait();

            // 同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedWhileDeleting()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 作成できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectTest(true).Wait(),
                () => CreateObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の作成(削除エラー終了)
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedWhileDeletingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 作成できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectError(_testObj).Wait(),  
                () => CreateObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedWhileDeleting()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 読込できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectTest(true).Wait(),
                () => QueryObjectTest(1, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の読込(削除エラー終了)
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedWhileDeletingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 読込できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectError(_testObj).Wait(),  
                () => ReadObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の更新
        /// 
        /// 待ち合わせを行うこと
        /// 削除終了後に更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedWhileDeleting()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 削除終了後に更新できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectTest(true).Wait(),
                () => UpdateObjectTest(true, 5).Wait()
            });

            // ローカルにクエリをかける
            var result = _offlineBucket.QueryAsync(new NbQuery().DeleteMark(true).EqualTo("_id", _testObj.Id)).Result.ToList();
            Assert.True(result[0].Deleted);
            Assert.True(result[0].HasKey("testKey1"));
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の更新(削除エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 削除エラー終了後に更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedWhileDeletingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, _testObj.ToJson(), _service);

            // ETagを変更する
            var etag = obj.Etag;
            obj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 削除エラー終了後に更新できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectError(obj).Wait(),  
                () => UpdateObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の削除
        /// 
        /// 待ち合わせを行うこと
        /// 削除終了後に削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedWhileDeleting()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 削除終了後に削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectTest(true).Wait(),
                () => DeleteObjectTest(true, true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中の削除(削除エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 削除エラー終了後に削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedWhileDeletingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, _testObj.ToJson(), _service);

            // ETagを変更する
            var etag = obj.Etag;
            obj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 削除エラー終了後に削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectError(obj).Wait(),  
                () => DeleteObjectTest(true, true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中のオブジェクト一括削除
        /// 
        /// 待ち合わせを行うこと
        /// 削除終了後にオブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedWhileDeleting()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 削除終了後にオブジェクト一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectTest(true).Wait(),
                () => DeleteAllObjectsTest(true, true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中のオブジェクト一括削除(削除エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 削除エラー終了後にオブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedWhileDeletingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 削除エラー終了後にオブジェクト一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectError(_testObj).Wait(),  
                () => DeleteAllObjectsTest(true, true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中のローカルデータ一括削除
        /// 
        /// 待ち合わせを行うこと
        /// 削除終了後にローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedWhileDeleting()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 削除終了後にローカルデータ一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectTest(true).Wait(),
                () => DeleteCacheAllTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除中のローカルデータ一括削除(削除エラー終了)
        /// 
        /// 待ち合わせを行うこと
        /// 削除エラー終了後にローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedWhileDeletingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 待ち合わせを行うこと
            // 削除エラー終了後にローカルデータ一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteObjectError(_testObj).Wait(),
                () => DeleteCacheAllTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除終了後の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedAfterDeleting()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteObjectTest(true).Wait();

            // 作成できること
            CreateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除終了後の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedAfterDeleting()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteObjectTest(true).Wait();

            // 読込できること
            QueryObjectTest(1).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除終了後の更新
        /// 
        /// 更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedAfterDeleting()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteObjectTest(true).Wait();

            // 更新できること
            UpdateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除終了後の削除
        /// 
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedAfterDeleting()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteObjectTest(true).Wait();

            // 削除できること
            DeleteObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除終了後のオブジェクト一括削除
        /// 
        /// オブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedAfterDeleting()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteObjectTest(true).Wait();

            // オブジェクト一括削除できること
            DeleteAllObjectsTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除終了後のローカルデータ一括削除
        /// 
        /// ローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedAfterDeleting()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteObjectTest(true).Wait();

            // ローカルデータ一括削除できること
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除エラー終了後の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedAfterDeletingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 削除エラー
            DeleteObjectError(_testObj).Wait();

            // 作成できること
            CreateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除エラー終了後の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedAfterDeletingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 削除エラー
            DeleteObjectError(_testObj).Wait();

            // 読込できること
            ReadObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除エラー終了後の更新
        /// 
        /// 更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedAfterDeletingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            var etag = _testObj.Etag;
            _testObj.Etag = "dummy";

            // 削除エラー
            DeleteObjectError(_testObj).Wait();

            // 更新できること
            _testObj.Etag = etag;
            UpdateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除エラー終了後の削除
        /// 
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedAfterDeletingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            var etag = _testObj.Etag;
            _testObj.Etag = "dummy";

            // 削除エラー
            DeleteObjectError(_testObj).Wait();

            // 削除できること
            _testObj.Etag = etag;
            DeleteObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除エラー終了後のオブジェクト一括削除
        /// 
        /// オブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedAfterDeletingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 削除エラー
            DeleteObjectError(_testObj).Wait();

            // オブジェクト一括削除できること
            DeleteAllObjectsTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（削除中のCRUD） - 削除エラー終了後のローカルデータ一括削除
        /// 
        /// ローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedAfterDeletingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 同期してETagを生成する
            SyncTest(true).Wait();

            // 再取得する
            ReadObjectTest(true).Wait();

            // ETagを変更する
            _testObj.Etag = "dummy";

            // 削除エラー
            DeleteObjectError(_testObj).Wait();

            // ローカルデータ一括削除できること
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中の同期） - オブジェクト一括削除中のバケット同期
        /// 
        /// 待ち合わせを行うこと
        /// オブジェクト一括削除終了後に同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileDeletingAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // オブジェクト一括削除終了後に同期できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteAllObjectsTest(true).Wait(),
                () => SyncTest(true, 5).Wait()
            });

            // サーバにクエリをかける
            var results = _onlineBucket.QueryAsync(new NbQuery()).Result.ToList();
            Assert.AreEqual(0, results.Count);
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中の同期） - オブジェクト一括削除後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterDeletingAllObjects()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 削除を実施
            DeleteAllObjectsTest(true).Wait();

            // オブジェクト一括削除終了後に同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除中の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedWhileDeletingAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 作成できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteAllObjectsTest(true).Wait(),
                () => CreateObjectTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除中の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedWhileDeletingAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 読込できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteAllObjectsTest(true).Wait(),
                () => QueryObjectTest(1, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除中の更新
        /// 
        /// 待ち合わせを行うこと
        /// オブジェクト一括削除終了後に更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedWhileDeletingAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // オブジェクト一括削除終了後に更新できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteAllObjectsTest(true).Wait(),
                () => UpdateObjectTest(true, 5).Wait()
            });

            // ローカルにクエリをかける
            var query = new NbQuery().DeleteMark(false);
            var result = _offlineBucket.QueryAsync(query).Result.ToList();
            Assert.AreEqual(1, result.Count);
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除中の削除
        /// 
        /// 待ち合わせを行うこと
        /// オブジェクト一括削除終了後に削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedWhileDeletingAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // オブジェクト一括削除終了後に削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteAllObjectsTest(true).Wait(),
                () => DeleteObjectTest(true, true, 5).Wait()
            });

            // ローカルにクエリをかける
            var query = new NbQuery().DeleteMark(false);
            var result = _offlineBucket.QueryAsync(query).Result.ToList();
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除中のオブジェクト一括削除
        /// 
        /// 待ち合わせを行うこと
        /// オブジェクト一括削除終了後にオブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedWhileDeletingAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // オブジェクト一括削除終了後にオブジェクト一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteAllObjectsTest(true).Wait(),
                () => DeleteAllObjectsTest(true, true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除中のローカルデータ一括削除
        /// 
        /// 待ち合わせを行うこと
        /// オブジェクト一括削除終了後にローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedWhileDeletingAllObjects()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // オブジェクト一括削除終了後にローカルデータ一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteAllObjectsTest(true).Wait(),
                () => DeleteCacheAllTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - ローカルデータ一括削除終了後の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedAfterDeletingAllObjects()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // オブジェクト一括削除を実施
            DeleteAllObjectsTest(true).Wait();

            // 作成できること
            CreateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除終了後の読込
        /// 
        /// 読込できること
        /// </summary>
        [Test]
        public void TestReadLockedAfterDeletingAllObjects()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // オブジェクト一括削除を実施
            DeleteAllObjectsTest(true).Wait();

            // 読込できること
            QueryObjectTest(1).Wait();
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除終了後の更新
        /// 
        /// 更新できること
        /// </summary>
        [Test]
        public void TestUpdateLockedAfterDeletingAllObjects()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // オブジェクト一括削除を実施
            DeleteAllObjectsTest(true).Wait();

            // 更新できること
            UpdateObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除終了後の削除
        /// 
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteLockedAfterDeletingAllObjects()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // オブジェクト一括削除を実施
            DeleteAllObjectsTest(true).Wait();

            // 削除できること
            DeleteObjectTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除終了後のオブジェクト一括削除
        /// 
        /// オブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedAfterDeletingAllObjects()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // オブジェクト一括削除を実施
            DeleteAllObjectsTest(true).Wait();

            // オブジェクト一括削除できること
            DeleteAllObjectsTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（オブジェクト一括削除中のCRUD） - オブジェクト一括削除終了後のローカルデータ一括削除
        /// 
        /// ローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedAfterDeletingAllObjects()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // オブジェクト一括削除を実施
            DeleteAllObjectsTest(true).Wait();

            // ローカルデータ一括削除できること
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中の同期） - ローカルデータ一括削除中のバケット同期
        /// 
        /// 待ち合わせを行うこと
        /// 同期範囲が削除されたため、InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileDeletingCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // 同期範囲が削除されたため、InvalidOperationExceptionが発生すること
            Parallel.Invoke(new Action[]
            {
                () => DeleteCacheAllTest(true).Wait(),
                () => 
                {
                    Thread.Sleep(10);
                    try
                    {
                        _syncManager.SyncBucketAsync(ITUtil.ObjectBucketName, NbObjectConflictResolver.PreferClientResolver).Wait();
                    }
                    catch (AggregateException e)
                    {
                        if (!(e.InnerException is InvalidOperationException))
                        {
                            Assert.Fail("Bad route");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中の同期） - ローカルデータ一括削除後のバケット同期
        /// 
        /// 同期範囲が削除されたため、InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterDeletingCacheAll()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // ローカルデータ一括削除を実施
            DeleteAllObjectsTest(true).Wait();

            // 同期範囲が削除されたため、InvalidOperationExceptionが発生すること
            try
            {
                _syncManager.SyncBucketAsync(ITUtil.ObjectBucketName, NbObjectConflictResolver.PreferClientResolver).Wait();

            }
            catch (AggregateException e)
            {
                if (!(e.InnerException is InvalidOperationException))
                {
                    Assert.Fail("Bad route");
                }
            }
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除中の作成
        /// 
        /// オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
        /// </summary>
        [Test]
        public void TestCreateLockedWhileDeletingCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
            Parallel.Invoke(new Action[]
            {
                () => DeleteCacheAllTest(true).Wait(),
                () => CreateObjectTestAfterDeleteCacheAll(false, 1).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除中の読込
        /// 
        /// オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
        /// </summary>
        [Test]
        public void TestReadLockedWhileDeletingCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
            Parallel.Invoke(new Action[]
            {
                () => DeleteCacheAllTest(true).Wait(),
                () => QueryObjectTestAfterDeleteCacheAll(false, 1).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除中の更新
        /// 
        /// オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
        /// </summary>
        [Test]
        public void TestUpdateLockedWhileDeletingCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
            Parallel.Invoke(new Action[]
            {
                () => DeleteCacheAllTest(true).Wait(),
                () => UpdateObjectTestAfterDeleteCacheAll(0, 3).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除中の削除
        /// 
        /// オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
        /// </summary>
        [Test]
        public void TestDeleteLockedWhileDeletingCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
            Parallel.Invoke(new Action[]
            {
                () => DeleteCacheAllTest(true).Wait(),
                () => DeleteObjectTestAfterDeleteCacheAll(0, 3).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除中のオブジェクト一括削除
        /// 
        /// オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedWhileDeletingCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // オブジェクトテーブルが存在しないため、SQLiteExceptionが発生すること
            Parallel.Invoke(new Action[]
            {
                () => DeleteCacheAllTest(true).Wait(),
                () => DeleteAllObjectsTestAfterDeleteCacheAll(false, 3).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除中のローカルデータ一括削除
        /// 
        /// 待ち合わせを行うこと
        /// ローカルデータ一括削除終了後にローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedWhileDeletingCacheAll()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 待ち合わせを行うこと
            // ローカルデータ一括削除終了後にローカルデータ一括削除できること
            Parallel.Invoke(new Action[]
            {
                () => DeleteCacheAllTest(true).Wait(),
                () => DeleteCacheAllTest(true, 3).Wait()
            });
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除終了後の作成
        /// 
        /// 作成できること
        /// </summary>
        [Test]
        public void TestCreateLockedAfterDeletingCacheAll()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // ローカルデータ一括削除を実施
            DeleteCacheAllTest(true).Wait();

            // 作成できること
            CreateObjectTestAfterDeleteCacheAll(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除終了後の読込
        /// 
        /// 対象オブジェクトが存在しないため、0件ヒットすること
        /// エラーが発生しないこと
        /// </summary>
        [Test]
        public void TestReadLockedAfterDeletingCacheAll()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // ローカルデータ一括削除を実施
            DeleteCacheAllTest(true).Wait();

            // 対象オブジェクトが存在しないため、0件ヒットすること
            // エラーが発生しないこと
            QueryObjectTestAfterDeleteCacheAll(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除終了後の更新
        /// 
        /// 対象オブジェクトが存在しないため、NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public void TestUpdateLockedAfterDeletingCacheAll()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // ローカルデータ一括削除を実施
            DeleteCacheAllTest(true).Wait();

            // 対象オブジェクトが存在しないため、NbHttpExceptionが発生すること
            UpdateObjectTestAfterDeleteCacheAll(1).Wait();
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除終了後の削除
        /// 
        /// 対象オブジェクトが存在しないため、NbHttpExceptionが発生すること
        /// </summary>
        [Test]
        public void TestDeleteLockedAfterDeletingCacheAll()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // ローカルデータ一括削除を実施
            DeleteCacheAllTest(true).Wait();

            // 対象オブジェクトが存在しないため、NbHttpExceptionが発生すること
            DeleteObjectTestAfterDeleteCacheAll(1).Wait();
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除終了後のオブジェクト一括削除
        /// 
        /// オブジェクト一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteAllObjectsLockedAfterDeletingCacheAll()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // ローカルデータ一括削除を実施
            DeleteCacheAllTest(true).Wait();

            // オブジェクト一括削除できること
            DeleteAllObjectsTestAfterDeleteCacheAll(true).Wait();
        }

        /// <summary>
        /// 更新・削除排他（ローカルデータ一括削除中のCRUD） - ローカルデータ一括削除終了後のローカルデータ一括削除
        /// 
        /// ローカルデータ一括削除できること
        /// </summary>
        [Test]
        public void TestDeleteCacheAllLockedAfterDeletingCacheAll()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // ローカルデータ一括削除を実施
            DeleteCacheAllTest(true).Wait();

            // ローカルデータ一括削除できること
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// 作成・読込中の同期 - 作成中のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileCreating()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期できること
            Parallel.Invoke(new Action[]
            {
                () => CreateObjectTest(true).Wait(),
                () => SyncTest(true, 10).Wait()
            });
        }

        /// <summary>
        /// 作成・読込中の同期 - 作成後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterCreating()
        {
            // 作成を実施
            CreateObjectTest(true).Wait();

            // 同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// 作成・読込中の同期 - 読込中のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileReading()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期できること
            Parallel.Invoke(new Action[]
            {
                () => ReadObjectTest(true).Wait(),
                () => SyncTest(true, 5).Wait(),
            });
        }

        /// <summary>
        /// 作成・読込中の同期 - 読込中のバケット同期(読込エラー終了)
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedWhileReadingError()
        {
            // オブジェクトを複数個作成する
            for (int i = 0; i < 100; i++)
            {
                CreateTestObject().Wait();
            }

            // 同期できること
            Parallel.Invoke(new Action[]
            {
                () => ReadObjectError().Wait(),
                () => SyncTest(true, 5).Wait()
            });
        }

        /// <summary>
        /// 作成・読込中の同期 - 読込後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterReading()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 読込を実施
            ReadObjectTest(true).Wait();

            // 同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// 作成・読込中の同期 - 読込エラー終了後のバケット同期
        /// 
        /// 同期できること
        /// </summary>
        [Test]
        public void TestSyncLockedAfterReadingError()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 読込エラー
            ReadObjectError().Wait();

            // 同期できること
            SyncTest(true).Wait();
        }

        /// <summary>
        /// CRUDとCRUDの組み合わせテスト
        /// 
        /// 複数スレッドでCRUDとCRUDを行い、デッドロックが起きないかを確認する
        /// </summary>
        [Test]
        public void TestCrudAndCrudInMultiThread()
        {
            _updateSuccessCount = 0;
            _updateErrCount = 0;
            _deleteSuccessCount = 0;
            _deleteErrCount = 0;

            // 更新用スレッド
            Task updateTask = new Task(() =>
            {
                for (int i = 0; i < CrudLoopCount; i++)
                {
                    CreateTestObject().Wait();
                    UpdateObjectTestInMultiThread().Wait();
                    Thread.Sleep(500);
                }
            });
            updateTask.Start();

            // 削除用スレッド
            Task deleteTask = new Task(() =>
            {
                for (int i = 0; i < CrudLoopCount; i++)
                {
                    CreateTestObject().Wait();
                    DeleteObjectTestInMultiThread().Wait();
                    Thread.Sleep(500);
                }
            });
            deleteTask.Start();

            updateTask.Wait();
            deleteTask.Wait();

            Debug.WriteLine("TestCrudAndCrudInMultiThread: updateSuccessCount = " + _updateSuccessCount);
            Debug.WriteLine("TestCrudAndCrudInMultiThread: updateErrCount = " + _updateErrCount);
            Debug.WriteLine("TestCrudAndCrudInMultiThread: deleteSuccessCount = " + _deleteSuccessCount);
            Debug.WriteLine("TestCrudAndCrudInMultiThread: deleteErrCount = " + _deleteErrCount);

            Assert.AreEqual(CrudLoopCount, _updateSuccessCount + _updateErrCount);
            Assert.AreEqual(CrudLoopCount, _deleteSuccessCount + _deleteErrCount);
        }

        /// <summary>
        /// CRUDと同期の組み合わせテスト
        /// 
        /// 複数スレッドでCRUDと同期を行い、デッドロックが起きないかを確認する
        /// </summary>
        [Test]
        public void TestSyncAndCrudInMultiThread()
        {
            _updateSuccessCount = 0;
            _updateErrCount = 0;
            _deleteSuccessCount = 0;
            _deleteErrCount = 0;
            _sync1SuccessCount = 0;
            _sync1ErrCount = 0;

            // 更新・削除用スレッド
            Task crudTask = new Task(() =>
            {
                for (int i = 0; i < CrudLoopCount; i++)
                {
                    CreateTestObject().Wait();
                    UpdateObjectTestInMultiThread().Wait();
                    DeleteObjectTestInMultiThread().Wait();
                    Thread.Sleep(500);
                }
            });
            crudTask.Start();

            // 同期用スレッド
            Task syncTask = new Task(() =>
            {
                for (int i = 0; i < SyncLoopCount; i++)
                {
                    //CreateTestObject().Wait();
                    SyncTest1InMultiThread().Wait();
                    Thread.Sleep(500);
                }
            });
            syncTask.Start();

            crudTask.Wait();
            syncTask.Wait();

            Debug.WriteLine("TestSyncAndCrudInMultiThread: updateSuccessCount = " + _updateSuccessCount);
            Debug.WriteLine("TestSyncAndCrudInMultiThread: updateErrCount = " + _updateErrCount);
            Debug.WriteLine("TestSyncAndCrudInMultiThread: deleteSuccessCount = " + _deleteSuccessCount);
            Debug.WriteLine("TestSyncAndCrudInMultiThread: deleteErrCount = " + _deleteErrCount);
            Debug.WriteLine("TestSyncAndCrudInMultiThread: syncSuccessCount = " + _sync1SuccessCount);
            Debug.WriteLine("TestSyncAndCrudInMultiThread: syncErrCount = " + _sync1ErrCount);

            Assert.AreEqual(CrudLoopCount, _updateSuccessCount + _updateErrCount);
            Assert.AreEqual(CrudLoopCount, _deleteSuccessCount + _deleteErrCount);
            Assert.AreEqual(SyncLoopCount, _sync1SuccessCount + _sync1ErrCount);
        }

        /// <summary>
        /// 同期と同期の組み合わせテスト
        /// 
        /// 複数スレッドで同期と同期を行い、デッドロックが起きないかを確認する
        /// </summary>
        [Test]
        public void TestSyncAndSyncInMultiThread()
        {
            _sync1SuccessCount = 0;
            _sync1ErrCount = 0;
            _sync2SuccessCount = 0;
            _sync2ErrCount = 0;

            // 同期用スレッド
            Task sync1Task = new Task(() =>
            {
                for (int i = 0; i < SyncLoopCount; i++)
                {
                    CreateTestObject().Wait();
                    SyncTest1InMultiThread().Wait();
                    Thread.Sleep(500);
                }
            });
            sync1Task.Start();

            // 同期用スレッド
            Task sync2Task = new Task(() =>
            {
                for (int i = 0; i < SyncLoopCount; i++)
                {
                    CreateTestObject().Wait();
                    SyncTest2InMultiThread().Wait();
                    Thread.Sleep(500);
                }
            });
            sync2Task.Start();

            sync1Task.Wait();
            sync2Task.Wait();

            Debug.WriteLine("TestSyncAndSyncInMultiThread: syncSuccessCount1 = " + _sync1SuccessCount);
            Debug.WriteLine("TestSyncAndSyncInMultiThread: syncErrCount1 = " + _sync1ErrCount);
            Debug.WriteLine("TestSyncAndSyncInMultiThread: syncSuccessCount2 = " + _sync2SuccessCount);
            Debug.WriteLine("TestSyncAndSyncInMultiThread: syncErrCount2 = " + _sync2ErrCount);

            Assert.AreEqual(SyncLoopCount, _sync1SuccessCount + _sync1ErrCount);
            Assert.AreEqual(SyncLoopCount, _sync2SuccessCount + _sync2ErrCount);
        }

        // 以下はAndroidで行っている、CRUD状態を擬似的に作り出して行う評価

        /// <summary>
        /// CRUD処理の排他テスト
        /// 
        /// CRUD中同期を確認
        /// 待ち合わせを行うことも確認
        /// </summary>
        [Test]
        public void TestSyncLockedWhileCrud()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            Task task = new Task(() =>
            {
                Thread.Sleep(500);
                Debug.WriteLine("Call EndCrud.");
                _processState.EndCrud();
            });
            task.Start();

            SyncTest(true).Wait();
        }

        /// <summary>
        /// CRUD処理の排他テスト
        /// 
        /// CRUD中CRUDを確認
        /// 待ち合わせを行うことも確認
        /// </summary>
        [Test]
        public void TestCrudLockedWhileCrud()
        {
            // オブジェクト作成
            CreateTestObject().Wait();

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            // CRUD中は作成できる
            CreateObjectTest(true).Wait();

            // CRUD中は読み込みできる
            ReadObjectTest(true).Wait();

            Task task = new Task(() =>
            {
                Thread.Sleep(500);
                Debug.WriteLine("Call EndCrud.");
                _processState.EndCrud();
            });
            task.Start();

            // 待ち合わせを行い、CRUD終了後に更新する
            UpdateObjectTest(true).Wait();

            task.Wait();

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            task = new Task(() =>
            {
                Thread.Sleep(500);
                Debug.WriteLine("Call EndCrud.");
                _processState.EndCrud();
            });
            task.Start();

            // 待ち合わせを行い、CRUD終了後に削除する
            DeleteObjectTest(true).Wait();

            task.Wait();

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            task = new Task(() =>
            {
                Thread.Sleep(500);
                Debug.WriteLine("Call EndCrud.");
                _processState.EndCrud();
            });
            task.Start();

            // 待ち合わせを行い、CRUD終了後にオブジェクト一括削除する
            DeleteAllObjectsTest(true).Wait();

            task.Wait();

            // 擬似的にCRUD状態を作る
            _processState.TryStartCrud();

            task = new Task(() =>
            {
                Thread.Sleep(500);
                Debug.WriteLine("Call EndCrud.");
                _processState.EndCrud();
            });
            task.Start();

            // 待ち合わせを行い、CRUD終了後にローカルデータ一括削除する
            DeleteCacheAllTest(true).Wait();
        }

        /// <summary>
        /// テストオブジェクトを生成する。
        /// </summary>
        /// <returns>NbOfflineObject</returns>
        private async Task<NbOfflineObject> CreateTestObject()
        {
            // 初期化
            _testObj = null;

            var obj = _offlineBucket.NewObject();
            obj["testKey"] = "testValue";
            obj.Acl = NbAcl.CreateAclForAnonymous();

            var tempObj = await obj.SaveAsync();
            _testObj = tempObj as NbOfflineObject;

            return _testObj;
        }

        /// <summary>
        /// 同期テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param> 
        /// <returns>Task</returns>
        private async Task SyncTest(bool isSuccess, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                await _syncManager.SyncBucketAsync(ITUtil.ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                Assert.AreEqual("Locked.", e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 作成テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        private async Task CreateObjectTest(bool isSuccess, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                await CreateTestObject();
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                Assert.AreEqual("Locked.", e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 作成テスト(ローカルデータ一括削除後)
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        private async Task CreateObjectTestAfterDeleteCacheAll(bool isSuccess, int delayedTime = 0)
        {
            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName, _service);

            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                await CreateTestObject();
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (Exception e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// 読込テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        private async Task ReadObjectTest(bool isSuccess, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                _testObj = await _offlineBucket.GetAsync(_testObj.Id);
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                Assert.AreEqual("Locked.", e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 読込(クエリ)テスト
        /// </summary>
        /// <param name="expectedCount">期待する検索カウント数</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        private async Task QueryObjectTest(int expectedCount, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                var query = new NbQuery().EqualTo("_id", _testObj.Id).DeleteMark(true);
                var result = await _offlineBucket.QueryAsync(query);
                Assert.AreEqual(expectedCount, result.ToList().Count);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 読込テスト(ローカルデータ一括削除後)
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        private async Task QueryObjectTestAfterDeleteCacheAll(bool isSuccess, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName, _service);

            try
            {
                var query = new NbQuery().EqualTo("_id", _testObj.Id).DeleteMark(true);
                var result = await _offlineBucket.QueryAsync(query);
                Assert.AreEqual(0, result.ToList().Count);
                if (!isSuccess)
                {
                    // 成功以外の場合
                    Assert.Fail("Bad route");
                }
            }
            catch (Exception e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// 更新テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param> 
        /// <returns>Task</returns>
        private async Task UpdateObjectTest(bool isSuccess, int delayedTime = 0)
        {
            _testObj["testKey1"] = "testValue1";
            await UpdateObjectTest(_testObj, isSuccess, delayedTime);
        }

        /// <summary>
        /// 更新テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param> 
        /// <returns>Task</returns>
        private async Task UpdateObjectTest2(bool isSuccess, int delayedTime = 0)
        {
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, _testObj.ToJson(), _service);
            obj["testKey2"] = "testValue2";
            await UpdateObjectTest(obj, isSuccess, delayedTime);
        }

        /// <summary>
        /// 更新テスト
        /// </summary>
        /// <param name="obj">オフラインオブジェクト</param>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param> 
        /// <returns>Task</returns>
        private async Task UpdateObjectTest(NbOfflineObject obj, bool isSuccess, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                await obj.SaveAsync();
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                Assert.AreEqual("Locked.", e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 更新テスト(ローカルデータ一括削除後)
        /// </summary>
        /// <param name="pattern">パターン</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        /// <remarks>SQLiteExceptionを期待する場合は0、NbHttpExceptionを期待する場合は1</remarks>
        private async Task UpdateObjectTestAfterDeleteCacheAll(int pattern, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName, _service);
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, _testObj.ToJson(), _service);

            obj["testKey1"] = "testValue1";
            try
            {
                await obj.SaveAsync();
                Assert.Fail("Bad route");
            }
            catch (Exception e)
            {
                if (pattern == 1)
                {
                    if (!(e is NbHttpException))
                    {
                        Assert.Fail("Bad route");
                    }
                }
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// 削除テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="softDelete">論理削除する場合は true</param>
        /// <param name="delayedTime">開始遅延時間</param> 
        /// <returns>Task</returns>
        private async Task DeleteObjectTest(bool isSuccess, bool softDelete = true, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                await _testObj.DeleteAsync(softDelete);
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                Assert.AreEqual("Locked.", e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 削除テスト(ローカルデータ一括削除後)
        /// </summary>
        /// <param name="pattern">パターン</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        /// <remarks>SQLiteExceptionを期待する場合は0、NbHttpExceptionを期待する場合は1</remarks>
        private async Task DeleteObjectTestAfterDeleteCacheAll(int pattern, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName, _service);
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName, _testObj.ToJson(), _service);

            try
            {
                await obj.DeleteAsync();
                Assert.Fail("Bad route");
            }
            catch (Exception e)
            {
                if (pattern == 1)
                {
                    if (!(e is NbHttpException))
                    {
                        Assert.Fail("Bad route");
                    }
                }
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// オブジェクト一括削除テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="softDelete">論理削除する場合は true</param>
        /// <param name="delayedTime">開始遅延時間</param> 
        /// <returns>Task</returns>
        private async Task DeleteAllObjectsTest(bool isSuccess, bool softDelete = true, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                await _offlineBucket.DeleteAsync(new NbQuery(), softDelete);
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                Assert.AreEqual("Locked.", e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オブジェクト一括削除テスト(ローカルデータ一括削除後)
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param>
        /// <returns>Task</returns>
        private async Task DeleteAllObjectsTestAfterDeleteCacheAll(bool isSuccess, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            _offlineBucket = new NbOfflineObjectBucket<NbOfflineObject>(ITUtil.ObjectBucketName, _service);

            try
            {
                await _offlineBucket.DeleteAsync(new NbQuery());
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (Exception e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// ローカルデータ一括削除テスト
        /// </summary>
        /// <param name="isSuccess">成功を期待する場合、true</param>
        /// <param name="delayedTime">開始遅延時間</param> 
        /// <returns>Task</returns>
        private async Task DeleteCacheAllTest(bool isSuccess, int delayedTime = 0)
        {
            if (delayedTime > 0)
            {
                Thread.Sleep(delayedTime);
            }

            try
            {
                await NbOfflineService.DeleteCacheAll();
                if (!isSuccess)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbException e)
            {
                if (isSuccess)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                Assert.AreEqual("Locked.", e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オブジェクト読込エラー
        /// </summary>
        /// <returns>Task</returns>
        private async Task ReadObjectError()
        {
            try
            {
                await _offlineBucket.GetAsync("dummy");
                Assert.Fail("Bad route");
            }
            catch (NbHttpException)
            {
                // ok
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オブジェクト更新エラー
        /// </summary>
        /// <param name="obj">オフラインオブジェクト</param>
        /// <returns>Task</returns>
        private async Task UpdateObjectError(NbOfflineObject obj)
        {
            try
            {
                await obj.SaveAsync();
                Assert.Fail("Bad route");
            }
            catch (NbHttpException)
            {
                // ok
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// オブジェクト削除エラー
        /// </summary>
        /// <param name="obj">オフラインオブジェクト</param>
        /// <returns>Task</returns>
        private async Task DeleteObjectError(NbOfflineObject obj)
        {
            try
            {
                await obj.DeleteAsync(true);
                Assert.Fail("Bad route");
            }
            catch (NbHttpException)
            {
                // ok
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 更新テスト 組み合わせ試験用
        /// </summary>
        /// <returns>Task</returns>
        private async Task UpdateObjectTestInMultiThread()
        {
            try
            {
                await _testObj.SaveAsync();
                _updateSuccessCount++;
            }
            catch (NbException e)
            {
                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                _updateErrCount++;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 削除テスト 組み合わせ試験用
        /// </summary>
        /// <returns>Task</returns>
        private async Task DeleteObjectTestInMultiThread()
        {
            try
            {
                await _testObj.DeleteAsync();
                _deleteSuccessCount++;
            }
            catch (NbException e)
            {
                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                _deleteErrCount++;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 同期テスト 組み合わせ試験用
        /// </summary>
        /// <returns>Task</returns>
        private async Task SyncTest1InMultiThread()
        {
            try
            {
                await _syncManager.SyncBucketAsync(ITUtil.ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
                _sync1SuccessCount++;
            }
            catch (NbException e)
            {
                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                _sync1ErrCount++;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// 同期テスト 組み合わせ試験用
        /// </summary>
        /// <returns>Task</returns>
        private async Task SyncTest2InMultiThread()
        {
            try
            {
                await _syncManager.SyncBucketAsync(ITUtil.ObjectBucketName, NbObjectConflictResolver.PreferClientResolver);
                _sync2SuccessCount++;
            }
            catch (NbException e)
            {
                Assert.AreEqual(NbStatusCode.Locked, e.StatusCode);
                _sync2ErrCount++;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }
    }
}
