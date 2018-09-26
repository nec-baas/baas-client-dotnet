using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    class ITUtil
    {
        public const string EndpointUrl = "http://sheltie.local.antcloud.biz/ci/v7.0/api";
        public const string TenantId = "5aa24a6c0da090111f973fb8";
        public const string AppId = "5aa24a900da090111f973fbc";
        public const string AppKey = "XWPF6MuX2FXjI2CYoBJjt5eUg1aeAlm9Ivq2TigZ";
        public const string MasterKey = "IQoV4RuBr7ygRcOBxTRgH1Thb5gj9uRf0M2otFmO";

        // for MultiTenant
        public const string TenantId2 = "5aa24ace0da090111f973fbd";
        public const string AppId2 = "5aa24afe0da090111f973fc1";
        public const string AppKey2 = "R7dbgksYBG0Wn4RXDgous88uyEqyUWkLp89ihBvj";
        public const string MasterKey2 = "7tuhnOLed8vKfgkJdYRakFQuDbedwHoFw9PLu8YO";

        // for MultiApp (Client-Push is not allowed)
        public const string AppId3 = "5aa24b270da090111f973fc2";
        public const string AppKey3 = "5g7xN6fqcQfITltvJa0J1NQFtVA0btXHSZuMd3vk";
        public const string MasterKey3 = "3KjIHCQwYWJPkla21ROA4eZNWyxsklPFPhMXMDiT";

        public const string ObjectBucketName = "DotnetITObjectBucket";
        public const string FileBucketName = "DotnetITFileBucket";
        public const string GroupsBucket = "_GROUPS";
        public const string UsersBucket = "_USERS";

        public const string Username = "DotnetITUser";
        public const string Email = "DotnetITUser@example.com";
        public const string Password = "Passw0rD";

        public const string ReasonCode = "reasonCode";
        public const string Detail = "detail";

        public const string ReasonCodeDuplicateFileName = "duplicate_filename";
        public const string ReasonCodeEtagMismatch = "etag_mismatch";
        public const string ReasonCodeFileLocked = "file_locked";

        public const long BytesPerMbyte = 1024 * 1024; // バイト数(1Mbyte)

        public static void InitNebula(NbService service = null, int no = 0)
        {
            try
            {
                service = service ?? NbService.Singleton;
            }
            catch (NullReferenceException)
            {
                // CommonITでNbServiceをDisposeしているので、
                // フェールセーフ処理を入れておく
                service = NbService.GetInstance();
            }

            service.EndpointUrl = EndpointUrl;
            service.DisableOffline();

            // for MultiTenant
            if (1 == no)
            {
                service.TenantId = TenantId2;
                service.AppId = AppId2;
                service.AppKey = AppKey2;
            }
            else
            {
                service.TenantId = TenantId;
                service.AppId = AppId;
                service.AppKey = AppKey;
            }
        }

        public static void UseNormalKey(NbService service = null, int no = 0)
        {
            service = service ?? NbService.Singleton;
            // for MultiTenant
            if (1 == no)
            {
                service.AppKey = AppKey2;
            }
            else
            {
                service.AppKey = AppKey;
            }
        }

        public static void UseMasterKey(NbService service = null, int no = 0)
        {
            service = service ?? NbService.Singleton;
            // for MultiTenant
            if (1 == no)
            {
                service.AppKey = MasterKey2;
            }
            else
            {
                service.AppKey = MasterKey;
            }
        }

        // for MultiApp
        public static void UseAppIDKey(int no = 0, bool isApp = true)
        {
            var service = NbService.Singleton;
            if (1 == no)
            {
                service.AppId = AppId3;
                if (isApp)
                    service.AppKey = AppKey3;
                else
                    service.AppKey = MasterKey3;
            }
            else
            {
                service.AppId = AppId;
                if (isApp)
                    service.AppKey = AppKey;
                else
                    service.AppKey = MasterKey;
            }
        }

        /// <summary>
        /// ACLを比較する
        /// </summary>
        /// <param name="acl1">acl1</param>
        /// <param name="acl1">acl2</param>
        /// <returns>ACLの内容が同じ場合はtrue</returns>
        public static bool CompareAcl(NbAcl acl1, NbAcl acl2)
        {
            // 比較対象が同一参照もしくはどちらもNull
            if (Object.ReferenceEquals(acl1, acl2)) return true;
            // 比較対象の何れかがNull
            if ((acl1 == null) || (acl2 == null)) return false;
            bool ret = true;
            ret &= acl1.R.SetEquals(acl2.R);
            ret &= acl1.W.SetEquals(acl2.W);
            ret &= acl1.U.SetEquals(acl2.U);
            ret &= acl1.C.SetEquals(acl2.C);
            ret &= acl1.D.SetEquals(acl2.D);
            ret &= acl1.Admin.SetEquals(acl2.Admin);
            ret &= (string.Compare(acl1.Owner, acl2.Owner, false) == 0);

            return ret;
        }

        /// <summary>
        /// バケットを作成する<br/>
        /// ACLはR/W/ADMINについてAnonymousを設定。<br/>
        /// ContentACLはR/WについてAnonymousを設定。<br/>
        /// </summary>
        /// <param name="bucketType">バケットタイプ（オブジェクトバケットorファイルバケット)</param>
        /// <param name="bucketName">バケット名</param>
        /// <param name="description">バケットの説明文</param>
        /// <param name="acl">ACL</param>
        /// <param name="contentAcl">ContentACL</param>
        /// <param name="service">Service</param>
        /// <returns>成否</returns>
        public static async Task<bool> CreateBucket(NbBucketManager.BucketType bucketType, string bucketName, string description, NbAcl acl = null, NbContentAcl contentAcl = null, NbService service = null)
        {
            bool result = true;
            var nbService = service ?? NbService.Singleton;

            var bucketManager = new NbBucketManager(bucketType, nbService);

            if (acl == null)
            {
                acl = NbAcl.CreateAclForAnonymous();
            }

            if (contentAcl == null)
            {
                contentAcl = new NbContentAcl(acl.ToJson());
            }

            try
            {
                var jsonObject = await bucketManager.CreateBucketAsync(bucketName, description, acl, contentAcl);
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// NbHttpException発生時にResponseからエラー情報を取得する
        /// </summary>
        /// <param name="response">Response情報</param>
        /// <param name="error">Responseから情報を取得するキー</param>
        /// <returns>エラー情報</returns>
        /// <remarks>JSON形式のデータも文字列で返す</remarks>
        public static string GetErrorInfo(HttpResponseMessage response, string error = "error")
        {
            if (response.Content != null)
            {
                var bodyString = response.Content.ReadAsStringAsync().Result;
                var responseJson = NbJsonObject.Parse(bodyString);
                var value = responseJson.Opt<object>(error, null);
                if (value is string)
                {
                    return (string)value;
                }
                else if (value is NbJsonObject)
                {
                    return ((NbJsonObject)value).ToString();
                }
            }
            return null;
        }

        #region Group

        /// <summary>
        /// 全グループの削除
        /// </summary>
        /// <returns>成否</returns>
        private static async Task<bool> DeleteAllGroups()
        {
            bool result = true;
            try
            {
                var groups = await NbGroup.QueryGroupsAsync();
                foreach (var group in groups)
                {
                    await group.DeleteAsync();
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public static NbAcl GetDefaltGroupAcl()
        {
            var acl = new NbAcl();
            acl.R.Add(NbGroup.GAuthenticated);
            acl.C.Add(NbGroup.GAuthenticated);
            acl.U.Add(NbGroup.GAuthenticated);
            acl.D.Add(NbGroup.GAuthenticated);
            acl.Admin.Add(NbGroup.GAnonymous);
            return acl;
        }

        public static NbContentAcl GetDefaultGroupContentAcl()
        {
            var acl = new NbContentAcl();
            acl.R.Add(NbGroup.GAnonymous);
            acl.W.Add(NbGroup.GAnonymous);
            acl.C.Add(NbGroup.GAnonymous);
            acl.U.Add(NbGroup.GAnonymous);
            acl.D.Add(NbGroup.GAnonymous);
            return acl;
        }

        public static async Task<bool> ResetAclForGroupBuckets()
        {
            return await CreateGroupBucket(GetDefaltGroupAcl(), GetDefaultGroupContentAcl());
        }

        public static async Task<bool> CreateGroupBucket(NbAcl acl = null, NbContentAcl contentAcl = null)
        {
            return await CreateBucket(NbBucketManager.BucketType.Object, GroupsBucket, "IT Bucket for Groups", acl, contentAcl);
        }

        /// <summary>
        /// グループ機能のオンラインデータ初期化<br/>
        /// グループ全削除を行う。
        /// </summary>
        /// <returns>成否</returns>
        public static async Task<bool> InitOnlineGroup()
        {
            var ret = true;
            UseMasterKey();
            var service = NbService.Singleton;
            service.SessionInfo.Clear();
            ret &= await DeleteAllGroups();
            ret &= await ResetAclForGroupBuckets();
            UseNormalKey();
            return ret;
        }
        #endregion

        #region User

        /// <summary>
        /// 全ユーザの削除
        /// </summary>
        /// <returns>成否</returns>
        private static async Task<bool> DeleteAllUsers()
        {
            bool result = true;

            try
            {
                var users = await NbUser.QueryUserAsync();

                foreach (var user in users)
                {
                    await user.DeleteAsync();
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// サインアップする。
        /// </summary>
        /// <returns>ユーザのインスタンス</returns>
        public static async Task<NbUser> SignUpUser(string userName = "foo", string email = "foo@example.com", NbJsonObject options = null)
        {
            var user = new NbUser();
            user.Username = userName;
            user.Email = email;
            if (options != null)
                user.Options = options;

            return await user.SignUpAsync(Password);
        }

        /// <summary>
        /// Emailでログインする。
        /// </summary>
        /// <returns>ユーザのインスタンス</returns>
        public static async Task<NbUser> LoginUser(string email = "foo@example.com", string password = Password)
        {
            return await NbUser.LoginWithEmailAsync(email, password);
        }

        /// <summary>
        /// ユーザ名でログインする。
        /// </summary>
        /// <returns>ユーザのインスタンス</returns>
        public static async Task<NbUser> LoginUserWithUser(string username = "foo", string password = Password)
        {
            return await NbUser.LoginWithUsernameAsync(username, password);
        }

        /// <summary>
        /// ログアウトする。
        /// </summary>
        /// <returns>ユーザのインスタンス</returns>
        public static async Task LogoutUser()
        {
            await Logout();
        }

        /// <summary>
        /// サインアップ・ログインする。
        /// </summary>
        /// <returns>ログイン中ユーザ、ログインに失敗した場合は空のインスタンスが返る</returns>
        public static async Task<NbUser> SignUpAndLogin()
        {
            var user = new NbUser
            {
                Username = Username,
                Email = Email
            };

            return await SignUpAndLogin(user);
        }

        /// <summary>
        /// サインアップ・ログインする。
        /// </summary>
        /// <param name="user">ユーザ</param>
        /// <returns>ログイン中ユーザ、ログインに失敗した場合は空のインスタンスが返る</returns>
        public static async Task<NbUser> SignUpAndLogin(NbUser user)
        {
            NbUser loggedInUser = new NbUser();
            try
            {
                await user.SignUpAsync(Password);
                loggedInUser = await NbUser.LoginWithUsernameAsync(user.Username, Password);
            }
            catch (Exception)
            {
                // do nothing
            }

            return loggedInUser;
        }

        /// <summary>
        /// ログアウトする。
        /// </summary>
        /// <returns>成否</returns>
        public static async Task<bool> Logout()
        {
            bool result = true;

            if (NbUser.IsLoggedIn())
            {
                try
                {
                    await NbUser.LogoutAsync();

                }
                catch (Exception)
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// ユーザ情報の複製を行う
        /// </summary>
        /// <returns>複製したユーザ情報</returns>
        public static NbUser CloneUser(NbUser user)
        {
            var newUser = new NbUser();
            newUser.UserId = user.UserId;
            newUser.Username = user.Username;
            newUser.Email = user.Email;
            newUser.Options = user.Options;
            newUser.Groups = user.Groups;
            newUser.CreatedAt = user.CreatedAt;
            newUser.UpdatedAt = user.UpdatedAt;
            return newUser;
        }

        /// <summary>
        /// REST APIを使用して直接ログアウトを行う
        /// </summary>
        public static void LogoutDirect()
        {
            var executor = NbService.Singleton.RestExecutor;
            var req = executor.CreateRequest("/login", HttpMethod.Delete);
            var json = executor.ExecuteRequestForJson(req).Result;
        }

        public static NbAcl GetDefaltUserAcl()
        {
            var acl = new NbAcl();
            acl.R.Add(NbGroup.GAuthenticated);
            acl.Admin.Add(NbGroup.GAnonymous);
            return acl;
        }

        public static NbContentAcl GetDefaultUserContentAcl()
        {
            var acl = new NbContentAcl();
            acl.R.Add(NbGroup.GAuthenticated);
            acl.C.Add(NbGroup.GAnonymous);
            return acl;
        }

        public static async Task<bool> ResetAclForUserBuckets()
        {
            return await CreateUserBucket(GetDefaltUserAcl(), GetDefaultUserContentAcl());
        }

        public static async Task<bool> CreateUserBucket(NbAcl acl = null, NbContentAcl contentAcl = null)
        {
            return await CreateBucket(NbBucketManager.BucketType.Object, UsersBucket, "IT Bucket for Users", acl, contentAcl);
        }


        /// <summary>
        /// ユーザ機能のオンラインデータ初期化<br/>
        /// ユーザ全削除を行う。
        /// </summary>
        /// <returns>成否</returns>
        public static async Task<bool> InitOnlineUser()
        {
            var ret = true;

            UseMasterKey();

            var service = NbService.Singleton;
            service.SessionInfo.Clear();

            ret &= await DeleteAllUsers();
            ret &= await ResetAclForUserBuckets();

            UseNormalKey();

            return ret;
        }
        #endregion

        #region ObjectStorage
        /// <summary>
        /// バケット内全オブジェクトの削除
        /// </summary>
        /// <param name="bucketName">バケット名</param>
        /// <returns>成否</returns>
        public static async Task<bool> DeleteAllObjects(String bucketName = ObjectBucketName)
        {
            bool result = true;

            var bucket = new NbObjectBucket<NbObject>(bucketName);

            try
            {
                await bucket.DeleteAsync(new NbQuery().DeleteMark(true), false);
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// オブジェクトバケットを作成する<br/>
        /// ACLはR/W/ADMINについてAnonymousを設定。<br/>
        /// ContentACLはR/WについてAnonymousを設定。<br/>
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <param name="contentAcl">ContentACL</param>
        /// <param name="service">Service</param>
        /// <returns>成否</returns>
        public static async Task<bool> CreateObjectBucket(NbAcl acl = null, NbContentAcl contentAcl = null, NbService service = null)
        {
            return await CreateBucket(NbBucketManager.BucketType.Object, ObjectBucketName, "IT Bucket for ObjectStorage", acl, contentAcl, service);
        }

        /// <summary>
        /// オブジェクトストレージ機能のオンラインデータ初期化<br/>
        /// オブジェクトバケットの作成とバケット内のオブジェクト全削除を行う。
        /// </summary>
        /// <returns>成否</returns>
        public static async Task<bool> InitOnlineObjectStorage()
        {
            var ret = true;

            UseMasterKey();

            ret &= await CreateObjectBucket();
            ret &= await DeleteAllObjects();

            UseNormalKey();

            return ret;
        }

        /// <summary>
        /// サーバに指定件数のオブジェクトを生成する
        /// </summary>
        /// <param name="bucket">バケット</param>
        /// <param name="number">生成するオブジェクト数</param>
        /// <returns>サーバのオブジェクト一覧</returns>
        public static async Task<IEnumerable<NbObject>> CreateOnlineObjects(NbObjectBucketBase<NbObject> bucket, int number)
        {
            var batch = new NbBatchRequest();

            for (int i = 0; i < number; i++)
            {
                var obj = bucket.NewObject();
                batch.AddInsertRequest(obj);
            }
            var batchResult = await bucket.BatchAsync(batch);

            var result = from x in batchResult select new NbObject(bucket.BucketName).FromJson(x.Data);

            return result;
        }

        /// <summary>
        /// サーバの指定オブジェクトを更新する
        /// </summary>
        /// <param name="bucket">バケット</param>
        /// <param name="target">更新するオブジェクト一覧</param>
        /// <returns>更新後のオブジェクト一覧</returns>
        public static async Task<IEnumerable<NbObject>> UpdateOnlineObjects(NbObjectBucketBase<NbObject> bucket, IEnumerable<NbObject> target)
        {
            var batch = new NbBatchRequest();

            foreach (var obj in target)
            {
                batch.AddUpdateRequest(obj);
            }
            var batchResult = await bucket.BatchAsync(batch);

            var result = from x in batchResult select new NbObject(bucket.BucketName).FromJson(x.Data);

            return result;
        }

        /// <summary>
        /// サーバの指定オブジェクトを論理削除する
        /// </summary>
        /// <param name="bucket">バケット</param>
        /// <param name="target">論理削除するオブジェクト一覧</param>
        /// <returns>論理削除後のオブジェクト一覧</returns>
        public static async Task<IEnumerable<NbObject>> LogicalDeleteOnlineObjects(NbObjectBucketBase<NbObject> bucket, IEnumerable<NbObject> target)
        {
            var batch = new NbBatchRequest();

            foreach (var obj in target)
            {
                batch.AddDeleteRequest(obj);
            }
            var batchResult = await bucket.BatchAsync(batch, true);
            var result = from x in batchResult select new NbObject(bucket.BucketName).FromJson(x.Data);

            return result;
        }

        /// <summary>
        /// オンラインオブジェクトを作成する
        /// </summary>
        /// <param name="obj">NbObject</param>
        /// <param name="acl">ACL、nullの場合はR/W/ADMINについてAnonymousを設定。</param>
        /// <param name="trySave">SaveAsync()を行う場合は true</param>
        /// <returns>NbObject</returns>
        public static async Task<NbObject> CreateOnlineObject(NbObject obj, NbAcl acl = null, bool trySave = true)
        {
            if (acl == null)
            {
                obj.Acl = NbAcl.CreateAclForAnonymous();
            }
            else
            {
                obj.Acl = acl;
            }

            if (trySave)
            {
                return await obj.SaveAsync();
            }
            else
            {
                return obj;
            }
        }

        /// <summary>
        /// オンラインオブジェクトを作成する（バケットはITUtil.ObjectBucketName固定）
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="key">値</param> 
        /// <param name="acl">ACL、nullの場合はR/W/ADMINについてAnonymousを設定。</param>
        /// <param name="trySave">SaveAsync()を行う場合は true</param>
        /// <returns>NbObject</returns>
        public static async Task<NbObject> CreateOnlineObject(string key, Object value, NbAcl acl = null, bool trySave = true)
        {
            var obj = new NbObject(ITUtil.ObjectBucketName);
            obj[key] = value;

            return await CreateOnlineObject(obj, acl, trySave);
        }

        /// <summary>
        /// オンラインオブジェクトを作成する（バケットはITUtil.ObjectBucketName固定）
        /// </summary>
        /// <param name="json">NbJsonObject</param>
        /// <param name="acl">ACL、nullの場合はR/W/ADMINについてAnonymousを設定。</param>
        /// <param name="trySave">SaveAsync()を行う場合は true</param>
        /// <returns>NbObject</returns>
        public static async Task<NbObject> CreateOnlineObject(NbJsonObject json, NbAcl acl = null, bool trySave = true)
        {
            var obj = new NbObject(ITUtil.ObjectBucketName);
            foreach (var kv in json)
            {
                obj.Add(kv.Key, kv.Value);
            }

            return await CreateOnlineObject(obj, acl, trySave);
        }

        /// <summary>
        /// オフラインオブジェクトを作成する
        /// </summary>
        /// <param name="obj">NbOfflineObject</param>
        /// <param name="acl">ACL、nullの場合はR/W/ADMINについてAnonymousを設定。</param>
        /// <returns>NbObject</returns>
        private static async Task<NbOfflineObject> CreateOfflineObject(NbOfflineObject obj, NbAcl acl = null)
        {
            if (acl == null)
            {
                obj.Acl = NbAcl.CreateAclForAnonymous();
            }
            else
            {
                obj.Acl = acl;
            }

            return (NbOfflineObject)await obj.SaveAsync();
        }

        /// <summary>
        /// オフラインオブジェクトを作成する（バケットはITUtil.ObjectBucketName固定）
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="key">値</param> 
        /// <param name="acl">ACL、nullの場合はR/W/ADMINについてAnonymousを設定。</param>
        /// <returns>NbOfflineObject</returns>
        public static async Task<NbOfflineObject> CreateOfflineObject(string key, Object value, NbAcl acl = null)
        {
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName);
            obj[key] = value;

            return await CreateOfflineObject(obj, acl);
        }

        /// <summary>
        /// オフラインオブジェクトを作成する（バケットはITUtil.ObjectBucketName固定）
        /// </summary>
        /// <param name="json">NbJsonObject</param>
        /// <param name="acl">ACL、nullの場合はR/W/ADMINについてAnonymousを設定。</param>
        /// <returns>NbOfflineObject</returns>
        public static async Task<NbOfflineObject> CreateOfflineObject(NbJsonObject json, NbAcl acl = null)
        {
            var obj = new NbOfflineObject(ITUtil.ObjectBucketName);
            foreach (var kv in json)
            {
                obj.Add(kv.Key, kv.Value);
            }

            return await CreateOfflineObject(obj, acl);
        }

        /// <summary>
        /// データAのオブジェクトを登録する
        /// </summary>
        /// <param name="isOnline">オンラインの場合はtrue</param>  
        /// <param name="acl">ACL</param>
        /// <returns>Task</returns>
        public static async Task CreateObjectsOfDataA(bool isOnline = true, NbAcl acl = null)
        {
            var req = new NbBatchRequest();
            for (int i = 100; i <= 145; i += 5)
            {
                if (isOnline)
                {
                    var robj = await ITUtil.CreateOnlineObject("data1", i, acl, false);
                    req.AddInsertRequest(robj);
                }
                else
                {
                    await ITUtil.CreateOfflineObject("data1", i, acl);
                }
            }
            if (isOnline)
            {
                var bucket = new NbObjectBucket<NbObject>(ObjectBucketName);
                await bucket.BatchAsync(req);
            }
        }

        /// <summary>
        /// データDのオブジェクトを登録する
        /// </summary>
        /// <param name="isOnline">オンラインの場合はtrue</param>  
        /// <returns>Task</returns>
        public static async Task CreateObjectsOfDataD(bool isOnline = true)
        {
            var json1 = new NbJsonObject()
            {
                {"name", "AAA"},
                {"number", 1},
                {"telno", new NbJsonObject()
                    {
                        {"home", 04400000001},
                        {"mobile", 09000000001}
                    }
                }
            };
            var json2 = new NbJsonObject()
            {
                {"name", "BBB"},
                {"number", 2},
                {"telno", new NbJsonObject()
                    {
                        {"home", 04400000002},
                        {"mobile", 09000000002}
                    }
                }
            };

            if (isOnline)
            {
                await ITUtil.CreateOnlineObject(json1);
                await ITUtil.CreateOnlineObject(json2);
            }
            else
            {
                await ITUtil.CreateOfflineObject(json1);
                await ITUtil.CreateOfflineObject(json2);
            }
        }

        /// <summary>
        /// Limitテスト用のオブジェクトを登録する
        /// </summary>
        /// <param name="isOnline">オンラインの場合はtrue</param>  
        /// <param name="acl">ACL</param>
        /// <returns>Task</returns>
        public static async Task CreateObjectsOfLimitTest(bool isOnline = true, NbAcl acl = null)
        {
            var req = new NbBatchRequest();
            for (int i = 1; i <= 200; i++)
            {
                if (isOnline)
                {
                    var robj = await ITUtil.CreateOnlineObject("data1", i, acl, false);
                    req.AddInsertRequest(robj);
                }
                else
                {
                    await ITUtil.CreateOfflineObject("data1", i, acl);
                }
            }
            if (isOnline)
            {
                var bucket = new NbObjectBucket<NbObject>(ObjectBucketName);
                await bucket.BatchAsync(req);
            }
        }
        #endregion

        #region FileStorage
        /// <summary>
        /// 指定ファイルの物理削除
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns>成否</returns>
        private static async Task<bool> DeleteFile(string fileName)
        {
            bool result = true;
            var bucket = new NbFileBucket(FileBucketName);
            try
            {
                var count = await bucket.DeleteFileAsync(fileName);
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// バケット内全ファイルデータの削除
        /// </summary>
        /// <returns>成否</returns>
        private static async Task<bool> DeleteAllFiles()
        {
            bool result = true;
            var bucket = new NbFileBucket(FileBucketName);
            var metadataList = await bucket.GetFilesAsync();

            foreach (var metadata in metadataList)
            {
                result &= await DeleteFile(metadata.Filename);
            }

            return result;
        }

        /// <summary>
        /// ファイルバケットを作成する<br/>
        /// ACLはR/W/ADMINについてAnonymousを設定。<br/>
        /// ContentACLはR/WについてAnonymousを設定。<br/>
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <param name="contentAcl">ContentACL</param>
        /// <param name="service">Service</param>
        /// <returns>成否</returns>
        public static async Task<bool> CreateFileBucket(NbAcl acl = null, NbContentAcl contentAcl = null, NbService service = null)
        {
            return await CreateBucket(NbBucketManager.BucketType.File, FileBucketName, "IT Bucket for FileStorage", acl, contentAcl, service);
        }

        /// <summary>
        /// ファイルストレージ機能のオンラインデータ初期化<br/>
        /// ファイルバケットの作成とバケット内のファイル全削除を行う。
        /// </summary>
        /// <returns>成否</returns>
        public static async Task<bool> InitOnlineFileStorage()
        {
            var ret = true;

            UseMasterKey();

            ret &= await CreateFileBucket();
            ret &= await DeleteAllFiles();

            UseNormalKey();

            return ret;
        }

        /// <summary>
        /// 指定ファイルのアップロード<br/>
        /// "ITUtilities"の文字列をファイルとしてアップロードする。<br/>
        /// ACLはR/W/ADMINについてAnonymousを設定。<br/>
        /// キャッシュ許可。
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns>アップロードファイルのメタデータ。失敗時はNULLを返却する。</returns>
        public static async Task<NbFileMetadata> UploadFile(string fileName)
        {
            return await UploadFile(fileName, NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// 指定ファイルのアップロード<br/>
        /// "ITUtilities"の文字列をファイルとしてアップロードする。<br/>
        /// キャッシュ許可。
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <param name="acl">ACL</param>
        /// <returns>アップロードファイルのメタデータ。失敗時はNULLを返却する。</returns>
        public static async Task<NbFileMetadata> UploadFile(string fileName, NbAcl acl)
        {
            NbFileMetadata result = null;
            var bucket = new NbFileBucket(FileBucketName);
            try
            {
                result = await bucket.UploadNewFileAsync(Encoding.UTF8.GetBytes("ITUtilities"), fileName, "text/plain", acl);

            }
            catch (Exception)
            {
                // do nothing
            }

            return result;
        }

        /// <summary>
        /// バイト配列の取得<br/>
        /// </summary>
        /// <param name="size">バイト数</param>
        /// <returns>バイト配列</returns>
        public static byte[] GetTextBytes(long size)
        {
            var data = new byte[size];
            Random rnd = new Random(123);
            rnd.NextBytes(data);

            return data;
        }

        #endregion
    }
}
