using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nec.Nebula.Internal;
using System.Net.Http;

namespace Nec.Nebula
{
    /// <summary>
    /// Pushメッセージ送信
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbPush
    {
        /// <summary>
        /// <para>送信先インスタレーションを指定するためのクエリ</para>
        /// <para>検索条件のJSONオブジェクト表記(MongoDBクエリ表記)のみ有効</para>
        /// </summary>
        public NbQuery Query { get; set; }

        /// <summary>
        /// Push メッセージ本文
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 通知を受信可能なユーザ・グループの一覧
        /// </summary>
        public ISet<string> AllowedReceivers { get; set; }

        /// <summary>
        /// iOS 固有値
        /// </summary>
        public NbApnsFields ApnsFields { get; set; }

        /// <summary>
        /// Android 固有値
        /// </summary>
        public NbGcmFields GcmFields { get; set; }

        /// <summary>
        /// SSE 固有値
        /// </summary>
        public NbSseFields SseFields { get; set; }

        internal NbService Service { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="service">サービス</param>
        public NbPush(NbService service = null)
        {
            Service = service ?? NbService.Singleton;
        }

        /// <summary>
        /// <para>Pushメッセージを送信する。</para>
        /// <para>Query, Message は事前に設定しておくこと。</para>
        /// </summary>
        /// <returns>Task。Resultは NbJsonObject。installationsキーで該当したインスタレーション数を取得可能。</returns>
        /// <exception cref="InvalidOperationException">Query, Message が未設定</exception>
        public async Task<NbJsonObject> SendAsync()
        {
            if (Query == null)
            {
                throw new InvalidOperationException("No query");
            }
            if (Message == null)
            {
                throw new InvalidOperationException("No message");
            }

            var json = ToJson();
            var req = Service.RestExecutor.CreateRequest("/push/notifications", HttpMethod.Post);
            req.SetJsonBody(json);

            return await Service.RestExecutor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// JSON Object 表現に変換
        /// </summary>
        /// <returns>JSON Object</returns>
        private NbJsonObject ToJson()
        {
            NbJsonObject json = new NbJsonObject();

            json[Field.Query] = Query.Conditions;

            json[Field.Message] = Message;

            if (AllowedReceivers != null)
            {
                json[Field.AllowedReceivers] = new NbJsonArray(AllowedReceivers);;
            }

            if (ApnsFields != null)
            {
                json.Merge(ApnsFields.Fields);
            }

            if (GcmFields != null)
            {
                json.Merge(GcmFields.Fields);
            }

            if (SseFields != null)
            {
                json.Merge(SseFields.Fields);
            }

            return json;
        }
    }
}