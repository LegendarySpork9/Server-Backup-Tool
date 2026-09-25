// Copyright © - Unpublished - Toby Hunter
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using ServerBackupTool.API.Functions;

namespace ServerBackupTool.UnitTests.API.Functions
{
    [TestClass]
    public class CertificateFunctionTest
    {
        /// <summary>
        /// Checks that LoadFromPem returns a valid certificate with a private key
        /// when given unencrypted PEM files.
        /// </summary>
        [TestMethod]
        public void LoadFromPem_ReturnsCertificateWithPrivateKey_WhenNoPassword()
        {
            AsymmetricCipherKeyPair keyPair = GenerateRsaKeyPair();
            Org.BouncyCastle.X509.X509Certificate bcCert = GenerateSelfSignedCert(keyPair);

            string certPath = Path.GetTempFileName();
            string keyPath = Path.GetTempFileName();

            try
            {
                WritePem(certPath, bcCert);
                WritePem(keyPath, keyPair.Private);

                string certPem = File.ReadAllText(certPath);
                string keyPem = File.ReadAllText(keyPath);

                System.Security.Cryptography.X509Certificates.X509Certificate2 result =
                    CertificateFunction.LoadFromPem(certPem, keyPem, null);

                Assert.IsTrue(result.HasPrivateKey);
                Assert.AreEqual(
                    "CN=Test",
                    result.Subject);
            }

            finally
            {
                File.Delete(certPath);
                File.Delete(keyPath);
            }
        }

        /// <summary>
        /// Checks that LoadFromPem returns a valid certificate with a private key
        /// when given an encrypted PEM key file with a password.
        /// </summary>
        [TestMethod]
        public void LoadFromPem_ReturnsCertificateWithPrivateKey_WhenPasswordProtected()
        {
            string password = "TestPassword123";
            AsymmetricCipherKeyPair keyPair = GenerateRsaKeyPair();
            Org.BouncyCastle.X509.X509Certificate bcCert = GenerateSelfSignedCert(keyPair);

            string certPath = Path.GetTempFileName();
            string keyPath = Path.GetTempFileName();

            try
            {
                WritePem(certPath, bcCert);
                WriteEncryptedPem(keyPath, keyPair.Private, password);

                string certPem = File.ReadAllText(certPath);
                string keyPem = File.ReadAllText(keyPath);

                System.Security.Cryptography.X509Certificates.X509Certificate2 result =
                    CertificateFunction.LoadFromPem(certPem, keyPem, password);

                Assert.IsTrue(result.HasPrivateKey);
                Assert.AreEqual(
                    "CN=Test",
                    result.Subject);
            }

            finally
            {
                File.Delete(certPath);
                File.Delete(keyPath);
            }
        }

        private static AsymmetricCipherKeyPair GenerateRsaKeyPair()
        {
            RsaKeyPairGenerator generator = new();

            generator.Init(new KeyGenerationParameters(
                new SecureRandom(),
                2048));

            return generator.GenerateKeyPair();
        }

        private static Org.BouncyCastle.X509.X509Certificate GenerateSelfSignedCert(
            AsymmetricCipherKeyPair keyPair)
        {
            X509V3CertificateGenerator certGenerator = new();

            certGenerator.SetSerialNumber(BigInteger.ProbablePrime(
                120,
                new Random()));

            certGenerator.SetIssuerDN(new X509Name("CN=Test"));
            certGenerator.SetSubjectDN(new X509Name("CN=Test"));
            certGenerator.SetNotBefore(DateTime.UtcNow.AddDays(-1));
            certGenerator.SetNotAfter(DateTime.UtcNow.AddDays(1));
            certGenerator.SetPublicKey(keyPair.Public);

            Asn1SignatureFactory signatureFactory = new(
                "SHA256WithRSA",
                keyPair.Private);

            return certGenerator.Generate(signatureFactory);
        }

        private static void WritePem(string path, object obj)
        {
            using StreamWriter writer = new(path);
            PemWriter pemWriter = new(writer);
            pemWriter.WriteObject(obj);
        }

        private static void WriteEncryptedPem(string path, object obj, string password)
        {
            using StreamWriter writer = new(path);
            PemWriter pemWriter = new(writer);

            pemWriter.WriteObject(
                obj,
                "AES-256-CBC",
                password.ToCharArray(),
                new SecureRandom());
        }
    }
}
