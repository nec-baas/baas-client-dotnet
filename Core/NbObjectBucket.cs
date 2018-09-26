using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// オンラインオブジェクトバケット
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    /// <typeparam name="T">NbObject及びそのサブクラス</typeparam>
    public class NbObjectBucket<T> : NbObjectBucketBase<T> where T : NbObject, new()
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="service">NbService</param>
        /// <exception cref="ArgumentException">バケット名が特定できない</exception>
        public NbObjectBucket(string bucketName = null, NbService service = null)
            : base(bucketName, service)
        {
        }

        /// <summary>
        /// オブジェクトを生成する
        /// </summary>
        /// <returns>新規オブジェクト</returns>
        public override T NewObject()
        {
            var obj = new T();
            obj.Init(BucketName, Service);
            return obj;
        }

        /// <summary>
        /// オブジェクトID検索
        /// </summary>
        /// <param name="objectId">オブジェクトID</param>
        /// <returns>オブジェクト</returns>
        /// <exception cref="ArgumentNullException">オブジェクトIDがnull</exception>
        public override async Task<T> GetAsync(string objectId)
        {
            NbUtil.NotNullWithArgument(objectId, "objectId");

            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}/{id}", HttpMethod.Get);
            req.SetUrlSegment("bucket", BucketName);
            req.SetUrlSegment("id", objectId);

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            var obj = NewObject();
            obj.FromJson(json);

            return obj;
        }

        /// <summary>
        /// オブジェクトを検索する
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <returns>オブジェクト検索結果</returns>
        /// <remarks>
        /// <paramref name="query"/>が未指定の場合、空のクエリが指定されたとみなす。
        /// </remarks>
        public override async Task<IEnumerable<T>> QueryAsync(NbQuery query)
        {
            // パラメータ未設定の場合は、空のクエリが指定されたとみなす
            if (query == null)
            {
                query = new NbQuery();
            }

            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}", HttpMethod.Get);
            req.SetUrlSegment("bucket", BucketName);

            SetQueryParameters(req, query);

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            var objects = JarrayToObjects(json.GetArray(Field.Results));
            return objects;
        }

        /// <summary>
        /// オブジェクトを検索する(カウント付き)
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <param name="queryCount">ヒット件数を取得する場合はtrue</param>
        /// <returns>オブジェクト検索結果</returns>
        /// <remarks>
        /// <paramref name="query"/>が未指定の場合、空のクエリが指定されたとみなす。
        /// </remarks>
        public override async Task<NbObjectQueryResult<T>> QueryWithOptionsAsync(NbQuery query, bool queryCount = false)
        {
            // パラメータ未設定の場合は、空のクエリが指定されたとみなす
            if (query == null)
            {
                query = new NbQuery();
            }

            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}", HttpMethod.Get);
            req.SetUrlSegment("bucket", BucketName);

            SetQueryParameters(req, query);
            if (queryCount)
            {
                req.SetQueryParameter(QueryParam.Count, "1");
            }

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            var result = new NbObjectQueryResult<T>
            {
                Objects = JarrayToObjects(json.GetArray(Field.Results)),
                Count = json.Opt(QueryParam.Count, -1),
                CurrentTime = json.Opt(Field.CurrentTime, "")
            };
            return result;
        }

        private void SetQueryParameters(NbRestRequest request, NbQuery query)
        {
            if (query == null) return;

            // where
            request.SetQueryParameter(QueryParam.Where, query.Conditions.ToString());

            // projection
            var projectionJson = query.ProjectionJson();
            if (projectionJson != null)
            {
                request.SetQueryParameter(QueryParam.Projection, projectionJson.ToString());
            }

            // sort order
            if (query.Order != null && query.Order.Length > 0)
            {
                request.SetQueryParameter(QueryParam.Order, string.Join(",", query.Order));
            }

            // skip, limit
            if (query.SkipValue >= 0)
            {
                request.SetQueryParameter(QueryParam.Skip, query.SkipValue.ToString());
            }
            if (query.LimitValue >= -1) // -1は無限大
            {
                request.SetQueryParameter(QueryParam.Limit, query.LimitValue.ToString());
            }

            // deletemark
            if (query.DeleteMarkValue)
            {
                request.SetQueryParameter(QueryParam.DeleteMark, "1");
            }
        }

        /// <summary>
        /// JSONArray から NbObject list に変換
        /// </summary>
        /// <param name="jarray">JSONArray</param>
        /// <returns>オブジェクトのリスト</returns>
        private IEnumerable<T> JarrayToObjects(IList<object> jarray)
        {
            IList<T> list = new List<T>();
            foreach (var json in jarray)
            {
                var obj = NewObject();
                obj.FromJson(json as NbJsonObject);
                list.Add(obj);
            }
            return list;
        }

        /// <summary>
        /// オブジェクトの一括削除。
        /// 読み込み・削除権限がないオブジェクトは削除されない。
        /// </summary>
        /// <param name="query">削除条件。クエリのConditionを適用する。</param>
        /// <param name="softDelete">論理削除する場合は true (デフォルトは true)</param>
        /// <returns>削除した件数</returns>
        /// <exception cref="ArgumentNullException">削除条件がnull</exception>
        public override async Task<int> DeleteAsync(NbQuery query, bool softDelete = true)
        {
            NbUtil.NotNullWithArgument(query, "query");

            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}", HttpMethod.Delete);
            req.SetUrlSegment("bucket", BucketName);

            req.SetQueryParameter(QueryParam.Where, query.Conditions.ToString());
            if (softDelete)
            {
                req.SetQueryParameter(QueryParam.DeleteMark, "1");
            }

            var json = await Service.RestExecutor.ExecuteRequestForJson(req);
            return json.Get<int>(Field.DeletedObjects);
        }

        /// <summary>
        /// 集計(Aggregation)を実行する。
        /// 集計結果は JSON配列 で返される。
        /// </summary>
        /// <remarks>
        /// <para>複数のアイテムに対して $sort を実行する場合は、以下のように単一アイテムの $sort を複数連結すること。</para>
        /// <para>$sort に複数のアイテムを記載した場合は、ソート順序が保証されない。</para>
        /// <code>
        /// [
        ///     { "$sort": { "item1": 1 } },
        ///     { "$sort": { "item2": -1 } },
        ///     { "$sort": { "item3": 1 } }
        /// ]
        /// </code>
        /// </remarks>
        /// <param name="pipeline">Aggregation Pipeline JSON配列</param>
        /// <param name="options">オプション</param>
        /// <returns>Aggregation 実行結果</returns>
        /// <exception cref="ArgumentNullException">Pipelineがnull</exception>
        public override async Task<NbJsonArray> AggregateAsync(NbJsonArray pipeline, NbJsonObject options = null)
        {
            NbUtil.NotNullWithArgument(pipeline, "pipeline");

            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}/_aggregate", HttpMethod.Post);
            req.SetUrlSegment("bucket", BucketName);

            var jsonObject = new NbJsonObject();
            jsonObject[Field.Pipeline] = pipeline;
            if (options != null)
            {
                jsonObject[Field.Options] = options;
            }

            req.SetJsonBody(jsonObject);

            var aggregateResult = await Service.RestExecutor.ExecuteRequestForJson(req);

            return aggregateResult.GetArray(Field.Results);
        }

        /// <summary>
        /// バッチリクエストを発行する。
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <param name="softDelete">論理削除を行う場合は true (デフォルトは true)</param>
        /// <returns>バッチ応答のリスト</returns>
        /// <exception cref="ArgumentNullException">リクエストがnull</exception>
        /// <exception cref="InvalidOperationException">リクエストにデータが未設定</exception>
        public override async Task<IList<NbBatchResult>> BatchAsync(NbBatchRequest request, bool softDelete = true)
        {
            NbUtil.NotNullWithArgument(request, "request");
            // リクエストが含まれない場合、BadRequestとなるため、事前チェック
            if (request.Requests.Count == 0)
            {
                throw new InvalidOperationException("request contains no data");
            }

            var req = Service.RestExecutor.CreateRequest("/objects/{bucket}/_batch", HttpMethod.Post);
            req.SetUrlSegment("bucket", BucketName);

            req.SetQueryParameter(Field.RequestToken, request.RequestToken);
            if (softDelete)
            {
                req.SetQueryParameter(QueryParam.DeleteMark, "1");
            }

            req.SetJsonBody(request.Json);

            var batchResult = await Service.RestExecutor.ExecuteRequestForJson(req);
            var array = batchResult.GetArray(Field.Results);

            return (from NbJsonObject json in array select new NbBatchResult(json)).ToList();
        }
    }
}
