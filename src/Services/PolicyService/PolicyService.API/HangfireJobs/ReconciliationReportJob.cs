using PolicyService.Infrastructure.Persistence;
using PolicyService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PolicyService.API.Jobs
{
    /// <summary>
    /// Generates daily reconciliation report
    /// Runs every day at 11 PM
    /// </summary>
    public class ReconciliationReportJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReconciliationReportJob> _logger;

        public ReconciliationReportJob(
            IServiceProvider serviceProvider,
            ILogger<ReconciliationReportJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[Hangfire] Starting daily reconciliation report...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PolicyDbContext>();

            try
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);

                _logger.LogInformation(
                    "Generating report for date: {Date}",
                    yesterday.ToString("yyyy-MM-dd"));

                // Get policy statistics
                var stats = await dbContext.Policies
                    .Where(p => p.CreatedAt >= yesterday && p.CreatedAt < today)
                    .GroupBy(p => p.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        TotalPremium = g.Sum(p => p.Premium)
                    })
                    .ToListAsync();

                var totalCreated = stats.Sum(s => s.Count);
                var totalPremium = stats.Sum(s => s.TotalPremium);

                var pending = stats.FirstOrDefault(s => s.Status == PolicyStatus.Pending);
                var approved = stats.FirstOrDefault(s => s.Status == PolicyStatus.Approved);
                var rejected = stats.FirstOrDefault(s => s.Status == PolicyStatus.Rejected);

                // Build report
                var report = $@"
╔══════════════════════════════════════════════════════════════╗
║          DAILY RECONCILIATION REPORT                         ║
║          Date: {yesterday:yyyy-MM-dd}                              ║
╚══════════════════════════════════════════════════════════════╝

📊 POLICY STATISTICS
─────────────────────────────────────────────────────────────
Total Policies Created:     {totalCreated,10}

Status Breakdown:
  ✅ Approved:              {approved?.Count ?? 0,10}  ({approved?.TotalPremium ?? 0:C})
  ❌ Rejected:              {rejected?.Count ?? 0,10}  ({rejected?.TotalPremium ?? 0:C})
  ⏳ Pending:               {pending?.Count ?? 0,10}  ({pending?.TotalPremium ?? 0:C})

💰 FINANCIAL SUMMARY
─────────────────────────────────────────────────────────────
Total Premium:              {totalPremium:C}
Average Premium:            {(totalCreated > 0 ? totalPremium / totalCreated : 0):C}

📈 SUCCESS RATE
─────────────────────────────────────────────────────────────
Approval Rate:              {(totalCreated > 0 ? (approved?.Count ?? 0) * 100.0 / totalCreated : 0):F2}%
Rejection Rate:             {(totalCreated > 0 ? (rejected?.Count ?? 0) * 100.0 / totalCreated : 0):F2}%
";

                _logger.LogInformation(report);

                // Check for stale policies (pending > 24 hours)
                var stalePolicies = await dbContext.Policies
                    .Where(p => p.Status == PolicyStatus.Pending
                             && p.CreatedAt < DateTime.UtcNow.AddHours(-24))
                    .CountAsync();

                if (stalePolicies > 0)
                {
                    _logger.LogWarning(
                        "ALERT: {Count} policies have been pending for more than 24 hours",
                        stalePolicies);
                }

                // In production:
                // - Save report to blob storage
                // - Email to management/operations
                // - Store metrics in monitoring system
                // await _blobService.UploadReportAsync($"reconciliation-{yesterday:yyyyMMdd}.txt", report);
                // await _emailService.SendReportAsync("Daily Reconciliation", report, "operations@company.com");

                _logger.LogInformation("[Hangfire] Daily reconciliation report completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Reconciliation report failed");
                throw;
            }
        }
    }
}