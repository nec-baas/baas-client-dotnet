namespace Nec.Nebula.Internal
{
    /// <summary>
    /// フィールド名定数
    /// </summary>
    internal static class Field
    {
        #region 共通
        // 共通
        public const string Id = "_id";
        public const string Acl = "ACL";
        public const string Etag = "etag";
        public const string CreatedAt = "createdAt";
        public const string UpdatedAt = "updatedAt";
        public const string Deleted = "_deleted";
        public const string Results = "results";
        public const string Options = "options";
        #endregion

        #region ユーザ
        /// <summary>
        /// ユーザ名
        /// </summary>
        public const string Username = "username";
        public const string Email = "email";
        public const string Password = "password";
        public const string SessionToken = "sessionToken";
        public const string Expire = "expire";
        public const string OneTimeToken = "token";
        #endregion

        #region グループ
        /// <summary>
        /// グループ名
        /// </summary>
        public const string Name = "name";
        public const string Users = "users";
        public const string Groups = "groups";
        #endregion

        #region オブジェクトストレージ
        /// <summary>
        /// リクエストトークン
        /// </summary>
        public const string RequestToken = "requestToken";

        /// <summary>
        /// 削除されたオブジェクト数 (response)
        /// </summary>
        public const string DeletedObjects = "deletedObjects";

        /// <summary>
        /// フルアップデート指定 (request)
        /// </summary>
        public const string FullUpdate = "$full_update";

        /// <summary>
        /// サーバ現在時刻 (response)
        /// </summary>
        public const string CurrentTime = "currentTime";

        /// <summary>
        /// Aggregation Pipeline
        /// </summary>
        public const string Pipeline = "pipeline";
        #endregion

        #region ファイルストレージ
        public const string MetaEtag = "metaETag";
        public const string FileEtag = "fileETag";
        public const string Filename = "filename";
        public const string ContentType = "contentType";
        public const string Length = "length";
        public const string PublicUrl = "publicUrl";
        public const string CacheDisabled = "cacheDisabled";
        #endregion

        #region Push
        public const string Query = "query";
        public const string Message = "message";
        public const string AllowedReceivers = "allowedReceivers";
        public const string Badge = "badge";
        public const string Sound = "sound";
        public const string ContentAvailable = "content-available";
        public const string Category = "category";
        public const string Title = "title";
        public const string Uri = "uri";
        public const string SseEventId = "sseEventId";
        public const string SseEventType = "sseEventType";
        #endregion
    }
}
