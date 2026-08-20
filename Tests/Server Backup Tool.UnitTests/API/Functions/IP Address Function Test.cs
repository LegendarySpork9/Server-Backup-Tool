// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Http;
using ServerBackupTool.API.Functions;
using System.Net;

namespace ServerBackupTool.UnitTests.API.Functions
{
    [TestClass]
    public class IPAddressFunctionTest
    {
        /// <summary>
        /// Checks that the CF-Connecting-IP header is returned when it exists.
        /// </summary>
        [TestMethod]
        public void FetchIpAddress_ReturnsCFConnectingIP_WhenHeaderExists()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["CF-Connecting-IP"] = "203.0.113.50";

            string result = IPAddressFunction.FetchIpAddress(context);

            Assert.AreEqual(
                "203.0.113.50",
                result);
        }

        /// <summary>
        /// Checks that the X-Forwarded-For header is returned when the CF-Connecting-IP header is missing.
        /// </summary>
        [TestMethod]
        public void FetchIpAddress_ReturnsXForwardedFor_WhenCFHeaderMissing()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.10";

            string result = IPAddressFunction.FetchIpAddress(context);

            Assert.AreEqual(
                "198.51.100.10",
                result);
        }

        /// <summary>
        /// Checks that the first IP from a comma-separated X-Forwarded-For header is returned and trimmed.
        /// </summary>
        [TestMethod]
        public void FetchIpAddress_ReturnsXForwardedFor_FirstIP_WhenMultipleIPs()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.10, 203.0.113.50, 10.0.0.1";

            string result = IPAddressFunction.FetchIpAddress(context);

            Assert.AreEqual(
                "198.51.100.10",
                result);
        }

        /// <summary>
        /// Checks that RemoteIpAddress is returned when no proxy headers are present.
        /// </summary>
        [TestMethod]
        public void FetchIpAddress_ReturnsRemoteIpAddress_WhenNoHeaders()
        {
            DefaultHttpContext context = new();
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");

            string result = IPAddressFunction.FetchIpAddress(context);

            Assert.AreEqual(
                "192.168.1.100",
                result);
        }

        /// <summary>
        /// Checks that an empty string is returned when no headers or RemoteIpAddress are available.
        /// </summary>
        [TestMethod]
        public void FetchIpAddress_ReturnsEmptyString_WhenNothingAvailable()
        {
            DefaultHttpContext context = new();

            string result = IPAddressFunction.FetchIpAddress(context);

            Assert.AreEqual(
                string.Empty,
                result);
        }

        /// <summary>
        /// Checks that CF-Connecting-IP takes priority over X-Forwarded-For when both headers are set.
        /// </summary>
        [TestMethod]
        public void FetchIpAddress_PrefersCFConnectingIP_OverXForwardedFor()
        {
            DefaultHttpContext context = new();
            context.Request.Headers["CF-Connecting-IP"] = "203.0.113.50";
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.10";

            string result = IPAddressFunction.FetchIpAddress(context);

            Assert.AreEqual(
                "203.0.113.50",
                result);
        }
    }
}
