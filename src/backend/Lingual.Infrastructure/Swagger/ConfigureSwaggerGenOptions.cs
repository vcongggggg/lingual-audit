using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lingual.Infrastructure.Swagger;

public sealed class ConfigureSwaggerGenOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        if (!provider.ApiVersionDescriptions.Any())
        {
            options.SwaggerDoc("v1",
                new()
                {
                    Title = "Lingual API v1",
                    Description = "Modular Monolith Backend API for Lingual Social Language Learning Platform",
                    Version = "v1",
                    Contact = new() { Name = "Minh Nguyen", Email = "minhcong19082005@gmail.com" },
                    License = new() { Name = "MIT", Url = new("https://opensource.org/licenses/MIT") }
                });
        }
        else
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName,
                    new()
                    {
                        Title = $"Lingual API {description.ApiVersion}",
                        Description = "Modular Monolith Backend API for Lingual Social Language Learning Platform",
                        Version = description.ApiVersion.ToString(),
                        Contact = new() { Name = "Minh Nguyen", Email = "minhcong19082005@gmail.com" },
                        License = new() { Name = "MIT", Url = new("https://opensource.org/licenses/MIT") }
                    });
            }
        }

        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
            new()
            {
                Name = "Authorization",
                Description = "Enter the Bearer Authorization string as following: `Generated-JWT-Token`",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });

        options.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Name = JwtBearerDefaults.AuthenticationScheme,
                    In = ParameterLocation.Header,
                    Reference = new()
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                },
                new List<string>()
            }
        });
    }
}