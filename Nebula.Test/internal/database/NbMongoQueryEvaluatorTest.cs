using Nec.Nebula.Internal.Database;
using NUnit.Framework;

namespace Nec.Nebula.Test.Internal.Database
{
    [TestFixture]
    class NbMongoQueryEvaluatorTest
    {
        private NbMongoQueryEvaluator _evaluator;
        private NbJsonObject _json;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new NbMongoQueryEvaluator();

            _json = new NbJsonObject
            {
                {"string", "abc"},
                {"integer", 12345},
                {"json", new NbJsonObject {
                    {"json1", "def"},
                    {"json3", null}
                }},
                {"array", new string[] {"a", "b", "c"}},
                {"arrayint", new NbJsonArray() {1, 2, 3}}
            };
        }


        /**
         * Evaluate
         **/

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（正常）
        /// </summary>
        [Test]
        public void TestEvaluateNormal()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", "abc" } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", 12345 } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", "a" } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "arrayint", 1 } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", "cde" } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", 34567 } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", "d" } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", "null" } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "arrayint", 4 } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "json", "abc" } }));
            var tmp = new string[] { null, "e", "f" };
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", "e" } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", "d" } }));
            var tmp2 = new NbJsonArray() { null, 1, 2 };
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", 1 } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", null } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonArray() { null, 1, 2 } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（value、conditionsが空）
        /// </summary>
        [Test]
        public void TestEvaluateNormalEmpty()
        {
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject(), new NbJsonObject()));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（Conditionsが空）
        /// </summary>
        [Test]
        public void TestEvaluateNormalEmptyConditions()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject()));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（And）
        /// </summary>
        [Test]
        public void TestEvaluateNormalLogicalOperatorAnd()
        {
            var c1 = new NbJsonObject { { "string", "abc" } };
            var c2 = new NbJsonObject { { "integer", 12345 } };
            var c3 = new NbJsonObject { { "integer", 1234567 } };
            var and = new NbJsonArray { c1, c2 };
            var and2 = new NbJsonArray { c1, c3 };

            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "$and", and } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "$and", and2 } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（Or）
        /// </summary>
        [Test]
        public void TestEvaluateNormalLogicalOperatorOr()
        {
            var c1 = new NbJsonObject { { "string", "abc" } };
            var c2 = new NbJsonObject { { "integer", 1234567 } };
            var c3 = new NbJsonObject { { "string", "def" } };
            var or = new NbJsonArray { c1, c2 };
            var or2 = new NbJsonArray { c2, c3 };

            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "$or", or } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "$or", or2 } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（Nor）
        /// </summary>
        [Test]
        public void TestEvaluateNormalLogicalOperatorNor()
        {
            var c1 = new NbJsonObject { { "string", "abc" } };
            var c2 = new NbJsonObject { { "integer", 1234567 } };
            var c3 = new NbJsonObject { { "string", "def" } };
            var nor = new NbJsonArray { c1, c2 };
            var nor2 = new NbJsonArray { c2, c3 };

            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "$nor", nor } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "$nor", nor2 } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（Not）
        /// </summary>
        [Test]
        public void TestEvaluateNormalLogicalOperatorNot()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "$not", new NbJsonObject { { "string", "def" } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "$not", new NbJsonObject { { "string", "abc" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（Other）
        /// マッチしないこと
        /// </summary>
        [Test]
        public void TestEvaluateSubNormalLogicalOperatorOther()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "$aaa", new NbJsonObject { { "string", "abc" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（キャスト失敗）
        /// マッチしないこと
        /// </summary>
        [Test]
        public void TestEvaluateSubNormalInvalidCast()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$exists", "aaa" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（Operandがnull）（Keyに対するValueがnull）
        /// マッチすること
        /// </summary>
        [Test]
        public void TestEvaluateNormalNoOperandNoValue()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "aaa", null } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（Operandがnull）
        /// マッチしないこと
        /// </summary>
        [Test]
        public void TestEvaluateSubNormalNoOperand()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", null } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（数値）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandNumeric()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", 12345 } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（配列完全一致）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandIEnumerableEquals()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", _json["array"] } }));
            var tmp = new string[] { null, "b", "c" };
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", tmp } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（配列内容一致、インスタンス違い）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandIEnumerableNotEquals()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new string[] { "a", "b", "c" } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（in）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeIn()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$in", new string[] { "b" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$in", new string[] { "d" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$in", new string[] { "a", "b", "c" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$in", new string[] { "a", "b", "c", "d" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "arrayint", new NbJsonObject { { "$in", new NbJsonArray() { 1 } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "arrayint", new NbJsonObject { { "$in", new NbJsonArray() { 4 } } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（in、Valueがnull）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeInNoValue()
        {
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$in", new string[] { "a" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$in", null } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$in", new string[] { null } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", new string[] { null } } }, new NbJsonObject { { "array", new NbJsonObject { { "$in", new string[] { null } } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（nin）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeNin()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$nin", new string[] { "b" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$nin", new string[] { "d" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$nin", new string[] { "a", "b", "c" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$nin", new string[] { "a", "b", "c", "d" } } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（nin、Valueがnull）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeNinNoValue()
        {
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$nin", new string[] { "a" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$nin", null } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（all）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeAll()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$all", new string[] { "b" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$all", new string[] { "d" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$all", new string[] { "a", "b", "c" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$all", new string[] { "a", "b", "c", "d" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "arrayint", new NbJsonObject { { "$all", new NbJsonArray() { 1, 2 } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "arrayint", new NbJsonObject { { "$all", new NbJsonArray() { 1, 2, 3, 4 } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", new NbJsonArray() { 5, 6, null } } }, new NbJsonObject { { "array", new NbJsonObject { { "$all", new NbJsonArray() { null } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", new NbJsonArray() { 5, 6, null } } }, new NbJsonObject { { "array", new NbJsonObject { { "$all", new NbJsonArray() { null, 7, 8 } } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（all、Valueがnull）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeAllNoValue()
        {
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$all", new string[] { "a", "b", "c" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$all", null } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（exists）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeExists()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$exists", true } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$exists", true } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "json", new NbJsonObject { { "$exists", true } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$exists", true } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "arrayy", new NbJsonObject { { "$exists", false } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（exists、Valueがnull）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeExistsNoValue()
        {
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$exists", true } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$exists", false } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$exists", null } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（eq）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeEq()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$eq", 12346 } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$eq", 12345 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$eq", "abd" } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$eq", "abc" } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$eq", null } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$eq", new NbJsonArray() { "e", "f" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$eq", new NbJsonArray() { "e", "f" } } } } }));
            var tmp = new string[] { null, "e", "f" };
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$eq", "e" } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$eq", new NbJsonArray() { null, "e", "f" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$eq", new NbJsonArray() { null, "e" } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$eq", new NbJsonArray() { null, "g", "h" } } } } }));
            var tmp2 = new NbJsonArray() { null, 1, 2 };
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$eq", 1 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$eq", null } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$eq", new NbJsonArray() { null, 1, 2 } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$eq", new NbJsonArray() { null, 3, 4 } } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$eq", new NbJsonArray() { 1, 2, null } } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（ne）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeNe()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$ne", 12346 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$ne", 12345 } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$ne", "abd" } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$ne", "abc" } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$ne", null } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", null } }, new NbJsonObject { { "array", new NbJsonObject { { "$ne", new NbJsonArray() { "e", "f" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$ne", new NbJsonArray() { "e", "f" } } } } }));
            var tmp = new string[] { null, "e", "f" };
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$ne", "e" } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$ne", new NbJsonArray() { null, "e", "f" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$ne", new NbJsonArray() { null, "e" } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "array", tmp } }, new NbJsonObject { { "array", new NbJsonObject { { "$ne", new NbJsonArray() { null, "g", "h" } } } } }));
            var tmp2 = new NbJsonArray() { null, 1, 2 };
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$ne", 1 } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$ne", null } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$ne", new NbJsonArray() { null, 1, 2 } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$ne", new NbJsonArray() { null, 3, 4 } } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "arrayint", tmp2 } }, new NbJsonObject { { "arrayint", new NbJsonObject { { "$ne", new NbJsonArray() { 1, 2, null } } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（ne、Valueがnull）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeNeNoValue()
        {
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "integer", null } }, new NbJsonObject { { "integer", new NbJsonObject { { "$ne", 12345 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "integer", null } }, new NbJsonObject { { "integer", new NbJsonObject { { "$ne", null } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$ne", null } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "string", "a" } }, new NbJsonObject { { "integer", new NbJsonObject { { "$ne", null } } } }));
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "string", null } }, new NbJsonObject { { "integer", new NbJsonObject { { "$ne", null } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（gt）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeGt()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$gt", 12344 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$gt", 12345 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$gt", 12346 } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（gt、Valueがnull）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeGtNoValue()
        {
            Assert.IsFalse(_evaluator.Evaluate(new NbJsonObject { { "integer", null } }, new NbJsonObject { { "integer", new NbJsonObject { { "$gt", 12345 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$gt", null } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（gte）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeGte()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$gte", 12344 } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$gte", 12345 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$gte", 12346 } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（gte、Valueがstring）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeGteString()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$gte", "a" } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$gte", "abc" } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$gte", "abcde" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（lte）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeLte()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$lte", 12346 } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$lte", 12345 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$lt", 12344 } } } }));

        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（lt）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeLt()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$lt", 12346 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$lt", 12345 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$lt", 12344 } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（lt、valueが配列）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeLtArray()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array", new NbJsonObject { { "$lt", "array.0" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（regex、valueがstring以外）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeRegexNotString()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$regex", "^1" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（regex、regexValueがnull）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeRegexNotNull()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", null } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（regex、オペランドにoptionsなし）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeRegexNoOptions()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^a" } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^A" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（regex、optionがi）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeRegexOptionsI()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^a" }, { "$options", "i" } } } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^A" }, { "$options", "i" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（regex、optionがm）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeRegexOptionsM()
        {
            Assert.IsTrue(_evaluator.Evaluate(new NbJsonObject { { "string", "aaa\nbbb\n" } }, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^b" }, { "$options", "m" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（regex、optionがs）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeRegexOptionsS()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^a" }, { "$options", "s" } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^A" }, { "$options", "s" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（regex、optionがx）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeRegexOptionsX()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$regex", "^ a" }, { "$options", "x" } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（options）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeOptions()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "string", new NbJsonObject { { "$options", null } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（not）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeNot()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$not", 12346 } } } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$not", 12345 } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（other）
        /// </summary>
        [Test]
        public void TestEvaluateNormalOperandCompositeOther()
        {
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "integer", new NbJsonObject { { "$aaa", 12346 } } } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（フォーマット違反）
        /// </summary>
        [Test]
        public void TestEvaluateSubNormalOperandFormatException()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "json.json1", "def" } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "json.json3", null } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "json.json1", "fgh" } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "json.json2", "def" } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "json.json1", 12345 } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（配列インデックス外）
        /// </summary>
        [Test]
        public void TestEvaluateSubNormalOperandArgumentOutOfRangeException()
        {
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array.0", "a" } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array.1", "b" } }));
            Assert.IsTrue(_evaluator.Evaluate(_json, new NbJsonObject { { "array.2", "c" } }));
            Assert.IsFalse(_evaluator.Evaluate(_json, new NbJsonObject { { "array.3", "d" } }));
        }

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる（キャスト失敗）
        /// </summary>
        [Test]
        public void TestEvaluateSubNormalOperandInvalidCastException()
        {
            // 配列を JSON Object アクセス
            Assert.False(_evaluator.Evaluate(_json, new NbJsonObject { { "array.array1", "a" } }));

            // JSON Object を配列アクセス
            Assert.False(_evaluator.Evaluate(_json, new NbJsonObject { { "json.1", "a" } }));
        }

        /// <summary>
        /// JSONオブジェクト直接比較テスト (#3376)
        /// </summary>
        [Test]
        public void TestEvaluateJsonObjectDirectMatch()
        {
            var data3 = new NbJsonObject {{"data3", 3}};
            var json = new NbJsonObject {{"data1", 1}, {"data2", data3}};

            var query = new NbJsonObject {{"data2", new NbJsonObject {{"$eq", data3}}}};

            Assert.True(_evaluator.Evaluate(json, query));
        }
    }
}
