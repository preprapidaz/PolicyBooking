using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using PolicyService.Infrastructure.ExternalServices;
using PolicyService.Infrastructure.Persistence;
using PolicyService.Worker;
using PolicyService.Worker.Services;
using Serilog;
using PolicyService.Worker.Resilience;
using PolicyService.Infrastructure.ExternalServices;
using Polly.CircuitBreaker;

// Configure Serilog FIRST (before anything else)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Changed to Debug for troubleshooting
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/worker-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("========================================");
    Log.Information("Starting Policy Processing Worker");
    Log.Information("========================================");

    var builder = Host.CreateApplicationBuilder(args);

    Log.Information("Configuration files loaded");

    // Configure Azure Key Vault
    var keyVaultUrl = builder.Configuration["KeyVault:VaultUri"];
    Log.Information("Key Vault URL from config: {Url}", keyVaultUrl ?? "NOT SET");

    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        try
        {
            Log.Information("Attempting to connect to Key Vault...");
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());

            Log.Information("Azure Key Vault configured");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Key Vault - continuing without it");
        }
    }
    else
    {
        Log.Warning("Key Vault not configured");
    }

    // Add Serilog
    builder.Services.AddSerilog();
    Log.Information("Serilog configured");

    // Add Database Context
    var useAzureSql = builder.Configuration.GetValue<bool>("UseAzureSql");
    Log.Information("Use Azure SQL: {UseAzureSql}", useAzureSql);

    var connectionString = useAzureSql
        ? builder.Configuration["AzureSqlConnectionString"]
        : builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Error("Database connection string not found!");
        throw new InvalidOperationException("Database connection string is missing");
    }

    Log.Information("Database connection string found (length: {Length})", connectionString.Length);

    builder.Services.AddDbContext<PolicyDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
            sqlOptions.MigrationsAssembly("PolicyService.Infrastructure");
        });
    });

    Log.Information("Database context configured");

    var endBookingSystemUrl = builder.Configuration["EndBookingSystem:BaseUrl"]
    ?? "https://httpstat.us";

    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var pollyLogger = loggerFactory.CreateLogger("Polly");

    builder.Services.AddHttpClient<IEndBookingSystemClient, EndBookingSystemClient>(client =>
    {
        client.BaseAddress = new Uri(endBookingSystemUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(PollyPolicies.GetRetryPolicy(pollyLogger))
    .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy(pollyLogger))
    .AddPolicyHandler(PollyPolicies.GetTimeoutPolicy());

    // Add Services
    builder.Services.AddSingleton<PolicyProcessingService>();
    builder.Services.AddSingleton<FileGenerationService>();
    Log.Information("Processing services registered");

    // Add Worker Service
    builder.Services.AddHostedService<Worker>();
    Log.Information("Worker service registered");

    var host = builder.Build();

    Log.Information("========================================");
    Log.Information("Worker configured successfully");
    Log.Information("========================================");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
    Log.Fatal("Press any key to exit...");
    Console.ReadKey(); // Keep console open to see error
}
finally
{
    Log.CloseAndFlush();
}