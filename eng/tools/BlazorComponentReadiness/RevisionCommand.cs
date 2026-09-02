// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class RevisionCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        string? previousPath = null;
        string? revisedPath = null;
        string? identifiers = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--previous":
                    if (!TryReadValue(args, ref index, out previousPath))
                    {
                        return MissingValue("--previous", error);
                    }

                    break;
                case "--changed-ids":
                    if (!TryReadValue(args, ref index, out identifiers))
                    {
                        return MissingValue("--changed-ids", error);
                    }

                    break;
                default:
                    if (args[index].StartsWith("--", StringComparison.Ordinal))
                    {
                        error.WriteLine($"ERROR: unknown option '{args[index]}'.");
                        return 1;
                    }

                    if (revisedPath is not null)
                    {
                        error.WriteLine(
                            "ERROR: only one revised report path may be supplied.");
                        return 1;
                    }

                    revisedPath = args[index];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(previousPath))
        {
            error.WriteLine("ERROR: --previous requires a report path.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(revisedPath))
        {
            error.WriteLine("ERROR: a revised report path is required.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(identifiers))
        {
            error.WriteLine(
                "ERROR: --changed-ids requires a comma-separated requirement list.");
            return 1;
        }

        try
        {
            var changedIdentifiers = identifiers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);
            if (changedIdentifiers.Count == 0)
            {
                error.WriteLine(
                    "ERROR: --changed-ids requires a comma-separated requirement list.");
                return 1;
            }

            var previous = ScorecardValidator.ReadReportSnapshot(previousPath);
            var revised = ScorecardValidator.ReadReportSnapshot(revisedPath);
            var errors = RevisionValidator.Validate(
                previous,
                revised,
                changedIdentifiers);
            if (errors.Count > 0)
            {
                foreach (var validationError in errors)
                {
                    error.WriteLine($"ERROR: {validationError}");
                }

                return 1;
            }

            output.WriteLine(
                $"Revision validation passed for {changedIdentifiers.Count} changed " +
                "requirement ID(s).");
            return 0;
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            InvalidDataException or
            System.Text.Json.JsonException)
        {
            error.WriteLine($"ERROR: {exception.Message}");
            return 1;
        }
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        out string value)
    {
        if (index + 1 >= args.Length ||
            args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }

    private static int MissingValue(string option, TextWriter error)
    {
        error.WriteLine($"ERROR: {option} requires a value.");
        return 1;
    }
}
