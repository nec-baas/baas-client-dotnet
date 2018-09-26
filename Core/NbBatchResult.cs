
namespace Nec.Nebula
{
    /// <summary>
    /// バッチリクエスト結果。
    /// オブジェクト１件分の結果を格納する。
    /// </summary>
    public class NbBatchResult
    {
        /// <summary>
        /// リザルトコード
        /// </summary>
        public enum ResultCode
        {
            /// <summary>
            /// 正常に操作が完了した
            /// </summary>
            Ok,
            /// <summary>
            /// 衝突が発生した
            /// </summary>
            Conflict,
            /// <summary>
            /// ACL違反のためアクセス不可
            /// </summary>
            Forbidden,
            /// <summary>
            /// 該当データが存在しない
            /// </summary>
            NotFound,
            /// <summary>
            /// リクエスト不正
            /// </summary>
            BadRequest,
            /// <summary>
            /// サーバエラー
            /// </summary>
            ServerError,
            /// <summary>
            /// 結果不明
            /// </summary>
            Unknown
        }

        private ResultCode ResultCodeFromString(string s)
        {
            switch (s)
            {
                case "ok":
                    return ResultCode.Ok;
                case "conflict":
                    return ResultCode.Conflict;
                case "forbidden":
                    return ResultCode.Forbidden;
                case "notFound":
                    return ResultCode.NotFound;
                case "badRequest":
                    return ResultCode.BadRequest;
                case "serverError":
                    return ResultCode.ServerError;
                default:
                    return ResultCode.Unknown;
            }
        }

        /// <summary>
        /// リーズンコード
        /// </summary>
        public enum ReasonCode
        {
            /// <summary>
            /// 不明(指定なし)
            /// </summary>
            Unspecified,
            /// <summary>
            /// 更新・削除処理衝突
            /// </summary>
            RequestConflicted,
            /// <summary>
            /// ユニーク制約エラー
            /// </summary>
            DuplicateKey,
            /// <summary>
            /// ID衝突
            /// </summary>
            DuplicateId,
            /// <summary>
            /// ETag不一致(楽観ロックエラー)
            /// </summary>
            EtagMismatch,
            /// <summary>
            /// 原因不明
            /// </summary>
            Unknown
        };

        private ReasonCode ReasonCodeFromString(string s)
        {
            switch (s)
            {
                case "unspecified":
                    return ReasonCode.Unspecified;
                case "request_conflicted":
                    return ReasonCode.RequestConflicted;
                case "duplicate_key":
                    return ReasonCode.DuplicateKey;
                case "duplicate_id":
                    return ReasonCode.DuplicateId;
                case "etag_mismatch":
                    return ReasonCode.EtagMismatch;
                default:
                    return ReasonCode.Unknown;
            }
        }

        /// <summary>
        /// オブジェクトID
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// リクエスト結果。
        /// </summary>
        public ResultCode Result { get; internal set; }

        /// <summary>
        /// 衝突発生時の原因コード。
        /// </summary>
        /// <remarks><see cref="Result"/>が<see cref="ResultCode.Conflict"/>の場合に有効</remarks>
        public ReasonCode Reason { get; internal set; }

        /// <summary>
        /// 更新後の ETag値
        /// </summary>
        public string Etag { get; internal set; }

        /// <summary>
        /// 更新後の UpdatedAt 値
        /// </summary>
        public string UpdatedAt { get; internal set; }

        /// <summary>
        /// 更新されたオブジェクトデータ(JSON)
        /// </summary>
        public NbJsonObject Data { get; internal set; }

        /// <summary>
        /// JSON結果から NbBatchResult を生成する
        /// </summary>
        /// <param name="json">JSON</param>
        internal NbBatchResult(NbJsonObject json)
        {
            Id = json.Opt<string>("_id", null);
            Result = ResultCodeFromString(json.Opt<string>("result", null));
            Reason = ReasonCodeFromString(json.Opt<string>("reasonCode", null));
            Etag = json.Opt<string>("etag", null);
            UpdatedAt = json.Opt<string>("updatedAt", null);
            Data = json.GetJsonObject("data");
        }
    }
}