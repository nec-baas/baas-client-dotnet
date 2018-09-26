using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;

namespace Nec.Nebula
{
    /// <summary>
    /// ファイルメタデータ
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbFileMetadata
    {
        /// <summary>
        /// バケット名
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// ファイルID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// ファイル名
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Content Type
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// ファイルサイズ(バイト数)
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// ACL
        /// </summary>
        public NbAcl Acl { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        public string UpdatedAt { get; set; }

        /// <summary>
        /// Meta ETag
        /// </summary>
        public string MetaEtag { get; set; }

        /// <summary>
        /// ファイルETag
        /// </summary>
        public string FileEtag { get; set; }

        /// <summary>
        /// 公開URL
        /// </summary>
        public string PublicUrl { get; set; }

        /// <summary>
        /// キャッシュ禁止フラグ
        /// </summary>
        public bool CacheDisabled { get; set; }

        ///// <summary>
        ///// 削除フラグ
        ///// </summary>
        // public bool Deleted { get; set; }

        /// <summary>
        /// JSON表記からメタデータへの変換
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="json">JSON</param>
        /// <exception cref="ArgumentNullException">バケット名、JSONが未設定</exception>
        /// <exception cref="KeyNotFoundException">JSON不正</exception>
        public NbFileMetadata(string bucketName, NbJsonObject json)
        {
            NbUtil.NotNullWithArgument(bucketName, "bucketName");
            NbUtil.NotNullWithArgument(json, "json");

            BucketName = bucketName;

            Id = json.Get<string>(Field.Id);
            Filename = json.Get<string>(Field.Filename);
            ContentType = json.Get<string>(Field.ContentType);
            Length = json.Get<int>(Field.Length);
            Acl = new NbAcl(json.Get<NbJsonObject>(Field.Acl));

            CreatedAt = json.Get<string>(Field.CreatedAt);
            UpdatedAt = json.Get<string>(Field.UpdatedAt);

            MetaEtag = json.Get<string>(Field.MetaEtag);
            FileEtag = json.Get<string>(Field.FileEtag);

            if (json.ContainsKey(Field.PublicUrl))
            {
                PublicUrl = json.Get<string>(Field.PublicUrl);
            }

            if (json.ContainsKey(Field.CacheDisabled))
            {
                CacheDisabled = json.Get<bool>(Field.CacheDisabled);
            }

            // if (json.ContainsKey(Field.Deleted))
            // {
            //     Deleted = json.Get<bool>(Field.Deleted);
            // }
        }

        /// <summary>
        /// 更新用JSON表記へ変換
        /// </summary>
        /// <returns>JSON</returns>
        internal NbJsonObject ToUpdateJson()
        {
            var json = new NbJsonObject();

            if (Filename != null)
            {
                json[Field.Filename] = Filename;
            }
            if (ContentType != null)
            {
                json[Field.ContentType] = ContentType;
            }
            if (Acl != null)
            {
                json[Field.Acl] = Acl.ToJson();
            }

            json[Field.CacheDisabled] = CacheDisabled;

            return json;
        }
    }
}
