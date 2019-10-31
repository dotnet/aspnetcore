using Microsoft.AspNetCore.Authorization;

namespace CustomPolicyProvider
{
    internal class MinimumValueAuthorizationRequirement : IAuthorizationRequirement
    {
        public int MinimumValue { get; }

        public MinimumValueAuthorizationRequirement(int minimumValue) { MinimumValue = minimumValue; }
    }
}
