using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// オブジェクトキャッシュデータベース。
    /// <para>
    /// テーブル名は、"OBJECT_" + バケット名。
    /// テーブルのカラムは以下の通り。
    /// 
    /// <list type="bullet">
    ///   <item>objectId : オブジェクトID (プライマリキー)</item>
    ///   <item>json : JSONテキスト</item>
    ///   <item>state : 状態 (NbSyncStateの値)</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// なお、バケット管理は、Entity Framework を使用し、NbManageDbContext クラス側で扱う。
    /// </para>
    /// </summary>
    /// <remarks>
    /// Android SDK 実装のテーブルに存在していた etag, permission, updatedAt などは使用しないため削除。
    /// </remarks>
    internal partial class NbObjectCache
    {
        private readonly NbService _service;
        private const string ObjectTablePrefix = "OBJECT_";

        private NbDatabaseImpl Database
        {
            get { return (NbDatabaseImpl)_service.OfflineService.Database; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="service">Service</param>
        /// <exception cref="ArgumentNullException">Serviceがnull</exception>
        public NbObjectCache(NbService service)
        {
            NbUtil.NotNullWithArgument(service, "service");
            _service = service;
        }

        private string TableName(string bucketName)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");
            return ObjectTablePrefix + bucketName;
        }

        /// <summary>
        /// オブジェクトキャッシュテーブルを生成する。
        /// すでにテーブルが存在する場合はなにもしない。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public void CreateCacheTable(string bucketName)
        {
            // TODO: インデックス追加は未対応

            var sql = "CREATE TABLE IF NOT EXISTS " + TableName(bucketName) +
                      " (objectId TEXT PRIMARY KEY, json TEXT, state INTEGER)";
            Database.ExecSql(sql);

            // objectId にSQLインデックス設定
            //sql = "CREATE INDEX IF NOT EXISTS oid_" + bucketName + " ON " + TableName(bucketName) + "(objectId)";
            //_database.ExecSql(sql);
        }

        /// <summary>
        /// オブジェクトキャッシュテーブルを DROP する。
        /// テーブルが存在しなくても例外にはならない。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public void DeleteCacheTable(string bucketName)
        {
            var sql = "DROP TABLE IF EXISTS " + TableName(bucketName);
            Database.ExecSql(sql);
        }

        /// <summary>
        /// オブジェクトをキャッシュに保存する
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <param name="state">同期状態</param>
        /// <exception cref="ArgumentNullException">オブジェクトがnull</exception>
        public virtual void InsertObject(NbObject obj, NbSyncState state)
        {
            NbUtil.NotNullWithArgument(obj, "obj");
            if (obj.Id == null)
            {
                // クライアント側新規データ挿入の場合。
                // クライアント側でオブジェクトIDを付与する
                obj.Id = MongoObjectIdGenerator.CreateObjectId();
            }

            var tuple = MakeTuple(obj, state);
            Database.Insert(TableName(obj.BucketName), tuple);
        }

        private Dictionary<string, object> MakeTuple(NbObject obj, NbSyncState state)
        {
            var tuple = new Dictionary<string, object>
            {
                {"objectId", obj.Id},
                {"json", obj.ToJson().ToString()},
                {"state", (int) state}
            };
            return tuple;
        }


        /// <summary>
        /// キャッシュからオブジェクトを読み込む
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectId"></param>
        /// <returns>オブジェクト</returns>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public virtual T FindObject<T>(string bucketName, string objectId) where T : NbOfflineObject, new()
        {
            var reader = Database.SelectForReader(TableName(bucketName), new[] { "json", "state" }, "objectId = ?", new object[] { objectId });
            if (!reader.Read())
            {
                return null; // not found
            }

            var json = reader.GetString(0);
            var obj = new T();
            obj.Init(bucketName, _service);
            obj.FromJson(NbJsonParser.Parse(json));

            var state = (NbSyncState)reader.GetInt32(1);
            obj.SyncState = state;
            reader.Dispose();

            return obj;
        }

        /// <summary>
        /// オブジェクトクエリ (SQLレベル)
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="where"></param>
        /// <param name="whereArgs"></param>
        /// <returns>オブジェクトの一覧</returns>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public virtual IList<NbOfflineObject> QueryObjects(string bucketName, string where, object[] whereArgs)
        {
            var reader = Database.SelectForReader(TableName(bucketName), new[] { "json", "state" },
                where, whereArgs);

            var objects = new List<NbOfflineObject>();
            while (reader.Read())
            {
                var json = reader.GetString(0);
                var state = (NbSyncState)reader.GetInt32(1);

                var obj = new NbOfflineObject(bucketName, NbJsonParser.Parse(json), _service)
                {
                    SyncState = state
                };

                objects.Add(obj);
            }
            return objects;
        }

        /// <summary>
        /// キャッシュから Dirty なオブジェクト一覧を読み込む
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns>Dirtyなオブジェクトの一覧</returns>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public virtual IList<NbOfflineObject> QueryDirtyObjects(string bucketName)
        {
            return QueryObjects(bucketName, "state = " + (int)NbSyncState.Dirty, null);
        }

        /// <summary>
        /// キャッシュから Dirty なオブジェクトのIdを抽出して読み込む
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>取得したObjectIdのリスト</returns>
        /// <remarks>Dirtyなオブジェクトが無い場合は、空のリストを返却する。</remarks>
        public virtual IList<string> QueryDirtyObjectIds(string bucketName)
        {
            var reader = Database.SelectForReader(TableName(bucketName), new[] { "objectId" }, "state = " + (int)NbSyncState.Dirty, null);
            var objectIds = new List<string>();
            while (reader.Read())
            {
                var objectId = reader.GetString(0);
                objectIds.Add(objectId);
            }
            return objectIds;
        }


        /// <summary>
        /// 指定されたObjectIdのオブジェクトを取得する
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="objectIds">ObjectIdの一覧</param>
        /// <returns>取得したオブジェクト一覧</returns>
        public virtual IList<NbOfflineObject> QueryObjectsWithIds(string bucketName, IEnumerable<string> objectIds)
        {
            // ObjectIdが指定されていない場合、空のリストを返却
            if (objectIds == null || objectIds.Count() == 0)
            {
                return new List<NbOfflineObject>();
            }

            // where条件生成
            var whereBuilder = new StringBuilder();
            whereBuilder.Append("objectId IN (");
            // 指定Id数のプレースホルダ―を追加
            foreach (var objId in objectIds)
            {
                whereBuilder.Append(" ?,");
            }
            whereBuilder.Remove(whereBuilder.Length - 1, 1); // 最終のカンマは削除する
            whereBuilder.Append(" ) ");
            var where = whereBuilder.ToString();

            // 指定Idのオブジェクトを取得
            var objects = QueryObjects(bucketName, where, objectIds.ToArray());
            return objects;
        }

        /// <summary>
        /// オブジェクトを更新する
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="state"></param>
        /// <returns>更新した件数</returns>
        /// <exception cref="ArgumentNullException">オブジェクトがnull</exception>
        public virtual int UpdateObject(NbObject obj, NbSyncState state)
        {
            NbUtil.NotNullWithArgument(obj, "obj");
            if (obj.Id == null)
            {
                throw new InvalidOperationException("No id for update");
            }

            var tuple = MakeTuple(obj, state);
            return Database.Update(TableName(obj.BucketName), tuple, "objectId = ?", new object[] { obj.Id });
        }

        /// <summary>
        /// オブジェクトを削除する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>削除した件数</returns>
        /// <exception cref="ArgumentNullException">オブジェクトがnull</exception>
        public virtual int DeleteObject(NbObject obj)
        {
            NbUtil.NotNullWithArgument(obj, "obj");
            if (obj.Id == null)
            {
                throw new InvalidOperationException("No id for Delete");
            }
            return Database.Delete(TableName(obj.BucketName), "objectId = ?", new object[] { obj.Id });
        }

        /// <summary>
        /// sqlite_masterからオブジェクトテーブル名の一覧を取得する
        /// </summary>
        /// <returns>取得したオブジェクトテーブル名の一覧</returns>
        internal List<string> GetTables()
        {
            var list = new List<string>();
            using (var reader = Database.SelectForReader("sqlite_master", null, "type='table'"))
            {
                while (reader.Read())
                {
                    var name = reader["name"].ToString();
                    if (!name.StartsWith(ObjectTablePrefix)) continue;
                    list.Add(name);
                }
            }
            return list;
        }

        /// <summary>
        /// オブジェクトテーブルにキャッシュが存在するか確認する
        /// </summary>
        /// <returns>オブジェクトキャッシュが存在する場合はtrue、存在しない場合はfalse</returns>
        internal bool IsObjectCacheExists()
        {
            var tables = GetTables();
            foreach (var table in tables)
            {
                // 存在確認のため、Limitは1にする
                using (var reader = Database.SelectForReader(table, new[] { "json", "state" }, null, null, 0, 1))
                {
                    if (reader.Read()) return true;
                }
            }
            return false;
        }
    }
}
