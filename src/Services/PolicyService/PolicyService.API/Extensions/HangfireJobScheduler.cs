using Hangfire;
using PolicyService.API.Jobs;

namespace PolicyService.API.Extensions
{
    public static class HangfireJobScheduler
    {
        public static void ScheduleRecurringJobs()
        {
            Console.WriteLine("Scheduling Hangfire recurring jobs...");

            // Job 1: DLQ Monitor - Every 15 minutes
            RecurringJob.AddOrUpdate<DeadLetterQueueMonitorJob>(
                "dlq-monitor",
                job => job.ExecuteAsync(),
                "*/15 * * * *", // Cron: Every 15 minutes
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });

            Console.WriteLine("Scheduled: DLQ Monitor (Every 15 minutes)");

            // Job 2: Daily Reconciliation - Every day at 11 PM
            RecurringJob.AddOrUpdate<ReconciliationReportJob>(
                "daily-reconciliation",
                job => job.ExecuteAsync(),
                Cron.Daily(23, 0), // 11:00 PM daily
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });

            Console.WriteLine("Scheduled: Daily Reconciliation (11:00 PM daily)");

            // Job 3: Log Cleanup - Every Sunday at 2 AM
            RecurringJob.AddOrUpdate<LogCleanupJob>(
                "weekly-cleanup",
                job => job.ExecuteAsync(),
                Cron.Weekly(DayOfWeek.Sunday, 2, 0), // Sunday 2:00 AM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });

            Console.WriteLine("Scheduled: Weekly Cleanup (Sunday 2:00 AM)");

            Console.WriteLine("All Hangfire jobs scheduled successfully");
        }
    }
}