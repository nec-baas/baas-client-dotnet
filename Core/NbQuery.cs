using Nec.Nebula.Internal;
using System;
using System.Linq;

namespace Nec.Nebula
{
    /// <summary>
    /// オブジェクトクエリ。
    /// MongoDB のクエリ演算子と機能的にほぼ等価。
    /// </summary>
    /// <example>
    /// 例1:
    /// <code>
    /// var query = new NbQuery().EqualTo("key1", "xyz").GreaterThan("key2", 100).Limit(100).Skip(200);
    /// </code>
    /// 例2:OR条件
    /// <code>
    /// var query = NbQuery.Or(
    ///     new NbQuery().EqualTo("key1", "xyz"),
    ///     new NbQuery().In("key2", "A", "B", "C"));
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>本クラスのインスタンスはスレッドセーフではない。</para>
    /// <para>オフラインのクエリではプリミティブ型の配列は未サポート。</para>
    /// </remarks> 
    /// 
    public class NbQuery
    {
        /// <summary>
        /// 検索条件のJSONオブジェクト表記(MongoDBクエリ表記)。
        /// </summary>
        /// <exception cref="ArgumentNullException">検索条件がnull</exception>
        public NbJsonObject Conditions
        {
            get
            {
                return _conditions;
            }
            set
            {
                NbUtil.NotNullWithArgument(value, "Conditions");
                _conditions = value;
            }
        }

        /// <summary>
        /// 検索条件のJSONObject
        /// </summary>
        private NbJsonObject _conditions;

        /// <summary>
        /// プロジェクション。検索するトップレベルキーの配列。
        /// </summary>
        public string[] ProjectionValue { get; internal set; }

        /// <summary>
        /// ソート条件。ソートキーの配列で、先に指定した条件が優先される。
        /// デフォルトは昇順。逆順にする場合はキー名の先頭に "-" を付加。
        /// </summary>
        public string[] Order { get; internal set; }

        /// <summary>
        /// 検索上限数。-1 の場合は上限なし。
        /// </summary>
        public int LimitValue { get; internal set; }

        /// <summary>
        /// 検索スキップ数。0 ないし負の値の場合はスキップしない。
        /// </summary>
        public int SkipValue { get; internal set; }

