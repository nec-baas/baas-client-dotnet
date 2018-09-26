using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Nec.Nebula.Test
{
    [TestFixture]
    internal class NbBucketManagerTest
    {
        private MockRestExecutor restExecutor;

        [SetUp]
        public void SetUp()
        {
            TestUtils.Init();

            restExecutor = new MockRestExecutor();

            // inject rest executor
            NbService.Singleton.RestExecutor = restExecutor;
        }

        [Test]
        public void TestCreateObjectBucket()
        {
            TestCreateBucket(NbBucketManager.BucketType.Object);
        }

        [Test]
        public void TestCreateFileBucket()
        {
            TestCreateBucket(NbBucketManager.BucketType.File);
        }

        private void TestCreateBucket(NbBucketManager.BucketType bucketType)
        {
            var responseJson = SetMockSuccessfulResponse();

            var manager = new NbBucketManager(bucketType);

            var json = manager.CreateBucketAsync("bucket1", "desc1", new NbAcl(), new NbContentAcl()).Result;
            Assert.AreEqual(responseJson, json);

            var req = restExecutor.LastRequest;
            Assert.AreEqual(GetBucketUri(bucketType) +  "/bucket1", req.Uri);
            Assert.AreEqual(HttpMethod.Put, req.Method);
            // TODO: check content
        }

        private NbJsonObject SetMockSuccessfulResponse()
        {
            var responseJson = new NbJsonObject()
            {
                {"results", "ok"}
            };
            var response = new MockRestResponse()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson.ToString())
            };
            restExecutor.AddResponse(response);
            return responseJson;
        }

        private string GetBucketUri(NbBucketManager.BucketType bucketType)
        {
            return "http://api.example.com/1/tenant/buckets/" +
                   (bucketType == NbBucketManager.BucketType.Object ? "object" : "file");
        }

        [Test]
        public void TestDeleteObjectBucket()
        {
            TestDeleteBucket(NbBucketManager.BucketType.Object);
        }

        [Test]
        public void TestDeleteFileBucket()
        {
            TestDeleteBucket(NbBucketManager.BucketType.File);
        }

        private void TestDeleteBucket(NbBucketManager.BucketType bucketType)
        {
            var responseJson = SetMockSuccessfulResponse();

            var manager = new NbBucketManager(bucketType);

            manager.DeleteBucketAsync("bucket1").Wait();

            var req = restExecutor.LastRequest;
            Assert.AreEqual(GetBucketUri(bucketType) + "/bucket1", req.Uri);
            Assert.AreEqual(HttpMethod.Delete, req.Method);
        }

        [Test]
        public void TestGetObjectBucket()
        {
            TestGetBucket(NbBucketManager.BucketType.Object);
        }
        
        [Test]
        public void TestGetFileBucket()
        {
            TestGetBucket(NbBucketManager.BucketType.Object);
        }

        private void TestGetBucket(NbBucketManager.BucketType bucketType)
        {
            var responseJson = SetMockSuccessfulResponse();

            var manager = new NbBucketManager(bucketType);

            var json = manager.GetBucketAsync("bucket1").Result;
            Assert.AreEqual(responseJson, json);

            var req = restExecutor.LastRequest;
            Assert.AreEqual(GetBucketUri(bucketType) + "/bucket1", req.Uri);
            Assert.AreEqual(HttpMethod.Get, req.Method);
        }
    }
}
