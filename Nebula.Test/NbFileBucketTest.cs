using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    internal class NbFileBucketTest
    {
        private NbFileBucket bucket;
        private MockRestExecutor restExecutor;

        private const string appKey = "X-Application-Key";
        private const string appId = "X-Application-Id";

        [SetUp]
        public void Setup()
        {
            TestUtils.Init();

            bucket = new NbFileBucket("test");
            restExecutor = new MockRestExecutor();

            // inject rest executor
            NbService.Singleton.RestExecutor = restExecutor;

        }

        [Test]
        public void TestInit()
        {
            Assert.AreEqual("test", bucket.BucketName);
        }

        /**
        * Constructor
        **/

        /// <summary>
        /// コンストラクタテスト（正常）
        /// インスタンス生成できること
        /// バケット名とサービスは指定したものと同一であること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var bucketName = "TestFileBucket";
            var testBucket = new NbFileBucket(bucketName);

            Assert.AreEqual(bucketName, testBucket.BucketName);
            Assert.NotNull(testBucket.Service);

        }

        /// <summary>
        /// コンストラクタテスト（バケット名Null）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionBucketNameNull()
        {
            new NbFileBucket(null);
        }

        /// <summary>
        /// コンストラクタテスト（複数サービス）
        /// マルチテナント有効時は別サービスを指定できること
        /// </summary>
        [Test]
        public void TestConstructorNormalMultiService()
        {
            var bucketName = "TestFileBucket";

            NbService.EnableMultiTenant(true);

            var TestService = NbService.GetInstance();

            var testBucket = new NbFileBucket(bucketName, TestService);

            Assert.AreEqual(bucketName, testBucket.BucketName);
            Assert.AreEqual(TestService, testBucket.Service);

            NbService.EnableMultiTenant(false);
        }

        /**
         * GetFilesAsync
         **/
        /// <summary>
        /// GetFilesAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestGetFilesAsyncNormal()
        {
            // object
            var num = 5;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGetMetaDataResponseJson(num).ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.GetFilesAsync();
            var files = result.ToArray();

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName));

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);
            Assert.IsFalse(req.QueryParams.ContainsKey("published"));

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(num, files.Count());
            for (int i = 0; i < num; i++)
            {
                Assert.AreEqual("fileName" + i, files[i].Filename);
            }

        }

        /// <summary>
        /// GetFilesAsync（公開ファイル）
        /// リクエストパラメータにpublishedが含まれていること
        /// </summary>
        [Test]
        public async void TestGetFilesAsyncNormalWithPublished()
        {
            // object
            var num = 10;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGetMetaDataResponseJson(num).ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.GetFilesAsync(true);
            var files = result.ToArray();

            // Request Check
            var req = restExecutor.LastRequest;
            Assert.AreEqual("1", req.QueryParams["published"]);

            // Response Check
            Assert.AreEqual(num, files.Count());
            for (int i = 0; i < num; i++)
            {
                Assert.AreEqual("fileName" + i, files[i].Filename);
            }
        }

        /// <summary>
        /// GetFilesAsync（0ファイル）
        /// 取得件数0件であっても異常終了しないこと
        /// </summary>
        [Test]
        public async void TestGetFilesAsyncNormalFileNotExists()
        {
            // object
            var num = 0;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateGetMetaDataResponseJson(num).ToString());
            restExecutor.AddResponse(response);

            var result = await bucket.GetFilesAsync();
            var files = result.ToArray();

            // Requset Check
            var req = restExecutor.LastRequest;
            Assert.IsFalse(req.QueryParams.ContainsKey("published"));

            // Response Check
            Assert.AreEqual(num, files.Count());

        }

        /// <summary>
        /// GetFilesAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestGetFilesAsyncExceptionWithResponseError()
        {
            // object
            var num = 1;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, CreateGetMetaDataResponseJson(num).ToString());
            restExecutor.AddResponse(response);

            IEnumerable<NbFileMetadata> resp = null;

            try
            {
                resp = await bucket.GetFilesAsync(true);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }
        }

        /**
         * UploadNewFileAsync
         **/
        /// <summary>
        /// UploadNewFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestUploadNewFileAsyncNormal()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var acl = new NbAcl();
            var cacheDisabled = true;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadNewFileAsync(fileData, fileName, contentType, acl, cacheDisabled);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Post, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName));

            // Header
            Assert.AreEqual(4, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));
            Assert.AreEqual(acl.ToJson().ToString(), req.Headers["X-ACL"].ToString());

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            Assert.AreEqual("true", req.QueryParams["cacheDisabled"].ToString());
            
            // Request Body
            Assert.AreEqual(fileData, req.Content.ReadAsByteArrayAsync().Result);

            // Response Check
            Assert.AreEqual(fileName, meta.Filename);

        }

        /// <summary>
        /// UploadNewFileAsync（ACLなし）
        /// HttpHeaderにACLが含まれないこと
        /// </summary>
        [Test]
        public async void TestUploadNewFileAsyncNormalWithoutAcl()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var cacheDisabled = true;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadNewFileAsync(fileData, fileName, contentType, null, cacheDisabled);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Header
            Assert.IsFalse(req.Headers.ContainsKey("X-ACL"));

            // Response Check
            Assert.AreEqual(fileName, meta.Filename);

        }

        /// <summary>
        /// UploadNewFileAsync（CacheDisabledなし）
        /// RequestParameterにキャッシュ禁止フラグが含まれないこと
        /// </summary>
        [Test]
        public async void TestUploadNewFileAsyncNormalWithoutCacheDisabled()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var acl = new NbAcl();

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadNewFileAsync(fileData, fileName, contentType, acl);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.IsFalse(req.QueryParams.ContainsKey("cachedDisabled"));

            // Response Check
            Assert.AreEqual(fileName, meta.Filename);

        }

        /// <summary>
        /// UploadNewFileAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadNewFileAsyncExceptionWithoutFileName()
        {
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var contentType = "image/jpeg";
            var acl = new NbAcl();

            var meta = await bucket.UploadNewFileAsync(fileData, null, contentType, acl);

        }

        /// <summary>
        /// UploadNewFileAsync（ファイルデータなし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadNewFileAsyncExceptionWithoutFileData()
        {
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var acl = new NbAcl();

            var meta = await bucket.UploadNewFileAsync(null, fileName, contentType, acl);

        }

        /// <summary>
        /// UploadNewFileAsync（Content-Typeなし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadNewFileAsyncExceptionWithoutContentType()
        {
            var fileName = "TestFile";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var acl = new NbAcl();

            var meta = await bucket.UploadNewFileAsync(fileData, fileName, null, acl);

        }

        /// <summary>
        /// UploadNewFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestUploadNewFileAsyncExceptionWithResponseError()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var acl = new NbAcl();

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            NbFileMetadata meta = null;

            try
            {
                meta = await bucket.UploadNewFileAsync(fileData, fileName, contentType, acl);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);

            }
        }

        /**
         * UploadUpdateFileAsync（個別設定）
         **/
        /// <summary>
        /// UploadUpdateFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileAsyncNormal()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var metaEtag = "metaEtag";
            var fileEtag = "fileEtag";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadUpdateFileAsync(fileData, fileName, contentType, metaEtag, fileEtag);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName));

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("metaEtag", req.QueryParams["metaETag"].ToString());
            Assert.AreEqual("fileEtag", req.QueryParams["fileETag"].ToString());

            // Request Body
            Assert.AreEqual(fileData, req.Content.ReadAsByteArrayAsync().Result);

            // Response Check
            Assert.AreEqual(fileName, meta.Filename);

        }

        /// <summary>
        /// UploadUpdateFileAsync（オプションなし）
        /// リクエストパラメータにContent-Type、metaETag、fileETagが含まれないこと
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileAsyncNormalWithoutOptions()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadUpdateFileAsync(fileData, fileName, null);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Response Check
            Assert.AreEqual(fileName, meta.Filename);

        }


        /// <summary>
        /// UploadUpdateFileAsync（ファイルデータなし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadUpdateFileAsyncExceptionWithoutFileData()
        {

            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var meta = await bucket.UploadUpdateFileAsync(null, fileName, contentType);

        }

        /// <summary>
        /// UploadUpdateFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileAsyncExceptionWithResponseError()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            NbFileMetadata meta = null;

            try
            {
                meta = await bucket.UploadUpdateFileAsync(fileData, fileName, contentType);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);

            }
        }

        /**
         * UploadUpdateFileAsync（メタデータ）
         **/
        /// <summary>
        /// UploadUpdateFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileAsyncWithMetaDataNormal()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var updateMetadata = new NbFileMetadata(bucketName, jsonObject);

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadUpdateFileAsync(fileData, updateMetadata);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName));

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("meta", req.QueryParams["metaETag"].ToString());
            Assert.AreEqual("file", req.QueryParams["fileETag"].ToString());

            // Request Body
            Assert.AreEqual(fileData, req.Content.ReadAsByteArrayAsync().Result);

            // Response Check
            Assert.AreEqual(fileName, meta.Filename);

        }

        /// <summary>
        /// UploadUpdateFileAsync（オプションなし）
        /// リクエストパラメータにContent-Type、metaETag、fileETagが含まれないこと
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileAsyncWithMetaDataNormalWithoutOptions()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var updateMetadata = new NbFileMetadata(bucketName, jsonObject);

            updateMetadata.FileEtag = null;
            updateMetadata.MetaEtag = null;
            updateMetadata.ContentType = null;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadUpdateFileAsync(fileData, updateMetadata);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Response Check
            Assert.AreEqual(fileName, meta.Filename);

        }

        /// <summary>
        /// UploadUpdateFileAsync（正常）
        /// リクエストにキャッシュ禁止フラグが含まれないこと
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileAsyncWithMetaDataNormalNoCacheDisabled()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var updateMetadata = new NbFileMetadata(bucketName, jsonObject);
            updateMetadata.CacheDisabled = true;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var meta = await bucket.UploadUpdateFileAsync(fileData, updateMetadata);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("meta", req.QueryParams["metaETag"].ToString());
            Assert.AreEqual("file", req.QueryParams["fileETag"].ToString());
            Assert.IsFalse(req.QueryParams.ContainsKey("cacheDisabled"));

        }

        /// <summary>
        /// UploadUpdateFileAsync（メタデータなし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadUpdateFileAsyncWithMetaDataExceptionWithoutMetadata()
        {

            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var meta = await bucket.UploadUpdateFileAsync(fileData, null);

        }

        /// <summary>
        /// UploadUpdateFileAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadUpdateFileAsyncWithMetaDataExceptionWithoutFileName()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, null, contentType, fileData.Length);
            var updateMetadata = new NbFileMetadata(bucketName, jsonObject);

            var meta = await bucket.UploadUpdateFileAsync(fileData, updateMetadata);
        }

        /// <summary>
        /// UploadUpdateFileAsync（ファイルデータなし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadUpdateFileAsyncWithMetaDataExceptionWithoutFileData()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var updateMetadata = new NbFileMetadata(bucketName, jsonObject);

            var meta = await bucket.UploadUpdateFileAsync(null, updateMetadata);

        }

        /// <summary>
        /// UploadUpdateFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileAsyncWithMetaDataExceptionWithResponseError()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var updateMetadata = new NbFileMetadata(bucketName, jsonObject);

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            NbFileMetadata meta = null;

            try
            {
                meta = await bucket.UploadUpdateFileAsync(fileData, updateMetadata);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }
        }

        /**
         * DownloadFileAsync
         **/
        /// <summary>
        /// DownloadFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDownloadFileAsyncNormal()
        {
            var byteData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var length = byteData.Length;
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, byteData, contentType);
            response.RawBytes = byteData;
            response.Headers.Add("X-Content-Length", length.ToString());
            restExecutor.AddResponse(response);

            var downloadData = await bucket.DownloadFileAsync(fileName);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName));

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(fileName, downloadData.Filename);
            Assert.AreEqual(length, downloadData.ContentLength);
            Assert.AreEqual(length, downloadData.XContentLength);
            Assert.AreEqual(contentType, downloadData.ContentType);
            Assert.AreEqual(byteData, downloadData.RawBytes);

        }

        /// <summary>
        /// DownloadFileAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDownloadFileAsyncExceptionWithoutFileName()
        {
            var downloadData = await bucket.DownloadFileAsync(null);
        }

        /// <summary>
        /// DownloadFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDownloadFileAsyncExceptionWithResponseError()
        {
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.NotFound);
            response.Content = new ByteArrayContent(fileData);
            restExecutor.AddResponse(response);

            try
            {
                var downloadData = await bucket.DownloadFileAsync(fileName);
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
        }

        /// <summary>
        /// DownloadFileAsync（ダウンロードサイズエラー）
        /// NbExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDownloadFileAsyncExceptionWithSizeError()
        {
            var byteData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var length = byteData.Length;
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, byteData, contentType);
            response.RawBytes = byteData;
            response.Headers.Add("X-Content-Length", (length - 1).ToString());
            restExecutor.AddResponse(response);

            try
            {
                var downloadData = await bucket.DownloadFileAsync(fileName);
            }
            catch (NbException e)
            {
                Assert.AreEqual(NbStatusCode.FailedToDownload, e.StatusCode);
            }

        }

        /**
         * DeleteFileAsync（ファイル名）
         **/
        /// <summary>
        /// DeleteFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteFileAsyncNormal()
        {
            var fileName = "TestFile";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.DeleteFileAsync(fileName);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName));

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.IsEmpty(resp);
        }

        /// <summary>
        /// DeleteFileAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteFileAsyncExceptionWithoutFileName()
        {
            string fileName = null;
            var resp = await bucket.DeleteFileAsync(fileName);

        }

        /// <summary>
        /// DeleteFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDeleteFileAsyncExceptionWithResponseError()
        {
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            try
            {
                var resp = await bucket.DeleteFileAsync(fileName);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }
        }

        /**
         * DeleteFileAsync（メタデータ）
         **/
        /// <summary>
        /// DeleteFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteFileAsyncWithMetadataNormal()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.DeleteFileAsync(targetMetadata);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName));

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("meta", req.QueryParams["metaETag"].ToString());
            Assert.AreEqual("file", req.QueryParams["fileETag"].ToString());

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.IsEmpty(resp);
        }

        /// <summary>
        /// DeleteFileAsync（オプションなし）
        /// リクエストパラメータにmetaETag、fileETagが含まれないこと
        /// </summary>
        [Test]
        public async void TestDeleteFileAsyncWithMetadataNormalWithoutOptions()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);
            targetMetadata.MetaEtag = null;
            targetMetadata.FileEtag = null;

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.DeleteFileAsync(targetMetadata);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Response Check
            Assert.IsEmpty(resp);
        }

        /// <summary>
        /// DeleteFileAsync（メタデータなし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteFileAsyncWithMetadataExceptionWithoutMetadata()
        {
            NbFileMetadata targetMetadata = null;
            var resp = await bucket.DeleteFileAsync(targetMetadata);

        }

        /// <summary>
        /// DeleteFileAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteFileAsyncWithMetadataExceptionWithoutFileName()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);

            targetMetadata.Filename = null;

            var resp = await bucket.DeleteFileAsync(targetMetadata);

        }

        /// <summary>
        /// DeleteFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDeleteFileAsyncWithMetadataExceptionWithResponseError()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            NbJsonObject resp = null;

            try
            {
                resp = await bucket.DeleteFileAsync(targetMetadata);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {

                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);

            }

            Assert.IsNull(resp);
        }

        /**
         * GetFileMetadataAsync
         **/
        /// <summary>
        /// GetFileMetadataAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと。
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestGetFileMetadataAsyncNormal()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.GetFileMetadataAsync(fileName);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Get, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName + "/meta"));

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(fileName, resp.Filename);

        }

        /// <summary>
        /// GetFileMetadataAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestGetFileMetadataAsyncExceptionWithoutFileName()
        {
            var resp = await bucket.GetFileMetadataAsync(null);

        }

        /// <summary>
        /// GetFileMetadataAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestGetFileMetadataAsyncExceptionWithResponseError()
        {

            var fileName = "TestFile";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            NbFileMetadata resp = null;
            try
            {
                resp = await bucket.GetFileMetadataAsync(fileName);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);

            }
        }

        /**
         * UpdateFileMetadataAsync
         **/
        /// <summary>
        /// UpdateFileMetadataAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataAsyncNormal()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.UpdateFileMetadataAsync(fileName, targetMetadata);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName + "/meta"));

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            Assert.AreEqual("meta", req.QueryParams["metaETag"].ToString());

            // Request Body
            var requestBody = NbJsonObject.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(4, requestBody.ToArray().Length);
            Assert.AreEqual(fileName, requestBody["filename"]);
            Assert.AreEqual(contentType, requestBody["contentType"]);
            Assert.AreEqual(NbAcl.CreateAclForAnonymous().ToJson(), requestBody["ACL"]);
            Assert.AreEqual(false, requestBody["cacheDisabled"]);

            // Response Check
            Assert.AreEqual(fileName, resp.Filename);
        }

        /// <summary>
        /// UpdateFileMetadataAsync（オプションなし）
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataAsyncNormalWithoutOptions()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);

            targetMetadata.Filename = null;
            targetMetadata.ContentType = null;
            targetMetadata.Acl = null;
            targetMetadata.MetaEtag = null;
            
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.UpdateFileMetadataAsync(fileName, targetMetadata);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName + "/meta"));

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            var requestBody = NbJsonObject.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, requestBody.ToArray().Length);
            Assert.AreEqual(false, requestBody["cacheDisabled"]);

            // Response Check
            Assert.AreEqual(fileName, resp.Filename);
        }

        /// <summary>
        /// UpdateFileMetadataAsync（メタデータなし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUpdateFileMetadataAsyncExceptionWithoutMetadata()
        {
            var fileName = "TestFile";

            var resp = await bucket.UpdateFileMetadataAsync(fileName, null);

        }

        /// <summary>
        /// UpdateFileMetadataAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUpdateFileMetadataAsyncExceptionWithoutFileName()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);

            var resp = await bucket.UpdateFileMetadataAsync(null, targetMetadata);

        }

        /// <summary>
        /// UpdateFileMetadataAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataAsyncExceptionWithResponseError()
        {
            var bucketName = "TestFileBucket";
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            var jsonObject = CreateMetaJson(id, fileName, contentType, fileData.Length);
            var targetMetadata = new NbFileMetadata(bucketName, jsonObject);

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            NbFileMetadata resp = null;
            try
            {
                resp = await bucket.UpdateFileMetadataAsync(fileName, targetMetadata);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);

            }

        }

        /**
         * PublishFileAsync
         **/
        /// <summary>
        /// PublishFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestPublishFileAsyncNormal()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var metaEtag = "metaEtag";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.PublishFileAsync(fileName, metaEtag);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName + "/publish"));

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            Assert.AreEqual(metaEtag, req.QueryParams["metaETag"].ToString());

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(fileName, resp.Filename);
        }

        /// <summary>
        /// PublishFileAsync（オプションなし）
        /// リクエストパラメータにmetaETagが含まれないこと
        /// </summary>
        [Test]
        public async void TestPublishFileAsyncNormalWithoutOptions()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.PublishFileAsync(fileName);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

        }

        /// <summary>
        /// PublishFileAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestPublishFileAsyncExceptionWithoutFileName()
        {

            var resp = await bucket.PublishFileAsync(null);

        }

        /// <summary>
        /// PublishFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestPublishFileAsyncExceptionWithResponseError()
        {
            var fileName = "TestFile";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            NbFileMetadata resp = null;

            try
            {
                resp = await bucket.PublishFileAsync(fileName);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);

            }

            Assert.IsNull(resp);

        }

        /**
         * UnpublishFileAsync
         **/
        /// <summary>
        /// UnpublishFileAsync（正常）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestUnpublishFileAsyncNormal()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";
            var metaEtag = "metaEtag";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.UnpublishFileAsync(fileName, metaEtag);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // Header
            Assert.AreEqual(3, req.Headers.Count);
            Assert.IsTrue(req.Headers.ContainsKey(appKey));
            Assert.IsTrue(req.Headers.ContainsKey(appId));

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/files/" + bucket.BucketName + "/" + fileName + "/publish"));

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            Assert.AreEqual(metaEtag, req.QueryParams["metaETag"].ToString());

            // Request Body
            Assert.IsNull(req.Content);

            // Response Check
            Assert.AreEqual(fileName, resp.Filename);
        }

        /// <summary>
        /// UnpublishFileAsync（オプションなし）
        /// リクエストパラメータにmetaETagが含まれないこと
        /// </summary>
        [Test]
        public async void TestUnpublishFileAsyncNormalWithoutOptions()
        {
            var id = "fileId";
            var fileData = System.Text.Encoding.UTF8.GetBytes("TestData");
            var fileName = "TestFile";
            var contentType = "image/jpeg";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateMetaJson(id, fileName, contentType, fileData.Length).ToString());
            restExecutor.AddResponse(response);

            var resp = await bucket.UnpublishFileAsync(fileName);

            // Requset Check
            var req = restExecutor.LastRequest;

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);
        }

        /// <summary>
        /// UnpublishFileAsync（ファイル名なし）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUnpublishFileAsyncExceptionWithoutFileName()
        {
            var resp = await bucket.UnpublishFileAsync(null);
        }

        /// <summary>
        /// UnpublishFileAsync（レスポンスエラー）
        /// NbHttpExceptionが発行され
        /// </summary>
        [Test]
        public async void TestUnpublishFileAsyncExceptionWithResponseError()
        {
            var fileName = "TestFile";

            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            NbFileMetadata resp = null;

            try
            {
                resp = await bucket.UnpublishFileAsync(fileName);
                Assert.Fail();
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }

        }

        /**
         * Test Utilities
         **/
        private NbJsonObject CreateMetaJson(string id, string filename, string contentType, int length)
        {
            var json = new NbJsonObject()
            {
                {"_id", id},
                {"filename", filename},
                {"contentType", contentType},
                {"length", length},
                {"ACL", NbAcl.CreateAclForAnonymous().ToJson()},
                {"createdAt", "1970-01-01T00:00:00.000Z"},
                {"updatedAt", "1970-01-01T00:00:00.000Z"},
                {"metaETag", "meta"},
                {"fileETag", "file"},
            };
            return json;
        }

        private NbJsonObject CreateGetMetaDataResponseJson(int objectNumber)
        {
            var json = new NbJsonObject();

            var array = new NbJsonArray();
            for (int i = 0; i < objectNumber; i++)
            {
                array.Add(CreateMetaJson("id" + i, "fileName" + i, "image/jpeg", 10));

            }
            json["currentTime"] = "1970-01-01T00:00:00.000Z";
            json["results"] = new NbJsonArray(array);

            return json;
        }

        private async Task<NbJsonObject> GetResponseJson(HttpResponseMessage response)
        {
            var bodyString = await response.Content.ReadAsStringAsync();
            var responseJson = NbJsonObject.Parse(bodyString);
            return responseJson;
        }

    }
}