        /// <summary>
        /// 削除マークオブジェクトを取得する場合は true
        /// </summary>
        public bool DeleteMarkValue { get; internal set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NbQuery()
        {
            Conditions = new NbJsonObject();
            LimitValue = -1;
            SkipValue = -1;
            DeleteMarkValue = false;
        }

        /// <summary>
        /// プロジェクションを設定する。<br/>
        /// 取得したいフィールド名を列挙する。<br/>
        /// フィールドを抑制したい場合は、フィールド名の先頭に "-" を付与する。<br/>
        /// 列挙・抑制を混在させることはできない。例外として、_id のみを抑制することは可能。<br/>
        /// 
        /// <code>
        /// // 例1) name のみを含める場合
        /// query.Projection("name");
        /// 
        /// // 例2) address のみを除外する場合
        /// query.Projection("-address");
        /// 
        /// // 例3) name を含め、_id を除外する場合
        /// query.Projection("name", "-_id");
        /// </code>
        /// </summary>
        /// <param name="projection">プロジェクション</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">プロジェクションがnull</exception>
        /// <exception cref="ArgumentException">プロジェクションにnullの要素が含まれている</exception>
        public NbQuery Projection(params string[] projection)
        {
            NbUtil.NotNullWithArgument(projection, "projection");
            // 可変長引数にnullが含まれている場合、ArgumentExceptionとする
            NbUtil.NotContainsNullWithArgumentException(projection);

            ProjectionValue = projection;
            return this;
        }

        /// <summary>
        /// Projection の JSON 形式を返却する。
        /// </summary>
        /// <returns>JSONObject</returns>
        internal NbJsonObject ProjectionJson()
        {
            if (ProjectionValue == null || ProjectionValue.Length == 0)
            {
                return null;
            }

            var json = new NbJsonObject();
            foreach (var p in ProjectionValue)
            {
                if (p.StartsWith("-"))
                {
                    json[p.Substring(1)] = 0;
                }
                else
                {
                    json[p] = 1;
                }
            }
            return json;
        }

        /// <summary>
        /// ソート条件を設定する。ソートキーを指定する。
        /// デフォルトは昇順。逆順にする場合はキー名の先頭に "-" を付加。
        /// </summary>
        /// <example>
        /// key1昇順、key2降順でソートする場合:
        /// <code>
        /// query.OrderBy("key1", "-key2");
        /// </code>
        /// </example>
        /// <param name="order">ソートキー</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">ソートキーがnull</exception>
        /// <exception cref="ArgumentException">ソートキーにnullの要素が含まれている</exception>
        /// <remarks>
        /// オフラインデータのソートを行う場合、数値のみ、もしくは文字列のみの単一種別のデータを格納すること。<br/>
        /// nullの混在は許容する。
        /// </remarks>
        public NbQuery OrderBy(params string[] order)
        {
            NbUtil.NotNullWithArgument(order, "order");
            // 可変長引数にnullが含まれている場合、ArgumentExceptionとする
            NbUtil.NotContainsNullWithArgumentException(order);

            Order = order;
            return this;
        }

        /// <summary>
        /// Limit値を設定する
        /// </summary>
        /// <param name="limit">検索上限数</param>
        /// <returns>this</returns>
        public NbQuery Limit(int limit)
        {
            LimitValue = limit;
            return this;
        }

        /// <summary>
        /// Skip値を設定する
        /// </summary>
        /// <param name="skip">検索スキップ数</param>
        /// <returns>this</returns>
        public NbQuery Skip(int skip)
        {
            SkipValue = skip;
            return this;
        }

        /// <summary>
        /// 論理削除されたオブジェクトを検索対象とする
        /// </summary>
        /// <param name="deleteMark">論理削除されたオブジェクトを取得する場合、true</param>
        /// <returns>this</returns>
        public NbQuery DeleteMark(bool deleteMark)
        {
            DeleteMarkValue = deleteMark;
            return this;
        }

        /// <summary>
        /// JSON文字列に変換する。
        /// </summary>
        /// <returns>JSON文字列</returns>
        public override string ToString()
        {
            var jsonObject = new NbJsonObject();
            // where
            if (Conditions != null)
            {
                jsonObject[QueryParam.Where] = Conditions;
            }
            // projection
            if (ProjectionValue != null)
            {
                var projectionArray = new NbJsonArray(ProjectionValue);
                jsonObject[QueryParam.Projection] = projectionArray;
            }
            // order
            if (Order != null)
            {
                var orderArray = new NbJsonArray(Order);
                jsonObject[QueryParam.Order] = orderArray;
            }
            // limit
            jsonObject[QueryParam.Limit] = LimitValue;
            // skip
            jsonObject[QueryParam.Skip] = SkipValue;
            // deleteMark
            jsonObject[QueryParam.DeleteMark] = DeleteMarkValue;

            return jsonObject.ToString();
        }

        /// <summary>
        /// JSON stringを反映したクエリを生成する
        /// </summary>
        /// <param name="jsonString">JSON文字列</param>
        /// <returns>クエリ</returns>
        internal static NbQuery FromJSONString(string jsonString)
        {
            var jsonObject = NbJsonObject.Parse(jsonString);
            var query = new NbQuery();

            // where
            var where = jsonObject.Opt<NbJsonObject>(QueryParam.Where, null);
            if (where != null)
            {
                query.Conditions = where;
            }
            // projection
            var projectionArray = jsonObject.Opt<NbJsonArray>(QueryParam.Projection, null);
            if (projectionArray != null)
            {
                var rawArray = projectionArray.Select(o => o.ToString()).ToArray();
                query.Projection(rawArray);
            }
            // order
            var orderArray = jsonObject.Opt<NbJsonArray>(QueryParam.Order, null);
            if (orderArray != null)
            {
                var rawArray = orderArray.Select(o => o.ToString()).ToArray();
                query.OrderBy(rawArray);
            }

            // limit
            // int型なので、keyが無い場合はdefault値を設定
            var limit = jsonObject.Opt<int>(QueryParam.Limit, -1);
            query.Limit(limit);

            // skip
            // int型なので、keyが無い場合はdefault値を設定
            var skip = jsonObject.Opt<int>(QueryParam.Skip, -1);
            query.Skip(skip);

            // deleteMark
            // bool型なので、keyが無い場合はdefault値を設定
            var deleteMark = jsonObject.Opt<bool>(QueryParam.DeleteMark, false);
            query.DeleteMark(deleteMark);

            return query;
        }

        /// <summary>
        /// 一致条件を追加する。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery EqualTo(string key, object value)
        {
            NbUtil.NotNullWithArgument(key, "key");
            if (value is NbJsonObject)
            {
                return AddSimpleOp("$eq", key, value);
            }
            Conditions[key] = value;
            return this;
        }

        private NbQuery AddSimpleOp(string op, string key, object value)
        {
            NbUtil.NotNullWithArgument(key, "key");

            var j = new NbJsonObject
            {
                {op, value}
            };
            MergePut(key, j);
            return this;
        }

        private void MergePut(string key, NbJsonObject condition)
        {
            // 同一キーが存在しない場合はそのまま追加
            if (!Conditions.ContainsKey(key))
            {
                Conditions.Add(key, condition);
                return;
            }


            // 存在する場合は、ターゲットの JSON Object に追加
            var targetObject = Conditions.Get<object>(key);
            if (targetObject is NbJsonObject)
            {
                var target = (NbJsonObject)targetObject;
                foreach (var kv in condition)
                {
                    // 同一演算子がある場合は上書き
                    target[kv.Key] = kv.Value;
                }
            }
            else
            {
                // その他の場合は上書き
                Conditions[key] = condition;
            }
        }

        /// <summary>
        /// 不一致条件
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery NotEquals(string key, object value)
        {
            return AddSimpleOp("$ne", key, value);
        }

        /// <summary>
        /// 小なり条件
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery LessThan(string key, object value)
        {
            return AddSimpleOp("$lt", key, value);
        }

        /// <summary>
        /// 小なりまたは等しい
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery LessThanOrEqual(string key, object value)
        {
            return AddSimpleOp("$lte", key, value);
        }

        /// <summary>
        /// 大なり条件
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery GreaterThan(string key, object value)
        {
            return AddSimpleOp("$gt", key, value);
        }

        /// <summary>
        /// 大なりまたは等しい
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery GreaterThanOrEqual(string key, object value)
        {
            return AddSimpleOp("$gte", key, value);
        }

        /// <summary>
        /// args に指定された値のいずれかと一致すること
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="args">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery In(string key, params object[] args)
        {
            return AddSimpleOp("$in", key, args);
        }

        /// <summary>
        /// args に指定された値がすべて合致すること
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="args">値</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery All(string key, params object[] args)
        {
            return AddSimpleOp("$all", key, args);
        }

        /// <summary>
        /// フィールドの存在条件
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery Exists(string key)
        {
            return AddSimpleOp("$exists", key, true);
        }

        /// <summary>
        /// フィールドの非存在条件
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public NbQuery NotExists(string key)
        {
            return AddSimpleOp("$exists", key, false);
        }

        /// <summary>
        /// 正規表現一致条件を追加する。
        /// オプション文字列には以下の文字の組み合わせを指定できる。
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>i</term>
        ///     <description>大文字小文字を区別しない</description>
        ///   </item>
        ///   <item>
        ///     <term>m</term>
        ///     <description>複数行にマッチする</description>
        ///   </item>
        ///   <item>
        ///     <term>x</term>
        ///     <description>拡張正規表現を使用する</description>
        ///   </item>
        ///   <item>
        ///     <term>s</term>
        ///     <description>'.' が改行に一致する</description>
        ///   </item>
        /// </list>
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="regexp">正規表現</param>
        /// <param name="options">オプション</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キー、正規表現がnull</exception>
        public NbQuery Regex(string key, string regexp, string options = null)
        {
            NbUtil.NotNullWithArgument(key, "key");
            NbUtil.NotNullWithArgument(regexp, "regexp");

            var j = new NbJsonObject
            {
                {"$regex", regexp}
            };

            if (options != null)
            {
                j.Add("$options", options);
            }

            MergePut(key, j);
            return this;
        }

        /// <summary>
        /// OR条件を生成する
        /// </summary>
        /// <param name="queries">クエリ</param>
        /// <returns>生成したクエリ</returns>
        /// <exception cref="ArgumentNullException">クエリがnull</exception>
        /// <exception cref="ArgumentException">クエリの配列にnull要素が含まれている</exception>
        public static NbQuery Or(params NbQuery[] queries)
        {
            return ConcatQueries("$or", queries);
        }

        /// <summary>
        /// AND条件を生成する
        /// </summary>
        /// <param name="queries">クエリ</param>
        /// <returns>生成したクエリ</returns>
        /// <exception cref="ArgumentNullException">クエリがnull</exception>
        /// <exception cref="ArgumentException">クエリの配列にnull要素が含まれている</exception>
        public static NbQuery And(params NbQuery[] queries)
        {
            return ConcatQueries("$and", queries);
        }

        private static NbQuery ConcatQueries(string op, NbQuery[] queries)
        {
            NbUtil.NotNullWithArgument(queries, "queries");
            NbUtil.NotContainsNullWithArgumentException(queries);

            var concatClauses = new NbQuery();

            var list = new NbJsonArray();
            list.AddRange(from clause in queries select clause.Conditions);

            concatClauses.Conditions.Add(op, list);
            return concatClauses;
        }

        /// <summary>
        /// 指定したキーの条件を反転(not)する
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="ArgumentException">指定したキーが存在しない</exception>
        public NbQuery Not(string key)
        {
            NbUtil.NotNullWithArgument(key, "key");

            if (!Conditions.ContainsKey(key))
            {
                throw new ArgumentException("No such key: " + key);
            }

            var orgCondition = Conditions.GetJsonObject(key);

            var not = new NbJsonObject
            {
                {"$not", orgCondition}
            };

            Conditions.Remove(key);
            Conditions.Add(key, not);

            return this;
        }
    }
}
