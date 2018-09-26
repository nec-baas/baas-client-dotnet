using Nec.Nebula.Internal;
using NUnit.Framework;
using System;

namespace Nec.Nebula.Test.Internal
{
    [TestFixture]
    public class NbTypeConverterTest
    {
        /**
        * ConvertValue
        **/
        /// <summary>
        /// ConvertValue（null）
        /// 指定した型の通りとなること
        /// </summary>
        [Test]
        public void TestConvertValueNormalNull()
        {
            Assert.AreEqual(0, NbTypeConverter.ConvertValue<int>(null));
            Assert.AreEqual(0.0, NbTypeConverter.ConvertValue<double>(null));
            Assert.IsNull(NbTypeConverter.ConvertValue<string>(null));
            Assert.False(NbTypeConverter.ConvertValue<bool>(null));
        }

        /// <summary>
        /// ConvertValue（型が同一）
        /// 指定した型で取得できること
        /// </summary>
        [Test]
        public void TestConvertValueNormalSameType()
        {
            Assert.AreEqual(sbyte.MaxValue, NbTypeConverter.ConvertValue<sbyte>(sbyte.MaxValue));
            Assert.AreEqual(byte.MaxValue, NbTypeConverter.ConvertValue<byte>(byte.MaxValue));
            Assert.AreEqual('a', NbTypeConverter.ConvertValue<char>('a'));
            Assert.AreEqual(short.MaxValue, NbTypeConverter.ConvertValue<short>(short.MaxValue));
            Assert.AreEqual(ushort.MaxValue, NbTypeConverter.ConvertValue<ushort>(ushort.MaxValue));
            Assert.AreEqual(int.MaxValue, NbTypeConverter.ConvertValue<int>(int.MaxValue));
            Assert.AreEqual(uint.MaxValue, NbTypeConverter.ConvertValue<uint>(uint.MaxValue));
            Assert.AreEqual(long.MaxValue, NbTypeConverter.ConvertValue<long>(long.MaxValue));
            Assert.AreEqual(ulong.MaxValue, NbTypeConverter.ConvertValue<ulong>(ulong.MaxValue));
            Assert.AreEqual(float.MaxValue, NbTypeConverter.ConvertValue<float>(float.MaxValue));
            Assert.AreEqual(double.MaxValue, NbTypeConverter.ConvertValue<double>(double.MaxValue));
            Assert.AreEqual(decimal.MaxValue, NbTypeConverter.ConvertValue<decimal>(decimal.MaxValue));
            Assert.True(NbTypeConverter.ConvertValue<bool>(true));
            Assert.AreEqual("abcde", NbTypeConverter.ConvertValue<string>("abcde"));
            // object型は個別に下で行う
        }

        /// <summary>
        /// ConvertValue（型が同一 object）
        /// 指定した型で取得できること
        /// </summary>
        [Test]
        public void TestConvertValueNormalSameTypeObject()
        {
            var obj = new object();
            Assert.AreEqual(obj, NbTypeConverter.ConvertValue<object>(obj));
        }

        /// <summary>
        /// ConvertValue（数値型でないキャスト char→sring）
        /// InvalidCastExceptionを発行すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestConvertValueExceptionCharToString()
        {
            NbTypeConverter.ConvertValue<string>('a');
        }

        /// <summary>
        /// ConvertValue（数値型でないキャスト bool→sring）
        /// InvalidCastExceptionを発行すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestConvertValueExceptionBoolToString()
        {
            NbTypeConverter.ConvertValue<string>((bool)true);
        }

        /// <summary>
        /// ConvertValue（int→stringにキャスト）
        /// InvalidCastExceptionを発行すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestConvertValueExceptionIntToString()
        {
            NbTypeConverter.ConvertValue<string>(12345);
        }

        /// <summary>
        /// ConvertValue（string→intにキャスト）
        /// InvalidCastExceptionを発行すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestConvertValueExceptionStringToInt()
        {
            NbTypeConverter.ConvertValue<int>("12345");
        }

        /// <summary>
        /// ConvertValue（bool→intにキャスト）
        /// InvalidCastExceptionを発行すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestConvertValueExceptionBoolToInt()
        {
            NbTypeConverter.ConvertValue<int>(false);
        }

        /// <summary>
        /// ConvertValue（byte→longにキャスト）
        /// 指定した型で取得できること
        /// </summary>
        [Test]
        public void TestConvertValueNormalByteToLong()
        {
            byte b = 100;
            Assert.AreEqual(100L, NbTypeConverter.ConvertValue<long>(b));
        }

