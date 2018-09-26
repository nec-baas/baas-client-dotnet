namespace Nec.Nebula
{
    /// <summary>
    /// 同期状態。
    /// </summary>
    public enum NbSyncState
    {
        /// <summary>
        ///  サーバ同期済み状態
        /// </summary>
        Sync = 0,

        /// <summary>
        /// クライアント変更状態 (サーバ未同期)
        /// </summary>
        Dirty = 1
    }
}