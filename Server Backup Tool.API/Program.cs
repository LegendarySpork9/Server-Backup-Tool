// Copyright © - Unpublished - Toby Hunter
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace ServerBackupTool.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

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
                            Name = "WeatherForecast",
                            Description = "Randomly generates some weather data as an example for how the API controller works."
                        }
                    };

                    return Task.CompletedTask;
                });
            });

            WebApplication app = builder.Build();

            app.MapOpenApi(pattern: "api/{document}.json");

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

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapStaticAssets();

            app.Run();
        }
    }
}
