using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbAclTest
    {
        /**
        * Set/Get Permission
        **/
        /// <summary>
        /// R,W,U,C,D,Admin,Ownerに設定可能なこと
        /// 設定した値が取得できること
        /// </summary>
        [Test]
        public void TestSetGetPermissionNormal()
        {
            var acl = new NbAcl();
            acl.R.Add("r1");
            acl.W.Add("w1");
            acl.U.Add("u1");
            acl.C.Add("c1");
            acl.D.Add("d1");
            acl.Admin.Add("a1");
            acl.Owner = "o1";

            Assert.AreEqual(1, acl.R.Count);
            Assert.True(acl.R.Contains("r1"));
            Assert.AreEqual(1, acl.W.Count);
            Assert.True(acl.W.Contains("w1"));
            Assert.AreEqual(1, acl.U.Count);
            Assert.True(acl.U.Contains("u1"));
            Assert.AreEqual(1, acl.C.Count);
            Assert.True(acl.C.Contains("c1"));
            Assert.AreEqual(1, acl.D.Count);
            Assert.True(acl.D.Contains("d1"));
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.True(acl.Admin.Contains("a1"));
            Assert.AreEqual("o1", acl.Owner);
        }

        /// <summary>
        /// RのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetRExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var acl = new NbAcl();
            acl.R = null;
        }

        /// <summary>
        /// WのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetWExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var acl = new NbAcl();
            acl.W = null;
        }

        /// <summary>
        /// UのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetUExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var acl = new NbAcl();
            acl.U = null;
        }

        /// <summary>
        /// CのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetCExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var acl = new NbAcl();
            acl.C = null;
        }

        /// <summary>
        /// DのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetDExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var acl = new NbAcl();
            acl.D = null;
        }

        /// <summary>
        /// AdminのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetAdminExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var acl = new NbAcl();
            acl.Admin = null;
        }

        /// <summary>
        /// OwnerのSetter（nullを設定）
        /// nullが設定可能なこと
        /// </summary>
        [Test]
        public void TestSetOwnerNormalNull()
        {
            var acl = new NbAcl();
            acl.Owner = null;

            Assert.IsNull(acl.Owner);
        }

        /**
        * Constructor
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// R,W,U,C,D,Adminが空のセットであること
        /// OwnerがNullであること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var acl = new NbAcl();

            Assert.IsEmpty(acl.R);
            Assert.IsEmpty(acl.W);
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.IsEmpty(acl.Admin);
            Assert.IsNull(acl.Owner);
        }

        /**
        * Constructor 引数がjson
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// R,W,U,C,D,Admin,Ownerには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonNormal()
        {
            var json = NbJsonObject.Parse("{r:['u1', 'u2'], w:['u3'], u:['u4'], c:['u5'], d:['u6'], admin:['u7'], owner:'u8'}");
            var acl = new NbAcl(json);

            Assert.AreEqual(2, acl.R.Count);
            Assert.True(acl.R.Contains("u1"));
            Assert.True(acl.R.Contains("u2"));
            Assert.AreEqual(1, acl.W.Count);
            Assert.True(acl.W.Contains("u3"));
            Assert.AreEqual(1, acl.U.Count);
            Assert.True(acl.U.Contains("u4"));
            Assert.AreEqual(1, acl.C.Count);
            Assert.True(acl.C.Contains("u5"));
            Assert.AreEqual(1, acl.D.Count);
            Assert.True(acl.D.Contains("u6"));
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.True(acl.Admin.Contains("u7"));
            Assert.AreEqual("u8", acl.Owner);
        }

        /// <summary>
        /// コンストラクタテスト（jsonが空）
        /// R,W,U,C,D,Adminが空のセットであること
        /// OwnerがNullであること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonNormalEmpty()
        {
            var json = new NbJsonObject();
            var acl = new NbAcl(json);

            Assert.IsEmpty(acl.R);
            Assert.IsEmpty(acl.W);
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.IsNull(acl.Owner);
            Assert.IsEmpty(acl.Admin);
        }

        /// <summary>
        /// コンストラクタテスト（余剰パラメータ）
        /// jsonに余計なKeyがあっても無視されること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonSubnormalWrongKeys()
        {
            var json = NbJsonObject.Parse("{r:['u1'], w:['u2'], u:['u3'], c:['u4'], d:['u5'], admin:['u6'], owner:'u7', test:'u8'}");
            var acl = new NbAcl(json);

            Assert.AreEqual(1, acl.R.Count);
            Assert.True(acl.R.Contains("u1"));
            Assert.AreEqual(1, acl.W.Count);
            Assert.True(acl.W.Contains("u2"));
            Assert.AreEqual(1, acl.U.Count);
            Assert.True(acl.U.Contains("u3"));
            Assert.AreEqual(1, acl.C.Count);
            Assert.True(acl.C.Contains("u4"));
            Assert.AreEqual(1, acl.D.Count);
            Assert.True(acl.D.Contains("u5"));
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.True(acl.Admin.Contains("u6"));
            Assert.AreEqual("u7", acl.Owner);
        }

        /// <summary>
        /// コンストラクタテスト（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorWithJsonExceptionJsonNull()
        {
            // Android版との差分（Android版ではインスタンスが生成される）
            var acl = new NbAcl(null);
        }

        /**
        * ConvertJsonArray
        **/
        /// <summary>
        /// ConvertJsonArray（正常）
        /// 戻り値が正しいこと
        /// </summary>
        [Test]
        public void TestConvertJsonArrayNormal()
        {
            var acl = new NbAcl();
            var array = new NbJsonArray { "u1", "u2" };
            var expected = new HashSet<string>();
            expected.Add("u1");
            expected.Add("u2");

            var result = acl.ConvertJsonArray(array);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// ConvertJsonArray（aryが空）
        /// 戻り値が空のセットになること
        /// </summary>
        [Test]
        public void TestConvertJsonArrayNormalAryEmpty()
        {
            var acl = new NbAcl();
            var array = new NbJsonArray();
            var expected = new HashSet<string>();

            var result = acl.ConvertJsonArray(array);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// ConvertJsonArray（aryがnull）
        /// 戻り値が空のセットになること
        /// </summary>
        [Test]
        public void TestConvertJsonArraySubnormalAryNull()
        {
            var acl = new NbAcl();
            var expected = new HashSet<string>();

            var result = acl.ConvertJsonArray(null);

            Assert.AreEqual(expected, result);
        }

        /**
        * ToJson
        **/
        /// <summary>
        /// ToJson（正常）
        /// 生成されたJsonObjectの内容が正しいこと
        /// r,w,u,c,d,admin,ownerのキーが存在すること
        /// </summary>
        [Test]
        public void TestToJsonNormal()
        {
            var acl = new NbAcl();
            acl.R.Add("r1");
            acl.W.Add("w1");
            acl.U.Add("u1");
            acl.C.Add("c1");
            acl.D.Add("d1");
            acl.Admin.Add("a1");
            acl.Owner = "o1";

            var json = acl.ToJson();

            Assert.True(json.GetArray("r").Contains("r1"));
            Assert.True(json.GetArray("w").Contains("w1"));
            Assert.True(json.GetArray("u").Contains("u1"));
            Assert.True(json.GetArray("c").Contains("c1"));
            Assert.True(json.GetArray("d").Contains("d1"));
            Assert.True(json.GetArray("admin").Contains("a1"));
            Assert.AreEqual("o1", json.Get<string>("owner"));
        }

        /// <summary>
        /// ToJson（未設定時）
        /// 生成されたJsonObjectの内容が正しいこと
        /// r,w,u,c,d,admin,ownerのキーが存在すること
        /// </summary>
        [Test]
        public void TestToJsonNormalEmpty()
        {
            var acl = new NbAcl();
            var json = acl.ToJson();

            Assert.True(json.ContainsKey("r"));
            Assert.IsEmpty(json.GetArray("r"));
            Assert.True(json.ContainsKey("w"));
            Assert.IsEmpty(json.GetArray("w"));
            Assert.True(json.ContainsKey("u"));
            Assert.IsEmpty(json.GetArray("u"));
            Assert.True(json.ContainsKey("c"));
            Assert.IsEmpty(json.GetArray("c"));
            Assert.True(json.ContainsKey("d"));
            Assert.IsEmpty(json.GetArray("d"));
            Assert.True(json.ContainsKey("admin"));
            Assert.IsEmpty(json.GetArray("admin"));
            Assert.True(json.ContainsKey("owner"));
            Assert.IsNull(json.Get<string>("owner"));
        }

        /**
        * CreateAclForAnonymous
        **/
        /// <summary>
        /// CreateAclForAnonymous（正常）
        /// 生成されたACLの内容が正しいこと
        /// R,W,Adminに"g:anonymous"が設定されていること
        /// </summary>
        [Test]
        public void TestCreateAclForAnonymousNormal()
        {
            var acl = NbAcl.CreateAclForAnonymous();

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("g:anonymous", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("g:anonymous", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("g:anonymous", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /**
        * CreateAclForAuthenticated
        **/
        /// <summary>
        /// CreateAclForAuthenticated（正常）
        /// 生成されたACLの内容が正しいこと
        /// R,W,Adminに"g:authenticated"が設定されていること
        /// </summary>
        [Test]
        public void TestCreateAclForAuthenticatedNormal()
        {
            var acl = NbAcl.CreateAclForAuthenticated();

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("g:authenticated", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("g:authenticated", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("g:authenticated", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /**
        * CreateAclForUser
        **/
        /// <summary>
        /// CreateAclForUser（正常）
        /// 生成されたACLの内容が正しいこと
        /// R,W,Admin,Ownerには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestCreateAclForUserNormal()
        {
            var user = new NbUser();
            user.UserId = "u1";
            var acl = NbAcl.CreateAclForUser(user);

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("u1", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("u1", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("u1", acl.Admin.First());
            Assert.AreEqual("u1", acl.Owner);
        }

        /// <summary>
        /// CreateAclForUser（userがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>       
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForUserExceptionUserNull()
        {
            var acl = NbAcl.CreateAclForUser(null);
        }

        /// <summary>
        /// CreateAclForUser（UserIdがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>       
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForUserExceptionUserIdNull()
        {
            var user = new NbUser();
            user.UserId = null;
            var acl = NbAcl.CreateAclForUser(user);
        }

        /**
        * CreateAclFor 引数がentry
        **/
        /// <summary>
        /// CreateAclFor（正常）
        /// 生成されたACLの内容が正しいこと
        /// R,W,Adminには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestCreateAclForWithEntryNormal()
        {
            var acl = NbAcl.CreateAclFor("u1");

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("u1", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("u1", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("u1", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// CreateAclFor（entryがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>       
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForWithEntryExceptionEntryNull()
        {
            var acl = NbAcl.CreateAclFor((string)null);
        }

        /**
        * CreateAclFor 引数がentries
        **/
        /// <summary>
        /// CreateAclFor（正常）
        /// 生成されたACLの内容が正しいこと
        /// R,W,Adminには指定の値が格納されること 
        /// </summary>
        [Test]
        public void TestCreateAclForWithEntriesNormal()
        {
            var acl = NbAcl.CreateAclFor(new[] { "u1" });

            Assert.AreEqual(1, acl.R.Count);
            Assert.AreEqual("u1", acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.AreEqual("u1", acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.AreEqual("u1", acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// CreateAclFor（entriesのサイズが0）
        /// 生成されたACLの内容が正しいこと
        /// R,W,Adminが空であること
        /// </summary>
        [Test]
        public void TestCreateAclForWithEntriesSubnormalSizeZero()
        {
            var acl = NbAcl.CreateAclFor(new string[0]);

            Assert.IsEmpty(acl.R);
            Assert.IsEmpty(acl.W);
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.IsEmpty(acl.Admin);
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// CreateAclFor（entriesのサイズが1）
        /// 生成されたACLの内容が正しいこと
        /// R,W,Adminには指定の値が格納されること 
        /// </summary>
        [Test]
        public void TestCreateAclForWithEntriesSubnormalSizeOne()
        {
            var acl = NbAcl.CreateAclFor(new string[1]);

            Assert.AreEqual(1, acl.R.Count);
            Assert.IsNull(acl.R.First());
            Assert.AreEqual(1, acl.W.Count);
            Assert.IsNull(acl.W.First());
            Assert.IsEmpty(acl.U);
            Assert.IsEmpty(acl.C);
            Assert.IsEmpty(acl.D);
            Assert.AreEqual(1, acl.Admin.Count);
            Assert.IsNull(acl.Admin.First());
            Assert.IsNull(acl.Owner);
        }

        /// <summary>
        /// CreateAclFor（entriesがnull）
        /// ArgumentNullExceptionが発行されること
        /// </summary>      
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateAclForWithEntriesExceptionEntriesNull()
        {
            var acl = NbAcl.CreateAclFor((IEnumerable<string>)null);
        }
    }
}
