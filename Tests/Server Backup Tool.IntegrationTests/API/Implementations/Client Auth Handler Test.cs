// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc.Testing;
using ServerBackupTool.IntegrationTests.API.Fixtures;
using ServerBackupTool.IntegrationTests.API.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace ServerBackupTool.IntegrationTests.API.Implementations
{
    [TestClass]
    public class ClientAuthHandlerTest
    {
        private static CustomWebApplicationFactory _Factory = null!;
        private static HttpClient _Client = null!;

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
            TestDataSeeder.ClearLogs(_Factory.DatabaseConnectionString);
        }

        /// <summary>
        /// Checks that valid credentials return a 200 response.
        /// </summary>
        [TestMethod]
        public async Task ValidCredentials_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                1);

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a missing Authorization header returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task MissingAuthorizationHeader_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an invalid scheme returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidScheme_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                "some-token");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that malformed Base64 returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task MalformedBase64_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                "!!!notbase64");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that credentials without a colon separator return a 401 response.
        /// </summary>
        [TestMethod]
        public async Task MissingSeparator_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("test")));

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an invalid client ID returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidClientId_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                "wrong-client",
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an invalid client secret returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidClientSecret_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                "wrong-secret");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an empty Authorization header returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task EmptyAuthorizationHeader_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                "");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }
    }
}
