using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PolicyService.Application.Commands;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Interfaces;
using System.Diagnostics;

namespace PolicyService.API.Controllers
{
    /// <summary>
    /// Policy Controller V2 - Enhanced with additional fields
    /// </summary>
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/policy")]
    [Produces("application/json")]
    public class PolicyV2Controller : ControllerBase
    {
        private readonly CreatePolicyCommandHandler _createPolicyHandler;
        private readonly IPolicyRepository _policyRepository;
        private readonly ILogger<PolicyV2Controller> _logger;

        public PolicyV2Controller(
            CreatePolicyCommandHandler createPolicyHandler,
            IPolicyRepository policyRepository,
            ILogger<PolicyV2Controller> logger)
        {
            _createPolicyHandler = createPolicyHandler;
            _policyRepository = policyRepository;
            _logger = logger;
        }

        /// <summary>
        /// Create a new insurance policy (V2 - Enhanced)
        /// </summary>
        [HttpPost]
        [MapToApiVersion("2.0")]
        [ProducesResponseType(typeof(CreatePolicyResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyCommand command)
        {
            try
            {
                _logger.LogInformation("V2: Creating policy for {CustomerName}", command.CustomerName);

                var result = await _createPolicyHandler.HandleAsync(command);

                return CreatedAtAction(
                    nameof(GetPolicy),
                    new { version = "2.0", id = result.PolicyId },
                    result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get policy by ID (V2 - Enhanced with metadata)
        /// </summary>
        [HttpGet("{id}")]
        [MapToApiVersion("2.0")]
        [ProducesResponseType(typeof(PolicyV2Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPolicy(Guid id)
        {
            var policy = await _policyRepository.GetByIdAsync(id);

            if (policy == null)
                return NotFound(new { error = $"Policy {id} not found" });

            // V2 Enhanced Response with metadata
            return Ok(new PolicyV2Response
            {
                // Core data
                Policy = new PolicyDataV2
                {
                    PolicyId = policy.Id,
                    PolicyNumber = policy.PolicyNumber,
                    CustomerName = policy.CustomerName,
                    CustomerEmail = policy.CustomerEmail,
                    CustomerAge = policy.CustomerAge,
                    PolicyType = policy.PolicyType.ToString(),
                    Premium = policy.Premium,
                    Status = policy.Status.ToString(),
                    CreatedAt = policy.CreatedAt,
                    ProcessedAt = policy.ProcessedAt
                },
                // Metadata (NEW in V2!)
                Metadata = new PolicyMetadata
                {
                    ApiVersion = "2.0",
                    IsActive = policy.Status == Domain.Enums.PolicyStatus.Approved,
                    DaysActive = policy.ProcessedAt.HasValue
                        ? (DateTime.UtcNow - policy.ProcessedAt.Value).Days
                        : 0,
                    RiskLevel = CalculateRiskLevel(policy.CustomerAge, policy.Premium)
                },
                // Links (HATEOAS - NEW in V2!)
                Links = new PolicyLinks
                {
                    Self = $"/api/v2/policy/{policy.Id}",
                    Cancel = policy.Status == Domain.Enums.PolicyStatus.Approved
                        ? $"/api/v2/policy/{policy.Id}/cancel"
                        : null,
                    Renew = policy.Status == Domain.Enums.PolicyStatus.Approved
                        ? $"/api/v2/policy/{policy.Id}/renew"
                        : null
                }
            });
        }

        private string CalculateRiskLevel(int age, decimal premium)
        {
            if (age > 65 || premium > 5000) return "High";
            if (age > 50 || premium > 2000) return "Medium";
            return "Low";
        }
    }

    // V2 Enhanced Response DTOs
    public class PolicyV2Response
    {
        public PolicyDataV2 Policy { get; set; }
        public PolicyMetadata Metadata { get; set; }
        public PolicyLinks Links { get; set; }
    }

    public class PolicyDataV2
    {
        public Guid PolicyId { get; set; }
        public string PolicyNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public int CustomerAge { get; set; }
        public string PolicyType { get; set; }
        public decimal Premium { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class PolicyMetadata
    {
        public string ApiVersion { get; set; }
        public bool IsActive { get; set; }
        public int DaysActive { get; set; }
        public string RiskLevel { get; set; }
    }

    public class PolicyLinks
    {
        public string Self { get; set; }
        public string Cancel { get; set; }
        public string Renew { get; set; }
    }
}