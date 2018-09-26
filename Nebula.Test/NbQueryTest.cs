using NUnit.Framework;
using System;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbQueryTest
    {
        NbJsonObject jsonObj;

        [SetUp]
        public void Init()
        {
            jsonObj = new NbJsonObject()
            {
                {"key", "value0"},
                {"key1", "value1"}
            };
        }

        /**
        * コンストラクタ
        **/
        /// <summary>
        /// コンストラクタ（正常）
        /// 各プロパティが初期化されること
        /// </summary>
        /// 
        [Test]
        public void TestConstructorNormal()
        {
            var q = new NbQuery();

            Assert.IsNotNull(q);
            Assert.IsEmpty(q.Conditions);
            Assert.IsNull(q.ProjectionValue);
            Assert.IsNull(q.Order);
            Assert.AreEqual(-1, q.LimitValue);
            Assert.AreEqual(-1, q.SkipValue);
            Assert.False(q.DeleteMarkValue);
        }

        /**
        * Projection、ProjectionJson
        **/
        /// <summary>
        /// Projection、ProjectionJson（初期値）
        /// ProjectionJson()でnullが返ること
        /// </summary>
        ///
        [Test]
        public void TestProjectionNormalInit()
        {
            Assert.IsNull(new NbQuery().ProjectionJson());
        }

        /// <summary>
        /// Projection、ProjectionJson（含める場合）
        /// ProjectionJson()で期待通りの値が返ること
        /// </summary>
        ///
        [Test]
        public void TestProjectionNormalInclude()
        {
            var q = new NbQuery().Projection("name");
            var json = q.ProjectionJson();

            Assert.AreEqual(1, json.Count);
            Assert.AreEqual(1, json["name"]);
        }

        /// <summary>
        /// Projection、ProjectionJson（含めない場合）
        /// ProjectionJson()で期待通りの値が返ること
        /// </summary>
        ///
        [Test]
        public void TestProjectionNormalExclude()
        {
            var q = new NbQuery().Projection("-address");
            var json = q.ProjectionJson();

            Assert.AreEqual(1, json.Count);
            Assert.AreEqual(0, json["address"]);
        }

        /// <summary>
        /// Projection、ProjectionJson（キーが-で始まる、含めない場合）
        /// ProjectionJson()で期待通りの値が返ること
        /// </summary>
        [Test]
        public void TestProjectionNormalKeyStartsWithHyphen()
        {
            var q = new NbQuery().Projection("--name");
            var json = q.ProjectionJson();

            Assert.AreEqual(1, json.Count);
            Assert.AreEqual(0, json["-name"]);
        }

        /// <summary>
        /// Projection、ProjectionJson（複数）
        /// ProjectionJson()で期待通りの値が返ること
        /// </summary>
        [Test]
        public void TestProjectionNormalMulti()
        {
            var q = new NbQuery().Projection("a", "-b", "-_id");
            var json = q.ProjectionJson();

            Assert.AreEqual(3, json.Count);
            Assert.AreEqual(1, json["a"]);
            Assert.AreEqual(0, json["b"]);
            Assert.AreEqual(0, json["_id"]);
        }

        /// <summary>
        /// Projection、ProjectionJson（projectionが空文字）
        /// ProjectionJson()で期待通りの値が返ること
        /// </summary>
        [Test]
        public void TestProjectionNormalProjectionEmpty()
        {
            var q = new NbQuery().Projection("");
            var json = q.ProjectionJson();

            Assert.AreEqual(1, json.Count);
            Assert.AreEqual(1, json[""]);
        }

        /// <summary>
        /// Projection、ProjectionJson（projectionがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestProjectionExceptionProjectionNull()
        {
            var q = new NbQuery().Projection(null);
        }

        /// <summary>
        /// Projection、ProjectionJson（projection内にnullを含む）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestProjectionExceptionContainsNull()
        {
            var q = new NbQuery().Projection("a", null, "b");
        }

        /// <summary>
        /// Projection、ProjectionJson（projectionがサイズ0）
        /// ProjectionJson()でnullが返ること
        /// </summary>
        [Test]
        public void TestProjectionExceptionEmpty()
        {
            var q = new NbQuery().Projection(new string[0]);

            Assert.IsNull(q.ProjectionJson());
        }

        /**
        * OrderBy
        **/
        /// <summary>
        /// OrderBy（正常）
        /// ソート条件には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestOrderByNormal()
        {
            var q = new NbQuery().OrderBy("key1", "-key2");

            Assert.AreEqual(new string[] { "key1", "-key2" }, q.Order);
        }

        /// <summary>
        /// OrderBy（orderが空文字）
        /// ソート条件には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestOrderByNormalOrderEmpty()
        {
            var q = new NbQuery().OrderBy("");

            Assert.AreEqual(new string[] { "" }, q.Order);
        }

        /// <summary>
        /// OrderBy（orderがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOrderByExceptionOrderNull()
        {
            var q = new NbQuery().OrderBy(null);
        }

        /// <summary>
        ///OrderBy（order内にnullを含む）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestOrderByExceptionContainsNull()
        {
            var q = new NbQuery().OrderBy(null, "key1", "-key2");
        }

        /**
        * Limit
        **/
        /// <summary>
        /// Limit（正常）
        /// 検索上限数には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestLimitNormal()
        {
            var q = new NbQuery().Limit(10);

            Assert.AreEqual(10, q.LimitValue);
        }

        /// <summary>
        /// Limit（-1）
        /// 検索上限数には指定の値が格納されること
        /// </summary>
        [Test]
        public void TestLimitNormalInfinity()
        {
            var q = new NbQuery().Limit(-1);

            Assert.AreEqual(-1, q.LimitValue);
        }

        /**
        * Skip
        **/
        /// <summary>
        /// Skip（正常）
        /// 検索スキップ数には指定の値が格納されること
        /// </summary>

        [Test]
        public void TestSkipNormal()
        {
            var q = new NbQuery().Skip(100);

            Assert.AreEqual(100, q.SkipValue);
        }

        /**
        * DeleteMark
        **/
        /// <summary>
        /// DeleteMark（正常）
        /// DeleteMarkValueには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestDeleteMarkNormal()
        {
            var q = new NbQuery().DeleteMark(true);

            Assert.True(q.DeleteMarkValue);
        }

        /**
        * ToString
        * FromJSONString
        **/
        /// <summary>
        /// ToString、FromJSONString（全てのパラメータ）
        /// string⇔NbQueryの相互変換が成功すること
        /// </summary>
        [Test]
        public void TestToStringAndFromJSONStringNormal()
        {
            var q = new NbQuery().EqualTo("a", 1).Projection("b", "c").OrderBy("d", "-e").Limit(100).Skip(200).DeleteMark(true);

            var jsonString = q.ToString();

            // stringの比較ではなく、Parseしてから検証する
            var json = NbJsonObject.Parse(jsonString);

            Assert.AreEqual(1, json.GetJsonObject("where")["a"]);
            Assert.AreEqual(new NbJsonArray() { "b", "c" }, json.GetArray("projection"));
            Assert.AreEqual(new NbJsonArray() { "d", "-e" }, json.GetArray("order"));
            Assert.AreEqual(100, json["limit"]);
            Assert.AreEqual(200, json["skip"]);
            Assert.True(json.Get<bool>("deleteMark"));

            var query = NbQuery.FromJSONString(jsonString);

            Assert.AreEqual(q.Conditions, query.Conditions);
            Assert.AreEqual(q.ProjectionValue, query.ProjectionValue);
            Assert.AreEqual(q.Order, query.Order);
            Assert.AreEqual(q.LimitValue, query.LimitValue);
            Assert.AreEqual(q.SkipValue, query.SkipValue);
            Assert.AreEqual(q.DeleteMarkValue, query.DeleteMarkValue);
        }

        /// <summary>
        /// ToString、FromJSONString（空のNbQuery）
        /// string⇔NbQueryの相互変換が成功すること
        /// </summary>
        [Test]
        public void TestToStringAndFromJSONStringNormalEmpty()
        {
            var q = new NbQuery();

            var jsonString = q.ToString();

            // stringの比較ではなく、Parseしてから検証する
            var json = NbJsonObject.Parse(jsonString);

            Assert.IsEmpty(json.GetJsonObject("where"));
            Assert.False(json.ContainsKey("projection"));
            Assert.False(json.ContainsKey("order"));
            Assert.AreEqual(-1, json["limit"]);
            Assert.AreEqual(-1, json["skip"]);
            Assert.False(json.Get<bool>("deleteMark"));

            var query = NbQuery.FromJSONString(jsonString);

            Assert.AreEqual(q.Conditions, query.Conditions);
            Assert.AreEqual(q.ProjectionValue, query.ProjectionValue);
            Assert.AreEqual(q.Order, query.Order);
            Assert.AreEqual(q.LimitValue, query.LimitValue);
            Assert.AreEqual(q.SkipValue, query.SkipValue);
            Assert.AreEqual(q.DeleteMarkValue, query.DeleteMarkValue);
        }

        /// <summary>
        /// FromJSONString（空文字）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestFromJSONStringExceptionJsonStringEmpty()
        {
            var q = NbQuery.FromJSONString("");
        }

        /// <summary>
        /// FromJSONString（空のJson）
        /// 生成されたNbQueryが正しいこと
        /// </summary>
        [Test]
        public void TestFromJSONStringNormaljsonStringEmptyJson()
        {
            var q = NbQuery.FromJSONString("{}");

            Assert.IsNotNull(q);
            Assert.IsEmpty(q.Conditions);
            Assert.IsNull(q.ProjectionValue);
            Assert.IsNull(q.Order);
            Assert.AreEqual(-1, q.LimitValue);
            Assert.AreEqual(-1, q.SkipValue);
            Assert.False(q.DeleteMarkValue);
        }

        /// <summary>
        /// FromJSONString（他のKeyを含む）
        /// JSON文字列が正しいこと
        /// </summary>
        [Test]
        public void TestFromJSONStringSubnormalContainsWrongKey()
        {
            var jsonString = "{\"where\":{\"a\":1},\"projection\":[\"b\",\"c\"],\"order\":[\"d\",\"-e\"],\"limit\":100,\"skip\":200,\"deleteMark\":true,\"testKey\":1}";

            var q = NbQuery.FromJSONString(jsonString);

            var expectedQuery = new NbQuery().EqualTo("a", 1).Projection("b", "c").OrderBy("d", "-e").Limit(100).Skip(200).DeleteMark(true);

            Assert.AreEqual(expectedQuery.Conditions, q.Conditions);
            Assert.AreEqual(expectedQuery.ProjectionValue, q.ProjectionValue);
            Assert.AreEqual(expectedQuery.Order, q.Order);
            Assert.AreEqual(expectedQuery.LimitValue, q.LimitValue);
            Assert.AreEqual(expectedQuery.SkipValue, q.SkipValue);
            Assert.AreEqual(expectedQuery.DeleteMarkValue, q.DeleteMarkValue);
        }

        // FromJSONString()はinternalメソッドのため、引数にnullを設定した場合のUTは省略する

        /**
        * Conditions
        **/
        /// <summary>
        /// Conditions（正常）
        /// 検索条件のJSONオブジェクト表記を設定・取得できること
        /// </summary>
        [Test]
        public void TestConditionsNormal()
        {
            var q = new NbQuery().LessThan("key", 2);
            Assert.IsNotEmpty(q.Conditions);

            q.Conditions = new NbJsonObject();
            Assert.IsEmpty(q.Conditions);
        }

        /// <summary>
        /// Conditions（nullを設定）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConditionsExceptionNull()
        {
            var q = new NbQuery();

            q.Conditions = null;
        }

        /**
        * EqualTo
        * NotEquals
        * LessThan
        * LessThanOrEqual
        * GreaterThan
        * GreaterThanOrEqual
        **/
        /// <summary>
        /// EqualTo～GreaterThanOrEqual（正常）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestComparatorNormal()
        {
            var q = new NbQuery()
                .EqualTo("a", 1)
                .NotEquals("b", 2)
                .GreaterThan("c", 3)
                .GreaterThanOrEqual("d", 4)
                .LessThan("e", 5)
                .LessThanOrEqual("f", 6);

            var conditions = q.Conditions;

            Assert.AreEqual(1, conditions["a"]);
            Assert.AreEqual(2, conditions.GetJsonObject("b")["$ne"]);
            Assert.AreEqual(3, conditions.GetJsonObject("c")["$gt"]);
            Assert.AreEqual(4, conditions.GetJsonObject("d")["$gte"]);
            Assert.AreEqual(5, conditions.GetJsonObject("e")["$lt"]);
            Assert.AreEqual(6, conditions.GetJsonObject("f")["$lte"]);
        }

        /// <summary>
        /// EqualTo（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestEqualToNormalValueNull()
        {
            var q = new NbQuery().EqualTo("a", null);
            Assert.IsNull(q.Conditions["a"]);
        }

        /// <summary>
        /// EqualTo（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestEqualToExceptionKeyNull()
        {
            var q = new NbQuery().EqualTo(null, "test");
        }

        /// <summary>
        /// NotEquals（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestNotEqualsNormalValueNull()
        {
            var q = new NbQuery().NotEquals("a", null);
            Assert.IsNull(q.Conditions.GetJsonObject("a")["$ne"]);
        }

        /// <summary>
        /// NotEquals（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotEqualsExceptionKeyNull()
        {
            var q = new NbQuery().NotEquals(null, "test");
        }

        /// <summary>
        /// LessThan（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestLessThanNormalValueNull()
        {
            var q = new NbQuery().LessThan("a", null);
            Assert.IsNull(q.Conditions.GetJsonObject("a")["$lt"]);
        }

        /// <summary>
        /// LessThan（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestLessThanExceptionKeyNull()
        {
            var q = new NbQuery().LessThan(null, 200);
        }

        /// <summary>
        /// LessThanOrEqual（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestLessThanOrEqualNormalValueNull()
        {
            var q = new NbQuery().LessThanOrEqual("a", null);
            Assert.IsNull(q.Conditions.GetJsonObject("a")["$lte"]);
        }

        /// <summary>
        /// LessThanOrEqual（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestLessThanOrEqualExceptionKeyNull()
        {
            var q = new NbQuery().LessThanOrEqual(null, 10);
        }

        /// <summary>
        /// GreaterThan（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestGreaterThanNormalValueNull()
        {
            var q = new NbQuery().GreaterThan("a", null);
            Assert.IsNull(q.Conditions.GetJsonObject("a")["$gt"]);
        }

        /// <summary>
        /// GreaterThan（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGreaterThanExceptionKeyNull()
        {
            var q = new NbQuery().GreaterThan(null, 0);
        }

        /// <summary>
        /// GreaterThanOrEqual（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestGreaterThanOrEqualNormalValueNull()
        {
            var q = new NbQuery().GreaterThanOrEqual("a", null);
            Assert.IsNull(q.Conditions.GetJsonObject("a")["$gte"]);
        }

        /// <summary>
        /// GreaterThanOrEqual（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGreaterThanOrEqualExceptionKeyNull()
        {
            var q = new NbQuery().GreaterThanOrEqual(null, 99);
        }

        // AddSimpleOp()についてはNotEquals()等の他のUTで検証済

        /**
        * MergePut
        **/
        /// <summary>
        /// MergePut（異なるキー）
        /// 同一キーが存在しない場合はそのまま追加されること
        /// </summary>
        [Test]
        public void TestMergePutNormalOtherKey()
        {
            var q = new NbQuery().GreaterThan("key1", 100).LessThan("key2", 200);

            Assert.AreEqual(100, q.Conditions.GetJsonObject("key1")["$gt"]);
            Assert.AreEqual(200, q.Conditions.GetJsonObject("key2")["$lt"]);
        }

        /// <summary>
        /// MergePut（マージ）
        /// 同一キーに対する複数条件指定がマージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase1()
        {
            var q = new NbQuery().GreaterThan("key", 100).LessThan("key", 200);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(100, json["$gt"]);
            Assert.AreEqual(200, json["$lt"]);
        }

        /// <summary>
        /// MergePut（後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase2()
        {
            var q = new NbQuery().EqualTo("key", 100).EqualTo("key", 200);

            Assert.AreEqual(200, q.Conditions["key"]);
        }

        /// <summary>
        /// MergePut（後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase3()
        {
            var q = new NbQuery().EqualTo("key", 100).GreaterThan("key", 200);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
        }

        /// <summary>
        /// MergePut（後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase4()
        {
            var q = new NbQuery().GreaterThan("key", 100).EqualTo("key", 200);

            Assert.AreEqual(200, q.Conditions["key"]);
        }

        /// <summary>
        /// MergePut（後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase5()
        {
            var q = new NbQuery().GreaterThan("key", 100).GreaterThan("key", 200);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
        }

        /// <summary>
        /// MergePut（後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase6()
        {
            var q = new NbQuery().EqualTo("key", 100).EqualTo("key", 200).EqualTo("key", 300);

            Assert.AreEqual(300, q.Conditions["key"]);
        }

        /// <summary>
        /// MergePut（後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase7()
        {
            var q = new NbQuery().EqualTo("key", 100).GreaterThan("key", 200).LessThan("key", 300);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
            Assert.AreEqual(300, json["$lt"]);
        }

        /// <summary>
        /// MergePut（後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase8()
        {
            var q = new NbQuery().GreaterThan("key", 100).LessThan("key", 200).EqualTo("key", 300);

            Assert.AreEqual(300, q.Conditions["key"]);
        }

        /// <summary>
        /// MergePut（後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase9()
        {
            var q = new NbQuery().GreaterThan("key", 100).GreaterThan("key", 200).LessThan("key", 300);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
            Assert.AreEqual(300, json["$lt"]);
        }

        /// <summary>
        /// MergePut（後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase10()
        {
            var q = new NbQuery().GreaterThan("key", 100).LessThan("key", 200).GreaterThan("key", 300);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(300, json["$gt"]);
            Assert.AreEqual(200, json["$lt"]);
        }

        /// <summary>
        /// MergePut（後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyCase11()
        {
            var q = new NbQuery().LessThan("key", 100).GreaterThan("key", 200).GreaterThan("key", 300);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(300, json["$gt"]);
            Assert.AreEqual(100, json["$lt"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase1()
        {
            var q = new NbQuery().EqualTo("key", 100).EqualTo("key", 200).EqualTo("key", jsonObj);

            var json = q.Conditions.GetJsonObject("key");
            var json1 = json.GetJsonObject("$eq");
            Assert.AreEqual("value0", json1["key"]);
            Assert.AreEqual("value1", json1["key1"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase2()
        {
            var q = new NbQuery().EqualTo("key", 100).EqualTo("key", jsonObj).EqualTo("key", 300);

            Assert.AreEqual(300, q.Conditions["key"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先）
        /// 後発条件で上書きされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase3()
        {
            var q = new NbQuery().EqualTo("key", jsonObj).EqualTo("key", 200).EqualTo("key", 300);

            Assert.AreEqual(300, q.Conditions["key"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase4()
        {
            var q = new NbQuery().EqualTo("key", 100).GreaterThan("key", 200).LessThan("key", jsonObj);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
            Assert.AreEqual(jsonObj, json["$lt"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase5()
        {
            var q = new NbQuery().EqualTo("key", 100).GreaterThan("key", jsonObj).LessThan("key", 300);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(jsonObj, json["$gt"]);
            Assert.AreEqual(300, json["$lt"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase6()
        {
            var q = new NbQuery().EqualTo("key", jsonObj).GreaterThan("key", 200).LessThan("key", 300);

            var json = q.Conditions.GetJsonObject("key");
            var json1 = json.GetJsonObject("$eq");
            Assert.AreEqual("value0", json1["key"]);
            Assert.AreEqual("value1", json1["key1"]);
            Assert.AreEqual(200, json["$gt"]);
            Assert.AreEqual(300, json["$lt"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase7()
        {
            var q = new NbQuery().GreaterThan("key", 100).GreaterThan("key", 200).LessThan("key", 300).LessThan("key", jsonObj);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
            var json1 = json.GetJsonObject("$lt");
            Assert.AreEqual("value0", json1["key"]);
            Assert.AreEqual("value1", json1["key1"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase8()
        {
            var q = new NbQuery().GreaterThan("key", 100).GreaterThan("key", 200).LessThan("key", jsonObj).LessThan("key", 400);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
            Assert.AreEqual(400, json["$lt"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase9()
        {
            var q = new NbQuery().GreaterThan("key", 100).GreaterThan("key", jsonObj).LessThan("key", 300).LessThan("key", 400);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(400, json["$lt"]);
            var json1 = json.GetJsonObject("$gt");
            Assert.AreEqual("value0", json1["key"]);
            Assert.AreEqual("value1", json1["key1"]);
        }

        /// <summary>
        /// MergePut（JsonObject、後発優先＆マージ）
        /// 後発条件で上書き、マージされること
        /// </summary>
        [Test]
        public void TestMergePutNormalSameKeyJsonCase10()
        {
            var q = new NbQuery().GreaterThan("key", jsonObj).GreaterThan("key", 200).LessThan("key", 300).LessThan("key", 400);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(200, json["$gt"]);
            Assert.AreEqual(400, json["$lt"]);
        }

        /**
        * In
        * All
        **/
        /// <summary>
        /// In、All（正常）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestInAllNormal()
        {
            var q = new NbQuery().In("in", 1, 2, 3).All("all", 4, 5, 6);

            Assert.AreEqual(new object[] { 1, 2, 3 }, q.Conditions.GetJsonObject("in")["$in"]);
            Assert.AreEqual(new object[] { 4, 5, 6 }, q.Conditions.GetJsonObject("all")["$all"]);
        }

        /// <summary>
        /// In（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestInNormalValueNull()
        {
            var q = new NbQuery().In("a", null);

            Assert.IsNull(q.Conditions.GetJsonObject("a")["$in"]);
        }

        /// <summary>
        /// In（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestInExceptionKeyNull()
        {
            var q = new NbQuery().In(null, 1, 2, 3);
        }

        /// <summary>
        /// All（値がnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestAllNormalValueNull()
        {
            var q = new NbQuery().All("a", null);
            Assert.IsNull(q.Conditions.GetJsonObject("a")["$all"]);
        }

        /// <summary>
        /// All（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAllExceptionKeyNull()
        {
            var q = new NbQuery().All(null, 4, 5, 6);
        }

        /**
        * Exists
        * NotExists
        **/
        /// <summary>
        /// Exists、NotExists（正常）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestExistsAndNotExistsNormal()
        {
            var q = new NbQuery().Exists("exists").NotExists("nexists");

            Assert.AreEqual(true, q.Conditions.GetJsonObject("exists")["$exists"]);
            Assert.AreEqual(false, q.Conditions.GetJsonObject("nexists")["$exists"]);
        }

        /// <summary>
        /// Exists（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestExistsExceptionKeyNull()
        {
            var q = new NbQuery().Exists(null);
        }

        /// <summary>
        /// NotExists（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotExistsExceptionKeyNull()
        {
            var q = new NbQuery().NotExists(null);
        }

        /**
        * Regex
        **/
        /// <summary>
        /// Regex（options未設定）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestRegexNormalOptionsUnset()
        {
            var q = new NbQuery().Regex("key", ".*");

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(".*", json["$regex"]);
            Assert.False(json.ContainsKey("$options"));
        }

        /// <summary>
        /// Regex（optionsがnull）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestRegexNormalOptionsNull()
        {
            var q = new NbQuery().Regex("key", ".*", null);

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(".*", json["$regex"]);
            Assert.False(json.ContainsKey("$options"));
        }

        /// <summary>
        /// Regex（optionsあり）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestRegexNormalWithOptions()
        {
            var q = new NbQuery().Regex("key", ".*", "imsx");

            var json = q.Conditions.GetJsonObject("key");
            Assert.AreEqual(".*", json["$regex"]);
            Assert.AreEqual("imsx", json["$options"]);
        }

        /// <summary>
        /// Regex（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRegexExceptionKeyNull()
        {
            var q = new NbQuery().Regex(null, ".*");
        }

        /// <summary>
        /// Regex（regexpがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRegexExceptionRegexpNull()
        {
            var q = new NbQuery().Regex("key", null);
        }

        /**
        * Or
        **/
        /// <summary>
        /// Or（複数指定）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestOrNormalCase1()
        {
            var q = NbQuery.Or(new NbQuery().EqualTo("a", 1), new NbQuery().EqualTo("b", 2));

            var or = q.Conditions.GetArray("$or");
            Assert.AreEqual(2, or.Count);
            Assert.AreEqual(1, or.GetJsonObject(0)["a"]);
            Assert.AreEqual(2, or.GetJsonObject(1)["b"]);
        }

        /// <summary>
        /// Or（複数指定）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestOrNormalCase2()
        {
            var q = NbQuery.Or(new NbQuery().EqualTo("key", "test"), new NbQuery().GreaterThan("test", 30), new NbQuery().LessThan("test", 90));

            var and = q.Conditions.GetArray("$or");
            Assert.AreEqual(3, and.Count);
            Assert.AreEqual("test", and.GetJsonObject(0)["key"]);
            var gt = and.GetJsonObject(1)["test"] as NbJsonObject;
            Assert.AreEqual(30, gt["$gt"]);
            var lt = and.GetJsonObject(2)["test"] as NbJsonObject;
            Assert.AreEqual(90, lt["$lt"]);
        }

        /// <summary>
        /// Or（一つ指定）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestOrNormalSingle()
        {
            var q = NbQuery.Or(new NbQuery().EqualTo("a", 1));

            var or = q.Conditions.GetArray("$or");
            Assert.AreEqual(1, or.Count);
            Assert.AreEqual(1, or.GetJsonObject(0)["a"]);
        }

        /// <summary>
        /// Or（queriesがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOrExceptionQueriesNull()
        {
            var q = NbQuery.Or(null);
        }

        /// <summary>
        /// Or（queries内にnullを含む）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestOrExceptionContainsNull()
        {
            var q = NbQuery.Or(new NbQuery().EqualTo("a", 1), null, new NbQuery().EqualTo("b", 2));
        }

        /**
        * And
        **/
        /// <summary>
        /// And（複数指定）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestAndNormalCase1()
        {
            var q = NbQuery.And(new NbQuery().EqualTo("a", 1), new NbQuery().EqualTo("b", 2));

            var and = q.Conditions.GetArray("$and");
            Assert.AreEqual(2, and.Count);
            Assert.AreEqual(1, and.GetJsonObject(0)["a"]);
            Assert.AreEqual(2, and.GetJsonObject(1)["b"]);
        }

        /// <summary>
        /// And（複数指定）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestAndNormalCase2()
        {
            var q = NbQuery.And(new NbQuery().EqualTo("key", "test"), new NbQuery().GreaterThan("test", 30), new NbQuery().LessThan("test", 90));

            var and = q.Conditions.GetArray("$and");
            Assert.AreEqual(3, and.Count);
            Assert.AreEqual("test", and.GetJsonObject(0)["key"]);
            var gt = and.GetJsonObject(1)["test"] as NbJsonObject;
            Assert.AreEqual(30, gt["$gt"]);
            var lt = and.GetJsonObject(2)["test"] as NbJsonObject;
            Assert.AreEqual(90, lt["$lt"]);
        }

        /// <summary>
        /// And（一つ指定）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestAndNormalSingle()
        {
            var q = NbQuery.And(new NbQuery().EqualTo("a", 1));

            var and = q.Conditions.GetArray("$and");
            Assert.AreEqual(1, and.Count);
            Assert.AreEqual(1, and.GetJsonObject(0)["a"]);
        }

        /// <summary>
        /// And（queriesがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAndExceptionQueriesNull()
        {
            var q = NbQuery.And(null);
        }

        /// <summary>
        /// And（queries内にnullを含む）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestAndExceptionContainsNull()
        {
            var q = NbQuery.And(null, new NbQuery().EqualTo("a", 1), new NbQuery().EqualTo("b", 2));
        }

        // ConcatQueries()についてはOr()等の他のUTで検証済

        /**
        * Not
        **/
        /// <summary>
        /// Not
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestNotNormal()
        {
            var q = new NbQuery().GreaterThan("key1", 100).EqualTo("key2", 200).Not("key1");

            var json = q.Conditions.GetJsonObject("key1");
            var not = json.GetJsonObject("$not");
            Assert.AreEqual(100, not["$gt"]);
            Assert.AreEqual(200, q.Conditions["key2"]);
        }

        /// <summary>
        /// Not（2回実施）
        /// Conditionsには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestNotNormalTwice()
        {
            var q = new NbQuery().GreaterThan("key1", 100).Not("key1").Not("key1");

            var json = q.Conditions.GetJsonObject("key1");
            var not = json.GetJsonObject("$not");
            var innerNot = not.GetJsonObject("$not");
            Assert.AreEqual(100, innerNot["$gt"]);
        }

        /// <summary>
        /// Not（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotExceptionKeyNull()
        {
            var q = new NbQuery().GreaterThan("key1", 100).EqualTo("key2", 200).Not(null);
        }

        /// <summary>
        /// Not（指定したキーが存在しない）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestNotExceptionKeyNotFound()
        {
            var q = new NbQuery().GreaterThan("key1", 100).Not("key2");
        }

        /**
        * 複合テスト
        **/
        /// <summary>
        /// 複合テスト（正常）
        /// 各プロパティには指定の値が格納されること
        /// </summary>
        /// 
        [Test]
        public void TestQueryNormalCase1()
        {
            var q = new NbQuery().EqualTo("key1", "xyz").GreaterThan("key2", 100).Limit(100).Skip(200);

            Assert.AreEqual("xyz", q.Conditions["key1"]);
            Assert.AreEqual(100, q.Conditions.GetJsonObject("key2")["$gt"]);
            Assert.AreEqual(100, q.LimitValue);
            Assert.AreEqual(200, q.SkipValue);
        }

        /// <summary>
        /// 複合テスト（正常）
        /// 各プロパティには指定の値が格納されること
        /// </summary>
        /// 
        [Test]
        public void TestQueryNormalCase2()
        {
            var q = NbQuery.Or(
                new NbQuery().EqualTo("key1", "xyz"),
                new NbQuery().In("key2", "A", "B", "C")
            );

            var or = q.Conditions.GetArray("$or");
            Assert.AreEqual(2, or.Count);
            Assert.AreEqual("xyz", or.GetJsonObject(0)["key1"]);

            var key2Json = or.GetJsonObject(1)["key2"] as NbJsonObject;
            Assert.AreEqual(new object[] { "A", "B", "C" }, key2Json["$in"]);
        }
    }
}
