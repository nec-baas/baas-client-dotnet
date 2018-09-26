using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula.Internal.Database
{
    internal partial class NbObjectCache
    {
        private readonly NbMongoQueryEvaluator _evaluator = new NbMongoQueryEvaluator();

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="query">クエリ</param>
        /// <param name="checkAcl">ACLチェックを行う</param>
        /// <param name="user">アクセスユーザ</param>
        /// <returns>クエリ結果</returns>
        /// <exception cref="ArgumentNullException">バケット名、クエリがnull</exception>
        public virtual IEnumerable<NbOfflineObject> MongoQueryObjects(string bucketName, NbQuery query, bool checkAcl = false, NbUser user = null)
        {
            return MongoQueryObjects<NbOfflineObject>(bucketName, query, checkAcl, user);
        }

        /// <summary>
        /// オブジェクトキャッシュに対するクエリ(型パラメータ付き)
        /// </summary>
        /// <typeparam name="T">型パラメータ</typeparam>
        /// <param name="bucketName">バケット名</param>
        /// <param name="query">クエリ</param>
        /// <param name="checkAcl">ACLチェックを行う</param>
        /// <param name="user">アクセスユーザ</param>
        /// <returns>クエリ結果</returns>
        /// <exception cref="ArgumentNullException">バケット名、クエリがnull</exception>
        public virtual IEnumerable<T> MongoQueryObjects<T>(string bucketName, NbQuery query, bool checkAcl = false, NbUser user = null) where T : NbOfflineObject, new()
        {
            NbUtil.NotNullWithArgument(query, "query");
            var resultsList = new List<T>();

            // TODO: 全件クエリ(インデックス未対応)
            var reader = Database.SelectForReader(TableName(bucketName), new[] { "objectId", "state", "json" });

            while (reader.Read())
            {
                var objectId = reader.GetString(0);
                var state = (NbSyncState)reader.GetInt32(1);
                var jsonString = reader.GetString(2);

                var json = NbJsonParser.Parse(jsonString);

                // deleteMark 対応
                if (!query.DeleteMarkValue && json.Opt(Field.Deleted, false))
                {
                    continue;
                }

                // クエリ照合
                if (query.Conditions == null || _evaluator.Evaluate(json, query.Conditions))
                {
                    var obj = new T();
                    obj.Init(bucketName, _service);
                    obj.FromJson(json);
                    obj.SyncState = state;

                    // ACL チェック
                    if (!checkAcl || NbUser.IsAclAccessibleForRead(user, obj.Acl))
                    {
                        resultsList.Add(obj);
                    }
                }
            }

            // sort
            if (resultsList.Count > 0 && query.Order != null)
            {
                resultsList.Sort(new ResultComparer(query.Order));
            }

            // List化不要とするため、IEnumerableにアップキャスト
            IEnumerable<T> results = resultsList;
            // skip, limit
            if (query.SkipValue > 0)
            {
                results = results.Skip(query.SkipValue);
            }
            // 負の値の場合は無制限とみなす
            if (query.LimitValue >= 0)
            {
                results = results.Take(query.LimitValue);
            }
            return results;
        }

        /// <summary>
        /// NbObjectのコンパレータ。OrderBy を指定して比較する。
        /// </summary>
        private class ResultComparer : IComparer<NbObject>
        {
            private readonly IEnumerable<string> _sortOrder;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="sortOrder">OrderBy</param>
            public ResultComparer(IEnumerable<string> sortOrder)
            {
                _sortOrder = sortOrder;
            }

            public int Compare(NbObject x, NbObject y)
            {
                foreach (var k in _sortOrder)
                {
                    var ascend = true;
                    var key = k;
                    if (k.StartsWith("-"))
                    {
                        key = k.Remove(0, 1);
                        ascend = false;
                    }
                    var xdata = x.ToJson().Opt<object>(key, null);
                    var ydata = y.ToJson().Opt<object>(key, null);

                    var result = CompareObject(xdata, ydata);
                    if (!ascend) result = -result;

                    if (result != 0)
                    {
                        return result;
                    }
                }
                return 0;
            }

            private int CompareObject(object x, object y)
            {
                if (x == null && y == null) return 0;

                // null は無限小扱い
                if (x == null) return -1;
                if (y == null) return 1;

                if (x is string && y is string)
                {
                    return string.Compare(((string)x), (string)y, StringComparison.Ordinal);
                }
                if (NbTypeConverter.IsNumeric(x) && NbTypeConverter.IsNumeric(y))
                {
                    double d = NbTypeConverter.ConvertValue<double>(x) - NbTypeConverter.ConvertValue<double>(y);
                    if (d < 0) return -1;
                    if (d > 0) return 1;
                    return 0;
                }

                // imcompatible type
                return 0;
            }
        }
    }
}
