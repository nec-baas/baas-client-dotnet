using Nec.Nebula.Internal;

namespace Nec.Nebula
{
    /// <summary>
    /// Pushメッセージ内パラメータの各方式基底クラス
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbPushFieldsBase
    {
        /// <summary>
        /// 設定値
        /// </summary>
        public NbJsonObject Fields { get; internal set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NbPushFieldsBase()
        {
            Fields = new NbJsonObject();
        }

        // null でない場合設定する。nullの場合フィールドを削除する
        internal void SetIfNotNull(string key, object value)
        {
            if (value != null)
            {
                Fields[key] = value;
            }
            else
            {
                Fields.Remove(key);
            }
        }
    }
}