using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// オブジェクトバケット(基底クラス)
    /// </summary>
    public interface INbObjectBucket
    {
        /// <summary>
        /// バケット名
        /// </summary>
        string BucketName { get; }

        /// <summary>
        /// サービス
        /// </summary>
        NbService Service { get; }
    }

    /// <summary>
    /// オブジェクトバケット
    /// </summary>
    /// <typeparam name="T">NbObject及びそのサブクラス</typeparam>
    public abstract class NbObjectBucketBase<T> : INbObjectBucket where T : NbObject, new()
    {
        /// <summary>
        /// オブジェクト検索結果
        /// </summary>
        /// <typeparam name="TT">NbObject及びそのサブクラス</typeparam>
        public struct NbObjectQueryResult<TT>
        {
            /// <summary>
            /// オブジェクトのリスト
            /// </summary>
            public IEnumerable<TT> Objects { get; set; }

            /// <summary>
            /// ヒット件数 (不明時は -1)
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// サーバ現在時刻
            /// </summary>
            public string CurrentTime { get; set; }
        }

        /// <summary>
        /// バケット名
        /// </summary>
        public string BucketName { get; internal set; }

        /// <summary>
        /// サービス
        /// </summary>
        public NbService Service { get; internal set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="service">サービス</param>
        /// <exception cref="ArgumentException">バケット名が特定できない</exception>
        protected NbObjectBucketBase(string bucketName = null, NbService service = null)
        {
            if (bucketName == null)
            {
                bucketName = new T().BucketName;
            }
            if (bucketName == null)
            {
                throw new ArgumentException("Bucket name is unspecified.");
            }

            Service = service ?? NbService.Singleton;
            BucketName = bucketName;
        }

        /// <summary>
        /// オブジェクトを生成する
        /// </summary>
        /// <returns>新規オブジェクト</returns>
        public abstract T NewObject();

        /// <summary>
        /// オブジェクトID検索
        /// </summary>
        /// <param name="objectId">オブジェクトID</param>
        /// <returns>オブジェクト</returns>
        public abstract Task<T> GetAsync(string objectId);

        /// <summary>
        /// オブジェクトを検索する
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <returns>オブジェクト検索結果</returns>
        public abstract Task<IEnumerable<T>> QueryAsync(NbQuery query);

        /// <summary>
        /// オブジェクトを検索する(カウント付き)
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <param name="queryCount">ヒット件数を取得する場合はtrue</param>
        /// <returns>オブジェクト検索結果</returns>
        public abstract Task<NbObjectQueryResult<T>> QueryWithOptionsAsync(NbQuery query, bool queryCount = false);

        /// <summary>
        /// オブジェクトの一括削除。
        /// 読み込み・削除権限がないオブジェクトは削除されない。
        /// </summary>
        /// <param name="query">削除条件。クエリのConditionを適用する。</param>
        /// <param name="softDelete">論理削除する場合は true (デフォルトは true)</param>
        /// <returns>削除した件数</returns>
        public abstract Task<int> DeleteAsync(NbQuery query, bool softDelete = true);

        /// <summary>
        /// 集計(Aggregation)を実行する。
        /// 集計結果は JSON配列 で返される。
        /// </summary>
        /// <param name="pipeline">Aggregation Pipeline JSON配列</param>
        /// <param name="options">オプション</param>
        /// <returns>Aggregation 実行結果</returns>
        /// <exception cref="ArgumentNullException">Pipelineがnull</exception>
        public abstract Task<NbJsonArray> AggregateAsync(NbJsonArray pipeline, NbJsonObject options = null);

        /// <summary>
        /// バッチリクエストを発行する。
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <param name="softDelete">論理削除を行う場合は true (デフォルトは true)</param>
        /// <returns>バッチ応答のリスト</returns>
        public abstract Task<IList<NbBatchResult>> BatchAsync(NbBatchRequest request, bool softDelete = true);
    }
}