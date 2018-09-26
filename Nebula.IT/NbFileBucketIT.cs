using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class NbFileBucketIT
    {
        private const string BucketName = ITUtil.FileBucketName;
        private const string FileName = "UploadFileA";
        private const string ContentType = "text/plain";

        private NbFileBucket _bucket;

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineUser().Wait();
            ITUtil.InitOnlineFileStorage().Wait();

            _bucket = new NbFileBucket(BucketName);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ・未ログイン<br/>
        /// ・ACL指定なし<br/>
        /// ・キャッシュ禁止<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileNormalNoLogin()
        {

            var expectedAcl = NbAcl.CreateAclForAnonymous();
            expectedAcl.Admin.Clear();

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);

            Assert.NotNull(meta.Id);
            Assert.AreEqual(FileName, meta.Filename);
            Assert.AreEqual(ContentType, meta.ContentType);
            Assert.AreEqual(data.Length, meta.Length);
            Assert.True(ITUtil.CompareAcl(expectedAcl, meta.Acl));
            Assert.NotNull(meta.CreatedAt);
            Assert.NotNull(meta.UpdatedAt);
            Assert.NotNull(meta.MetaEtag);
            Assert.NotNull(meta.FileEtag);
            Assert.AreEqual(cacheDisabled, meta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ・未ログイン<br/>
        /// ・ACL指定あり<br/>
        /// ・キャッシュ許可<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileNormalNoLoginWithACL()
        {

            var acl = NbAcl.CreateAclForAuthenticated();
            acl.Owner = "TestUser";
            var cacheDisabled = false;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl, cacheDisabled);

            Assert.NotNull(meta.Id);
            Assert.AreEqual(FileName, meta.Filename);
            Assert.AreEqual(ContentType, meta.ContentType);
            Assert.AreEqual(data.Length, meta.Length);
            Assert.True(ITUtil.CompareAcl(acl, meta.Acl));
            Assert.NotNull(meta.CreatedAt);
            Assert.NotNull(meta.UpdatedAt);
            Assert.NotNull(meta.MetaEtag);
            Assert.NotNull(meta.FileEtag);
            Assert.AreEqual(cacheDisabled, meta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ・未ログイン<br/>
        /// ・ACL指定あり（Owner未設定）<br/>
        /// ・キャッシュ許可<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileNormalNoLoginNoOwner()
        {

            var acl = NbAcl.CreateAclForAuthenticated();
            var cacheDisabled = false;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl, cacheDisabled);

            Assert.NotNull(meta.Id);
            Assert.AreEqual(FileName, meta.Filename);
            Assert.AreEqual(ContentType, meta.ContentType);
            Assert.AreEqual(data.Length, meta.Length);
            Assert.True(ITUtil.CompareAcl(acl, meta.Acl));
            Assert.IsNull(meta.Acl.Owner);
            Assert.NotNull(meta.CreatedAt);
            Assert.NotNull(meta.UpdatedAt);
            Assert.NotNull(meta.MetaEtag);
            Assert.NotNull(meta.FileEtag);
            Assert.AreEqual(cacheDisabled, meta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ・ログイン済み<br/>
        /// ・ACL指定なし<br/>
        /// ・キャッシュ禁止<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileNormalLogin()
        {

            var user = await ITUtil.SignUpAndLogin();

            var expectedAcl = new NbAcl();
            expectedAcl.Owner = user.UserId;

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);

            Assert.NotNull(meta.Id);
            Assert.AreEqual(FileName, meta.Filename);
            Assert.AreEqual(ContentType, meta.ContentType);
            Assert.AreEqual(data.Length, meta.Length);
            Assert.True(ITUtil.CompareAcl(expectedAcl, meta.Acl));
            Assert.NotNull(meta.CreatedAt);
            Assert.NotNull(meta.UpdatedAt);
            Assert.NotNull(meta.MetaEtag);
            Assert.NotNull(meta.FileEtag);
            Assert.AreEqual(cacheDisabled, meta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ・ログイン済み<br/>
        /// ・ACL指定あり<br/>
        /// ・キャッシュ許可<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileNormalLoginWithACL()
        {

            var user = await ITUtil.SignUpAndLogin();

            var acl = NbAcl.CreateAclForAuthenticated();
            acl.Owner = "TestUser";
            var cacheDisabled = false;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl, cacheDisabled);

            Assert.NotNull(meta.Id);
            Assert.AreEqual(FileName, meta.Filename);
            Assert.AreEqual(ContentType, meta.ContentType);
            Assert.AreEqual(data.Length, meta.Length);
            Assert.True(ITUtil.CompareAcl(acl, meta.Acl));
            Assert.NotNull(meta.CreatedAt);
            Assert.NotNull(meta.UpdatedAt);
            Assert.NotNull(meta.MetaEtag);
            Assert.NotNull(meta.FileEtag);
            Assert.AreEqual(cacheDisabled, meta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ・ログイン済み<br/>
        /// ・ACL指定あり（Owner未設定）<br/>
        /// ・キャッシュ許可<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileNormalLoginNoOwner()
        {

            var user = await ITUtil.SignUpAndLogin();

            var acl = NbAcl.CreateAclForAuthenticated();
            var cacheDisabled = false;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl, cacheDisabled);

            Assert.NotNull(meta.Id);
            Assert.AreEqual(FileName, meta.Filename);
            Assert.AreEqual(ContentType, meta.ContentType);
            Assert.AreEqual(data.Length, meta.Length);
            Assert.AreEqual(acl.R, meta.Acl.R);
            Assert.AreEqual(acl.W, meta.Acl.W);
            Assert.AreEqual(acl.C, meta.Acl.C);
            Assert.AreEqual(acl.U, meta.Acl.U);
            Assert.AreEqual(acl.D, meta.Acl.D);
            Assert.AreEqual(acl.Admin, meta.Acl.Admin);
            Assert.AreEqual(user.UserId, meta.Acl.Owner);
            Assert.NotNull(meta.CreatedAt);
            Assert.NotNull(meta.UpdatedAt);
            Assert.NotNull(meta.MetaEtag);
            Assert.NotNull(meta.FileEtag);
            Assert.AreEqual(cacheDisabled, meta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ファイルサイズ0KB<br/>
        /// 登録ファイルサイズが0KB<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileSubnormalFileSize0()
        {

            var acl = NbAcl.CreateAclForAuthenticated();
            var cacheDisabled = false;
            var data = new byte[0];

            try
            {
                var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl, cacheDisabled);
                Assert.AreEqual(0, meta.Length);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// 不正なファイル名<br/>
        /// 登録ファイル名を以下とする<br/>
        /// UTF-8で900バイト以上<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileExceptionInvalidFilename()
        {
            StringBuilder sb = new StringBuilder();
            while (sb.Length <= 900)
            {
                sb.Append("UploadFileA");
            }
            var fileName = sb.ToString();
            var acl = NbAcl.CreateAclForAuthenticated();
            var cacheDisabled = false;
            var data = Encoding.UTF8.GetBytes("FileObject");

            try
            {
                var meta = await _bucket.UploadNewFileAsync(data, fileName, ContentType, acl, cacheDisabled);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// 処理競合<br/>
        /// ファイル名重複<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileExceptionConflictDuplicateFilename()
        {

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            await ITUtil.UploadFile(FileName);

            try
            {
                await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
                Assert.NotNull(ITUtil.ReasonCodeDuplicateFileName, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// HTTPヘッダ パラメータ誤り<br/>
        /// Application-Idが空文字列<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileExceptionHttpHeaderInvalidAppId()
        {

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var service = NbService.Singleton;
            service.AppId = "";

            try
            {
                await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ACL設定<br/>
        /// バケットのcontentACLにcreate権限がない<br/>
        /// 未ログイン<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileExceptionPermissionBucketContentAclCreateNone()
        {

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            try
            {
                await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// マスターキー（ACLなし）<br/>
        /// バケットのcontentACLにcreate権限がない<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileSubnormalMasterKeyAclNone()
        {

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            ITUtil.UseMasterKey();

            try
            {
                await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// マスターキー（ACLあり）<br/>
        /// バケットのcontentACLにcreate権限がある<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestUploadNewFileSubnormalMasterKeyAclExists()
        {

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            ITUtil.UseMasterKey();

            try
            {
                await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ファイル名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadNewFileExceptionNoFileName()
        {

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            await _bucket.UploadNewFileAsync(data, null, ContentType, null, cacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// ファイルがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadNewFileExceptionNoFile()
        {

            var cacheDisabled = true;

            await _bucket.UploadNewFileAsync(null, FileName, ContentType, null, cacheDisabled);

        }

        /// <summary>
        /// ファイルの新規アップロード<br/>
        /// content-Typeがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadNewFileExceptionNoContentType()
        {

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            await _bucket.UploadNewFileAsync(data, FileName, null, null, cacheDisabled);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// メタデータによる情報一括指定<br/>
        /// 同一ファイル名なし<br/>
        /// 未ログイン<br/>
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileNormalWithMetadata()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.ContentType = "text/html";
            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            var newMeta = await _bucket.UploadUpdateFileAsync(data, meta);

            Assert.AreEqual(meta.Id, newMeta.Id);
            Assert.AreEqual(meta.Filename, newMeta.Filename);
            Assert.AreEqual(meta.ContentType, newMeta.ContentType);
            Assert.AreEqual(data.Length, newMeta.Length);
            Assert.True(ITUtil.CompareAcl(meta.Acl, newMeta.Acl));
            Assert.AreEqual(meta.CreatedAt, newMeta.CreatedAt);
            Assert.AreNotEqual(meta.UpdatedAt, newMeta.UpdatedAt);
            Assert.AreNotEqual(meta.MetaEtag, newMeta.MetaEtag);
            Assert.AreNotEqual(meta.FileEtag, newMeta.FileEtag);
            Assert.AreEqual(meta.CacheDisabled, newMeta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// 情報個別指定<br/>
        /// 同一ファイル名あり<br/>
        /// ログイン済み<br/>
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileNormalWithString()
        {

            var user = await ITUtil.SignUpAndLogin();

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            var contentType = "text/html";
            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            var newMeta = await _bucket.UploadUpdateFileAsync(data, FileName, contentType, meta.MetaEtag, meta.FileEtag);

            Assert.AreEqual(meta.Id, newMeta.Id);
            Assert.AreEqual(meta.Filename, newMeta.Filename);
            Assert.AreEqual(contentType, newMeta.ContentType);
            Assert.AreEqual(data.Length, newMeta.Length);
            Assert.True(ITUtil.CompareAcl(meta.Acl, newMeta.Acl));
            Assert.AreEqual(meta.CreatedAt, newMeta.CreatedAt);
            Assert.AreNotEqual(meta.UpdatedAt, newMeta.UpdatedAt);
            Assert.AreNotEqual(meta.MetaEtag, newMeta.MetaEtag);
            Assert.AreNotEqual(meta.FileEtag, newMeta.FileEtag);
            Assert.AreEqual(meta.CacheDisabled, newMeta.CacheDisabled);

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// オプションパラメータ未設定<br/>
        /// 以下パラメータを未設定とする
        /// Content-Type<br/>
        /// MetaEtag<br/>
        /// FileEtag<br/>
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileSubnormalNoOptionParams()
        {

            var user = await ITUtil.SignUpAndLogin();

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            var newMeta = await _bucket.UploadUpdateFileAsync(data, FileName, null);

            Assert.AreEqual(meta.ContentType, newMeta.ContentType);

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// リクエストパラメータ パラメータ誤り<br/>
        /// Etag不一致<br/>
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileExceptionConflictMetaEtagMismatch()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.ContentType = "text/html";
            meta.MetaEtag = "";
            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            try
            {
                await _bucket.UploadUpdateFileAsync(data, meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
                Assert.AreEqual(ITUtil.ReasonCodeEtagMismatch, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// ACL設定<br/>
        /// バケットのcontentACLにupdate権限がない<br/>
        /// 未ログイン<br/>
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileExceptionPermissionBucketContentAclUpdateNone()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.ContentType = "text/html";
            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            try
            {
                var newMeta = await _bucket.UploadUpdateFileAsync(data, meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// マスターキー（ACLなし）<br/>
        /// contentACLにupdate権限がない<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileSubnormalMasterKeyAclNone()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.ContentType = "text/html";
            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            ITUtil.UseMasterKey();

            try
            {
                var newMeta = await _bucket.UploadUpdateFileAsync(data, meta);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// マスターキー（ACLあり）<br/>
        /// contentACLにupdate権限あり<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestUploadUpdateFileSubnormalMasterKeyAclExists()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.ContentType = "text/html";
            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            ITUtil.UseMasterKey();

            try
            {
                var newMeta = await _bucket.UploadUpdateFileAsync(data, meta);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// メタデータがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadUpdateFileExceptionNoMetadata()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            await _bucket.UploadUpdateFileAsync(data, null);

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// ファイル名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadUpdateFileExceptionNoFileName()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            var data = Encoding.UTF8.GetBytes("UpdateFileObject");

            await _bucket.UploadUpdateFileAsync(data, null, meta.ContentType, meta.MetaEtag, meta.FileEtag);

        }

        /// <summary>
        /// ファイルの更新アップロード<br/>
        /// ファイルがnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUploadUpdateFileExceptionNoFile()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            await _bucket.UploadUpdateFileAsync(null, FileName, meta.ContentType, meta.MetaEtag, meta.FileEtag);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイルのダウンロード<br/>
        /// ダウンロード成功<br/>
        /// </summary>
        [Test]
        public async void TestDonwloadFileNormal()
        {

            var expectedAcl = NbAcl.CreateAclForAnonymous();
            expectedAcl.Admin.Clear();

            var cacheDisabled = true;
            var data = Encoding.UTF8.GetBytes("FileObject");

            var meta = await _bucket.UploadNewFileAsync(data, FileName, ContentType, null, cacheDisabled);

            var downloadData = await _bucket.DownloadFileAsync(meta.Filename);

            Assert.AreEqual(FileName, downloadData.Filename);
            Assert.AreEqual(ContentType, downloadData.ContentType);
            Assert.AreNotEqual(0, downloadData.ContentLength);
            Assert.AreNotEqual(0, downloadData.XContentLength);
            Assert.AreEqual(data, downloadData.RawBytes);

        }

        /// <summary>
        /// ファイルのダウンロード<br/>
        /// パス不正<br/>
        /// filenameに誤り<br/>
        /// </summary>
        [Test]
        public async void TestDonwloadFileExceptionInvalidFilename()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            try
            {
                var downloadData = await _bucket.DownloadFileAsync("DownloadFileA");
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルのダウンロード<br/>
        /// ファイル名がnull<br/>
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDonwloadFileExceptionNoFileName()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            await _bucket.DownloadFileAsync(null);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイル一覧の取得<br/>
        /// 公開状態指定なし<br/>
        /// </summary>
        [Test]
        public async void TestGetFilesNormalAllData()
        {

            var meta1 = await ITUtil.UploadFile("UploadFileA");
            var meta2 = await ITUtil.UploadFile("UploadFileB");
            var meta3 = await ITUtil.UploadFile("UploadFileC");

            await _bucket.PublishFileAsync(meta1.Filename);
            await _bucket.PublishFileAsync(meta3.Filename);

            var result = await _bucket.GetFilesAsync(false);

            Assert.AreEqual(3, result.ToList().Count);
        }

        /// <summary>
        /// ファイル一覧の取得<br/>
        /// 公開状態指定あり<br/>
        /// </summary>
        [Test]
        public async void TestGetFilesNormalPublishOnly()
        {

            var meta1 = await ITUtil.UploadFile("UploadFileA");
            var meta2 = await ITUtil.UploadFile("UploadFileB");
            var meta3 = await ITUtil.UploadFile("UploadFileC");

            await _bucket.PublishFileAsync(meta1.Filename);
            await _bucket.PublishFileAsync(meta3.Filename);

            var result = await _bucket.GetFilesAsync(true);
            var files = result.ToList();

            var v = from file in files where file.Filename == "UploadFileB" select file;

            Assert.AreEqual(2, files.Count);
            Assert.IsEmpty(v);
        }

        /// <summary>
        /// ファイル一覧の取得<br/>
        /// 該当ファイルがない(0件Hit)<br/>
        /// </summary>
        [Test]
        public async void TestGetFilesNormalNoData()
        {

            var result = await _bucket.GetFilesAsync(true);

            Assert.AreEqual(0, result.ToList().Count);

        }

        /// <summary>
        /// ファイル一覧の取得<br/>
        /// ACL設定<br/>
        /// バケットのcontentACLにread権限がない<br/>
        /// </summary>
        [Test]
        public async void TestGetFilesExceptionPermissionBucketContentAclReadNone()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            await ITUtil.CreateFileBucket(null, new NbContentAcl());

            try
            {
                await _bucket.GetFilesAsync(false);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイル一覧の取得<br/>
        /// ACL設定<br/>
        /// ファイルのACLにread権限がない<br/>
        /// </summary>
        [Test]
        public async void TestGetFilesSubnormalPermissionFileAclReadNone()
        {

            var meta1 = await ITUtil.UploadFile("UploadFileA", new NbAcl());
            var meta2 = await ITUtil.UploadFile("UploadFileB");

            var result = await _bucket.GetFilesAsync(false);
            var files = result.ToList();

            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("UploadFileB", files[0].Filename);
        }

        /// <summary>
        /// ファイル一覧の取得<br/>
        /// マスターキー（ACLなし）<br/>
        /// バケットのcontentACLとファイルのACLにread権限がない<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestGetFilesSubnormalMasterKeyAclNone()
        {

            var meta1 = await ITUtil.UploadFile("UploadFileA", new NbAcl());
            var meta2 = await ITUtil.UploadFile("UploadFileB");

            await ITUtil.CreateFileBucket(null, new NbContentAcl());

            ITUtil.UseMasterKey();

            var result = await _bucket.GetFilesAsync(false);
            var files = result.ToList();

            Assert.AreEqual(2, files.Count);

        }

        /// <summary>
        /// ファイル一覧の取得<br/>
        /// マスターキー（ACLあり）<br/>
        /// バケットのcontentACLとファイルのACLにread権限がある<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestGetFilesSubnormalMasterKeyAclExists()
        {

            var meta1 = await ITUtil.UploadFile("UploadFileA");
            var meta2 = await ITUtil.UploadFile("UploadFileB");

            ITUtil.UseMasterKey();

            var result = await _bucket.GetFilesAsync(false);
            var files = result.ToList();

            Assert.AreEqual(2, files.Count);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 特定ファイルのメタデータ取得<br/>
        /// </summary>
        [Test]
        public async void TestGetFileMetadataNormalFileExist()
        {

            var meta1 = await ITUtil.UploadFile("UploadFileA");
            var meta2 = await ITUtil.UploadFile("UploadFileB");
            var meta3 = await ITUtil.UploadFile("UploadFileC");

            var fileMetadata = await _bucket.GetFileMetadataAsync(meta1.Filename);

            Assert.AreEqual(meta1.BucketName, fileMetadata.BucketName);
            Assert.AreEqual(meta1.Id, fileMetadata.Id);
            Assert.AreEqual(meta1.Filename, fileMetadata.Filename);
            Assert.AreEqual(meta1.ContentType, fileMetadata.ContentType);
            Assert.AreEqual(meta1.Length, fileMetadata.Length);
            Assert.True(ITUtil.CompareAcl(meta1.Acl, fileMetadata.Acl));
            Assert.AreEqual(meta1.CreatedAt, fileMetadata.CreatedAt);
            Assert.AreEqual(meta1.UpdatedAt, fileMetadata.UpdatedAt);
            Assert.AreEqual(meta1.MetaEtag, fileMetadata.MetaEtag);
            Assert.AreEqual(meta1.FileEtag, fileMetadata.FileEtag);
            Assert.AreEqual(meta1.CacheDisabled, fileMetadata.CacheDisabled);

        }

        /// <summary>
        /// 特定ファイルのメタデータ取得<br/>
        /// HTTPヘッダ パラメータ誤り<br/>
        /// Application-Idに誤り<br/>
        /// </summary>
        [Test]
        public async void TestGetFileMetadataExceptionHttpHeaderInvalidAppId()
        {

            var meta1 = await ITUtil.UploadFile("UploadFileA");
            var meta2 = await ITUtil.UploadFile("UploadFileB");
            var meta3 = await ITUtil.UploadFile("UploadFileC");

            var service = NbService.Singleton;
            service.AppId = "dummy";

            try
            {
                await _bucket.GetFileMetadataAsync(meta1.Filename);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// 特定ファイルのメタデータ取得<br/>
        /// ファイル名がnull
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestGetFileMetadataExceptionNoFileName()
        {
            await _bucket.GetFileMetadataAsync(null);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// ファイルメタデータの更新<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataNormal()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var acl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = acl;
            meta.CacheDisabled = false;

            var newMeta = await _bucket.UpdateFileMetadataAsync(FileName, meta);

            Assert.AreEqual(meta.Filename, newMeta.Filename);
            Assert.AreEqual(meta.ContentType, newMeta.ContentType);
            Assert.True(ITUtil.CompareAcl(meta.Acl, newMeta.Acl));
            Assert.AreEqual(meta.CacheDisabled, newMeta.CacheDisabled);
            Assert.AreEqual(meta.Id, newMeta.Id);
            Assert.AreEqual(meta.CreatedAt, newMeta.CreatedAt);
            Assert.AreNotEqual(meta.UpdatedAt, newMeta.UpdatedAt);
            Assert.AreEqual(meta.Length, newMeta.Length);
            Assert.AreNotEqual(meta.MetaEtag, newMeta.MetaEtag);
            Assert.AreEqual(meta.FileEtag, newMeta.FileEtag);

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// オプションパラメータ未設定<br/>
        /// 以下パラメータを未設定とする（Null指定）<br/>
        /// Content-Type<br/>
        /// Acl<br/>
        /// MetaEtag<br/>
        /// FileEtag<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataSubnormalNoOptionParams()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);
            var expectedFileName = meta.Filename;
            var expectedContentType = meta.ContentType;
            var expectedAcl = meta.Acl;

            meta.ContentType = null;
            meta.Acl = null;
            meta.MetaEtag = null;
            meta.FileEtag = null;

            var newMeta = await _bucket.UpdateFileMetadataAsync(FileName, meta);

            Assert.AreEqual(expectedFileName, newMeta.Filename);
            Assert.AreEqual(expectedContentType, newMeta.ContentType);
            Assert.True(ITUtil.CompareAcl(expectedAcl, newMeta.Acl));
            Assert.AreEqual(meta.CacheDisabled, newMeta.CacheDisabled);

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// ファイルメタデータの更新<br/>
        /// ファイル名重複<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataExceptionConflictDuplicateFilename()
        {

            var meta1 = await ITUtil.UploadFile(FileName);
            var meta2 = await ITUtil.UploadFile("UploadFileB");

            meta1.Filename = "UploadFileB";
            meta1.ContentType = "text/html";
            var acl = NbAcl.CreateAclForAuthenticated();
            meta1.Acl = acl;
            meta1.CacheDisabled = false;

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta1);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
                Assert.AreEqual(ITUtil.ReasonCodeDuplicateFileName, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// パス不正<br/>
        /// ファイル名に誤り<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataExceptionInvalidFileName()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var acl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = acl;
            meta.CacheDisabled = false;

            try
            {
                await _bucket.UpdateFileMetadataAsync("DummyUploadFileA", meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// リクエストパラメータ パラメータ誤り<br/>
        /// Etag不一致<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataExceptionRequsetParameterEtagMismatch()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var acl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = acl;
            meta.CacheDisabled = false;
            meta.MetaEtag = "dummy";

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
                Assert.AreEqual(ITUtil.ReasonCodeEtagMismatch, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// 不正なファイル名<br/>
        /// 登録ファイル名を以下とする<br/>
        /// !\"#$%&'()*+,:;<=>?@[]^{|}~<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataExceptionRequestBodyInvalidFilename()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.Filename = "!\"#$%&'()*+,:;<=>?@[]^{|}~";
            meta.ContentType = "text/html";
            var acl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = acl;
            meta.CacheDisabled = false;

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/> 
        /// ACL設定<br/>
        /// バケットのcontentACLにupdate権限がない<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataExceptionPermissionBucketContentAclUpdateNone()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var acl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = acl;
            meta.CacheDisabled = false;

            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// ACL設定<br/>
        /// ファイルのACLにupdate権限がない<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataExceptionPermissionFileAclUpdateNone()
        {

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");

            var meta = await ITUtil.UploadFile(FileName, acl);
            Assert.NotNull(meta);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var newAcl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = newAcl;
            meta.CacheDisabled = false;

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// ACL設定<br/>
        /// ファイルのadmin権限がない<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataExceptionPermissionFileAclAdminNone()
        {

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            acl.W.Add("g:anonymous");

            var meta = await ITUtil.UploadFile(FileName, acl);
            Assert.NotNull(meta);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var newAcl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = newAcl;
            meta.CacheDisabled = false;

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// マスターキー（ACLなし）<br/>
        /// バケットのcontentACLとファイルのACLにupdate権限、Admin権限がない<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataSubnormalMasterKeyAclNone()
        {

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");

            var meta = await ITUtil.UploadFile(FileName, acl);
            Assert.NotNull(meta);

            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var newAcl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = newAcl;
            meta.CacheDisabled = false;

            ITUtil.UseMasterKey();

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// マスターキー（ACLあり）<br/>
        /// バケットのcontentACLとファイルのACLにupdate権限、Admin権限がある<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestUpdateFileMetadataSubnormalMasterKeyAclExists()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.Filename = "UploadFileB";
            meta.ContentType = "text/html";
            var newAcl = NbAcl.CreateAclForAuthenticated();
            meta.Acl = newAcl;
            meta.CacheDisabled = false;

            ITUtil.UseMasterKey();

            try
            {
                await _bucket.UpdateFileMetadataAsync(FileName, meta);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// ファイル名がnull
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUpdateFileMetadataExceptionNoFileName()
        {

            var meta = await ITUtil.UploadFile(FileName);

            await _bucket.UpdateFileMetadataAsync(null, meta);

        }

        /// <summary>
        /// ファイルメタデータの更新<br/>
        /// メタデータがnull
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUpdateFileMetadataExceptionNoMetadata()
        {

            var meta = await ITUtil.UploadFile(FileName);

            await _bucket.UpdateFileMetadataAsync(FileName, null);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイルの削除<br/>
        /// ファイルの削除<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileNormal()
        {

            var meta = await ITUtil.UploadFile(FileName);

            await _bucket.DeleteFileAsync(FileName);

            try
            {
                await _bucket.GetFileMetadataAsync(FileName);
                Assert.Fail("No exception");
            }
            catch (Exception)
            {
                // ok
            }

        }

        /// <summary>
        /// ファイルの削除<br/>
        /// オプションパラメータ未設定<br/>
        /// 以下パラメータを未設定とする（Null指定）<br/>
        /// MetaEtag<br/>
        /// FileEtag<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileSubnormalNoOptionParams()
        {

            var meta = await ITUtil.UploadFile(FileName);

            meta.MetaEtag = null;
            meta.FileEtag = null;

            await _bucket.DeleteFileAsync(meta);

            try
            {
                await _bucket.GetFileMetadataAsync(FileName);
                Assert.Fail("No exception");
            }
            catch (Exception)
            {
                // ok
            }

        }

        /// <summary>
        /// ファイルの削除<br/>
        /// 処理競合<br/>
        /// ファイルロック中<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileExceptionConflictFileLock()
        {

            var meta = await ITUtil.UploadFile(FileName);

            // 更新アップロード中にファイル削除を実施する
            // NW負荷がない状態でも確実にファイルロックとなるよう、
            // 大き目のサイズにしてある
            var data = ITUtil.GetTextBytes(7 * ITUtil.BytesPerMbyte);
            var uploadTask = _bucket.UploadUpdateFileAsync(data, meta);

            try
            {
                // #7578 タイミングによりファイル更新実行前にファイル削除実行してしまう場合があるので Sleep を入れる
                Thread.Sleep(50);
                await _bucket.DeleteFileAsync(FileName);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                // #7578 v6.5以前のサーバはタイミングにより 409 Conflict, 423 Locked のどちらかを返すため、どちらでもOKとする
                // いずれ 423 に統一する(申し送り #7607)
                Assert.IsTrue((e.StatusCode == HttpStatusCode.Conflict) || (e.StatusCode == (HttpStatusCode)423));
                if (e.StatusCode == HttpStatusCode.Conflict)
                {
                    Assert.AreEqual(ITUtil.ReasonCodeFileLocked, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                    Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
                }
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

            await uploadTask;

        }

        /// <summary>
        /// ファイルの削除<br/>
        /// リクエストパラメータ パラメータ誤り<br/>
        /// FileEtag不一致<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileExceptionRequsetParameterEtagMismatch()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            meta.FileEtag = "dummy";

            try
            {
                await _bucket.DeleteFileAsync(meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
                Assert.AreEqual(ITUtil.ReasonCodeEtagMismatch, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの削除<br/> 
        /// ACL設定<br/>
        /// バケットのcontentACLにdelete権限がない<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileExceptionPermissionBucketContentAclDelete()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            try
            {
                await _bucket.DeleteFileAsync(meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの削除<br/> 
        /// ACL設定<br/>
        /// ファイルのACLにdelete権限がない<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileExceptionPermissionFileAclDelete()
        {

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");

            var meta = await ITUtil.UploadFile(FileName, acl);
            Assert.NotNull(meta);

            try
            {
                await _bucket.DeleteFileAsync(meta);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの削除<br/>
        /// マスターキー（ACLなし）<br/>
        /// バケットのcontentACLとファイルのACL設定にdelete権限がない<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileSubnormalMasterKeyAclNone()
        {

            var acl = new NbAcl();
            acl.R.Add("g:anonymous");

            var meta = await ITUtil.UploadFile(FileName, acl);
            Assert.NotNull(meta);

            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("g:anonymous");

            await ITUtil.CreateFileBucket(null, contentAcl);

            ITUtil.UseMasterKey();

            try
            {
                await _bucket.DeleteFileAsync(meta);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの削除<br/>
        /// マスターキー（ACLあり）<br/>
        /// バケットのcontentACLとファイルのACL設定にdelete権限がある<br/>
        /// 未ログイン<br/>
        /// AppKeyにマスターキーを使用する<br/>
        /// </summary>
        [Test]
        public async void TestDeleteFileSubnormalMasterKeyAclExists()
        {

            var meta = await ITUtil.UploadFile(FileName);
            Assert.NotNull(meta);

            ITUtil.UseMasterKey();

            try
            {
                await _bucket.DeleteFileAsync(meta);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの削除<br/>
        /// ファイル名がnull
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteFileExceptionNoMetadata()
        {

            var meta = await ITUtil.UploadFile(FileName);

            await _bucket.DeleteFileAsync((string)null);

        }

        /// <summary>
        /// ファイルの削除<br/>
        /// メタデータがnull
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestDeleteFileExceptionNoFileName()
        {

            var meta = await ITUtil.UploadFile(FileName);

            await _bucket.DeleteFileAsync((NbFileMetadata)null);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイルの公開<br/>
        /// ファイルの公開<br/>
        /// </summary>
        [Test]
        public async void TestPublishFileNormal()
        {

            var meta = await ITUtil.UploadFile(FileName);

            var newMeta = await _bucket.PublishFileAsync(FileName, meta.MetaEtag);

            Assert.AreEqual(meta.Id, newMeta.Id);
            Assert.AreEqual(meta.Filename, newMeta.Filename);
            Assert.AreEqual(meta.ContentType, newMeta.ContentType);
            Assert.AreEqual(meta.Length, newMeta.Length);
            Assert.True(ITUtil.CompareAcl(meta.Acl, newMeta.Acl));
            Assert.AreEqual(meta.CreatedAt, newMeta.CreatedAt);
            Assert.AreNotEqual(meta.UpdatedAt, newMeta.UpdatedAt);
            Assert.AreNotEqual(meta.MetaEtag, newMeta.MetaEtag);
            Assert.AreEqual(meta.FileEtag, newMeta.FileEtag);
            Assert.AreEqual(meta.CacheDisabled, newMeta.CacheDisabled);
            Assert.NotNull(newMeta.PublicUrl);

        }

        /// <summary>
        /// ファイルの公開<br/>
        /// オプションパラメータ未設定<br/>
        /// 以下パラメータを未設定とする<br/>
        /// MetaEtag<br/>
        /// </summary>
        [Test]
        public async void TestPublishFileSubnormalNoOptionParams()
        {

            var meta = await ITUtil.UploadFile(FileName);

            var newMeta = await _bucket.PublishFileAsync(FileName);

            Assert.NotNull(newMeta.PublicUrl);

        }

        /// <summary>
        /// ファイルの公開<br/>
        /// ファイル名がnull
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestPublishFileExceptionNoFileName()
        {

            var meta = await ITUtil.UploadFile(FileName);

            await _bucket.PublishFileAsync(null, meta.MetaEtag);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// ファイルの公開解除<br/>
        /// ファイルの公開解除<br/>
        /// </summary>
        [Test]
        public async void TestUnpublishFileNormal()
        {

            var meta = await ITUtil.UploadFile(FileName);
            meta = await _bucket.PublishFileAsync(FileName, meta.MetaEtag);

            var newMeta = await _bucket.UnpublishFileAsync(FileName, meta.MetaEtag);

            Assert.AreEqual(meta.Id, newMeta.Id);
            Assert.AreEqual(meta.Filename, newMeta.Filename);
            Assert.AreEqual(meta.ContentType, newMeta.ContentType);
            Assert.AreEqual(meta.Length, newMeta.Length);
            Assert.True(ITUtil.CompareAcl(meta.Acl, newMeta.Acl));
            Assert.AreEqual(meta.CreatedAt, newMeta.CreatedAt);
            Assert.AreNotEqual(meta.UpdatedAt, newMeta.UpdatedAt);
            Assert.AreNotEqual(meta.MetaEtag, newMeta.MetaEtag);
            Assert.AreEqual(meta.FileEtag, newMeta.FileEtag);
            Assert.AreEqual(meta.CacheDisabled, newMeta.CacheDisabled);
            Assert.IsNull(newMeta.PublicUrl);

        }

        /// <summary>
        /// ファイルの公開解除<br/>
        /// オプションパラメータ未設定<br/>
        /// 以下パラメータを未設定とする<br/>
        /// MetaEtag<br/>
        /// </summary>
        [Test]
        public async void TestUnpublishFileSubnormalNoOptionParams()
        {

            var meta = await ITUtil.UploadFile(FileName);
            meta = await _bucket.PublishFileAsync(FileName, meta.MetaEtag);

            var newMeta = await _bucket.UnpublishFileAsync(FileName);

            Assert.IsNull(newMeta.PublicUrl);

        }

        /// <summary>
        /// ファイルの公開解除<br/>
        /// 解除中の解除<br/>
        /// </summary>
        [Test]
        public async void TestUnpublishFileSubnormalReUnpublishing()
        {

            var meta = await ITUtil.UploadFile(FileName);
            meta = await _bucket.PublishFileAsync(FileName, meta.MetaEtag);

            var newMeta = await _bucket.UnpublishFileAsync(FileName, meta.MetaEtag);

            try
            {
                await _bucket.UnpublishFileAsync(FileName);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの公開解除<br/>
        /// リクエストパラメータ パラメータ誤り<br/>
        /// Etag不一致<br/>
        /// </summary>
        [Test]
        public async void TestUnpublishFileExceptionConflictMetaEtagMismatch()
        {

            var meta = await ITUtil.UploadFile(FileName);
            meta = await _bucket.PublishFileAsync(FileName, meta.MetaEtag);

            meta.MetaEtag = "dummy";

            try
            {
                await _bucket.UnpublishFileAsync(FileName, meta.MetaEtag);
                Assert.Fail("No exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, e.StatusCode);
                Assert.AreEqual(ITUtil.ReasonCodeEtagMismatch, ITUtil.GetErrorInfo(e.Response, ITUtil.ReasonCode));
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response, ITUtil.Detail));
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }

        }

        /// <summary>
        /// ファイルの公開解除<br/>
        /// ファイル名がnull
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestUnpublishFileExceptionNoFileName()
        {

            var meta = await ITUtil.UploadFile(FileName);

            await _bucket.UnpublishFileAsync(null, meta.MetaEtag);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 初期値<br/>
        /// NbFileBucket<br/>
        /// newで生成する<br/>
        /// </summary>
        [Test]
        public void TestNbFileBucketConstructorNormal()
        {

            var bucket = new NbFileBucket(ITUtil.FileBucketName);

            Assert.AreEqual(ITUtil.FileBucketName, bucket.BucketName);
        }

        /// <summary>
        /// 初期値<br/>
        /// DownloadData<br/>
        /// newで生成する<br/>
        /// </summary>
        [Test]
        public void TestDownloadDataNormal()
        {

            var data = new NbFileBucket.DownloadData();

            Assert.Null(data.Filename);
            Assert.Null(data.ContentType);
            Assert.AreEqual(0, data.ContentLength);
            Assert.AreEqual(0, data.XContentLength);
            Assert.Null(data.RawBytes);

        }

        /// <summary>
        /// 初期値<br/>
        /// NbFileMetadata<br/>
        /// newで生成する<br/>
        /// </summary>
        [Test]
        public void TestNbFileMetadataConstructorNormal()
        {
            var acl = NbAcl.CreateAclForAnonymous();

            var json = new NbJsonObject()
            {
                {Field.Id, "testId"},
                {Field.Filename, FileName},
                {Field.ContentType, ContentType},
                {Field.Length, 2},
                {Field.Acl, acl.ToJson()},
                {Field.CreatedAt, "2015-03-03T03:03:03.003"},
                {Field.UpdatedAt, "2015-04-04T04:04:04.004"},
                {Field.MetaEtag, "testMetaEtag"},
                {Field.FileEtag, "testFileEtag"}
            };

            var meta = new NbFileMetadata(ITUtil.FileBucketName, json);

            Assert.AreEqual(ITUtil.FileBucketName, meta.BucketName);
            Assert.AreEqual("testId", meta.Id);
            Assert.AreEqual(FileName, meta.Filename);
            Assert.AreEqual(ContentType, meta.ContentType);
            Assert.AreEqual(2, meta.Length);
            Assert.True(ITUtil.CompareAcl(acl, meta.Acl));
            Assert.AreEqual("2015-03-03T03:03:03.003", meta.CreatedAt);
            Assert.AreEqual("2015-04-04T04:04:04.004", meta.UpdatedAt);
            Assert.AreEqual("testMetaEtag", meta.MetaEtag);
            Assert.AreEqual("testFileEtag", meta.FileEtag);
            Assert.Null(meta.PublicUrl);
            Assert.False(meta.CacheDisabled);

        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 繰り返し評価<br/>
        /// アップロード、更新、ダウンロード、検索、削除<br/>
        /// </summary>
        [Test]
        public async void TestCrudRepeatNormal()
        {

            var data = Encoding.UTF8.GetBytes("FileObject");
            var updateData = Encoding.UTF8.GetBytes("UpdateFileObject");
            var acl = NbAcl.CreateAclForAnonymous();

            // ファイルのアップロード、メタデータ取得、ダウンロードを3回実施
            for (int i = 0; i < 3; i++)
            {
                // 新規アップロード
                await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl);

                // メタデータ取得
                await _bucket.GetFileMetadataAsync(FileName);

                // ダウンロード
                await _bucket.DownloadFileAsync(FileName);

                // 削除
                await _bucket.DeleteFileAsync(FileName);
            }

            // 新規アップロード
            await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl);

            // ファイルの公開、非公開を3回実施
            for (int i = 0; i < 3; i++)
            {
                // ファイルの公開
                await _bucket.PublishFileAsync(FileName);

                // ファイルの非公開
                await _bucket.UnpublishFileAsync(FileName);
            }

            // 削除
            await _bucket.DeleteFileAsync(FileName);

            // ファイルの更新、メタデータ一覧取得、削除を3回実施
            for (int i = 0; i < 3; i++)
            {
                // 新規アップロード
                await _bucket.UploadNewFileAsync(data, FileName, ContentType, acl);

                // 更新アップロード
                await _bucket.UploadUpdateFileAsync(updateData, FileName, "text/html");

                // メタデータ一覧取得
                await _bucket.GetFilesAsync();

                // 削除
                await _bucket.DeleteFileAsync(FileName);
            }
        }

    }
}
