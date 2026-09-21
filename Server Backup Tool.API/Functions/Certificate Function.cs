// Copyright © - Unpublished - Toby Hunter
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using ServerBackupTool.Common.Implementations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ServerBackupTool.API.Functions
{
    /// <summary>
    /// Provides certificate loading functionality for PEM files.
    /// </summary>
    public static class CertificateFunction
    {
        /// <summary>
        /// Loads an X509 certificate with its private key from PEM content strings.
        /// </summary>
        public static X509Certificate2 LoadFromPem(
            string certPem,
            string keyPem,
            string? password)
        {
            X509Certificate2 cert = X509Certificate2.CreateFromPem(certPem);

            PemReader pemReader;

            if (!string.IsNullOrEmpty(password))
            {
                pemReader = new PemReader(
                    new StringReader(keyPem),
                    new PasswordFinder(password));
            }

            else
            {
                pemReader = new PemReader(new StringReader(keyPem));
            }

            object keyObject = pemReader.ReadObject();

            RsaPrivateCrtKeyParameters rsaParameters;

            if (keyObject is AsymmetricCipherKeyPair keyPair)
            {
                rsaParameters = (RsaPrivateCrtKeyParameters)keyPair.Private;
            }

            else
            {
                rsaParameters = (RsaPrivateCrtKeyParameters)keyObject;
            }

            RSA rsa = RSA.Create(new RSAParameters
            {
                Modulus = rsaParameters.Modulus.ToByteArrayUnsigned(),
                Exponent = rsaParameters.PublicExponent.ToByteArrayUnsigned(),
                D = rsaParameters.Exponent.ToByteArrayUnsigned(),
                P = rsaParameters.P.ToByteArrayUnsigned(),
                Q = rsaParameters.Q.ToByteArrayUnsigned(),
                DP = rsaParameters.DP.ToByteArrayUnsigned(),
                DQ = rsaParameters.DQ.ToByteArrayUnsigned(),
                InverseQ = rsaParameters.QInv.ToByteArrayUnsigned()
            });

            X509Certificate2 certWithKey = cert.CopyWithPrivateKey(rsa);

            byte[] pfxBytes = certWithKey.Export(
                X509ContentType.Pfx,
                "temp");

            return X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                "temp");
        }
    }
}
