using Asp.Versioning.ApiExplorer;
using Azure.Identity;
using Hangfire;
using Hellang.Middleware.ProblemDetails;
using PolicyService.API.Extensions;
using PolicyService.API.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//azure keyVault
var keyVaultUrl = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());

    Log.Information("Azure Key Vault configured: {KeyVaultUrl}", keyVaultUrl);
} 

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/policy-service-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting PolicyService API");

    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddCustomProblemDetails();


    builder.Services.AddApiVersioningConfiguration();
    builder.Services.AddVersionedSwagger();

    // Add application services (DbContext, repositories, etc.)
    builder.Services.AddApplicationServices(builder.Configuration);

    var app = builder.Build();

    // Configure middleware
   // NEW: Problem Details middleware FIRST (before custom exception handling)
     app.UseProblemDetails();

    // Remove old exception handling (Problem Details handles it now)
    // app.UseExceptionHandling(); // REMOVE THIS

    // Sanitizer (removes stack traces in production)
    app.UseProblemDetailsSanitizer();

    app.UseProblemDetailsEnricher();

    // Request logging SECOND
    app.UseRequestLogging();

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        // Build a swagger endpoint for each discovered API version
        foreach (var description in provider.ApiVersionDescriptions.Reverse())
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });


    // HTTPS redirection
    app.UseHttpsRedirection();

    // Authorization
    app.UseAuthorization();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DashboardTitle = "Policy Service Jobs",
        StatsPollingInterval = 2000, // Update stats every 2 seconds
        AppPath = null // Don't show back to site link
    });

    HangfireJobScheduler.ScheduleRecurringJobs();

    Log.Information("Hangfire dashboard available at /hangfire");

    // Map controllers
    app.MapControllers();

    Log.Information("Middleware pipeline configured successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}