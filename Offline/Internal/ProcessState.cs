using System.Threading;

namespace Nec.Nebula.Internal
{
    /// <summary>
    /// 処理状態フラグ管理クラス
    /// </summary>
    internal class ProcessState
    {
        private static ProcessState _sSingleton = new ProcessState();

        /// <summary>
        /// 同期状態
        /// </summary>
        internal bool Syncing { get; set; }

        /// <summary>
        /// CRUD状態
        /// </summary>
        internal bool Crud { get; set; }

        private static readonly object _lock = new object();

        /// <summary>
        /// インスタンスを生成する。
        /// 毎回同一のインスタンスが返却される。
        /// </summary>
        /// <returns>インスタンス</returns>
        public static ProcessState GetInstance()
        {
            return _sSingleton;
        }

        /// <summary>
        /// 同期開始可能かどうかを確認する（同期開始前にコールすること）。
        /// 更新・削除中は、Waitする。
        /// </summary>
        /// <returns>同期開始可能であれば true</returns>
        public bool TryStartSync()
        {
            lock (_lock)
            {
                // CRUD中は待ち
                while (Crud)
                {
                    Monitor.Wait(_lock);
                }

                // 同期中はエラー
                if (Syncing)
                {
                    Monitor.PulseAll(_lock);
                    return false;
                }

                Syncing = true;
                Monitor.PulseAll(_lock);
            }

            return true;
        }

        /// <summary>
        /// 同期処理が終了する（同期終了後にコールすること）。
        /// </summary>
        public void EndSync()
        {
            lock (_lock)
            {
                Syncing = false;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// 更新・削除可能かどうかを確認する（更新・削除開始前にコールすること）。
        /// 更新・削除中は、Waitする。
        /// </summary>
        /// <returns>更新・削除可能であれば true</returns>
        public bool TryStartCrud()
        {
            lock (_lock)
            {
                // CRUD中は待ち
                while (Crud)
                {
                    Monitor.Wait(_lock);
                }

                // 同期中はエラー
                if (Syncing)
                {
                    Monitor.PulseAll(_lock);
                    return false;
                }

                Crud = true;
                Monitor.PulseAll(_lock);
            }

            return true;
        }

        /// <summary>
        /// 更新・削除処理が終了する（更新・削除終了後にコールすること）。
        /// </summary>
        public void EndCrud()
        {
            lock (_lock)
            {
                Crud = false;
                Monitor.PulseAll(_lock);
            }
        }
    }
}

