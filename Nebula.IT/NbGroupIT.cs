using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class NbGroupIT
    {
        IEnumerable<string> mUsers;
        IEnumerable<string> mGroups;

        // 特殊バケット
        private const string ObjectBucket = "GroupIT";

        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
            ITUtil.InitOnlineGroup().Wait();
            ITUtil.InitOnlineUser().Wait();

            mUsers = null;
            mUsers = CreateUsers(3).Result;
            mGroups = null;
            mGroups = CreateGroups(3);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            ITUtil.InitOnlineGroup().Wait();
            ITUtil.InitOnlineUser().Wait();
        }


        [Test]
        public void QueryGroupsEmpty()
        {
            var groups = NbGroup.QueryGroupsAsync().Result;
            //Assert.IsEmpty(groups);
            Assert.AreEqual(3, groups.Count());
        }

        private void CreateTestGroups()
        {
            var acl = NbAcl.CreateAclForAnonymous();

            var group = new NbGroup("group1");
            group.Acl = acl;
            group.Etag = "test";

            var g = group.SaveAsync().Result;
            Assert.NotNull(g);
            Assert.AreEqual("group1", g.Name);
            Assert.IsEmpty(g.Users);
            Assert.IsEmpty(g.Groups);
            Assert.AreEqual(g.Etag, group.Etag);

            // group2 作成
            var group2 = new NbGroup("group2");
            group2.Groups.Add("group1");
            group.Acl = acl;

            g = group2.SaveAsync().Result;
            Assert.True(g.Groups.Contains("group1"));
        }

        [Test]
        public void CreateQueryGetGroup()
        {
            CreateTestGroups();
        }

        [Test]
        public void TestQuery()
        {
            CreateTestGroups();

            var groups = NbGroup.QueryGroupsAsync().Result;
            Assert.AreEqual(5, groups.Count());
        }

        [Test]
        public void TestGet()
        {
            CreateTestGroups();

            var g = NbGroup.GetGroupAsync("group1").Result;
            Assert.AreEqual("group1", g.Name);
            Assert.IsNotNull(g.Etag);

            g = NbGroup.GetGroupAsync("group2").Result;
            Assert.AreEqual("group2", g.Name);
            Assert.IsNotNull(g.Etag);
        }

        [Test]
        public void TestUpdate()
        {
            CreateTestGroups();

            // 名前変更。ETag 付きで更新する。
            var g = NbGroup.GetGroupAsync("group1").Result;
            g.Name = "group11";
            var g2 = g.SaveAsync().Result;
            Assert.AreEqual("group11", g2.Name);
        }

        [Test]
        public void TestAddRemoveMembers()
        {
            CreateTestGroups();

            // メンバ削除
            var g = NbGroup.GetGroupAsync("group2").Result;
            Assert.AreEqual("group1", g.Groups.First());

            var g2 = g.DeleteMembersAsync(null, new[] { "group1" }).Result;
            Assert.IsEmpty(g2.Groups);

            // メンバ追加
            var g3 = g2.AddMembersAsync(null, new[] { "group1" }).Result;
            Assert.AreEqual("group1", g3.Groups.First());
        }


        /**
         * SaveAsync (create)
         **/

        /// <summary>
        /// グループを保存する。（すべて設定、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNomalAll()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", mUsers.ToList(), mGroups.ToList(), acl);
        }

        /// <summary>
        /// グループを保存する。（ユーザ一覧省略、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalNoUsers()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", null, mGroups.ToList(), acl);
        }

        /// <summary>
        /// グループを保存する。（グループ一覧省略、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalNoGroups()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", mUsers.ToList(), null, acl);
        }

        /// <summary>
        /// グループを保存する。（ACL省略、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalNoAcl()
        {
            SaveAsyncCreateCommon("test", mUsers.ToList(), mGroups.ToList(), null);
        }

        /// <summary>
        /// グループを保存する。（ACL省略、未ログイン）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalNoAclNotLoggedIn()
        {
            SaveAsyncCreateCommon("test", mUsers.ToList(), mGroups.ToList(), null, false);
        }

        /// <summary>
        /// グループを保存する。（ユーザ一覧が空、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalEmptyUsers()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", new List<string>(), mGroups.ToList(), acl);
        }

        /// <summary>
        /// グループを保存する。（グループ一覧が空、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsycnCreateNormalEmptyGroups()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", mUsers.ToList(), new List<string>(), acl);
        }

        /// <summary>
        /// グループを保存する。（ACLが空、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalEmptyAcl()
        {
            SaveAsyncCreateCommon("test", mUsers.ToList(), mGroups.ToList(), new NbAcl());
        }

        /// <summary>
        /// グループを保存する。（リクエストボディが空(すべて省略)、ログイン済み）
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalEmptyBody()
        {
            SaveAsyncCreateCommon("test", null, null, null);
        }

        /// <summary>
        /// グループを保存する。（すべて設定、ログイン済み）
        /// 関連するすべてのアクセス権限あり、マスターキー使用
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalAllMasterKey()
        {
            ITUtil.UseMasterKey();
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", mUsers.ToList(), mGroups.ToList(), acl);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを保存する。（バケットcontentACL権限なし）
        /// バケットcontentACLのcreate権限なし、マスターキー使用
        /// 保存できること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateNormalNoPermissionMasterKey()
        {
            ITUtil.UseMasterKey();
            var contentAcl = new NbContentAcl();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", mUsers.ToList(), mGroups.ToList(), acl);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを保存する。（バケットcontentACL権限なし）
        /// バケットcontentACLのcreate権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateExceptionNoPermissionBucketCreate()
        {
            var contentAcl = new NbContentAcl();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", mUsers.ToList(), mGroups.ToList(), acl, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループを保存する。（テナントID不正）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateExceptionInvalidTenantId()
        {
            var acl = NbAcl.CreateAclForAnonymous();

            var service = NbService.Singleton;
            service.TenantId = "dummy";

            var group = new NbGroup("test");
            group.Users = new HashSet<string>(from x in mUsers select x as string);
            group.Groups = new HashSet<string>(from x in mGroups select x as string);
            group.Acl = acl;
            try
            {
                var result = group.SaveAsync().Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// グループを保存する。（アプリケーションKey不正）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateExceptionInvalidAppKey()
        {
            var acl = NbAcl.CreateAclForAnonymous();

            var service = NbService.Singleton;
            service.AppKey = "dummy";

            var group = new NbGroup("test");
            group.Users = new HashSet<string>(from x in mUsers select x as string);
            group.Groups = new HashSet<string>(from x in mGroups select x as string);
            group.Acl = acl;
            try
            {
                var result = group.SaveAsync().Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// グループを保存する。（ユーザ一覧、存在しないユーザID）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateExceptionUnexistedUser()
        {
            var users = new List<string>();
            users.Add("xxx");
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", users, mGroups.ToList(), acl, true, false, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// グループを保存する。（グループ一覧、存在しないグループ名）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateExceptionUnexistedGroup()
        {
            var groups = new List<string>();
            groups.Add("xxx");
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("test", mUsers.ToList(), groups, acl, true, false, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// グループを保存する。（101文字以上グループ名）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateExceptionInvalidNameTooLong()
        {
            var acl = NbAcl.CreateAclForAnonymous();
            SaveAsyncCreateCommon("aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeffffffffffgggggggggghhhhhhhhhhiiiiiiiiiijjjjjjjjjjk",
                mUsers.ToList(), mGroups.ToList(), acl, true, false, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// グループを保存する。（グループ名に禁止文字）
        /// '/' を除く任意の UTF-8 文字列が使用可能
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncCreateExceptionInvalidNameProhibitedCharacter()
        {
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncCreateCommon("test/test", mUsers.ToList(), mGroups.ToList(), acl, true, false, HttpStatusCode.BadRequest);
        }


        /// <summary>
        /// グループの保存テスト共通メソッド
        /// </summary>
        private void SaveAsyncCreateCommon(string gname, List<string> users, List<string> groups, NbAcl acl,
            bool loggedin = true, bool success = true, HttpStatusCode statusCode = HttpStatusCode.OK, string msg = null)
        {
            // サーバ側が自動設定する場合の期待値を用意しておく
            var unspecifiedAcl = new NbAcl();
            if (loggedin)
            {
                // サインイン、ログイン
                var user = ITUtil.SignUpAndLogin().Result;
                if (acl == null)
                {
                    unspecifiedAcl.Owner = user.UserId;
                }
            }
            else
            {
                if (acl == null)
                {
                    unspecifiedAcl.R.Add(NbGroup.GAnonymous);
                    unspecifiedAcl.W.Add(NbGroup.GAnonymous);
                }
            }

            var group = new NbGroup(gname);
            if (users != null)
            {
                group.Users = new HashSet<string>(from x in users select x as string);
            }
            if (groups != null)
            {
                group.Groups = new HashSet<string>(from x in groups select x as string);
            }
            if (acl != null)
            {
                group.Acl = acl;
            }

            try
            {
                var result = group.SaveAsync().Result;
                if (success)
                {
                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.GroupId);
                    Assert.AreEqual(result.Name, gname);
                    if (users != null)
                        Assert.AreEqual(result.Users, group.Users);
                    else
                        Assert.IsEmpty(result.Users);
                    if (groups != null)
                        Assert.AreEqual(result.Groups, group.Groups);
                    else
                        Assert.IsEmpty(result.Groups);
                    if (acl != null)
                        Assert.AreEqual(result.Acl, group.Acl);
                    else
                        Assert.AreEqual(result.Acl.ToJson(), unspecifiedAcl.ToJson());
                    Assert.IsNotNull(result.CreatedAt);
                    Assert.IsNotNull(result.UpdatedAt);
                    Assert.IsNotNull(result.Etag);
                }
                else
                {
                    Assert.Fail("No Exception");
                }
            }
            catch (AggregateException e)
            {
                if (success)
                {
                    Assert.Fail("Exception accord!");
                }
                else
                {
                    var ex = e.InnerException as NbHttpException;
                    Assert.AreEqual(ex.StatusCode, statusCode);
                    if (ex.Response.Content.Headers.ContentLength > 0)
                    {
                        Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                    }
                    if (msg != null) Assert.AreEqual(ex.Message, msg);
                    if (!groups[0].Equals("xxx"))
                        Assert.AreEqual(QueryGrougs().Count(), groups.Count);
                }
            }
        }


        /**
         * SaveAsync (update)
         **/

        /// <summary>
        /// グループを更新する。（すべて更新、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalAll()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, groups, acl, NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// グループを更新する。（ユーザ一覧のみ更新、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalUsers()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            SaveAsyncUpdateCommon("test", users, null, null, null);
        }

        /// <summary>
        /// グループを更新する。（グループ一覧のみ更新、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalGroups()
        {
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            SaveAsyncUpdateCommon("test", null, groups, null, null);
        }

        /// <summary>
        /// グループを更新する。（ACLのみ更新、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalAcl()
        {
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", null, null, acl, NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// グループを更新する。（ユーザ一覧が空、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalEmptyUsers()
        {
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", new List<string>(), groups, acl, NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// グループを更新する。（グループ一覧が空、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalEmptyGroups()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, new List<string>(), acl, NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// グループを更新する。（ACLが空、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalEmptyAcl()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            SaveAsyncUpdateCommon("test", users, groups, new NbAcl(), NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// グループを更新する。（リクエストボディが空、ログイン済み）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalEmptyBody()
        {
            SaveAsyncUpdateCommon("test", null, null, null, null);
        }

        /// <summary>
        /// グループを更新する。（すべて更新、未ログイン）
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalAllNotLoggedIn()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, groups, acl, NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// グループを更新する。（すべて更新、ログイン済み）
        /// 関連するすべてのアクセス権限あり、マスターキー使用
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalAllMasterKey()
        {
            ITUtil.UseMasterKey();
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, groups, acl, NbAcl.CreateAclForAnonymous());
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを更新する。（バケットcontentACL権限なし、ACL更新）
        /// バケットcontentACLのupdate権限なし、対象グループのupdate権限なし
        /// 対象グループのadmin権限なし、マスターキー使用
        /// 更新できること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateNormalNoPermissionMasterKey()
        {
            ITUtil.UseMasterKey();
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.U = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.U = new HashSet<string>();
            oldAcl.W = new HashSet<string>();
            oldAcl.Admin = new HashSet<string>();

            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, groups, acl, oldAcl);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを更新する。（バケットcontentACL権限なし、ACL更新）
        /// バケットcontentACLのupdate権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateExceptionNoPermissionBucketUpdate()
        {
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.U = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, groups, acl, NbAcl.CreateAclForAnonymous(), true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループを更新する。（対象グループ権限なし）
        /// 対象グループのupdate権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateExceptionNoPermissionUpdate()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.U = new HashSet<string>();
            oldAcl.W = new HashSet<string>();
            SaveAsyncUpdateCommon("test", users, groups, acl, oldAcl, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループを更新する。（対象グループ権限なし、ACL更新）
        /// 対象グループのadmin権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateExceptionNoPermissionAdmin()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.Admin = new HashSet<string>();
            SaveAsyncUpdateCommon("test", users, groups, acl, oldAcl, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループを更新する。（更新処理が衝突）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Ignore]
        public void TestSaveAsyncUpdateExceptionRequestConflicted()
        {
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            var users = new List<string>(mUsers);
            users.Add(user.UserId);

            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            group.Groups = new HashSet<string>(from x in groups select x as string);
            var task1 = group.SaveAsync();
            group.Users = new HashSet<string>(from x in users select x as string);
            try
            {
                var result = group.SaveAsync().Result;
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
                Assert.AreEqual(ITUtil.GetErrorInfo(ex.Response, "reasonCode"), "request_conflicted");
            }
        }

        /// <summary>
        /// グループを更新する。（セッショントークン無効）
        /// RESTを直接たたいてサーバからユーザ削除もしくはログアウトを行った後に実施する
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateExceptionInvalidSessionToken()
        {
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());
            var clone = CloneGroup(group);

            ITUtil.SignUpUser().Wait();
            ITUtil.LoginUser().Wait();

            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();

            ITUtil.LogoutDirect();

            group.Users = new HashSet<string>(from x in users select x as string);
            group.Groups = new HashSet<string>(from x in groups select x as string);
            group.Acl = acl;
            try
            {
                var result = group.SaveAsync().Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
            finally
            {
                NbService.Singleton.SessionInfo.Clear();
                ITUtil.LoginUser().Wait();
                Assert.AreEqual(QueryGrougs().Count(), mGroups.Count() + 1);
                var old = GetGroup("test");
                Assert.AreEqual(old.Users, clone.Users);
                Assert.AreEqual(old.Groups, clone.Groups);
                Assert.AreEqual(old.Acl.ToJson(), clone.Acl.ToJson());
            }
        }

        /// <summary>
        /// グループを更新する。（存在しないユーザID）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateExceptionUnexistedUser()
        {
            var users = new List<string>();
            users.Add("xxx");
            var groups = new List<string>(mGroups);
            groups.Remove("testGroup1");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, groups, acl, NbAcl.CreateAclForAnonymous(), true, false, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// グループを更新する。（存在しないグループ名）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestSaveAsyncUpdateExceptionUnexistedGroup()
        {
            var users = new List<string>(mUsers);
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            groups.Add("xxx");
            var acl = NbAcl.CreateAclForAuthenticated();
            SaveAsyncUpdateCommon("test", users, groups, acl, NbAcl.CreateAclForAnonymous(), true, false, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// グループの更新テスト共通メソッド
        /// </summary>
        private void SaveAsyncUpdateCommon(string gname, List<string> users, List<string> groups, NbAcl acl, NbAcl oldAcl,
            bool loggedin = true, bool success = true, HttpStatusCode statusCode = HttpStatusCode.OK, string msg = null)
        {
            // サーバ側が自動設定する場合の期待値を用意しておく
            var unspecifiedAcl = new NbAcl();
            if (loggedin)
            {
                // サインイン、ログイン
                ITUtil.SignUpUser().Wait();
                var user = ITUtil.LoginUser().Result;
            }

            NbGroup group;
            if (gname.Equals("test-test"))
            {
                group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), oldAcl);
                group.Name = gname;
            }
            else
            {
                group = SaveGroup(gname, mUsers.ToList(), mGroups.ToList(), oldAcl);
            }
            var clone = CloneGroup(group);

            if (users != null)
            {
                group.Users = new HashSet<string>(from x in users select x as string);
            }
            if (groups != null)
            {
                group.Groups = new HashSet<string>(from x in groups select x as string);
            }
            if (acl != null)
            {
                group.Acl = acl;
            }

            try
            {
                var result = group.SaveAsync().Result;
                if (success)
                {
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.GroupId, clone.GroupId);
                    Assert.AreEqual(result.Name, gname);
                    if (users != null)
                        Assert.AreEqual(result.Users, group.Users);
                    else
                        Assert.AreEqual(result.Users, clone.Users);
                    if (groups != null)
                        Assert.AreEqual(result.Groups, group.Groups);
                    else
                        Assert.AreEqual(result.Groups, clone.Groups);
                    if (acl != null)
                        Assert.AreEqual(result.Acl, group.Acl);
                    else
                        Assert.AreEqual(result.Acl.ToJson(), clone.Acl.ToJson());
                    Assert.AreEqual(result.CreatedAt, clone.CreatedAt);
                    Assert.Greater(result.UpdatedAt.CompareTo(clone.UpdatedAt), 0);
                    Assert.IsNotNull(result.Etag);
                    Assert.AreNotEqual(result.Etag, clone.Etag);
                }
                else
                {
                    Assert.Fail("No Exception");
                }
            }
            catch (AggregateException e)
            {
                if (success)
                {
                    Assert.Fail("Exception accord!");
                }
                else
                {
                    var ex = e.InnerException as NbHttpException;
                    Assert.AreEqual(ex.StatusCode, statusCode);
                    Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                    if (msg != null) Assert.AreEqual(ex.Message, msg);
                    if (!groups[0].Equals("xxx"))
                        Assert.AreEqual(QueryGrougs().Count(), mGroups.Count() + 1);
                    var old = GetGroup(gname);
                    Assert.AreEqual(old.Users, clone.Users);
                    Assert.AreEqual(old.Groups, clone.Groups);
                    Assert.AreEqual(old.Acl.ToJson(), clone.Acl.ToJson());
                }
            }
        }

        /**
         * QueryGroupsAsync
         **/

        /// <summary>
        /// グループの一覧を取得する。（ログイン済み）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncNormal()
        {
            QueryGroupsAsyncCommon(NbAcl.CreateAclForAnonymous(), 3);
        }

        /// <summary>
        /// グループの一覧を取得する。（未ログイン）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncNormalNotLoggedIn()
        {
            QueryGroupsAsyncCommon(NbAcl.CreateAclForAnonymous(), 3, false);
        }

        /// <summary>
        /// グループの一覧を取得する。（ログイン済み）
        /// 関連するすべてのアクセス権限あり、マスターキー使用
        /// 取得できること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncNormalMasterKey()
        {
            ITUtil.UseMasterKey();
            QueryGroupsAsyncCommon(NbAcl.CreateAclForAnonymous(), 3);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループの一覧を取得する。（検索対象なし、ログイン済み）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncNormalNoGroup()
        {
            QueryGroupsAsyncCommon(NbAcl.CreateAclForAnonymous(), 0);
        }

        /// <summary>
        /// グループの一覧を取得する。（対象グループ権限なし、ログイン済み）
        /// 対象グループのread権限なし、複数件のうち、いくつかだけ権限なし
        /// 取得できること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncNormalNoPermission()
        {
            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.R = new HashSet<string>();
            QueryGroupsAsyncCommon(oldAcl, 2);
        }

        /// <summary>
        /// グループの一覧を取得する。（バケットcontentACL権限なし）
        /// 対象グループのread権限なし、マスターキー使用
        /// 取得できること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncNormalNoPermissionMasterKey()
        {
            ITUtil.UseMasterKey();
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.R = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.R = new HashSet<string>();
            QueryGroupsAsyncCommon(oldAcl, 3);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループの一覧を取得する。（バケットcontentACL権限なし）
        /// バケットcontentACLのread権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncExceptionNoPermissionBucketRead()
        {
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.R = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            QueryGroupsAsyncCommon(NbAcl.CreateAclForAnonymous(), 0, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループの一覧を取得する。（テナントID不正）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncExceptionInvalidTenantId()
        {
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());
            group = SaveGroup("test2", null, null, NbAcl.CreateAclForAnonymous());
            var users = new List<string>(mUsers);
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>(mGroups);
            groups.Add(mGroups.ToList()[0]);
            group = SaveGroup("test3", users, groups, NbAcl.CreateAclForAnonymous());

            NbService.Singleton.TenantId = "dummy";

            try
            {
                var result = NbGroup.QueryGroupsAsync().Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// グループの一覧を取得する。（アプリケーションID不正）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestQueryGroupsAsyncExceptionInvalidAppId()
        {
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());
            group = SaveGroup("test2", null, null, NbAcl.CreateAclForAnonymous());
            var users = new List<string>(mUsers);
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>(mGroups);
            groups.Add(mGroups.ToList()[0]);
            group = SaveGroup("test3", users, groups, NbAcl.CreateAclForAnonymous());

            NbService.Singleton.AppId = "dummy";

            try
            {
                var result = NbGroup.QueryGroupsAsync().Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }


        /// <summary>
        /// グループの検索テスト共通メソッド
        /// </summary>
        private void QueryGroupsAsyncCommon(NbAcl oldAcl, int count, bool loggedin = true, bool success = true,
            HttpStatusCode statusCode = HttpStatusCode.OK, string msg = null)
        {
            var groupsList = new List<NbGroup>();
            if (loggedin)
            {
                ITUtil.SignUpAndLogin().Wait();
            }
            if (count != 0)
            {
                var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), oldAcl);
                groupsList.Add(CloneGroup(group));
                group = SaveGroup("test2", null, null, NbAcl.CreateAclForAnonymous());
                groupsList.Add(CloneGroup(group));
                var users = new List<string>(mUsers);
                users.Add(mUsers.ToList()[0]);
                var groups = new List<string>(mGroups);
                groups.Add(mGroups.ToList()[0]);
                group = SaveGroup("test3", users, groups, NbAcl.CreateAclForAnonymous());
                groupsList.Add(CloneGroup(group));
            }
            else
            {
                foreach (var g in mGroups)
                {
                    DeleteGroup(g);
                }
            }

            try
            {
                var result = NbGroup.QueryGroupsAsync().Result;
                if (success)
                {
                    Assert.NotNull(result);
                    Assert.AreEqual((count == 0 ? 0 : mGroups.Count()) + count, result.Count());
                    foreach (var ret in result)
                    {
                        foreach (var group in groupsList)
                        {
                            if (ret.GroupId.Equals(group.GroupId))
                            {
                                Assert.AreEqual(ret.GroupId, group.GroupId);
                                Assert.AreEqual(ret.Name, group.Name);
                                Assert.AreEqual(ret.Users, group.Users);
                                Assert.AreEqual(ret.Groups, group.Groups);
                                Assert.AreEqual(ret.Acl.ToJson(), group.Acl.ToJson());
                                Assert.AreEqual(ret.CreatedAt, group.CreatedAt);
                                Assert.AreEqual(ret.UpdatedAt, group.UpdatedAt);
                                Assert.AreEqual(ret.Etag, group.Etag);
                            }
                        }
                    }
                }
                else
                {
                    Assert.Fail("No Exception");
                }
            }
            catch (AggregateException e)
            {
                if (success)
                {
                    Assert.Fail("Exception accord!");
                }
                else
                {
                    var ex = e.InnerException as NbHttpException;
                    Assert.AreEqual(ex.StatusCode, statusCode);
                    Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                }
            }

        }

        /**
         * GetGroupAsync
         **/

        /// <summary>
        /// グループを取得する。（ログイン済み、グループを登録しておく）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestGetGroupAsyncNormal()
        {
            GetGroupsAsyncCommon("test", NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// グループを取得する。（未ログイン、グループを登録しておく）
        /// 取得できること
        /// </summary>
        [Test]
        public void TestGetGroupAsyncNormalNotLoggedIn()
        {
            GetGroupsAsyncCommon("test", NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// グループを取得する。（ログイン済み、グループを登録しておく）
        /// 関連するすべてのアクセス権限あり、マスターキー使用
        /// 取得できること
        /// </summary>
        [Test]
        public void TestGetGroupAsyncNormalMasterKey()
        {
            ITUtil.UseMasterKey();
            GetGroupsAsyncCommon("test", NbAcl.CreateAclForAnonymous());
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを取得する。（バケットcontentACL権限なし）
        /// バケットcontentACLのread権限なし、対象グループのread権限なし、マスターキー使用
        /// 取得できること
        /// </summary>
        [Test]
        public void TestGetGroupAsyncNormalNoPermissionMasterKey()
        {
            ITUtil.UseMasterKey();
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.R = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.R = new HashSet<string>();

            GetGroupsAsyncCommon("test", oldAcl);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを取得する。（対象グループ権限なし）
        /// 対象グループのread権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestGetGroupAsyncExceptionNoPermissionRead()
        {
            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.R = new HashSet<string>();
            GetGroupsAsyncCommon("test", oldAcl, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループを取得する。（対象グループが存在しない）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestGetGroupAsyncExceptionUnexistedGroup()
        {
            GetGroupsAsyncCommon("dummy", NbAcl.CreateAclForAnonymous(), true, false, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// グループを取得する。（グループ名が0文字）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestGetGroupAsynExceptionInvalidNameEmpty()
        {
            // ""だとグループ一覧取得となるため、スペースで確認
            GetGroupsAsyncCommon(" ", NbAcl.CreateAclForAnonymous(), true, false, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// グループを取得する。（グループ名がnull）
        /// Exception（ArgumentNullException）が発行されること
        /// </summary>
        [Test]
        public void TestGetGroupAsyncExceptionNameNull()
        {
            try
            {
                var group = NbGroup.GetGroupAsync(null).Result;
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerException is ArgumentNullException);
            }
        }

        /// <summary>
        /// グループの取得テスト共通メソッド
        /// </summary>
        private void GetGroupsAsyncCommon(string groupName, NbAcl oldAcl, bool loggedin = true, bool success = true,
            HttpStatusCode statusCode = HttpStatusCode.OK, string msg = null)
        {
            if (loggedin)
            {
                ITUtil.SignUpAndLogin().Wait();
            }
            var group1 = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), oldAcl);
            var group2 = SaveGroup("test1", null, null, oldAcl);

            try
            {
                var group = NbGroup.GetGroupAsync(groupName).Result;
                if (success)
                {
                    Assert.IsNotNull(group);
                    Assert.AreEqual(group1.GroupId, group.GroupId);
                    Assert.AreEqual(group1.Name, group.Name);
                    Assert.AreEqual(group1.Users, group.Users);
                    Assert.AreEqual(group1.Groups, group.Groups);
                    Assert.AreEqual(group1.Acl.ToJson(), group.Acl.ToJson());
                    Assert.AreEqual(group1.CreatedAt, group.CreatedAt);
                    Assert.AreEqual(group1.UpdatedAt, group.UpdatedAt);
                    Assert.AreEqual(group1.Etag, group.Etag);
                }
                else
                {
                    Assert.Fail("No Exception");
                }
            }
            catch (AggregateException e)
            {
                if (success)
                {
                    Assert.Fail("Exception accord!");
                }
                else
                {
                    var ex = e.InnerException as NbHttpException;
                    Assert.AreEqual(ex.StatusCode, statusCode);
                    Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                }
            }
        }


        /**
         * DeleteAsync
         **/

        /// <summary>
        /// グループを削除する。（ログイン済み）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteAsyncNormal()
        {
            DeleteAsyncCommon("test", NbAcl.CreateAclForAnonymous());
        }

        /// <summary>
        /// グループを削除する。（未ログイン）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteAsyncNormalNotLoggedIn()
        {
            DeleteAsyncCommon("test", NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// グループを削除する。（ログイン済み）
        /// 関連するすべてのアクセス権限あり、マスターキー使用
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteAsyncNormalMasterKey()
        {
            ITUtil.UseMasterKey();
            DeleteAsyncCommon("test", NbAcl.CreateAclForAnonymous());
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを削除する。（バケットcontentACL権限なし）
        /// バケットcontentACLのdelete権限なし、対象グループのdelete権限なし、マスターキー使用
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteAsyncNormalNoPermissionMasterKey()
        {
            ITUtil.UseMasterKey();
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.D = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.D = new HashSet<string>();
            oldAcl.W = new HashSet<string>();

            DeleteAsyncCommon("test", oldAcl);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// グループを削除する。（バケットcontentACL権限なし）
        /// バケットcontentACLのdelete権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncExceptionNoPermissionBucketDelete()
        {
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.D = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            DeleteAsyncCommon("test", NbAcl.CreateAclForAnonymous(), true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループを削除する。（対象グループ権限なし）
        /// 対象グループのdelete権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncExceptionNoPermission()
        {
            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.D = new HashSet<string>();
            oldAcl.W = new HashSet<string>();

            DeleteAsyncCommon("test", oldAcl, false, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// グループを削除する。（ETag不一致）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncExceptionETagMismatch()
        {
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());
            var clone = CloneGroup(group);
            clone.Etag = "testEtag";
            try
            {
                clone.DeleteAsync().Wait();
                Assert.Fail("No Exception");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
                Assert.AreEqual(ITUtil.GetErrorInfo(ex.Response, "reasonCode"), "etag_mismatch");
            }
        }

        /// <summary>
        /// グループを削除する。（対象グループが存在しない）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteAsyncExceptionUnexistedGroup()
        {
            DeleteAsyncCommon("test1", NbAcl.CreateAclForAnonymous());
            var dummy = new NbGroup("test2");
            try
            {
                dummy.DeleteAsync().Wait();
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// グループの取得テスト共通メソッド
        /// </summary>
        private void DeleteAsyncCommon(string groupName, NbAcl oldAcl, bool loggedin = true, bool success = true,
            HttpStatusCode statusCode = HttpStatusCode.OK, string msg = null)
        {
            if (loggedin)
            {
                ITUtil.SignUpAndLogin().Wait();
            }
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), oldAcl);
            var clone = CloneGroup(group);
            var parentGroups = new List<string>();
            parentGroups.Add(clone.Name);
            var parent = SaveGroup("test_parent", null, parentGroups, NbAcl.CreateAclForAnonymous());

            try
            {
                group.DeleteAsync().Wait();
                if (success)
                {
                    var groups = QueryGrougs();
                    Assert.IsNotEmpty(groups);
                    Assert.AreEqual(mGroups.Count() + 1, groups.Count());

                    var parentResult = GetGroup("test_parent");
                    Assert.IsNotNull(parentResult);
                    foreach (var name in parentResult.Groups)
                    {
                        if (name.Equals(clone.Name))
                            Assert.Fail("deleted, but belong to parent group!");
                    }
                }
                else
                {
                    Assert.Fail("No Exception");
                }
            }
            catch (AggregateException e)
            {
                if (success)
                    Assert.Fail("Exception accord!");
                else
                {
                    var ex = e.InnerException as NbHttpException;
                    Assert.AreEqual(ex.StatusCode, statusCode);
                    Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));

                    var groups = QueryGrougs();
                    Assert.IsNotEmpty(groups);
                    Assert.AreEqual(mGroups.Count() + 1 + 1, groups.Count());
                }
            }
        }

        /**
         * AddMembersAsync
         **/

        /// <summary>
        /// 所属ユーザ、グループを追加する。（ログイン済み）
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncNormal()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            var acl = NbAcl.CreateAclForAuthenticated();
            AddDeleteAsyncCommon("test", users, groups, acl, true);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（未ログイン）
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncNormalNotLoggedIn()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            var acl = NbAcl.CreateAclForAnonymous();
            AddDeleteAsyncCommon("test", users, groups, acl, true, false);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（ユーザ一覧のみ追加、ログイン済み）
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncNormalUsers()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var acl = NbAcl.CreateAclForAuthenticated();
            AddDeleteAsyncCommon("test", users, null, acl, true);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（グループ一覧のみ追加、ログイン済み）
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncNormalGroups()
        {
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            var acl = NbAcl.CreateAclForAuthenticated();
            AddDeleteAsyncCommon("test", null, groups, acl, true);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（所属済みのユーザ、グループを追加、ログイン済み）
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExistsUsersGroups()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add("testGroup0");
            var acl = NbAcl.CreateAclForAnonymous();
            AddDeleteAsyncCommon("test", users, groups, acl, true);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（リクエストボディが空、ログイン済み）
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncNormalEmptyBody()
        {
            AddDeleteAsyncCommon("test", null, null, NbAcl.CreateAclForAnonymous(), true);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（すべて追加、ログイン済み）
        /// 関連するすべてのアクセス権限あり、マスターキー使用
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncNormalAllMasterKey()
        {
            ITUtil.UseMasterKey();
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            var acl = NbAcl.CreateAclForAuthenticated();
            AddDeleteAsyncCommon("test", users, groups, acl, true);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（バケットcontentACL権限なし）
        /// バケットcontentACLのupdate権限なし、対象グループのupdate権限なし、マスターキー使用
        /// 追加できること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncNormalNoPermissionMasterKey()
        {
            ITUtil.UseMasterKey();
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.U = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.U = new HashSet<string>();
            oldAcl.W = new HashSet<string>();

            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            AddDeleteAsyncCommon("test", users, groups, oldAcl, true);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（バケットcontentACL権限なし）
        /// バケットcontentACLのupdate権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionNoPermissionBucketUpdate()
        {
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.U = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            var bucket = new NbBucketManager(NbBucketManager.BucketType.Object);
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), true, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（対象グループ権限なし）
        /// 対象グループのupdate権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionNoPermissionUpdate()
        {
            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.U = new HashSet<string>();
            oldAcl.W = new HashSet<string>();

            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            AddDeleteAsyncCommon("test", users, groups, oldAcl, true, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（更新が衝突）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Ignore]
        public void TestAddMembersAsyncExceptionRequestConflicted()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            ITUtil.LoginUser("foo5@example.com").Wait();
            users.Add(user.UserId);
            var groups = new List<string>();
            var addgroup = SaveGroup("testGroup4");
            var addgroup2 = SaveGroup("testGroup5");
            groups.Add(addgroup.Name);
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());
            var task1 = group.AddMembersAsync(null, groups);
            var groups2 = new List<string>();
            groups2.Add(addgroup2.Name);
            try
            {
                var result = group.AddMembersAsync(null, groups2).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
            }
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（テナントID不正）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionInvalidTenantId()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);

            var temp = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());

            NbService.Singleton.TenantId = "dummy";

            try
            {
                var result = temp.AddMembersAsync(users, groups).Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（アプリケーションID不正）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionInvalidAppId()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);

            var temp = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());

            NbService.Singleton.AppId = "dummy";

            try
            {
                var result = temp.AddMembersAsync(users, groups).Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（グループ名が0文字）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionInvalidNameEmpty()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            groups.Add("");

            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), true, true, false, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（対象グループが存在しない）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionUnexistedGroup()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);

            var temp = new NbGroup("test");

            try
            {
                var result = temp.AddMembersAsync(users, groups).Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（存在しないユーザID）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionUnexistedUserID()
        {
            var users = new List<string>();
            users.Add("xxx");
            var groups = new List<string>();
            var group = SaveGroup("testGroup4");
            groups.Add(group.Name);
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), true, true, false, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// 所属ユーザ、グループを追加する。（存在しないグループ名）
        /// Exception（BadRequest）が発行されること
        /// </summary>
        [Test]
        public void TestAddMembersAsyncExceptionUnexistedGroupName()
        {
            var users = new List<string>();
            var user = ITUtil.SignUpUser(null, "foo5@example.com").Result;
            users.Add(user.UserId);
            var groups = new List<string>();
            groups.Add("xxx");
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), true, true, false, HttpStatusCode.BadRequest);
        }


        /**
         * DeleteMembersAsync
         **/

        /// <summary>
        /// 所属ユーザ、グループを削除する。（ログイン済み）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormal()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（未ログイン）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalNotLoggedIn()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), false, false);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（ユーザ一覧のみ削除、ログイン済み）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalUsers()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            AddDeleteAsyncCommon("test", users, null, NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（グループ一覧のみ削除、ログイン済み）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalGroups()
        {
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", null, groups, NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（リクエストボディが空、ログイン済み）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalEmptyBody()
        {
            AddDeleteAsyncCommon("test", null, null, NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（ログイン済み）
        /// 関連するすべてのアクセス権限あり、マスターキー使用
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalAllMasterKey()
        {
            ITUtil.UseMasterKey();
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), false);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（バケットcontentACL権限なし）
        /// バケットcontentACLのupdate権限なし、対象グループのupdate権限なし、マスターキー使用
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalNoPermissionMasterKey()
        {
            ITUtil.UseMasterKey();
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.U = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.U = new HashSet<string>();
            oldAcl.W = new HashSet<string>();

            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", users, groups, oldAcl, false);
            ITUtil.UseNormalKey();
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（存在しないユーザID）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalUnexistedUser()
        {
            var users = new List<string>();
            users.Add("xxx");
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（存在しないグループ名）
        /// 削除できること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncNormalUnexistedGroup()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add("xxx");
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), false);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（バケットcontentACL権限なし）
        /// バケットcontentACLのupdate権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncExceptionNoPermissionBucketUpdate()
        {
            var contentAcl = ITUtil.GetDefaultGroupContentAcl();
            contentAcl.U = new HashSet<string>();
            contentAcl.W = new HashSet<string>();
            ITUtil.CreateGroupBucket(ITUtil.GetDefaltGroupAcl(), contentAcl).Wait();

            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", users, groups, NbAcl.CreateAclForAnonymous(), false, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（対象グループ権限なし）
        /// 対象グループのupdate権限なし
        /// Exception（Forbidden）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncExceptionNoPermissionUpdate()
        {
            var oldAcl = NbAcl.CreateAclForAnonymous();
            oldAcl.U = new HashSet<string>();
            oldAcl.W = new HashSet<string>();

            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            AddDeleteAsyncCommon("test", users, groups, oldAcl, false, true, false, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（更新が衝突）
        /// Exception（Conflict）が発行されること
        /// </summary>
        [Ignore]
        public void TestDeleteMembersAsyncExceptionRequestConflicted()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);
            var group = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());
            var task = group.DeleteMembersAsync(users, groups);
            users = new List<string>();
            users.Add(mUsers.ToList()[1]);
            groups = new List<string>();
            groups.Add(mGroups.ToList()[1]);
            try
            {
                var result = group.DeleteMembersAsync(users, groups).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Conflict);
            }
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（テナントID不正）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncExceptionInvalidTenantId()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);

            var temp = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());

            NbService.Singleton.TenantId = "dummy";

            try
            {
                var result = temp.DeleteMembersAsync(users, groups).Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（アプリケーションKey不正）
        /// Exception（Unauthorized）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncExceptionInvalidAppKey()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);

            var temp = SaveGroup("test", mUsers.ToList(), mGroups.ToList(), NbAcl.CreateAclForAnonymous());

            NbService.Singleton.AppKey = "dummy";

            try
            {
                var result = temp.DeleteMembersAsync(users, groups).Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Unauthorized);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// 所属ユーザ、グループを削除する。（対象グループが存在しない）
        /// Exception（NotFound）が発行されること
        /// </summary>
        [Test]
        public void TestDeleteMembersAsyncExceptionUnexistedGroup()
        {
            var users = new List<string>();
            users.Add(mUsers.ToList()[0]);
            var groups = new List<string>();
            groups.Add(mGroups.ToList()[0]);

            var temp = new NbGroup("test");

            try
            {
                var result = temp.DeleteMembersAsync(users, groups).Result;
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.NotFound);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }


        /// <summary>
        /// 所属グループの追加、削除テスト共通メソッド
        /// </summary>
        private void AddDeleteAsyncCommon(string gname, List<string> users, List<string> groups, NbAcl acl,
            bool isAdd, bool loggedin = true, bool success = true, HttpStatusCode statusCode = HttpStatusCode.OK, string msg = null)
        {
            if (loggedin)
            {
                ITUtil.SignUpAndLogin().Wait();
            }

            var group = SaveGroup(gname, mUsers.ToList(), mGroups.ToList(), acl);
            var clone = CloneGroup(group);

            NbGroup result;
            try
            {
                if (isAdd)
                    result = group.AddMembersAsync(users, groups).Result;
                else
                    result = group.DeleteMembersAsync(users, groups).Result;

                if (success)
                {
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.GroupId, clone.GroupId);
                    Assert.AreEqual(result.Name, clone.Name);
                    Assert.AreEqual(result.Acl.ToJson(), clone.Acl.ToJson());
                    Assert.AreEqual(result.CreatedAt, clone.CreatedAt);
                    Assert.Greater(result.UpdatedAt.CompareTo(clone.UpdatedAt), 0);
                    Assert.IsNotNull(result.Etag);
                    Assert.AreNotEqual(result.Etag, clone.Etag);
                    if (isAdd)
                    {
                        if (users != null)
                        {
                            foreach (var user in users)
                            {
                                if (!result.Users.Contains(user))
                                    Assert.Fail("No add user!");
                            }
                        }
                        else
                        {
                            Assert.AreEqual(result.Users, clone.Users);
                        }
                        if (groups != null)
                        {
                            foreach (var gp in groups)
                            {
                                if (!result.Groups.Contains(gp))
                                    Assert.Fail("No add group!");
                            }
                        }
                        else
                        {
                            Assert.AreEqual(result.Groups, clone.Groups);
                        }
                    }
                    else
                    {
                        if (users != null)
                        {
                            foreach (var user in users)
                            {
                                if (result.Users.Contains(user))
                                    Assert.Fail("Exist del user!");
                            }
                        }
                        else
                        {
                            Assert.AreEqual(result.Users, clone.Users);
                        }
                        if (groups != null)
                        {
                            foreach (var gp in groups)
                            {
                                if (result.Groups.Contains(gp))
                                    Assert.Fail("Exist del group!");
                            }
                        }
                        else
                        {
                            Assert.AreEqual(result.Groups, clone.Groups);
                        }
                    }
                }
                else
                {
                    Assert.Fail("No Exception!");
                }
            }
            catch (AggregateException e)
            {
                if (success)
                    Assert.Fail("Exception accord!");
                else
                {
                    var ex = e.InnerException as NbHttpException;
                    Assert.AreEqual(ex.StatusCode, statusCode);
                    Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
                }
            }
        }

        /**
         * プロパティの確認
         **/

        /// <summary>
        /// グループID設定、取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetIdNormal()
        {
            var group = new NbGroup("text");
            group.GroupId = "12345";
            Assert.AreEqual("12345", group.GroupId);
        }

        /// <summary>
        /// グループID設定、取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetIdNormalInit()
        {
            var group = new NbGroup("text");
            Assert.IsNull(group.GroupId);
        }

        /// <summary>
        /// グループID設定、取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetIdSubnormalNull()
        {
            var group = new NbGroup("text");
            group.GroupId = "12345";
            Assert.AreEqual("12345", group.GroupId);
            group.GroupId = null;
            Assert.IsNull(group.GroupId);
        }

        /// <summary>
        /// グループ名設定、取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupNameNormal()
        {
            var group = new NbGroup("test");
            group.Name = "test_1";
            Assert.AreEqual("test_1", group.Name);
        }

        /// <summary>
        /// グループ名設定、取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupNameNormalInit()
        {
            var group = new NbGroup("test");
            Assert.AreEqual("test", group.Name);
        }

        /// <summary>
        /// グループ名設定、取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupNameSubnormalNull()
        {
            var group = new NbGroup("test");
            group.Name = null;
            Assert.IsNull(group.Name);
        }

        /// <summary>
        /// ユーザ一覧設定、取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetUsersNormal()
        {
            var group = new NbGroup("test");
            group.Users = new HashSet<string>(from x in mUsers select x as string);
            Assert.AreEqual(mUsers, group.Users);
        }

        /// <summary>
        /// ユーザ一覧設定、取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetUsersNormalInit()
        {
            var group = new NbGroup("test");
            Assert.IsEmpty(group.Users);
        }

        /// <summary>
        /// ユーザ一覧設定、取得
        /// 重複した値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetUsersSubnormalOverlap()
        {
            var group = new NbGroup("test");
            group.Users = new HashSet<string>(from x in mUsers select x as string);
            group.Users.Add(mUsers.ToList()[0]);
            Assert.AreEqual(3, group.Users.Count());
        }

        /// <summary>
        /// ユーザ一覧設定、取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetUsersSubnormalNull()
        {
            var group = new NbGroup("test");
            group.Users = null;
            Assert.IsNull(group.Users);
        }

        /// <summary>
        /// グループ一覧設定、取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupsNormal()
        {
            var group = new NbGroup("test");
            group.Groups = new HashSet<string>(from x in mGroups select x as string);
            Assert.AreEqual(mGroups, group.Groups);
        }

        /// <summary>
        /// グループ一覧設定、取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupsNormalInit()
        {
            var group = new NbGroup("test");
            Assert.IsEmpty(group.Groups);
        }

        /// <summary>
        /// グループ一覧設定、取得
        /// 重複した値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupsSubnormalOverlap()
        {
            var group = new NbGroup("test");
            group.Groups = new HashSet<string>(from x in mGroups select x as string);
            group.Groups.Add(mGroups.ToList()[0]);
            Assert.AreEqual(3, group.Groups.Count());

        }

        /// <summary>
        /// グループ一覧設定、取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetGroupsSubnormalNull()
        {
            var group = new NbGroup("test");
            group.Groups = null;
            Assert.IsNull(group.Groups);
        }

        /// <summary>
        /// ACL設定・取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetAclNormal()
        {
            var group = new NbGroup("test");
            group.Acl = NbAcl.CreateAclForAuthenticated();
            Assert.AreEqual(group.Acl.ToJson(), NbAcl.CreateAclForAuthenticated().ToJson());
        }

        /// <summary>
        /// ACL設定・取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetAclNormalInit()
        {
            var group = new NbGroup("test");
            Assert.IsNull(group.Acl);
        }

        /// <summary>
        /// ACL設定・取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetAclSubnormalNull()
        {
            var group = new NbGroup("test");
            group.Acl = null;
            Assert.IsNull(group.Acl);
        }

        /// <summary>
        /// ETag設定・取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetETagNormal()
        {
            var group = new NbGroup("test");
            group.Etag = "testEtag";
            Assert.AreEqual("testEtag", group.Etag);
        }

        /// <summary>
        /// ETag設定・取得
        /// 初期値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetETagNormalInit()
        {
            var group = new NbGroup("test");
            Assert.IsNull(group.Etag);
        }

        /// <summary>
        /// ETag設定・取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetETagSubnormalNull()
        {
            var group = new NbGroup("test");
            group.Etag = null;
            Assert.IsNull(group.Etag);
        }

        /// <summary>
        /// 作成日時設定、取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetCreatedTimeNormal()
        {
            var group = new NbGroup("test");
            group.CreatedAt = "12345";
            Assert.AreEqual("12345", group.CreatedAt);
        }

        /// <summary>
        /// 作成日時設定、取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetCreatedTimeNormalInit()
        {
            var group = new NbGroup("test");
            Assert.IsNull(group.CreatedAt);
        }

        /// <summary>
        /// 作成日時設定、取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetCreatedTimeSubnormalNull()
        {
            var group = new NbGroup("test");
            group.CreatedAt = null;
            Assert.IsNull(group.CreatedAt);
        }

        /// <summary>
        /// 更新日時設定、取得
        /// 値を設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetUpdatedTimeNormal()
        {
            var group = new NbGroup("test");
            group.UpdatedAt = "12345";
            Assert.AreEqual("12345", group.UpdatedAt);
        }

        /// <summary>
        /// 更新日時設定、取得
        /// 初期値を取得できること
        /// </summary>
        [Test]
        public void TestSetGetUpdatedTimeNormalInit()
        {
            var group = new NbGroup("test");
            Assert.IsNull(group.UpdatedAt);
        }

        /// <summary>
        /// 更新日時設定、取得
        /// nullを設定し、取得できること
        /// </summary>
        [Test]
        public void TestSetGetUpdatedTimeSubnormalNull()
        {
            var group = new NbGroup("test");
            group.UpdatedAt = null;
            Assert.IsNull(group.UpdatedAt);
        }


        /**
         * Json出力の確認
         **/

        /// <summary>
        /// Json出力の確認（データあり）
        /// データ通りのJSONを取得すること
        /// </summary>
        [Test]
        public void TestToJsonObjectNormal()
        {
            var group = SaveGroup("test");
            var json = group.ToJson();

            Assert.AreEqual(3, json.Count());
            Assert.AreEqual(group.Users, json[Field.Users]);
            Assert.AreEqual(group.Groups, json[Field.Groups]);
            Assert.AreEqual(group.Acl.ToJson(), json[Field.Acl]);
        }

        /// <summary>
        /// Json出力の確認（すべてnull)
        /// 以下キーの値が空であること
        /// ユーザリストキー、グループリストキー
        /// 以下キーの値がnullであること
        /// ACL、ID、グループ名、作成日時、更新日時
        /// </summary>
        [Test]
        public void TestToJsonObjectSubnormalNull()
        {
            var group = new NbGroup("test");
            var json = group.ToJson();
            Assert.IsEmpty(json.Get<ISet<string>>(Field.Users));
            Assert.IsEmpty(json.Get<ISet<string>>(Field.Groups));
            Assert.IsNull(json.GetJsonObject(Field.Acl));
            Assert.IsNull(json.GetJsonObject(Field.Id));
            Assert.IsNull(json.GetJsonObject(Field.Name));
            Assert.IsNull(json.GetJsonObject(Field.CreatedAt));
            Assert.IsNull(json.GetJsonObject(Field.UpdatedAt));
        }


        /**
         * その他（性能）
         **/

        /// <summary>
        /// グループの入れ子構造
        /// 正常に動作すること
        /// </summary>
        [Test]
        public void TestNestGroupNormal()
        {
            var group = SaveGroup("test");
            var groupA = CloneGroup(group);

            ITUtil.InitOnlineObjectStorage().Wait();

            var objectBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);

            var objectA = objectBucket.NewObject();
            objectA["k"] = "v";
            var aclA = new NbAcl();
            aclA.R.Add("g:" + groupA.Name);
            objectA.Acl = aclA;
            objectA = objectA.SaveAsync().Result;

            ITUtil.SignUpUser().Wait();
            var user = ITUtil.LoginUser().Result;
            var cloneUser = ITUtil.CloneUser(user);

            var users = new List<string>();
            users.Add(cloneUser.UserId);
            group = SaveGroup("test1", users);
            var groupB = CloneGroup(group);

            try
            {
                var result = objectBucket.GetAsync(objectA.Id).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }

            groupA.Groups.Add(groupB.Name);
            group = groupA.SaveAsync().Result;

            try
            {
                var result = objectBucket.GetAsync(objectA.Id).Result;
                Assert.IsNotNull(result);
            }
            catch (AggregateException)
            {
                Assert.Fail("Exception accord!");
            }

            groupA.Groups.Remove(groupB.Name);
            group = groupA.SaveAsync().Result;

            try
            {
                var result = objectBucket.GetAsync(objectA.Id).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }
        }

        /// <summary>
        /// 複数グループへの所属
        /// 正常に動作すること
        /// </summary>
        [Test]
        public void TestBelongToMultipleGroupsNormal()
        {
            ITUtil.SignUpUser().Wait();
            var user = ITUtil.LoginUser().Result;
            var cloneUser = ITUtil.CloneUser(user);

            ITUtil.InitOnlineObjectStorage().Wait();

            var objectBucket = new NbObjectBucket<NbObject>(ITUtil.ObjectBucketName);

            var objectA = objectBucket.NewObject();
            objectA["k"] = "v";
            var aclA = new NbAcl();
            aclA.R.Add("g:testA");
            aclA.Owner = "";
            objectA.Acl = aclA;
            objectA = objectA.SaveAsync().Result;

            var objectB = objectBucket.NewObject();
            objectB["k"] = "v";
            var aclB = new NbAcl();
            aclB.R.Add("g:testB");
            aclB.Owner = "";
            objectB.Acl = aclA;
            objectB = objectB.SaveAsync().Result;

            try
            {
                var result = objectBucket.GetAsync(objectA.Id).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }

            try
            {
                var result = objectBucket.GetAsync(objectB.Id).Result;
                Assert.Fail("No Exception!");
            }
            catch (AggregateException e)
            {
                var ex = e.InnerException as NbHttpException;
                Assert.AreEqual(ex.StatusCode, HttpStatusCode.Forbidden);
                Assert.IsNotNull(ITUtil.GetErrorInfo(ex.Response));
            }

            var users = new List<string>();
            users.Add(cloneUser.UserId);
            var groupA = SaveGroup("testA", users, null, null);
            users = new List<string>();
            users.Add(cloneUser.UserId);
            var groupB = SaveGroup("testB", users, null, null);

            try
            {
                var result = objectBucket.GetAsync(objectA.Id).Result;
                Assert.IsNotNull(result);
            }
            catch (AggregateException)
            {
                Assert.Fail("Exception accord!");
            }

            try
            {
                var result = objectBucket.GetAsync(objectB.Id).Result;
                Assert.IsNotNull(result);
            }
            catch (AggregateException)
            {
                Assert.Fail("Exception accord!");
            }
        }

        /// <summary>
        /// 繰り返し評価
        /// グループの作成、取得、削除を3回繰り返す
        /// </summary>
        [Test]
        public void TestCreateQueryDeleteRepeatNormal()
        {
            int count = 3;
            for (int i = 0; i < count; i++)
            {
                var group = SaveGroup("test");
                Assert.IsNotNull(group);
                var getGroup = GetGroup("test");
                Assert.IsNotNull(getGroup);
                DeleteGroup("test");
                var results = QueryGrougs();
                Assert.AreEqual(mGroups.Count(), results.Count());
            }
        }


        /**
         * Util for GroupIT
         */

        private IEnumerable<NbGroup> QueryGrougs()
        {
            ITUtil.UseMasterKey();
            var result = NbGroup.QueryGroupsAsync().Result;
            ITUtil.UseNormalKey();
            return result;
        }

        private NbGroup SaveGroup(string name, List<string> users = null, List<string> groups = null, NbAcl acl = null)
        {
            var group = new NbGroup(name);
            if (users != null) group.Users = new HashSet<string>(from x in users select x as string);
            if (groups != null) group.Groups = new HashSet<string>(from x in groups select x as string);
            group.Acl = acl;
            return group.SaveAsync().Result;
        }

        private IEnumerable<string> CreateGroups(int count = 1)
        {
            var groups = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var group = SaveGroup("testGroup" + i);
                groups.Add(group.Name);
            }
            return groups;
        }

        private async Task<IEnumerable<string>> CreateUsers(int count = 1)
        {
            var users = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var user = await ITUtil.SignUpUser(null, "foo" + i + "@example.com");
                users.Add(user.UserId);
            }
            return users;
        }

        private NbGroup GetGroup(string gname)
        {
            return NbGroup.GetGroupAsync(gname).Result;
        }

        private NbGroup CloneGroup(NbGroup group)
        {
            var newGroup = new NbGroup(group.Name);
            newGroup.GroupId = group.GroupId;
            newGroup.Users = group.Users;
            newGroup.Groups = group.Groups;
            newGroup.Acl = group.Acl;
            newGroup.CreatedAt = group.CreatedAt;
            newGroup.UpdatedAt = group.UpdatedAt;
            newGroup.Etag = group.Etag;
            return newGroup;
        }

        private void DeleteGroup(string name)
        {
            var group = new NbGroup(name);
            group.DeleteAsync().Wait();
        }

    }
}
