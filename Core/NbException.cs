using System;

namespace Nec.Nebula
{
    /// <summary>
    /// <para>Nebula 独自例外。</para>
    /// <para>独自ステータスコードを保持する。</para>
    /// </summary>
    public class NbException : Exception
    {
        /// <summary>
        /// 独自ステータスコード
        /// </summary>
        public NbStatusCode StatusCode { get; private set; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public NbException()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="statusCode">ステータスコード</param>
        /// <param name="message">message(reason)文字列</param>
        public NbException(NbStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
