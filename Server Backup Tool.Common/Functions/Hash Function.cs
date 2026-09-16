// Copyright © - Unpublished - Toby Hunter
using System.Security.Cryptography;
using System.Text;

namespace ServerBackupTool.Common.Functions
{
    public static class HashFunction
    {
        /// <summary>
        /// Takes the given value and hashes it using SHA-512.
        /// </summary>
        public static string HashValue(string value)
        {
            byte[] hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(value));

            StringBuilder hex = new(hashBytes.Length * 2);

            foreach (byte b in hashBytes)
            {
                hex.Append(b.ToString("x2"));
            }

            return hex.ToString();
        }
    }
}
