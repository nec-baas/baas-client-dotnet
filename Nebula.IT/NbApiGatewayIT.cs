using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Nec.Nebula.IT
{
    [TestFixture]
    public class NbApiGatewayIT
    {
        [SetUp]
        public void SetUp()
        {
            ITUtil.InitNebula();
        }

        [TearDown]
        public void TearDown()
        {
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
        }

        /// <summary>
        /// パラメータ値取得
        /// </summary>
        /// <param name="uri">uri</param>
        /// <param name="name">name</param>
        /// <returns>パラメータ値</returns>
        private string GetParameterValue(string uri, string name)
        {
            string[] parameters = uri.Split('?');
            foreach (var parameter in parameters)
            {
                string[] key = parameter.Split('=');
                if (key[0] == name)
                {
                    return key[1];
                }
            }

            return null;
        }

        /// <summary>
        /// バイナリレスポンス取得用API呼び出し(GET1)
        /// リクエストで指定したヘッダが正しく送信されていること
        /// リクエストで指定したUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが正しく送信されていること
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと        
        /// </summary>
        [Test]
        public void TestExecuteRawAsyncSetValueGet()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.SetHeader("Header1", "HeaderValue1");
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            apigateway.ContentType = "application/json";
            var result = apigateway.ExecuteRawAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // Check Request
            Assert.AreEqual("HeaderValue1", result.Response.RequestMessage.Headers.GetValues("Header1").First());
            Assert.AreEqual("User-Agent-TEST", result.Response.RequestMessage.Headers.GetValues("User-Agent").First());
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.AreEqual("Parameter-TEST", GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// バイナリレスポンス取得用API呼び出し(GET2)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteRawAsyncSetEmptyNullGet()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.SetHeader("User-Agent", "");
            apigateway.SetQueryParameter("username", "");
            apigateway.ContentType = null;
            var result = apigateway.ExecuteRawAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// バイナリレスポンス取得用API呼び出し(GET3)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteRawAsyncSetNullGet()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.SetHeader("User-Agent", null);
            apigateway.SetQueryParameter("username", null);
            var result = apigateway.ExecuteRawAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(GET1)
        /// リクエストで指定したヘッダが正しく送信されていること
        /// リクエストで指定したUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが正しく送信されていること
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetValueGet()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.SetHeader("Header1", "HeaderValue1");
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            apigateway.ContentType = "application/json";
            var result = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // Check Request
            Assert.AreEqual("HeaderValue1", result.Response.RequestMessage.Headers.GetValues("Header1").First());
            Assert.AreEqual("User-Agent-TEST", result.Response.RequestMessage.Headers.GetValues("User-Agent").First());
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.AreEqual("Parameter-TEST", GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(GET2)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetEmptyNullGet()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.SetHeader("User-Agent", "");
            apigateway.SetQueryParameter("username", "");
            apigateway.ContentType = null;
            var result = apigateway.ExecuteAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(GET3)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetNullGet()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.SetHeader("User-Agent", null);
            apigateway.SetQueryParameter("username", null);
            var result = apigateway.ExecuteAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(PUT1)
        /// リクエストで指定したヘッダが正しく送信されていること
        /// リクエストで指定したUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていること
        /// リクエストで指定したBodyが正しく送信されていること
        /// リクエストで指定したContent-Typeが正しく送信されていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetValuePut()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.SetHeader("Header1", "HeaderValue1");
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;

            // Check Request
            Assert.AreEqual("HeaderValue1", result.Response.RequestMessage.Headers.GetValues("Header1").First());
            Assert.AreEqual("User-Agent-TEST", result.Response.RequestMessage.Headers.GetValues("User-Agent").First());
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.AreEqual("Parameter-TEST", GetParameterValue(uri, "username"));

            var contentBody = NbJsonObject.Parse(result.Response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(jsonObj, contentBody);
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(PUT2)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetEmptyNullPut()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.SetHeader("User-Agent", "");
            apigateway.SetQueryParameter("username", "");
            apigateway.ContentType = null;
            var result = apigateway.ExecuteAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(PUT3)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したBodyが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetNullPut()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.SetHeader("User-Agent", null);
            apigateway.SetQueryParameter("username", null);
            apigateway.ContentType = "application/octet-stream";
            var json = "{'foo': 'bar'}";
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            var result = apigateway.ExecuteAsync(byteArray).Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));

            // Check Response
            var contentBody = result.Response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(json, contentBody);
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(PUT4)
        /// カスタムAPI呼び出しが成功すること
        /// 例外(InvalidOperationException)をスローすること
        /// ステータスコードに以下が設定されていること 400 Bad Request"
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetContentTypePut()
        {
            // Bodyなし、Content-Typeあり
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.ContentType = "application/octet-stream";
            await apigateway.ExecuteAsync();

            // Bodyあり（Not NULL）、Content-Type=NULL
            try
            {
                apigateway.ContentType = null;
                await apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}"));
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            // Bodyあり、Content-Typeあり
            try
            {
                apigateway.ContentType = "application/json";
                var byteArray = new byte[] { 0x41, 0x42, 0x43 }; // ABC
                await apigateway.ExecuteAsync(byteArray);
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.BadRequest, statusCode);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(POST1)
        /// リクエストで指定したヘッダが正しく送信されていること
        /// リクエストで指定したUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていること
        /// リクエストで指定したBodyが正しく送信されていること
        /// リクエストで指定したContent-Typeが正しく送信されていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetValuePost()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
            apigateway.SetHeader("Header1", "HeaderValue1");
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;

            // Check Request
            Assert.AreEqual("HeaderValue1", result.Response.RequestMessage.Headers.GetValues("Header1").First());
            Assert.AreEqual("User-Agent-TEST", result.Response.RequestMessage.Headers.GetValues("User-Agent").First());
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.AreEqual("Parameter-TEST", GetParameterValue(uri, "username"));

            var contentBody = NbJsonObject.Parse(result.Response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(jsonObj, contentBody);
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(POST2)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetEmptyNullPost()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
            apigateway.SetHeader("User-Agent", "");
            apigateway.SetQueryParameter("username", "");
            apigateway.ContentType = null;
            var result = apigateway.ExecuteAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(POST3)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したBodyが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetNullPost()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
            apigateway.SetHeader("User-Agent", null);
            apigateway.SetQueryParameter("username", null);
            apigateway.ContentType = "application/octet-stream";
            var json = "{'foo': 'bar'}";
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            var result = apigateway.ExecuteAsync(byteArray).Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));

            // Check Response
            var contentBody = result.Response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(json, contentBody);
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(POST4)
        /// カスタムAPI呼び出しが成功すること
        /// 例外(InvalidOperationException)をスローすること
        /// ステータスコードに以下が設定されていること 400 Bad Request"
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetContentTypePost()
        {
            // Bodyなし、Content-Typeあり
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
            apigateway.ContentType = "application/octet-stream";
            await apigateway.ExecuteAsync();

            // Bodyあり（Not NULL）、Content-Type=NULL
            try
            {
                apigateway.ContentType = null;
                await apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}"));
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            // Bodyあり、Content-Typeあり
            try
            {
                apigateway.ContentType = "application/json";
                var byteArray = new byte[] { 0x41, 0x42, 0x43 }; // ABC
                await apigateway.ExecuteAsync(byteArray);
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.BadRequest, statusCode);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(DELETE1)
        /// リクエストで指定したヘッダが正しく送信されていること
        /// リクエストで指定したUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが正しく送信されていること
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetValueDelete()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "resp");
            apigateway.SetHeader("Header1", "HeaderValue1");
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            apigateway.ContentType = "application/json";
            var result = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // Check Request
            Assert.AreEqual("HeaderValue1", result.Response.RequestMessage.Headers.GetValues("Header1").First());
            Assert.AreEqual("User-Agent-TEST", result.Response.RequestMessage.Headers.GetValues("User-Agent").First());
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.AreEqual("Parameter-TEST", GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(DELETE2)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// リクエストで指定したBodyが送信されていないこと
        /// リクエストで指定したContent-Typeが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetEmptyNullDelete()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "resp");
            apigateway.SetHeader("User-Agent", "");
            apigateway.SetQueryParameter("username", "");
            apigateway.ContentType = null;
            var result = apigateway.ExecuteAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
            Assert.IsNull(result.Response.RequestMessage.Content);
            try
            {
                result.Response.RequestMessage.Headers.Contains("Content-Type");
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(DELETE3)
        /// デフォルトのUser-Agentが正しく送信されていること
        /// リクエストで指定したパラメータが送信されていないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetNullDelete()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "resp");
            apigateway.SetHeader("User-Agent", null);
            apigateway.SetQueryParameter("username", null);
            var result = apigateway.ExecuteAsync().Result;

            // Check Request
            Assert.AreEqual(Header.UserAgentDefaultValue, string.Join(" ", result.Response.RequestMessage.Headers.GetValues("User-Agent")));
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            Assert.IsNull(GetParameterValue(uri, "username"));
        }

        /// <summary>
        /// カスタムAPI呼び出し ContentType(PUT)
        /// (ContentTypeプロパティ、HeaderのContent-Type)
        /// (値1,値2)　　ContentTypeプロパティの値1を送信すること
        /// (値1,null)　 ContentTypeプロパティの値1を送信すること
        /// (null,値2)　 HeaderのContent-Typeの値2を送信すること
        /// (空,値2)　　 HeaderのContent-Typeの値2を送信すること
        /// </summary>
        [Test]
        public void TestExecuteAsyncSetContentTypePropertyHeaderPut()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");

            // (値1,値2)　　ContentTypeプロパティの値1を送信すること
            apigateway.ContentType = "application/json";
            apigateway.SetHeader("Content-Type", "text/plain");
            var result = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());

            // (値1,null)　 ContentTypeプロパティの値1を送信すること
            apigateway.ContentType = "application/json";
            apigateway.SetHeader("Content-Type", null);
            result = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());

            // (null,値2)　 HeaderのContent-Typeの値2を送信すること
            apigateway.ContentType = null;
            apigateway.SetHeader("Content-Type", "application/json");
            result = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());

            // (空,値2)　　 HeaderのContent-Typeの値2を送信すること
            apigateway.ContentType = "";
            apigateway.SetHeader("Content-Type", "application/json");
            result = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// JSONレスポンス取得用API呼び出し(PUT)
        /// (ContentTypeプロパティ、HeaderのContent-Type)
        /// (null,null)　例外(InvalidOperationException)をスローすること
        /// (空,null)　　例外(InvalidOperationException)をスローすること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncSetContentTypePropertyHeaderExceptionPut()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");

            // (null,null)　例外(InvalidOperationException)をスローすること
            apigateway.ContentType = null;
            apigateway.SetHeader("Content-Type", null);
            try
            {
                var result = await apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}"));
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            // (空,null)　　例外(InvalidOperationException)をスローすること
            apigateway.ContentType = "";
            apigateway.SetHeader("Content-Type", null);
            try
            {
                var result = await apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}"));
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// GETメソッド以外実行時のバイナリレスポンス受信(PUT)
        /// カスタムAPI呼び出しが成功すること        
        /// </summary>
        [Test]
        public void TestExecuteRawAsyncSetValuePut()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.ContentType = "application/json";
            var result = apigateway.ExecuteRawAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;
        }

        /// <summary>
        /// カスタムAPI呼び出し(共通1)
        /// ヘッダ名、ヘッダ値がNULLのものは設定されないこと
        /// ヘッダ名、ヘッダ値が空のものは設定されないこと
        /// Key,ValueがNULLのものは設定されないこと
        /// Key,Valueが空のものは設定されないこと
        /// BodyがNULLのものは設定されないこと
        /// </summary>
        [Test]
        public void TestExecuteAsyncCommon()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.SetHeader(null, null);
            apigateway.SetQueryParameter(null, null);
            apigateway.SetHeader("", "");
            apigateway.SetQueryParameter("", "");
            var result = apigateway.ExecuteAsync().Result;

            // Check Request
            var uri = result.Response.RequestMessage.RequestUri.ToString();
            string[] parameters = uri.Split('?');
            Assert.AreEqual(1, uri.Split('?').Count());
            Assert.IsNull(result.Response.RequestMessage.Content);
            Assert.AreEqual(3, result.Response.RequestMessage.Headers.Count());
        }

        /// <summary>
        /// カスタムAPI呼び出し(共通2)
        /// subpathにスラッシュ"/"を含む：カスタムAPI呼び出しが成功すること
        /// subpathが空：ステータスコードに以下が設定されていること 404 Not Found
        /// subpathがNULL：ステータスコードに以下が設定されていること 404 Not Found
        /// </summary>
        [Test]
        public async void TestExecuteAsyncCommonSubPath()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp/rpath");
            var rtn = await apigateway.ExecuteAsync();
            // Check Request
            var uri = rtn.Response.RequestMessage.RequestUri.ToString();
            Assert.IsTrue(uri.EndsWith("resp/rpath"));

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, null);
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し
        /// BodyがJSON形式、バイナリデータ以外
        /// Get、Delete：リクエストで指定したBodyが送信されていないこと
        /// Put、Post：例外(ArgumentException)をスローすること
        /// </summary>
        [Test]
        public async void TestExecuteIntBody()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.ContentType = "application/octet-stream";
            try
            {
                int k = 3;
                var result = await apigateway.ExecuteRawAsync(k);
                Assert.IsNull(result.Response.RequestMessage.Content);
            }
            catch (Exception e)
            {
                throw e;
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.ContentType = "application/octet-stream";
            try
            {
                int k = 3;
                var result = await apigateway.ExecuteRawAsync(k);
                Assert.IsNull(result.Response.RequestMessage.Content);
            }
            catch (Exception e)
            {
                throw e;
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.ContentType = "application/octet-stream";
            try
            {
                int k = 3;
                await apigateway.ExecuteAsync(k);
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
            apigateway.ContentType = "application/octet-stream";
            try
            {
                int k = 3;
                await apigateway.ExecuteAsync(k);
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "resp");
            apigateway.ContentType = "application/octet-stream";
            try
            {
                int k = 3;
                var result = await apigateway.ExecuteRawAsync(k);
                Assert.IsNull(result.Response.RequestMessage.Content);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(共通 バイナリ1)
        /// バイナリレスポンス取得用API使用
        /// レスポンスヘッダが正しく取得できていること
        /// レスポンスボディが正しく取得できていること
        /// </summary>
        [Test]
        public void TestExecuteRawAsyncCommonHeaderBody()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteRawAsync(jsonObj).Result;

            // Check Response
            var contentBody = NbJsonObject.Parse(result.Response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(jsonObj, contentBody);
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// カスタムAPI呼び出し(共通 バイナリ2)
        /// バイナリレスポンス取得用API使用
        /// 例外(InvalidOperationException)をスローすること
        /// ステータスコードに以下が設定されていること 404 Not Found"
        /// エラー理由に要因が設定されること ステータスコードに以下が設定されていること 404 Not Found
        /// エラー理由に要因が設定されること
        /// </summary>
        [Test]
        public async void TestExecuteRawAsyncCommonApiNameSubPath()
        {
            var apigateway = new NbApiGateway(null, System.Net.Http.HttpMethod.Put, "resp");
            try
            {
                await apigateway.ExecuteRawAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            apigateway = new NbApiGateway("helloHoge", System.Net.Http.HttpMethod.Put, "resp");
            try
            {
                await apigateway.ExecuteRawAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "respTEST");
            try
            {
                await apigateway.ExecuteRawAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(共通 バイナリ3)
        /// バイナリレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public void TestExecuteRawAsyncCommonStatusCodeOK()
        {
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Get, "normal");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteRawAsync(jsonObj).Result;
            Assert.AreEqual(299, (int)result.Response.StatusCode);
        }

        /// <summary>
        /// カスタムAPI呼び出し(共通 バイナリ4)
        /// バイナリレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public async void TestExecuteRawAsyncCommonStatusCode()
        {
            // 504 Gateway Timeout
            var apigateway = new NbApiGateway("time", System.Net.Http.HttpMethod.Get, "timeout");
            try
            {
                await apigateway.ExecuteRawAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.GatewayTimeout, statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }

            // 555 Custom Error
            apigateway = new NbApiGateway("err", System.Net.Http.HttpMethod.Get, "error");
            try
            {
                await apigateway.ExecuteRawAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(555, (int)statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(GET JSON1)
        /// JSONレスポンス取得用API使用
        /// レスポンスヘッダが正しく取得できていること
        /// レスポンスボディが正しく取得できていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncHeaderBodyGet()
        {
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Get, "normal");
            var result = apigateway.ExecuteAsync().Result;

            // Check Response
            var contentBody = NbJsonObject.Parse(result.Response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual("world", contentBody.Get<string>("message"));
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// カスタムAPI呼び出し(GET JSON2)
        /// JSONレスポンス取得用API使用
        /// api-nameにNULL：例外(InvalidOperationException)をスローすること
        /// api-nameに不正文字：ステータスコードに以下が設定されていること 400 Bad Request エラー理由に要因が設定されること
        /// 存在しないapi-name：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// 存在しないsubpath：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字（エンコードされる）：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncApiNameSubPathGet()
        {
            var apigateway = new NbApiGateway(null, System.Net.Http.HttpMethod.Get, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            apigateway = new NbApiGateway("-----", System.Net.Http.HttpMethod.Get, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.BadRequest, statusCode);
                Assert.NotNull((e as NbHttpException).Response.Content.ReadAsStringAsync().Result);
            }

            apigateway = new NbApiGateway("helloHoge", System.Net.Http.HttpMethod.Get, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "-----");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "respTEST");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, ":/?#[]@!$&'()*+,;=");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(GET JSON3)
        /// バイナリレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncStatusCodeOKGet()
        {
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Get, "normal");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;
            Assert.AreEqual(299, (int)result.Response.StatusCode);
        }

        /// <summary>
        /// カスタムAPI呼び出し(GET JSON4)
        /// JSONレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncStatusCodeGet()
        {
            // 504 Gateway Timeout
            var apigateway = new NbApiGateway("time", System.Net.Http.HttpMethod.Get, "timeout");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.GatewayTimeout, statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }

            // 555 Custom Error
            apigateway = new NbApiGateway("err", System.Net.Http.HttpMethod.Get, "error");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(555, (int)statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(PUT JSON1)
        /// JSONレスポンス取得用API使用
        /// レスポンスヘッダが正しく取得できていること
        /// レスポンスボディが正しく取得できていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncHeaderBodyPut()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;

            // Check Response
            var contentBody = NbJsonObject.Parse(result.Response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(jsonObj, contentBody);
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// カスタムAPI呼び出し(PUT JSON2)
        /// JSONレスポンス取得用API使用
        /// api-nameにNULL：例外(InvalidOperationException)をスローすること
        /// api-nameに不正文字：ステータスコードに以下が設定されていること 400 Bad Request エラー理由に要因が設定されること
        /// 存在しないapi-name：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// 存在しないsubpath：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字（エンコードされる）：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncApiNameSubPathPut()
        {
            var apigateway = new NbApiGateway(null, System.Net.Http.HttpMethod.Put, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            apigateway = new NbApiGateway("-----", System.Net.Http.HttpMethod.Put, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.BadRequest, statusCode);
                Assert.NotNull((e as NbHttpException).Response.Content.ReadAsStringAsync().Result);
            }

            apigateway = new NbApiGateway("helloHoge", System.Net.Http.HttpMethod.Put, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "-----");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "respTEST");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, ":/?#[]@!$&'()*+,;=");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(PUT JSON3)
        /// バイナリレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncStatusCodeOKPut()
        {
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Put, "normal");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;
            Assert.AreEqual(299, (int)result.Response.StatusCode);
        }

        /// <summary>
        /// カスタムAPI呼び出し(PUT JSON4)
        /// JSONレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncStatusCodePut()
        {
            // 504 Gateway Timeout
            var apigateway = new NbApiGateway("time", System.Net.Http.HttpMethod.Put, "timeout");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.GatewayTimeout, statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }

            // 555 Custom Error
            apigateway = new NbApiGateway("err", System.Net.Http.HttpMethod.Put, "error");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(555, (int)statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(POST JSON1)
        /// JSONレスポンス取得用API使用
        /// レスポンスヘッダが正しく取得できていること
        /// レスポンスボディが正しく取得できていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncHeaderBodyPost()
        {
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;

            // Check Response
            var contentBody = NbJsonObject.Parse(result.Response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(jsonObj, contentBody);
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// カスタムAPI呼び出し(POST JSON2)
        /// JSONレスポンス取得用API使用
        /// api-nameにNULL：例外(InvalidOperationException)をスローすること
        /// api-nameに不正文字：ステータスコードに以下が設定されていること 400 Bad Request エラー理由に要因が設定されること
        /// 存在しないapi-name：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// 存在しないsubpath：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字（エンコードされる）：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncApiNameSubPathPost()
        {
            var apigateway = new NbApiGateway(null, System.Net.Http.HttpMethod.Post, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            apigateway = new NbApiGateway("-----", System.Net.Http.HttpMethod.Post, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.BadRequest, statusCode);
                Assert.NotNull((e as NbHttpException).Response.Content.ReadAsStringAsync().Result);
            }

            apigateway = new NbApiGateway("helloHoge", System.Net.Http.HttpMethod.Post, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "-----");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "respTEST");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, ":/?#[]@!$&'()*+,;=");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(POST JSON3)
        /// バイナリレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncStatusCodeOKPost()
        {
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Post, "normal");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;
            Assert.AreEqual(299, (int)result.Response.StatusCode);
        }

        /// <summary>
        /// カスタムAPI呼び出し(POST JSON4)
        /// JSONレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncStatusCodePost()
        {
            // 504 Gateway Timeout
            var apigateway = new NbApiGateway("time", System.Net.Http.HttpMethod.Post, "timeout");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.GatewayTimeout, statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }

            // 555 Custom Error
            apigateway = new NbApiGateway("err", System.Net.Http.HttpMethod.Post, "error");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(555, (int)statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(DELETE JSON1)
        /// JSONレスポンス取得用API使用
        /// レスポンスヘッダが正しく取得できていること
        /// レスポンスボディが正しく取得できていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncHeaderBodyDelete()
        {
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Delete, "normal");
            var result = apigateway.ExecuteAsync().Result;

            // Check Response
            var contentBody = NbJsonObject.Parse(result.Response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual("world", contentBody.Get<string>("message"));
            Assert.AreEqual("application/json", result.Response.Content.Headers.GetValues("Content-Type").First());
        }

        /// <summary>
        /// カスタムAPI呼び出し(DELETE JSON2)
        /// JSONレスポンス取得用API使用
        /// api-nameにNULL：例外(InvalidOperationException)をスローすること
        /// api-nameに不正文字：ステータスコードに以下が設定されていること 400 Bad Request エラー理由に要因が設定されること
        /// 存在しないapi-name：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// 存在しないsubpath：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// subpathに不正文字（エンコードされる）：ステータスコードに以下が設定されていること 404 Not Found エラー理由に要因が設定されること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncApiNameSubPathDelete()
        {
            var apigateway = new NbApiGateway(null, System.Net.Http.HttpMethod.Delete, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            apigateway = new NbApiGateway("-----", System.Net.Http.HttpMethod.Delete, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.BadRequest, statusCode);
                Assert.NotNull((e as NbHttpException).Response.Content.ReadAsStringAsync().Result);
            }

            apigateway = new NbApiGateway("helloHoge", System.Net.Http.HttpMethod.Delete, "resp");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "-----");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "respTEST");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, ":/?#[]@!$&'()*+,;=");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し(DELETE JSON3)
        /// バイナリレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public void TestExecuteAsyncStatusCodeOKDelete()
        {
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Delete, "normal");
            apigateway.ContentType = "application/json";
            var jsonObj = NbJsonObject.Parse("{'foo': 'bar'}");
            var result = apigateway.ExecuteAsync(jsonObj).Result;
            Assert.AreEqual(299, (int)result.Response.StatusCode);
        }

        /// <summary>
        /// カスタムAPI呼び出し(DELETE JSON4)
        /// JSONレスポンス取得用API使用
        /// ステータスコードが設定されていること
        /// </summary>
        [Test]
        public async void TestExecuteAsyncStatusCodeDelete()
        {
            // 504 Gateway Timeout
            var apigateway = new NbApiGateway("time", System.Net.Http.HttpMethod.Delete, "timeout");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.GatewayTimeout, statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }

            // 555 Custom Error
            apigateway = new NbApiGateway("err", System.Net.Http.HttpMethod.Delete, "error");
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(555, (int)statusCode);
                Assert.IsNotNullOrEmpty((e as NbHttpException).Response.ToString());
            }
        }

        /// <summary>
        /// カスタムAPI呼び出し
        /// マルチテナントテスト
        /// </summary>
        [Test]
        public void TestExecuteAsyncMulti()
        {
            int NumTenants = 2;
            NbService[] tenants;

            NbService.EnableMultiTenant(true);
            tenants = new NbService[NumTenants];
            for (int i = 0; i < NumTenants; i++)
            {
                tenants[i] = NbService.GetInstance();
                ITUtil.InitNebula(tenants[i], i);
            }

            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp", tenants[0]);
            var result = apigateway.ExecuteAsync().Result;
            apigateway = new NbApiGateway("res2", System.Net.Http.HttpMethod.Get, "resp2", tenants[1]);
            result = apigateway.ExecuteAsync().Result;

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp", tenants[0]);
            result = apigateway.ExecuteAsync().Result;
            apigateway = new NbApiGateway("res2", System.Net.Http.HttpMethod.Put, "resp2", tenants[1]);
            result = apigateway.ExecuteAsync().Result;

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp", tenants[0]);
            result = apigateway.ExecuteAsync().Result;
            apigateway = new NbApiGateway("res2", System.Net.Http.HttpMethod.Post, "resp2", tenants[1]);
            result = apigateway.ExecuteAsync().Result;

            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "resp", tenants[0]);
            result = apigateway.ExecuteAsync().Result;
            apigateway = new NbApiGateway("res2", System.Net.Http.HttpMethod.Delete, "resp2", tenants[1]);
            result = apigateway.ExecuteAsync().Result;

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// カスタムAPI呼び出し
        /// マルチテナントテスト
        /// テナントAからテナントBのカスタムAPIを呼び出す
        /// </summary>
        [Test]
        public async void TestExecuteAsyncMultiMasterKey()
        {
            int NumTenants = 2;
            NbService[] tenants;
            NbService[] tenantMasters;

            NbService.EnableMultiTenant(true);
            tenants = new NbService[NumTenants];
            tenantMasters = new NbService[NumTenants];

            for (int i = 0; i < NumTenants; i++)
            {
                tenantMasters[i] = NbService.GetInstance();
                ITUtil.InitNebula(tenantMasters[i], i);
                ITUtil.UseMasterKey(tenantMasters[i], i);
            }

            // テナントA→OK
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Get, "normal", tenantMasters[0]);
            await apigateway.ExecuteRawAsync();

            // テナントB→404 NotFound
            apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Put, "normal", tenantMasters[1]);
            try
            {
                await apigateway.ExecuteRawAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                Assert.NotNull(ITUtil.GetErrorInfo((e as NbHttpException).Response));
            }

            NbService.EnableMultiTenant(false);
        }

        /// <summary>
        /// カスタムAPI呼び出し
        /// マルチテナントテスト
        /// </summary>
        [Test]
        public void TestExecuteAsyncLoop()
        {
            for (int i = 0; i < 3; i++)
            {
                var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
                var result = apigateway.ExecuteAsync().Result;

                apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
                result = apigateway.ExecuteAsync().Result;

                apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
                result = apigateway.ExecuteAsync().Result;

                apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "resp");
                result = apigateway.ExecuteAsync().Result;
            }
        }

        /// <summary>
        /// 手動用TP
        /// </summary>
        [Test]
        public void TestExecuteManual()
        {
            // バイナリレスポンス取得用API使用
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.ContentType = "application/json";
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            var result = apigateway.ExecuteRawAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // JSONレスポンス取得用API使用（全パラメータ）Get
            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, "resp");
            apigateway.ContentType = "application/json";
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            var resultjson = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // JSONレスポンス取得用API使用（全パラメータ）Put
            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Put, "resp");
            apigateway.ContentType = "application/json";
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            resultjson = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // JSONレスポンス取得用API使用（全パラメータ）Post
            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Post, "resp");
            apigateway.ContentType = "application/json";
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            resultjson = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // JSONレスポンス取得用API使用（全パラメータ）Delete
            apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Delete, "resp");
            apigateway.ContentType = "application/json";
            apigateway.SetHeader("User-Agent", "User-Agent-TEST");
            apigateway.SetQueryParameter("username", "Parameter-TEST");
            resultjson = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;
        }

        /// <summary>
        /// 手動用TP（非同期）
        /// </summary>
        [Test]
        public async void TestExecuteManualAsync()
        {
            // JSONレスポンス取得用API使用（必須パラメータ）Get
            var apigateway = new NbApiGateway("res", System.Net.Http.HttpMethod.Get, null);
            try
            {
                await apigateway.ExecuteAsync();
                Assert.Fail("No Exception!");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NbHttpException);
                var statusCode = (e as NbHttpException).StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
            }
        }

        /// <summary>
        /// 手動用TP（リクエストボディ）
        /// </summary>
        [Test]
        public void TestExecuteManualBody()
        {
            // リクエストボディの指定方式 JSON形式
            var apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Put, "normal");
            apigateway.ContentType = "application/json";
            var resultjson = apigateway.ExecuteAsync(NbJsonObject.Parse("{'foo': 'bar'}")).Result;

            // リクエストボディの指定方式 バイナリデータ
            apigateway = new NbApiGateway("nor", System.Net.Http.HttpMethod.Put, "normal");
            apigateway.ContentType = "application/octet-stream";
            resultjson = apigateway.ExecuteAsync(new byte[] { 0x41, 0x42, 0x43 }).Result;  // ABC
        }
    }
}
