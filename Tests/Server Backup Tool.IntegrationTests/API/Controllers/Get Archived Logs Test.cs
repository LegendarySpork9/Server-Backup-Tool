// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc.Testing;
using ServerBackupTool.IntegrationTests.API.Fixtures;
using ServerBackupTool.IntegrationTests.API.Helpers;
using System.Net;
using System.Text.Json;

namespace ServerBackupTool.IntegrationTests.API.Controllers
{
    [TestClass]
    public class GetArchivedLogsTest
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
            if (Directory.Exists(_Factory.ArchiveDirectory))
            {
                foreach (string file in Directory.GetFiles(_Factory.ArchiveDirectory))
                {
                    File.Delete(file);
                }

                foreach (string dir in Directory.GetDirectories(_Factory.ArchiveDirectory))
                {
                    Directory.Delete(
                        dir,
                        true);
                }
            }
        }

        /// <summary>
        /// Checks that a request without authentication returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task NoAuth_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived/test.zip");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a valid archive returns a 200 response with parsed logs.
        /// </summary>
        [TestMethod]
        public async Task ValidArchive_Returns200()
        {
            TestDataSeeder.CreateTestArchive(
                _Factory.ArchiveDirectory,
                "valid.zip",
                new Dictionary<string, string[]>
                {
                    ["server.log"] =
                    [
                        "2025-01-01 12:00:00 INFO - Test message one",
                        "2025-01-01 12:01:00 WARN - Test message two"
                    ]
                });

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived/valid.zip");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            Assert.AreEqual(
                "TestServer",
                doc.RootElement.GetProperty("serverName").GetString());
            Assert.AreEqual(
                "valid.zip",
                doc.RootElement.GetProperty("archiveName").GetString());

            JsonElement logs = doc.RootElement.GetProperty("logs");
            Assert.IsTrue(logs.GetArrayLength() > 0);
        }

        /// <summary>
        /// Checks that a file name without a .zip extension returns a 400 response.
        /// </summary>
        [TestMethod]
        public async Task MissingZipExtension_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived/noext");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a path traversal attempt returns a 400 response.
        /// </summary>
        [TestMethod]
        public async Task PathTraversal_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived/..%5Cevil.zip");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that a non-existent archive returns a 204 response.
        /// </summary>
        [TestMethod]
        public async Task NonExistentArchive_Returns204()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived/missing.zip");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an archive with an empty file returns a 204 response.
        /// </summary>
        [TestMethod]
        public async Task EmptyArchive_Returns204()
        {
            TestDataSeeder.CreateTestArchive(
                _Factory.ArchiveDirectory,
                "empty.zip",
                new Dictionary<string, string[]>
                {
                    ["empty.log"] = []
                });

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived/empty.zip");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);
        }
    }
}
