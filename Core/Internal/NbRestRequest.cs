using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Nec.Nebula.Internal
{
    /// <summary>
    /// REST リクエスト。
    /// </summary>
    public class NbRestRequest
    {
        private readonly HttpClient _client;

        /// <summary>
        /// HTTP メソッド
        /// </summary>
        public HttpMethod Method { get; private set; }

        /// <summary>
        /// URI
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// ヘッダ
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// クエリパラメータ
        /// </summary>
        public Dictionary<string, string> QueryParams { get; private set; }

        /// <summary>
        /// コンテンツ（ボディ、コンテンツヘッダ）
        /// </summary>
        public HttpContent Content { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="client">HttpClient</param>
        /// <param name="baseUrl">ベースURL</param>
        /// <param name="path">ベースURLからの相対パス</param>
        /// <param name="method">HTTPメソッド</param>
        public NbRestRequest(HttpClient client, string baseUrl, string path, HttpMethod method)
        {
            Headers = new Dictionary<string, string>();
            QueryParams = new Dictionary<string, string>();

            Method = method;

            _client = client;
            Uri = baseUrl + path;
        }

        /// <summary>
        /// リクエスト送信
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> SendAsync()
        {
            var uriBuilder = new UriBuilder(Uri)
            {
                // keyはSDK内で付与するため、処理簡略化のため、URIエンコードしない
                Query =
                    string.Join("&", from x in QueryParams select x.Key + "=" + System.Uri.EscapeDataString(x.Value))
            };

            var message = new HttpRequestMessage(Method, uriBuilder.ToString())
            {
                Content = Content
            };
            foreach (var h in Headers)
            {
                message.Headers.Add(h.Key, h.Value);
            }

            LogRequest(message);

            var response = await SendAsync(message);

            LogResponse(response);

            return response;
        }

        /// <summary>
        /// リクエスト送信
        /// SendAsync()処理の動作検証用に分離。
        /// </summary>
        /// <param name="message">リクエストメッセージ</param>
        /// <returns>HttpResponseMessage</returns>
        internal virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
        {
            return await _client.SendAsync(message);
        }

        /// <summary>
        /// バイナリリクエストボディを設定する。
        /// 呼び元で第二引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="contentType">Content-Type</param>
        /// <param name="data">バイナリデータ</param>
        /// <returns>this</returns>
        public NbRestRequest SetRequestBody(string contentType, byte[] data)
        {
            Content = new ByteArrayContent(data);

            if (contentType != null)
            {
                Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            return this;
        }

        /// <summary>
        /// JSONをボティに設定する。
        /// 呼び元で引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="json">JSONオブジェクト</param>
        /// <returns>this</returns>
        public NbRestRequest SetJsonBody(NbJsonObject json)
        {
            Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            return this;
        }

        /// <summary>
        /// HTTPヘッダを設定する。
        /// すでに同一名のヘッダがあった場合は上書きされる。
        /// 呼び元で第一引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="name">ヘッダ名</param>
        /// <param name="value">ヘッダ値</param>
        /// <returns>this</returns>
        public NbRestRequest SetHeader(string name, string value)
        {
            Headers[name] = value;
            return this;
        }

        /// <summary>
        /// URLセグメントを設定する。
        /// パスの {name} プレースホルダの部分が置換される。
        /// 呼び元で引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="name">セグメント名</param>
        /// <param name="value">プレースホルダに設定する値</param>
        /// <returns>this</returns>
        public NbRestRequest SetUrlSegment(string name, string value)
        {
            Uri = Uri.Replace("{" + name + "}", System.Uri.EscapeDataString(value));
            return this;
        }

        /// <summary>
        /// URLセグメントを設定する（設定値にエスケープ処理をしない）。
        /// パスの {name} プレースホルダの部分が置換される。
        /// 呼び元で引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="name">セグメント名</param>
        /// <param name="value">プレースホルダに設定する値</param>
        /// <returns>this</returns>
        public NbRestRequest SetUrlSegmentNoEscape(string name, string value)
        {
            Uri = Uri.Replace("{" + name + "}", value);
            return this;
        }

        /// <summary>
        /// クエリパラメータを設定する。すでに同一名のパラメータがあった場合は上書きされる。
        /// 呼び元で第一引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="name">パラメータ名</param>
        /// <param name="value">パラメータ値</param>
        /// <returns>this</returns>
        public NbRestRequest SetQueryParameter(string name, string value)
        {
            QueryParams[name] = value;
            return this;
        }

        /// <summary>
        /// リクエストの内容をログ出力する。
        /// </summary>
        /// <param name="message">リクエストメッセージ</param>
        private void LogRequest(HttpRequestMessage message)
        {
            Debug.WriteLine("[Request]");
            Debug.WriteLine("Method:      " + message.Method);
            Debug.WriteLine("RequestUri:  " + message.RequestUri);
        }

        /// <summary>
        /// レスポンスの内容をログ出力する。
        /// </summary>
        /// <param name="message">レスポンスメッセージ</param>
        private void LogResponse(HttpResponseMessage message)
        {
            Debug.WriteLine("[Response]");
            Debug.WriteLine("StatusCode:  " + message.StatusCode);
            Debug.WriteLine("ReasonPhase: " + message.ReasonPhrase);
        }
    }
}
