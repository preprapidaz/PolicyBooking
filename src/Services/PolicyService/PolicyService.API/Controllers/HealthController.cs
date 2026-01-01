using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PolicyService.Domain.Interfaces;

namespace PolicyService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Health check endpoint called");

            return Ok(new
            {
                status = "Healthy",
                service = "PolicyService",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("error")]
        public IActionResult ThrowError()
        {
            // Test exception middleware
            throw new InvalidOperationException("This is a test exception!");
        }

        [HttpPost("echo")]
        public IActionResult Echo([FromBody] object data)
        {
            // Test request body logging
            return Ok(new
            {
                message = "Echo received",
                receivedData = data,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("cache")]
        public async Task<IActionResult> CheckCache()
        {
            try
            {
                var cache = HttpContext.RequestServices
                    .GetRequiredService<IDistributedCache>();

                // Test write
                var testKey = "health:test";
                var testValue = $"Test-{DateTime.UtcNow:HH:mm:ss}";

                await cache.SetStringAsync(testKey, testValue, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });

                // Test read
                var cached = await cache.GetStringAsync(testKey);

                // Cleanup
                await cache.RemoveAsync(testKey);

                return Ok(new
                {
                    status = "Connected",
                    cacheWorking = cached == testValue,
                    testValue = cached,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Error", error = ex.Message });
            }
        }

        [HttpGet("servicebus")]
        public async Task<IActionResult> CheckServiceBus()
        {
            try
            {
                var publisher = HttpContext.RequestServices
                    .GetRequiredService<IMessagePublisher>();

                // Send test message
                await publisher.SendMessageAsync(
                    "policy-processing-queue",
                    new { test = "health-check", timestamp = DateTime.UtcNow },
                    "health-check");

                return Ok(new
                {
                    status = "Connected",
                    messageSent = true,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    error = ex.Message
                });
            }
        }
    }
}
