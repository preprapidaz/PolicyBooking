using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PolicyService.API.Models;
using PolicyService.Application.Commands;
using PolicyService.Domain.Exceptions;
using PolicyService.Domain.Interfaces;

namespace PolicyService.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class PolicyController : ControllerBase
    {
        private readonly CreatePolicyCommandHandler _createPolicyHandler;
        private readonly IPolicyRepository _policyRepository;
        private readonly ILogger<PolicyController> _logger;

        public PolicyController(
            CreatePolicyCommandHandler createPolicyHandler,
            IPolicyRepository policyRepository,
            ILogger<PolicyController> logger)
        {
            _createPolicyHandler = createPolicyHandler;
            _policyRepository = policyRepository;
            _logger = logger;
        }

        /// <summary>
        /// Create a new insurance policy (V1)
        /// </summary>
        /// <param name="command">Policy creation details</param>
        /// <returns>Created policy details</returns>
        [HttpPost]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyCommand command)
        {
            // No try-catch needed! Problem Details middleware handles exceptions
            _logger.LogInformation("V1: Creating policy for {CustomerName}", command.CustomerName);

            var result = await _createPolicyHandler.HandleAsync(command);

            var response = ApiResponse<CreatePolicyResponse>.SuccessResponse(
                result,
                "Policy created successfully");

            return CreatedAtAction(
                nameof(GetPolicy),
                new { version = "1.0", id = result.PolicyId },
                response);
        }

        [HttpGet("{id}")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetPolicy(Guid id)
        {
            var policy = await _policyRepository.GetByIdAsync(id);

            if (policy == null)
                throw new PolicyNotFoundException(id); // ← Custom exception!

            var policyResponse = new PolicyV1Response
            {
                PolicyId = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                CustomerName = policy.CustomerName,
                CustomerEmail = policy.CustomerEmail,
                CustomerAge = policy.CustomerAge,
                PolicyType = policy.PolicyType.ToString(),
                Premium = policy.Premium,
                Status = policy.Status.ToString(),
                CreatedAt = policy.CreatedAt
            };

            var response = ApiResponse<PolicyV1Response>.SuccessResponse(policyResponse);

            return Ok(response);
        }

        /// <summary>
        /// Get all policies (V1)
        /// </summary>
        [HttpGet]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(IEnumerable<PolicyV1Response>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPolicies()
        {
            var policies = await _policyRepository.GetAllAsync();

            var response = policies.Select(p => new PolicyV1Response
            {
                PolicyId = p.Id,
                PolicyNumber = p.PolicyNumber,
                CustomerName = p.CustomerName,
                CustomerEmail = p.CustomerEmail,
                CustomerAge = p.CustomerAge,
                PolicyType = p.PolicyType.ToString(),
                Premium = p.Premium,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt
            });

            return Ok(response);
        }
    }

    // V1 Response DTO
    public class PolicyV1Response
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
    }
}