using NUnit.Framework;
using System;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbJsonParserTest
    {
        // NbJsonParserのUTではTestCaseSourceは使用しない
        /**
        * Case1: jsonStringが"{a:1, b:'txt', c:{c1:1, c2:2, c3:[1,2,3]}}"
        **/
        /// <summary>
        /// Parse
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormalCase1()
        {
            var jsonString1 = "{a:1, b:'txt', c:{c1:1, c2:2, c3:[1,2,3]}}";
            var jsonString2 = "{\"a\":1,\"b\":\"txt\",\"c\":{\"c1\":1,\"c2\":2,\"c3\":[1,2,3]}}";

            var json1 = NbJsonParser.Parse(jsonString1);
            var json2 = NbJsonParser.Parse(jsonString2);

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

        // ReadValue()の分岐は上を実行することで、全て網羅できる

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase1()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{a:1, b:'txt', c:{c1:1, c2:2, c3:[1,2,3]}}");
        }

        /**
        * Case2: jsonStringが"{\"a\":[{\"b\":1}, 2, 3]}"
        **/
        /// <summary>
        /// Parse
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormalCase2()
        {
            var json = NbJsonParser.Parse("{\"a\":[{\"b\":1}, 2, 3]}");

            var expectedJson = new NbJsonObject
            {
                {"a", new NbJsonArray() {
                          new NbJsonObject() {
                            {"b", 1}
                          }, 2, 3
                      }
                }
            };

            Assert.AreEqual(expectedJson, json);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase2()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{\"a\":[{\"b\":1}, 2, 3]}");
        }

        /**
        * Case3: jsonStringが"{a:[[1,2,3],[4,5]]}"
        **/
        /// <summary>
        /// Parse
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormalCase3()
        {
            var json = NbJsonParser.Parse("{a:[[1,2,3],[4,5]]}");

            var expectedJson = new NbJsonObject
            {
                {"a", new NbJsonArray(){
                    new NbJsonArray() {
                        1, 2, 3
                        },
                    new NbJsonArray() {
                        4, 5
                        }
                    }
                }
            };

            Assert.AreEqual(expectedJson, json);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase3()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{a:[[1,2,3],[4,5]]}");
        }

        /**
        * Case4: jsonStringが"{a:[]}"
        **/
        /// <summary>
        /// Parse
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormalCase4()
        {
            var json = NbJsonParser.Parse("{a:[]}");
            var expectedJson = new NbJsonObject()
            {
                {"a", new NbJsonArray()}
            };

            Assert.AreEqual(expectedJson, json);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase4()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{a:[]}");
        }

        /**
        * Case5: jsonStringが"{null:1}"
        **/
        /// <summary>
        /// Parse
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormalCase5()
        {
            var json = NbJsonParser.Parse("{null:1}");

            var expectedJson = new NbJsonObject()
            {
                {"null", 1}
            };

            Assert.AreEqual(expectedJson, json);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase5()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{null:1}");
        }

        /**
        * Case6: jsonStringが"[1,2,3]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase6()
        {
            // ReadJsonObject()のstringチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[1,2,3]");
        }

        /// <summary>
        /// ParseArray
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase6()
        {
            var json = NbJsonParser.ParseArray("[1,2,3]");

            Assert.AreEqual(3, json.Count);
            Assert.AreEqual(1, json[0]);
            Assert.AreEqual(2, json[1]);
            Assert.AreEqual(3, json[2]);
        }

        /**
        * Case7: jsonStringが"[\"a\",\"b\",\"c\"]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase7()
        {
            // ReadJsonObject()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[\"a\",\"b\",\"c\"]");
        }

        /// <summary>
        /// ParseArray
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase7()
        {
            var json = NbJsonParser.ParseArray("[\"a\",\"b\",\"c\"]");

            Assert.AreEqual(3, json.Count);
            Assert.AreEqual("a", json[0]);
            Assert.AreEqual("b", json[1]);
            Assert.AreEqual("c", json[2]);
        }

        /**
        * Case8: jsonStringが"[\"a\",1,\"c\"]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase8()
        {
            // ReadJsonObject()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[\"a\",1,\"c\"]");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase8()
        {
            var json = NbJsonParser.ParseArray("[\"a\",1,\"c\"]");

            var expectedJson = new NbJsonArray()
            {
                "a", 1, "c"
            };

            Assert.AreEqual(expectedJson, json);
        }

        /**
        * Case9: jsonStringが"{d:\"2015-01-01T00:00:00.000Z\"}"
        **/
        /// <summary>
        /// Parse
        /// DateTimeに変換されないこと
        /// </summary>
        [Test]
        public void TestParseNormalCase9()
        {
            var json = NbJsonParser.Parse("{d:\"2015-01-01T00:00:00.000Z\"}");
            var d = json["d"];

            Assert.True(d is string);
            Assert.AreEqual("2015-01-01T00:00:00.000Z", d);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase9()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{d:\"2015-01-01T00:00:00.000Z\"}");
        }

        /**
        * Case10: jsonStringが"[\"2015-01-01T00:00:00.000Z\"]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase10()
        {
            // ReadJsonObject()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[\"2015-01-01T00:00:00.000Z\"]");
        }

        /// <summary>
        /// ParseArray
        /// DateTimeに変換されないこと
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase10()
        {
            var json = NbJsonParser.ParseArray("[\"2015-01-01T00:00:00.000Z\"]");

            Assert.IsTrue(json[0] is string);
            Assert.AreEqual("2015-01-01T00:00:00.000Z", json[0]);
        }

        /**
        * Case11: jsonStringが"{}"
        **/
        /// <summary>
        /// Parse
        /// 空のNbJsonObjectが返ること
        /// </summary>
        [Test]
        public void TestParseNormalCase11()
        {
            var json = NbJsonParser.Parse("{}");

            Assert.IsEmpty(json);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase11()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{}");
        }

        /**
        * Case12: jsonStringが"{ }"
        **/
        /// <summary>
        /// Parse
        /// 空のNbJsonObjectが返ること
        /// </summary>
        [Test]
        public void TestParseNormalCase12()
        {
            var json = NbJsonParser.Parse("{ }");

            Assert.IsEmpty(json);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase12()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{ }");
        }

        /**
        * Case13: jsonStringが"[]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase13()
        {
            // ReadJsonObject()のkey nullチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[]");
        }

        /// <summary>
        /// ParseArray
        /// 空のNbJsonArrayが返ること
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase13()
        {
            var json = NbJsonParser.ParseArray("[]");

            // 空のJSONが返る
            Assert.IsEmpty(json);
        }

        /**
        * Case14: jsonStringが"[{}]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase14()
        {
            // ReadJsonObject()のkey nullチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[{}]");
        }

        /// <summary>
        /// ParseArray
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase14()
        {
            var json = NbJsonParser.ParseArray("[{}]");
            var expectedJson = new NbJsonArray()
            {
                new NbJsonObject()
            };

            Assert.AreEqual(expectedJson, json);
        }

        /**
        * Case15: jsonStringが"[[],[]]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase15()
        {
            // ReadJsonObject()のkey nullチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[[],[]]");
        }

        /// <summary>
        /// ParseArray
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase15()
        {
            var json = NbJsonParser.ParseArray("[[],[]]");
            var expectedJson = new NbJsonArray()
            {
                    new NbJsonArray() {},
                    new NbJsonArray() {}
            };

            Assert.AreEqual(expectedJson, json);
        }

        /**
        * Case16: jsonStringが"[[[],[]]]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase16()
        {
            // ReadJsonObject()のkey nullチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[[[],[]]]");
        }

        /// <summary>
        /// ParseArray
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseArrayNormalCase16()
        {
            var json = NbJsonParser.ParseArray("[[[],[]]]");
            var expectedJson = new NbJsonArray()
            {
                new NbJsonArray() {
                    new NbJsonArray() {},
                    new NbJsonArray() {}
                }
            };

            Assert.AreEqual(expectedJson, json);
        }

        /**
        * Case17: jsonStringが"{a:1}]"
        **/
        /// <summary>
        /// Parse
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseSubnormalCase17()
        {
            // Android版を確認したところ、同じく}のところまでParseされた
            var json = NbJsonParser.Parse("{a:1}]");

            var expectedJson = new NbJsonObject()
            {
                {"a", 1}
            };

            Assert.AreEqual(expectedJson, json);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase17()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{a:1}]");
        }

        /**
        * Case18: jsonStringが"{"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase18()
        {
            // ReadJsonObject()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("{");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase18()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{");
        }

        /**
        * Case19: jsonStringが"["
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase19()
        {
            // ReadJsonObject()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase19()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("[");
        }

        /**
        * Case20: jsonStringが"{{"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase20()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("{{");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase20()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{{");
        }

        /**
        * Case21: jsonStringが"[["
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase21()
        {
            // ReadJsonObject()のkey nullチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[[");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase21()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("[[");
        }

        /**
        * Case22 jsonStringが"{["
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase22()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("{[");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase22()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{[");
        }

        /**
        * Case23: jsonStringが"[{"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase23()
        {
            // ReadJsonObject()のkey nullチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[{");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase23()
        {
            // ReadJsonObject()のkey nullチェックのところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("[{");
        }

        /**
        * Case24: jsonStringが"]}"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase24()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("]}");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase24()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("]}");
        }

        /**
        * Case25: jsonStringが"}]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase25()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("}]");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase25()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("}]");
        }

        /**
        * Case26: jsonStringが"{[}"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase26()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("{[}");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase26()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{[}");
        }

        /**
        * Case27: jsonStringが"1"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase27()
        {
            // ReadJsonObject()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("1");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase27()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("1");
        }

        /**
        * Case28: jsonStringが"{a:1, b:"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase28()
        {
            // ReadJsonObject()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("{a:1, b:");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase28()
        {
            // ReadJsonArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{a:1, b:");
        }

        /**
        * Case29: jsonStringが"[a:1, b:"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase29()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[a:1, b:");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase29()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("[a:1, b:");
        }

        /**
        * Case30: jsonStringが"{a:1, b:2:3]"
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase30()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("{a:1, b:2:3]");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionCase30()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("{a:1, b:2:3]");
        }

        /**
        * Case31: jsonStringが"[\"a\"}]
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionCase31()
        {
            // Parse()のところでArgumentExceptionを発行する
            var json = NbJsonParser.Parse("[\"a\"}]");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayNormalCase31()
        {
            // ParseArray()のところでArgumentExceptionを発行する
            var json = NbJsonParser.ParseArray("[\"a\"}]");
        }

        /**
        * jsonStringがnull
        **/
        /// <summary>
        /// Parse
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestParseExceptionJsonStringNull()
        {
            // Android版との差分（Android版ではnullが返る）
            var json = NbJsonParser.Parse(null);
        }

        /// <summary>
        /// ParseArray
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestParseArrayExceptionJsonStringNull()
        {
            // Android版との差分（Android版ではnullが返る）
            var json = NbJsonParser.ParseArray(null);
        }

        /**
        * jsonStringが""
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionJsonStringEmptyCase1()
        {
            // if (!reader.Read())の分岐にヒットする
            // Android版との差分（Android版ではnullが返る）
            var json = NbJsonParser.Parse("");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionJsonStringEmptyCase1()
        {
            // if (!reader.Read())の分岐にヒットする
            // Android版との差分（Android版ではnullが返る）
            var json = NbJsonParser.ParseArray("");
        }

        /**
        * jsonStringが" "
        **/
        /// <summary>
        /// Parse
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseExceptionJsonStringEmptyCase2()
        {
            // if (!reader.Read())の分岐にヒットする
            var json = NbJsonParser.Parse(" ");
        }

        /// <summary>
        /// ParseArray
        /// ArgumentExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestParseArrayExceptionJsonStringEmptyCase2()
        {
            // if (!reader.Read())の分岐にヒットする
            var json = NbJsonParser.ParseArray(" ");
        }

        // CreateReader()、ReadJsonObject()、ReadJsonArray()、ReadValue()
        // はprivate staticメソッドなので、それ自身のUTは不可。
        // Parse()、ParseArray()にて条件網羅させる。
    }
}
