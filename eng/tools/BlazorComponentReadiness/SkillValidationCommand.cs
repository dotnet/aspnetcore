// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class SkillValidationCommand
{
    private const string DefaultSkillDirectory =
        ".github/skills/blazor-component-readiness";

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var skillDirectory = DefaultSkillDirectory;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] != "--skill-dir")
            {
                error.WriteLine($"Unknown option '{args[index]}'.");
                return 1;
            }

            if (index + 1 >= args.Length)
            {
                error.WriteLine("--skill-dir requires a value.");
                return 1;
            }

            skillDirectory = args[++index];
        }

        if (string.IsNullOrWhiteSpace(skillDirectory))
        {
            error.WriteLine("ERROR: --skill-dir requires a non-empty value.");
            return 1;
        }

        var errors = SkillValidator.Validate(SkillLayout.Create(skillDirectory));
        if (errors.Count > 0)
        {
            foreach (var validationError in errors)
            {
                error.WriteLine($"ERROR: {validationError}");
            }

            return 1;
        }

        output.WriteLine(
            $"Skill structure is valid: {SkillValidator.ExpectedCoreRequirementCount} core " +
            "requirements, 12 optional overlay requirements, complete area mapping, " +
            "and governed Vally eval coverage.");
        return 0;
    }
}
