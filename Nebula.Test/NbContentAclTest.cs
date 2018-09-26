using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbContentAclTest
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
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("r1");
            contentAcl.W.Add("w1");
            contentAcl.U.Add("u1");
            contentAcl.C.Add("c1");
            contentAcl.D.Add("d1");

            Assert.AreEqual(1, contentAcl.R.Count);
            Assert.True(contentAcl.R.Contains("r1"));
            Assert.AreEqual(1, contentAcl.W.Count);
            Assert.True(contentAcl.W.Contains("w1"));
            Assert.AreEqual(1, contentAcl.U.Count);
            Assert.True(contentAcl.U.Contains("u1"));
            Assert.AreEqual(1, contentAcl.C.Count);
            Assert.True(contentAcl.C.Contains("c1"));
            Assert.AreEqual(1, contentAcl.D.Count);
            Assert.True(contentAcl.D.Contains("d1"));
        }

        /// <summary>
        /// RのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetRExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var contentAcl = new NbContentAcl();
            contentAcl.R = null;
        }

        /// <summary>
        /// WのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetWExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var contentAcl = new NbContentAcl();
            contentAcl.W = null;
        }

        /// <summary>
        /// UのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetUExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var contentAcl = new NbContentAcl();
            contentAcl.U = null;
        }

        /// <summary>
        /// CのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetCExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var contentAcl = new NbContentAcl();
            contentAcl.C = null;
        }

        /// <summary>
        /// DのSetter（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetDExceptionNull()
        {
            // Android版との差分（Android版では空の一覧が設定される）
            var contentAcl = new NbContentAcl();
            contentAcl.D = null;
        }

        /**
        * Constructor
        **/
        /// <summary>
        /// コンストラクタテスト（正常）
        /// R,W,U,C,Dが空のセットであること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var contentAcl = new NbContentAcl();

            Assert.IsEmpty(contentAcl.R);
            Assert.IsEmpty(contentAcl.W);
            Assert.IsEmpty(contentAcl.U);
            Assert.IsEmpty(contentAcl.C);
            Assert.IsEmpty(contentAcl.D);
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
            var json = NbJsonObject.Parse("{r:['u1', 'u2'], w:['u3'], u:['u4'], c:['u5'], d:['u6']}");
            var contentAcl = new NbContentAcl(json);

            Assert.AreEqual(2, contentAcl.R.Count);
            Assert.True(contentAcl.R.Contains("u1"));
            Assert.True(contentAcl.R.Contains("u2"));
            Assert.AreEqual(1, contentAcl.W.Count);
            Assert.True(contentAcl.W.Contains("u3"));
            Assert.AreEqual(1, contentAcl.U.Count);
            Assert.True(contentAcl.U.Contains("u4"));
            Assert.AreEqual(1, contentAcl.C.Count);
            Assert.True(contentAcl.C.Contains("u5"));
            Assert.AreEqual(1, contentAcl.D.Count);
            Assert.True(contentAcl.D.Contains("u6"));
        }

        /// <summary>
        /// コンストラクタテスト（jsonが空）
        /// R,W,U,C,Dが空のセットであること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonNormalEmpty()
        {
            var json = new NbJsonObject();
            var contentAcl = new NbContentAcl(json);

            Assert.IsEmpty(contentAcl.R);
            Assert.IsEmpty(contentAcl.W);
            Assert.IsEmpty(contentAcl.U);
            Assert.IsEmpty(contentAcl.C);
            Assert.IsEmpty(contentAcl.D);
        }

        /// <summary>
        /// コンストラクタテスト（余剰パラメータ）
        /// jsonに余計なKeyがあっても無視されること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonSubnormalWrongKeys()
        {
            var json = NbJsonObject.Parse("{r:['u1'], w:['u2'], u:['u3'], c:['u4'], d:['u5'], admin:['u6'], owner:'u7'}");
            var contentAcl = new NbContentAcl(json);

            Assert.AreEqual(1, contentAcl.R.Count);
            Assert.True(contentAcl.R.Contains("u1"));
            Assert.AreEqual(1, contentAcl.W.Count);
            Assert.True(contentAcl.W.Contains("u2"));
            Assert.AreEqual(1, contentAcl.U.Count);
            Assert.True(contentAcl.U.Contains("u3"));
            Assert.AreEqual(1, contentAcl.C.Count);
            Assert.True(contentAcl.C.Contains("u4"));
            Assert.AreEqual(1, contentAcl.D.Count);
            Assert.True(contentAcl.D.Contains("u5"));
        }

        /// <summary>
        /// コンストラクタテスト（nullを設定）
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorWithJsonExceptionJsonNull()
        {
            // Android版との差分（Android版ではインスタンスが生成される）
            var contentAcl = new NbContentAcl(null);
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
            var contentAcl = new NbContentAcl();
            var array = new NbJsonArray { "u1", "u2" };
            var expected = new HashSet<string>();
            expected.Add("u1");
            expected.Add("u2");

            var result = contentAcl.ConvertJsonArray(array);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// ConvertJsonArray（aryが空）
        /// 戻り値が空のセットになること
        /// </summary>
        [Test]
        public void TestConvertJsonArrayNormalAryEmpty()
        {
            var contentAcl = new NbContentAcl();
            var array = new NbJsonArray();
            var expected = new HashSet<string>();

            var result = contentAcl.ConvertJsonArray(array);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// ConvertJsonArray（aryがnull）
        /// 戻り値が空のセットになること
        /// </summary>
        [Test]
        public void TestConvertJsonArraySubnormalAryNull()
        {
            var contentAcl = new NbContentAcl();
            var expected = new HashSet<string>();

            var result = contentAcl.ConvertJsonArray(null);

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
            var contentAcl = new NbContentAcl();
            contentAcl.R.Add("r1");
            contentAcl.W.Add("w1");
            contentAcl.U.Add("u1");
            contentAcl.C.Add("c1");
            contentAcl.D.Add("d1");

            var json = contentAcl.ToJson();

            Assert.True(json.GetArray("r").Contains("r1"));
            Assert.True(json.GetArray("w").Contains("w1"));
            Assert.True(json.GetArray("u").Contains("u1"));
            Assert.True(json.GetArray("c").Contains("c1"));
            Assert.True(json.GetArray("d").Contains("d1"));
        }

        /// <summary>
        /// ToJson（未設定時）
        /// 生成されたJsonObjectの内容が正しいこと
        /// r,w,u,c,d,admin,ownerのキーが存在すること
        /// </summary>
        [Test]
        public void TestToJsonNormalEmpty()
        {
            var contentAcl = new NbContentAcl();
            var json = contentAcl.ToJson();

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
        }
    }
}
