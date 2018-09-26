using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// ファイルバケット
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbFileBucket
    {
        /// <summary>
        /// ダウンロードファイルを格納するクラス
        /// </summary>
        public class DownloadData
        {
            /// <summary>
            /// ファイル名
            /// </summary>
            public string Filename { get; set; }

            /// <summary>
            /// Content-Type
            /// </summary>
            public string ContentType { get; set; }

            /// <summary>
            /// Content-Length
            /// </summary>
            public long ContentLength { get; set; }

            /// <summary>
            /// X-Content-Length
            /// </summary>
            public long XContentLength { get; set; }

            /// <summary>
            /// バイナリデータ
            /// </summary>
            public byte[] RawBytes { get; set; }
        }

        ///// <summary>
        ///// 削除モード。true にすると論理削除。
        ///// </summary>
        // public bool SoftDelete { get; set; }

        /// <summary>
        /// バケット名
        /// </summary>
        public string BucketName { get; internal set; }

        /// <summary>
        /// NbService
        /// </summary>
        internal NbService Service { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="service">NbService</param>
        /// <exception cref="ArgumentNullException">バケット名が未設定</exception>
        public NbFileBucket(string bucketName, NbService service = null)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");
            Service = service ?? NbService.Singleton;

            BucketName = bucketName;
        }

        /// <summary>
        /// ファイル一覧の取得
        /// </summary>
        /// <param name="published">公開ファイルのみの取得範囲限定 (オプション)</param>
        /// <returns>ファイルメタデータの一覧</returns>
        public async Task<IEnumerable<NbFileMetadata>> GetFilesAsync(bool published = false)
        {
            var req = Service.RestExecutor.CreateRequest("/files/{bucket}", HttpMethod.Get);
            req.SetUrlSegment("bucket", BucketName);

            if (published)
            {
                req.SetQueryParameter("published", "1");
            }

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            var results = json.GetArray(Field.Results);
            return from result in results select new NbFileMetadata(BucketName, result as NbJsonObject);
        }

        /// <summary>
        /// 新規ファイルアップロード
        /// </summary>
        /// <param name="dataBytes">ファイルデータ</param>
        /// <param name="fileName">ファイル名</param>
        /// <param name="contentType">Content-Type</param>
        /// <param name="acl">ACL (オプション)</param>
        /// <param name="cacheDisabled">キャッシュ禁止フラグ</param>
        /// <returns>ファイルメタデータ</returns>
        /// <exception cref="ArgumentNullException">ファイルデータ、ファイル名、Content-Typeが未設定</exception>
        public async Task<NbFileMetadata> UploadNewFileAsync(byte[] dataBytes, string fileName, string contentType, NbAcl acl = null, bool cacheDisabled = false)
        {
            return await UploadFileAsync(HttpMethod.Post, dataBytes, fileName, contentType, acl, cacheDisabled, null, null);
        }

        /// <summary>
        /// 更新ファイルアップロード
        /// </summary>
        /// <param name="dataBytes">ファイルデータ</param>
        /// <param name="metadata">メタデータ</param>
        /// <returns>更新後のファイルメタデータ</returns>
        /// <exception cref="ArgumentNullException">ファイルデータ、メタデータ、ファイル名、Content-Typeが未設定</exception>
        public async Task<NbFileMetadata> UploadUpdateFileAsync(byte[] dataBytes, NbFileMetadata metadata)
        {
            NbUtil.NotNullWithArgument(metadata, "metadata");

            return await UploadFileAsync(HttpMethod.Put, dataBytes, metadata.Filename, metadata.ContentType,
                null, false, metadata.MetaEtag, metadata.FileEtag);
        }

        /// <summary>
        /// 更新ファイルアップロード
        /// </summary>
        /// <param name="dataBytes">ファイルデータ</param>
        /// <param name="fileName">ファイル名</param>
        /// <param name="contentType">Content-Type</param>
        /// <param name="metaEtag">meta ETag (オプション)</param>
        /// <param name="fileEtag">file ETag (オプション)</param>
        /// <returns>更新後のファイルメタデータ</returns>
        /// <exception cref="ArgumentNullException">ファイルデータ、ファイル名、Content-Typeが未設定</exception>
        public async Task<NbFileMetadata> UploadUpdateFileAsync(byte[] dataBytes, string fileName, string contentType,
            string metaEtag = null, string fileEtag = null)
        {
            return await UploadFileAsync(HttpMethod.Put, dataBytes, fileName, contentType, null, false, metaEtag, fileEtag);
        }

        private async Task<NbFileMetadata> UploadFileAsync(HttpMethod method, byte[] dataBytes, string fileName, string contentType, NbAcl acl, bool cacheDisabled, string metaEtag, string fileEtag)
        {
            NbUtil.NotNullWithArgument(fileName, "fileName");
            NbUtil.NotNullWithArgument(dataBytes, "dataBytes");

            if (method.Equals(HttpMethod.Post))
            {
                NbUtil.NotNullWithArgument(contentType, "contentType");
            }

            var req = Service.RestExecutor.CreateRequest("/files/{bucket}/{filename}", method);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("filename", fileName);

            // TODO: RestSharp の AddFile は multipart でしかバイナリデータを送信できない。
            req.SetRequestBody(contentType, dataBytes);

            if (acl != null)
            {
                req.SetHeader(Header.XAcl, acl.ToJson().ToString());
            }
            if (cacheDisabled)
            {
                req.SetQueryParameter("cacheDisabled", "true");
            }
            if (metaEtag != null)
            {
                req.SetQueryParameter("metaETag", metaEtag);
            }
            if (fileEtag != null)
            {
                req.SetQueryParameter("fileETag", fileEtag);
            }

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            return new NbFileMetadata(BucketName, json);
        }

        /// <summary>
        /// ファイルのダウンロード
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns>ダウンロードデータ</returns>
        /// <exception cref="ArgumentNullException">ファイル名が未設定</exception>
        /// <exception cref="NbException">ダウンロードファイルサイズが異常</exception>
        public async Task<DownloadData> DownloadFileAsync(string fileName)
        {
            NbUtil.NotNullWithArgument(fileName, "fileName");

            var req = Service.RestExecutor.CreateRequest("/files/{bucket}/{filename}", HttpMethod.Get);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("filename", fileName);

            var result = await Service.RestExecutor.ExecuteRequest(req);

            var xclHeader = result.GetHeader(Header.XContentLength);

            var data = new DownloadData
            {
                Filename = fileName,
                RawBytes = result.RawBytes,
                ContentType = result.ContentType,
                ContentLength = result.ContentLength,
                XContentLength = xclHeader == null ? -1 : long.Parse(xclHeader)
            };

            if (data.RawBytes.Length != data.XContentLength)
            {
                throw new NbException(NbStatusCode.FailedToDownload, "Failed to download. (X-Content-Length does not match)");
            }

            return data;
        }

        /// <summary>
        /// ファイルの削除
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns>JSON応答</returns>
        /// <exception cref="ArgumentNullException">ファイル名が未設定</exception>
        public async Task<NbJsonObject> DeleteFileAsync(string fileName)
        {
            NbUtil.NotNullWithArgument(fileName, "fileName");

            var req = Service.RestExecutor.CreateRequest(("/files/{bucket}/{filename}"), HttpMethod.Delete);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("filename", fileName);

            // if (SoftDelete) req.SetQueryParameter("deleteMark", "1");

            return await Service.RestExecutor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// ファイルを削除する
        /// </summary>
        /// <param name="meta">メタデータ</param>
        /// <returns>JSON応答</returns>
        /// <exception cref="ArgumentNullException">メタデータ、ファイル名が未設定</exception>
        public async Task<NbJsonObject> DeleteFileAsync(NbFileMetadata meta)
        {
            NbUtil.NotNullWithArgument(meta, "meta");
            NbUtil.NotNullWithArgument(meta.Filename, "meta.Filename");

            var req = Service.RestExecutor.CreateRequest(("/files/{bucket}/{filename}"), HttpMethod.Delete);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("filename", meta.Filename);

            if (meta.MetaEtag != null)
            {
                req.SetQueryParameter("metaETag", meta.MetaEtag);
            }
            if (meta.FileEtag != null)
            {
                req.SetQueryParameter("fileETag", meta.FileEtag);
            }

            // if (SoftDelete) req.SetQueryParameter("deleteMark", "1");

            return await Service.RestExecutor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// メタデータの取得
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns>ファイルメタデータ</returns>
        /// <exception cref="ArgumentNullException">ファイル名が未設定</exception>
        public async Task<NbFileMetadata> GetFileMetadataAsync(string fileName)
        {
            NbUtil.NotNullWithArgument(fileName, "fileName");

            var req = Service.RestExecutor.CreateRequest("/files/{bucket}/{filename}/meta", HttpMethod.Get);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("filename", fileName);

            // if (SoftDelete) req.SetQueryParameter(QueryParam.DeleteMark, "1");

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            return new NbFileMetadata(BucketName, json);
        }

        /// <summary>
        /// メタデータの更新
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <param name="metadata">ファイルメタデータ</param>
        /// <returns>更新後のファイルメタデータ</returns>
        /// <exception cref="ArgumentNullException">ファイル名、メタデータが未設定</exception>

        public async Task<NbFileMetadata> UpdateFileMetadataAsync(string fileName, NbFileMetadata metadata)
        {
            NbUtil.NotNullWithArgument(fileName, "fileName");
            NbUtil.NotNullWithArgument(metadata, "metadata");

            var req = Service.RestExecutor.CreateRequest("/files/{bucket}/{filename}/meta", HttpMethod.Put);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("filename", fileName);

            if (metadata.MetaEtag != null)
            {
                req.SetQueryParameter(Field.MetaEtag, metadata.MetaEtag);
            }

            var json = metadata.ToUpdateJson();
            req.SetJsonBody(json);

            var rjson = await Service.RestExecutor.ExecuteRequestForJson(req);
            return new NbFileMetadata(BucketName, rjson);
        }

        /// <summary>
        /// ファイルを公開する
        /// </summary>
        /// <param name="filename">ファイル名</param>
        /// <param name="metaEtag">Meta ETag (オプション)</param>
        /// <returns>更新後のファイルメタデータ</returns>
        /// <exception cref="ArgumentNullException">ファイル名が未設定</exception>
        public async Task<NbFileMetadata> PublishFileAsync(string filename, string metaEtag = null)
        {
            return await PublishFileAsync(filename, metaEtag, true);
        }

        /// <summary>
        /// ファイル公開を解除する
        /// </summary>
        /// <param name="filename">ファイル名</param>
        /// <param name="metaEtag">Meta ETag (オプション)</param>
        /// <returns>更新後のファイルメタデータ</returns>
        /// <exception cref="ArgumentNullException">ファイル名が未設定</exception>
        public async Task<NbFileMetadata> UnpublishFileAsync(string filename, string metaEtag = null)
        {
            return await PublishFileAsync(filename, metaEtag, false);
        }

        private async Task<NbFileMetadata> PublishFileAsync(string fileName, string metaEtag, bool isPublish)
        {
            NbUtil.NotNullWithArgument(fileName, "fileName");

            var req = Service.RestExecutor.CreateRequest("/files/{bucket}/{filename}/publish", isPublish ? HttpMethod.Put : HttpMethod.Delete);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("filename", fileName);

            if (metaEtag != null)
            {
                req.SetQueryParameter(Field.MetaEtag, metaEtag);
            }

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            return new NbFileMetadata(BucketName, json);
        }
    }
}
