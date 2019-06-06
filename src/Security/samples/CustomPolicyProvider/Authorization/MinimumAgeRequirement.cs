using Microsoft.AspNetCore.Authorization;

namespace CustomPolicyProvider
{
    internal class MinimumAgeRequirement : IAuthorizationRequirement
    {
        public int Age { get; private set; }

        public MinimumAgeRequirement(int age) { Age = age; }
    }
}