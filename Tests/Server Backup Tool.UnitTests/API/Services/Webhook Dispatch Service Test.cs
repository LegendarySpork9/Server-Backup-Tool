// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Services;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ServerBackupTool.UnitTests.API.Services
{
    [TestClass]
    public class WebhookDispatchServiceTest
    {
        private record CapturedRequest(
            string? Signature,
            string? Body,
            string? ContentType);

        private class TestHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _Responses = new();

            public List<CapturedRequest> Requests { get; } = [];

            public void EnqueueResponse(HttpStatusCode statusCode)
            {
                _Responses.Enqueue(new HttpResponseMessage(statusCode));
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                string? body = null;
                string? signature = null;
                string? contentType = null;

                if (request.Content != null)
                {
                    body = await request.Content.ReadAsStringAsync(cancellationToken);
                    contentType = request.Content.Headers.ContentType?.MediaType;
                }

                if (request.Headers.TryGetValues("X-Webhook-Secret", out var values))
                {
                    signature = values.FirstOrDefault();
                }

                Requests.Add(new CapturedRequest(
                    signature,
                    body,
                    contentType));

                return _Responses.Count > 0
                    ? _Responses.Dequeue()
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        private class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                throw new HttpRequestException("Connection refused");
            }
        }

        private Mock<ILoggerService> _MockLogger = null!;
        private JsonSerializerOptions _JsonOptions = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockLogger.Setup(l => l.RequestId).Returns(Guid.NewGuid());

            _JsonOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Creates a test webhook payload.
        /// </summary>
        private static WebhookPayloadModel CreatePayload()
        {
            return new WebhookPayloadModel()
            {
                ServerName = "TestServer",
                Logs =
                [
                    new WebhookLogEntryModel()
                    {
                        Id = 1,
                        Timestamp = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc),
                        Level = "Info",
                        Type = "Tool",
                        Message = "Test message"
                    }
                ]
            };
        }

        /// <summary>
        /// Checks that Send returns true when the first attempt succeeds.
        /// </summary>
        [TestMethod]
        public async Task Send_ReturnsTrue_WhenFirstAttemptSucceeds()
        {
            TestHandler handler = new();
            handler.EnqueueResponse(HttpStatusCode.OK);

            HttpClient httpClient = new(handler);

            WebhookSettingsModel settings = new()
            {
                Secret = "test-secret",
                MaxRetries = 0
            };

            WebhookDispatchService service = new(
                _MockLogger.Object,
                httpClient,
                settings,
                _JsonOptions);

            (bool success, Exception? ex) = await service.Send(
                "https://example.com/webhook",
                CreatePayload());

            Assert.IsTrue(success);
            Assert.IsNull(ex);
            Assert.AreEqual(
                1,
                handler.Requests.Count);
        }

        /// <summary>
        /// Checks that Send returns false when the response is unsuccessful.
        /// </summary>
        [TestMethod]
        public async Task Send_ReturnsFalse_WhenResponseIsUnsuccessful()
        {
            TestHandler handler = new();
            handler.EnqueueResponse(HttpStatusCode.InternalServerError);

            HttpClient httpClient = new(handler);

            WebhookSettingsModel settings = new()
            {
                Secret = "test-secret",
                MaxRetries = 0
            };

            WebhookDispatchService service = new(
                _MockLogger.Object,
                httpClient,
                settings,
                _JsonOptions);

            (bool success, Exception? ex) = await service.Send(
                "https://example.com/webhook",
                CreatePayload());

            Assert.IsFalse(success);
        }

        /// <summary>
        /// Checks that Send retries and returns true when a later attempt succeeds.
        /// </summary>
        [TestMethod]
        public async Task Send_RetriesAndSucceeds_WhenLaterAttemptSucceeds()
        {
            TestHandler handler = new();
            handler.EnqueueResponse(HttpStatusCode.InternalServerError);
            handler.EnqueueResponse(HttpStatusCode.OK);

            HttpClient httpClient = new(handler);

            WebhookSettingsModel settings = new()
            {
                Secret = "test-secret",
                MaxRetries = 1
            };

            WebhookDispatchService service = new(
                _MockLogger.Object,
                httpClient,
                settings,
                _JsonOptions);

            (bool success, Exception? ex) = await service.Send(
                "https://example.com/webhook",
                CreatePayload());

            Assert.IsTrue(success);
            Assert.IsNull(ex);
            Assert.AreEqual(
                2,
                handler.Requests.Count);
        }

        /// <summary>
        /// Checks that Send returns false after all retries are exhausted.
        /// </summary>
        [TestMethod]
        public async Task Send_ReturnsFalse_AfterAllRetriesExhausted()
        {
            TestHandler handler = new();
            handler.EnqueueResponse(HttpStatusCode.InternalServerError);
            handler.EnqueueResponse(HttpStatusCode.InternalServerError);

            HttpClient httpClient = new(handler);

            WebhookSettingsModel settings = new()
            {
                Secret = "test-secret",
                MaxRetries = 1
            };

            WebhookDispatchService service = new(
                _MockLogger.Object,
                httpClient,
                settings,
                _JsonOptions);

            (bool success, Exception? ex) = await service.Send(
                "https://example.com/webhook",
                CreatePayload());

            Assert.IsFalse(success);
            Assert.AreEqual(
                2,
                handler.Requests.Count);
        }

        /// <summary>
        /// Checks that Send returns false when an exception is thrown during dispatch.
        /// </summary>
        [TestMethod]
        public async Task Send_ReturnsFalse_WhenExceptionThrown()
        {
            ThrowingHandler handler = new();

            HttpClient httpClient = new(handler);

            WebhookSettingsModel settings = new()
            {
                Secret = "test-secret",
                MaxRetries = 0
            };

            WebhookDispatchService service = new(
                _MockLogger.Object,
                httpClient,
                settings,
                _JsonOptions);

            (bool success, Exception? ex) = await service.Send(
                "https://example.com/webhook",
                CreatePayload());

            Assert.IsFalse(success);
            Assert.IsNotNull(ex);
        }

        /// <summary>
        /// Checks that Send includes the correct HMAC-SHA256 signature in the request header.
        /// </summary>
        [TestMethod]
        public async Task Send_IncludesCorrectHmacSignature()
        {
            TestHandler handler = new();
            handler.EnqueueResponse(HttpStatusCode.OK);

            HttpClient httpClient = new(handler);

            string secret = "test-secret-key";

            WebhookSettingsModel settings = new()
            {
                Secret = secret,
                MaxRetries = 0
            };

            WebhookDispatchService service = new(
                _MockLogger.Object,
                httpClient,
                settings,
                _JsonOptions);

            await service.Send(
                "https://example.com/webhook",
                CreatePayload());

            Assert.AreEqual(
                1,
                handler.Requests.Count);

            CapturedRequest captured = handler.Requests[0];

            Assert.IsNotNull(captured.Body);
            Assert.IsNotNull(captured.Signature);

            string expectedSignature;

            using (HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(captured.Body));

                StringBuilder hex = new(hash.Length * 2);

                foreach (byte b in hash)
                {
                    hex.Append(b.ToString("x2"));
                }

                expectedSignature = hex.ToString();
            }

            Assert.AreEqual(
                expectedSignature,
                captured.Signature);
        }

        /// <summary>
        /// Checks that Send sends the payload as JSON with the correct content type.
        /// </summary>
        [TestMethod]
        public async Task Send_SendsJsonPayload_WithCorrectContentType()
        {
            TestHandler handler = new();
            handler.EnqueueResponse(HttpStatusCode.OK);

            HttpClient httpClient = new(handler);

            WebhookSettingsModel settings = new()
            {
                Secret = "test-secret",
                MaxRetries = 0
            };

            WebhookDispatchService service = new(
                _MockLogger.Object,
                httpClient,
                settings,
                _JsonOptions);

            WebhookPayloadModel payload = CreatePayload();

            await service.Send(
                "https://example.com/webhook",
                payload);

            Assert.AreEqual(
                1,
                handler.Requests.Count);

            CapturedRequest captured = handler.Requests[0];

            Assert.IsNotNull(captured.Body);
            Assert.AreEqual(
                "application/json",
                captured.ContentType);

            string expectedJson = JsonSerializer.Serialize(
                payload,
                _JsonOptions);

            Assert.AreEqual(
                expectedJson,
                captured.Body);
        }
    }
}
