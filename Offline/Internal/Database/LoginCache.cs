using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// ログインキャッシュエンティティ
    /// </summary>
    public class LoginCache
    {
        /// <summary>
        /// CREATE TABLE SQL文
        /// </summary>
        public const string CreateTableSql =
            "CREATE TABLE IF NOT EXISTS LoginCaches (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId TEXT UNIQUE, UserName TEXT" +
            ", Email TEXT, OptionsJson TEXT, GroupsJson TEXT, PasswordHash TEXT, CreatedAt TEXT, UpdatedAt TEXT, SessionToken TEXT, SessionExpireAt INTEGER)";

        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } // unique

        /// <summary>
        /// User名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// E-mail
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// オプションJSON文字列
        /// </summary>
        public string OptionsJson
        {
            get { return Options != null ? Options.ToString() : "{}"; }
            set { Options = value != null ? NbJsonObject.Parse(value) : null; }
        }

        /// <summary>
        /// オプションJSON
        /// </summary>
        [NotMapped]
        public NbJsonObject Options { get; set; }

        /// <summary>
        /// 所属グループリスト(JSON表現)
        /// </summary>
        internal string GroupsJson
        {
            get
            {
                var array = new NbJsonArray();
                if (Groups != null)
                {
                    array.AddRange(Groups);
                }
                return array.ToString();
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Groups = new List<string>();
                }
                else
                {
                    try
                    {
                        Groups = (from x in NbJsonArray.Parse(value) select (string)x).ToList();
                    }
                    catch (ArgumentException)
                    {
                        // failsafe
                        Groups = new List<string>();
                    }
                }
            }
        }

        /// <summary>
        /// 所属グループリスト(JSON表現)。
        /// </summary>
        [NotMapped]
        public IList<string> Groups { get; internal set; }

        /// <summary>
        /// パスワードハッシュ
        /// </summary>
        public string PasswordHash { get; private set; }

        /// <summary>
        /// パスワード。設定すると PasswordHash が更新される。
        /// </summary>
        [NotMapped]
        public string Password
        {
            set { PasswordHash = CalcPasswordHash(value); }
        }

        /// <summary>
        /// 作成日時
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        public string UpdatedAt { get; set; }

        /// <summary>
        /// セッショントークン
        /// </summary>
        public string SessionToken { get; set; }

        /// <summary>
        /// セッショントークン期限切れ日時
        /// </summary>
        public long SessionExpireAt { get; set; }

        //Stretch count
        private const int HASH_STRETCH_COUNT = 10000;

        /// <summary>
        /// ユーザ情報をセットする
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <exception cref="ArgumentNullException">ユーザ情報がnull</exception> 
        public void SetUser(NbUser user)
        {
            NbUtil.NotNullWithArgument(user, "user");
            UserId = user.UserId;
            UserName = user.Username;
            Email = user.Email;
            Options = user.Options;
            Groups = user.Groups;
            CreatedAt = user.CreatedAt;
            UpdatedAt = user.UpdatedAt;
        }

        /// <summary>
        /// セッション情報をセットする
        /// </summary>
        /// <param name="session">セッション情報</param>
        /// <exception cref="ArgumentNullException">セッション情報がnull</exception> 
        public void SetSession(NbSessionInfo session)
        {
            NbUtil.NotNullWithArgument(session, "session");
            SessionToken = session.SessionToken;
            SessionExpireAt = session.Expire;
        }

        /// <summary>
        /// パスワードを検証する
        /// </summary>
        /// <param name="password">パスワード</param>
        /// <returns>正しいパスワードの場合はtrue、不正なパスワードの場合はfalse</returns>
        /// <exception cref="ArgumentNullException">パスワードがnull</exception> 
        public bool IsValidPassword(string password)
        {
            return CalcPasswordHash(password) == PasswordHash;
        }

        /// <summary>
        /// パスワードハッシュを計算する
        /// </summary>
        private string CalcPasswordHash(string s)
        {
            NbUtil.NotNullWithArgument(s, "password");
            var bytes = Encoding.UTF8.GetBytes(s);
            var sha256 = new SHA256CryptoServiceProvider();

            var hash = sha256.ComputeHash(bytes);

            // Stretch
            for (int i = 0; i < HASH_STRETCH_COUNT; i++)
            {
                hash = sha256.ComputeHash(hash);
            }

            var result = new StringBuilder();
            foreach (var b in hash)
            {
                result.Append(b.ToString("x2"));
            }
            return result.ToString();
        }
    }
}