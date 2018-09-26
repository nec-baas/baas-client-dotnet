using System;
using System.Linq;

namespace Nec.Nebula.Internal
{
    internal class NbUtil
    {
        private static readonly DateTime EpochDate = new DateTime(1970, 1, 1);

        /// <summary>
        /// UNIX Time (1970/1/1 00:00:00 UTC からの経過秒数) を返す
        /// </summary>
        /// <returns>UNIX Time</returns>
        public static long CurrentUnixTime()
        {
            long now = (long)((DateTime.UtcNow - EpochDate).TotalSeconds);
            return now;
        }

        /// <summary>
        /// null チェックを行う。null だった場合は ArgumentNullException を throw する
        /// </summary>
        /// <param name="x">パラメータ</param>
        /// <param name="name">パラメータ名</param>
        /// <exception cref="ArgumentNullException">パラメータがnull</exception>
        public static void NotNullWithArgument(Object x, string name)
        {
            if (x == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// null チェックを行う。null だった場合は InvalidOperationException を throw する
        /// </summary>
        /// <param name="x">パラメータ</param>
        /// <param name="message">メッセージ</param>
        /// <exception cref="InvalidOperationException">パラメータがnull</exception>
        public static void NotNullWithInvalidOperation(Object x, string message)
        {
            if (x == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// nullチェックと 空文字チェックを行う。nullか空文字の場合は InvalidOperationException を throw する
        /// </summary>
        /// <param name="x">チェック文字列</param>
        /// <param name="message">エラーメッセージ</param>
        /// <exception cref="InvalidOperationException">チェック文字列がnullか空文字</exception>
        public static void NotNullorEmptyWithInvalidOperation(string x, string message)
        {
            if (String.IsNullOrEmpty(x))
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// 配列要素にnullが含まれないことを確認する。
        /// </summary>
        /// <param name="args">チェック対象の配列</param>
        /// <remarks><paramref name="args"/>がnullでないことを保証すること</remarks>
        /// <exception cref="ArgumentException">配列の要素にnullが含まれている</exception>
        public static void NotContainsNullWithArgumentException(object[] args)
        {
            if (args.Contains(null))
            {
                throw new ArgumentException("argument contains null.");
            }
        }

        /// <summary>
        /// NbException(排他エラー) を Throw する。
        /// 以下から false が返ってきた時にコールすること。
        /// <see cref="M:ProcessState.TryStartSync()"/><br/>
        /// <see cref="M:ProcessState.ProcessState.TryStartCrud()"/>
        /// </summary>
        /// <exception cref="NbException">排他エラー</exception> 
        public static void ThrowLockedException()
        {
            throw new NbException(NbStatusCode.Locked, "Locked.");
        }
    }
}
