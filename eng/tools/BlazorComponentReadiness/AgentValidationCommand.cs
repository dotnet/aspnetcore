// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class AgentValidationCommand
{
    private const string DefaultAgentProfile =
        ".github/agents/blazor-component-readiness.agent.md";

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var agentProfile = DefaultAgentProfile;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] != "--agent-profile")
            {
                error.WriteLine($"Unknown option '{args[index]}'.");
                return 1;
            }

            if (index + 1 >= args.Length)
            {
                error.WriteLine("--agent-profile requires a value.");
                return 1;
            }

            agentProfile = args[++index];
        }

        if (string.IsNullOrWhiteSpace(agentProfile))
        {
            error.WriteLine("ERROR: --agent-profile requires a non-empty value.");
            return 1;
        }

        var errors = SkillValidator.Validate(SkillLayout.Create(agentProfile));
        if (errors.Count > 0)
        {
            foreach (var validationError in errors)
            {
                error.WriteLine($"ERROR: {validationError}");
            }

            return 1;
        }

        output.WriteLine(
            $"Agent structure is valid: {SkillValidator.ExpectedCoreRequirementCount} core " +
            "requirements, 12 optional overlay requirements, complete area mapping, " +
            "and governed Vally eval coverage.");
        return 0;
    }
}
