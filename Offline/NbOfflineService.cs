using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Data.Entity;
using System.IO;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// オフラインサービス
    /// </summary>
    public class NbOfflineService : INbOfflineService
    {
        /// <summary>
        /// データベース
        /// </summary>
        public NbDatabase Database { get; internal set; }
        private static string _sIdpath;
        internal static bool MemoryMode = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        internal NbOfflineService()
        {
        }

        /// <summary>
        /// <para>オフラインサービスを有効にする。</para>
        /// <para>データベースファイルは「AppData/Local/NEC/Nebula/テナントID/アプリID」配下に作成される。</para>
        /// <para>ディレクトリは自動で作成される。</para>
        /// </summary>
        /// <param name="service">オフラインサービスを有効にする NbService</param>
        /// <param name="password">暗号化パスワード。省略時は暗号化しない。</param>
        /// <exception cref="ArgumentNullException">serviceがnull</exception>
        /// <exception cref="InvalidOperationException">TenantIDもしくはAppIDがnullまたは空文字</exception>
        public static void EnableOfflineService(NbService service, string password = null)
        {
            NbUtil.NotNullWithArgument(service, "service");
            if ((service.TenantId == null || service.TenantId == "") ||
                (service.AppId == null || service.AppId == ""))
            {
                throw new InvalidOperationException("Bad TenantID or Bad AppID");
            }
            _sIdpath = service.TenantId + "/" + service.AppId;
            var offlineService = new NbOfflineService();
            offlineService.OpenDatabase(password);
            service.OfflineService = offlineService;
        }

        private void OpenDatabase(string password = null)
        {
            var dbpath = ":memory:";
            if (!MemoryMode)
            {
                dbpath = GetDbFullPath();

                // ディレクトリ作成
                var dir = Path.GetDirectoryName(dbpath);
                Directory.CreateDirectory(dir);
            }

            Database = new NbDatabaseImpl(dbpath, password);
            Database.Open();

            Database.ExecSql("PRAGMA auto_vacuum=FULL");
        }

        /// <summary>
        /// データベースのフルパスを得る
        /// </summary>
        /// <returns>データベースのフルパス</returns>
        private static string GetDbFullPath()
        {
            var documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fulldbpath = "NEC/Nebula/" + _sIdpath + "/offline.db";
            fulldbpath = Path.Combine(documentFolder, fulldbpath);
            return fulldbpath;
        }

        /// <summary>
        /// データベースをクローズする。
        /// </summary>
        public void CloseDatabase()
        {
            if (Database != null)
            {
                Database.Close();
            }
            Database = null;
        }

        /// <summary>
        /// データベースファイルを強制削除する
        /// </summary>
        public void DeleteOfflineDatabase()
        {
            var fullpath = GetDbFullPath();
            File.Delete(fullpath);
        }

        /// <summary>
        /// オフラインデータベースのパスワードを変更する
        /// </summary>
        /// <param name="newPassword">新パスワード</param>
        public void ChangeOfflineDatabasePassword(string newPassword)
        {
            if (Database != null)
                Database.ChangePassword(newPassword);
        }

        /// <summary>
        /// テスト用にインメモリDBを使用する場合、EnableOfflineServiceをCallする前にmodeをtrueに設定する
        /// 本APIをCallしたあとに、EnableOfflineServiceをCallすること
        /// </summary>
        internal static void SetInMemoryMode(bool mode = true)
        {
            MemoryMode = mode;
        }

        /// <summary>
        /// オブジェクトキャッシュ、バケットキャッシュをすべてクリアする
        /// </summary>
        /// <exception cref="InvalidOperationException">オフラインサービスが無効</exception>
        /// <exception cref="NbException">同期処理中</exception>
        /// <returns>Task</returns>
        public static async Task DeleteCacheAll()
        {
            if (!NbService.Singleton.IsOfflineEnabled())
            {
                throw new InvalidOperationException("Offline service is not enabled");
            }

            Task task = Task.Run(new Action(() =>
            {
                NbOfflineObjectBucket<NbOfflineObject>.DeleteAll();
            }));

            await task;
        }
    }
}