        /// <summary>
        /// ConvertValue（int→longにキャスト）
        /// 指定した型で取得できること
        /// </summary>
        [Test]
        public void TestConvertValueNormalIntToLong()
        {
            int i = 100;
            Assert.AreEqual(100L, NbTypeConverter.ConvertValue<long>(i));
        }

        /// <summary>
        /// ConvertValue（long→doubleにキャスト）
        /// 指定した型で取得できること
        /// </summary>
        [Test]
        public void TestConvertValueNormalLongToDouble()
        {
            Assert.AreEqual(100.0, NbTypeConverter.ConvertValue<double>(100L));
        }

        /// <summary>
        /// ConvertValue（long→intにキャスト）
        /// 指定した型で取得できること
        /// </summary>
        [Test]
        public void TestConvertValueNormalLongToInt()
        {
            Assert.AreEqual(12345, NbTypeConverter.ConvertValue<int>(12345L));
        }

        /// <summary>
        /// ConvertValue（double→longにキャスト）
        /// 指定した型で取得できること
        /// </summary>
        [Test]
        public void TestConvertValueNormalDoubleToLong()
        {
            Assert.AreEqual(100L, NbTypeConverter.ConvertValue<long>(100.123456));
        }

        private static TestCaseData[] IsNumericWithObjectTestSource
            = new[]
        {
            new TestCaseData(sbyte.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalSbyte"),
            new TestCaseData(byte.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalByte"),
            new TestCaseData('a').Returns(false).SetName("TestIsNumericWithObjectNormalChar"),
            new TestCaseData(short.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalShort"),
            new TestCaseData(ushort.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalUshort"),
            new TestCaseData(int.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalInt"),
            new TestCaseData(uint.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalUint"),
            new TestCaseData(long.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalLong"),
            new TestCaseData(ulong.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalUlong"),
            new TestCaseData(float.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalFloat"),
            new TestCaseData(double.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalDouble"),
            new TestCaseData(decimal.MaxValue).Returns(true).SetName("TestIsNumericWithObjectNormalDecimal"),
            new TestCaseData(false).Returns(false).SetName("TestIsNumericWithObjectNormalBool"),
            new TestCaseData("string").Returns(false).SetName("TestIsNumericWithObjectNormalString"),
            new TestCaseData(new object()).Returns(false).SetName("TestIsNumericWithObjectNormalObject")
        };

        /**
        * IsNumeric 引数がobject
        **/
        /// <summary>
        /// IsNumeric
        /// 戻り値が正しいこと
        /// </summary>
        [Test, TestCaseSource("IsNumericWithObjectTestSource")]
        public bool TestIsNumericWithObjectNormal(object value)
        {
            return NbTypeConverter.IsNumeric(value);
        }

        private static TestCaseData[] IsNumericWithTypeTestSource
            = new[]
        {
            new TestCaseData(typeof(sbyte)).Returns(true).SetName("TestIsNumericWithTypeNormalSbyte"),
            new TestCaseData(typeof(byte)).Returns(true).SetName("TestIsNumericWithTypeNormalByte"),
            new TestCaseData(typeof(char)).Returns(false).SetName("TestIsNumericWithTypeNormalChar"),
            new TestCaseData(typeof(short)).Returns(true).SetName("TestIsNumericWithTypeNormalShort"),
            new TestCaseData(typeof(ushort)).Returns(true).SetName("TestIsNumericWithTypeNormalUshort"),
            new TestCaseData(typeof(int)).Returns(true).SetName("TestIsNumericWithTypeNormalInt"),
            new TestCaseData(typeof(uint)).Returns(true).SetName("TestIsNumericWithTypeNormalUint"),
            new TestCaseData(typeof(long)).Returns(true).SetName("TestIsNumericWithTypeNormalLong"),
            new TestCaseData(typeof(ulong)).Returns(true).SetName("TestIsNumericWithTypeNormalUlong"),
            new TestCaseData(typeof(float)).Returns(true).SetName("TestIsNumericWithTypeNormalFloat"),
            new TestCaseData(typeof(double)).Returns(true).SetName("TestIsNumericWithTypeNormalDouble"),
            new TestCaseData(typeof(decimal)).Returns(true).SetName("TestIsNumericWithTypeNormalDecimal"),
            new TestCaseData(typeof(bool)).Returns(false).SetName("TestIsNumericWithTypeNormalBool"),
            new TestCaseData(typeof(string)).Returns(false).SetName("TestIsNumericWithTypeNormalString"),
            new TestCaseData(typeof(object)).Returns(false).SetName("TestIsNumericWithTypeNormalObject")
        };

        /**
        * IsNumeric 引数がtype
        **/
        /// <summary>
        /// IsNumeric
        /// 戻り値が正しいこと
        /// </summary>
        [Test, TestCaseSource("IsNumericWithTypeTestSource")]
        public bool TestIsNumericWithTypeNormal(Type type)
        {
            return NbTypeConverter.IsNumeric(type);
        }

        /**
        * CompareObject
        **/

        /// <summary>
        /// CompareObject
        /// 数値同士の比較
        /// </summary>
        [Test]
        public void TestCompareObjectNormalNumeric()
        {
            Assert.IsTrue(NbTypeConverter.CompareObject(1, 1));
            Assert.IsTrue(NbTypeConverter.CompareObject(1.0, 1.0));
            Assert.IsFalse(NbTypeConverter.CompareObject(2, 1));
            Assert.IsFalse(NbTypeConverter.CompareObject(1.2, 1.3));
        }

        /// <summary>
        /// CompareObject
        /// 文字列同士の比較
        /// </summary>
        [Test]
        public void TestCompareObjectNormalString()
        {
            Assert.IsTrue(NbTypeConverter.CompareObject("a", "a"));
            Assert.IsFalse(NbTypeConverter.CompareObject("a", "b"));
            Assert.IsFalse(NbTypeConverter.CompareObject("aaa", "aab"));
        }

        /// <summary>
        /// CompareObject
        /// NbJsonObject同士の比較
        /// </summary>
        [Test]
        public void TestCompareObjectNormalNbJsonObject()
        {
            var obj = new NbJsonObject() { { "a", 1 }, { "b", 2 } };
            var obj2 = new NbJsonObject() { { "a", 1 }, { "b", 2 } };
            var obj3 = new NbJsonObject() { { "a", 2 }, { "b", 3 } };
            var obj4 = new NbJsonObject() { { "c", 1 }, { "d", 2 } };

            Assert.IsTrue(NbTypeConverter.CompareObject(obj, obj2));
            Assert.IsFalse(NbTypeConverter.CompareObject(obj, obj3));
            Assert.IsFalse(NbTypeConverter.CompareObject(obj, obj4));
        }

        /// <summary>
        /// CompareObject
        /// NbJsonArray同士の比較
        /// </summary>
        [Test]
        public void TestCompareObjectNormalNbJsonArray()
        {
            var obj = new NbJsonArray() { 1, 2, 3 };
            var obj2 = new NbJsonArray() { 1, 2, 3 };
            var obj3 = new NbJsonArray() { "1", "2", "3" };

            Assert.IsTrue(NbTypeConverter.CompareObject(obj, obj2));
            Assert.IsFalse(NbTypeConverter.CompareObject(obj, obj3));
        }

        /// <summary>
        /// CompareObject
        /// 引数がnull
        /// </summary>
        [Test]
        public void TestCompareObjectNormalNull()
        {
            Assert.IsTrue(NbTypeConverter.CompareObject(null, null));
            Assert.IsFalse(NbTypeConverter.CompareObject(null, 1));
            Assert.IsFalse(NbTypeConverter.CompareObject("a", null));
        }

        /**
        * GetHashCode
        **/


        /// <summary>
        /// GetHashCode
        /// 数値
        /// </summary>
        [Test]
        public void TestGetHashCodeNormalNumeric()
        {
            Assert.AreNotEqual(0, NbTypeConverter.GetHashCode(1));
        }

        /// <summary>
        /// GetHashCode
        /// 文字列
        /// </summary>
        [Test]
        public void TestGetHashCodeNormalString()
        {
            Assert.AreNotEqual(0, NbTypeConverter.GetHashCode("a"));
        }

        /// <summary>
        /// GetHashCode
        /// NbJsonObject
        /// </summary>
        [Test]
        public void TestGetHashCodeNormalNbJsonObject()
        {
            var obj = new NbJsonObject() { { "a", 1 }, { "b", 2 } };
            Assert.AreNotEqual(0, NbTypeConverter.GetHashCode(obj));
        }

        /// <summary>
        /// GetHashCode
        /// NbJsonArray
        /// </summary>
        [Test]
        public void TestGetHashCodeNormalNbJsonArray()
        {
            var obj = new NbJsonArray() { 1, 2, 3 };
            Assert.AreNotEqual(0, NbTypeConverter.GetHashCode(obj));
        }

        /// <summary>
        /// GetHashCode
        /// 引数がnull
        /// </summary>
        [Test]
        public void TestGetHashCodeNormalNull()
        {
            Assert.AreEqual(0, NbTypeConverter.GetHashCode(null));
        }
    }
}
