
namespace Nec.Nebula
{
    /// <summary>
    /// 衝突解決定義
    /// </summary>
    public static class NbObjectConflictResolver
    {
        /// <summary>
        /// 衝突解決レゾルバ。
        /// サーバ選択するときは server を、クライアント選択するときは client を返却すること。
        /// データをマージするときは、client にデータをマージして client を返却すること。
        /// </summary>
        /// <param name="server">サーバデータ</param>
        /// <param name="client">クライアントデータ</param>
        /// <returns>選択データ</returns>
        public delegate NbObject Resolver(NbObject server, NbObject client);

        /// <summary>
        /// サーバ優先レゾルバ
        /// </summary>
        public static readonly Resolver PreferServerResolver = (server, client) => server;

        /// <summary>
        /// クライアント優先レゾルバ
        /// </summary>
        public static readonly Resolver PreferClientResolver = (server, client) => client;

        ///// <summary>
        ///// 更新日時が新しいほうを選択するレゾルバ
        ///// </summary>
        //public static readonly Resolver PreferRecentResolver =
        //    (server, client) => string.Compare(server.UpdatedAt, client.UpdatedAt, StringComparison.Ordinal) >= 0 ? server : client;
    }
}
