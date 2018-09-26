using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Nec.Nebula
{
    /// <summary>
    /// REST レスポンス（基底クラス）。
    /// HttpResponseMessage のラッパ。
    /// </summary>
    public class NbRestResponseBase
    {
        /// <summary>
        /// レスポンス
        /// </summary>
        public HttpResponseMessage Response { get; internal set; }

        /// <summary>
        /// ヘッダ
        /// </summary>
        public HttpResponseHeaders Headers
        {
            get { return Response.Headers; }
        }

        /// <summary>
        /// 指定したヘッダの値を取得する。
        /// 存在しない場合は null を返却する。
        /// 複数存在する場合は先頭の値を返却する。
        /// </summary>
        /// <param name="headerName">ヘッダ名</param>
        /// <returns>ヘッダ値</returns>
        /// <remarks>Content-Type はこのメソッドでは取得できない</remarks>
        public string GetHeader(string headerName)
        {
            IEnumerable<string> headerValues;
            var found = Headers.TryGetValues(headerName, out headerValues);

            if (!found) return null;

            return headerValues.FirstOrDefault();
        }

        /// <summary>
        /// Content-Type
        /// </summary>
        public string ContentType
        {
            get { return Response.Content.Headers.GetValues("Content-Type").FirstOrDefault(); }
        }

        /// <summary>
        /// Content-Length
        /// </summary>
        public long ContentLength { get; internal set; }
    }
}
