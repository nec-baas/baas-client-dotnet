using Nec.Nebula.Internal;
using System;

namespace Nec.Nebula
{
    /// <summary>
    /// <para>Nebula サービスハンドル。</para>
    /// <para>Nebula の使用を開始する前に作成、設定しておかなければならない。</para>
    /// </summary>
    public class NbService
    {
        private static NbService _sSingleton = new NbService();

        private static bool _sIsMultiTenantEnabled = false;

        private static readonly object _Lock = new object();

        /// <summary>
        /// オフラインサービス
        /// </summary>
        internal INbOfflineService OfflineService { get; set; }

        /// <summary>
        /// シングルトンインスタンスを返却する。
        /// マルチテナントアクセスモードが有効な場合は InvalidOperationException がスローされる。
        /// </summary>
        /// <exception cref="InvalidOperationException">マルチテナントアクセスモードが有効</exception>
        internal static NbService Singleton
        {
            get
            {
                if (_sIsMultiTenantEnabled)
                {
                    throw new InvalidOperationException("Multi tenant mode is enabled!");
                }
                return _sSingleton;
            }
        }

        /// <summary>
        /// <para>インスタンスを生成する。</para>
        /// <para>マルチテナントモード無効時は、毎回同一のインスタンスが返却される。
        /// マルチテナントモード有効時は、毎回異なるインスタンスが生成される。</para>
        /// </summary>
        /// <returns>サービス</returns>
        public static NbService GetInstance()
        {
            lock (_Lock)
            {
                if (_sIsMultiTenantEnabled)
                {
                    return new NbService();
                }

                if (_sSingleton == null)
                {
                    _sSingleton = new NbService();
                }
                return _sSingleton;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private NbService()
        {
            SessionInfo = new NbSessionInfo();
            RestExecutor = new NbRestExecutor(this);

            _sSingleton = this;
        }

        internal static void DisposeSingleton()
        {
            _sSingleton = null;
        }

        /// <summary>
        /// <para>マルチテナントアクセスモードを有効にする。デフォルトは無効。</para>
        /// <para>各 API 呼び出し時は、NbService を引数に明示的に指定しなければならない。</para>
        /// </summary>
        /// <param name="enabled">有効時は true</param>
        public static void EnableMultiTenant(bool enabled)
        {
            _sIsMultiTenantEnabled = enabled;
        }

        /// <summary>
        /// テナントID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// アプリケーションID
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// アプリケーションキー
        /// </summary>
        public string AppKey { get; set; }

        private string _endpointUrl = null;

        /// <summary>
        /// <para>エンドポイントURL</para>
        /// <para>末尾がスラッシュでない場合は、自動補完される。</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">エンドポイントURLがnull</exception>
        public string EndpointUrl
        {
            get { return _endpointUrl; }
            set
            {
                NbUtil.NotNullWithArgument(value, "EndpointUrl");

                _endpointUrl = value;

                if (!_endpointUrl.EndsWith("/"))
                {
                    _endpointUrl = string.Concat(_endpointUrl, "/");
                }
            }
        }

        /// <summary>
        /// HTTPリクエストタイムアウト。
        /// 初期値は System.Threading.TimeSpan.InfiniteTimeSpan。
        /// 
        /// REST リクエストを一度でも発行したあとにタイムアウト値を変更すると、
        /// 内部の HttpClient が再生成されHTTPSコネクションが別途再接続となる。
        /// また、タイムアウト値変更前のリクエストはキャンセルできなくなるので注意すること。
        /// </summary>
        public TimeSpan HttpTimeout
        {
            get { return RestExecutor.HttpTimeout; }
            set { RestExecutor.HttpTimeout = value;  }
        }

        /// <summary>
        /// 実行中の HTTP REST リクエストをキャンセルする。
        /// 実行待ちのタスクには TaskCanceledException 例外が送出される。
        /// </summary>
        public void CancelPendingHttpRequests()
        {
            RestExecutor.CancelPendingRequests();
        }

        /// <summary>
        /// セッション情報
        /// </summary>
        internal NbSessionInfo SessionInfo { get; private set; }

        /// <summary>
        /// REST Executor
        /// </summary>
        public NbRestExecutor RestExecutor { get; set; }

        /// <summary>
        /// オフラインサービスの有効状態を返す
        /// </summary>
        /// <returns>有効であれば true</returns>
        public bool IsOfflineEnabled()
        {
            return OfflineService != null;
        }

        /// <summary>
        /// オフラインサービスを無効にする
        /// </summary>
        internal void DisableOffline()
        {
            if (OfflineService != null)
            {
                OfflineService.CloseDatabase();
                OfflineService = null;
            }
        }

        /// <summary>
        /// データベースのパスワードを変更する
        /// </summary>
        /// <remarks>
        /// オフラインサービスが有効時のみ変更可能
        /// </remarks>
        /// <exception cref="InvalidOperationException">オフラインサービスが無効</exception>
        public void ChangeDatabasePassword(string newPassword)
        {
            if (OfflineService == null)
            {
                throw new InvalidOperationException("Offline Service is disabled!");
            }
            else
            {
                OfflineService.ChangeOfflineDatabasePassword(newPassword);
            }
        }
    }
}
