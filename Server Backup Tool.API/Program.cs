// Copyright © - Unpublished - Toby Hunter
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Filters;
using ServerBackupTool.API.Functions;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Responses;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace ServerBackupTool.API
{
    public class Program
    {
        /// <summary>
        /// Configures the application at startup.
        /// </summary>
        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo(Path.Combine(
                AppContext.BaseDirectory,
                "log4net.config")));

            ILoggerService _logger = new LoggerServiceWrapper("System");
            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting API");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Created Builder");

            builder.Services.AddControllers(options =>
                {
                    options.Filters.Add<RequestLoggingFilter>();
                    options.Filters.Add<ResponseLoggingFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

            builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    string errors = string.Join(
                        " ",
                        context.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .Where(m => !string.IsNullOrWhiteSpace(m)));

                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                        new FailureModel()
                        {
                            Error = errors
                        });
                };
            });

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Added Controllers");

            builder.Services.AddAuthentication("ClientAuth")
                .AddScheme<ClientAuthOptions, ClientAuthHandler>(
                    "ClientAuth",
                    null);

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Added Authentication");

            builder.Services.AddAuthorization();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Added Authorisation");

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "Server Backup Tool API",
                        Version = "v1",
                        Description = @"The Server Backup Tool (SBT) API provides remote monitoring and control over game servers managed by the tool. Live log messages from the SBT are added to a queue where the API can then retrieve them on demand. Archived logs are also accessible but require fetching from their archive. Commands are added to a queue where they are then picked up and processed by the tool like regular imputs.

Each instance of the tool is identified by the name of the server it manages. The API for each instance is available on the same domain the server is hosted on with the prefix “api-”. EG. api-ark.legendaryspork9.co.uk. Endpoints are authenticated through a client id and secret sent in the auth header of each API call.",
                        Contact = new OpenApiContact
                        {
                            Name = "API Team",
                            Email = "api@hunter-industries.co.uk"
                        }
                    };

                    document.Servers =
                    [
                        new OpenApiServer
                        {
                            Url = "https://api-servername.legendaryspork9.co.uk"
                        }
                    ];

                    document.Components ??= new OpenApiComponents();

                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                    document.Components.SecuritySchemes["basicAuth"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "basic",
                        Description = "Use your Client Id as the username and Client Secret as the password."
                    };

                    document.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecuritySchemeReference(
                                    "basicAuth",
                                    document,
                                    null),
                                []
                            }
                        }
                    ];

                    document.Tags = new HashSet<OpenApiTag>
                    {
                        new()
                        {
                            Name = "Logs",
                            Description = "The calls allowing the user to access the live/archived logs for the tool and server."
                        },
                        new()
                        {
                            Name = "Commands",
                            Description = "The calls allowing the user to send commands to the tool and server."
                        },
                        new()
                        {
                            Name = "Webhooks",
                            Description = "The calls allowing the user to register and unregister webhooks for realtime log notifications."
                        }
                    };

                    return Task.CompletedTask;
                });
            });

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Added OpenAPI Documentaion");

            AuthenticationModel authentication = builder.Configuration.GetSection("Authentication")
                .Get<AuthenticationModel>()!;

            builder.Services.AddSingleton(authentication);

            DatabaseOptionsModel options = builder.Configuration.GetSection("Database")
                .Get<DatabaseOptionsModel>()!;

            builder.Services.AddSingleton(options);

            ArchiveSettingsModel archiveSettings = builder.Configuration.GetSection("ArchiveSettings")
                .Get<ArchiveSettingsModel>()!;

            builder.Services.AddSingleton(archiveSettings);

            WebhookSettingsModel webhookSettings = builder.Configuration.GetSection("Webhook")
                .Get<WebhookSettingsModel>()!;

            builder.Services.AddSingleton(webhookSettings);

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Loaded Configuration");

            builder.Services.AddHttpContextAccessor();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Added HTTP Context Accessor");

            builder.Services.AddScoped<ILoggerService, LoggerServiceWrapper>();
            builder.Services.AddSingleton<IExtendedDatabase, ExtendedDatabaseWrapper>();
            builder.Services.AddSingleton<IExtendedFileSystem, ExtendedFileSystemWrapper>();
            builder.Services.AddSingleton<IClock, SystemClockProvider>();
            builder.Services.AddScoped<IWebhookRegistrationService, WebhookRegistrationService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IWebhookDispatchService>(sp =>
            {
                ILoggerService logger = sp.GetRequiredService<ILoggerService>();
                HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient();
                WebhookSettingsModel settings = sp.GetRequiredService<WebhookSettingsModel>();
                httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                System.Text.Json.JsonSerializerOptions jsonOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;

                return new WebhookDispatchService(
                    logger,
                    httpClient,
                    settings,
                    jsonOptions);
            });
            builder.Services.AddHostedService<Services.LogPollingService>();

            _logger.LogMessage(
               StandardValues.LoggerValues.Debug,
               "Configured Services");

            IConfigurationSection pemSection = builder.Configuration.GetSection("PemCertificate");

            if (pemSection.Exists())
            {
                string? certPath = pemSection["CertificatePath"];
                string? keyPath = pemSection["KeyPath"];
                string? certPassword = pemSection["Password"];
                string httpUrl = pemSection["HttpUrl"] ?? "http://localhost:5000";
                string httpsUrl = pemSection["HttpsUrl"] ?? "https://localhost:5001";

                if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(keyPath))
                {
                    ExtendedFileSystemWrapper fileSystem = new();

                    string certPem = fileSystem.ReadAllText(certPath)
                        .GetAwaiter()
                        .GetResult();
                    string keyPem = fileSystem.ReadAllText(keyPath)
                        .GetAwaiter()
                        .GetResult();

                    X509Certificate2 cert = CertificateFunction.LoadFromPem(
                        certPem,
                        keyPem,
                        certPassword);

                    builder.WebHost.ConfigureKestrel(serverOptions =>
                    {
                        Uri httpUri = new(httpUrl);

                        serverOptions.Listen(
                            httpUri.Host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(httpUri.Host),
                            httpUri.Port);

                        Uri httpsUri = new(httpsUrl);

                        serverOptions.Listen(
                            httpsUri.Host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(httpsUri.Host),
                            httpsUri.Port,
                            listenOptions =>
                            {
                                listenOptions.UseHttps(httpsOptions =>
                                {
                                    httpsOptions.ServerCertificate = cert;
                                });
                            });
                    });

                    _logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Configured Kestrel with PEM certificate");
                }
            }

            WebApplication app = builder.Build();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Built Application");

            app.MapOpenApi(pattern: "api/{document}.json");

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Mapped OpenAPI Documentation");

            app.MapScalarApiReference("/docs", options =>
            {
                options.OpenApiRoutePattern = "api/{document}.json";
                options.Title = "Server Backup Tool API";
                options.Favicon = "/Logo.ico";
                options.DefaultOpenAllTags = false;
                options.ForceDarkMode();
                options.ExpandAllTags();
                options.HideTestRequestButton();
            });

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Mapped Scalar Reference");

            app.UseAuthorization();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured Authorisation");

            app.MapControllers();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured Controllers");

            app.MapStaticAssets();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured Static Assets");
            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Running API");

            app.Run();
        }
    }
}
