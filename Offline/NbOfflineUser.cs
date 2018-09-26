using Nec.Nebula.Internal;
using Nec.Nebula.Internal.Database;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// オフラインユーザ
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbOfflineUser : NbUser
    {
        /// <summary>
        /// ユーザ名でログインする
        /// </summary>
        /// <param name="username">ユーザ名</param>
        /// <param name="password">パスワード</param>
        /// <param name="mode">ログインモード(デフォルトはOnline)</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインしたユーザ情報</returns>
        /// <exception cref="ArgumentNullException">ユーザ名、パスワードがnull</exception>
        /// <exception cref="UnauthorizedAccessException">前回と異なるユーザでのログイン</exception>
        public static async Task<NbUser> LoginWithUsernameAsync(string username, string password,
            LoginMode mode = LoginMode.Online, NbService service = null)
        {
            NbUtil.NotNullWithArgument(username, "username");
            var param = new NbUser.LoginParam()
            {
                Username = username,
                Password = password
            };
            return await LoginAsync(param, mode, service);
        }

        /// <summary>
        /// Email でログインする
        /// </summary>
        /// <param name="email">E-mail</param>
        /// <param name="password">パスワード</param>
        /// <param name="mode">ログインモード(デフォルトはOnline)</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインしたユーザ情報</returns>
        /// <exception cref="ArgumentNullException">E-mail、パスワードがnull</exception>
        /// <exception cref="UnauthorizedAccessException">前回と異なるユーザでのログイン</exception>
        public static async Task<NbUser> LoginWithEmailAsync(string email, string password,
            LoginMode mode = LoginMode.Online, NbService service = null)
        {
            NbUtil.NotNullWithArgument(email, "email");
            var param = new NbUser.LoginParam()
            {
                Email = email,
                Password = password
            };
            return await NbOfflineUser.LoginAsync(param, mode, service);
        }

        /// <summary>
        /// ログイン
        /// </summary>
        /// <param name="param">ログインパラメータ</param>
        /// <param name="mode">ログインモード(デフォルトはOnline)</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインしたユーザ情報</returns>
        /// <exception cref="ArgumentNullException">必須フィールドがnull</exception>
        /// <exception cref="UnauthorizedAccessException">前回と異なるユーザでのログイン</exception>
        public static async Task<NbUser> LoginAsync(LoginParam param,
            LoginMode mode = LoginMode.Online, NbService service = null)
        {
            NbUtil.NotNullWithArgument(param.Password, "password");
            service = service ?? NbService.Singleton;

            // 前回ログインユーザでない場合、キャッシュが残っているか確認
            // キャッシュが残っていたら"UnauthorizedAccessException"を発行
            if (!CheckLoginUser(param.Username, param.Email, service))
            {
                throw new UnauthorizedAccessException("cache of old user exist");
            }

            if (mode == LoginMode.Offline ||
                (mode == LoginMode.Auto && !NetworkInterface.GetIsNetworkAvailable()))
            {
                return _LoginAsyncOffline(param, service);
            }
            else
            {
                var user = await _LoginAsyncOnline(param, service);

                // ログイン成功：キャッシュ保存
                SaveLoginCache(user, service.SessionInfo, param.Password, service);
                return user;
            }
        }

        /// <summary>
        /// オフラインログイン
        /// </summary>
        /// <param name="param">ログインパラメータ</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインしたユーザ情報</returns>
        private static NbUser _LoginAsyncOffline(LoginParam param, NbService service)
        {
            var login = TryOfflineLogin(param, service);
            if (login == null) throw new NbHttpException(HttpStatusCode.Unauthorized, "Offline login failed");

            var user = new NbUser(service)
            {
                UserId = login.UserId,
                Username = login.UserName,
                Email = login.Email,
                Options = login.Options,
                CreatedAt = login.CreatedAt,
                UpdatedAt = login.UpdatedAt
            };

            // save session token
            var session = service.SessionInfo;
            session.Set(login.SessionToken, login.SessionExpireAt, user);
            return user;
        }

        /// <summary>
        /// オフラインログイン
        /// </summary>
        /// <param name="param">ログインパラメータ</param>
        /// <param name="service">サービス</param>
        /// <returns>ログインキャッシュ</returns>
        private static LoginCache TryOfflineLogin(LoginParam param, NbService service)
        {
            if (!service.IsOfflineEnabled()) return null; // no offline service

            // ログインキャッシュ検索
            var database = (NbDatabaseImpl)service.OfflineService.Database;
            using (var dbContext = database.CreateDbContext())
            {
                var dao = new LoginCacheDao(dbContext);

                LoginCache login = null;
                if (param.Username != null)
                {
                    login = dao.FindByUsername(param.Username);
                }
                else if (param.Email != null)
                {
                    login = dao.FindByEmail(param.Email);
                }
                if (login == null) return null;

                // Expire チェック
                var now = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                if (login.SessionExpireAt <= now)
                {
                    // expired
                    dao.Remove(login);
                    dao.SaveChanges();
                    return null;
                }

                // パスワードチェック
                if (!login.IsValidPassword(param.Password))
                {
                    return null; // password does not match
                }
                return login;
            }
        }

        /// <summary>
        /// ログインキャッシュを保存する。
        /// 以前のキャッシュはすべて破棄される。
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <param name="session">セッショントークン情報</param>
        /// <param name="password">パスワード</param>
        /// <param name="service">サービス</param>
        private static void SaveLoginCache(NbUser user, NbSessionInfo session, string password, NbService service)
        {
            if (!service.IsOfflineEnabled()) return;

            var database = (NbDatabaseImpl)service.OfflineService.Database;
            using (var transaction = database.BeginTransaction())
            {
                using (var dbContext = database.CreateDbContext())
                {
                    var dao = new LoginCacheDao(dbContext);

                    // 全キャッシュ破棄
                    dao.RemoveAll();

                    // キャッシュ生成
                    var login = new LoginCache();

                    login.SetUser(user);
                    login.SetSession(session);
                    login.Password = password;

                    dao.Add(login);
                    dao.SaveChanges();

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// 前回ログインユーザか確認する
        /// ログインキャッシュのユーザと異なり、オブジェクトキャッシュが存在する場合は
        /// ログイン不可とする。
        /// </summary>
        /// <param name="username">ユーザ名</param>
        /// <param name="email">E-mail</param>
        /// <param name="service">サービス</param>
        /// <returns>ログイン要求可能ならtrue、ログイン要求不可ならfalse</returns>
        private static bool CheckLoginUser(string username, string email, NbService service)
        {
            // fale safe
            if (!service.IsOfflineEnabled()) return true;

            var canLogin = true;
            var database = (NbDatabaseImpl)service.OfflineService.Database;
            using (var transaction = database.BeginTransaction())
            {
                using (var dbContext = database.CreateDbContext())
                {
                    var dao = new LoginCacheDao(dbContext);
                    var logincaches = dao.FindAll();
                    if (logincaches.Any(e => (e.UserName != username) && (e.Email != email)))
                    {
                        var _cache = new NbObjectCache(service);
                        if (_cache.IsObjectCacheExists())
                        {
                            canLogin = false;
                        }
                    }
                    transaction.Rollback();
                }
            }
            return canLogin;
        }
    }
}
