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
    public class PostCommandsTest
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
            TestDataSeeder.ClearCommands(_Factory.DatabaseConnectionString);
        }

        /// <summary>
        /// Checks that a request without authentication returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task NoAuth_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/commands");
            request.Content = new StringContent(
                "{\"target\":\"Tool\",\"command\":\"stop\"}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a request with invalid authentication returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidAuth_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/commands");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                "invalid",
                "invalid");
            request.Content = new StringContent(
                "{\"target\":\"Tool\",\"command\":\"stop\"}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a valid command returns a 200 response with the correct body.
        /// </summary>
        [TestMethod]
        public async Task ValidCommand_Returns200_WithCorrectBody()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/commands");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"target\":\"Tool\",\"command\":\"stop\"}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            Assert.AreEqual(
                "TestServer",
                doc.RootElement.GetProperty("serverName")
                    .GetString());
            Assert.AreEqual(
                "Tool",
                doc.RootElement.GetProperty("target")
                    .GetString());
            Assert.AreEqual(
                "stop",
                doc.RootElement.GetProperty("command")
                    .GetString());
            Assert.IsTrue(
                doc.RootElement.GetProperty("id")
                    .GetInt32() > 0);
            Assert.IsFalse(
                string.IsNullOrEmpty(
                    doc.RootElement.GetProperty("createdAt")
                        .GetString()));
        }

        /// <summary>
        /// Checks that an invalid target returns a 400 response with an error message.
        /// </summary>
        [TestMethod]
        public async Task InvalidTarget_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/commands");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"target\":\"Invalid\",\"command\":\"stop\"}",
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
                    .Contains("is not a valid target"));
        }

        /// <summary>
        /// Checks that a missing target field returns a 400 response.
        /// </summary>
        [TestMethod]
        public async Task MissingTarget_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/commands");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"command\":\"stop\"}",
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
                    .Contains("target", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks that a missing command field returns a 400 response.
        /// </summary>
        [TestMethod]
        public async Task MissingCommand_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/commands");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "{\"target\":\"Tool\"}",
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
                    .Contains("command", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks that an empty body returns a 400 or 415 response.
        /// </summary>
        [TestMethod]
        public async Task EmptyBody_Returns400Or415()
        {
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "/commands");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);
            request.Content = new StringContent(
                "",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.IsTrue(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.UnsupportedMediaType,
                $"Expected 400 or 415 but got {(int)response.StatusCode}.");
        }
    }
}
