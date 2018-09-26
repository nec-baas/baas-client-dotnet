using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nec.Nebula
{
    /// <summary>
    /// グループ
    /// </summary>
    /// <remarks>
    /// 本クラスのインスタンスはスレッドセーフではない。
    /// </remarks>
    public class NbGroup
    {
        /// <summary>
        /// Anonymous グループ名 (g:anonymous)
        /// </summary>
        public static readonly string GAnonymous = "g:anonymous";

        /// <summary>
        /// Authenticated グループ名 (g:authenticated)
        /// </summary>
        public static readonly string GAuthenticated = "g:authenticated";

        /// <summary>
        /// グループID
        /// </summary>
        public string GroupId { get; internal set; }

        /// <summary>
        /// グループ名
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// 所属ユーザIDのセット
        /// </summary>
        public ISet<string> Users { get; internal set; }

        /// <summary>
        /// 所属グループ名のセット
        /// </summary>
        public ISet<string> Groups { get; internal set; }

        /// <summary>
        /// ACL
        /// </summary>
        public NbAcl Acl { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        public string CreatedAt { get; internal set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        public string UpdatedAt { get; internal set; }

        /// <summary>
        /// ETag
        /// </summary>
        public string Etag { get; internal set; }

        internal NbService Service { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">グループ名</param>
        /// <param name="service">サービス</param>
        /// <exception cref="ArgumentNullException">グループ名がnull</exception>
        public NbGroup(string name, NbService service = null)
        {
            NbUtil.NotNullWithArgument(name, "name");
            Service = service ?? NbService.Singleton;

            Name = name;

            Users = new HashSet<string>();
            Groups = new HashSet<string>();
        }

        /// <summary>
        /// グループを保存する。
        /// </summary>
        /// <returns>保存したグループ情報</returns>
        public async Task<NbGroup> SaveAsync()
        {
            var json = ToJson();

            var req = Service.RestExecutor.CreateRequest("/groups/{groupName}", HttpMethod.Put);
            req.SetUrlSegment("groupName", Name);
            req.SetJsonBody(json);

            if (Etag != null)
            {
                req.SetQueryParameter(Field.Etag, Etag);
            }

            var result = await Service.RestExecutor.ExecuteRequestForJson(req);
            UpdateWithJson(result);
            return this;
        }

        /// <summary>
        /// JSON Object 表現に変換
        /// </summary>
        /// <returns>NbJsonObject</returns>
        public NbJsonObject ToJson()
        {
            NbJsonObject json = new NbJsonObject
            {
                {Field.Users, Users}, 
                {Field.Groups, Groups}
            };

            if (Acl != null)
            {
                json.Add(Field.Acl, Acl.ToJson());
            }

            return json;
        }

        /// <summary>
        /// JSON Object から NbGroup へ変換
        /// </summary>
        /// <param name="json">JSON</param>
        /// <param name="service">サービス</param>
        /// <returns>グループ情報</returns>
        /// <exception cref="ArgumentNullException">Jsonがnull</exception>
        public static NbGroup FromJson(NbJsonObject json, NbService service = null)
        {
            NbUtil.NotNullWithArgument(json, "json");
            var group = new NbGroup("", service);
            return group.UpdateWithJson(json);
        }

        /// <summary>
        /// JSON Object で NbGroup を更新
        /// </summary>
        /// <param name="json">JSON</param>
        /// <returns>更新したグループ情報</returns>
        /// <exception cref="ArgumentNullException">Jsonがnull</exception>
        public NbGroup UpdateWithJson(NbJsonObject json)
        {
            NbUtil.NotNullWithArgument(json, "json");
            GroupId = json.Get<string>(Field.Id);
            Name = json.Get<string>(Field.Name);

            Users = new HashSet<string>(from x in json.GetArray(Field.Users) select x as string);
            Groups = new HashSet<string>(from x in json.GetArray(Field.Groups) select x as string);

            Acl = new NbAcl(json.GetJsonObject(Field.Acl));

            CreatedAt = json.Get<string>(Field.CreatedAt);
            UpdatedAt = json.Get<string>(Field.UpdatedAt);
            Etag = json.Get<string>(Field.Etag);

            return this;
        }

        /// <summary>
        /// グループ一覧を取得する
        /// </summary>
        /// <param name="service">サービス</param>
        /// <returns>取得したグループ情報一覧</returns>
        public static async Task<IEnumerable<NbGroup>> QueryGroupsAsync(NbService service = null)
        {
            service = service ?? NbService.Singleton;
            var executor = service.RestExecutor;

            var req = executor.CreateRequest("/groups", HttpMethod.Get);

            var json = await executor.ExecuteRequestForJson(req);
            var array = json.GetArray(Field.Results);
            var groups = from t in array select FromJson(t as NbJsonObject, service);
            return groups;
        }

        /// <summary>
        /// グループを取得する
        /// </summary>
        /// <param name="groupName">グループ名</param>
        /// <param name="service">サービス</param>
        /// <returns>取得したグループ情報</returns>
        /// <exception cref="ArgumentNullException">グループ名がnull</exception>
        public static async Task<NbGroup> GetGroupAsync(string groupName, NbService service = null)
        {
            NbUtil.NotNullWithArgument(groupName, "groupName");
            service = service ?? NbService.Singleton;
            var executor = service.RestExecutor;

            var req = executor.CreateRequest("/groups/{groupName}", HttpMethod.Get);
            req.SetUrlSegment("groupName", groupName);

            var json = await executor.ExecuteRequestForJson(req);
            var group = FromJson(json, service);
            return group;
        }

        /// <summary>
        /// グループを削除する
        /// </summary>
        /// <returns>Task</returns>
        public async Task DeleteAsync()
        {
            var req = Service.RestExecutor.CreateRequest("/groups/{groupName}", HttpMethod.Delete);
            req.SetUrlSegment("groupName", Name);

            if (Etag != null)
            {
                req.SetQueryParameter(Field.Etag, Etag);
            }

            await Service.RestExecutor.ExecuteRequestForJson(req);
        }

        /// <summary>
        /// グループメンバ追加
        /// </summary>
        /// <param name="users">ユーザ一覧</param>
        /// <param name="groups">グループ一覧</param>
        /// <returns>グループメンバ追加後のグループ情報</returns>
        public async Task<NbGroup> AddMembersAsync(IEnumerable<string> users, IEnumerable<string> groups)
        {
            return await AddRemoveMembers(users, groups, true);
        }

        /// <summary>
        /// グループメンバ削除
        /// </summary>
        /// <param name="users">ユーザ一覧</param>
        /// <param name="groups">グループ一覧</param>
        /// <returns>グループメンバ削除後のグループ情報</returns>
        public async Task<NbGroup> DeleteMembersAsync(IEnumerable<string> users, IEnumerable<string> groups)
        {
            return await AddRemoveMembers(users, groups, false);
        }

        private async Task<NbGroup> AddRemoveMembers(IEnumerable<string> users, IEnumerable<string> groups, bool isAdd)
        {
            var req = Service.RestExecutor.CreateRequest("/groups/{groupName}/{type}", HttpMethod.Put);
            req.SetUrlSegment("groupName", Name);
            req.SetUrlSegment("type", isAdd ? "addMembers" : "removeMembers");

            var json = new NbJsonObject();
            if (users != null)
            {
                json.Add(Field.Users, users);
            }
            if (groups != null)
            {
                json.Add(Field.Groups, groups);
            }
            req.SetJsonBody(json);

            var result = await Service.RestExecutor.ExecuteRequestForJson(req);
            UpdateWithJson(result);
            return this;
        }
    }
}
