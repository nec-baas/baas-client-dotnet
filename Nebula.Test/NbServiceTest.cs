using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class NbServiceTest
    {
        private NbService _service;

        [TearDown]
        public void TearDown()
        {
            NbService.DisposeSingleton();
        }

        private void CreateService()
        {
            _service = NbService.GetInstance();
            _service.TenantId = "tenant";
            _service.AppId = "appId";
            _service.AppKey = "appKey";
            _service.EndpointUrl = "http://example.com/";
        }

        /**
        * GetInstance
        **/
        /// <summary>
        /// GetInstance（初期値）
        /// テナントID、アプリケーションID、アプリケーションキー、エンドポイントURLの値がnullであること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalInit()
        {
            var s = NbService.GetInstance();

            Assert.IsNull(s.TenantId);
            Assert.IsNull(s.AppId);
            Assert.IsNull(s.AppKey);
            Assert.IsNull(s.EndpointUrl);
            Assert.IsNotNull(s.SessionInfo);
            Assert.IsNotNull(s.RestExecutor);
        }

        /// <summary>
        /// GetInstance（マルチテナントモード無効時）
        /// 同一のインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalMultiTenantDisabled()
        {
            CreateService();
            var s = NbService.GetInstance();

            Assert.AreSame(_service, s);
        }

        /// <summary>
        /// GetInstance（マルチテナントモード無効時）
        /// 同一のインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalMultiTenantDisabledOtherThread()
        {
            var s1 = NbService.GetInstance();

            Task.Run(() =>
            {
                var s2 = NbService.GetInstance();
                Assert.AreEqual(s1, s2);
            }).Wait();
        }

        /// <summary>
        /// GetInstance（マルチテナントモード有効時） 
        /// 異なるインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalMultiTenantEnabled()
        {
            CreateService();
            NbService.EnableMultiTenant(true);

            var s = NbService.GetInstance();

            Assert.AreNotSame(_service, s);

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// GetInstance（マルチテナントモード有効時） 
        /// 異なるインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalMultiTenantEnabledOtherThread()
        {
            NbService.EnableMultiTenant(true);

            var s1 = NbService.GetInstance();

            Task.Run(() =>
            {
                var s2 = NbService.GetInstance();
                Assert.AreNotSame(s1, s2);
            }).Wait();

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// GetInstance（Dispose後のインスタンス取得）
        /// Dispose後のGetInstance()でインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceSubnoramlAfterDispose()
        {
            CreateService();
            NbService.DisposeSingleton();

            Assert.IsNotNull(NbService.GetInstance());
        }

        /**
        * Singleton
        **/
        /// <summary>
        /// Singleton（マルチテナントモード無効時） 
        /// 同一のインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestSingletonNormalMultiTenantDisabled()
        {
            CreateService();

            Assert.AreSame(_service, NbService.Singleton);
        }

        /// <summary>
        /// Singleton（マルチテナントモード有効時）
        /// InvalidOperationExceptionが発生すること
        /// </summary>
        [Test]
        public void TestSingletonExceptionMultiTenantEnabled()
        {
            CreateService();
            NbService.EnableMultiTenant(true);

            try
            {
                var s = NbService.Singleton;
                Assert.Fail("Bad Route.");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
            finally
            {
                NbService.EnableMultiTenant(false);
            }
        }

        /// <summary>
        /// Singleton（Dispose後のSingleton）
        /// Dispose後のSingletonでnullが返ること
        /// </summary>
        [Test]
        public void TestSingletonSubnoramlAfterDispose()
        {
            CreateService();
            NbService.DisposeSingleton();

            Assert.IsNull(NbService.Singleton);
        }

        /**
        * DisposeSingleton
        **/
        /// <summary>
        /// DisposeSingleton 
        /// _sSingletonがnullになること
        /// </summary>
        [Test]
        public void TestDisposeSingletonNormal()
        {
            CreateService();
            var s = NbService.Singleton;
            NbService.DisposeSingleton();

            // privateフィールドとなるため、Reflectionを使用
            FieldInfo service = s.GetType().GetField("_sSingleton", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNull(service.GetValue(s));
        }

        /**
        * EnableMultiTenant
        **/
        /// <summary>
        /// EnableMultiTenant（未設定）
        /// _sIsMultiTenantEnabledがfalseであること
        /// </summary>
        [Test]
        public void TestEnableMultiTenantNormalUnset()
        {
            CreateService();

            // privateフィールドとなるため、Reflectionを使用
            FieldInfo flag = _service.GetType().GetField("_sIsMultiTenantEnabled", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.False((bool)flag.GetValue(_service));
        }

        /// <summary>
        /// EnableMultiTenant（マルチテナントモード有効）
        /// _sIsMultiTenantEnabledがtrueになること
        /// </summary>
        [Test]
        public void TestEnableMultiTenantNormalEnabled()
        {
            CreateService();
            NbService.EnableMultiTenant(true);

            // privateフィールドとなるため、Reflectionを使用
            FieldInfo flag = _service.GetType().GetField("_sIsMultiTenantEnabled", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.True((bool)flag.GetValue(_service));

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// EnableMultiTenant（マルチテナントモード無効）
        /// _sIsMultiTenantEnabledがfalseになること
        /// </summary>
        [Test]
        public void TestEnableMultiTenantNormalDisabled()
        {
            CreateService();
            NbService.EnableMultiTenant(true);
            NbService.EnableMultiTenant(false);

            // privateフィールドとなるため、Reflectionを使用
            FieldInfo flag = _service.GetType().GetField("_sIsMultiTenantEnabled", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.False((bool)flag.GetValue(_service));
        }

        /**
        * Setter/Getter
        **/
        /// <summary>
        /// プロパティ設定（正常） 
        /// テナントID、アプリケーションID、アプリケーションキー、エンドポイントURLには指定の値が格納されること
        /// </summary>
        [Test]
        public void TestSetPropertiesNormal()
        {
            CreateService();

            Assert.AreEqual("tenant", _service.TenantId);
            Assert.AreEqual("appId", _service.AppId);
            Assert.AreEqual("appKey", _service.AppKey);
            Assert.AreEqual("http://example.com/", _service.EndpointUrl);
        }

        /// <summary>
        /// エンドポイントURL設定（末尾が/でない場合） 
        /// エンドポイントURLには末尾に/が補完された値が格納されること
        /// </summary>
        [Test]
        public void TestEndpointUrlNormalEndWithNotSlash()
        {
            CreateService();
            _service.EndpointUrl = "http://example.com.hoge";

            Assert.AreEqual("http://example.com.hoge/", _service.EndpointUrl);
        }

        /// <summary>
        /// エンドポイントURL設定（nullが設定） 
        /// ArgumentNullExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestEndpointUrlNormalException()
        {
            CreateService();
            _service.EndpointUrl = null;
        }

        /**
        * IsOfflineEnabled
        **/
        /// <summary>
        /// IsOfflineEnabled（未設定） 
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestIsOfflineEnabledNormalUnset()
        {
            CreateService();
            Assert.False(_service.IsOfflineEnabled());
        }

        /// <summary>
        /// IsOfflineEnabled（有効） 
        /// trueが返ること
        /// </summary>
        [Test]
        public void TestIsOfflineEnabledNormalEnabled()
        {
            CreateService();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(_service);
            Assert.True(_service.IsOfflineEnabled());
        }

        /// <summary>
        /// IsOfflineEnabled（無効） 
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestIsOfflineEnabledNormalDisabled()
        {
            CreateService();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(_service);
            _service.DisableOffline();

            Assert.False(_service.IsOfflineEnabled());
        }

        /**
        * DisableOffline
        **/
        /// <summary>
        /// DisableOffline（有効時） 
        /// オフラインサービスが無効になること
        /// </summary>
        [Test]
        public void TestDisableOfflineNormalEnabled()
        {
            CreateService();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(_service);
            Assert.True(_service.IsOfflineEnabled());

            _service.DisableOffline();

            Assert.False(_service.IsOfflineEnabled());
        }

        /// <summary>
        /// DisableOffline（無効時） 
        /// 例外などが発生しないこと
        /// </summary>
        [Test]
        public void TestDisableOfflineSubnormalDisabled()
        {
            CreateService();
            Assert.False(_service.IsOfflineEnabled());

            _service.DisableOffline();

            Assert.False(_service.IsOfflineEnabled());
        }


        /**
         * ChangeDatabasePassword
         **/

        /// <summary>
        /// データベースパスワード変更（正常）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestChangeDatabasePasswordNormal()
        {
            CreateService();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(_service);
            _service.ChangeDatabasePassword("test");
        }

        /// <summary>
        /// データベースパスワード変更（パスワードがnull）
        /// 正常終了すること
        /// </summary>
        [Test]
        public void TestChangeDatabasePasswordNormalNoPassword()
        {
            CreateService();
            NbOfflineService.SetInMemoryMode(true);
            NbOfflineService.EnableOfflineService(_service);
            _service.ChangeDatabasePassword(null);
        }

        /// <summary>
        /// データベースパスワード変更（オフライン機能が無効）
        /// InvalidOperationExceptionが発行されること
        /// </summary>
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestChangeDatabasePasswordExceptionDisabledOffline()
        {
            CreateService();
            _service.ChangeDatabasePassword("test");
        }

        /// <summary>
        /// HttpTimeout: 初期値が無限大であること、正常に値をセットできること。
        /// </summary>
        [Test]
        public void TestSetRestTimeout()
        {
            CreateService();


            Assert.AreEqual(_service.HttpTimeout, System.Threading.Timeout.InfiniteTimeSpan);

            _service.HttpTimeout = TimeSpan.FromSeconds(1);
            Assert.AreEqual(_service.HttpTimeout, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// HttpTimeout: 不正な値を設定したときに ArgumentOutOfRangeException がスローされること。
        /// </summary>
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetRestTimeoutBadValue()
        {
            CreateService();
            _service.HttpTimeout = TimeSpan.Zero;
        }
    }
}
