using Nec.Nebula.Internal;

namespace Nec.Nebula
{
    /// <summary>
    /// APIゲートウェイ REST レスポンス。
    /// </summary>
    public class NbRestJsonResponse : NbRestResponseBase
    {
        /// <summary>
        /// JSON オブジェクト
        /// </summary>
        public NbJsonObject JsonObject { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="nbrestResponse">レスポンス</param>
        public NbRestJsonResponse(NbRestResponse nbrestResponse)
        {
            Response = nbrestResponse.Response;
            var bodyString = nbrestResponse.Response.Content.ReadAsStringAsync().Result;
            JsonObject = NbJsonObject.Parse(bodyString);
            ContentLength = System.Text.Encoding.UTF8.GetBytes(bodyString).Length;
        }
    }
}
