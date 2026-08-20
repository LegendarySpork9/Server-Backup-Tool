// Copyright © - Unpublished - Toby Hunter
using System.Net.Http.Headers;
using System.Text;

namespace ServerBackupTool.IntegrationTests.API.Helpers
{
    public static class AuthHelper
    {
        /// <summary>
        /// Creates a Basic authentication header from the given credentials.
        /// </summary>
        public static AuthenticationHeaderValue CreateBasicAuth(
            string clientId,
            string clientSecret)
        {
            string encoded = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            return new AuthenticationHeaderValue("Basic", encoded);
        }
    }
}
