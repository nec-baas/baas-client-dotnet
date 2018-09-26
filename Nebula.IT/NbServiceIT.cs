using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Nec.Nebula.IT
{
    [TestFixture]
    class NbServiceIT
    {
        private NbService service;

        [SetUp]
        public void SetUp()
        {
            //NbService.EnableMultiTenant(true);
            service = NbService.GetInstance();
            ITUtil.InitNebula(service);
        }

        [TearDown]
        public void TearDown()
        {
            //NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// CancelPendingRequest: リクエストが存在しないときにキャンセルしてもエラーが発生しないこと
        /// </summary>
        [Test]
        public void TestCancelPendingRequestsNone()
        {
            service.CancelPendingHttpRequests();
        }

        /// <summary>
        /// CancelPendingRequest: RESTリクエストを正常にキャンセルできること
        /// </summary>
        [Test]
        public void TestCancelPendingRequests()
        {
            // リクエストを出して即座にキャンセルする
            var task = NbUser.LoginWithUsernameAsync("dummy", "dummy", service);
            service.CancelPendingHttpRequests();

            try
            {
                task.Wait();
                Assert.Fail("no error");
            }
            catch (AggregateException ae)
            {
                var e = ae.GetBaseException();
                Assert.True(e is TaskCanceledException);
            }
        }

        /// <summary>
        /// HttpTimeout: リクエストタイムアウトが発生すること。
        /// リクエスト発行後でもタイムアウト値を変更できること。
        /// </summary>
        [Test]
        public async void TestHttpTimeout()
        {
            // 1ms に設定
            service.HttpTimeout = TimeSpan.FromMilliseconds(1);

            // REST API 発行 → タイムアウト
            try
            {
                await NbUser.LoginWithUsernameAsync("dummy", "dummy", service);
                Assert.Fail("No timeout");
            }
            catch (TaskCanceledException e)
            {
                // ok
            }

            // API 発行後にタイムアウト値を 10s に変更
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), service.HttpTimeout);
            service.HttpTimeout = TimeSpan.FromMilliseconds(10000);
            Assert.AreEqual(TimeSpan.FromMilliseconds(10000), service.HttpTimeout);

            // REST API 発行 → タイムアウトにならずサーバから応答を受け取ること
            try
            {
                await NbUser.LoginWithUsernameAsync("dummy", "dummy", service);
                Assert.Fail("No error?");
            }
            catch (NbHttpException e)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, e.StatusCode);
            }
        }
    }
}
