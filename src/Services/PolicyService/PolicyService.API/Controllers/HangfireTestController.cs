using Hangfire;
using Microsoft.AspNetCore.Mvc;
using PolicyService.API.Jobs;

namespace PolicyService.API.Controllers
{
    [ApiController]
    [Route("api/test/hangfire")]
    public class HangfireTestController : ControllerBase
    {
        private readonly ILogger<HangfireTestController> _logger;

        public HangfireTestController(ILogger<HangfireTestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Trigger DLQ reprocess job manually
        /// </summary>
        [HttpPost("reprocess-dlq")]
        public IActionResult ReprocessDLQ(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] string? errorFilter = null,
            [FromQuery] int maxMessages = 10)
        {
            var jobId = BackgroundJob.Enqueue<DeadLetterQueueReprocessJob>(
                job => job.ExecuteAsync(fromDate, null, errorFilter, maxMessages));

            _logger.LogInformation(
                "DLQ reprocess job enqueued | JobId: {JobId}",
                jobId);

            return Ok(new
            {
                message = "DLQ reprocess job enqueued",
                jobId,
                dashboardUrl = "/hangfire/jobs/details/" + jobId
            });
        }

        /// <summary>
        /// Trigger reconciliation report manually
        /// </summary>
        [HttpPost("run-reconciliation")]
        public IActionResult RunReconciliation()
        {
            var jobId = BackgroundJob.Enqueue<ReconciliationReportJob>(
                job => job.ExecuteAsync());

            _logger.LogInformation(
                "Reconciliation report job enqueued | JobId: {JobId}",
                jobId);

            return Ok(new
            {
                message = "Reconciliation report job enqueued",
                jobId,
                dashboardUrl = "/hangfire/jobs/details/" + jobId
            });
        }

        /// <summary>
        /// Trigger log cleanup manually
        /// </summary>
        [HttpPost("run-cleanup")]
        public IActionResult RunCleanup()
        {
            var jobId = BackgroundJob.Enqueue<LogCleanupJob>(
                job => job.ExecuteAsync());

            _logger.LogInformation(
                "Log cleanup job enqueued | JobId: {JobId}",
                jobId);

            return Ok(new
            {
                message = "Log cleanup job enqueued",
                jobId,
                dashboardUrl = "/hangfire/jobs/details/" + jobId
            });
        }
    }
}