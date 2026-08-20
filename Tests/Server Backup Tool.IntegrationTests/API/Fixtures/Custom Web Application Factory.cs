// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ServerBackupTool.API.Models;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;
using System.Security.Cryptography;
using System.Text;

namespace ServerBackupTool.IntegrationTests.API.Fixtures
{
    public class CustomWebApplicationFactory : WebApplicationFactory<ServerBackupTool.API.Program>
    {
        public const string TestClientId = "test-client";
        public const string TestClientSecret = "test-secret";

        private readonly string DbName = $"TestDb_{Guid.NewGuid():N}";
        private SqliteConnection? _KeepAliveConnection;
        private string? _ArchiveDirectory;

        public string DatabaseConnectionString => $"Data Source={DbName};Mode=Memory;Cache=Shared";

        public string ArchiveDirectory => _ArchiveDirectory ?? throw new InvalidOperationException("Factory has not been initialised.");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ArchiveDirectory = Path.Combine(
                Path.GetTempPath(),
                $"SBT_Archives_{Guid.NewGuid():N}");

            Directory.CreateDirectory(_ArchiveDirectory);

            _KeepAliveConnection = new SqliteConnection(DatabaseConnectionString);
            _KeepAliveConnection.Open();

            using (SqliteCommand cmd = _KeepAliveConnection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ServerName TEXT,
                    Timestamp TEXT,
                    Level TEXT,
                    Logger TEXT,
                    Message TEXT
                )";

                cmd.ExecuteNonQuery();
            }

            string hashedClientId = HashValue(TestClientId);
            string hashedClientSecret = HashValue(TestClientSecret);

            builder.UseEnvironment("Testing");

            builder.UseSetting(
                "HTTPS_PORT",
                "");

            builder.ConfigureServices(services =>
            {
                ServiceDescriptor? authDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AuthenticationModel));

                if (authDescriptor != null)
                {
                    services.Remove(authDescriptor);
                }

                ServiceDescriptor? dbOptionsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DatabaseOptionsModel));

                if (dbOptionsDescriptor != null)
                {
                    services.Remove(dbOptionsDescriptor);
                }

                ServiceDescriptor? archiveDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ArchiveSettingsModel));

                if (archiveDescriptor != null)
                {
                    services.Remove(archiveDescriptor);
                }

                ServiceDescriptor? dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDatabase));

                if (dbDescriptor != null)
                {
                    services.Remove(dbDescriptor);
                }

                services.AddSingleton(new AuthenticationModel
                {
                    ClientId = hashedClientId,
                    ClientSecret = hashedClientSecret
                });

                DatabaseOptionsModel dbOptions = new()
                {
                    Path = $"{DbName};Mode=Memory;Cache=Shared",
                    ServerName = "TestServer",
                    PollingIntervalMs = 1000
                };
                services.AddSingleton(dbOptions);

                services.AddSingleton(new ArchiveSettingsModel
                {
                    ArchiveDirectory = _ArchiveDirectory
                });

                services.AddSingleton<IDatabase, DatabaseWrapper>();
            });
        }

        private static string HashValue(string value)
        {
            byte[] hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(value));

            StringBuilder hex = new(hashBytes.Length * 2);

            foreach (byte b in hashBytes)
            {
                hex.Append(b.ToString("x2"));
            }

            return hex.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _KeepAliveConnection?.Close();
                _KeepAliveConnection?.Dispose();

                if (_ArchiveDirectory != null && Directory.Exists(_ArchiveDirectory))
                {
                    try
                    {
                        Directory.Delete(
                            _ArchiveDirectory,
                            true);
                    }

                    catch
                    {
                    }
                }
            }

            base.Dispose(disposing);
        }
    }
}
