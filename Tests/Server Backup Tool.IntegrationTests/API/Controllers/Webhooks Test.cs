// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc.Testing;
using ServerBackupTool.IntegrationTests.API.Fixtures;
using ServerBackupTool.IntegrationTests.API.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ServerBackupTool.IntegrationTests.API.Controllers
{
    [TestClass]
    public class WebhooksTest
    {
        private static CustomWebApplicationFactory _Factory = null!;
        private static HttpClient _Client = null!;
        private static readonly JsonSerializerOptions _JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initialises the test class.
        /// </summary>
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _Factory = new CustomWebApplicationFactory();
            _Client = _Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        /// <summary>
        /// Cleans up the test class.
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            _Client.Dispose();
            _Factory.Dispose();
        }

        /// <summary>
        /// Initialises the test.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            TestDataSeeder.ClearWebhooks(_Factory.DatabaseConnectionString);
        }

        /// <summary>
        /// Checks that a register request without authentication returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task NoAuth_Register_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/webhooks");
            request.Content = new StringContent(
                "{\"url\":\"https://example.com/webhook\",\"logType\":\"All\",\"logLevel\":\"All\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a register request with invalid authentication returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidAuth_Register_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/webhooks");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                "invalid",
                "invalid");
            request.Content = new StringContent(
                "{\"url\":\"https://example.com/webhook\",\"logType\":\"All\",\"logLevel\":\"All\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a valid registration returns a 201 response with an id and server name.
        /// </summary>
        [TestMethod]
        public async Task ValidRegistration_Returns201_WithIdAndServerName()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/webhooks");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"url\":\"https://example.com/webhook\",\"logType\":\"All\",\"logLevel\":\"All\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Created,
                response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            Assert.IsFalse(
                string.IsNullOrEmpty(
                    doc.RootElement.GetProperty("id")
                        .GetString()));
            Assert.AreEqual(
                "TestServer",
                doc.RootElement.GetProperty("serverName")
                    .GetString());
        }

        /// <summary>
        /// Checks that an invalid log type returns a 400 response with an error message.
        /// </summary>
        [TestMethod]
        public async Task InvalidLogType_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/webhooks");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"url\":\"https://example.com/webhook\",\"logType\":\"Invalid\",\"logLevel\":\"All\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            Assert.IsTrue(
                doc.RootElement.GetProperty("error")
                    .GetString()!
                    .Contains("is not a valid log type"));
        }

        /// <summary>
        /// Checks that an invalid log level returns a 400 response with an error message.
        /// </summary>
        [TestMethod]
        public async Task InvalidLogLevel_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/webhooks");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"url\":\"https://example.com/webhook\",\"logType\":\"All\",\"logLevel\":\"Invalid\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            Assert.IsTrue(
                doc.RootElement.GetProperty("error")
                    .GetString()!
                    .Contains("is not a valid log level"));
        }

        /// <summary>
        /// Checks that a missing url field returns a 400 response.
        /// </summary>
        [TestMethod]
        public async Task MissingUrl_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/webhooks");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"logType\":\"All\",\"logLevel\":\"All\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an invalid url returns a 400 response with an error message.
        /// </summary>
        [TestMethod]
        public async Task InvalidUrl_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/webhooks");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"url\":\"not-a-url\",\"logType\":\"All\",\"logLevel\":\"All\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            Assert.IsTrue(
                doc.RootElement.GetProperty("error")
                    .GetString()!
                    .Contains("not a valid HTTP"));
        }

        /// <summary>
        /// Checks that unregistering a valid webhook returns a 200 response.
        /// </summary>
        [TestMethod]
        public async Task ValidUnregister_Returns200()
        {
            HttpRequestMessage registerRequest = new(
                HttpMethod.Post,
                "/webhooks");
            registerRequest.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            registerRequest.Content = new StringContent(
                "{\"url\":\"https://example.com/webhook\",\"logType\":\"All\",\"logLevel\":\"All\",\"afterId\":0}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage registerResponse = await _Client.SendAsync(registerRequest);

            Assert.AreEqual(
                HttpStatusCode.Created,
                registerResponse.StatusCode);

            string registerBody = await registerResponse.Content.ReadAsStringAsync();
            using JsonDocument registerDoc = JsonDocument.Parse(registerBody);

            string id = registerDoc.RootElement.GetProperty("id")
                .GetString()!;

            HttpRequestMessage deleteRequest = new(
                HttpMethod.Delete,
                $"/webhooks/{id}");
            deleteRequest.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage deleteResponse = await _Client.SendAsync(deleteRequest);

            Assert.AreEqual(
                HttpStatusCode.OK,
                deleteResponse.StatusCode);
        }

        /// <summary>
        /// Checks that unregistering a non-existent webhook returns a 404 response.
        /// </summary>
        [TestMethod]
        public async Task UnregisterNonExistent_Returns404()
        {
            HttpRequestMessage request = new(
                HttpMethod.Delete,
                "/webhooks/nonexistent-id");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
    }
}
