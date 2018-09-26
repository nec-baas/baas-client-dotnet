using Nec.Nebula.Internal.Database;

namespace Nec.Nebula
{
    /// <summary>
    /// オフラインサービス。
    /// データベースハンドルなどを保持する。
    /// </summary>
    /// <remarks>
    /// オフラインサービスの実体は Offline ライブラリ側に存在するため、
    /// オフライン機能を使用する場合は Offline ライブラリへの参照が必要。
    /// </remarks>
    public interface INbOfflineService
    {
        /// <summary>
        /// データベースハンドル
        /// </summary>
        NbDatabase Database { get; }

        /// <summary>
        /// データベースをクローズする。
        /// </summary>
        void CloseDatabase();

        /// <summary>
        /// データベースファイルを強制削除する
        /// </summary>
        void DeleteOfflineDatabase();

        /// <summary>
        /// データベースパスワードを変更する
        /// </summary>
        /// <param name="newPassword">新パスワード</param>
        void ChangeOfflineDatabasePassword(string newPassword);
    }
}