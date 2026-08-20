// Copyright © - Unpublished - Toby Hunter
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Filters;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Values;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;

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

Each instance of the tool is identified by the name of the server it manages. The API for each instance is available on the same domain the server is hosted on with the prefix “/api”. EG. Ark.legendaryspork9.co.uk/api. Endpoints are authenticated through a client id and secret sent in the auth header of each API call.",
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
                            Url = "https://gamehost.legendaryspork9.co.uk/api"
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

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Loaded Configuration");

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ILoggerService, LoggerServiceWrapper>();
            builder.Services.AddSingleton<IDatabase, DatabaseWrapper>();
            builder.Services.AddSingleton<IExtendedFileSystem, ExtendedFileSystemWrapper>();

            _logger.LogMessage(
               StandardValues.LoggerValues.Debug,
               "Configured Services");

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
                options.ForceDarkMode();
                options.ExpandAllResponses();
                options.ExpandAllModelSections();
                options.HideTestRequestButton();
            });

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Mapped Scalar Reference");

            app.UseHttpsRedirection();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured HTTPS Redirection");

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
