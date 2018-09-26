namespace Nec.Nebula
{
    /// <summary>
    /// Nebula 独自ステータスコード。
    /// </summary>
    public enum NbStatusCode
    {
        #region SDK共通(10xx)
        /// <summary>
        /// 排他エラー
        /// </summary>
        Locked = 1000,
        #endregion

        #region 通信(11xx)
        /// <summary>
        /// ChunkedEncodingによるDLでのファイルサイズ不整合
        /// </summary>
        FailedToDownload = 1100
        #endregion
    }
}
