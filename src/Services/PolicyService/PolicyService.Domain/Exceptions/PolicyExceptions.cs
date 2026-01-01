namespace PolicyService.Domain.Exceptions
{
    /// <summary>
    /// Base exception for all policy-related errors
    /// </summary>
    public abstract class PolicyException : Exception
    {
        public string ErrorCode { get; }

        protected PolicyException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Thrown when policy is not found
    /// </summary>
    public class PolicyNotFoundException : PolicyException
    {
        public Guid PolicyId { get; }

        public PolicyNotFoundException(Guid policyId)
            : base($"Policy with ID {policyId} was not found", "POLICY_NOT_FOUND")
        {
            PolicyId = policyId;
        }
    }

    /// <summary>
    /// Thrown when policy number already exists
    /// </summary>
    public class DuplicatePolicyException : PolicyException
    {
        public string PolicyNumber { get; }

        public DuplicatePolicyException(string policyNumber)
            : base($"Policy with number {policyNumber} already exists", "DUPLICATE_POLICY")
        {
            PolicyNumber = policyNumber;
        }
    }

    /// <summary>
    /// Thrown when policy validation fails
    /// </summary>
    public class PolicyValidationException : PolicyException
    {
        public Dictionary<string, string[]> ValidationErrors { get; }

        public PolicyValidationException(Dictionary<string, string[]> errors)
            : base("Policy validation failed", "POLICY_VALIDATION_FAILED")
        {
            ValidationErrors = errors;
        }

        public PolicyValidationException(string field, string error)
            : base("Policy validation failed", "POLICY_VALIDATION_FAILED")
        {
            ValidationErrors = new Dictionary<string, string[]>
            {
                { field, new[] { error } }
            };
        }
    }

    /// <summary>
    /// Thrown when attempting invalid state transition
    /// </summary>
    public class InvalidPolicyStateException : PolicyException
    {
        public string CurrentState { get; }
        public string AttemptedAction { get; }

        public InvalidPolicyStateException(string currentState, string attemptedAction)
            : base($"Cannot {attemptedAction} a policy in {currentState} state", "INVALID_STATE_TRANSITION")
        {
            CurrentState = currentState;
            AttemptedAction = attemptedAction;
        }
    }

    /// <summary>
    /// Thrown when premium calculation fails
    /// </summary>
    public class PremiumCalculationException : PolicyException
    {
        public PremiumCalculationException(string message)
            : base(message, "PREMIUM_CALCULATION_FAILED")
        {
        }
    }
}