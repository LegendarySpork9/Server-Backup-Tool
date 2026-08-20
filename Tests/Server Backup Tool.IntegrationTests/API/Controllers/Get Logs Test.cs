// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc.Testing;
using ServerBackupTool.IntegrationTests.API.Fixtures;
using ServerBackupTool.IntegrationTests.API.Helpers;
using System.Net;
using System.Text.Json;

namespace ServerBackupTool.IntegrationTests.API.Controllers
{
    [TestClass]
    public class GetLogsTest
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
            TestDataSeeder.ClearLogs(_Factory.DatabaseConnectionString);
        }

        /// <summary>
        /// Checks that a request without authentication returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task NoAuth_Returns401()
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
        /// Checks that a request with invalid authentication returns a 401 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidAuth_Returns401()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                "invalid",
                "invalid");

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that logs are returned when data exists.
        /// </summary>
        [TestMethod]
        public async Task WithData_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                5);

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

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            Assert.AreEqual(
                "TestServer",
                doc.RootElement.GetProperty("serverName")
                    .GetString());
            Assert.AreEqual(
                5,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
        }

        /// <summary>
        /// Checks that an empty database returns a 204 response.
        /// </summary>
        [TestMethod]
        public async Task EmptyDatabase_Returns204()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an invalid log level returns a 400 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidLevel_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?level=Invalid");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that an invalid log type returns a 400 response.
        /// </summary>
        [TestMethod]
        public async Task InvalidType_Returns400()
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?type=Invalid");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }

        /// <summary>
        /// Checks that filtering by log level returns the correct results.
        /// </summary>
        [TestMethod]
        public async Task FilterByLevel_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                3,
                level: "Info");
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                2,
                level: "Debug");

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?level=Info");
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
                3,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
        }

        /// <summary>
        /// Checks that filtering by log type returns the correct results.
        /// </summary>
        [TestMethod]
        public async Task FilterByType_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                3,
                logger: "Tool");
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                2,
                logger: "Server");

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?type=Tool");
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
                3,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
        }

        /// <summary>
        /// Checks that the limit parameter restricts the number of results.
        /// </summary>
        [TestMethod]
        public async Task LimitParameter_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                10);

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?limit=3");
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
                3,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
        }

        /// <summary>
        /// Checks that the afterId parameter returns only logs before the given ID.
        /// </summary>
        [TestMethod]
        public async Task AfterIdParameter_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                5);

            HttpRequestMessage allRequest = new(
                HttpMethod.Get,
                "/logs");
            allRequest.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage allResponse = await _Client.SendAsync(allRequest);
            string allBody = await allResponse.Content.ReadAsStringAsync();
            using JsonDocument allDoc = JsonDocument.Parse(allBody);

            JsonElement logs = allDoc.RootElement.GetProperty("logs");
            int highestId = 0;

            foreach (JsonElement log in logs.EnumerateArray())
            {
                int id = log.GetProperty("id")
                    .GetInt32();

                if (id > highestId)
                {
                    highestId = id;
                }
            }

            int afterId = highestId - 2;

            HttpRequestMessage request = new(
                HttpMethod.Get,
                $"/logs?afterId={afterId}");
            request.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage response = await _Client.SendAsync(request);

            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            JsonElement filteredLogs = doc.RootElement.GetProperty("logs");

            foreach (JsonElement log in filteredLogs.EnumerateArray())
            {
                Assert.IsTrue(log.GetProperty("id")
                    .GetInt32() < afterId);
            }
        }

        /// <summary>
        /// Checks that filtering by both level and type returns only matching results.
        /// </summary>
        [TestMethod]
        public async Task FilterByLevelAndType_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                3,
                level: "Info",
                logger: "Tool");
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                2,
                level: "Info",
                logger: "Server");
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                2,
                level: "Debug",
                logger: "Tool");

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?level=Info&type=Tool");
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
                3,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
        }

        /// <summary>
        /// Checks that filtering by level with a limit returns restricted results.
        /// </summary>
        [TestMethod]
        public async Task FilterByLevelWithLimit_Returns200()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                5,
                level: "Info");
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                3,
                level: "Debug");

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?level=Info&limit=2");
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
                2,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
        }

        /// <summary>
        /// Checks that nextAfter is populated when the result count equals the limit.
        /// </summary>
        [TestMethod]
        public async Task LimitParameter_SetsNextAfter_WhenMoreResultsExist()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                10);

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?limit=5");
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
                5,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
            Assert.AreNotEqual(
                JsonValueKind.Null,
                doc.RootElement.GetProperty("nextAfter")
                    .ValueKind);
        }

        /// <summary>
        /// Checks that nextAfter is null when fewer results than the limit are returned.
        /// </summary>
        [TestMethod]
        public async Task LimitParameter_NoNextAfter_WhenFewerResults()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                3);

            HttpRequestMessage request = new(
                HttpMethod.Get,
                "/logs?limit=10");
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
                3,
                doc.RootElement.GetProperty("logs")
                    .GetArrayLength());
            Assert.AreEqual(
                JsonValueKind.Null,
                doc.RootElement.GetProperty("nextAfter")
                    .ValueKind);
        }

        /// <summary>
        /// Checks that using nextAfter from the first page retrieves the next page with no overlap.
        /// </summary>
        [TestMethod]
        public async Task Pagination_SecondPageHasNoOverlap()
        {
            TestDataSeeder.SeedLogs(
                _Factory.DatabaseConnectionString,
                10);

            HttpRequestMessage firstRequest = new(
                HttpMethod.Get,
                "/logs?limit=5");
            firstRequest.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage firstResponse = await _Client.SendAsync(firstRequest);
            string firstBody = await firstResponse.Content.ReadAsStringAsync();
            using JsonDocument firstDoc = JsonDocument.Parse(firstBody);

            int nextAfter = firstDoc.RootElement.GetProperty("nextAfter")
                .GetInt32();
            JsonElement firstPageLogs = firstDoc.RootElement.GetProperty("logs");

            List<int> firstPageIds = [];

            foreach (JsonElement log in firstPageLogs.EnumerateArray())
            {
                firstPageIds.Add(log.GetProperty("id")
                    .GetInt32());
            }

            HttpRequestMessage secondRequest = new(
                HttpMethod.Get,
                $"/logs?limit=5&afterId={nextAfter}");
            secondRequest.Headers.Authorization = AuthHelper.CreateBasicAuth(
                CustomWebApplicationFactory.TestClientId,
                CustomWebApplicationFactory.TestClientSecret);

            HttpResponseMessage secondResponse = await _Client.SendAsync(secondRequest);

            Assert.AreEqual(
                HttpStatusCode.OK,
                secondResponse.StatusCode);

            string secondBody = await secondResponse.Content.ReadAsStringAsync();
            using JsonDocument secondDoc = JsonDocument.Parse(secondBody);

            JsonElement secondPageLogs = secondDoc.RootElement.GetProperty("logs");

            foreach (JsonElement log in secondPageLogs.EnumerateArray())
            {
                int id = log.GetProperty("id")
                    .GetInt32();

                Assert.IsFalse(
                    firstPageIds.Contains(id),
                    $"Log Id {id} appeared on both pages.");
            }
        }
    }
}
