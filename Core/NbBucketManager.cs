using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nec.Nebula.Internal;

namespace Nec.Nebula
{
    /// <summary>
    /// バケットマネージャ
    /// (非公開API)
    /// </summary>
    internal class NbBucketManager
    {
        /// <summary>
        /// バケットタイプ
        /// </summary>
        public enum BucketType
        {
            /// <summary>
            /// オブジェクトバケット
            /// </summary>
            Object,

            /// <summary>
            /// ファイルバケット
            /// </summary>
            File
        };

        private readonly NbService Service;
        private readonly BucketType Type;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bucketType">バケット種別</param>
        /// <param name="service">サービス</param>
        public NbBucketManager(BucketType bucketType, NbService service = null)
        {
            Service = service ?? NbService.Singleton;
            Type = bucketType;
        }

        /// <summary>
        /// バケット作成
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="description">バケットの説明文</param>
        /// <param name="acl">ACL</param>
        /// <param name="contentAcl">ContentACL</param>
        /// <returns>JSON応答</returns>
        public async Task<NbJsonObject> CreateBucketAsync(string bucketName, string description,
            NbAcl acl, NbContentAcl contentAcl)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");
            var req = CreateRequest(bucketName, HttpMethod.Put);

            var body = new NbJsonObject();
            body["ACL"] = acl.ToJson();
            body["contentACL"] = contentAcl.ToJson();
            body["description"] = description;
            req.SetJsonBody(body);

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            return json;
        }

        /// <summary>
        /// バケット削除
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        public async Task DeleteBucketAsync(string bucketName)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");
            var req = CreateRequest(bucketName, HttpMethod.Delete);
            await Service.RestExecutor.ExecuteRequest(req);
        }

        /// <summary>
        /// バケット情報の取得
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns></returns>
        public async Task<NbJsonObject> GetBucketAsync(string bucketName)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");
            var req = CreateRequest(bucketName, HttpMethod.Get);
            return await Service.RestExecutor.ExecuteRequestForJson(req);
        }

        private NbRestRequest CreateRequest(string bucketName, HttpMethod method)
        {
            var req = Service.RestExecutor.CreateRequest("/buckets/{type}/{name}", method);
            req.SetUrlSegment("type", Type == BucketType.Object ? "object" : "file");
            req.SetUrlSegment("name", bucketName);
            return req;
        }
    }
}
