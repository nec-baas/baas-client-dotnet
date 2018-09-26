using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbJsonObjectTest
    {
        /**
        * Parse
        **/
        /// <summary>
        /// Parse（正常）
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormal1()
        {
            var jsonString1 = "{a:1, b:'txt', c:{c1:1, c2:2, c3:[1,2,3]}}";
            var jsonString2 = "{\"a\":1,\"b\":\"txt\",\"c\":{\"c1\":1,\"c2\":2,\"c3\":[1,2,3]}}";

            var json1 = NbJsonObject.Parse(jsonString1);
            var json2 = NbJsonObject.Parse(jsonString2);

            var expectedJson = new NbJsonObject
            {
                {"a", 1},
                {"b", "txt"},
                {"c", new NbJsonObject {
                    {"c1", 1},
                    {"c2", 2},
                    {"c3", new int[] {1, 2, 3}}
                }}
            };

            Assert.AreEqual(expectedJson, json1);
            Assert.AreEqual(json1, json2);
            //Assert.AreEqual(3, json.Count);
            //Assert.AreEqual(1, json["a"]);
            //Assert.AreEqual("txt", json["b"]);
            //var cJson = json.GetJsonObject("c");
            //Assert.AreEqual(1, cJson["c1"]);
            //Assert.AreEqual(2, cJson["c2"]);
            //Assert.AreEqual(new int[] {1, 2, 3}, cJson["c3"]);
        }

        /// <summary>
        /// Parse（正常）
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormal2()
        {
            var jsonString = "{\"a\": 1, \"b\": [1, 2, 3, 4, 5], \"c\": {\"d\": \"abc\"}, \"e\": true, \"f\": false, \"g\": null}";

            var json = NbJsonObject.Parse(jsonString);

            Assert.AreEqual(6, json.Count);
            Assert.AreEqual(1, json["a"]);
            Assert.True(json["b"] is NbJsonArray);
            Assert.True(json["c"] is NbJsonObject);
            Assert.True((bool)json["e"]);
            Assert.False((bool)json["f"]);
            Assert.IsNull(json["g"]);
        }

        public static IEnumerable JsonTestAbnormalArg()
        {
            yield return new TestCaseData(";l;:sdflg:s;dfgl").Returns(0);
        }

        /// <summary>
        /// Parse（パース失敗）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, TestCaseSource("JsonTestAbnormalArg"), ExpectedException(typeof(ArgumentException))]
        public int TestParseExceptionIllegaldata(string jsonString)
        {
            var json = NbJsonObject.Parse(jsonString);
            return 0;
        }

        // Parse()の詳細なUTはNbJsonParserTest側にて実施する

        /**
        * ToString
        **/
        /// <summary>
        /// ToString（正常）
        /// JSON文字列に正しく変換されること
        /// </summary>
        [Test]
        public void TestToStringNormal()
        {
            var json = new NbJsonObject
            {
                {"a", 1},
                {"b", "txt"},
                {"c", new NbJsonObject {
                    {"c1", 1},
                    {"c2", 2},
                    {"c3", new int[] {1, 2, 3}}
                }}
            };

            var s = json.ToString();

            // stringで比較すると、Failになるため、JSONで比較
            Assert.AreEqual(json, NbJsonObject.Parse(s));
        }

        /// <summary>
        /// ToString（空のJson）
        /// JSON文字列に正しく変換されること
        /// </summary>
        [Test]
        public void TestToStringNormalEmpty()
        {
            var json = new NbJsonObject();

            var s = json.ToString();

            Assert.AreEqual("{}", s);
        }

        /**
        * Get
        **/
        /// <summary>
        /// Get（string）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalString()
        {
            var o = new NbJsonObject()
            {
                {"a", "b"}
            };

            Assert.AreEqual("b", o.Get<string>("a"));
        }

        /// <summary>
        /// Get（int）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalInt()
        {
            var o = new NbJsonObject()
            {
                {"a", 3}
            };

            Assert.AreEqual(3, o.Get<int>("a"));
        }

        /// <summary>
        /// Get（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetExceptionKeyNull()
        {
            var o = new NbJsonObject()
            {
                {"a", "test"}
            };

            var v = o.Get<string>(null);
        }

        /// <summary>
        /// Get（null）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalNulll()
        {
            var o = new NbJsonObject()
            {
                {"a", null}
            };

            Assert.AreEqual(0, o.Get<int>("a"));
            Assert.AreEqual(null, o.Get<string>("a"));
        }

        /// <summary>
        /// Get（キーが存在しない）
        /// KeyNotFoundExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(KeyNotFoundException))]
        public void TestGetExceptionKeyNotFound()
        {
            var o = new NbJsonObject()
            {
                {"a", "b"}
            };

            var v = o.Get<int>("c");
        }

        /// <summary>
        /// Get（型が違う）
        /// InvalidCastExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetExceptionInvalidType()
        {
            var o = new NbJsonObject()
            {
                {"a", "b"}
            };

            var v = o.Get<int>("a");
        }

        /**
        * Opt
        **/
        /// <summary>
        /// Opt（string）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestOptNormalString()
        {
            var o = new NbJsonObject()
            {
                {"a", "test"}
            };

            var v = o.Opt<string>("a", null);
            Assert.AreEqual("test", v);
        }

        /// <summary>
        /// Opt（int）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestOptNormalInt()
        {
            var o = new NbJsonObject()
            {
                {"a", 1}
            };

            var v = o.Opt<int>("a", 0);
            Assert.AreEqual(1, v);
        }

        /// <summary>
        /// Opt（null）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestOptNormalNulll()
        {
            var o = new NbJsonObject()
            {
                {"a", null}
            };

            Assert.AreEqual(0, o.Opt<int>("a", 1));
            Assert.AreEqual(null, o.Opt<string>("a", "abc"));
        }

        /// <summary>
        /// Opt（キーが存在しない）
        /// デフォルト値を取得できること
        /// </summary>
        [Test]
        public void TestOptSubnormalKeyNotFound()
        {
            var o = new NbJsonObject()
            {
                {"a", "test"}
            };

            var v = o.Opt<int>("c", 1);
            Assert.AreEqual(1, v);
        }

        /// <summary>
        /// Opt（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOptExceptionKeyNull()
        {
            var o = new NbJsonObject()
            {
                {"a", "test"}
            };

            var value = o.Opt<int>(null, 1);
        }

        /// <summary>
        /// Opt（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestOptExceptionInvalidType()
        {
            var o = new NbJsonObject()
            {
                {"a", 1}
            };

            var v = o.Opt<string>("a", null);
        }

        /**
        * GetJsonObject
        **/
        /// <summary>
        /// GetJsonObject（正常）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestGetJsonObjectNormal()
        {
            var aJson = new NbJsonObject()
            {
                {"b", 1},
                {"c", "test"}
            };
            var o = new NbJsonObject()
            {
                {"a", aJson}
            };

            var v = o.GetJsonObject("a");
            Assert.AreEqual(aJson, v);
        }

        /// <summary>
        /// GetJsonObject（キーが存在しない）
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestGetJsonObjectSubnormalKeyNotFound()
        {
            var aJson = new NbJsonObject()
            {
                {"b", 1},
                {"c", "test"}
            };
            var o = new NbJsonObject()
            {
                {"a", aJson}
            };

            var v = o.GetJsonObject("b");
            Assert.IsNull(v);
        }

        /// <summary>
        /// GetJsonObject（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetJsonObjectExceptionKeyNull()
        {
            var aJson = new NbJsonObject()
            {
                {"b", 1},
                {"c", "test"}
            };
            var o = new NbJsonObject()
            {
                {"a", aJson}
            };

            var v = o.GetJsonObject(null);
        }

        /// <summary>
        /// GetJsonObject（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetJsonObjectExceptionInvalidType()
        {
            var o = new NbJsonObject()
            {
                {"a", 1}
            };

            var v = o.GetJsonObject("a");
        }

        /**
        * GetEnumerable
        **/
        /// <summary>
        /// GetEnumerable（正常）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestGetEnumerableNormal()
        {
            var aValue = new HashSet<string>();
            aValue.Add("b");
            aValue.Add("c");

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetEnumerable("a");
            Assert.AreEqual(aValue, v);
        }

        /// <summary>
        /// GetEnumerable（キーが存在しない）
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestGetEnumerableSubnormalKeyNotFound()
        {
            var aValue = new HashSet<string>();
            aValue.Add("b");
            aValue.Add("c");

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetEnumerable("d");
            Assert.IsNull(v);
        }

        /// <summary>
        /// GetEnumerable（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetEnumerableExceptionKeyNull()
        {
            var aValue = new HashSet<string>();
            aValue.Add("b");
            aValue.Add("c");

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetEnumerable(null);
        }

        /// <summary>
        /// GetEnumerable（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetEnumerableExceptionInvalidType()
        {
            var o = new NbJsonObject()
            {
                {"a", "b"}
            };

            var v = o.GetEnumerable("a");
        }

        /**
        * GetArray
        **/
        /// <summary>
        /// GetArray（正常）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestGetArrayNormal1()
        {
            var aValue = new NbJsonArray
            {
                new NbJsonObject {{"b", "test"}},
                new NbJsonObject {{"c", 1}}
            };

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetArray("a");
            Assert.AreEqual(aValue, v);
        }

        /// <summary>
        /// GetArray（正常）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestGetArrayNormal2()
        {
            var jsonString = "{a:[1,2,3]}";

            var json = NbJsonObject.Parse(jsonString);

            var array = json.GetArray("a");
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(2, array[1]);
            Assert.AreEqual(3, array[2]);
        }

        /// <summary>
        /// GetArray（キーが存在しない）
        /// nullが取得できること
        /// </summary>
        [Test]
        public void TestGetArraySubnormalKeyNotFound()
        {
            var aValue = new NbJsonArray
            {
                new NbJsonObject {{"b", "test"}},
                new NbJsonObject {{"c", 1}}
            };

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetArray("d");
            Assert.IsNull(v);
        }

        /// <summary>
        /// GetArray（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetArrayExceptionKeyNull()
        {
            var aValue = new NbJsonArray
            {
                new NbJsonObject {{"b", "test"}},
                new NbJsonObject {{"c", 1}}
            };

            var o = new NbJsonObject()
            {
                {"a", aValue}
            };

            var v = o.GetArray(null);
        }

        /// <summary>
        /// GetArray（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetArrayExceptionInvalidType()
        {
            var o = new NbJsonObject()
            {
                {"a", "b"}
            };

            var v = o.GetArray("a");
        }

        /// <summary>
        /// 自動型変換テスト
        /// </summary>
        [Test]
        public void TestTypeConversionNormal()
        {
            var json = new NbJsonObject
            {
                {"int", 100},
                {"long", 100L},
                {"double", 100.123456},
                {"bool", true},
                {"string", "100"}
            };

            Assert.True(json.Get<bool>("bool"));
            Assert.AreEqual("100", json.Get<string>("string"));

            // 拡大方向
            Assert.AreEqual(100L, json.Get<long>("int"));
            Assert.AreEqual(100.0, json.Get<double>("long"));

            // 縮小方向
            Assert.AreEqual(100, json.Get<int>("long"));
            Assert.AreEqual(100L, json.Get<long>("double"));

            try
            {
                json.Get<int>("bool");
                Assert.Fail("no exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }

            try
            {
                json.Get<int>("string");
                Assert.Fail("no exception");
            }
            catch (InvalidCastException)
            {
                // ok
            }
        }

        /// <summary>
        /// Merge 重複キーがない場合正常にマージできること
        /// </summary>
        [Test]
        public void TestMergeNormal()
        {
            var obj1 = new NbJsonObject
            {
                {"key1", 100},
                {"key2", "value"},
            };

            var obj2 = new NbJsonObject
            {
                {"key3", null},
                {"key4", new NbJsonObject {{"child", 1.23}}}
            };

            obj1.Merge(obj2);

            Assert.AreEqual(obj1.Count, 4);
            Assert.AreEqual(obj1["key1"], 100);
            Assert.AreEqual(obj1["key2"], "value");
            Assert.AreEqual(obj1["key3"], null);
            Assert.AreEqual(obj1["key4"], new NbJsonObject {{ "child", 1.23 }});
        }

        /// <summary>
        /// Merge 重複キーがある場合上書きされること
        /// </summary>
        [Test]
        public void TestMergeOverwrite()
        {
            var obj1 = new NbJsonObject
            {
                {"key1", 100},
                {"key2", null},
            };

            var obj2 = new NbJsonObject
            {
                {"key2", false},
                {"key3", new NbJsonObject {{"child", 1.23}}}
            };

            obj1.Merge(obj2);

            Assert.AreEqual(obj1.Count, 3);
            Assert.AreEqual(obj1["key1"], 100);
            Assert.AreEqual(obj1["key2"], false);
            Assert.AreEqual(obj1["key3"], new NbJsonObject { { "child", 1.23 } });
        }

        /// <summary>
        /// Merge 空のオブジェクトがマージされても変化ないこと
        /// </summary>
        [Test]
        public void TestMergeFromEmpty()
        {
            var obj1 = new NbJsonObject
            {
                {"key1", 100},
                {"key2", "value"},
            };

            var obj2 = new NbJsonObject();

            obj1.Merge(obj2);

            Assert.AreEqual(obj1.Count, 2);
            Assert.AreEqual(obj1["key1"], 100);
            Assert.AreEqual(obj1["key2"], "value");
        }

        /// <summary>
        /// Merge 空のオブジェクトにマージできること
        /// </summary>
        [Test]
        public void TestMergeToEmpty()
        {
            var obj1 = new NbJsonObject();

            var obj2 = new NbJsonObject
            {
                { "key2", null},
                { "key3", new NbJsonObject { { "child", 1.23 } }}
            };

            obj1.Merge(obj2);

            Assert.AreEqual(obj1.Count, 2);
            Assert.AreEqual(obj1["key2"], null);
            Assert.AreEqual(obj1["key3"], new NbJsonObject { { "child", 1.23 } });
        }

        /// <summary>
        /// Merge 引数が null の場合にArgumentNullExceptionとなること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestMergeNull()
        {
            var obj1 = new NbJsonObject();

            obj1.Merge(null);
        }


        /**
        * Equals/GetHashCode
        **/

        /// <summary>
        /// 一致比較テスト
        /// </summary>
        [Test]
        public void TestEquals()
        {
            var obj1 = NbJsonParser.Parse("{key1: [1, 2, 3]}");

            // 同一インスタンス
            Assert.True(obj1.Equals(obj1));

            // null テスト
            Assert.False(obj1.Equals(null));

            // type 不一致
            Assert.False(obj1.Equals(200));

            NbJsonObject that;
            that = NbJsonParser.Parse("{key1: [1, 2, 3]}");
            Assert.True(obj1.Equals(that));
            Assert.AreEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: [1, 2, 4]}");
            Assert.False(obj1.Equals(that));
            Assert.AreNotEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: [1, 2, 3], key2: 4}");
            Assert.False(obj1.Equals(that));

            that = NbJsonParser.Parse("{key1: null}");
            Assert.False(obj1.Equals(that));
        }

        /// <summary>
        /// 数値型が異なっていても値が同一なら同一とみなすこと
        /// </summary>
        [Test]
        public void TestEqualsGetHashCodeWithTypeDiffer()
        {
            var obj1 = new NbJsonObject
            {
                {"key1", 1}
            };
            var obj2 = new NbJsonObject
            {
                {"key1", 1L}
            };

            Assert.True(obj1.Equals(obj2));
            Assert.AreEqual(obj1.GetHashCode(), obj2.GetHashCode());
        }

        /// <summary>
        /// Equals
        /// 数値
        /// </summary>
        [Test]
        public void TestEqualsNormalNumeric()
        {
            var obj1 = NbJsonParser.Parse("{key1: 1}");

            // 同一インスタンス
            Assert.True(obj1.Equals(obj1));

            NbJsonObject that;
            that = NbJsonParser.Parse("{key1: 1}");
            Assert.True(obj1.Equals(that));
            Assert.AreEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: 2}");
            Assert.False(obj1.Equals(that));
            Assert.AreNotEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: 1, key2: 2}");
            Assert.False(obj1.Equals(that));
        }

        /// <summary>
        /// Equals
        /// 文字列
        /// </summary>
        [Test]
        public void TestEqualsNormalString()
        {
            var obj1 = NbJsonParser.Parse("{key1: \"1\"}");

            // 同一インスタンス
            Assert.True(obj1.Equals(obj1));

            NbJsonObject that;
            that = NbJsonParser.Parse("{key1: \"1\"}");
            Assert.True(obj1.Equals(that));
            Assert.AreEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: \"2\"}");
            Assert.False(obj1.Equals(that));
            Assert.AreNotEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: \"1\", key2: \"2\"}");
            Assert.False(obj1.Equals(that));
        }

        /// <summary>
        /// Equals
        /// 配列
        /// </summary>
        [Test]
        public void TestEqualsNormalArray()
        {
            var obj1 = NbJsonParser.Parse("{key1: [1, 2, 3]}");

            // 同一インスタンス
            Assert.True(obj1.Equals(obj1));

            NbJsonObject that;
            that = NbJsonParser.Parse("{key1: [1, 2, 3]}");
            Assert.True(obj1.Equals(that));
            Assert.AreEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: [1, 2, 4]}");
            Assert.False(obj1.Equals(that));
            Assert.AreNotEqual(obj1.GetHashCode(), that.GetHashCode());

            that = NbJsonParser.Parse("{key1: [1, 2, 3], key2: 4}");
            Assert.False(obj1.Equals(that));

            var obj2 = NbJsonParser.Parse("{key1: [\"1\", \"2\", \"3\"]}");

            // 同一インスタンス
            Assert.True(obj2.Equals(obj2));

            NbJsonObject that2;
            that2 = NbJsonParser.Parse("{key1: [\"1\", \"2\", \"3\"]}");
            Assert.True(obj2.Equals(that2));
            Assert.AreEqual(obj2.GetHashCode(), that2.GetHashCode());

            that2 = NbJsonParser.Parse("{key1: [\"1\", \"2\", \"4\"]}");
            Assert.False(obj2.Equals(that2));
            Assert.AreNotEqual(obj2.GetHashCode(), that2.GetHashCode());

            that2 = NbJsonParser.Parse("{key1: [\"1\", \"2\", \"3\"], key2: \"4\"}");
            Assert.False(obj2.Equals(that2));
        }

        /// <summary>
        /// Equals
        /// null
        /// </summary>
        [Test]
        public void TestEqualsNormalNull()
        {
            var obj1 = NbJsonParser.Parse("{key1: 1}");
            // null テスト
            Assert.False(obj1.Equals(null));

            var that = NbJsonParser.Parse("{key1: null}");
            Assert.False(obj1.Equals(that));
            Assert.AreNotEqual(obj1.GetHashCode(), that.GetHashCode());
        }
    }
}
