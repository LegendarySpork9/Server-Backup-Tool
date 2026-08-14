// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Models;
using System.Text;
using System.Text.Encodings.Web;

namespace ServerBackupTool.Tests.API.Implementations
{
    [TestClass]
    public class ClientAuthHandlerTest
    {
        private const string ValidClientId = "testclient";
        private const string ValidClientSecret = "testsecret";

        private static string ValidClientIdHash = "";
        private static string ValidClientSecretHash = "";

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ValidClientIdHash = HashValue(ValidClientId);
            ValidClientSecretHash = HashValue(ValidClientSecret);
        }

        /// <summary>
        /// Checks whether the handler skips authentication for endpoints without the Authorize attribute.
        /// </summary>
        [TestMethod]
        public async Task TestNoAuthorizeAttribute()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: false);

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Failure);
        }

        /// <summary>
        /// Checks whether the handler returns no result when the Authorization header is missing.
        /// </summary>
        [TestMethod]
        public async Task TestMissingAuthorizationHeader()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Failure);
        }

        /// <summary>
        /// Checks whether the handler rejects a non-Basic authorization scheme.
        /// </summary>
        [TestMethod]
        public async Task TestInvalidScheme()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);
            httpContext.Request.Headers.Authorization = "Bearer some-token";

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Failure);
            Assert.IsTrue(result.Failure.Message.Contains("Authorization scheme"));
        }

        /// <summary>
        /// Checks whether the handler rejects malformed Base64 in the Authorization header.
        /// </summary>
        [TestMethod]
        public async Task TestMalformedBase64()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);
            httpContext.Request.Headers.Authorization = "Basic !!!not-base64!!!";

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Failure);
            Assert.IsTrue(result.Failure.Message.Contains("Malformed"));
        }

        /// <summary>
        /// Checks whether the handler rejects credentials that are missing the colon separator.
        /// </summary>
        [TestMethod]
        public async Task TestMissingSeparator()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("nocredentialseparator"));
            httpContext.Request.Headers.Authorization = $"Basic {encoded}";

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Failure);
            Assert.IsTrue(result.Failure.Message.Contains("credential format"));
        }

        /// <summary>
        /// Checks whether the handler rejects an incorrect client ID.
        /// </summary>
        [TestMethod]
        public async Task TestInvalidClientId()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"wrongclient:{ValidClientSecret}"));
            httpContext.Request.Headers.Authorization = $"Basic {encoded}";

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Failure);
            Assert.IsTrue(result.Failure.Message.Contains("Invalid credentials"));
        }

        /// <summary>
        /// Checks whether the handler rejects an incorrect client secret.
        /// </summary>
        [TestMethod]
        public async Task TestInvalidClientSecret()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ValidClientId}:wrongsecret"));
            httpContext.Request.Headers.Authorization = $"Basic {encoded}";

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Failure);
            Assert.IsTrue(result.Failure.Message.Contains("Invalid credentials"));
        }

        /// <summary>
        /// Checks whether the handler succeeds with correct credentials.
        /// </summary>
        [TestMethod]
        public async Task TestValidCredentials()
        {
            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                ValidClientSecretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ValidClientId}:{ValidClientSecret}"));
            httpContext.Request.Headers.Authorization = $"Basic {encoded}";

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(result.Principal);
        }

        /// <summary>
        /// Checks whether the handler succeeds when the client secret contains a colon.
        /// </summary>
        [TestMethod]
        public async Task TestSecretContainingColon()
        {
            string secretWithColon = "secret:with:colons";
            string secretHash = HashValue(secretWithColon);

            ClientAuthHandler handler = CreateHandler(
                ValidClientIdHash,
                secretHash);

            DefaultHttpContext httpContext = CreateHttpContext(authorizeEndpoint: true);
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ValidClientId}:{secretWithColon}"));
            httpContext.Request.Headers.Authorization = $"Basic {encoded}";

            await handler.InitializeAsync(
                new AuthenticationScheme("ClientAuth", null, typeof(ClientAuthHandler)),
                httpContext);

            AuthenticateResult result = await handler.AuthenticateAsync();

            Assert.IsTrue(result.Succeeded);
        }

        /// <summary>
        /// Creates the auth handler with the given client credentials.
        /// </summary>
        private static ClientAuthHandler CreateHandler(
            string clientIdHash,
            string clientSecretHash)
        {
            AuthenticationModel authModel = new()
            {
                ClientId = clientIdHash,
                ClientSecret = clientSecretHash
            };

            Mock<ILoggerService> mockLogger = new();
            Mock<IOptionsMonitor<ClientAuthOptions>> mockOptions = new();
            mockOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ClientAuthOptions());

            Mock<ILoggerFactory> mockLoggerFactory = new();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            return new ClientAuthHandler(
                mockOptions.Object,
                mockLoggerFactory.Object,
                UrlEncoder.Default,
                authModel,
                mockLogger.Object);
        }

        /// <summary>
        /// Creates the HTTP context adding the auth if needed.
        /// </summary>
        private static DefaultHttpContext CreateHttpContext(bool authorizeEndpoint)
        {
            DefaultHttpContext httpContext = new();

            if (authorizeEndpoint)
            {
                Endpoint endpoint = new(
                    null,
                    new EndpointMetadataCollection(new AuthorizeAttribute()),
                    "TestEndpoint");

                httpContext.SetEndpoint(endpoint);
            }

            return httpContext;
        }

        /// <summary>
        /// Creates a hash of the given string.
        /// </summary>
        private static string HashValue(string value)
        {
            byte[] hashBytes = System.Security.Cryptography.SHA512.HashData(
                Encoding.UTF8.GetBytes(value));

            StringBuilder hex = new(hashBytes.Length * 2);

            foreach (byte b in hashBytes)
            {
                hex.Append(b.ToString("x2"));
            }

            return hex.ToString();
        }
    }
}
