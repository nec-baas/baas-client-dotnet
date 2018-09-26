using Nec.Nebula.Internal;

namespace Nec.Nebula
{
    /// <summary>
    /// Pushメッセージ内のiOS固有値
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbApnsFields : NbPushFieldsBase
    {
        /// <summary>
        /// バッジカウント
        /// </summary>
        public int? Badge
        {
            get { return Fields.Opt<int?>(Field.Badge, null); }
            set { SetIfNotNull(Field.Badge, value); }
        }

        /// <summary>
        /// Application Bundle 内のサウンドファイル名
        /// </summary>
        public string Sound
        {
            get { return Fields.Opt<string>(Field.Sound, null); }
            set { SetIfNotNull(Field.Sound, value); }
        }

        /// <summary>
        /// <para>バックグランド更新</para>
        /// <para>1にセットすると、バックグランド Push が有効</para>
        /// </summary>
        public int? ContentAvailable
        {
            get { return Fields.Opt<int?>(Field.ContentAvailable, null); }
            set { SetIfNotNull(Field.ContentAvailable, value); }
        }

        /// <summary>
        /// Notification カテゴリ
        /// </summary>
        public string Category
        {
            get { return Fields.Opt<string>(Field.Category, null); }
            set { SetIfNotNull(Field.Category, value); }
        }

    }
}
