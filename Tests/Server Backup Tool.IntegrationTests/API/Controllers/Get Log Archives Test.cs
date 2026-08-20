// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc.Testing;
using ServerBackupTool.IntegrationTests.API.Fixtures;
using ServerBackupTool.IntegrationTests.API.Helpers;
using System.Net;
using System.Text.Json;

namespace ServerBackupTool.IntegrationTests.API.Controllers
{
    [TestClass]
    public class GetLogArchivesTest
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
                "/logs/archived");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that archives are returned when ZIP files exist.
        /// </summary>
        [TestMethod]
        public async Task WithArchives_Returns200()
        {
            TestDataSeeder.CreateTestArchive(
                _Factory.ArchiveDirectory,
                "archive1.zip",
                new Dictionary<string, string[]>
                {
                    ["server.log"] = ["2025-01-01 12:00:00 INFO - Test message 1"]
                });

            TestDataSeeder.CreateTestArchive(
                _Factory.ArchiveDirectory,
                "archive2.zip",
                new Dictionary<string, string[]>
                {
                    ["server.log"] = ["2025-01-02 12:00:00 INFO - Test message 2"]
                });

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived");
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
                2,
                doc.RootElement.GetProperty("archives").GetArrayLength());
        }

        /// <summary>
        /// Checks that an empty archive directory returns a 204 response.
        /// </summary>
        [TestMethod]
        public async Task EmptyDirectory_Returns204()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs/archived");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }
    }
}
