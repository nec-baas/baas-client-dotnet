using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// ユーザ
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbUser
    {
        /// <summary>
        /// ログインモード。
        /// NbUser のログイン、ログアウトメソッドで使用する。
        /// </summary>
        public enum LoginMode
        {
            /// <summary>
            /// オンラインログインのみを行う。
            /// </summary>
            Online,

            /// <summary>
            /// オフラインログインのみを行う。
            /// </summary>
            Offline,

            /// <summary>
            /// ネットワークに接続されていればオンラインログイン、
            /// されていなければオフラインログインを行う。
            /// </summary>
            Auto
        }

        /// <summary>
        /// ユーザID (ObjectId)
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// ユーザ名 (ユーザ識別子、ログインID)
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// E-mail アドレス
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// オプション情報
        /// </summary>
        public NbJsonObject Options { get; set; }

        /// <summary>
        /// 所属グループ一覧
        /// </summary>
        public IList<string> Groups { get; internal set; }

        /// <summary>
        /// 生成日時
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        public string UpdatedAt { get; set; }

        internal NbService Service { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="service">サービス</param>
        public NbUser(NbService service = null)
        {
            Service = service ?? NbService.Singleton;
        }

        /// <summary>
        /// ログインパラメータ
        /// </summary>
        public struct LoginParam
        {
            /// <summary>
            /// ユーザ名
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// E-mailアドレス
            /// </summary>
            public string Email { get; set; }

            /// <summary>
            /// パスワード
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// ワンタイムトークン
            /// </summary>
            public string Token { get; set; }
        }

        /// <summary>
        /// サインアップする。
        /// username, email は事前に設定しておくこと。
        /// </summary>
        /// <param name="password">パスワード</param>
        /// <returns>作成したユーザ情報</returns>
        /// <exception cref="ArgumentNullException">パスワードがnull</exception>
        /// <exception cref="InvalidOperationException">E-mailがnull</exception>
        public async Task<NbUser> SignUpAsync(string password)
        {
            NbUtil.NotNullWithArgument(password, "password");
            NbUtil.NotNullWithInvalidOperation(Email, "No Email");

            var json = new NbJsonObject();
            if (Username != null)
            {
                json[Field.Username] = Username;
            }
            if (Email != null)
            {
                json[Field.Email] = Email;
            }
            if (Options != null)
            {
                json[Field.Options] = Options;
            }
            json[Field.Password] = password;

            var req = Service.RestExecutor.CreateRequest("/users", HttpMethod.Post);
            req.SetJsonBody(json);

            var rjson = await Service.RestExecutor.ExecuteRequestForJson(req);
            return FromJson(Service, rjson);
        }

        /// <summary>
        /// ユーザ名でオンラインログインする
        /// </summary>
        /// <remarks>本APIではオフラインログインキャッシュは生成されない。
        /// キャッシュを生成したい場合は NbOfflineUser を使用すること。</remarks>
        /// <param name="username">ユーザ名</param>
        /// <param name="password">パスワード</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインしたユーザ情報</returns>
        /// <exception cref="ArgumentNullException">ユーザ名、パスワードがnull</exception>
        public static async Task<NbUser> LoginWithUsernameAsync(string username, string password, NbService service = null)
        {
            NbUtil.NotNullWithArgument(username, "username");
            var param = new LoginParam()
            {
                Username = username,
                Password = password
            };
            return await LoginAsync(param, service);
        }

        /// <summary>
        /// Email でオンラインログインする。
        /// </summary>
        /// <remarks>本APIではオフラインログインキャッシュは生成されない。
        /// キャッシュを生成したい場合は NbOfflineUser を使用すること。</remarks>
        /// <param name="email">E-mail</param>
        /// <param name="password">パスワード</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインしたユーザ情報</returns>
        /// <exception cref="ArgumentNullException">E-mail、パスワードがnull</exception>
        public static async Task<NbUser> LoginWithEmailAsync(string email, string password, NbService service = null)
        {
            NbUtil.NotNullWithArgument(email, "email");
            var param = new LoginParam()
            {
                Email = email,
                Password = password
            };
            return await LoginAsync(param, service);
        }

        /// <summary>
        /// オンラインログインする。
        /// </summary>
        /// <remarks>本APIではオフラインログインキャッシュは生成されない。
        /// キャッシュを生成したい場合は NbOfflineUser を使用すること。</remarks>
        /// <param name="param">パスワード</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインしたユーザ情報</returns>
        /// <exception cref="ArgumentException">パラメータ不正</exception>
        public static async Task<NbUser> LoginAsync(LoginParam param, NbService service = null)
        {
            return await _LoginAsyncOnline(param, service);
        }

        protected static async Task<NbUser> _LoginAsyncOnline(LoginParam param, NbService service = null)
        {
            var json = new NbJsonObject();

            if (param.Token == null)
            {
                NbUtil.NotNullWithArgument(param.Password, "password");

                if (param.Username != null)
                {
                    json[Field.Username] = param.Username;
                }
                else if (param.Email != null)
                {
                    json[Field.Email] = param.Email;
                }
                else
                {
                    throw new ArgumentException("Both Username and Email are null");
                }
                json[Field.Password] = param.Password;
            }
            else
            {
                json[Field.OneTimeToken] = param.Token;
            }

            service = service ?? NbService.Singleton;

            var executor = service.RestExecutor;
            var req = executor.CreateRequest("/login", HttpMethod.Post);
            req.SetJsonBody(json);

            var responseJson = await executor.ExecuteRequestForJson(req);
            var user = FromJson(service, responseJson);

            // save session token
            var session = service.SessionInfo;
            session.Set(responseJson.Get<string>(Field.SessionToken), responseJson.Get<long>(Field.Expire), user);

            return user;
        }

        /// <summary>
        /// JSON Object から NbUser へ変換
        /// </summary>
        /// <param name="service">サービス</param>
        /// <param name="json">JSON</param>
        /// <returns>ユーザ情報</returns>
        protected static NbUser FromJson(NbService service, NbJsonObject json)
        {
            var user = new NbUser(service)
            {
                UserId = json.Get<string>(Field.Id),
                Username = json.Get<string>(Field.Username),
                Email = json.Get<string>(Field.Email),
                Options = json.GetJsonObject(Field.Options),
                CreatedAt = json.Get<string>(Field.CreatedAt),
                UpdatedAt = json.Get<string>(Field.UpdatedAt)
            };

            // groups フィールドはログイン時のみ
            if (json.ContainsKey(Field.Groups))
            {
                user.Groups = json.GetArray(Field.Groups).ToList<string>();
            }

            return user;
        }

        /// <summary>
        /// ログイン状態を返す
        /// </summary>
        /// <param name="service">サービス</param>
        /// <returns>ログイン状態なら true</returns>
        public static bool IsLoggedIn(NbService service = null)
        {
            service = service ?? NbService.Singleton;
            return service.SessionInfo.IsAvailable();
        }

        /// <summary>
        /// 現在ログイン中のユーザを返す
        /// </summary>
        /// <param name="service">サービス</param>
        /// <returns>ユーザ情報</returns>
        public static NbUser CurrentUser(NbService service = null)
        {
            service = service ?? NbService.Singleton;
            var si = service.SessionInfo;
            return si.IsAvailable() ? si.CurrentUser : null;
        }

        /// <summary>
        /// ログアウトする
        /// </summary>
        /// <param name="mode">モード</param>
        /// <param name="service">サービス</param>
        /// <returns>なし</returns>
        /// <exception cref="InvalidOperationException">ログイン中でない</exception>
        public static async Task LogoutAsync(LoginMode mode = LoginMode.Online, NbService service = null)
        {
            service = service ?? NbService.Singleton;

            if (!IsLoggedIn(service))
            {
                throw new InvalidOperationException("Not logged in.");
            }

            try
            {
                if (mode == LoginMode.Online ||
                    (mode == LoginMode.Auto && NetworkInterface.GetIsNetworkAvailable()))
                {
                    var executor = service.RestExecutor;

                    var req = executor.CreateRequest("/login", HttpMethod.Delete);

                    // ログアウト実行
                    var json = await executor.ExecuteRequestForJson(req);
                }
            }
            finally
            {
                // セッションクリア
                // ログアウト API が失敗しても、セッションは強制クリアする
                var session = service.SessionInfo;
                session.Clear();
            }
        }

        /// <summary>
        /// ユーザ情報を更新する
        /// </summary>
        /// <param name="password">変更後のパスワード</param>
        /// <returns>更新したユーザ情報</returns>
        /// <exception cref="InvalidOperationException">ユーザIDがnull</exception>
        public async Task<NbUser> SaveAsync(string password = null)
        {
            NbUtil.NotNullWithInvalidOperation(UserId, "No UserId");

            var req = Service.RestExecutor.CreateRequest("/users/{userId}", HttpMethod.Put);
            req.SetUrlSegment("userId", UserId);

            var json = new NbJsonObject
            {
                {Field.Username, Username},
                {Field.Email, Email},
                {Field.Options, Options}
            };
            if (password != null)
            {
                json.Add("password", password);
            }
            req.SetJsonBody(json);

            var result = await Service.RestExecutor.ExecuteRequestForJson(req);
            var user = FromJson(Service, result);

            // ログイン中のユーザ更新の場合、セッショントークンのユーザ情報を更新する
            var currentuser = NbUser.CurrentUser(Service);
            if (currentuser != null && currentuser.UserId == UserId)
            {
                var session = Service.SessionInfo;
                session.Set(session.SessionToken, session.Expire, user);
            }

            return user;
        }

        /// <summary>
        /// ユーザを削除する
        /// </summary>
        /// <returns>Task</returns>
        /// <exception cref="InvalidOperationException">ユーザIDがnull</exception>
        public async Task DeleteAsync()
        {
            NbUtil.NotNullWithInvalidOperation(UserId, "No UserId");
            var req = Service.RestExecutor.CreateRequest("/users/{userId}", HttpMethod.Delete);
            req.SetUrlSegment("userId", UserId);

            await Service.RestExecutor.ExecuteRequestForJson(req);

            // ログイン中のユーザ削除の場合、セッショントークンをクリアする
            var user = NbUser.CurrentUser(Service);
            if (user != null && user.UserId == UserId)
            {
                var session = Service.SessionInfo;
                session.Clear();
            }
        }

        /// <summary>
        /// パスワードリセット (username指定)
        /// </summary>
        /// <param name="username">ユーザ名</param>
        /// <param name="service">サービス</param>
        /// <returns>Task</returns>
        /// <exception cref="ArgumentNullException">ユーザ名がnull</exception>
        public static async Task ResetPasswordWithUsernameAsync(string username, NbService service = null)
        {
            NbUtil.NotNullWithArgument(username, "username");
            await ResetPasswordAsync(username, null, service);
        }

        /// <summary>
        /// パスワードリセット (email指定)
        /// </summary>
        /// <param name="email">E-mail</param>
        /// <param name="service">サービス</param>
        /// <returns>Task</returns>
        /// <exception cref="ArgumentNullException">E-mailがnull</exception>
        public static async Task ResetPasswordWithEmailAsync(string email, NbService service = null)
        {
            NbUtil.NotNullWithArgument(email, "email");
            await ResetPasswordAsync(null, email, service);
        }

        private static async Task ResetPasswordAsync(string username, string email, NbService service)
        {
            service = service ?? NbService.Singleton;
            var executor = service.RestExecutor;

            var req = executor.CreateRequest("/request_password_reset", HttpMethod.Post);
            var json = new NbJsonObject();
            if (username != null)
            {
                json.Add(Field.Username, username);
            }
            if (email != null)
            {
                json.Add(Field.Email, email);
            }
            req.SetJsonBody(json);

            await executor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// ユーザ情報一覧を取得する
        /// </summary>
        /// <param name="username">ユーザ名</param>
        /// <param name="email">E-mail</param>
        /// <param name="service">サービス</param>
        /// <returns>取得したユーザ情報一覧</returns>
        public static async Task<IEnumerable<NbUser>> QueryUserAsync(string username = null, string email = null, NbService service = null)
        {
            service = service ?? NbService.Singleton;
            var executor = service.RestExecutor;

            var req = executor.CreateRequest("/users", HttpMethod.Get);
            if (username != null)
            {
                req.SetQueryParameter(Field.Username, username);
            }
            if (email != null)
            {
                req.SetQueryParameter(Field.Email, email);
            }

            var json = await executor.ExecuteRequestForJson(req);
            var results = json.GetArray("results");

            var users = from result in results select FromJson(service, result as NbJsonObject);
            return users;
        }

        /// <summary>
        /// ユーザ情報を取得する
        /// </summary>
        /// <param name="userId">ユーザID</param>
        /// <param name="service">サービス</param>
        /// <returns>取得したユーザ情報</returns>
        /// <exception cref="ArgumentNullException">ユーザIDがnull</exception>
        public static async Task<NbUser> GetUserAsync(string userId, NbService service = null)
        {
            NbUtil.NotNullWithArgument(userId, "userId");

            service = service ?? NbService.Singleton;
            var executor = service.RestExecutor;

            var req = executor.CreateRequest("/users/{userId}", HttpMethod.Get);
            req.SetUrlSegment("userId", userId);

            var json = await executor.ExecuteRequestForJson(req);
            return FromJson(service, json);
        }

        /// <summary>
        /// ログイン中ユーザの情報を取得する
        /// </summary>
        /// <param name="service">サービス</param>
        /// <returns>取得したユーザ情報</returns>
        /// <exception cref="InvalidOperationException">ログイン中でない</exception>
        public static async Task<NbUser> RefreshCurrentUserAsync(NbService service = null)
        {
            service = service ?? NbService.Singleton;
            if (!IsLoggedIn(service))
            {
                throw new InvalidOperationException("Not logged in.");
            }

            var executor = service.RestExecutor;
            var req = executor.CreateRequest("/users/current", HttpMethod.Get);

            var json = await executor.ExecuteRequestForJson(req);
            var user = FromJson(service, json);
            service.SessionInfo.CurrentUser = user;
            return user;
        }

        /// <summary>
        /// ユーザが ACL に read アクセス可能か調べる
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <returns>アクセス可能であればtrue、不可であればfalse</returns>
        public bool IsAclAccessibleForRead(NbAcl acl)
        {
            return IsAclAccessibleForRead(this, acl);
        }

        /// <summary>
        /// ユーザが ACL に update アクセス可能か調べる
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <returns>アクセス可能であればtrue、不可であればfalse</returns>
        public bool IsAclAccessibleForUpdate(NbAcl acl)
        {
            return IsAclAccessibleForUpdate(this, acl);
        }

        /// <summary>
        /// ユーザが ACL に delete アクセス可能か調べる
        /// </summary>
        /// <param name="acl">ACL</param>
        /// <returns>アクセス可能であればtrue、不可であればfalse</returns>
        public bool IsAclAccessibleForDelete(NbAcl acl)
        {
            return IsAclAccessibleForDelete(this, acl);
        }

        /// <summary>
        /// ユーザ がacl に read アクセス可能か調べる
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <param name="acl">ACL</param>
        /// <returns>アクセス可能であればtrue、不可であればfalse</returns>
        public static bool IsAclAccessibleForRead(NbUser user, NbAcl acl)
        {
            if (IsOwnerMatch(user, acl)) return true;
            return IsAclAccessibleFor(user, acl, "r");
        }

        /// <summary>
        /// ユーザが acl に update アクセス可能か調べる
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <param name="acl">ACL</param>
        /// <returns>アクセス可能であればtrue、不可であればfalse</returns>
        public static bool IsAclAccessibleForUpdate(NbUser user, NbAcl acl)
        {
            if (IsOwnerMatch(user, acl)) return true;
            if (IsAclAccessibleFor(user, acl, "w")) return true;
            return IsAclAccessibleFor(user, acl, "u");
        }

        /// <summary>
        /// ユーザが acl に delete アクセス可能か調べる
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <param name="acl">ACL</param>
        /// <returns>アクセス可能であればtrue、不可であればfalse</returns>
        public static bool IsAclAccessibleForDelete(NbUser user, NbAcl acl)
        {
            if (IsOwnerMatch(user, acl)) return true;
            if (IsAclAccessibleFor(user, acl, "w")) return true;
            return IsAclAccessibleFor(user, acl, "d");
        }

        /// <summary>
        /// ユーザが acl に admin権限を持っているか調べる
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <param name="acl">ACL</param>
        /// <returns>権限があればtrue、なければfalse</returns>
        public static bool IsAclAccessibleForAdmin(NbUser user, NbAcl acl)
        {
            if (IsOwnerMatch(user, acl)) return true;
            return IsAclAccessibleFor(user, acl, "admin");
        }

        private static bool IsOwnerMatch(NbUser user, NbAcl acl)
        {
            if (user == null || user.UserId == null) return false;
            if (acl == null) return false;
            return user.UserId == acl.Owner;
        }

        /// <summary>
        /// ユーザ(認証済みユーザ)が指定されたACLをもつオブジェクトにアクセス可能か調べる
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <param name="acl">ACL</param>
        /// <param name="aclKey">ACL Key</param>
        /// <returns>アクセス可能であればtrue、不可であればfalse</returns>
        private static bool IsAclAccessibleFor(NbUser user, NbAcl acl, string aclKey)
        {
            if (acl == null)
            {
                return false; // fail safe
            }

            ISet<string> set;
            switch (aclKey)
            {
                case "r":
                    set = acl.R;
                    break;
                case "w":
                    set = acl.W;
                    break;
                case "c":
                    set = acl.C;
                    break;
                case "u":
                    set = acl.U;
                    break;
                case "d":
                    set = acl.D;
                    break;
                case "admin":
                    set = acl.Admin;
                    break;
                default:
                    throw new ArgumentException();
            }

            if (set.Contains("g:anonymous"))
            {
                return true;
            }
            if (user == null || user.UserId == null)
            {
                return false; // not authenticated
            }
            if (set.Contains("g:authenticated"))
            {
                return true; // authenticated
            }
            if (set.Contains(user.UserId))
            {
                return true; // user match
            }
            if (user.Groups == null)
            {
                return false;
            }

            // group match
            return user.Groups.Any(@group => set.Contains("g:" + @group));
        }
    }
}
