using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbJsonArrayTest
    {
        /**
        * Parse
        **/
        /// <summary>
        /// Parse（正常）
        /// パース結果が正しいこと
        /// </summary>
        [Test]
        public void TestParseNormal()
        {
            var jsonString = "[1,2,3]";
            var jsonArray = NbJsonArray.Parse(jsonString);

            Assert.AreEqual(3, jsonArray.Count);
            Assert.AreEqual(1, jsonArray[0]);
            Assert.AreEqual(2, jsonArray[1]);
            Assert.AreEqual(3, jsonArray[2]);
        }

        // Parse()の詳細なUTはNbJsonParserTest側にて実施する

        /**
        * ToString
        **/
        /// <summary>
        /// ToString（正常）
        /// JSON配列文字列に正しく変換されること
        /// </summary>
        [Test]
        public void TestToStringNormal()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            Assert.AreEqual("[1,2,3]", jsonArray.ToString());
        }

        /// <summary>
        /// ToString（空のJsonArray）
        /// JSON文字列に正しく変換されること
        /// </summary>
        [Test]
        public void TestToStringNormalEmpty()
        {
            var jsonArray = new NbJsonArray();

            var s = jsonArray.ToString();

            Assert.AreEqual("[]", s);
        }

        /**
        * コンストラクタ
        **/
        /// <summary>
        /// コンストラクタ（正常）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var jsonArray = new NbJsonArray();

            Assert.IsNotNull(jsonArray);
        }

        /**
        * コンストラクタ 引数がcollection
        **/
        /// <summary>
        /// コンストラクタ（引数にSet型を設定）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorWithCollectionNormalSet()
        {
            var set = new HashSet<string>();
            set.Add("1");
            set.Add("2");

            var jsonArray = new NbJsonArray(set);

            Assert.AreEqual(2, jsonArray.Count);
            Assert.AreEqual("1", jsonArray[0]);
            Assert.AreEqual("2", jsonArray[1]);
        }

        /// <summary>
        /// コンストラクタ（引数にList型を設定）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorWithCollectionNormalList()
        {
            var list = new List<string>();
            list.Add("1");
            list.Add("2");

            var jsonArray = new NbJsonArray(list);

            Assert.AreEqual(2, jsonArray.Count);
            Assert.AreEqual("1", jsonArray[0]);
            Assert.AreEqual("2", jsonArray[1]);
        }

        /// <summary>
        /// コンストラクタ（引数に空のSetを設定）
        /// インスタンスが生成できること
        /// </summary>
        [Test]
        public void TestConstructorWithCollectionNormalEmpty()
        {
            var set = new HashSet<string>();

            var jsonArray = new NbJsonArray(set);

            Assert.AreEqual(0, jsonArray.Count);
        }

        /// <summary>
        /// コンストラクタ（collectionにnullを設定）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorWithCollectionExceptionCollectionNull()
        {
            var jsonArray = new NbJsonArray(null);
        }

        /**
        * Get
        **/
        /// <summary>
        /// Get（型がstring）
        /// 指定した位置の値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalString()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add("1");
            jsonArray.Add("2");
            jsonArray.Add("3");

            Assert.AreEqual("1", jsonArray.Get<string>(0));
            Assert.AreEqual("2", jsonArray.Get<string>(1));
            Assert.AreEqual("3", jsonArray.Get<string>(2));
        }

        /// <summary>
        /// Get（型がint）
        /// 指定した位置の値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalInt()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            Assert.AreEqual(1, jsonArray.Get<int>(0));
            Assert.AreEqual(2, jsonArray.Get<int>(1));
            Assert.AreEqual(3, jsonArray.Get<int>(2));
        }

        /// <summary>
        /// Get（indexが0未満）
        /// ArgumentOutOfRangeExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetExceptionIndexLessThanZero()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.Get<int>(-1);
        }

        /// <summary>
        /// Get（indexが範囲外）
        /// ArgumentOutOfRangeExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetExceptionIndexOutOfRange()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.Get<int>(4);
        }

        /// <summary>
        /// Get（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetExceptionInvalidType()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.Get<string>(0);
        }

        /**
        * GetJsonObject
        **/
        /// <summary>
        /// GetJsonObject
        /// 指定した位置の値を取得できること
        /// </summary>
        [Test]
        public void TestGetJsonObjectNormal()
        {
            var jsonArray = new NbJsonArray();
            var json = new NbJsonObject()
            {
                {"a", "b"}
            };
            jsonArray.Add(json);

            Assert.AreEqual(json, jsonArray.GetJsonObject(0));
        }

        /// <summary>
        /// GetJsonObject（indexが範囲外）
        /// ArgumentOutOfRangeExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetJsonObjectExceptionIndexOutOfRange()
        {
            var jsonArray = new NbJsonArray();
            var json = new NbJsonObject()
            {
                {"a", "b"}
            };
            jsonArray.Add(json);

            var v = jsonArray.GetJsonObject(4);
        }

        /// <summary>
        /// GetJsonObject（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetJsonObjectExceptionInvalidType()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.GetJsonObject(0);
        }

        /**
        * GetArray
        **/
        /// <summary>
        /// GetArray
        /// 指定した位置の値を取得できること
        /// </summary>
        [Test]
        public void TestGetArrayNormal()
        {
            var json = new NbJsonArray();
            json.Add(1);
            json.Add(2);
            json.Add(3);
            var jsonArray = new NbJsonArray();
            jsonArray.Add(json);

            Assert.AreEqual(json, jsonArray.GetArray(0));
        }

        /// <summary>
        /// GetArray（indexが範囲外）
        /// ArgumentOutOfRangeExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGetArrayExceptionIndexOutOfRange()
        {
            var json = new NbJsonArray();
            json.Add(1);
            json.Add(2);
            json.Add(3);
            var jsonArray = new NbJsonArray();
            jsonArray.Add(json);

            var v = jsonArray.GetArray(-1);
        }

        /// <summary>
        /// GetArray（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetArrayExceptionInvalidType()
        {
            var jsonArray = new NbJsonArray();
            var json = new NbJsonObject()
            {
                {"a", "b"}
            };
            jsonArray.Add(json);

            var v = jsonArray.GetArray(0);
        }

        /**
        * ToList
        **/
        /// <summary>
        /// ToList
        /// リストを取得できること
        /// </summary>
        [Test]
        public void TestToListNormal()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.ToList<int>();

            Assert.True(v is IList<int>);
            Assert.AreEqual(3, v.Count);
            Assert.AreEqual(1, v[0]);
            Assert.AreEqual(2, v[1]);
            Assert.AreEqual(3, v[2]);
        }

        /// <summary>
        /// ToList
        /// リストを取得できること
        /// </summary>
        [Test]
        public void TestToListNormalEmpty()
        {
            var jsonArray = new NbJsonArray();

            var v = jsonArray.ToList<string>();

            Assert.True(v is IList<string>);
            Assert.IsEmpty(v);
        }

        /// <summary>
        /// ToList（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestToListExceptionInvalidType()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add(2);
            jsonArray.Add(3);

            var v = jsonArray.ToList<string>();
        }

        /**
        * Add, Clear
        **/
        /// <summary>
        /// Add, Clear（正常）
        /// 要素を追加できること、要素をクリアできること
        /// </summary>
        [Test]
        public void TestAddAndClearNormal()
        {
            var jsonArray = new NbJsonArray();
            jsonArray.Add(1);
            jsonArray.Add("s");

            Assert.AreEqual(2, jsonArray.Count);
            Assert.AreEqual(1, jsonArray[0]);
            Assert.AreEqual("s", jsonArray.Get<string>(1));

            jsonArray.Clear();
            Assert.IsEmpty(jsonArray);
        }


        /**
        * Equals/GetHashCode
        **/

        /// <summary>
        /// Equals テスト
        /// </summary>
        [Test]
        public void TestEquals()
        {
            var array1 = new NbJsonArray {1, 2, 3};

            // 同一インスタンス
            Assert.True(array1.Equals(array1));

            // null テスト
            Assert.False(array1.Equals(null));

            // 型不一致テスト
            Assert.False(array1.Equals(100));

            // 一致テスト
            NbJsonArray that;
            that = new NbJsonArray {1, 2, 3};
            Assert.True(array1.Equals(that));
            Assert.AreEqual(array1.GetHashCode(), that.GetHashCode());

            // 内容不一致
            that = new NbJsonArray {1, 2, 4};
            Assert.False(array1.Equals(that));
            Assert.AreNotEqual(array1.GetHashCode(), that.GetHashCode());

            // サイズ不一致
            that = new NbJsonArray {1, 2, 3, 4};
            Assert.False(array1.Equals(that));
        }

        /// <summary>
        /// Equals
        /// 数値
        /// </summary>
        [Test]
        public void TestEqualsNormalNumeric()
        {
            var array1 = new NbJsonArray { 1, 2, 3 };

            // 同一インスタンス
            Assert.True(array1.Equals(array1));

            // 一致テスト
            NbJsonArray that;
            that = new NbJsonArray { 1, 2, 3 };
            Assert.True(array1.Equals(that));
            Assert.AreEqual(array1.GetHashCode(), that.GetHashCode());

            // 内容不一致
            that = new NbJsonArray { 1, 2, 4 };
            Assert.False(array1.Equals(that));
            Assert.AreNotEqual(array1.GetHashCode(), that.GetHashCode());

            // サイズ不一致
            that = new NbJsonArray { 1, 2, 3, 4 };
            Assert.False(array1.Equals(that));
        }

        /// <summary>
        /// Equals
        /// 文字列
        /// </summary>
        [Test]
        public void TestEqualsNormalString()
        {
            var array1 = new NbJsonArray { "1", "2", "3" };

            // 同一インスタンス
            Assert.True(array1.Equals(array1));

            // 一致テスト
            NbJsonArray that;
            that = new NbJsonArray { "1", "2", "3" };
            Assert.True(array1.Equals(that));
            Assert.AreEqual(array1.GetHashCode(), that.GetHashCode());

            // 内容不一致
            that = new NbJsonArray { "1", "2", "4" };
            Assert.False(array1.Equals(that));
            Assert.AreNotEqual(array1.GetHashCode(), that.GetHashCode());

            // サイズ不一致
            that = new NbJsonArray { "1", "2", "3", "4" };
            Assert.False(array1.Equals(that));
        }

        /// <summary>
        /// Equals
        /// null
        /// </summary>
        [Test]
        public void TestEqualsNormalNull()
        {
            var array1 = new NbJsonArray { 1, 2, 3 };

            // null テスト
            Assert.False(array1.Equals(null));

            var that = new NbJsonArray { null, null, null };
            Assert.False(array1.Equals(that));
            Assert.AreNotEqual(array1.GetHashCode(), that.GetHashCode());
        }
    }
}
