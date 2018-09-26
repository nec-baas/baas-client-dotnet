using NUnit.Framework;
using System.Linq;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class MultiAppIT
    {
        private const int NumApps = 2;

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            DeleteAllGroups();
            DeleteAllUsers();
            DeleteAllFiles();
            DeleteAllObjects();
        }

        [TearDown]
        public void TearDown()
        {
        }

        /**
         * グループ管理
         **/

        /// <summary>
        /// マルチアプリ評価
        /// </summary>
        [Test]
        public void TestMultiAppGroups()
        {
            // Save
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                var group = new NbGroup("mtgroup" + i);
                var result = group.SaveAsync().Result;
                Assert.AreEqual("mtgroup" + i, result.Name);
            }

            // Query
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                var results = NbGroup.QueryGroupsAsync().Result;
                Assert.AreEqual(NumApps, results.Count());
                Assert.AreEqual("mtgroup" + i, results.ToList()[i].Name);
            }

            // Get
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                var result = NbGroup.GetGroupAsync("mtgroup" + i).Result;
                Assert.AreEqual("mtgroup" + i, result.Name);
            }

            // Delete
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                var results = NbGroup.QueryGroupsAsync().Result;
                results.ToList()[0].DeleteAsync().Wait();
            }

        }


        /**
         * ユーザ管理
         **/

        /// <summary>
        /// マルチアプリ評価
        /// </summary>
        [Test]
        public void TestMultiAppUsers()
        {
            // signup
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                var user = new NbUser();
                user.Username = "mtUser" + i;
                user.Email = "mtUser" + i + "@example.com";
                var result = user.SignUpAsync("password").Result;
                Assert.AreEqual(result.Username, user.Username);
                Assert.AreEqual(result.Email, user.Email);
                Assert.AreEqual(result.Service, NbService.Singleton);
            }

            // Query
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i, false);
                var results = NbUser.QueryUserAsync().Result;
                Assert.AreEqual(NumApps, results.Count());
                Assert.AreEqual(results.ToList()[i].Username, "mtUser" + i);
                ITUtil.UseAppIDKey(i);
            }

            // IsLoggedIn
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                Assert.IsFalse(NbUser.IsLoggedIn());
            }

            // Login
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                var result = NbUser.LoginWithUsernameAsync("mtUser" + i, "password").Result;
                Assert.AreEqual(result.Service, NbService.Singleton);

                // IsLoggedIn
                Assert.IsTrue(NbUser.IsLoggedIn());

                // CurrentUser
                var currentUser = NbUser.CurrentUser();
                Assert.AreEqual(currentUser.Username, "mtUser" + i);
                Assert.IsNotNull(NbService.Singleton.SessionInfo.SessionToken);
                Assert.IsTrue(NbService.Singleton.SessionInfo.Expire > 0);

                // GetUserAsync
                var getUser = NbUser.GetUserAsync(result.UserId).Result;
                Assert.AreEqual(getUser.Username, "mtUser" + i);
                Assert.AreEqual(result.UserId, getUser.UserId);

                // RefreshCurrentUserAsync
                var refreshUser = NbUser.RefreshCurrentUserAsync().Result;
                Assert.AreEqual(refreshUser.Username, "mtUser" + i);
                Assert.AreEqual(result.UserId, refreshUser.UserId);
            }

            // Logout
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                if (i == 0)
                {
                    NbUser.LogoutAsync().Wait();
                    // IsLoggedIn
                    Assert.IsFalse(NbUser.IsLoggedIn());
                }
                else
                {
                    Assert.IsFalse(NbUser.IsLoggedIn());
                }
            }

            // Delete
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i, false);
                var results = NbUser.QueryUserAsync().Result;
                var queryUser = results.ToList()[0];
                queryUser.DeleteAsync().Wait();
                ITUtil.UseAppIDKey(i);
            }
        }


        /**
         * ファイルストレージ
         **/

        /// <summary>
        /// マルチアプリ評価
        /// </summary>
        [Test]
        public void TestMultiAppFileStorage()
        {
            var bucket = new NbFileBucket(ITUtil.FileBucketName);
            var acl = NbAcl.CreateAclForAnonymous();
            var data = ITUtil.GetTextBytes(10);
            var contentType = "text/plain";

            for (int i = 0; i < NumApps; i++)
            {
                // UploadNewFile
                ITUtil.UseAppIDKey(i);
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
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);
                var result = bucket.GetFilesAsync(false).Result.ToList();
                Assert.AreEqual(NumApps, result.Count);

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
        /// マルチアプリ評価
        /// </summary>
        [Test]
        public void TestMultiAppObjectStorage()
        {
            var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);
            var acl = NbAcl.CreateAclForAnonymous();

            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);

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
                var result = from x in batchResult select new NbObject(bucket.BucketName).FromJson(x.Data);
                var batchObj = result.First();
                Assert.True(batchObj.HasKey("batchKey"));

                // DeleteObject
                batchObj.DeleteAsync(true).Wait();
            }

            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);

                // Query
                var queryResult = bucket.QueryAsync(new NbQuery().DeleteMark(true)).Result;
                Assert.AreEqual(NumApps, queryResult.ToList().Count);
            }

            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i);

                // DeleteAllObjects
                var deleteCount = bucket.DeleteAsync(new NbQuery().DeleteMark(true), false).Result;
                Assert.AreEqual(NumApps, deleteCount);

                if (deleteCount == NumApps)
                {
                    Assert.AreEqual(0, i);
                    break;
                }
            }
        }

        private void DeleteAllGroups()
        {
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i, false);
                var results = NbGroup.QueryGroupsAsync().Result;
                foreach (var result in results)
                {
                    result.DeleteAsync().Wait();
                }
                ITUtil.UseAppIDKey(i);
            }
        }

        private void DeleteAllUsers()
        {
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i, false);
                var results = NbUser.QueryUserAsync().Result;
                foreach (var result in results)
                {
                    result.DeleteAsync().Wait();
                }
                ITUtil.UseAppIDKey(i);
            }
        }

        private void DeleteAllFiles()
        {
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i, false);
                ITUtil.CreateFileBucket().Wait();
                var bucket = new NbFileBucket(ITUtil.FileBucketName);
                var results = bucket.GetFilesAsync().Result;
                foreach (var result in results)
                {
                    bucket.DeleteFileAsync(result.Filename).Wait();
                }
                ITUtil.UseAppIDKey(i);
            }
        }

        private void DeleteAllObjects()
        {
            for (int i = 0; i < NumApps; i++)
            {
                ITUtil.UseAppIDKey(i, false);
                ITUtil.CreateObjectBucket().Wait();
                var bucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);
                bucket.DeleteAsync(new NbQuery().DeleteMark(true), false).Wait();
                ITUtil.UseAppIDKey(i);
            }
        }
    }
}
