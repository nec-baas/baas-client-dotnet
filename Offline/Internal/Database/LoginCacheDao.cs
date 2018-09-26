using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// ログインキャッシュ DAO
    /// </summary>
    internal class LoginCacheDao
    {
        private readonly NbManageDbContext _context;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context">NbManageDbContext</param>
        /// <exception cref="ArgumentNullException">NbManageDbContextがnull</exception>
        public LoginCacheDao(NbManageDbContext context)
        {
            NbUtil.NotNullWithArgument(context, "context");
            _context = context;
        }

        /// <summary>
        /// ユーザ名でログインキャッシュを検索する
        /// </summary>
        /// <param name="username"></param>
        /// <returns>ログインキャッシュ</returns>
        public LoginCache FindByUsername(string username)
        {
            var result = from x in _context.LoginCaches where x.UserName == username select x;
            return result.Any() ? result.First() : null;
        }

        /// <summary>
        /// E-mail でログインキャッシュを検索する
        /// </summary>
        /// <param name="email"></param>
        /// <returns>ログインキャッシュ</returns>
        public LoginCache FindByEmail(string email)
        {
            var result = from x in _context.LoginCaches where x.Email == email select x;
            return result.Any() ? result.First() : null;
        }

        /// <summary>
        /// 全ログインキャッシュを検索する
        /// </summary>
        /// <returns>ログインキャッシュの一覧</returns>
        public IEnumerable<LoginCache> FindAll()
        {
            return from x in _context.LoginCaches select x;
        }

        /// <summary>
        /// エントリを追加する
        /// </summary>
        /// <param name="cache">ログインキャッシュ</param>
        public void Add(LoginCache cache)
        {
            _context.LoginCaches.Add(cache);
        }

        /// <summary>
        /// エントリを削除する
        /// </summary>
        /// <param name="cache">ログインキャッシュ</param>
        public void Remove(LoginCache cache)
        {
            _context.LoginCaches.Remove(cache);
        }

        /// <summary>
        /// 全エントリを削除する
        /// </summary>
        public void RemoveAll()
        {
            _context.LoginCaches.RemoveRange(FindAll());
        }

        /// <summary>
        /// 変更を保存する
        /// </summary>
        /// <returns>変更した件数</returns>
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }
    }
}
