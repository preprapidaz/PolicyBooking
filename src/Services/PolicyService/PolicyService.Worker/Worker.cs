using Azure.Messaging.ServiceBus;
using PolicyService.Worker.Services;

namespace PolicyService.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly PolicyProcessingService _processingService;
        private ServiceBusProcessor? _processor;
        private ServiceBusClient? _client;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration configuration,
            PolicyProcessingService processingService)
        {
            _logger = logger;
            _configuration = configuration;
            _processingService = processingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Policy Processing Worker starting...");

            try
            {
                // Get Service Bus connection string
                var connectionString = _configuration["AzureServiceBusConnectionString"];
                var queueName = _configuration["ServiceBus:QueueName"];
                var maxConcurrentCalls = _configuration.GetValue<int>("ServiceBus:MaxConcurrentCalls");

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("ServiceBusConnectionString not configured");
                    return;
                }

                // Create Service Bus client
                _client = new ServiceBusClient(connectionString);

                // Create processor options
                var options = new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = maxConcurrentCalls,
                    AutoCompleteMessages = false, // We'll complete manually
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
                };

                // Create processor
                _processor = _client.CreateProcessor(queueName, options);

                // Register message handler
                _processor.ProcessMessageAsync += async (args) =>
                {
                    await _processingService.ProcessPolicyAsync(args, stoppingToken);
                };
                _processor.ProcessErrorAsync += _processingService.HandleErrorAsync;

                // Start processing
                await _processor.StartProcessingAsync(stoppingToken);

                _logger.LogInformation(
                    "Worker started | Queue: {Queue} | MaxConcurrent: {Max}",
                    queueName,
                    maxConcurrentCalls);

                _logger.LogInformation("Listening for messages...");

                // Keep running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    _logger.LogDebug("Heartbeat");
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Worker failed to start");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping...");

            if (_processor != null)
            {
                await _processor.StopProcessingAsync(cancellationToken);
                await _processor.DisposeAsync();
            }

            if (_client != null)
            {
                await _client.DisposeAsync();
            }

            _logger.LogInformation("Worker stopped");

            await base.StopAsync(cancellationToken);
        }
    }
}