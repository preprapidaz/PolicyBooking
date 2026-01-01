using PolicyService.Domain.Messages;
using System.Text;

namespace PolicyService.Worker.Services
{
    /// <summary>
    /// Generates .txt files for end booking system
    /// </summary>
    public class FileGenerationService
    {
        private readonly ILogger<FileGenerationService> _logger;
        private readonly IConfiguration _configuration;

        public FileGenerationService(
            ILogger<FileGenerationService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Generate pipe-delimited .txt file
        /// </summary>
        public async Task<string> GeneratePipeDelimitedFileAsync(
            ProcessPolicyCommand policy,
            string correlationId)
        {
            var outputPath = _configuration["FileGeneration:OutputPath"]
                ?? @"D:\PolicyBooking-FlatFiles\Outbound";

            // Create directory if it doesn't exist
            Directory.CreateDirectory(outputPath);

            // Filename: POLICY_YYYYMMDD_HHMMSS_CorrelationId.txt
            var fileName = $"POLICY_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{correlationId[..8]}.txt";
            var filePath = Path.Combine(outputPath, fileName);

            var content = new StringBuilder();

            // Header
            content.AppendLine("POLICY_SUBMISSION|V1.0|" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            content.AppendLine();

            // Policy Details
            content.AppendLine($"POLICY_NUMBER|{policy.PolicyNumber}");
            content.AppendLine($"CUSTOMER_NAME|{policy.CustomerName}");
            content.AppendLine($"CUSTOMER_EMAIL|{policy.CustomerEmail}");
            content.AppendLine($"CUSTOMER_AGE|{policy.CustomerAge}");
            content.AppendLine($"PREMIUM|{policy.Premium:F2}");
            content.AppendLine($"SUBMITTED_DATE|{DateTime.UtcNow:yyyy-MM-dd}");
            content.AppendLine($"SUBMITTED_TIME|{DateTime.UtcNow:HH:mm:ss}");
            content.AppendLine($"CORRELATION_ID|{correlationId}");
            content.AppendLine($"SOURCE_SYSTEM|PolicyService");

            // Footer
            content.AppendLine();
            content.AppendLine("END_OF_FILE");

            // Write to file
            await File.WriteAllTextAsync(filePath, content.ToString());

            _logger.LogInformation(
                "Generated file | Name: {FileName} | Policy: {PolicyNumber}",
                fileName,
                policy.PolicyNumber);

            return filePath;
        }
    }
}