using System.Net.Http;

namespace Nec.Nebula
{
    /// <summary>
    /// REST レスポンス。
    /// </summary>
    public class NbRestResponse : NbRestResponseBase
    {
        /// <summary>
        /// データ(raw bytes)
        /// </summary>
        public byte[] RawBytes { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response">レスポンス</param>
        /// <param name="rawBytes">Bodyデータ</param>
        public NbRestResponse(HttpResponseMessage response, byte[] rawBytes)
        {
            Response = response;
            RawBytes = rawBytes;
            ContentLength = rawBytes.Length;
        }
    }
}
