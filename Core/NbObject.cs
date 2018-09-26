using Nec.Nebula.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// オンラインオブジェクト
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks> 
    public class NbObject : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// サービス
        /// </summary>
        internal NbService Service { get; set; }

        /// <summary>
        /// バケット名
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// オブジェクトID
        /// </summary>
        public string Id
        {
            get { return _json.Opt<string>(Field.Id, null); }
            internal set { _json[Field.Id] = value; }
        }

        /// <summary>
        /// ACL
        /// </summary>
        public NbAcl Acl { get; set; } // ACL は直接 JSON には埋め込まず、メンバとして保持する。

        /// <summary>
        /// ETag
        /// </summary>
        public string Etag
        {
            get { return _json.Opt<string>(Field.Etag, null); }
            internal set { _json[Field.Etag] = value; }
        }

        /// <summary>
        /// 作成日時
        /// </summary>
        public string CreatedAt
        {
            get { return _json.Opt<string>(Field.CreatedAt, null); }
            internal set { _json[Field.CreatedAt] = value; }
        }

        /// <summary>
        /// 更新日時
        /// </summary>
        public string UpdatedAt
        {
            get { return _json.Opt<string>(Field.UpdatedAt, null); }
            internal set { _json[Field.UpdatedAt] = value; }
        }

        /// <summary>
        /// 削除マーク
        /// </summary>
        public bool Deleted
        {
            get { return _json.Opt(Field.Deleted, false); }
            internal set { _json[Field.Deleted] = value; }
        }

        /// <summary>
        /// インデクサ
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>値。該当キーがない場合は null。</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="ArgumentException">不正文字列を含むキーを検出</exception>
        /// <remarks><see paramref="key"/>には、空の文字、先頭に"$"が含まれる文字列、もしくは"."が含まれる文字列は使用できない。</remarks>
        public object this[string key]
        {
            get
            {
                NbUtil.NotNullWithArgument(key, "key");
                object value;
                return _json.TryGetValue(key, out value) ? value : null;
            }
            set
            {
                Set(key, value);
            }
        }

        /// <summary>
        /// JSONデータ
        /// </summary>
        private NbJsonObject _json;

        /// <summary>
        /// デフォルトコンストラクタ。<br/>
        /// 通常は以下コンストラクタを使用すること。<br/>
        /// <see cref="NbObject(string,NbService)"/><br/>
        /// <see cref="NbObject(string,NbJsonObject,NbService)"/>
        /// </summary>
        public NbObject()
        {
        }

        /// <summary>
        /// コンストラクタ。バケット名から生成。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="service">サービス</param>
        /// <exception cref="ArgumentNullException">バケット名がnull</exception>
        public NbObject(string bucketName, NbService service = null)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");
            Init(bucketName, service);
        }

        /// <summary>
        /// コンストラクタ。JSON Object から生成。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="json">JSON Object</param>
        /// <param name="service">サービス</param>
        /// <exception cref="ArgumentNullException">バケット名、JSONがnull</exception>
        /// <exception cref="ArgumentException">不正文字列を含むキーを検出</exception>
        public NbObject(string bucketName, NbJsonObject json, NbService service = null)
            : this(bucketName, service)
        {
            NbUtil.NotNullWithArgument(json, "json");
            FromJson(json);
        }

        internal virtual void Init(string bucketName, NbService service)
        {
            service = service ?? NbService.Singleton;

            BucketName = bucketName;
            Service = service;

            _json = new NbJsonObject();
        }

        /// <summary>
        /// KeyValuePair の enumerator を返す
        /// </summary>
        /// <returns>enumerator</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _json.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 指定されたキーに対応する値があるか調べる
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>値があれば true</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public bool HasKey(string key)
        {
            NbUtil.NotNullWithArgument(key, "key");
            return _json.ContainsKey(key);
        }


        /// <summary>
        /// 指定されたキーに対応する値を取得する。
        /// キーが存在しない場合は KeyNotFoundException がスローされる。
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="key">キー</param>
        /// <returns>値</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="KeyNotFoundException">対応するキーが存在しない</exception>
        public T Get<T>(string key)
        {
            NbUtil.NotNullWithArgument(key, "key");
            return _json.Get<T>(key);
        }

        /// <summary>
        /// 指定されたキーに対応する値を取得する。
        /// 対応するキーが存在しない場合や型が不整合な場合は false が返る。
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="key">キー</param>
        /// <param name="value">取得した値</param>
        /// <returns>正常取得した場合は true</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public bool TryGet<T>(string key, out T value)
        {
            NbUtil.NotNullWithArgument(key, "key");
            try
            {
                value = _json.Get<T>(key);
                return true;
            }
            catch (Exception)
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// 指定したキーに対応する値を取得する。
        /// 値が存在しない場合はデフォルト値が返却される。
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="key">キー</param>
        /// <param name="defValue">デフォルト値</param>
        /// <returns>値</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        public T Opt<T>(string key, T defValue)
        {
            NbUtil.NotNullWithArgument(key, "key");
            return _json.Opt(key, defValue);
        }


        /// <summary>
        /// 指定されたキーに対応する値を設定する。
        /// 設定が可能な型は、プリミティブ型、string、 IList、IDictionary のいずれか。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="ArgumentException">不正文字列を含むキーを検出</exception>
        /// <remarks>
        /// コレクション初期化子を使うことで NbObject の生成時に一括で値を設定することも可能。
        /// 例:
        /// <code>
        /// var obj = new NbObject("Emp") {
        ///     {"name", "Taro Nichiden"},
        ///     {"age", 32}
        /// };
        /// </code>
        /// <br/>
        /// <see paramref="key"/>には、空の文字、先頭に"$"が含まれる文字列、もしくは"."が含まれる文字列は使用できない。
        /// </remarks>
        public void Set(string key, object value)
        {
            NbUtil.NotNullWithArgument(key, "key");
            // keyの不正文字チェック
            ValidateFieldName(key);
            _json[key] = value;
        }

        /// <summary>
        /// 指定されたキーに対応する値を削除する
        /// </summary>
        /// <param name="key">キー</param>
        /// <exception cref="ArgumentNullException">keyがnull</exception>
        /// <returns>正常に削除された場合はtrue。</returns>
        public bool Remove(string key)
        {
            NbUtil.NotNullWithArgument(key, "key");
            return _json.Remove(key);
        }

        /// <summary>
        /// 指定されたキーに対応する値を設定する。
        /// 設定が可能な型は、プリミティブ型、string、 IList、IDictionary のいずれか。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="ArgumentException">不正文字列を含むキーを検出、または同じキーを持つ要素が既に存在する。</exception>
        /// <remarks><see paramref="key"/>には、空の文字、先頭に"$"が含まれる文字列、もしくは"."が含まれる文字列は使用できない。</remarks>
        public void Add(string key, object value)
        {
            // コレクション初期化子対応のため追加
            NbUtil.NotNullWithArgument(key, "key");
            // keyの不正文字チェック
            ValidateFieldName(key);
            _json.Add(key, value);
        }

        /// <summary>
        /// JSONデータをセットする
        /// </summary>
        /// <param name="json">JSON</param>
        /// <returns>this</returns>
        internal NbObject FromJson(NbJsonObject json)
        {
            _json = new NbJsonObject();

            foreach (var j in json)
            {
                // 使用可能文字列のチェック
                ValidateFieldName(j.Key);
                string key = j.Key;
                object val = j.Value;
                _json[key] = val;
            }

            // ACL 復元
            var aclJson = _json.Opt<NbJsonObject>(Field.Acl, null);
            Acl = aclJson != null ? new NbAcl(aclJson) : null;

            return this;
        }

        /// <summary>
        /// JSON表現に変換
        /// </summary>
        /// <returns>JSON表現に変換したObject</returns>
        public NbJsonObject ToJson()
        {
            // ACL 設定
            if (Acl != null)
            {
                _json[Field.Acl] = Acl.ToJson();
            }
            else
            {
                _json.Remove(Field.Acl);
            }

            NbJsonObject json = new NbJsonObject();
            foreach (var kv in _json)
            {
                json.Add(kv.Key, kv.Value);
            }

            return json;
        }

        private NbRestRequest CreateBucketRequest(HttpMethod method)
        {
            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}", method);
            req.SetUrlSegment("bucket", BucketName);
            return req;
        }

        private NbRestRequest CreateObjectRequest(HttpMethod method)
        {
            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}/{id}", method);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("id", Id);
            return req;
        }

        /// <summary>
        /// オブジェクトをサーバに保存する。
        /// </summary>
        /// <returns>保存されたオブジェクト。</returns>
        /// <exception cref="InvalidOperationException">バケット名がnull</exception>
        public virtual async Task<NbObject> SaveAsync()
        {
            NbUtil.NotNullWithInvalidOperation(BucketName, "No BucketName");

            // 送信用 JSON 生成
            NbJsonObject json = ToJson();

            // リクエスト作成
            NbRestRequest req;
            if (Id == null)
            {
                // 新規
                req = CreateBucketRequest(HttpMethod.Post);
                req.SetJsonBody(json);
            }
            else
            {
                // 更新 (フルアップデート)
                req = CreateObjectRequest(HttpMethod.Put);
                if (Etag != null)
                {
                    req.SetQueryParameter(Field.Etag, Etag);
                }
                var fullUpdate = new NbJsonObject()
                {
                    {Field.FullUpdate, json}
                };

                req.SetJsonBody(fullUpdate);
            }

            var rjson = await Service.RestExecutor.ExecuteRequestForJson(req);
            return new NbObject(BucketName, Service).FromJson(rjson);
        }

        /// <summary>
        /// オブジェクトを部分更新する。
        /// </summary>
        /// <param name="json">部分更新用 JSON</param>
        /// <returns>保存されたオブジェクト。</returns>
        /// <exception cref="ArgumentNullException">JSONがnull</exception>
        /// <exception cref="InvalidOperationException">Id、バケット名がnull</exception>
        public virtual async Task<NbObject> PartUpdateAsync(NbJsonObject json)
        {
            NbUtil.NotNullWithArgument(json, "json");
            NbUtil.NotNullWithInvalidOperation(Id, "No Id");
            NbUtil.NotNullWithInvalidOperation(BucketName, "No BucketName");

            var req = CreateObjectRequest(HttpMethod.Put);
            if (Etag != null)
            {
                req.SetQueryParameter(Field.Etag, Etag);
            }
            req.SetJsonBody(json);

            var rjson = await Service.RestExecutor.ExecuteRequestForJson(req);
            return new NbObject(BucketName, Service).FromJson(rjson);
        }

        /// <summary>
        /// オブジェクトを削除する。
        /// </summary>
        /// <param name="softDelete">論理削除する場合は true (デフォルトは true)</param>
        /// <returns>Task</returns>
        /// <exception cref="InvalidOperationException">Id、バケット名がnull</exception>
        public virtual async Task DeleteAsync(bool softDelete = true)
        {
            NbUtil.NotNullorEmptyWithInvalidOperation(Id, "No Id");
            NbUtil.NotNullWithInvalidOperation(BucketName, "No BucketName");

            var req = CreateObjectRequest(HttpMethod.Delete);
            if (Etag != null)
            {
                req.SetQueryParameter(Field.Etag, Etag);
            }

            // 論理削除
            if (softDelete)
            {
                req.SetQueryParameter(QueryParam.DeleteMark, "1");
            }

            await Service.RestExecutor.ExecuteRequest(req);
        }

        /// <summary>
        /// 禁止文字を含む正規表現パターン
        /// 先頭に"$"が含まれる、もしくは"."が含まれる文字列にマッチ
        /// </summary>
        private static readonly string InvalidFieldNamePattern = @"(^\$.*)|(.*\..*)";
        private static readonly Regex InvalidFieldRegex = new Regex(InvalidFieldNamePattern);

        /// <summary>
        /// フィールドの使用文字チェックを行う
        /// </summary>
        /// <param name="field">フィールド名</param>
        /// <exception cref="ArgumentNullException">フィールドがnull</exception>
        /// <exception cref="ArgumentException">不正文字列を含むフィールドを検出</exception>
        internal static void ValidateFieldName(string field)
        {
            // fail safe: コール元で適切なnullチェックを行う
            NbUtil.NotNullWithArgument(field, "field");

            // 空文字は許容しない
            if (field.Length == 0)
            {
                throw new ArgumentException("Empty key is not allowed");
            }

            // 先頭に"$"が含まれる、もしくは"."が含まれる文字列は許容しない
            var isMatched = InvalidFieldRegex.IsMatch(field);
            if (isMatched)
            {
                throw new ArgumentException(field + " contains invalid string");
            }

        }
    }
}
