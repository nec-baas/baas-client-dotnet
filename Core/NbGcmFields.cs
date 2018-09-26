using Nec.Nebula.Internal;

namespace Nec.Nebula
{
    /// <summary>
    /// Pushメッセージ内のAndroid固有値
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbGcmFields : NbPushFieldsBase
    {
        /// <summary>
        /// システムバーに表示するタイトル
        /// </summary>
        public string Title
        {
            get { return Fields.Opt<string>(Field.Title, null); }
            set { SetIfNotNull(Field.Title, value); }
        }

        /// <summary>
        /// 通知を開いたときに起動するURI
        /// </summary>
        public string Uri
        {
            get { return Fields.Opt<string>(Field.Uri, null); }
            set { SetIfNotNull(Field.Uri, value); }
        }

    }
}