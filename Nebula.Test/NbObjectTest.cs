using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbObjectTest
    {
        private NbObject obj;
        private MockRestExecutor restExecutor;

        [SetUp]
        public void init()
        {
            TestUtils.Init();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(NbService.Singleton);

            // inject Mock RestExecutor
            restExecutor = new MockRestExecutor();
            NbService.Singleton.RestExecutor = restExecutor;

            obj = new NbObject("test");
        }

        /**
        * デフォルトコンストラクタ
        **/
        /// <summary>
        /// デフォルトコンストラクタ（正常）
        /// インスタンスが取得できること
        /// </summary>
        /// 
        [Test]
        public void TestDefaultConstructorNormal()
        {
            Assert.NotNull(new NbObject());
        }

        /**
        * コンストラクタ 引数はbucketName, service
        **/
        /// <summary>
        /// コンストラクタ（service指定あり）
        /// サービス、バケット名には指定の値が格納されること
        /// オブジェクトID、ACL、ETag、作成日時、更新日時がnullであること 
        /// 削除マークがFalseであること
        /// </summary>
        [Test]
        public void TestConstructorNormal()
        {
            var s1 = NbService.Singleton;
            NbService.EnableMultiTenant(true);
            var s2 = NbService.GetInstance();
            Assert.AreNotSame(s1, s2);

            var obj = new NbObject("test", s2);

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(s2, obj.Service);
            FieldInfo json = obj.GetType().GetField("_json", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(json.GetValue(obj));
            Assert.IsNull(obj.Id);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsNull(obj.UpdatedAt);
            Assert.IsNull(obj.Acl);
            Assert.IsNull(obj.Etag);
            Assert.False(obj.Deleted);
            Assert.AreEqual(0, obj.Count());

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// コンストラクタ（service指定なし）
        /// バケット名には指定の値が格納されること
        /// サービスはNbService.Singletonであること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceUnset()
        {
            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
        }

        /// <summary>
        /// コンストラクタ（serviceがnull）
        /// バケット名には指定の値が格納されること
        /// サービスはNbService.Singletonであること
        /// </summary>
        [Test]
        public void TestConstructorNormalServiceNull()
        {
            var obj = new NbObject("test", null);

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
        }

        /// <summary>
        /// コンストラクタ（bucketNameがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorExceptionBucketNameNull()
        {
            var obj = new NbObject(null);
        }

        /**
        * コンストラクタ 引数はbucketName, json, service
        **/
        /// <summary>
        /// コンストラクタ（正常）
        /// 各プロパティ、フィールドには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonNormal()
        {
            const string objId = "0123456789abcdef";
            const string acl = "{owner:'o', r:[], w:[], c:[], u:[], d:[], admin:[]}";
            const string createdAt = "2015-01-01T00:00:00.000Z";
            const string updatedAt = "2015-02-02T00:00:00.000Z";
            const string etag = "43488373-3499-49d4-ab21-5e44f1149310";
            string jsonString = string.Format("{{_id:'{0}', test_key:'test_value', ACL:{1}, createdAt:'{2}', updatedAt:'{3}', etag:'{4}', _deleted:true}}", objId, acl, createdAt, updatedAt, etag);

            var obj = new NbObject("test", NbJsonObject.Parse(jsonString));

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
            Assert.AreEqual(objId, obj.Id);
            Assert.AreEqual(createdAt, obj.CreatedAt);
            Assert.AreEqual(updatedAt, obj.UpdatedAt);
            Assert.AreEqual("o", obj.Acl.Owner);
            Assert.IsEmpty(obj.Acl.R);
            Assert.IsEmpty(obj.Acl.W);
            Assert.IsEmpty(obj.Acl.C);
            Assert.IsEmpty(obj.Acl.U);
            Assert.IsEmpty(obj.Acl.D);
            Assert.IsEmpty(obj.Acl.Admin);
            Assert.AreEqual(etag, obj.Etag);
            Assert.True(obj.Deleted);
            Assert.AreEqual("test_value", obj["test_key"]);
            Assert.AreEqual(7, obj.Count()); // 任意のkey-valueだけでなく、他のkeyも含まれるため
        }

        /// <summary>
        /// コンストラクタ（jsonが空）
        /// オブジェクトID、ACL、ETag、作成日時、更新日時がnullであること
        /// 削除マークがFalseであること
        /// </summary>
        [Test]
        public void TestConstructorWithJsonNormalJsonEmpty()
        {
            var obj = new NbObject("test", new NbJsonObject());

            Assert.AreEqual("test", obj.BucketName);
            Assert.AreEqual(NbService.Singleton, obj.Service);
            Assert.IsNull(obj.Id);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsNull(obj.UpdatedAt);
            Assert.IsNull(obj.Acl);
            Assert.IsNull(obj.Etag);
            Assert.False(obj.Deleted);
            Assert.AreEqual(0, obj.Count());
        }

        /// <summary>
        /// コンストラクタ（jsonがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorWithJsonExceptionJsonNull()
        {
            var obj = new NbObject("test", (NbJsonObject)null);
        }

        /// <summary>
        /// コンストラクタ（json内に不正フィールドを含む）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestConstructorWithJsonExceptionInvalidField()
        {
            var json = new NbJsonObject()
            {
                {".test", "value"}
            };
            var obj = new NbObject("test", json);
        }

        // フィールドの不正文字チェックの詳細なUTは、ValidateFieldName()側のUTで実施する

        // bucketNameがnull、serviceが未指定、null、指定ありの動作については、
        // 引数がbucketName, serviceのコンストラクタの方で検証したので、UTは省略する

        // Setter/Getterについては、カスタマイズしている部分のみUTを実施する
        /**
        * Id
        **/
        /// <summary>
        /// Id（正常）
        /// オブジェクトIDを取得できること
        /// </summary>
        [Test]
        public void TestIdNormal()
        {
            // 未設定時はnull
            Assert.IsNull(obj.Id);

            obj.Id = "testId";

            Assert.AreEqual("testId", obj.Id);
        }

        /**
        * Etag
        **/
        /// <summary>
        /// Etag（正常）
        /// ETagを取得できること
        /// </summary>
        [Test]
        public void TestEtagNormal()
        {
            // 未設定時はnull
            Assert.IsNull(obj.Etag);

            obj.Etag = "testEtag";

            Assert.AreEqual("testEtag", obj.Etag);
        }

        /**
        * CreatedAt
        **/
        /// <summary>
        /// CreatedAt（正常）
        /// 作成日時を取得できること
        /// </summary>
        [Test]
        public void TestCreatedAtNormal()
        {
            // 未設定時はnull
            Assert.IsNull(obj.CreatedAt);

            obj.CreatedAt = "testCreatedAt";

            Assert.AreEqual("testCreatedAt", obj.CreatedAt);
        }

        /**
        * UpdatedAt
        **/
        /// <summary>
        /// UpdatedAt（正常）
        /// 更新日時を取得できること
        /// </summary>
        [Test]
        public void TestUpdatedAtNormal()
        {
            // 未設定時はnull
            Assert.IsNull(obj.UpdatedAt);

            obj.UpdatedAt = "testUpdatedAt";

            Assert.AreEqual("testUpdatedAt", obj.UpdatedAt);
        }

        /**
        * Deleted
        **/
        /// <summary>
        /// Deleted（正常）
        /// 削除マークを取得できること
        /// </summary>
        [Test]
        public void TestDeletedNormal()
        {
            // 未設定時はfalse
            Assert.False(obj.Deleted);

            obj.Deleted = true;

            Assert.True(obj.Deleted);
        }

        /**
        * インデクサ
        **/
        /// <summary>
        /// インデクサ（正常）
        /// 設定・取得できること
        /// </summary>
        [Test]
        public void TestIndexerNormal()
        {
            obj["key1"] = "abcde";

            Assert.AreEqual("abcde", obj["key1"]);
        }

        /// <summary>
        /// インデクサ（取得時、該当キーが存在しない）
        /// nullを取得すること
        /// </summary>
        [Test]
        public void TestIndexerNormalKeyNotFound()
        {
            Assert.IsNull(obj["key_not_exists"]);
        }

        /// <summary>
        /// インデクサ（同じキーを複数回設定）
        /// 後で設定した値を取得できること
        /// 例外が発生しないこと
        /// </summary>
        [Test]
        public void TestIndexerNormalSameKey()
        {
            obj["key1"] = "abcde";
            obj["key1"] = 12345;

            Assert.AreEqual(12345, obj["key1"]);
        }

        /// <summary>
        /// インデクサ（取得時、keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestIndexerExceptionGetKeyNull()
        {
            var v = obj[null];
        }

        /// <summary>
        /// インデクサ（設定時、keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestIndexerExceptionSetKeyNull()
        {
            obj[null] = "abcde";
        }

        /// <summary>
        /// インデクサ（設定したキーが不正）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestIndexerExceptionSetInvalidKey()
        {
            obj["$test"] = "abcde";
        }

        // keyの不正文字チェックの詳細なUTは、ValidateFieldName()側のUTで実施する

        /**
        * Init
        **/
        // 引数がbucketName, serviceのコンストラクタの方で検証したので、UTは省略する

        /**
        * GetEnumerator
        **/
        /// <summary>
        /// GetEnumerator（正常）
        /// foreachが使用できること
        /// </summary>
        [Test]
        public void TestGetEnumeratorNormalForeach()
        {
            obj["key1"] = 123456;
            obj["key2"] = 123456;

            var count = 0;
            foreach (var key_value in obj)
            {
                count++;
            }
            Assert.AreEqual(2, count);
        }

        /// <summary>
        /// GetEnumerator（オブジェクトID等を含む場合）
        /// foreachが使用できること
        /// </summary>
        [Test]
        public void TestGetEnumeratorNormalForeachContainsProperty()
        {
            const string objId = "0123456789abcdef";
            const string acl = "{owner:'o', r:[], w:[], c:[], u:[], d:[], admin:[]}";
            const string createdAt = "2015-01-01T00:00:00.000Z";
            const string updatedAt = "2015-01-01T00:00:00.000Z";
            const string etag = "43488373-3499-49d4-ab21-5e44f1149310";
            string jsonString = string.Format("{{_id:'{0}', test_key:'test_value', ACL:{1}, createdAt:'{2}', updatedAt:'{3}', etag:'{4}', _deleted:true}}", objId, acl, createdAt, updatedAt, etag);

            var obj = new NbObject("test", NbJsonObject.Parse(jsonString));

            var count = 0;
            foreach (var key_value in obj)
            {
                count++;
            }
            Assert.AreEqual(7, count); // 任意のJSONだけでなく、オブジェクトID、ETag、ACL、作成日時、更新日時、削除マークも含む
        }

        /// <summary>
        /// GetEnumerator（正常）
        /// LINQが使用できること
        /// </summary>
        [Test]
        public void TestGetEnumeratorNormalLINQ()
        {
            obj["key1"] = 123456;
            obj["key2"] = "test";

            var v = from x in obj where x.Key == "key1" select x.Value;

            Assert.AreEqual(1, v.Count());
            Assert.AreEqual(123456, v.First());
        }

        /**
        * HasKey
        **/
        /// <summary>
        /// HasKey（正常）
        /// 戻り値が正しいこと
        /// </summary>
        [TestCase("key1", "12345", "key1", Result = true, TestName = "TestHasKeyNormalKeyFound")]
        [TestCase("key1", "12345", "key2", Result = false, TestName = "TestHasKeyNormalKeyNotFound")]
        public bool TestHasKeyNormal(string key1, string value, string key2)
        {
            obj[key1] = value;
            return obj.HasKey(key2);
        }

        /// <summary>
        /// HasKey（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestHasKeyExceptionKeyNull()
        {
            obj.HasKey(null);
        }

        /**
        * Get
        **/
        /// <summary>
        /// Get（int型）
        /// キーに対応する値を取得できること
        /// </summary>
        [TestCase(1, Result = 1)]
        [TestCase(0, Result = 0)]
        [TestCase(int.MaxValue, Result = int.MaxValue)]
        [TestCase(int.MinValue, Result = int.MinValue)]
        public int TestGetNormalInt(int value)
        {
            obj["key"] = value;
            return obj.Get<int>("key");
        }

        /// <summary>
        /// Get（bool型）
        /// キーに対応する値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalBool()
        {
            obj["key"] = false;
            Assert.False(obj.Get<bool>("key"));
        }

        /// <summary>
        /// Get（string型）
        /// キーに対応する値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalString()
        {
            obj["key"] = "test";
            Assert.AreEqual("test", obj.Get<string>("key"));
        }

        /// <summary>
        /// Get（NbJsonObject型）
        /// キーに対応する値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalJsonObject()
        {
            var json = new NbJsonObject()
            {
                {"jsonKey", "jsonValue"}
            };

            obj["key"] = json;
            Assert.AreEqual(json, obj.Get<NbJsonObject>("key"));
        }

        /// <summary>
        /// Get（NbJsonArray型）
        /// キーに対応する値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalJsonArray()
        {
            var jsonArray = new NbJsonArray()
            {
                1, 2, 3
            };

            obj["key"] = jsonArray;
            Assert.AreEqual(jsonArray, obj.Get<NbJsonArray>("key"));
        }

        /// <summary>
        /// Get（null）
        /// キーに対応する値を取得できること
        /// </summary>
        [Test]
        public void TestGetNormalNull()
        {
            obj["key"] = null;

            Assert.IsNull(obj.Get<string>("key"));
            Assert.AreEqual(0, obj.Get<int>("key"));
        }

        /// <summary>
        /// Get（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetExceptionKeyNull()
        {
            obj["key"] = 123L;
            obj.Get<long>(null);
        }

        /// <summary>
        /// Get（対応するキーが存在しない）
        /// KeyNotFoundExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(KeyNotFoundException))]
        public void TestGetExceptionKeyNotFound()
        {
            obj["key"] = 123F;
            obj.Get<string>("key_not_exist");
        }

        /// <summary>
        /// Get（型が不一致）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestGetExceptionInvalidType()
        {
            obj["key"] = "test";
            obj.Get<int>("key");
        }

        private void CheckTryGetLong(long value, long expectedValue, bool expectedResult)
        {
            obj["key"] = value;

            long outValue;
            bool result = obj.TryGet<long>("key", out outValue);

            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(expectedValue, outValue);
        }

        /**
        * TryGet
        **/
        /// <summary>
        /// TryGet（対応するキーが存在する）
        /// trueが返り、取得した値が格納されていること
        /// </summary>
        [TestCase(1L, 1L, true)]
        [TestCase(0L, 0L, true)]
        [TestCase(long.MaxValue, long.MaxValue, true)]
        [TestCase(long.MinValue, long.MinValue, true)]
        public void TestTryGetNormalLong(long value, long expectedValue, bool result)
        {
            CheckTryGetLong((long)value, expectedValue, result);
        }

        /// <summary>
        /// TryGet（対応するキーが存在しない）
        /// falseが返ること
        /// </summary>
        [TestCase(Result = false)]
        public bool TestTryGetSubnormalIntIllegalkey()
        {
            int value = 1;
            obj["key1"] = value;
            return obj.TryGet("key2", out value);
        }

        private void CheckTryGetString(string key1, string key2, string value, string expectedValue, bool expectedResult)
        {
            obj[key1] = value;

            string outValue;
            bool result = obj.TryGet<string>(key2, out outValue);

            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(expectedValue, outValue);
        }

        /// <summary>
        /// TryGet（string型）
        /// 戻り値が正しいこと
        /// </summary>
        [TestCase("key1", "key1", "12345", "12345", true, TestName = "TryGetNormalString")]
        [TestCase("key1", "key2", "12345", null, false, TestName = "TryGetSubnormalStringIllegalkey")]
        public void TestTryGetString(string key1, string key2, string value, string expectedValue, bool result)
        {
            CheckTryGetString(key1, key2, value, expectedValue, result);
        }

        /// <summary>
        /// TryGet（型が不一致）
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestTryGetSubnormalInvalidType()
        {
            obj["key"] = "test";
            int value = 1;

            Assert.False(obj.TryGet<int>("key", out value));
            Assert.AreEqual(0, value);
        }

        /**
        * Opt
        **/
        /// <summary>
        /// Opt（string）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestOptNormalString()
        {
            obj["key"] = "test";
            var v = obj.Opt<string>("key", null);

            Assert.AreEqual("test", v);
        }

        /// <summary>
        /// Opt（int）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestOptNormalInt()
        {
            obj["key"] = 1;
            var v = obj.Opt<int>("key", 0);

            Assert.AreEqual(1, v);
        }

        /// <summary>
        /// Opt（null）
        /// 指定したキーの値を取得できること
        /// </summary>
        [Test]
        public void TestOptNormalNulll()
        {
            obj["key"] = null;

            Assert.AreEqual(0, obj.Opt<int>("key", 1));
            Assert.AreEqual(null, obj.Opt<string>("key", "abc"));
        }

        /// <summary>
        /// Opt（キーが存在しない）
        /// デフォルト値を取得できること
        /// </summary>
        [Test]
        public void TestOptSubnormalKeyNotFound()
        {
            obj["key"] = "test";
            var v = obj.Opt<int>("c", 1);

            Assert.AreEqual(1, v);
        }

        /// <summary>
        /// Opt（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestOptExceptionKeyNull()
        {
            obj["key"] = "test";

            var v = obj.Opt<string>(null, "test1");
        }

        /// <summary>
        /// Opt（型が違う）
        /// InvalidCastExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidCastException))]
        public void TestOptExceptionInvalidType()
        {
            obj["key"] = 1;

            var v = obj.Opt<string>("key", null);
        }

        /**
        * Set
        **/
        /// <summary>
        /// Set（プリミティブ型：byte）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalByte()
        {
            byte v = 1;
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（プリミティブ型：char）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalChar()
        {
            char v = 'a';
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（プリミティブ型：int）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalInt()
        {
            int v = 1;
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（プリミティブ型：long）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalLong()
        {
            long v = 100L;
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（プリミティブ型：double）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalDouble()
        {
            double v = 100.123456;
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（プリミティブ型：bool）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalBool()
        {
            bool v = false;
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（string）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalString()
        {
            string v = "test";
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（List）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalList()
        {
            var v = new List<string>();
            v.Add("test");
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（Set）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalSet()
        {
            var v = new HashSet<string>();
            v.Add("test");
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（Dictionary）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalDictionary()
        {
            var v = new Dictionary<string, object>();
            v["string"] = "string";
            v["int"] = 1;
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（NbJsonObject）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalNbJsonObject()
        {
            var v = new NbJsonObject()
            {
                {"key", "value"}
            };
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（NbJsonArray）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalNbJsonArray()
        {
            var v = new NbJsonArray()
            {
                1, 2, 3
            };
            obj.Set("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Set（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestSetExceptionKeyNull()
        {
            obj.Set(null, 1);
        }

        /// <summary>
        /// Set（設定したキーが不正）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestSetExceptionSetInvalidKey()
        {
            obj.Set("test.", "abcde");
        }

        /// <summary>
        /// Set（コレクション初期化子）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestSetNormalCollectionInitializers()
        {
            var obj = new NbObject("test")
            {
                {"name", "Taro Nichiden"},
                {"age", 32}
            };

            Assert.AreEqual("Taro Nichiden", obj["name"]);
            Assert.AreEqual(32, obj["age"]);
        }

        /// <summary>
        /// Set（コレクション初期化子）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestSetExceptionCollectionInitializersInvalidField()
        {
            var obj = new NbObject("test")
            {
                {"$name", "Taro Nichiden"},
                {"age", 32}
            };
        }

        // keyの不正文字チェックの詳細なUTは、ValidateFieldName()側のUTで実施する

        /**
        * Remove
        **/
        /// <summary>
        /// Remove（正常）
        /// 指定されたキーを削除できること
        /// 戻り値がtrueであること
        /// </summary>
        [Test]
        public void TestRemoveNormal()
        {
            obj.Set("key", "abcde");
            Assert.True(obj.HasKey("key"));
            Assert.AreEqual("abcde", obj["key"]);

            var r = obj.Remove("key");

            Assert.False(obj.HasKey("key"));
            Assert.True(r);
        }

        /// <summary>
        /// Remove（指定されたキーが存在しない場合）
        /// 例外が発生しないこと
        /// 戻り値がfalseであること
        /// </summary>
        [Test]
        public void TestRemoveSubnormalKeyNotFound()
        {
            Assert.False(obj.HasKey("key"));

            var r = obj.Remove("key");

            Assert.False(r);
        }

        /// <summary>
        /// Remove（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestRemoveExceptionKeyNull()
        {
            obj["key"] = "test";

            obj.Remove(null);
        }

        /**
        * Add
        **/
        /// <summary>
        /// Add（プリミティブ型：sbyte）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalSbyte()
        {
            sbyte v = 1;
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（プリミティブ型：short）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalShort()
        {
            short v = 3;
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（プリミティブ型：uint）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalUint()
        {
            uint v = 1000;
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（プリミティブ型：ulong）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalUlong()
        {
            ulong v = 100L;
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（プリミティブ型：float）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalFloat()
        {
            float v = float.MaxValue;
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（プリミティブ型：decimal）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalDecimal()
        {
            decimal v = decimal.MinValue;
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（string）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalString()
        {
            string v = "test";
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（List）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalList()
        {
            var v = new List<string>();
            v.Add("test");
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（Dictionary）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalDictionary()
        {
            var v = new Dictionary<string, object>();
            v["string"] = "string";
            v["int"] = 1;
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（NbJsonObject）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalNbJsonObject()
        {
            var v = new NbJsonObject()
            {
                {"key", "value"}
            };
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（NbJsonArray）
        /// 設定可能なこと
        /// </summary>
        [Test]
        public void TestAddNormalNbJsonArray()
        {
            var v = new NbJsonArray()
            {
                1, 2, 3
            };
            obj.Add("key", v);

            Assert.True(obj.HasKey("key"));
            Assert.AreEqual(v, obj["key"]);
        }

        /// <summary>
        /// Add（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestAddExceptionKeyNull()
        {
            obj.Add(null, 1);
        }

        /// <summary>
        /// Add（設定したキーが不正）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestAddExceptionInvalidKey()
        {
            obj.Add("test.", 12345);
        }

        // keyの不正文字チェックの詳細なUTは、ValidateFieldName()側のUTで実施する

        /**
        * FromJson
        **/
        // 引数がbucketName, json, serviceのコンストラクタの方で検証したので、UTは省略する

        /**
        * ToJson
        **/
        /// <summary>
        /// ToJson（正常：Aclあり）
        /// 
        /// </summary>
        [Test]
        public void TestToJsonNormalWithAcl()
        {
            var obj = CreateObject();

            var json = obj.ToJson();

            Assert.AreEqual(6, json.Count);
            Assert.AreEqual(CreateSaveAsyncResponseJson(), json);
        }

        /// <summary>
        /// ToJson（null値を含む）
        /// null値もJsonObjectに含むこと
        /// </summary>
        [Test]
        public void TestToJsonNormalNullValue()
        {
            obj.Id = null;
            obj.Acl = null;
            obj.Etag = null;
            obj.CreatedAt = null;
            obj.UpdatedAt = null;

            var json = obj.ToJson();

            // ACLはnullの場合、含まれない
            Assert.AreEqual(4, json.Count);
            Assert.IsNull(json["_id"]);
            Assert.IsNull(json["etag"]);
            Assert.IsNull(json["createdAt"]);
            Assert.IsNull(json["updatedAt"]);
        }

        /// <summary>
        /// ToJson（インスタンス初期値）
        /// 空のJsonObjectが生成できること
        /// </summary>
        [Test]
        public void TestToJsonNormalEmpty()
        {
            var json = obj.ToJson();

            Assert.IsEmpty(json);
        }

        /**
        * CreateBucketRequest
        **/
        // privateメソッドのため、SaveAsync()側のUTで検証する

        /**
        * CreateObjectRequest
        **/
        // privateメソッドのため、SaveAsync()側のUTで検証する

        /**
        * SaveAsync
        **/
        /// <summary>
        /// SaveAsync（新規作成）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalCreate()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateSaveAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            obj["testKey"] = "testValue";
            var result = await obj.SaveAsync();

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Post, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual("testValue", reqJson["testKey"]);
            Assert.AreEqual(1, reqJson.Count);

            // Response Check
            CheckResponse(result);
        }

        /// <summary>
        /// SaveAsync（フルアップデート、Etagあり）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalFullUpdateWithEtag()
        {
            // Set Dummy Response
            var json = CreateSaveAsyncResponseJson();
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();
            var result = await obj.SaveAsync();

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            Assert.AreEqual("12345", req.QueryParams["etag"]);

            // Request Body
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(json, reqJson["$full_update"]);
            Assert.AreEqual(1, reqJson.Count);

            // Response Check
            CheckResponse(result);
        }

        /// <summary>
        /// SaveAsync（フルアップデート、Etagなし）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestSaveAsyncNormalFullUpdateNoEtag()
        {
            // Set Dummy Response
            var json = CreateSaveAsyncResponseJson();
            var response = new MockRestResponse(HttpStatusCode.OK, json.ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();
            obj.Etag = null;
            var result = await obj.SaveAsync();

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            json["etag"] = null;
            Assert.AreEqual(json, reqJson["$full_update"]);
            Assert.AreEqual(1, reqJson.Count);

            // Response Check
            CheckResponse(result);
        }

        /// <summary>
        /// SaveAsync（バケット名がnull）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestSaveAsyncExceptionBucketNameNull()
        {
            obj.BucketName = null;

            var result = await obj.SaveAsync();
        }

        /// <summary>
        /// SaveAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestSaveAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.BadRequest, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            try
            {
                var result = await obj.SaveAsync();
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
        * PartUpdateAsync
        **/
        /// <summary>
        /// PartUpdateAsync（Etagあり）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestPartUpdateAsyncNormalWithEtag()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreatePartUpdateAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();
            var json = new NbJsonObject()
            {
                {"testKey", "testValeu"}
            };
            var result = await obj.PartUpdateAsync(json);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            Assert.AreEqual("12345", req.QueryParams["etag"]);

            // Request Body
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(json, reqJson);
            Assert.AreEqual(1, reqJson.Count);

            // Response Check
            CheckResponse(result);
        }

        /// <summary>
        /// PartUpdateAsync（Etagなし）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestPartUpdateAsyncNormalNoEtag()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreatePartUpdateAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();
            obj.Etag = null;
            var json = new NbJsonObject()
            {
                {"testKey", "testValeu"}
            };
            var result = await obj.PartUpdateAsync(json);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Put, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            var reqJson = NbJsonParser.Parse(req.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(json, reqJson);
            Assert.AreEqual(1, reqJson.Count);

            // Response Check
            CheckResponse(result);
        }

        /// <summary>
        /// PartUpdateAsync（jsonがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public async void TestPartUpdateAsyncExceptionJsonNull()
        {
            var obj = CreateObject();
            var result = await obj.PartUpdateAsync(null);
        }

        /// <summary>
        /// PartUpdateAsync（オブジェクトIDがnull）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestPartUpdateAsyncExceptionIdNull()
        {
            var obj = CreateObject();
            obj.Id = null;
            var result = await obj.PartUpdateAsync(new NbJsonObject());
        }

        /// <summary>
        /// PartUpdateAsync（バケット名がnull）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestPartUpdateAsyncExceptionBucketNameNull()
        {
            var obj = CreateObject();
            obj.BucketName = null;
            var result = await obj.PartUpdateAsync(new NbJsonObject());
        }

        /// <summary>
        /// PartUpdateAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestPartUpdateAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.InternalServerError, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();

            try
            {
                var result = await obj.PartUpdateAsync(new NbJsonObject());
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.InternalServerError, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
        * DeleteAsync
        **/
        /// <summary>
        /// DeleteAsync（softDelete指定なし、Etagあり）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteUnSetWithEtag()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateSoftDeleteAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();

            await obj.DeleteAsync();

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(2, req.QueryParams.Count);
            Assert.AreEqual("12345", req.QueryParams["etag"]);
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);

            // Request Body
            Assert.IsNull(req.Content);
        }

        /// <summary>
        /// DeleteAsync（softDeleteがtrue、Etagなし）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalSoftDeleteNoEtag()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, CreateSoftDeleteAsyncResponseJson().ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();
            obj.Etag = null;

            await obj.DeleteAsync(true);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(1, req.QueryParams.Count);
            Assert.AreEqual("1", req.QueryParams["deleteMark"]);

            // Request Body
            Assert.IsNull(req.Content);
        }

        /// <summary>
        /// DeleteAsync（softDeleteがfalse、Etagなし）
        /// 設定しているメソッド、パス、リクエストボディが正しいこと
        /// 処理が正常終了すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncNormalHardDeleteNoEtag()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.OK, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();
            obj.Etag = null;

            await obj.DeleteAsync(false);

            // Request Check
            var req = restExecutor.LastRequest;

            // Method
            Assert.AreEqual(HttpMethod.Delete, req.Method);

            // URL
            Assert.IsTrue(req.Uri.EndsWith("/objects/" + "test/" + "abcdef"));

            // Header
            // このクラスの範囲外なので、チェックしない

            // Request Parameter
            Assert.AreEqual(0, req.QueryParams.Count);

            // Request Body
            Assert.IsNull(req.Content);
        }

        /// <summary>
        /// DeleteAsync（オブジェクトIDがnull）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteAsyncExceptionIdNull()
        {
            var obj = CreateObject();
            obj.Id = null;
            await obj.DeleteAsync();
        }

        /// <summary>
        /// DeleteAsync（オブジェクトIDが空文字）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionIdEmpty()
        {
            var obj = CreateObject();
            obj.Id = "";

            try
            {
                await obj.DeleteAsync();
                Assert.Fail("no exception");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual("No Id", e.Message);
            }
            catch (Exception)
            {
                Assert.Fail("Unexpected Exception");
            }
        }

        /// <summary>
        /// DeleteAsync（バケット名がnull）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public async void TestDeleteAsyncExceptionBucketNameNull()
        {
            var obj = CreateObject();
            obj.BucketName = null;
            await obj.DeleteAsync();
        }

        /// <summary>
        /// DeleteAsync（レスポンスエラー）
        /// NbHttpExceptionが発行されること
        /// </summary>
        [Test]
        public async void TestDeleteAsyncExceptionResponseError()
        {
            // Set Dummy Response
            var response = new MockRestResponse(HttpStatusCode.NotFound, new NbJsonObject().ToString());
            restExecutor.AddResponse(response);

            var obj = CreateObject();

            try
            {
                await obj.DeleteAsync(true);
                Assert.Fail("no exception");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, e.StatusCode);
            }
            catch (Exception)
            {
                Assert.Fail("No expected exception");
            }
        }

        /**
        * ValidateFieldName
        **/
        /// <summary>
        /// ValidateFieldName（正常）
        /// 例外が発生しないこと
        /// </summary>
        [TestCase("a$b", Result = 0, TestName = "TestValidateFieldNameNormalContainsDollar")]
        [TestCase("a_-@!?#%+*", Result = 0, TestName = "TestValidateFieldNameNormalMark")]
        [TestCase("あいう", Result = 0, TestName = "TestValidateFieldNameNormalDoubleByte")]
        [TestCase(" ", Result = 0, TestName = "TestValidateFieldNameNormalSpace")]
        public int TestValidateFieldNameNormal(string key)
        {
            // 途中 $ はOK
            // ピリオド以外の記号OK(使う可能性が高そうなもの)
            // ２バイト文字OK

            NbObject.ValidateFieldName(key);

            return 0;
        }

        private static TestCaseData[] validateFieldNameTestSource
            = new[]
        {
            new TestCaseData("").Returns(0).SetName("TestValidateFieldNameExceptionEmpty"),
            new TestCaseData("$a").Returns(0).SetName("TestValidateFieldNameExceptionStartWithDollar"),
            new TestCaseData("a.b").Returns(0).SetName("TestValidateFieldNameExceptionContainsPeriod"),
            new TestCaseData("$").Returns(0).SetName("TestValidateFieldNameExceptionDollar"),
            new TestCaseData(".").Returns(0).SetName("TestValidateFieldNameExceptionPeriod")
        };

        /// <summary>
        /// ValidateFieldName（不正文字列）
        /// ArgumentExceptionが発生すること
        /// </summary>
        [Test, TestCaseSource("validateFieldNameTestSource"), ExpectedException(typeof(ArgumentException))]
        public int TestValidateFieldNameException(string key)
        {
            // 空文字はNG
            // 先頭 $ はNG
            // ピリオドはNG
            NbObject.ValidateFieldName(key);

            return 0;
        }

        /// <summary>
        /// ValidateFieldName（keyがnull）
        /// ArgumentNullExceptionが発生すること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestValidateFieldNameExceptionKeyNull()
        {
            NbObject.ValidateFieldName(null);
        }

        /**
         * Test Utilities
         **/
        // 以下はNbObjectBucketTest.csでも使用
        public static NbJsonObject CreateSaveAsyncResponseJson()
        {
            var json = new NbJsonObject()
            {
                {"_id", "abcdef"},
                {"etag", "12345"},
                {"ACL", CreateAcl().ToJson()},
                {"createdAt", "2015-01-01T00:00:00.000Z"},
                {"updatedAt", "2015-02-02T00:00:00.000Z"},
                {"testKey", "testValue"}
            };

            return json;
        }

        private NbJsonObject CreatePartUpdateAsyncResponseJson()
        {
            return CreateSaveAsyncResponseJson();
        }

        private NbJsonObject CreateSoftDeleteAsyncResponseJson()
        {
            var json = CreateSaveAsyncResponseJson();
            json["_deleted"] = true;

            return json;
        }

        // 以下はNbObjectBucketTest.csでも使用
        public static NbAcl CreateAcl()
        {
            var acl = new NbAcl();
            acl.R.Add("g:anonymous");
            acl.W.Add("g:authenticated");

            return acl;
        }

        private static NbObject CreateObject()
        {
            var obj = new NbObject("test", CreateSaveAsyncResponseJson());

            return obj;
        }

        // 以下はNbObjectBucketTest.csでも使用
        public static void CheckResponse(NbObject obj, string bucketName = "test")
        {
            var comparedObj = CreateObject();
            comparedObj.BucketName = bucketName;

            Assert.AreEqual(comparedObj, obj);
        }
    }
}
