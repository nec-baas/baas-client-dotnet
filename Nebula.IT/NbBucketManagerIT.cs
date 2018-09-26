using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Nec.Nebula.IT
{
    /// <summary>
    /// バケットマネージャテスト
    /// </summary>
    class NbBucketManagerIT
    {
        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
        }

        
        /// <summary>
        /// バケットの作成・取得・削除ができること
        /// </summary>
        [Test]
        public async void CreateGetDeleteBucket()
        {
            CreateGetDeleteBucketSub(NbBucketManager.BucketType.Object);
            CreateGetDeleteBucketSub(NbBucketManager.BucketType.File);
        }

        private async void CreateGetDeleteBucketSub(NbBucketManager.BucketType type) {
            ITUtil.UseMasterKey();
            var manager = new NbBucketManager(type);

            var name = "BucketManagerIT";

            var json = await manager.CreateBucketAsync(name, "desc", new NbAcl(), new NbContentAcl());
            Assert.AreEqual(name, json["name"]);

            json = await manager.GetBucketAsync(name);
            Assert.AreEqual(name, json["name"]);

            await manager.DeleteBucketAsync(name);

            try
            {
                await manager.GetBucketAsync(name);
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
        }
    }
}
