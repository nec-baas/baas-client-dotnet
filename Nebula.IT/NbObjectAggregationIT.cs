using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    [TestFixture]
    public class NbObjectAggregationIT
    {
        private const string BucketOrders = "orders";
        private const string BucketInventory = "inventory";
        private const string BucketWarehouses = "warehouses";
        private NbService _service;
        private static NbObjectBucket<NbObject> _orderBucket;
        private static NbObjectBucket<NbObject> _inventoryBucket;
        private static NbObjectBucket<NbObject> _warehousesBucket;
        private NbJsonArray _aggregateResult;

        private static List<string> OrdersObjects = new List<string>()
        {
            "{\"_id\": \"112233445566778899aabb01\", \"item\": \"item1\", \"price\": 12," +
            " \"ordered\": {\"$numberLong\": \"2\"}, \"date\": {\"$date\": \"2018-01-01T08:00:00.111Z\"}}",
            "{\"_id\": \"112233445566778899aabb02\", \"item\": \"item2\", \"price\": 20," +
            " \"ordered\": {\"$numberLong\": \"1\"}, \"date\": {\"$date\": \"2018-01-01T09:00:00.222Z\"}}",
            "{\"_id\": \"112233445566778899aabb03\", \"item\": \"item3\", \"price\": 20," +
            " \"ordered\": {\"$numberLong\": \"1\"}, \"date\": {\"$date\": \"2018-01-15T09:00:00.333Z\"}}",
            "{\"_id\": \"112233445566778899aabb04\", \"item\": \"item4\", \"price\": 20," +
            " \"ordered\": {\"$numberLong\": \"35\"}, \"date\": {\"$date\": \"2018-02-04T11:21:39.444Z\"}}",
            "{\"_id\": \"112233445566778899aabb05\", \"item\": \"item5\", \"price\": 20," +
            " \"ordered\": {\"$numberLong\": \"50\"}, \"date\": {\"$date\": \"2018-02-04T21:23:13.555Z\"}}",
            "{\"_id\": \"112233445566778899aabb06\", \"item\": \"item6\", \"price\": 10," +
            " \"ordered\": {\"$numberLong\": \"60\"}, \"date\": {\"$date\": \"2018-02-06T20:11:13.666Z\"}}",
            "{\"_id\": \"112233445566778899aabb07\", \"item\": \"item7\", \"price\": 10," +
            " \"ordered\": {\"$numberLong\": \"60\"}, \"date\": {\"$date\": \"2018-02-09T11:11:11.777Z\"}}"
        };

        private static List<string> InventoryObjects = new List<string>()
        {
            "{\"sku\": \"item1\", \"description\": \"product 1\", \"instock\": {\"$numberLong\": \"120\"}}",
            "{\"sku\": \"item9\", \"description\": \"product 2\", \"instock\": {\"$numberLong\": \"80\"}}",
            "{\"sku\": \"item3\", \"description\": \"product 3\", \"instock\": {\"$numberLong\": \"60\"}}",
            "{\"sku\": \"item2\", \"description\": \"product 4\", \"instock\": {\"$numberLong\": \"70\"}}",
            "{\"sku\": \"item4\", \"description\": \"product 5\", \"instock\": {\"$numberLong\": \"90\"}}",
            "{\"sku\": \"item5\", \"description\": \"product 6\", \"instock\": {\"$numberLong\": \"100\"}}",
            "{\"sku\": \"item7\", \"description\": \"product 7\", \"instock\": {\"$numberLong\": \"110\"}}",
            "{\"sku\": \"item6\", \"description\": \"product 8\", \"instock\": {\"$numberLong\": \"50\"}}",
            "{\"sku\": \"item8\", \"description\": \"product 9\", \"instock\": {\"$numberLong\": \"40\"}}",
            "{\"sku\": null, \"description\": \"Incomplete\" }"
        };

        private static List<string> WarehousesObjects = new List<string>()
        {
            "{\"stock_item\": \"item1\", \"warehouse\": \"A\", \"instock\": {\"$numberLong\": \"120\"}}",
            "{\"stock_item\": \"item2\", \"warehouse\": \"A\", \"instock\": {\"$numberLong\": \"10\"}}",
            "{\"stock_item\": \"item3\", \"warehouse\": \"A\", \"instock\": {\"$numberLong\": \"20\"}}",
            "{\"stock_item\": \"item4\", \"warehouse\": \"A\", \"instock\": {\"$numberLong\": \"30\"}}",
            "{\"stock_item\": \"item5\", \"warehouse\": \"A\", \"instock\": {\"$numberLong\": \"40\"}}",
            "{\"stock_item\": \"item6\", \"warehouse\": \"A\", \"instock\": {\"$numberLong\": \"80\"}}",
            "{\"stock_item\": \"item7\", \"warehouse\": \"A\", \"instock\": {\"$numberLong\": \"50\"}}",
            "{\"stock_item\": \"item1\", \"warehouse\": \"B\", \"instock\": {\"$numberLong\": \"120\"}}",
            "{\"stock_item\": \"item2\", \"warehouse\": \"B\", \"instock\": {\"$numberLong\": \"20\"}}",
            "{\"stock_item\": \"item3\", \"warehouse\": \"B\", \"instock\": {\"$numberLong\": \"30\"}}",
            "{\"stock_item\": \"item4\", \"warehouse\": \"B\", \"instock\": {\"$numberLong\": \"40\"}}",
            "{\"stock_item\": \"item5\", \"warehouse\": \"B\", \"instock\": {\"$numberLong\": \"40\"}}",
            "{\"stock_item\": \"item6\", \"warehouse\": \"B\", \"instock\": {\"$numberLong\": \"70\"}}",
            "{\"stock_item\": \"item7\", \"warehouse\": \"B\", \"instock\": {\"$numberLong\": \"70\"}}"
        };

        private static string PipelineGroupBase = "[" +
                "{" +
                "  \"$group\": {" +
                "    \"_id\": {\"month\": {\"$month\": \"$date\"}, \"day\": {\"$dayOfMonth\": \"$date\"}, \"year\": {\"$year\": \"$date\"}}," +
                "    \"totalPrice\": {\"$sum\": {\"$multiply\": [\"$price\", \"$ordered\"]}}," +
                "    \"averageQuantity\": {\"$avg\": \"$ordered\"}," +
                "    \"count\": {\"$sum\": 1}" +
                "  }" +
                "}," +
                "{" +
                "  \"$sort\": {\"_id.year\": 1, \"_id.month\": 1, \"_id.day\": 1}" +
                "}]";

        private static string PipelineLookupForeignFieldBase = "[" +
                "{" +
                "  \"$lookup\": {" +
                "    \"from\": \"inventory\"," +
                "    \"localField\": \"item\"," +
                "    \"foreignField\": \"sku\"," +
                "    \"as\": \"inventory_docs\"" +
                "  }" +
                "}," +
                "{" +
                "  \"$sort\": {\"item\": 1}" +
                "}]";

        private static string PipelineLookupPipelineBase = "[" +
                "{" +
                "  \"$lookup\": {" +
                "    \"from\": \"warehouses\"," +
                "    \"let\": {\"order_item\": \"$item\", \"order_qty\":\"$ordered\"}," +
                "    \"pipeline\": [" +
                "      {\"$match\":" +
                "        {\"$expr\":" +
                "          {\"$and\": [" +
                "            {\"$eq\": [\"$stock_item\", \"$$order_item\"]}," +
                "            {\"$gte\": [\"$instock\", \"$$order_qty\"]}" +
                "          ]}" +
                "        }" +
                "      }," +
                "      {\"$project\": {\"warehouse\": 1, \"instock\": 1, \"_id\": 0}}," +
                "      {\"$sort\": {\"instock\": 1}}" +
                "    ]," +
                "    \"as\": \"stockdata\"" +
                "  }" +
                "}," +
                "{" +
                "  \"$sort\": {\"item\": 1}" +
                "}]";


        /// <summary>
        /// NbObjectAggregationIT 開始時に１度だけ実行
        /// </summary>
        [SetUp]
        public void TestFixtureSetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineUser().Wait();

            _orderBucket = new NbObjectBucket<NbObject>(BucketOrders);
            _inventoryBucket = new NbObjectBucket<NbObject>(BucketInventory);
            _warehousesBucket = new NbObjectBucket<NbObject>(BucketWarehouses);
            InitAggregationObjectStorage().Wait();

            ITUtil.SignUpAndLogin().Wait();

            _service = NbService.Singleton;
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// Aggregate 実行($lookupなし)
        /// </summary>
        [Test]
        public async void TestAggregateNormalLookupNone()
        {
            var pipeline = NbJsonArray.Parse(PipelineGroupBase);

            // test
            await AggregateTest(_orderBucket, pipeline);

            Assert.AreEqual(5, _aggregateResult.Count);
            AssertGroupStage(_aggregateResult.GetJsonObject(0), 2018, 1, 1, 44, 1.5, 2);
            AssertGroupStage(_aggregateResult.GetJsonObject(1), 2018, 1, 15, 20, 1.0, 1);
            AssertGroupStage(_aggregateResult.GetJsonObject(2), 2018, 2, 4, 1700, 42.5, 2);
            AssertGroupStage(_aggregateResult.GetJsonObject(3), 2018, 2, 6, 600, 60.0, 1);
            AssertGroupStage(_aggregateResult.GetJsonObject(4), 2018, 2, 9, 600, 60.0, 1);
        }

        /// <summary>
        /// Aggregate 実行($sort順変更)
        /// </summary>
        [Test]
        public async void TestAggregateNormalSortMultiple()
        {
            var pipeline = NbJsonArray.Parse(PipelineGroupBase);
            pipeline.RemoveAt(1);
            var sortParam = new NbJsonObject();
            sortParam.Add("_id.month", -1);
            sortParam.Add("totalPrice", -1);
            sortParam.Add("_id.year", 1);
            sortParam.Add("averageQuantity", 1);
            sortParam.Add("_id.day", 1);
            sortParam.Add("count", -1);
            var sort = new NbJsonObject();
            sort.Add("$sort", sortParam);
            pipeline.Add(sort);

            // test
            await AggregateTest(_orderBucket, pipeline);

            Assert.AreEqual(5, _aggregateResult.Count);
            AssertGroupStage(_aggregateResult.GetJsonObject(0), 2018, 2, 4, 1700, 42.5, 2);
            AssertGroupStage(_aggregateResult.GetJsonObject(1), 2018, 2, 6, 600, 60.0, 1);
            AssertGroupStage(_aggregateResult.GetJsonObject(2), 2018, 2, 9, 600, 60.0, 1);
            AssertGroupStage(_aggregateResult.GetJsonObject(3), 2018, 1, 1, 44, 1.5, 2);
            AssertGroupStage(_aggregateResult.GetJsonObject(4), 2018, 1, 15, 20, 1.0, 1);
        }

        /// <summary>
        /// Aggregate 実行($lookup(foreignField))
        /// </summary>
        [Test]
        public async void TestAggregateNormalLookupForeignField()
        {
            var pipeline = NbJsonArray.Parse(PipelineLookupForeignFieldBase);
            var options = new NbJsonObject();

            // test
            await AggregateTest(_orderBucket, pipeline, options);

            Assert.AreEqual(7, _aggregateResult.Count);
            AssertLookupInventory(_aggregateResult.GetJsonObject(0), "item1", "2018-01-01T08:00:00.111Z", "product 1", 120);
            AssertLookupInventory(_aggregateResult.GetJsonObject(1), "item2", "2018-01-01T09:00:00.222Z", "product 4", 70);
            AssertLookupInventory(_aggregateResult.GetJsonObject(2), "item3", "2018-01-15T09:00:00.333Z", "product 3", 60);
            AssertLookupInventory(_aggregateResult.GetJsonObject(3), "item4", "2018-02-04T11:21:39.444Z", "product 5", 90);
            AssertLookupInventory(_aggregateResult.GetJsonObject(4), "item5", "2018-02-04T21:23:13.555Z", "product 6", 100);
            AssertLookupInventory(_aggregateResult.GetJsonObject(5), "item6", "2018-02-06T20:11:13.666Z", "product 8", 50);
            AssertLookupInventory(_aggregateResult.GetJsonObject(6), "item7", "2018-02-09T11:11:11.777Z", "product 7", 110);
        }

        /// <summary>
        /// Aggregate 実行($lookup(pipeline))
        /// </summary>
        [Test]
        public async void TestAggregateNormalLookupPipeline()
        {
            var pipeline = NbJsonArray.Parse(PipelineLookupPipelineBase);
            var options = new NbJsonObject();
            options.Add("allowDiskUse", true);
            options.Add("maxTimeMS", 1000);
            options.Add("batchSize", 10);

            // test
            await AggregateTest(_orderBucket, pipeline, options);

            Assert.AreEqual(7, _aggregateResult.Count);
            NbJsonArray stockdata = NbJsonArray.Parse("[{\"warehouse\":\"A\", \"instock\":120}, {\"warehouse\":\"B\", \"instock\":120}]");
            AssertLookupWarehouse(_aggregateResult.GetJsonObject(0), "item1", stockdata);
            stockdata = NbJsonArray.Parse("[{\"warehouse\":\"A\", \"instock\":10}, {\"warehouse\":\"B\", \"instock\":20}]");
            AssertLookupWarehouse(_aggregateResult.GetJsonObject(1), "item2", stockdata);
            stockdata = NbJsonArray.Parse("[{\"warehouse\":\"A\", \"instock\":20}, {\"warehouse\":\"B\", \"instock\":30}]");
            AssertLookupWarehouse(_aggregateResult.GetJsonObject(2), "item3", stockdata);
            stockdata = NbJsonArray.Parse("[{\"warehouse\":\"B\", \"instock\":40}]");
            AssertLookupWarehouse(_aggregateResult.GetJsonObject(3), "item4", stockdata);
            stockdata = NbJsonArray.Parse("[]");
            AssertLookupWarehouse(_aggregateResult.GetJsonObject(4), "item5", stockdata);
            stockdata = NbJsonArray.Parse("[{\"warehouse\":\"B\", \"instock\":70}, {\"warehouse\":\"A\", \"instock\":80}]");
            AssertLookupWarehouse(_aggregateResult.GetJsonObject(5), "item6", stockdata);
            stockdata = NbJsonArray.Parse("[{\"warehouse\":\"B\", \"instock\":70}]");
            AssertLookupWarehouse(_aggregateResult.GetJsonObject(6), "item7", stockdata);
        }

        /// <summary>
        /// Aggregate 実行: pipeline null
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestAggregatePipelineNull()
        {
            // test
            await _orderBucket.AggregateAsync(null, null);
        }

        /// <summary>
        /// Aggregate 実行: options 不正
        /// </summary>
        [Test]
        public async void TestAggregateInvalidOptions()
        {
            var pipeline = NbJsonArray.Parse(PipelineLookupPipelineBase);
            var options = new NbJsonObject();
            options.Add("allowDiskUse", NbJsonParser.Parse("{\"value\": true}"));

            // test
            await AggregateTest(_orderBucket, pipeline, options, HttpStatusCode.BadRequest);
        }

        //-----------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// Aggregation用のオブジェクトストレージデータ初期化<br/>
        /// バケットの作成とバケット内のデータ全削除を行い、オブジェクトを登録する
        /// </summary>
        /// <returns>成否</returns>
        public static async Task<bool> InitAggregationObjectStorage()
        {
            var ret = true;

            ITUtil.UseMasterKey();

            ret &= await ITUtil.CreateBucket(NbBucketManager.BucketType.Object, BucketOrders, "IT Bucket for Aggregation");
            ret &= await ITUtil.CreateBucket(NbBucketManager.BucketType.Object, BucketInventory, "IT Bucket for Aggregation");
            ret &= await ITUtil.CreateBucket(NbBucketManager.BucketType.Object, BucketWarehouses, "IT Bucket for Aggregation");
            ret &= await ITUtil.DeleteAllObjects(BucketOrders);
            ret &= await ITUtil.DeleteAllObjects(BucketInventory);
            ret &= await ITUtil.DeleteAllObjects(BucketWarehouses);
            await CreateObjects(_orderBucket, OrdersObjects);
            await CreateObjects(_inventoryBucket, InventoryObjects);
            await CreateObjects(_warehousesBucket, WarehousesObjects);

            ITUtil.UseNormalKey();

            return ret;
        }

        /// <summary>
        /// オブジェクトを登録する
        /// </summary>
        /// <param name="jsonList">オブジェクト(JSON文字列)リスト</param>
        /// <returns>Task</returns>
        public static async Task CreateObjects(NbObjectBucket<NbObject> bucket, List<string> jsonList)
        {
            var req = new NbBatchRequest();
            foreach (var json in jsonList)
            {
                var obj = new NbObject(bucket.BucketName, NbJsonParser.Parse(json));
                req.AddInsertRequest(obj);
            }

            await bucket.BatchAsync(req);
        }

        /// <summary>
        /// Aggregateテスト
        /// </summary>
        /// <param name="pipeline">Aggregation Pipeline JSON配列</param>
        /// <param name="options">オプション</param>
        /// <returns>Task</returns>
        private async Task AggregateTest(NbObjectBucket<NbObject> bucket, NbJsonArray pipeline, NbJsonObject options = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            try
            {
                _aggregateResult = await bucket.AggregateAsync(pipeline, options);
                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }
            }
            catch (NbHttpException e)
            {
                if (expectedStatusCode == HttpStatusCode.OK)
                {
                    Assert.Fail("Bad route");
                }

                Assert.AreEqual(expectedStatusCode, e.StatusCode);
                Assert.NotNull(ITUtil.GetErrorInfo(e.Response));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Assert.Fail("Bad route");
            }
        }

        /// <summary>
        /// $group の結果確認
        /// </summary>
        /// <param name="doc">確認対象ドキュメント</param>
        /// <param name="year">year</param>
        /// <param name="month">month</param>
        /// <param name="day">day</param>
        /// <param name="totalPrice">totalPrice</param>
        /// <param name="average">average</param>
        /// <param name="count">count</param>
        private void AssertGroupStage(NbJsonObject doc, int year, int month, int day, int totalPrice, double average, int count)
        {
            Assert.AreEqual(4, doc.Count);
            Assert.AreEqual(totalPrice, doc.Get<int>("totalPrice"));
            Assert.AreEqual(average, doc.Get<double>("averageQuantity"));
            Assert.AreEqual(count, doc.Get<int>("count"));

            NbJsonObject id = doc.GetJsonObject("_id");
            Assert.AreEqual(3, id.Count);
            Assert.AreEqual(year, id.Get<int>("year"));
            Assert.AreEqual(month, id.Get<int>("month"));
            Assert.AreEqual(day, id.Get<int>("day"));
        }

        /// <summary>
        /// $lookup(foreignField) の結果確認
        /// </summary>
        /// <param name="doc">確認対象ドキュメント</param>
        /// <param name="item">item</param>
        /// <param name="date">date</param>
        /// <param name="desc">description</param>
        /// <param name="instock">instock</param>
        private void AssertLookupInventory(NbJsonObject doc, String item, String date, String desc, int instock)
        {
            Assert.AreEqual(10, doc.Count);
            Assert.AreEqual(item, doc.Get<string>("item"));
            NbJsonObject dateObj = doc.GetJsonObject("date");
            Assert.AreEqual(1, dateObj.Count);
            Assert.AreEqual(date, dateObj.Get<string>("$date"));

            NbJsonArray inventoryDocs = doc.GetArray("inventory_docs");
            Assert.AreEqual(1, inventoryDocs.Count);
            NbJsonObject inventory = inventoryDocs.GetJsonObject(0);
            Assert.AreEqual(8, inventory.Count);
            Assert.IsTrue(inventory.ContainsKey("_id"));
            Assert.AreEqual(item, inventory.Get<string>("sku"));
            Assert.AreEqual(desc, inventory.Get<string>("description"));
            Assert.AreEqual(instock, inventory.Get<int>("instock"));
        }

        /// <summary>
        /// $lookup(pipeline) の結果確認
        /// </summary>
        /// <param name="doc">確認対象ドキュメント</param>
        /// <param name="item">item</param>
        /// <param name="warehouse">warehouse</param>
        private void AssertLookupWarehouse(NbJsonObject doc, String item, NbJsonArray warehouse)
        {
            Assert.AreEqual(10, doc.Count);
            Assert.AreEqual(item, doc.Get<string>("item"));

            NbJsonArray stockdata = doc.GetArray("stockdata");
            Assert.AreEqual(stockdata.Count, warehouse.Count);

            for (int i = 0; i < warehouse.Count; i++) {
                Assert.AreEqual(stockdata.GetJsonObject(i), warehouse.GetJsonObject(i));
            }
        }
    }
}
