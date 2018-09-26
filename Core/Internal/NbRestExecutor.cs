using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Nec.Nebula.Internal
{
    /// <summary>
    /// REST Executor
    /// </summary>
    public class NbRestExecutor
    {
        private readonly NbService _service;
        private readonly NbSessionInfo _sessionInfo;

        private HttpClient _httpClient;

        private const string HeaderAppId = "X-Application-Id";
        private const string HeaderAppKey = "X-Application-Key";
        private const string HeaderSessionToken = "X-Session-Token";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="service">サービス</param>
        public NbRestExecutor(NbService service = null)
        {
            if (service == null)
            {
                service = NbService.Singleton;
            }
            _service = service;
            _sessionInfo = service.SessionInfo;

            _httpClient = new HttpClient()
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        /// <summary>
        /// リクエスト生成
        /// 呼び元で引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="path">APIパス(テナントID以降)</param>
        /// <param name="method">HTTPメソッド</param>
        /// <returns>リクエスト</returns>
        /// <exception cref="InvalidOperationException">NbServiceの設定値がnull</exception>
        public NbRestRequest CreateRequest(string path, HttpMethod method)
        {
            NbUtil.NotNullWithInvalidOperation(_service.EndpointUrl, "No EndpointUrl");
            NbUtil.NotNullWithInvalidOperation(_service.TenantId, "No TenantId");
            NbUtil.NotNullWithInvalidOperation(_service.AppId, "No AppId");
            NbUtil.NotNullWithInvalidOperation(_service.AppKey, "No AppKey");

            var request = new NbRestRequest(_httpClient, _service.EndpointUrl + "1/" + _service.TenantId, path, method);

            request.SetHeader(HeaderAppId, _service.AppId);
            request.SetHeader(HeaderAppKey, _service.AppKey);
            request.SetHeader(Header.UserAgent, Header.UserAgentDefaultValue);

            if (_sessionInfo.IsAvailable())
            {
                request.SetHeader(HeaderSessionToken, _sessionInfo.SessionToken);
            }

            return request;
        }

        /// <summary>
        /// リクエスト実行
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <returns>Task</returns>
        protected virtual async Task<HttpResponseMessage> _ExecuteRequest(NbRestRequest request)
        {
            var response = await request.SendAsync();

            return response;
        }

        /// <summary>
        /// リクエスト実行
        /// 呼び元で引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <returns>Task</returns>
        /// <exception cref="NbHttpException">リクエスト失敗時</exception>
        public async Task<NbRestResponse> ExecuteRequest(NbRestRequest request)
        {
            var response = await _ExecuteRequest(request);

            // UTにおける各機能のレスポンス制御のため_ExecuteRequestでの共通化は行わない
            if (!IsSuccessful((int)response.StatusCode))
            {
                //Debug.Print(response.ToString());
                throw new NbHttpException(response);
            }

            var rawBytes = await response.Content.ReadAsByteArrayAsync();
            var r = new NbRestResponse(response, rawBytes);
            return r;
        }

        /// <summary>
        /// リクエスト実行(レスポンスが JSONObject)。
        /// リクエスト失敗時は、NbHttpException がスローされる。
        /// 呼び元で引数にnullを設定しないようにすること。
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <returns>Task</returns>
        /// <exception cref="NbHttpException">リクエスト失敗時</exception>
        public async Task<NbJsonObject> ExecuteRequestForJson(NbRestRequest request)
        {
            var response = await _ExecuteRequest(request);
            if (!IsSuccessful((int)response.StatusCode))
            {
                //Debug.Print(response.ToString());
                throw new NbHttpException(response);
            }

            NbJsonObject responseJson;
            if (response.Content != null)
            {
                // サーバ対向で確認すると、レスポンスボディが含まないケースがなかったが、
                // REST APIリファレンス上は存在するので、Fail Safe対応としてnullチェックを実施する
                var bodyString = await response.Content.ReadAsStringAsync();
                responseJson = NbJsonObject.Parse(bodyString);
            }
            else
            {
                responseJson = new NbJsonObject();
            }

            return responseJson;
        }

        /// <summary>
        /// ステータスコードが成功 (200番台)であれば true を返す
        /// </summary>
        /// <param name="statusCode">ステータスコード</param>
        /// <returns>2xx なら true</returns>
        private bool IsSuccessful(int statusCode)
        {
            return statusCode / 100 == 2;
        }

        /// <summary>
        /// 実行中のリクエストをキャンセルする
        /// </summary>
        public void CancelPendingRequests()
        {
            _httpClient.CancelPendingRequests();
        }

        /// <summary>
        /// HTTPタイムアウト。
        /// HTTPリクエストを一度でも発行したあとに変更しようとすると、
        /// HttpClient が再作成される。
        /// </summary>
        public TimeSpan HttpTimeout
        {
            get { return _httpClient.Timeout; }
            set
            {
                try
                {
                    _httpClient.Timeout = value;
                }
                catch (InvalidOperationException e)
                {
                    // HTTP リクエスト発行後のタイムアウト変更。
                    // HttpClient を再作成する。
                    Debug.WriteLine("Re-create HttpClient to change HttpTimeout");
                    _httpClient = new HttpClient()
                    {
                        Timeout = value
                    };
                }
            }
        }
    }
}
