using NUnit.Framework;
using System;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbBatchRequestTest
    {
        private NbBatchRequest request;
        private NbObject obj;

        [SetUp]
        public void Setup()
        {
            request = new NbBatchRequest();
            obj = new NbObject("bucket");
            obj["key"] = "Value";
        }

        /**
         * Constructor
         */

        /// <summary>
        /// Constructor
        /// ・オペレーションは0件であること
        /// ・JSONには"requests"をキーとする空配列が設定されていること
        /// ・RequestTokenはNULLでないこと
        /// </summary>
        [Test]
        public void TestConstructNormal()
        {
            Assert.AreEqual(0, request.Requests.Count);

            Assert.AreEqual(1, request.Json.Keys.Count);
            Assert.AreEqual(0, request.Json.GetArray("requests").Count);

            Assert.IsNotNull(request.RequestToken);
        }

        /**
         * AddInsertRequest
         */

        /// <summary>
        /// 追加要求
        /// ・"requests"には1件の要求があること
        /// ・"op"キーには"insert"が設定されていること
        /// ・"data"キーには指定したオブジェクトのJSONが含まれていること
        /// </summary>
        [Test]
        public void TestAddInsertRequestNormal()
        {
            request.AddInsertRequest(obj);

            Assert.AreEqual(1, request.Json.GetArray("requests").Count);
            var json = request.Requests.GetJsonObject(0);
            Assert.AreEqual(2, json.Keys.Count);
            Assert.AreEqual("insert", json["op"]);
            Assert.AreEqual(obj.ToJson(), json["data"]);
        }

        /// <summary>
        /// 追加要求
        /// ・オブジェクト未指定時はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddInsertRequestExceptionNoObject()
        {
            request.AddInsertRequest(null);
        }

        /// <summary>
        /// 追加要求
        /// ・オブジェクト情報にETagが設定されている場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestAddInsertRequestExceptionEtagExist()
        {
            obj.Etag = "invalid info";
            request.AddInsertRequest(obj);
        }

        /**
         * AddUpdateRequest
        */

        /// <summary>
        /// 更新要求
        /// ・"requests"には1件の要求があること
        /// ・"op"キーには"update"が設定されていること
        /// ・"id"キーには指定したオブジェクトのIDが設定されること
        /// ・"etag"キーには指定したオブジェクトのETagが設定されること
        /// ・"data"キーには指定したオブジェクトのJSONが含まれていること
        /// ・指定したオブジェクトのJSONは"$full_update"演算子をキーとしていること
        /// </summary>
        [Test]
        public void TestAddUpdateRequestNormal()
        {
            obj.Id = "updateId";
            obj.Etag = "updateETag";
            request.AddUpdateRequest(obj);

            Assert.AreEqual(1, request.Json.GetArray("requests").Count);
            var json = request.Requests.GetJsonObject(0);

            Assert.AreEqual(4, json.Keys.Count);
            Assert.AreEqual("update", json["op"]);
            Assert.AreEqual(obj.Id, json["_id"]);
            Assert.AreEqual(obj.Etag, json["etag"]);

            var updateData = (NbJsonObject)json["data"];
            Assert.AreEqual(1, updateData.Keys.Count);
            Assert.AreEqual(obj.ToJson(), updateData["$full_update"]);
        }

        /// <summary>
        /// 更新要求
        /// ・"etag"キーは任意情報であること
        /// </summary>
        [Test]
        public void TestAddUpdateRequsetSubnormalNoETag()
        {
            obj.Id = "updateId";
            request.AddUpdateRequest(obj);

            Assert.AreEqual(1, request.Json.GetArray("requests").Count);
            var json = request.Requests.GetJsonObject(0);

            Assert.AreEqual(3, json.Keys.Count);
            Assert.AreEqual("update", json["op"]);
            Assert.AreEqual(obj.Id, json["_id"]);
            Assert.IsFalse(json.ContainsKey("etag"));

            var updateData = (NbJsonObject)json["data"];
            Assert.AreEqual(1, updateData.Keys.Count);
            Assert.AreEqual(obj.ToJson(), updateData["$full_update"]);
        }

        /// <summary>
        /// 更新要求
        /// ・オブジェクト未指定時はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddUpdateRequestExceptionNoObject()
        {
            request.AddUpdateRequest(null);
        }

        /// <summary>
        /// 更新要求
        /// ・オブジェクト情報にオブジェクトIDが設定されていない場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddUpdateRequestExceptionNoObjectID()
        {
            obj.Etag = "updateETag";
            request.AddUpdateRequest(obj);
        }

        /**
        * AddDeleteRequest
        */

        /// <summary>
        /// 削除要求
        /// ・"requests"には1件の要求があること
        /// ・"op"キーには"delete"が設定されていること
        /// ・"id"キーには指定したオブジェクトのIDが設定されること
        /// ・"etag"キーには指定したオブジェクトのETagが設定されること
        /// </summary>
        [Test]
        public void TestAddDeleteRequestNormal()
        {
            obj.Id = "deleteId";
            obj.Etag = "deleteETag";
            request.AddDeleteRequest(obj);

            Assert.AreEqual(1, request.Json.GetArray("requests").Count);
            var json = request.Requests.GetJsonObject(0);

            Assert.AreEqual(3, json.Keys.Count);
            Assert.AreEqual("delete", json["op"]);
            Assert.AreEqual(obj.Id, json["_id"]);
            Assert.AreEqual(obj.Etag, json["etag"]);

        }

        /// <summary>
        /// 削除要求
        /// ・"etag"キーは任意情報であること
        /// </summary>
        [Test]
        public void TestAddDeleteRequsetSubnormalNoETag()
        {
            obj.Id = "deleteId";
            request.AddDeleteRequest(obj);

            Assert.AreEqual(1, request.Json.GetArray("requests").Count);
            var json = request.Requests.GetJsonObject(0);

            Assert.AreEqual(2, json.Keys.Count);
            Assert.AreEqual("delete", json["op"]);
            Assert.AreEqual(obj.Id, json["_id"]);
            Assert.IsFalse(json.ContainsKey("etag"));

        }

        /// <summary>
        /// 削除要求
        /// ・オブジェクト未指定時はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddDeleteRequestExceptionNoObject()
        {
            request.AddDeleteRequest(null);
        }

        /// <summary>
        /// 削除要求
        /// ・オブジェクト情報にオブジェクトIDが設定されていない場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddDeleteRequestExceptionNoObjectID()
        {
            obj.Etag = "deleteETag";
            request.AddDeleteRequest(obj);
        }

        /**
         * GetOp
         */

        /// <summary>
        /// リクエストオペレーション取得
        /// ・追加したリクエスト通りのオペレーションが取得できること
        /// </summary>
        [Test]
        public void TestGetOpNormal()
        {
            obj.Id = "Id";
            request.AddInsertRequest(obj);
            request.AddUpdateRequest(obj);
            request.AddDeleteRequest(obj);

            var requestArray = request.Json.GetArray("requests");
            Assert.AreEqual(3, requestArray.Count);

            for (int i = 0; i < requestArray.Count; i++)
            {
                var op = request.GetOp(i);

                switch (i)
                {
                    case 0:
                        Assert.AreEqual("insert", op);
                        break;
                    case 1:
                        Assert.AreEqual("update", op);
                        break;
                    case 2:
                        Assert.AreEqual("delete", op);
                        break;
                    default:
                        Assert.Fail();
                        break;
                }
            }

        }

        /// <summary>
        /// リクエストオペレーション取得
        /// ・範囲外を指定した場合はエラーとなること
        /// </summary>
        [Test, ExpectedException(typeof (ArgumentOutOfRangeException))]
        public void TestGetOpExceptionOutOfRangeIndex()
        {
            obj.Id = "Id";

            var requestArray = request.Json.GetArray("requests");
            Assert.AreEqual(0, requestArray.Count);

            var op = request.GetOp(10);
        }

    }
}
