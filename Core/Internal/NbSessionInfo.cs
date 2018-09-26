using System;

namespace Nec.Nebula.Internal
{
    /// <summary>
    /// セッション情報
    /// </summary>
    public class NbSessionInfo
    {
        /// <summary>
        /// セッショントークン
        /// </summary>
        public string SessionToken { get; private set; }

        /// <summary>
        /// セッショントークン有効期限(unix time, 秒)
        /// </summary>
        public long Expire { get; private set; }

        /// <summary>
        /// 現在ログイン中のユーザ
        /// </summary>
        public NbUser CurrentUser { get; set; }

        /// <summary>
        /// セッション情報をセットする
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <param name="expire"></param>
        /// <param name="currentUser"></param>
        public void Set(string sessionToken, long expire, NbUser currentUser)
        {
            SessionToken = sessionToken;
            Expire = expire;
            CurrentUser = currentUser;
        }

        /// <summary>
        /// セッションをクリアする
        /// </summary>
        public void Clear()
        {
            SessionToken = null;
            Expire = 0;
            CurrentUser = null;
        }

        /// <summary>
        /// セッションが有効か調べる
        /// </summary>
        /// <returns></returns>
        public bool IsAvailable()
        {
            if (SessionToken == null) return false;
            return NbUtil.CurrentUnixTime() < Expire;
        }
    }
}
