using NUnit.Framework;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbBatchResultTest
    {
        private NbBatchResult result;
        private NbJsonObject json;
        private NbObject obj;

        [SetUp]
        public void Setup()
        {

            result = null;

            obj = new NbObject("bucket");
            obj.Id = "testId";
            obj.Etag = "testETag";
            obj.CreatedAt = "createdAt";
            obj.UpdatedAt = "updatedAt";

            obj["key"] = "Value";

            json = new NbJsonObject();
            json["_id"] = obj.Id;
            json["result"] = "ok";
            json["reasonCode"] = "unspecified";
            json["etag"] = obj.Etag;
            json["updatedAt"] = obj.UpdatedAt;
            json["data"] = obj.ToJson();

        }

        /**
         * Constructor
         */

        /// <summary>
        /// Constructor
        /// 各プロパティにJsonで記載された値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            result = new NbBatchResult(json);

            Assert.AreEqual(json["_id"], result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Ok, result.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.Unspecified, result.Reason);
            Assert.AreEqual(json["etag"], result.Etag);
            Assert.AreEqual(json["updatedAt"], result.UpdatedAt);
            Assert.AreEqual(json["data"].ToString(), result.Data.ToString());

        }

        /// <summary>
        /// Constructor
        /// Jsonが空の場合、初期値が設定されること
        /// ID:Null
        /// Etag:Null
        /// UpdateAt:Null
        /// Data:Null
        /// Result:ResultCode.Unknown
        /// Reason:ReasonCode.Unknown
        /// </summary>
        [Test]
        public void TestConstructorSubnormalNoJson()
        {
            json.Clear();
            result = new NbBatchResult(json);

            Assert.IsNull(result.Id);
            Assert.AreEqual(NbBatchResult.ResultCode.Unknown, result.Result);
            Assert.AreEqual(NbBatchResult.ReasonCode.Unknown, result.Reason);
            Assert.IsNull(result.Etag);
            Assert.IsNull(result.UpdatedAt);
            Assert.IsNull(result.Data);
        }

        /**
         * ResultCodeFromString
         * レスポンスに含まれるResult文字列がNbBatchResult.ResultCodeに変換できること
         */
        [Test]
        public void TestResultCodeFromStirngNormal()
        {
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ResultCode.Ok, result.Result);

            json["result"] = "conflict";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ResultCode.Conflict, result.Result);

            json["result"] = "forbidden";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ResultCode.Forbidden, result.Result);

            json["result"] = "notFound";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ResultCode.NotFound, result.Result);

            json["result"] = "badRequest";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ResultCode.BadRequest, result.Result);

            json["result"] = "serverError";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ResultCode.ServerError, result.Result);

            json["result"] = "TestResult";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ResultCode.Unknown, result.Result);

        }

        /**
         * ReasonCodeFromString
         * レスポンスに含まれるReasonCode文字列がNbBatchResult.ReasonCodeに変換できること
         */
        [Test]
        public void TestReasonCodeFromStirngNormal()
        {
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ReasonCode.Unspecified, result.Reason);

            json["reasonCode"] = "request_conflicted";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ReasonCode.RequestConflicted, result.Reason);

            json["reasonCode"] = "duplicate_key";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ReasonCode.DuplicateKey, result.Reason);

            json["reasonCode"] = "duplicate_id";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ReasonCode.DuplicateId, result.Reason);

            json["reasonCode"] = "etag_mismatch";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ReasonCode.EtagMismatch, result.Reason);

            json["reasonCode"] = "TestResult";
            result = new NbBatchResult(json);
            Assert.AreEqual(NbBatchResult.ReasonCode.Unknown, result.Reason);
        }

    }
}
