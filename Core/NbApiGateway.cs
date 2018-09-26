using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// APIゲートウェイ
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbApiGateway
    {
        /// <summary>
        /// Nebula サービス
        /// </summary>
        internal NbService Service { get; set; }

        /// <summary>
        /// API名
        /// </summary>
        public string ApiName { get; set; }

        /// <summary>
        /// サブパス
        /// </summary>
        public string SubPath { get; set; }

        /// <summary>
        /// HttpMethod
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// ヘッダ
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// クエリパラメータ
        /// </summary>
        public Dictionary<string, string> QueryParams { get; private set; }

        /// <summary>
        /// Content-Type
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// バイナリデータを格納するクラス
        /// </summary>
        public class ByteArrayData
        {
            /// <summary>
            /// Content-Type
            /// </summary>
            public string ContentType { get; set; }

            /// <summary>
            /// Content-Length
            /// </summary>
            public long ContentLength { get; set; }

            /// <summary>
            /// バイナリデータ
            /// </summary>
            public byte[] RawBytes { get; set; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="apiname">API名</param>
        /// <param name="httpmethod">HTTPメソッド</param>
        /// <param name="subpath">サブパス</param>
        /// <param name="service">サービス</param>
        public NbApiGateway(string apiname, HttpMethod httpmethod, string subpath, NbService service = null)
        {
            Headers = new Dictionary<string, string>();
            QueryParams = new Dictionary<string, string>();

            ApiName = apiname;
            Method = httpmethod;
            SubPath = subpath;

            Service = service ?? NbService.Singleton;
        }

        /// <summary>
        /// APIを実行する（レスポンスがJSONObject）
        /// </summary>
        /// <param name="body">ボディ（NbJsonObject又はbyte[]とする）</param>
        /// <returns>Task</returns>
        /// <exception cref="InvalidOperationException">ApiNameがNULL</exception>
        /// <exception cref="InvalidOperationException">POST/PUT要求時にボディに対するContentTypeが未設定</exception>
        /// <exception cref="ArgumentException">ボディの型が対象外</exception>
        public async Task<NbRestJsonResponse> ExecuteAsync(object body = null)
        {
            NbUtil.NotNullWithInvalidOperation(ApiName, "apiname");

            var req = CreateRestRequest(body);

            var nbrestResponse = await Service.RestExecutor.ExecuteRequest(req);

            return new NbRestJsonResponse(nbrestResponse);
        }

        /// <summary>
        /// APIを実行する（レスポンスがバイナリデータ）
        /// </summary>
        /// <param name="body">ボディ（NbJsonObject又はbyte[]とする）</param>
        /// <returns>Task</returns>
        /// <exception cref="InvalidOperationException">ApiNameがNULL</exception>
        /// <exception cref="InvalidOperationException">POST/PUT要求時にボディに対するContentTypeが未設定</exception>
        /// <exception cref="ArgumentException">ボディの型が対象外</exception>
        public async Task<NbRestResponse> ExecuteRawAsync(object body = null)
        {
            NbUtil.NotNullWithInvalidOperation(ApiName, "apiname");

            var req = CreateRestRequest(body);

            var nbrestResponse = await Service.RestExecutor.ExecuteRequest(req);

            return nbrestResponse;
        }

        /// <summary>
        /// リクエスト生成
        /// </summary>
        /// <param name="body">ボディ（NbJsonObject又はbyte[]とする）</param>
        /// <returns>リクエスト</returns>
        /// <exception cref="InvalidOperationException">POST/PUT要求時にボディに対するContentTypeが未設定</exception>
        /// <exception cref="ArgumentException">ボディの型が対象外</exception>
        private NbRestRequest CreateRestRequest(object body)
        {
            var req = Service.RestExecutor.CreateRequest("/api/{apiname}/{subpath}", Method);
            req.SetUrlSegment("apiname", ApiName);
            req.SetUrlSegmentNoEscape("subpath", SubPath ?? string.Empty);

            Dictionary<string, string> tmpHeaders = new Dictionary<string, string>(Headers);

            // Content-Typeとその他ヘッダの振り分け
            string priorityContentType = null;
            if (tmpHeaders.TryGetValue("Content-Type", out priorityContentType))
            {
                tmpHeaders.Remove("Content-Type");
            }

            // ContentType統合
            if (!string.IsNullOrEmpty(ContentType))
            {
                priorityContentType = ContentType;
            }

            if (Method.Equals(HttpMethod.Post) || Method.Equals(HttpMethod.Put))
            {
                // ContentType指定漏れチェック
                if (body != null)
                {
                    NbUtil.NotNullWithInvalidOperation(priorityContentType, "Content-Type");
                }

                if (body is NbJsonObject)
                {
                    req.SetJsonBody((NbJsonObject)body);
                    req.Content.Headers.ContentType = new MediaTypeHeaderValue(priorityContentType);
                }
                else if (body is byte[])
                {
                    req.SetRequestBody(priorityContentType, (byte[])body);
                }
                else if (body != null)
                {
                    throw new ArgumentException("body is not NbJsonObject/byte[]");
                }
            }

            foreach (var qparam in QueryParams)
            {
                req.SetQueryParameter(qparam.Key, qparam.Value);
            }

            foreach (var header in tmpHeaders)
            {
                req.SetHeader(header.Key, header.Value);
            }

            return req;
        }

        /// <summary>
        /// HTTPヘッダを設定する。
        /// すでに同一名のヘッダがあった場合は上書きされる。
        /// </summary>
        /// <param name="name">ヘッダ名</param>
        /// <param name="value">ヘッダ値</param>
        public void SetHeader(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            {
                return;
            }

            Headers[name] = value;
        }

        /// <summary>
        /// HTTPヘッダを削除する。
        /// </summary>
        /// <param name="name">ヘッダ名</param>
        public bool RemoveHeader(string name)
        {
            return Headers.Remove(name);
        }

        /// <summary>
        /// HTTPヘッダの全てを削除する。
        /// </summary>
        public void ClearHeaders()
        {
            Headers.Clear();
        }

        /// <summary>
        /// クエリパラメータを設定する。すでに同一名のパラメータがあった場合は上書きされる。
        /// </summary>
        /// <param name="name">クエリパラメータ名</param>
        /// <param name="value">クエリパラメータ値</param>
        public void SetQueryParameter(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            {
                return;
            }

            QueryParams[name] = value;
        }

        /// <summary>
        /// クエリパラメータを削除する。
        /// </summary>
        /// <param name="name">クエリパラメータ名</param>
        /// <returns>正常に削除された場合は true</returns>
        public bool RemoveQueryParameter(string name)
        {
            return QueryParams.Remove(name);
        }

        /// <summary>
        /// クエリパラメータの全てを削除する。
        /// </summary>
        public void ClearQueryParameters()
        {
            QueryParams.Clear();
        }
    }
}