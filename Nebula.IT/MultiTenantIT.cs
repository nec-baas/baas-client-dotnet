using NUnit.Framework;
using System.Linq;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class MultiTenantIT
    {
        private const int NumTenants = 2;
        private NbService[] tenants;
        private NbService[] tenantMasters;

        [SetUp]
        public void SetUp()
        {
            NbService.EnableMultiTenant(true);
            tenants = new NbService[NumTenants];
            tenantMasters = new NbService[NumTenants];

            for (int i = 0; i < NumTenants; i++)
            {
                tenants[i] = NbService.GetInstance();
                ITUtil.InitNebula(tenants[i], i);
                tenantMasters[i] = NbService.GetInstance();
                ITUtil.InitNebula(tenantMasters[i], i);
                ITUtil.UseMasterKey(tenantMasters[i], i);
            }

            DeleteAllGroups();
            DeleteAllUsers();
            DeleteAllFiles();
            DeleteAllObjects();
        }

        [TearDown]
        public void TearDown()
        {
            NbService.EnableMultiTenant(false);
        }


        /**
         * グループ管理
         **/

        /// <summary>
        /// マルチテナント評価
        /// </summary>
        [Test]
        public void TestMultiGroups()
        {
            // Save
            for (int i = 0; i < NumTenants; i++)
            {
                var group = new NbGroup("mtgroup" + i, tenants[i]);
                var result = group.SaveAsync().Result;
                Assert.AreEqual("mtgroup" + i, result.Name);
            }

            // Query
            for (int i = 0; i < NumTenants; i++)
            {
                var results = NbGroup.QueryGroupsAsync(tenantMasters[i]).Result;
                Assert.AreEqual(1, results.Count());
                Assert.AreEqual("mtgroup" + i, results.ToList()[0].Name);
            }

            // Get
            for (int i = 0; i < NumTenants; i++)
            {
                var result = NbGroup.GetGroupAsync("mtgroup" + i, tenantMasters[i]).Result;
                Assert.AreEqual("mtgroup" + i, result.Name);
            }

            // Delete
            for (int i = 0; i < NumTenants; i++)
            {
                var results = NbGroup.QueryGroupsAsync(tenantMasters[i]).Result;
                results.ToList()[0].DeleteAsync().Wait();
            }
        }


        /**
         * ユーザ管理
         **/

        /// <summary>
        /// マルチテナント評価
        /// </summary>
        [Test]
        public void TestMultiUsers()
        {
            // signup
            for (int i = 0; i < NumTenants; i++)
            {
                var user = new NbUser(tenants[i]);
                user.Username = "mtUser" + i;
                user.Email = "mtUser" + i + "@example.com";
                var result = user.SignUpAsync("password").Result;
                Assert.AreEqual(result.Username, user.Username);
                Assert.AreEqual(result.Email, user.Email);
                Assert.AreEqual(result.Service, tenants[i]);
            }

            // Query
            for (int i = 0; i < NumTenants; i++)
            {
                var results = NbUser.QueryUserAsync(null, null, tenantMasters[i]).Result;
                Assert.AreEqual(1, results.Count());
                Assert.AreEqual(results.ToList()[0].Username, "mtUser" + i);
            }

            // IsLoggedIn
            for (int i = 0; i < NumTenants; i++)
            {
                Assert.IsFalse(NbUser.IsLoggedIn(tenants[i]));
                Assert.IsFalse(NbUser.IsLoggedIn(tenantMasters[i]));
            }

            // Login
            for (int i = 0; i < NumTenants; i++)
            {
                var result = NbUser.LoginWithUsernameAsync("mtUser" + i, "password", tenants[i]).Result;
                Assert.AreEqual(result.Service, tenants[i]);

                // IsLoggedIn
                Assert.IsTrue(NbUser.IsLoggedIn(tenants[i]));
                Assert.IsFalse(NbUser.IsLoggedIn(tenantMasters[i]));
                if (i == 0)
                    Assert.IsFalse(NbUser.IsLoggedIn(tenantMasters[1]));

                // CurrentUser
                var currentUser = NbUser.CurrentUser(tenants[i]);
                Assert.AreEqual(currentUser.Username, "mtUser" + i);
                Assert.IsNotNull(tenants[i].SessionInfo.SessionToken);
                Assert.IsTrue(tenants[i].SessionInfo.Expire > 0);

                // GetUserAsync
                var getUser = NbUser.GetUserAsync(result.UserId, tenants[i]).Result;
                Assert.AreEqual(getUser.Username, "mtUser" + i);
                Assert.AreEqual(result.UserId, getUser.UserId);

                // RefreshCurrentUserAsync
                var refreshUser = NbUser.RefreshCurrentUserAsync(tenants[i]).Result;
                Assert.AreEqual(refreshUser.Username, "mtUser" + i);
                Assert.AreEqual(result.UserId, refreshUser.UserId);
            }

            // Logout
            for (int i = 0; i < NumTenants; i++)
            {
                NbUser.LogoutAsync(NbUser.LoginMode.Online, tenants[i]).Wait();

                // IsLoggedIn
                Assert.IsFalse(NbUser.IsLoggedIn(tenants[i]));
                if (i == 0)
                    Assert.IsTrue(NbUser.IsLoggedIn(tenants[1]));
            }

            // Delete
            for (int i = 0; i < NumTenants; i++)
            {
                var results = NbUser.QueryUserAsync(null, null, tenantMasters[i]).Result;
                Assert.AreEqual(1, results.Count());
                var queryUser = results.ToList()[0];
                Assert.AreEqual(queryUser.Username, "mtUser" + i);
                queryUser.DeleteAsync().Wait();
            }
        }


        /**
         * ファイルストレージ
         **/

        /// <summary>
        /// マルチテナント評価
        /// </summary>
        [Test]
        public void TestMultiTenantFileStorage()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            var data = ITUtil.GetTextBytes(10);
            var contentType = "text/plain";

            for (int i = 0; i < NumTenants; i++)
            {
                var bucket = new NbFileBucket(ITUtil.FileBucketName, tenants[i]);

                // UploadNewFile
                var fileName = string.Format("UploadFile_{0:D4}.txt", i);
                var result = bucket.UploadNewFileAsync(data, fileName, contentType, acl, true).Result;
                Assert.NotNull(result.Id);
                Assert.AreEqual(fileName, result.Filename);

                // GetFileMetadata
                var meta = bucket.GetFileMetadataAsync(fileName).Result;
                Assert.AreEqual(result.Id, meta.Id);

                // Download
                var downloadData = bucket.DownloadFileAsync(fileName).Result;
                Assert.AreEqual(data, downloadData.RawBytes);
            }

            // GetFiles
            for (int i = 0; i < NumTenants; i++)
            {
                var bucket = new NbFileBucket(ITUtil.FileBucketName, tenants[i]);

                var result = bucket.GetFilesAsync(false).Result.ToList();
                Assert.AreEqual(1, result.Count);

                foreach (var file in result)
                {
                    Assert.True(file.Filename.StartsWith("UploadFile_"));
                }
            }
        }

        /**
         * オブジェクトストレージ
         **/

        /// <summary>
        /// マルチテナント評価
        /// </summary>
        [Test]
        public void TestMultiTenantObjectStorage()
        {
            var acl = NbAcl.CreateAclForAnonymous();

            for (int i = 0; i < NumTenants; i++)
            {
                var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName, tenants[i]);

                // CreateObject                
                var obj = bucket.NewObject();
                obj.Acl = acl;
                obj["key"] = "value";
                obj = obj.SaveAsync().Result;
                Assert.NotNull(obj.Id);

                // UpdateObject
                obj["updateKey"] = "updateValue";
                var updateObj = obj.SaveAsync().Result;
                Assert.True(updateObj.HasKey("updateKey"));

                // Batch
                updateObj["batchKey"] = "batchValue";
                var batchRequest = new NbBatchRequest().AddUpdateRequest(updateObj);
                var batchResult = bucket.BatchAsync(batchRequest).Result;
                var result = from x in batchResult select new NbObject(bucket.BucketName, tenants[i]).FromJson(x.Data);
                var batchObj = result.First();
                Assert.True(batchObj.HasKey("batchKey"));

                // DeleteObject
                batchObj.DeleteAsync(true).Wait();
            }

            for (int i = 0; i < NumTenants; i++)
            {
                var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName, tenants[i]);

                // Query
                var queryResult = bucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;
                Assert.AreEqual(1, queryResult.ToList().Count);
            }

            for (int i = 0; i < NumTenants; i++)
            {
                var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName, tenants[i]);

                // DeleteAllObjects
                var deleteCount = bucket.DeleteAsync(new NbQuery().DeleteMark(true), false).Result;
                Assert.AreEqual(1, deleteCount);
            }
        }

        /**
          * Util for MultiTenantIT
          **/

        private void DeleteAllGroups()
        {
            for (int i = 0; i < NumTenants; i++)
            {
                var results = NbGroup.QueryGroupsAsync(tenantMasters[i]).Result;
                foreach (var result in results)
                {
                    result.DeleteAsync().Wait();
                }
            }
        }

        private void DeleteAllUsers()
        {
            for (int i = 0; i < NumTenants; i++)
            {
                var results = NbUser.QueryUserAsync(null, null, tenantMasters[i]).Result;
                foreach (var result in results)
                {
                    result.DeleteAsync().Wait();
                }
            }
        }

        private void DeleteAllFiles()
        {
            for (int i = 0; i < NumTenants; i++)
            {
                ITUtil.CreateFileBucket(null, null, tenantMasters[i]).Wait();
                var bucket = new NbFileBucket(ITUtil.FileBucketName, tenantMasters[i]);
                var results = bucket.GetFilesAsync().Result;
                foreach (var result in results)
                {
                    bucket.DeleteFileAsync(result.Filename).Wait();
                }
            }
        }

        private void DeleteAllObjects()
        {
            for (int i = 0; i < NumTenants; i++)
            {
                ITUtil.CreateObjectBucket(null, null, tenantMasters[i]).Wait();
                var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName, tenantMasters[i]);
                bucket.DeleteAsync(new NbQuery().DeleteMark(true), false).Wait();
            }
        }
    }
}
