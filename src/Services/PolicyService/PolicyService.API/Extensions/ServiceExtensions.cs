using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PolicyService.API.Resilience;
using PolicyService.Application.Commands;
using PolicyService.Application.Services;
using PolicyService.Domain.Interfaces;
using PolicyService.Domain.Strategies;
using PolicyService.Infrastructure.Caching;
using PolicyService.Infrastructure.ExternalServices;
using PolicyService.Infrastructure.Messaging;
using PolicyService.Infrastructure.Persistence;
using PolicyService.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.SqlServer;

namespace PolicyService.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Domain Services (Strategies)
            services.AddSingleton<IPremiumCalculationStrategy, HealthPremiumStrategy>();
            services.AddSingleton<IPremiumCalculationStrategy, LifePremiumStrategy>();
            //services.AddSingleton<IPremiumCalculationStrategy, AutoPremiumStrategy>();
            services.AddSingleton<PremiumCalculationStrategyFactory>();

            // Register Application Services (Command Handlers)
            services.AddScoped<CreatePolicyCommandHandler>();
            services.AddScoped<RefundService>();


            AddRedisCache(services, configuration);

            AddMessaging(services);


            services.AddScoped<PolicyRepository>(); // Concrete class
            services.AddScoped<IPolicyRepository>(provider =>
            {
                var repo = provider.GetRequiredService<PolicyRepository>();
                var cache = provider.GetRequiredService<IDistributedCache>();
                var logger = provider.GetRequiredService<ILogger<CachedPolicyRepository>>();

                return new CachedPolicyRepository(repo, cache, logger);
            });

            // Register Infrastructure Services (Repositories)
            //services.AddScoped<IPolicyRepository, PolicyRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register DbContext with Azure SQL or LocalDB
            AddDatabaseContext(services, configuration);

            AddHttpClients(services, configuration);

            //Hangfire
            AddHangfire(services, configuration);


            return services;
        }

        private static void AddDatabaseContext(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var useAzureSql = configuration.GetValue<bool>("UseAzureSql");

            var connectionString = useAzureSql ? configuration["AzureSqlConnectionString"]  // Reads from Key Vault!
                                    : configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured");
            }


            services.AddDbContext<PolicyDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Azure SQL specific settings
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);

                    // Command timeout (important for Azure SQL)
                    sqlOptions.CommandTimeout(60);

                    // Migrations assembly
                    sqlOptions.MigrationsAssembly("PolicyService.Infrastructure");
                });

                // Development settings
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });
        }

        private static void AddRedisCache(IServiceCollection services,IConfiguration configuration)
        {
            var redisConnection = configuration["RedisConnectionString"];

            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "PolicyService:";
                });

                Console.WriteLine("Redis cache configured");
            }
            else
            {
                services.AddDistributedMemoryCache();
                Console.WriteLine("Using in-memory cache (Redis not configured)");
            }
        }

        private static void AddMessaging(IServiceCollection services)
        {
            // Register message publisher as singleton (reuse connection)
            services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();

            Console.WriteLine("Service Bus messaging configured");
        }

        private static void AddHttpClients(IServiceCollection services, IConfiguration configuration)
        {
            var endBookingSystemUrl = configuration["EndBookingSystem:BaseUrl"]
                ?? "https://mock-endbooking-api.azurewebsites.net";

            // Create logger for Polly policies
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Polly");

            // Register End Booking System Client with Polly policies
            services.AddHttpClient<IEndBookingSystemClient, EndBookingSystemClient>(client =>
            {
                client.BaseAddress = new Uri(endBookingSystemUrl);
                client.Timeout = TimeSpan.FromSeconds(30); // Overall timeout
                client.DefaultRequestHeaders.Add("User-Agent", "PolicyService/1.0");
            })
            // Add Polly policies in order (executes from bottom to top)
            .AddPolicyHandler(PollyPolicies.GetRetryPolicy(logger))
            .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy(logger))
            .AddPolicyHandler(PollyPolicies.GetTimeoutPolicy())
            .AddPolicyHandler(PollyPolicies.GetBulkheadPolicy(logger));

            Console.WriteLine("End Booking System client configured with Polly resilience");
        }

        private static void AddHangfire( IServiceCollection services, IConfiguration configuration)
        {
            // Get database connection string
            var useAzureSql = configuration.GetValue<bool>("UseAzureSql");
            var connectionString = useAzureSql
                ? configuration["AzureSqlConnectionString"]
                : configuration.GetConnectionString("DefaultConnection");

            // Configure Hangfire to use SQL Server
            services.AddHangfire(config =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true,
                        SchemaName = "HangFire" // Separate schema for Hangfire tables
                    });
            });

            // Add Hangfire server
            var workerCount = configuration.GetValue<int>("Hangfire:WorkerCount", 5);
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = workerCount;
                options.ServerName = $"PolicyService-{Environment.MachineName}";
            });

            Console.WriteLine($"Hangfire configured | Workers: {workerCount}");
        }
    }
}