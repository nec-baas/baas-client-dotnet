using Nec.Nebula.Internal.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class NbObjectCacheTest
    {
        private NbDatabaseImpl _database;
        private NbObjectCache _objectCache;

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();

            _database = new NbDatabaseImpl(":memory:", null);
            _database.Open();

            var offlineService = new NbOfflineService();
            offlineService.Database = _database;
            NbService.Singleton.OfflineService = offlineService;

            _objectCache = new NbObjectCache(NbService.Singleton);

            _objectCache.CreateCacheTable("test1");
            _objectCache.CreateCacheTable("test1"); // 2回実行してもエラーにならないこと
        }

        [TearDown]
        public void TearDown()
        {
            _database.Close();
        }

        private void addTestDataForQuery()
        {
            var obj1 = new NbOfflineObject("test1");
            obj1["key"] = "a";
            _objectCache.InsertObject(obj1, NbSyncState.Sync);

            var obj2 = new NbOfflineObject("test1");
            obj2["key"] = "b";
            _objectCache.InsertObject(obj2, NbSyncState.Dirty);
        }

        private void addTestDataForQueryNum()
        {
            var obj1 = new NbOfflineObject("test1");
            obj1["key"] = 1;
            _objectCache.InsertObject(obj1, NbSyncState.Sync);

            var obj2 = new NbOfflineObject("test1");
            obj2["key"] = 2;
            _objectCache.InsertObject(obj2, NbSyncState.Dirty);

            var obj3 = new NbOfflineObject("test1");
            obj3["key"] = 0;
            _objectCache.InsertObject(obj3, NbSyncState.Dirty);

            var obj4 = new NbOfflineObject("test1");
            obj4["key"] = 0;
            _objectCache.InsertObject(obj4, NbSyncState.Dirty);
        }

        private void addTestDataForQueryarray()
        {
            var obj1 = new NbOfflineObject("test1");
            obj1["key"] = new string[] { "a", "b" };
            _objectCache.InsertObject(obj1, NbSyncState.Sync);

            var obj2 = new NbOfflineObject("test1");
            obj2["key"] = new string[] { "c", "d" };
            _objectCache.InsertObject(obj2, NbSyncState.Dirty);
        }

        private NbOfflineObject CreateObject()
        {
            var obj = new NbOfflineObject("test1");
            obj.Id = "12345";
            obj.CreatedAt = "CreatedAt";
            obj.UpdatedAt = "UpdatedAt";
            obj.Etag = "ETAG";
            obj.Acl = new NbAcl();
            obj["key"] = "value";

            return obj;
        }

        private void CreateAndInsertObjects(string id)
        {
            var obj = new NbOfflineObject("test1");
            obj.Id = id;
            obj.CreatedAt = "CreatedAt";
            obj.UpdatedAt = "UpdatedAt";
            obj.Etag = "ETAG";
            obj.Acl = new NbAcl();
            obj["key"] = "value";

            _objectCache.InsertObject(obj, NbSyncState.Dirty);
        }

        private bool IsObjectCacheExists()
        {
            var tables = _objectCache.GetTables();
            foreach (var table in tables)
            {
                using (var reader = _database.SelectForReader(table, new[] { "json", "state" }, null, null, 0, 1))
                {
                    if (reader.Read()) return true;
                }
            }
            return false;
        }

        [Test]
        public void TestSaveRead()
        {
            var obj = new NbOfflineObject("test1");
            //obj.Id = "1234567890";
            obj.CreatedAt = "CreatedAt";
            obj.UpdatedAt = "UpdatedAt";
            obj.Etag = "ETAG";
            obj.Acl = NbAcl.CreateAclForAnonymous();

            obj["key"] = 12345;

            _objectCache.InsertObject(obj, NbSyncState.Dirty);
            Assert.IsNotNull(obj.Id);

            var robj = _objectCache.FindObject<NbOfflineObject>("test1", obj.Id);

            Assert.NotNull(robj);
            Assert.AreEqual(obj.ToJson(), robj.ToJson());
        }

        [Test]
        public void TestUpdate()
        {
            var obj = new NbOfflineObject("test1");
            obj["key"] = 12345;
            obj.Acl = NbAcl.CreateAclForAnonymous();

            _objectCache.InsertObject(obj, NbSyncState.Dirty);
            Assert.IsNotNull(obj.Id);

            var robj = _objectCache.FindObject<NbOfflineObject>("test1", obj.Id);
            Assert.AreEqual(12345, robj["key"]);
            Assert.AreEqual(NbSyncState.Dirty, robj.SyncState);

            obj["key"] = 54321;
            _objectCache.UpdateObject(obj, NbSyncState.Sync);

            robj = _objectCache.FindObject<NbOfflineObject>("test1", obj.Id);

            Assert.AreEqual(54321, robj["key"]);
            Assert.AreEqual(NbSyncState.Sync, robj.SyncState);
        }

        [Test]
        public void TestDelete()
        {
            var obj = new NbOfflineObject("test1");
            _objectCache.InsertObject(obj, NbSyncState.Dirty);
            Assert.IsNotNull(obj.Id);

            Assert.AreEqual(1, _objectCache.DeleteObject(obj));
        }

        [Test]
        public void TestQuery()
        {
            addTestDataForQuery();

            // no where
            var result = _objectCache.QueryObjects("test1", null, null);
            Assert.AreEqual(2, result.Count);

            result = _objectCache.QueryObjects("test1", "state = ?", new object[] { NbSyncState.Sync });
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void TestQueryDirty()
        {
            addTestDataForQuery();

            var result = _objectCache.QueryDirtyObjects("test1");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("b", result[0]["key"]);
        }

        [Test]
        public void TestMongoQuerySortSkipLimit()
        {
            addTestDataForQuery();

            // 昇順
            var query = new NbQuery().OrderBy("key");
            var result = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("a", result[0]["key"]);
            Assert.AreEqual("b", result[1]["key"]);

            // 降順
            query = new NbQuery().OrderBy("-key");
            result = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("b", result[0]["key"]);
            Assert.AreEqual("a", result[1]["key"]);

            // Limit 追加
            query = new NbQuery().OrderBy("key").Limit(1);
            result = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("a", result[0]["key"]);

            // Skip, Limit 追加
            query = new NbQuery().OrderBy("key").Skip(1).Limit(1);
            result = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("b", result[0]["key"]);
        }

        [Test]
        public void TestDeleteCacheTable()
        {
            _objectCache.DeleteCacheTable("test1");

            // table が存在しなくても例外にならないこと
            _objectCache.DeleteCacheTable("test1");
        }

        [Test]
        public void TestQueryPerformance()
        {
            using (var transaction = _database.BeginTransaction())
            {
                const int count = 10000;
                var start = DateTime.Now;

                for (var i = 0; i < count; i++)
                {
                    var obj = new NbOfflineObject("test1");
                    //obj.Id = i.ToString();
                    obj.CreatedAt = "CreatedAt";
                    obj.UpdatedAt = "UpdatedAt";
                    obj.Etag = "ETAG";
                    obj.Acl = new NbAcl();

                    obj["key"] = i;

                    _objectCache.InsertObject(obj, NbSyncState.Dirty);
                }
                var elapsed = DateTime.Now - start;
                Debug.Print("insert time (" + count + ") = " + elapsed);

                // クエリ実行
                var query = new NbQuery().EqualTo("key", 0);

                start = DateTime.Now;

                var results = _objectCache.MongoQueryObjects("test1", query).ToList();

                elapsed = DateTime.Now - start;
                Debug.Print("query time = " + elapsed);

                Assert.AreEqual(1, results.Count());

                var robj = results.First();
                Assert.AreEqual(0, robj.Get<int>("key"));


                transaction.Commit();
            }
        }

        /**
         * Constructor(NbObjectCache)
         **/

        /// <summary>
        /// コンストラクタ（正常）
        /// インスタンスが正常に作成できること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var objectcache = new NbObjectCache(NbService.Singleton);
            FieldInfo context = typeof(NbObjectCache).GetField("_service", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(context.GetValue(objectcache));
            Assert.AreEqual(context.GetValue(objectcache), NbService.Singleton);
        }

        /// <summary>
        /// コンストラクタ（サービスがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionNoService()
        {
            new NbObjectCache(null);
        }


        /**
         * CreateCacheTable
         **/

        /// <summary>
        /// オブジェクトキャッシュテーブルを生成する。（正常）
        /// テーブルが生成されること
        /// </summary>
        [Test]
        public void TestCreateCacheTableNormal()
        {
            Assert.AreEqual(1, _objectCache.GetTables().Count());
            Assert.IsTrue(_objectCache.GetTables().Contains("OBJECT_test1"));
            _objectCache.CreateCacheTable("objectTest");
            Assert.AreEqual(2, _objectCache.GetTables().Count());
            Assert.IsTrue(_objectCache.GetTables().Contains("OBJECT_objectTest"));

            // 同じバケット名ですでに存在しているテーブルを作成しても問題ないこと
            _objectCache.CreateCacheTable("objectTest");
            Assert.AreEqual(2, _objectCache.GetTables().Count());
            Assert.IsTrue(_objectCache.GetTables().Contains("OBJECT_objectTest"));
        }

        /// <summary>
        /// オブジェクトキャッシュテーブルを生成する。（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateCacheTableExceptionNoBucketName()
        {
            _objectCache.CreateCacheTable(null);
        }


        /**
         * DeleteCacheTable
         **/

        /// <summary>
        /// オブジェクトキャッシュテーブルを DROP する。（正常）
        /// テーブルが削除されること
        /// </summary>
        [Test]
        public void TestDeleteCacheTableNormal()
        {
            _objectCache.DeleteCacheTable("test1");
            Assert.IsEmpty(_objectCache.GetTables());

            // 存在しないテーブルを削除してもエラーにならないこと
            _objectCache.DeleteCacheTable("test1");
            Assert.IsEmpty(_objectCache.GetTables());
        }

        /// <summary>
        /// オブジェクトキャッシュテーブルを DROP する。（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestDeleteCacheTableExceptionNoBucketName()
        {
            _objectCache.DeleteCacheTable(null);
        }


        /**
         * InsertObject
         **/

        /// <summary>
        /// オブジェクトをキャッシュに保存する（正常）
        /// オブジェクトを保存できること
        /// </summary>
        [Test]
        public void TestInsertObjectNormal()
        {
            Assert.IsFalse(IsObjectCacheExists());
            _objectCache.InsertObject(CreateObject(), NbSyncState.Dirty);
            Assert.IsTrue(IsObjectCacheExists());
        }

        /// <summary>
        /// オブジェクトをキャッシュに保存する（空のobj）
        /// オブジェクトを保存できること
        /// </summary>
        [Test]
        public void TestInsertObjectNormalUnsetObjValues()
        {
            Assert.IsFalse(IsObjectCacheExists());
            var obj = new NbOfflineObject("test1");
            _objectCache.InsertObject(obj, NbSyncState.Dirty);
            Assert.IsTrue(IsObjectCacheExists());
            Assert.IsNotNull(obj.Id);
        }

        /// <summary>
        /// オブジェクトをキャッシュに保存する（objがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestInsertObjectExceptionNoObj()
        {
            _objectCache.InsertObject(null, NbSyncState.Dirty);
        }


        /**
         * FindObject
         **/

        /// <summary>
        /// キャッシュからオブジェクトを読み込む（正常）
        /// オブジェクトを読み込めること
        /// </summary>
        [Test]
        public void TestFindObjectNormal()
        {
            var obj = CreateObject();
            _objectCache.InsertObject(obj, NbSyncState.Dirty);
            var found = _objectCache.FindObject<NbOfflineObject>("test1", obj.Id);
            Assert.IsNotNull(found);
            Assert.AreEqual(found.Id, obj.Id);
            Assert.AreEqual(found.CreatedAt, obj.CreatedAt);
            Assert.AreEqual(found.UpdatedAt, obj.UpdatedAt);
            Assert.AreEqual(found.Etag, obj.Etag);
            Assert.AreEqual(obj.ToJson(), found.ToJson());
        }

        /// <summary>
        /// キャッシュからオブジェクトを読み込む（ヒットなし）
        /// オブジェクトが読み込めないこと
        /// </summary>
        [Test]
        public void TestFindObjectNormalNoHit()
        {
            Assert.IsNull(_objectCache.FindObject<NbOfflineObject>("test1", "234"));
        }

        /// <summary>
        /// キャッシュからオブジェクトを読み込む（ObjectIdがnull）
        /// オブジェクトが読み込めないこと
        /// </summary>
        [Test]
        public void TestFindObjectSubNormalNoObjectId()
        {
            Assert.IsNull(_objectCache.FindObject<NbOfflineObject>("test1", null));
        }

        /// <summary>
        /// キャッシュからオブジェクトを読み込む（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestFindObjectExceptionNoBucketName()
        {
            _objectCache.FindObject<NbOfflineObject>(null, null);
        }


        /**
         * QueryObjects
         **/

        /// <summary>
        /// オブジェクトクエリ (SQLレベル)（正常）
        /// オブジェクトを読み込めること
        /// </summary>
        [Test]
        public void TestQueryObjectsNormal()
        {
            addTestDataForQuery();
            var results = _objectCache.QueryObjects("test1", "state = ?", new object[] { NbSyncState.Dirty });
            Assert.AreEqual(1, results.Count);
        }

        /// <summary>
        /// オブジェクトクエリ (SQLレベル)（ヒットなし）
        /// オブジェクトが読み込めないこと
        /// </summary>
        [Test]
        public void TestQueryObjectsNormalNoHit()
        {
            var results = _objectCache.QueryObjects("test1", "state = ?", new object[] { NbSyncState.Dirty });
            Assert.IsEmpty(results);

            addTestDataForQuery();
            results = _objectCache.QueryObjects("test1", "state = ?", new object[] { "a" });
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトクエリ (SQLレベル)（WHRER,WHERE引数がnull）
        /// オブジェクトを全件読み込めること
        /// </summary>
        [Test]
        public void TestQueryObjectsNormalNoWhereWhereArgs()
        {
            addTestDataForQuery();
            var results = _objectCache.QueryObjects("test1", null, null);
            Assert.AreEqual(2, results.Count);
        }

        /// <summary>
        /// オブジェクトクエリ (SQLレベル)（WHEREがnull）
        /// オブジェクトを全件読み込めること
        /// </summary>
        [Test]
        public void TestQueryObjectsNormalNoWhere()
        {
            addTestDataForQuery();
            var results = _objectCache.QueryObjects("test1", null, new object[] { NbSyncState.Dirty });
            Assert.AreEqual(2, results.Count);
        }

        /// <summary>
        /// オブジェクトクエリ (SQLレベル)（WHERE引数がnull）
        /// SQLiteExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(SQLiteException))]
        public void TestQueryObjectsNormalNoWhereArgs()
        {
            addTestDataForQuery();
            var results = _objectCache.QueryObjects("test1", "state = ?", null);
        }

        /// <summary>
        /// オブジェクトクエリ (SQLレベル)（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestQueryObjectsExceptionNoBucketName()
        {
            _objectCache.QueryObjects(null, null, null);
        }


        /**
         * QueryDirtyObjects
         **/

        /// <summary>
        /// キャッシュから Dirty なオブジェクト一覧を読み込む（正常）
        /// オブジェクトを読み込めること
        /// </summary>
        [Test]
        public void TestQueryDirtyObjectsNormal()
        {
            addTestDataForQuery();
            var results = _objectCache.QueryDirtyObjects("test1");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("b", results[0]["key"]);
        }

        // <summary>
        /// キャッシュから Dirty なオブジェクト一覧を読み込む（Dirtyなオブジェクトなし）
        /// オブジェクトを読み込めないこと
        /// </summary>
        [Test]
        public void TestQueryDirtyObjectsNormalNoHit()
        {
            var obj = CreateObject();
            _objectCache.InsertObject(obj, NbSyncState.Sync);
            var results = _objectCache.QueryDirtyObjects("test1");
            Assert.IsEmpty(results);
        }

        // <summary>
        /// キャッシュから Dirty なオブジェクト一覧を読み込む（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestQueryDirtyObjectsExceptionNoBucketName()
        {
            _objectCache.QueryDirtyObjects(null);
        }


        /**
         * UpdateObject
         **/

        /// <summary>
        /// オブジェクトを更新する（正常）
        /// オブジェクトを更新できること
        /// </summary>
        [Test]
        public void TestUpdateObjectNormal()
        {
            var obj = CreateObject();
            _objectCache.InsertObject(obj, NbSyncState.Dirty);

            obj.Etag = "Etag2";
            obj["key"] = "value2";
            _objectCache.UpdateObject(obj, NbSyncState.Dirty);

            var found = _objectCache.FindObject<NbOfflineObject>("test1", obj.Id);
            Assert.IsNotNull(found);
            Assert.AreEqual(found.Id, obj.Id);
            Assert.AreEqual(found.CreatedAt, obj.CreatedAt);
            Assert.AreEqual(found.UpdatedAt, obj.UpdatedAt);
            Assert.AreEqual(found.Etag, "Etag2");
            Assert.AreEqual(found["key"], "value2");
        }

        /// <summary>
        /// オブジェクトを更新する（objがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestUpdateObjectExceptionNoObj()
        {
            _objectCache.UpdateObject(null, NbSyncState.Dirty);
        }

        /// <summary>
        /// オブジェクトを更新する（ObjectIdがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestUpdateObjectExceptionNoObjId()
        {
            _objectCache.UpdateObject(new NbObject("test"), NbSyncState.Dirty);
        }


        /**
         * DeleteObject
         **/

        /// <summary>
        /// オブジェクトを削除する（正常）
        /// オブジェクトを削除できること
        /// </summary>
        [Test]
        public void TestDeleteObjectNormal()
        {
            var obj = CreateObject();
            _objectCache.InsertObject(obj, NbSyncState.Dirty);
            Assert.IsTrue(IsObjectCacheExists());
            Assert.AreEqual(1, _objectCache.DeleteObject(obj));
            Assert.IsFalse(IsObjectCacheExists());
        }

        /// <summary>
        /// オブジェクトを削除する（objがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestDeleteObjectExceptionNoObj()
        {
            _objectCache.DeleteObject(null);
        }

        /// <summary>
        /// オブジェクトを削除する（obj内のバケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestDeleteObjectExceptionNoObjBucketName()
        {
            var obj = new NbObject("test");
            obj.Id = "12345";
            obj.BucketName = null;
            _objectCache.DeleteObject(obj);
        }

        /// <summary>
        /// オブジェクトを削除する（obj.Idがnull）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestDeleteObjectExceptionNoObjectId()
        {
            var obj = CreateObject();
            obj.Id = null;
            _objectCache.DeleteObject(obj);
        }

        /**
         * GetTables
         **/

        /// <summary>
        /// sqlite_masterからテーブル名の一覧を取得する（正常）
        /// テーブル一覧を取得できること
        /// </summary>
        [Test]
        public void TestGetTablesNormal()
        {
            var results = _objectCache.GetTables();
            Assert.IsTrue(results.Contains("OBJECT_test1"));
            Assert.AreEqual(1, results.Count);

            _objectCache.CreateCacheTable("test2");
            results = _objectCache.GetTables();
            Assert.IsTrue(results.Contains("OBJECT_test2"));
            Assert.AreEqual(2, results.Count);
        }

        /// <summary>
        /// sqlite_masterからテーブル名の一覧を取得する（テーブルなし）
        /// テーブル一覧を取得できないこと
        /// </summary>
        [Test]
        public void TestGetTablesNormalNoHit()
        {
            _objectCache.DeleteCacheTable("test1");
            Assert.IsEmpty(_objectCache.GetTables());
        }

        /**
         * MongoQueryObjects
         **/

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（正常）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormal()
        {
            addTestDataForQuery();
            var results = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", new NbQuery());
            Assert.AreEqual(2, results.Count());
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ヒットなし）
        /// 取得されないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalNoHit()
        {
            var results = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", new NbQuery());
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（キャッシュにデリートマーク設定あり）
        /// 取得されないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalJsonDeleteMark()
        {
            var obj = CreateObject();
            obj.Deleted = true;
            _objectCache.InsertObject(obj, NbSyncState.Sync);

            var results = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", new NbQuery());
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（クエリにデリートマーク設定あり）
        /// 取得されること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalQueryDeleteMark()
        {
            var obj = CreateObject();
            obj.Deleted = true;
            _objectCache.InsertObject(obj, NbSyncState.Sync);

            var query = new NbQuery().DeleteMark(true);
            var results = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", query);
            Assert.AreEqual(1, results.Count());
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（クエリのConditions設定）
        /// 取得されること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalNoQueryConditions()
        {
            addTestDataForQuery();
            var query = new NbQuery().EqualTo("key", "a");
            query.Conditions = new NbJsonObject { { "key", "a" } };
            var results = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", query);
            Assert.AreEqual(1, results.Count());

            query.EqualTo("key", "b");
            results = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", query);
            Assert.AreEqual(1, results.Count());
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（Evaluateがfalse）
        /// 取得されないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalFailEvaluate()
        {
            addTestDataForQuery();
            var query = new NbQuery().EqualTo("string", "a");
            var results = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", query);
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ソートに設定）
        /// 取得結果がソートされること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalQueryOrder()
        {
            addTestDataForQuery();
            // ASC
            var query = new NbQuery().OrderBy("key");
            var results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("a", results[0]["key"]);
            Assert.AreEqual("b", results[1]["key"]);

            // DESC
            query = new NbQuery().OrderBy("-key");
            results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("b", results[0]["key"]);
            Assert.AreEqual("a", results[1]["key"]);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ソートに設定）（数値）
        /// 取得結果がソートされること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalQueryOrderNumber()
        {
            addTestDataForQueryNum();
            // ASC
            var query = new NbQuery().OrderBy("key");
            var results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(4, results.Count);
            Assert.AreEqual(0, results[0]["key"]);
            Assert.AreEqual(0, results[1]["key"]);
            Assert.AreEqual(1, results[2]["key"]);
            Assert.AreEqual(2, results[3]["key"]);

            // DESC
            query = new NbQuery().OrderBy("-key");
            results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(4, results.Count);
            Assert.AreEqual(2, results[0]["key"]);
            Assert.AreEqual(1, results[1]["key"]);
            Assert.AreEqual(0, results[2]["key"]);
            Assert.AreEqual(0, results[3]["key"]);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ソートに設定）（string、数値以外）
        /// 取得結果がソートされること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalQueryOrderArray()
        {
            addTestDataForQueryarray();
            // ASC
            var query = new NbQuery().OrderBy("key");
            var results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(2, results.Count);

            // DESC
            query = new NbQuery().OrderBy("-key");
            results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(2, results.Count);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ソートに設定）（null）
        /// 取得結果がソートされること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalQueryOrderNull()
        {
            var obj1 = new NbOfflineObject("test1");
            obj1["key"] = null;
            _objectCache.InsertObject(obj1, NbSyncState.Sync);

            var obj2 = new NbOfflineObject("test1");
            obj2["key"] = null;
            _objectCache.InsertObject(obj2, NbSyncState.Sync);

            var obj3 = new NbOfflineObject("test1");
            obj3["key"] = "a";
            _objectCache.InsertObject(obj3, NbSyncState.Sync);

            var obj4 = new NbOfflineObject("test1");
            obj4["key"] = null;
            _objectCache.InsertObject(obj4, NbSyncState.Sync);

            // ASC
            var query = new NbQuery().OrderBy("key");
            var results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(4, results.Count);

            // DESC
            query = new NbQuery().OrderBy("-key");
            results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(4, results.Count);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ヒットなし、ソート設定あり）
        /// 取得されないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalNoHitQueryOrder()
        {
            var query = new NbQuery().OrderBy("key");
            var results = _objectCache.MongoQueryObjects("test1", query);
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（空のソート設定あり）
        /// 取得されること。ソート順は順不同となるため、取得結果の内容を確認する
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalBlankQueryOrder()
        {
            addTestDataForQuery();
            var query = new NbQuery().OrderBy();
            var results = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(2, results.Count);
            foreach (var result in results)
            {
                if (!result["key"].Equals("a") && !result["key"].Equals("b"))
                {
                    Assert.Fail("Error accord!");
                }
            }
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（skip、limitともに設定）
        /// 設定した条件で取得できること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalQuerySkipLimitValue()
        {
            addTestDataForQuery();
            var query = new NbQuery().Skip(1).Limit(1);
            var result = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("b", result[0]["key"]);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（skipのみ設定）
        /// skipされないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalQuerySkipValue()
        {
            addTestDataForQuery();
            var query = new NbQuery().Skip(1);
            var result = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("b", result[0]["key"]);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（limitのみ設定）
        /// limitで指定した値分取得されること
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalNoQueryLimitValue()
        {
            addTestDataForQuery();
            var query = new NbQuery().Limit(1);
            var result = _objectCache.MongoQueryObjects("test1", query).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("a", result[0]["key"]);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ユーザ、obj内のACLともにnull）
        /// 取得されないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalNoAclUser()
        {
            var obj = CreateObject();
            obj.Acl = null;
            _objectCache.InsertObject(obj, NbSyncState.Sync);
            var results = _objectCache.MongoQueryObjects("test1", new NbQuery(), true, null);
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（obj内のACLがnull）
        /// 取得されないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalNoAcl()
        {
            var obj = CreateObject();
            obj.Acl = null;
            _objectCache.InsertObject(obj, NbSyncState.Sync);
            var user = new NbUser();
            user.UserId = "12345";
            var results = _objectCache.MongoQueryObjects("test1", new NbQuery(), true, user);
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（ユーザIDがnull）
        /// 取得されないこと
        /// </summary>
        [Test]
        public void TestMongoQueryObjectsNormalNoUserID()
        {
            var obj = CreateObject();
            _objectCache.InsertObject(obj, NbSyncState.Sync);
            var user = new NbUser();
            var results = _objectCache.MongoQueryObjects("test1", new NbQuery(), true, user);
            Assert.IsEmpty(results);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestMongoQueryObjectsExceptionNoBucketName()
        {
            _objectCache.MongoQueryObjects(null, new NbQuery());
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)（クエリがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestMongoQueryObjectsExceptionNoQuery()
        {
            _objectCache.MongoQueryObjects("test", null);
        }

        /**
         * QueryDirtyObjectIds
         **/

        /// <summary>
        /// キャッシュから Dirty なオブジェクトのId一覧を読み込む（正常）
        /// オブジェクトのId一覧を読み込めること
        /// </summary>
        [Test]
        public void TestQueryDirtyObjectIdsNormal()
        {
            addTestDataForQuery();
            var result = _objectCache.MongoQueryObjects<NbOfflineObject>("test1", new NbQuery());
            string expectedId = null;
            foreach (var obj in result)
            {
                if ("b".Equals(obj.Get<string>("key")))
                {
                    expectedId = obj.Id;
                }
            }

            var results = _objectCache.QueryDirtyObjectIds("test1");

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(expectedId, results[0]);
        }

        // <summary>
        /// キャッシュから Dirty なオブジェクトのId一覧を読み込む（Dirtyなオブジェクトなし）
        /// 空のリストを返却すること
        /// </summary>
        [Test]
        public void TestQueryDirtyObjectIdsNormalNoHit()
        {
            var obj = CreateObject();
            _objectCache.InsertObject(obj, NbSyncState.Sync);

            var results = _objectCache.QueryDirtyObjectIds("test1");

            Assert.IsEmpty(results);
        }

        /// <summary>
        /// キャッシュから Dirty なオブジェクトのId一覧を読み込む（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestQueryDirtyObjectIdsExceptionNoBucketName()
        {
            _objectCache.QueryDirtyObjectIds(null);
        }

        /**
         * QueryObjectsWithIds
         **/

        /// <summary>
        /// 指定されたObjectIdのオブジェクトを取得する（正常）
        /// オブジェクトを取得できること
        /// </summary>
        [Test]
        public void TestQueryObjectsWithIdsNormal()
        {
            CreateAndInsertObjects("9876");
            CreateAndInsertObjects("5432");
            IList<string> objectIds = new List<string>();
            objectIds.Add("9876");
            objectIds.Add("5432");

            var results = _objectCache.QueryObjectsWithIds("test1", objectIds);

            Assert.AreEqual(2, results.Count);
            int count = 0;
            foreach (var obj in results)
            {
                if ("9876".Equals(obj.Id))
                {
                    count++;
                }
                else if ("5432".Equals(obj.Id))
                {
                    count++;
                }
                Assert.AreEqual("CreatedAt", obj.CreatedAt);
                Assert.AreEqual("UpdatedAt", obj.UpdatedAt);
                Assert.AreEqual("ETAG", obj.Etag);
                Assert.AreEqual("value", obj["key"]);
            }
            Assert.AreEqual(2, count);
        }

        /// <summary>
        /// 指定されたObjectIdのオブジェクトを取得する（objectIdsが一つ）
        /// オブジェクトを取得できること
        /// </summary>
        [Test]
        public void TestQueryObjectsWithIdsNormalOneObjectId()
        {
            CreateAndInsertObjects("9876");
            CreateAndInsertObjects("5432");
            IList<string> objectIds = new List<string>();
            objectIds.Add("9876");

            var results = _objectCache.QueryObjectsWithIds("test1", objectIds);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("9876", results[0].Id);
            Assert.AreEqual("CreatedAt", results[0].CreatedAt);
            Assert.AreEqual("UpdatedAt", results[0].UpdatedAt);
            Assert.AreEqual("ETAG", results[0].Etag);
            Assert.AreEqual("value", results[0]["key"]);
        }

        // <summary>
        /// 指定されたObjectIdのオブジェクトを取得する（該当するオブジェクトなし）
        /// 空のリストを返却すること
        /// </summary>
        [Test]
        public void TestQueryObjectsWithIdsNormalNoHit()
        {
            CreateAndInsertObjects("9876");
            IList<string> objectIds = new List<string>();
            objectIds.Add("5432");

            var results = _objectCache.QueryObjectsWithIds("test1", objectIds);

            Assert.IsEmpty(results);
        }

        // <summary>
        /// 指定されたObjectIdのオブジェクトを取得する（ObjectIdの一覧がnull）
        /// 空のリストを返却すること
        /// </summary>
        [Test]
        public void TestQueryObjectsWithIdsNormalNoObjectIds()
        {
            CreateAndInsertObjects("9876");

            var results = _objectCache.QueryObjectsWithIds("test1", null);

            Assert.IsEmpty(results);
        }

        // <summary>
        /// 指定されたObjectIdのオブジェクトを取得する（ObjectIdの一覧が空）
        /// 空のリストを返却すること
        /// </summary>
        [Test]
        public void TestQueryObjectsWithIdsNormalEmptyObjectIds()
        {
            CreateAndInsertObjects("9876");
            IList<string> objectIds = new List<string>();

            var results = _objectCache.QueryObjectsWithIds("test1", objectIds);

            Assert.IsEmpty(results);
        }

        /// <summary>
        /// 指定されたObjectIdのオブジェクトを取得する（バケット名がnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestQueryObjectsWithIdsExceptionNoBucketName()
        {
            IList<string> objectIds = new List<string>();
            objectIds.Add("9876");
            objectIds.Add("5432");

            _objectCache.QueryObjectsWithIds(null, objectIds);
        }

    }
}
