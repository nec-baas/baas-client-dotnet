using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbFileMetadataTest
    {
        /**
         * Constructor
         **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// メタデータの生成ができること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            string bucketName = "bucket";
            var json = CreateJsonObject(true);
            var meta = new NbFileMetadata(bucketName, json);

            Assert.AreEqual(bucketName, meta.BucketName);
            Assert.AreEqual(json["_id"], meta.Id);
            Assert.AreEqual(json["filename"], meta.Filename);
            Assert.AreEqual(json["contentType"], meta.ContentType);
            Assert.AreEqual(json["length"], meta.Length);
            Assert.AreEqual(json["createdAt"], meta.CreatedAt);
            Assert.AreEqual(json["updatedAt"], meta.UpdatedAt);
            Assert.AreEqual(json["ACL"], meta.Acl.ToJson());
            Assert.AreEqual(json["publicUrl"], meta.PublicUrl);
            Assert.AreEqual(json["cacheDisabled"], meta.CacheDisabled);
        }

        /// <summary>
        /// コンストラクタテスト（バケット名NULL）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionBucketNameNull()
        {
            string bucketName = null;
            var json = CreateJsonObject(true);

            var meta = new NbFileMetadata(bucketName, json);

        }

        /// <summary>
        /// コンストラクタテスト（JSONデータNULL）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionJsonNull()
        {
            string bucketName = "bucket";
            NbJsonObject json = null;

            var meta = new NbFileMetadata(bucketName, json);

        }

        /// <summary>
        /// コンストラクタテスト（オプションパラメータ未指定）
        /// 必須でないパラメータがKeyに含まれていない場合でも生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormalWithoutOptionParams()
        {
            string bucketName = "bucket";
            var json = CreateJsonObject(false);
            var meta = new NbFileMetadata(bucketName, json);

            Assert.AreEqual(bucketName, meta.BucketName);
            Assert.AreEqual(json["_id"], meta.Id);
            Assert.AreEqual(json["filename"], meta.Filename);
            Assert.AreEqual(json["contentType"], meta.ContentType);
            Assert.AreEqual(json["length"], meta.Length);
            Assert.AreEqual(json["createdAt"], meta.CreatedAt);
            Assert.AreEqual(json["updatedAt"], meta.UpdatedAt);
            Assert.AreEqual(json["ACL"], meta.Acl.ToJson());
            Assert.IsNull(meta.PublicUrl);
            Assert.IsFalse(meta.CacheDisabled);
        }

        /// <summary>
        /// コンストラクタテスト（余剰パラメータ）
        /// Jsonデータに余計なKeyがあっても無視されること
        /// </summary>
        [Test]
        public void TestConstructorSubnormalWrongKeys()
        {
            string bucketName = "bucket";
            var json = CreateJsonObject(true);
            json.Add("wrongkey", "wrongValue");

            var meta = new NbFileMetadata(bucketName, json);

        }

        /// <summary>
        /// コンストラクタテスト（必須パラメータ不足）
        /// KeyNotFoundExceptionが発行されること
        /// </summary>
        [Test]
        public void TestConstructorExceptionKeyNotFound()
        {
            string bucketName = "bucket";
            var json = CreateJsonObject(false);

            string[] array = new string[] { "_id", "filename", "contentType", "length", "ACL", "createdAt", "updatedAt", "metaETag", "fileETag" };

            foreach (var key in array)
            {
                // キーを一時的に削除
                var value = json[key];
                json.Remove(key);

                try
                {
                    var meta = new NbFileMetadata(bucketName, json);
                    Assert.Fail();
                }
                catch (KeyNotFoundException)
                {
                    // expected exception
                    // 削除キーを戻す
                    json.Add(key, value);
                }
            }

        }

        /**
         * ToUpdateJson
         **/
        /// <summary>
        /// ToUpdateJson（正常）
        /// 更新対象のJSONオブジェクトを作成できること
        /// </summary>
        [Test]
        public void TestToUpdateJsonNormal()
        {
            string bucketName = "bucket";
            var json = CreateJsonObject(true);
            var meta = new NbFileMetadata(bucketName, json);

            var updateJson = meta.ToUpdateJson();
            Assert.AreEqual(4, updateJson.ToList().Count);

            string[] array = new string[] { "filename", "contentType", "cacheDisabled", "ACL" };

            foreach (var name in array)
            {
                Assert.AreEqual(json[name], updateJson[name]);
            }

        }

        /// <summary>
        /// ToUpdateJson（更新プロパティ初期値）
        /// 更新対象がNullであった場合、Jsonに含まないこと
        /// </summary>
        [Test]
        public void TestToUpdateJsonSubnormalNoProperty()
        {
            string bucketName = "bucket";
            var json = CreateJsonObject(true);
            var meta = new NbFileMetadata(bucketName, json);

            meta.Filename = null;
            meta.ContentType = null;
            meta.Acl = null;
            meta.CacheDisabled = false;

            var updateJson = meta.ToUpdateJson();

            Assert.AreEqual(1, updateJson.ToList().Count);
            Assert.AreEqual(false, updateJson["cacheDisabled"]);

        }

        /**
         * Test Utilities
         **/
        private NbJsonObject CreateJsonObject(bool option = false)
        {
            var json = new NbJsonObject()
            {
                {"_id", "aaaaaa"},
                {"filename", "test.jpeg"},
                {"contentType", "image/jpeg"},
                {"length", 100},
                {"ACL", NbAcl.CreateAclForAnonymous().ToJson()},
                {"createdAt", "1970-01-01T00:00:00.000Z"},
                {"updatedAt", "1970-01-01T00:00:00.000Z"},
                {"metaETag", "meta"},
                {"fileETag", "file"}
            };

            if (option == true)
            {
                json.Add("publicUrl", "http://");
                json.Add("cacheDisabled", true);
                //{"_deleted", true}
            }

            return json;
        }
    }
}
