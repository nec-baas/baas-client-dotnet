using Nec.Nebula.Internal;
using System;

namespace Nec.Nebula
{
    /// <summary>
    /// バッチリクエスト
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbBatchRequest
    {
        internal const string OpInsert = "insert";

        internal const string OpUpdate = "update";

        internal const string OpDelete = "delete";

        internal const string KeyRequests = "requests";
        internal const string KeyOp = "op";
        internal const string KeyId = "_id";
        internal const string KeyData = "data";
        internal const string KeyEtag = "etag";


        /// <summary>
        /// リクエストの JSON 表現
        /// </summary>
        public NbJsonObject Json { get; private set; }

        /// <summary>
        /// リクエスト配列
        /// </summary>
        public NbJsonArray Requests { get; private set; }

        /// <summary>
        /// リクエストトークン。GUIDで生成。
        /// </summary>
        public string RequestToken { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NbBatchRequest()
        {
            Json = new NbJsonObject();

            Requests = new NbJsonArray();
            Json.Add(KeyRequests, Requests);

            RequestToken = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// INSERT リクエスト追加
        /// </summary>
        /// <param name="obj">Insert対象のオブジェクト</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">オブジェクトがnull</exception>
        /// <exception cref="ArgumentException">オブジェクトにETagが設定されている</exception>
        public NbBatchRequest AddInsertRequest(NbObject obj)
        {
            NbUtil.NotNullWithArgument(obj, "obj");

            if (obj.Etag != null)
            {
                throw new ArgumentException("Can't insert data with ETag");
            }

            var r = new NbJsonObject
            {
                {KeyOp, OpInsert}, 
                {KeyData, obj.ToJson()}
            };
            Requests.Add(r);
            return this;
        }

        /// <summary>
        /// UPDATEリクエスト追加。
        /// 常に更新は Full Update として扱われる。
        /// </summary>
        /// <param name="obj">Update対象のオブジェクト</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">オブジェクト、オブジェクトのIdがnull</exception>
        public NbBatchRequest AddUpdateRequest(NbObject obj)
        {
            NbUtil.NotNullWithArgument(obj, "obj");
            NbUtil.NotNullWithArgument(obj.Id, "Id");

            // 常に full update 扱い
            var fullUpdate = new NbJsonObject()
            {
                {"$full_update", obj.ToJson()}
            };

            var r = new NbJsonObject
            {
                {KeyOp, OpUpdate}, 
                {KeyId, obj.Id},
                {KeyData, fullUpdate}
            };
            if (obj.Etag != null)
            {
                r.Add(KeyEtag, obj.Etag);
            }

            Requests.Add(r);
            return this;
        }

        /// <summary>
        /// DELETEリクエスト追加
        /// </summary>
        /// <param name="obj">Delete対象のオブジェクト</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentNullException">オブジェクト、オブジェクトのIdがnull</exception>
        public NbBatchRequest AddDeleteRequest(NbObject obj)
        {
            NbUtil.NotNullWithArgument(obj, "obj");
            NbUtil.NotNullWithArgument(obj.Id, "Id");

            var r = new NbJsonObject
            {
                {KeyOp, OpDelete}, 
                {KeyId, obj.Id}
            };
            if (obj.Etag != null)
            {
                r.Add(KeyEtag, obj.Etag);
            }
            Requests.Add(r);
            return this;
        }

        /// <summary>
        /// 指定位置のリクエストのオペレーションを返却する
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>insert, update, delete のいずれか</returns>
        /// <remarks>追加したリクエスト数の範囲でindexを指定すること</remarks>
        /// <exception cref="ArgumentOutOfRangeException "><paramref name="index"/>に要素の範囲外の値を指定した</exception>
        public string GetOp(int index)
        {
            var json = (NbJsonObject)Requests[index];
            return (string)json[KeyOp];
        }
    }
}
