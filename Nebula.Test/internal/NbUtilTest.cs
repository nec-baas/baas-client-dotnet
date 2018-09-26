using Nec.Nebula.Internal;
using NUnit.Framework;
using System;

namespace Nec.Nebula.Test.Internal
{
    [TestFixture]
    public class NbUtilTest
    {
        /**
        * CurrentUnixTime
        **/
        /// <summary>
        /// CurrentUnixTime（正常）
        /// 戻り値が0でないこと
        /// </summary>
        [Test]
        public void TestCurrentUnixTimeNormal()
        {
            // DateTime.UtcNowがStaticなので、Mock化できない
            // Reflectionを使っての書き換えもしない
            // 0でないことだけの検証をする

            Assert.AreNotEqual(0, NbUtil.CurrentUnixTime());
        }

        /**
        * NotNullWithArgument
        **/
        /// <summary>
        /// NotNullWithArgument（正常）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotNullWithArgumentNormal()
        {
            try
            {
                NbUtil.NotNullWithArgument("test", "testName");
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        /// <summary>
        /// NotNullWithArgument（nameがnull）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotNullWithArgumentSubnormalNameNull()
        {
            try
            {
                NbUtil.NotNullWithArgument("test", null);
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        /// <summary>
        /// NotNullWithArgument（xがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotNullWithArgumentExceptionXNull()
        {
            NbUtil.NotNullWithArgument(null, "testName");
        }

        /// <summary>
        /// NotNullWithArgument（x,nameがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNotNullWithArgumentExceptionBothNull()
        {
            NbUtil.NotNullWithArgument(null, null);
        }

        /**
        * NotNullWithInvalidOperation
        **/
        /// <summary>
        /// NotNullWithInvalidOperation（正常）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotNullWithInvalidOperationNormal()
        {
            try
            {
                NbUtil.NotNullWithInvalidOperation("test", "testName");
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        /// <summary>
        /// NotNullWithInvalidOperation（messageがnull）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotNullWithInvalidOperationSubnormalMessageNull()
        {
            try
            {
                NbUtil.NotNullWithInvalidOperation("test", null);
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        /// <summary>
        /// NotNullWithInvalidOperation（xがnull）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestNotNullWithInvalidOperationExceptionXNull()
        {
            NbUtil.NotNullWithInvalidOperation(null, "testName");
        }

        /// <summary>
        /// NotNullWithInvalidOperation（x,messageがnull）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestNotNullWithInvalidOperationExceptionBothNull()
        {
            NbUtil.NotNullWithInvalidOperation(null, null);
        }

        /**
        * NotContainsNullWithArgumentException
        **/
        /// <summary>
        /// NotContainsNullWithArgumentException（string型配列）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotContainsNullWithArgumentExceptionNormalStringArray()
        {
            string[] strs = { "a", "b", "c" };

            try
            {
                NbUtil.NotContainsNullWithArgumentException(strs);
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        /// <summary>
        /// NotContainsNullWithArgumentException（object型配列）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotContainsNullWithArgumentExceptionNormalObjectArray()
        {
            try
            {
                NbUtil.NotContainsNullWithArgumentException(new object[] { 1, 2, 3, 4 });
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        /// <summary>
        /// NotContainsNullWithArgumentException（空配列）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotContainsNullWithArgumentExceptionNormalEmpty()
        {
            try
            {
                NbUtil.NotContainsNullWithArgumentException(new string[0]);
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        // 以下のような配列内の配列内にnullを含む場合はチェック対象ではないので、
        // Exceptionを発行しなくても問題ない
        /// <summary>
        /// NotContainsNullWithArgumentException（配列内の配列内にnullを含む場合）
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestNotContainsNullWithArgumentExceptionNormalContainsStringArrayNull()
        {
            string[] s = { "1", null, "3" };
            object[] obj = { 1, 2, s };
            try
            {
                NbUtil.NotContainsNullWithArgumentException(obj);
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }

        /// <summary>
        /// NotContainsNullWithArgumentException（nullを含むstring型配列）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestNotContainsNullWithArgumentExceptionExceptionStringArrayContainsNull()
        {
            NbUtil.NotContainsNullWithArgumentException(new string[] { "1", "2", null });
        }

        /// <summary>
        /// NotContainsNullWithArgumentException（nullを含むobject型配列）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestNotContainsNullWithArgumentExceptionExceptionObjectArrayContainsNull()
        {
            NbUtil.NotContainsNullWithArgumentException(new object[] { null, 1, 2 });
        }

        // internalクラスとなるため、引数にnullが渡される場合のUTは省略する
        // 上位側でnullを指定しないようにチェックする必要がある

        /**
        * ThrowLockedException
        **/
        /// <summary>
        /// ThrowLockedException（正常）
        /// NbExceptionが発行されること
        /// </summary>
        [Test]
        public void TestThrowLockedExceptionException()
        {
            try
            {
                NbUtil.ThrowLockedException();
                Assert.Fail("Bad Route.");
            }
            catch (NbException ex)
            {
                Assert.AreEqual(NbStatusCode.Locked, ex.StatusCode);
                Assert.AreEqual("Locked.", ex.Message);
            }
            catch (Exception)
            {
                Assert.Fail("Bad Route.");
            }
        }
    }
}
