namespace PolicyService.API.Jobs
{
    /// <summary>
    /// Cleans up old log files and generated policy files
    /// Runs weekly on Sunday at 2 AM
    /// </summary>
    public class LogCleanupJob
    {
        private readonly ILogger<LogCleanupJob> _logger;
        private readonly IConfiguration _configuration;

        public LogCleanupJob(
            ILogger<LogCleanupJob> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[Hangfire] Starting log cleanup job...");

            try
            {
                var logRetentionDays = _configuration.GetValue<int>("Hangfire:LogRetentionDays", 30);
                var fileRetentionDays = _configuration.GetValue<int>("Hangfire:FileRetentionDays", 90);

                // Cleanup 1: Delete old log files
                await CleanupLogFilesAsync(logRetentionDays);

                // Cleanup 2: Delete old policy files
                await CleanupPolicyFilesAsync(fileRetentionDays);

                _logger.LogInformation("[Hangfire] Log cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Log cleanup failed");
                throw;
            }
        }

        private async Task CleanupLogFilesAsync(int retentionDays)
        {
            var logPath = "logs"; // From Serilog configuration
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);

            if (!Directory.Exists(logPath))
            {
                _logger.LogWarning("Log directory not found: {Path}", logPath);
                return;
            }

            var logFiles = Directory.GetFiles(logPath, "*.txt");
            var deletedCount = 0;
            long deletedSize = 0;

            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime < cutoffDate)
                {
                    var size = fileInfo.Length;
                    File.Delete(file);
                    deletedCount++;
                    deletedSize += size;

                    _logger.LogInformation(
                        "Deleted old log file | File: {FileName} | Size: {Size} KB | Age: {Days} days",
                        fileInfo.Name,
                        size / 1024,
                        (DateTime.Now - fileInfo.CreationTime).Days);
                }
            }

            _logger.LogInformation(
                "Log file cleanup complete | Deleted: {Count} files | Space freed: {Size} MB | Retention: {Days} days",
                deletedCount,
                deletedSize / 1024 / 1024,
                retentionDays);

            await Task.CompletedTask;
        }

        private async Task CleanupPolicyFilesAsync(int retentionDays)
        {
            var fileGenPath = _configuration["FileGeneration:OutputPath"];

            if (string.IsNullOrEmpty(fileGenPath) || !Directory.Exists(fileGenPath))
            {
                _logger.LogInformation("Policy file directory not configured or doesn't exist");
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var policyFiles = Directory.GetFiles(fileGenPath, "POLICY_*.txt");
            var deletedCount = 0;
            long deletedSize = 0;

            foreach (var file in policyFiles)
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime < cutoffDate)
                {
                    var size = fileInfo.Length;
                    File.Delete(file);
                    deletedCount++;
                    deletedSize += size;

                    _logger.LogInformation(
                        "Deleted old policy file | File: {FileName} | Age: {Days} days",
                        fileInfo.Name,
                        (DateTime.Now - fileInfo.CreationTime).Days);
                }
            }

            _logger.LogInformation(
                "Policy file cleanup complete | Deleted: {Count} files | Space freed: {Size} MB | Retention: {Days} days",
                deletedCount,
                deletedSize / 1024 / 1024,
                retentionDays);

            await Task.CompletedTask;
        }
    }
}