using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PolicyService.API.Extensions
{
    /// <summary>
    /// Extension methods for API versioning configuration
    /// </summary>
    public static class ApiVersioningExtensions
    {
        public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
        {
            // Add API Versioning
            services.AddApiVersioning(options =>
            {
                // Specify the default API version
                options.DefaultApiVersion = new ApiVersion(1, 0);

                // Use default version when client doesn't specify
                options.AssumeDefaultVersionWhenUnspecified = true;

                // Report API versions in response headers
                options.ReportApiVersions = true;

                // Read version from URL segment
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                // Format version as 'v'major.minor
                options.GroupNameFormat = "'v'VVV";

                // Substitute version in URL
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        public static IServiceCollection AddVersionedSwagger(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            services.AddSwaggerGen(options =>
            {
                // Add XML comments if you want
                options.OperationFilter<SwaggerDefaultValues>();
            });

            return services;
        }
    }

    /// <summary>
    /// Configures Swagger for API versioning
    /// </summary>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            // Add a swagger document for each discovered API version
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = $"Policy Service API {description.ApiVersion}",
                        Version = description.ApiVersion.ToString(),
                        Description = description.IsDeprecated
                            ? "⚠️ This API version has been deprecated"
                            : "Microservice for managing insurance policies",
                        Contact = new Microsoft.OpenApi.Models.OpenApiContact
                        {
                            Name = "Policy Service Team",
                            Email = "policy-service@company.com"
                        }
                    });
            }
        }
    }

    /// <summary>
    /// Adds default values to Swagger operations
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(
            Microsoft.OpenApi.Models.OpenApiOperation operation,
            Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            operation.Deprecated |= apiDescription.IsDeprecated();

            if (operation.Parameters == null)
                return;

            foreach (var parameter in operation.Parameters)
            {
                var description = apiDescription.ParameterDescriptions
                    .First(p => p.Name == parameter.Name);

                parameter.Description ??= description.ModelMetadata?.Description;

                if (parameter.Schema.Default == null &&
                    description.DefaultValue != null)
                {
                    parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(
                        description.DefaultValue.ToString());
                }

                parameter.Required |= description.IsRequired;
            }
        }
    }
}