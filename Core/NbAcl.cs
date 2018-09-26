using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;

namespace Nec.Nebula
{
    /// <summary>
    /// ACL
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbAcl : NbAclBase
    {
        // コンストラクタで空が設定される
        private ISet<string> _admin;

        /// <summary>
        /// オーナユーザID
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Admin ユーザID/グループ名
        /// </summary>
        /// <exception cref="ArgumentNullException">Admin ユーザID/グループ名がnull</exception>
        public ISet<string> Admin
        {
            get { return _admin; }
            set { _admin = CheckValue(value, "Admin"); }
        }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public NbAcl()
        {
            Admin = new HashSet<string>();
            Owner = null;
        }

        /// <summary>
        /// ACL の JSON Object 表現から変換
        /// </summary>
        /// <param name="json">JSON Object</param>
        /// <exception cref="ArgumentNullException">JSON Objectがnull</exception>
        public NbAcl(NbJsonObject json)
            : base(json)
        {
            Admin = ConvertJsonArray(json.GetEnumerable("admin"));
            Owner = json.Opt<string>("owner", null);
        }

        /// <inheritdoc/>
        public override NbJsonObject ToJson()
        {
            var json = base.ToJson();

            json["admin"] = new NbJsonArray(Admin);
            json["owner"] = Owner;

            return json;
        }

        /// <summary>
        /// Anonymousアクセス(R/W/Admin)可能な ACL を生成する
        /// </summary>
        /// <returns>ACL</returns>
        public static NbAcl CreateAclForAnonymous()
        {
            return CreateAclFor(NbGroup.GAnonymous);
        }

        /// <summary>
        /// Authenticatedアクセス(R/W/Admin)可能な ACL を生成する
        /// </summary>
        /// <returns>ACL</returns>
        public static NbAcl CreateAclForAuthenticated()
        {
            return CreateAclFor(NbGroup.GAuthenticated);
        }

        /// <summary>
        /// 特定ユーザのみがアクセス可能な ACL を生成する。
        /// Owner も同時に設定される。
        /// </summary>
        /// <param name="user">ユーザ</param>
        /// <returns>ACL</returns>
        /// <exception cref="ArgumentNullException">ユーザ、ユーザIDがnull</exception> 
        public static NbAcl CreateAclForUser(NbUser user)
        {
            NbUtil.NotNullWithArgument(user, "user");
            NbUtil.NotNullWithArgument(user.UserId, "UserId");

            var acl = CreateAclFor(user.UserId);
            acl.Owner = user.UserId;
            return acl;
        }

        /// <summary>
        /// R/W/Adminが同一の ACL を生成する
        /// </summary>
        /// <param name="entry">ユーザIDまたはグループ名</param>
        /// <returns>ACL</returns>
        /// <exception cref="ArgumentNullException">ユーザIDまたはグループ名がnull</exception> 
        public static NbAcl CreateAclFor(string entry)
        {
            NbUtil.NotNullWithArgument(entry, "entry");

            return CreateAclFor(new[] { entry });
        }

        /// <summary>
        /// R/W/Adminが同一のACLを生成する
        /// </summary>
        /// <param name="entries">ユーザID/グループ名のリスト</param>
        /// <returns>ACL</returns>
        /// <exception cref="ArgumentNullException">ユーザID/グループ名のリストがnull</exception> 
        public static NbAcl CreateAclFor(IEnumerable<string> entries)
        {
            NbUtil.NotNullWithArgument(entries, "entries");

            var acl = new NbAcl();
            foreach (var entry in entries)
            {
                acl.R.Add(entry);
                acl.W.Add(entry);
                acl.Admin.Add(entry);
            }
            return acl;
        }

    }
}
