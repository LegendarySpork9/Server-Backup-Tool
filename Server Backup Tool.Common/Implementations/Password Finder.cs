// Copyright © - Unpublished - Toby Hunter
using Org.BouncyCastle.OpenSsl;

namespace ServerBackupTool.Common.Implementations
{
    public class PasswordFinder(string password) : IPasswordFinder
    {
        /// <summary>
        /// Returns the password as a character array.
        /// </summary>
        public char[] GetPassword() => password.ToCharArray();
    }
}
