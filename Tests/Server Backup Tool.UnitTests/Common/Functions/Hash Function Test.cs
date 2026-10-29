// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Functions;
using System.Text.RegularExpressions;

namespace ServerBackupTool.UnitTests.Common.Functions
{
    [TestClass]
    public class HashFunctionTest
    {
        /// <summary>
        /// Checks that HashValue returns the correct SHA-512 hex string for a known input.
        /// </summary>
        [TestMethod]
        public void HashValue_ReturnsExpectedSha512_ForKnownInput()
        {
            string expected = "ee26b0dd4af7e749aa1a8ee3c10ae9923f618980772e473f8819a5d4940e0db27ac185f8a0e1d5f84f88bc887fd67b143732c304cc5fa9ad8e6f57f50028a8ff";

            string result = HashFunction.HashValue("test");

            Assert.AreEqual(
                expected,
                result);
        }

        /// <summary>
        /// Checks that HashValue returns different hashes for different inputs.
        /// </summary>
        [TestMethod]
        public void HashValue_ReturnsDifferentHashes_ForDifferentInputs()
        {
            string hashHello = HashFunction.HashValue("hello");
            string hashWorld = HashFunction.HashValue("world");

            Assert.AreNotEqual(
                hashHello,
                hashWorld);
        }

        /// <summary>
        /// Checks that HashValue returns a consistent hash when called multiple times with the same input.
        /// </summary>
        [TestMethod]
        public void HashValue_ReturnsConsistentHash_ForSameInput()
        {
            string hash1 = HashFunction.HashValue("consistency");
            string hash2 = HashFunction.HashValue("consistency");

            Assert.AreEqual(
                hash1,
                hash2);
        }

        /// <summary>
        /// Checks that HashValue returns a lowercase hex string of 128 characters (SHA-512 = 64 bytes = 128 hex chars).
        /// </summary>
        [TestMethod]
        public void HashValue_ReturnsLowercaseHexString()
        {
            string result = HashFunction.HashValue("anything");

            Assert.AreEqual(
                128,
                result.Length,
                $"Expected length 128 but got {result.Length}.");
            Assert.IsTrue(
                Regex.IsMatch(result, "^[0-9a-f]+$"),
                $"Expected all lowercase hex characters but got '{result}'.");
        }
    }
}
