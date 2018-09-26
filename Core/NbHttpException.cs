using System;
using System.Net;
using System.Net.Http;

namespace Nec.Nebula
{
    /// <summary>
    /// <para>Nebula 通信例外。</para>
    /// <para>HTTPステータスコードと、RESTレスポンスを保持する。</para>
    /// </summary>
    public class NbHttpException : Exception
    {
        /// <summary>
        /// HTTPステータスコード
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// REST Response
        /// </summary>
        public HttpResponseMessage Response { get; private set; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public NbHttpException()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message">メッセージ</param>
        public NbHttpException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="inner">内部例外</param>
        public NbHttpException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="statusCode">ステータスコード</param>
        public NbHttpException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="statusCode">ステータスコード</param>
        /// <param name="message">message(reason)文字列</param>
        public NbHttpException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response">HTTP応答メッセージ</param>
        public NbHttpException(HttpResponseMessage response)
            : base((response != null) ? response.ReasonPhrase : null)
        {
            Response = response;
            StatusCode = (response != null) ? response.StatusCode : 0;
        }
    }
}
