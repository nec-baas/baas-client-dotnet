using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;


namespace Nec.Nebula
{
    /// <summary>
    /// オフラインオブジェクト
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbOfflineObject : NbObject
    {
        private NbObjectCache _cache;
        private ProcessState _processState;

        /// <summary>
        /// 同期状態
        /// </summary>
        public NbSyncState SyncState { get; internal set; }

        /// <summary>
        /// デフォルトコンストラクタ。<br/>
        /// 通常は以下コンストラクタを使用すること。<br/>
        /// <see cref="NbOfflineObject(string,NbService)"/><br/>
        /// <see cref="NbOfflineObject(string,NbJsonObject,NbService)"/>
        /// </summary>
        public NbOfflineObject()
        {
        }

        /// <summary>
        /// コンストラクタ。バケット名から生成。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="service">サービス</param>
        /// <exception cref="InvalidOperationException">オフラインサービスが無効</exception>
        public NbOfflineObject(string bucketName, NbService service = null)
            : base(bucketName, service)
        {
            // base経由でInitがコールされる
        }

        /// <summary>
        /// コンストラクタ。JSON Object から生成。
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <param name="json">JSON Object</param>
        /// <param name="service">サービス</param>
        /// <exception cref="InvalidOperationException">オフラインサービスが無効</exception>
        public NbOfflineObject(string bucketName, NbJsonObject json, NbService service = null)
            : base(bucketName, json, service)
        {
            // base経由でInitがコールされる
        }

        internal override void Init(string bucketName, NbService service)
        {
            base.Init(bucketName, service);
            if (!Service.IsOfflineEnabled())
            {
                throw new InvalidOperationException("Offline service is not enabled");
            }

            _cache = new NbObjectCache(Service);
            _processState = ProcessState.GetInstance();
        }

        /// <summary>
        /// オブジェクトをローカル保存する。
        /// Idが存在しない場合は INSERT、存在する場合は UPDATE となる。
        /// </summary>
        /// <returns>this</returns>
        /// <exception cref="InvalidOperationException">バケット名がnull</exception>
        /// <exception cref="NbHttpException">
        /// <list type="table">
        ///   <item>
        ///     <term><see cref="HttpStatusCode.BadRequest"/></term>
        ///     <description>オブジェクト更新時にACL未設定</description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpStatusCode.Forbidden"/></term>
        ///     <description>ACLの権限不正</description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpStatusCode.NotFound"/></term>
        ///     <description>更新対象のオブジェクトが存在しない</description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpStatusCode.Conflict"/></term>
        ///     <description>ETag不一致</description>
        ///   </item>
        /// </list>
        /// </exception>
        /// <exception cref="NbException">同期処理中</exception>
        public override Task<NbObject> SaveAsync()
        {
            NbUtil.NotNullWithInvalidOperation(BucketName, "No BucketName");

            // Create Object
            if (Id == null)
            {
                EnsureOfflineAcl();
                _cache.InsertObject(this, NbSyncState.Dirty);

                // 保存成功のため状態を更新
                SyncState = NbSyncState.Dirty;

                return Task.FromResult((NbObject)this);
            }

            // Fullupdate Object
            // ACLがnullの場合、不正リクエスト扱いとする
            if (Acl == null)
            {
                throw new NbHttpException(HttpStatusCode.BadRequest, "ACL is null");
            }

            var database = (NbDatabaseImpl)Service.OfflineService.Database;

            if (!_processState.TryStartCrud()) NbUtil.ThrowLockedException();

            try
            {
                using (var transaction = database.BeginTransaction())
                {

                    // Read Object
                    var obj = _cache.FindObject<NbOfflineObject>(BucketName, Id);

                    if (obj != null)
                    {

                        // ACL Check
                        var currentUser = NbUser.CurrentUser(Service);
                        if (!NbUser.IsAclAccessibleForUpdate(currentUser, obj.Acl))
                        {
                            throw new NbHttpException(HttpStatusCode.Forbidden, "No ACL");
                        }

                        if (IsUpdateAcl(obj.Acl) && !NbUser.IsAclAccessibleForAdmin(currentUser, obj.Acl))
                        {
                            throw new NbHttpException(HttpStatusCode.Forbidden, "No ACL");
                        }

                        // ETag Check
                        if (IsConflict(obj))
                        {
                            throw new NbHttpException(HttpStatusCode.Conflict, "ETag Mismatch");
                        }

                    }
                    else
                    {
                        // DBにオブジェクトが保存されていない
                        throw new NbHttpException(HttpStatusCode.NotFound, "No such object");
                    }

                    // Update
                    _cache.UpdateObject(this, NbSyncState.Dirty);

                    // Commit
                    transaction.Commit();

                    // 保存成功のため状態を更新
                    SyncState = NbSyncState.Dirty;
                }
            }
            finally
            {
                _processState.EndCrud();
            }

            return Task.FromResult((NbObject)this);
        }

        /// <summary>
        /// オフライン新規保存時の ACL を設定する
        /// </summary>
        private void EnsureOfflineAcl()
        {
            if (Acl != null) return;

            if (NbUser.IsLoggedIn(Service))
            {
                // Ownerのみ設定されたACLを生成
                var localAcl = new NbAcl();
                localAcl.Owner = NbUser.CurrentUser().UserId;
                Acl = localAcl;
            }
            else
            {
                // Anonymousアクセス(R/W)可能な ACLを生成
                var localAcl = NbAcl.CreateAclForAnonymous();
                // Admin権限は付与しない
                localAcl.Admin = new HashSet<string>();
                Acl = localAcl;
            }
        }

        /// <summary>
        /// オフラインオブジェクトの部分更新はサポートしない。
        /// 常に NotImplementedException をスローする。
        /// </summary>
        /// <param name="json">JSON</param>
        /// <returns>オブジェクト</returns>
        /// <exception cref="NotSupportedException">未サポート</exception>
        public override Task<NbObject> PartUpdateAsync(NbJsonObject json)
        {
            throw new NotSupportedException("PartUpdateAsync for offline object is not supported.");
        }

        /// <summary>
        /// オブジェクトをローカル削除する。
        /// </summary>
        /// <param name="softDelete">論理削除する場合は true (デフォルトは true)</param>
        /// <returns>Task</returns>
        /// <exception cref="InvalidOperationException">Id、バケット名がnull</exception>
        /// <exception cref="NbHttpException">
        /// <list type="table">
        ///   <item>
        ///     <term><see cref="HttpStatusCode.Forbidden"/></term>
        ///     <description>ACLの権限不正</description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpStatusCode.NotFound"/></term>
        ///     <description>更新対象のオブジェクトが存在しない</description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpStatusCode.Conflict"/></term>
        ///     <description>ETag不一致</description>
        ///   </item>
        /// </list>
        /// </exception>
        /// <exception cref="NbException">同期処理中</exception>
        public override Task DeleteAsync(bool softDelete = true)
        {
            NbUtil.NotNullWithInvalidOperation(BucketName, "No BucketName");
            NbUtil.NotNullWithInvalidOperation(Id, "No Id");

            var database = (NbDatabaseImpl)Service.OfflineService.Database;

            if (!_processState.TryStartCrud()) NbUtil.ThrowLockedException();

            try
            {
                using (var transaction = database.BeginTransaction())
                {
                    // Read Object
                    var obj = _cache.FindObject<NbOfflineObject>(BucketName, Id);

                    if (obj != null)
                    {
                        // ACL Check
                        var currentUser = NbUser.CurrentUser(Service);
                        if (!NbUser.IsAclAccessibleForDelete(currentUser, obj.Acl))
                        {
                            throw new NbHttpException(HttpStatusCode.Forbidden, "No ACL");
                        }

                        // ETag Check
                        if (IsConflict(obj))
                        {
                            throw new NbHttpException(HttpStatusCode.Conflict, "ETag Mismatch");
                        }
                    }
                    else
                    {
                        // DBにオブジェクトが保存されていない
                        throw new NbHttpException(HttpStatusCode.NotFound, "No such object");
                    }

                    // Delete
                    if (softDelete)
                    {
                        Deleted = true;
                        _cache.UpdateObject(this, NbSyncState.Dirty);
                    }
                    else
                    {
                        _cache.DeleteObject(this);
                    }

                    // Commit
                    transaction.Commit();

                    // 更新成功のため状態を更新
                    SyncState = NbSyncState.Dirty;
                }
            }
            finally
            {
                _processState.EndCrud();
            }

            return Task.FromResult(this);
        }


        private bool IsConflict(NbOfflineObject cacheObj)
        {

            var ret = false;

            // キャッシュにETag情報がなければ衝突と扱わない
            if (cacheObj.Etag == null)
            {
                return false;
            }

            // ETagが存在し一致しない場合衝突として扱う
            if (Etag == null || !Etag.Equals(cacheObj.Etag))
            {
                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// プロパティのAclと引数のAclの内容を比較する
        /// </summary>
        /// <param name="acl">比較対象のACL</param>
        /// <returns>設定内容が異なればTrue。同一であればFalse。</returns>
        private bool IsUpdateAcl(NbAcl acl)
        {
            // 比較対象が同一参照もしくはどちらもNull
            if (Object.ReferenceEquals(Acl, acl)) return false;

            // 比較対象の何れかがNull
            if ((Acl == null) || (acl == null)) return true;

            bool ret = true;
            ret &= Acl.C.SetEquals(acl.C);
            ret &= Acl.W.SetEquals(acl.W);
            ret &= Acl.R.SetEquals(acl.R);
            ret &= Acl.U.SetEquals(acl.U);
            ret &= Acl.D.SetEquals(acl.D);
            ret &= Acl.Admin.SetEquals(acl.Admin);
            ret &= (String.Compare(Acl.Owner, acl.Owner, false) == 0);

            return !ret;
        }
    }

}
