using Nec.Nebula.Internal;

namespace Nec.Nebula
{
    /// <summary>
    /// Pushメッセージ内のSSE固有値
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbSseFields : NbPushFieldsBase
    {
        /// <summary>
        /// イベントID
        /// </summary>
        public string EventId
        {
            get { return Fields.Opt<string>(Field.SseEventId, null); }
            set { SetIfNotNull(Field.SseEventId, value); }
        }

        /// <summary>
        /// イベントタイプ
        /// </summary>
        public string EventType
        {
            get { return Fields.Opt<string>(Field.SseEventType, null); }
            set { SetIfNotNull(Field.SseEventType, value); }
        }

    }
}
