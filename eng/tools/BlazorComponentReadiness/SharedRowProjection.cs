// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace BlazorComponentReadiness;

internal sealed record SharedRowProjection(
    int SchemaVersion,
    string SourceLedgerSha256,
    IReadOnlyList<SharedRowProjectionRow> Rows);

internal sealed record SharedRowProjectionRow(
    string Identifier,
    string Requirement,
    string RequirementScope,
    string Status,
    string EvidenceAnchors,
    string MaintainerAction,
    string ReviewerFollowUp);

internal static class SharedRowProjectionParser
{
    private const int SchemaVersion = 1;

    internal static SharedRowProjection Parse(ReportSnapshot snapshot)
    {
        using var document = JsonDocument.Parse(snapshot.Bytes);
        var root = document.RootElement;
        RequireObject(root, "shared-row projection");
        RequireProperties(
            root,
            ["rows", "schema_version", "source_ledger_sha256"],
            "shared-row projection");

        var version = GetRequiredInt32(root, "schema_version");
        if (version != SchemaVersion)
        {
            throw new InvalidDataException(
                $"PROJ001: shared-row projection schema version {version} is unsupported.");
        }

        var sourceLedgerSha256 = GetRequiredString(
            root,
            "source_ledger_sha256");
        EvidenceIdentity.ValidateDigest(
            new Sha256Digest("sha256", sourceLedgerSha256),
            "shared-row projection source_ledger_sha256");

        var rowsElement = root.GetProperty("rows");
        if (rowsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection 'rows' must be an array.");
        }

        var rows = new List<SharedRowProjectionRow>();
        foreach (var element in rowsElement.EnumerateArray())
        {
            RequireObject(element, "shared-row projection row");
            RequireProperties(
                element,
                [
                    "evidence_anchors",
                    "maintainer_action",
                    "requirement",
                    "requirement_id",
                    "requirement_scope",
                    "reviewer_follow_up",
                    "status",
                ],
                "shared-row projection row");
            rows.Add(new SharedRowProjectionRow(
                GetRequiredString(element, "requirement_id"),
                GetRequiredString(element, "requirement"),
                GetRequiredString(element, "requirement_scope"),
                GetRequiredString(element, "status"),
                GetRequiredString(element, "evidence_anchors"),
                GetRequiredString(element, "maintainer_action"),
                GetRequiredString(element, "reviewer_follow_up")));
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection must contain at least one row.");
        }

        var duplicate = rows
            .GroupBy(row => row.Identifier, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"PROJ001: shared-row projection repeats '{duplicate.Key}'.");
        }

        return new SharedRowProjection(
            version,
            sourceLedgerSha256,
            rows);
    }

    private static void RequireObject(JsonElement element, string context)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"PROJ001: {context} must be a JSON object.");
        }
    }

    private static void RequireProperties(
        JsonElement element,
        IReadOnlyCollection<string> expected,
        string context)
    {
        var actual = element.EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var orderedExpected = expected.Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(orderedExpected, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"PROJ001: {context} properties must be exactly: " +
                string.Join(", ", orderedExpected) + ".");
        }
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName)
    {
        var property = element.GetProperty(propertyName);
        if (property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(
                $"PROJ001: '{propertyName}' must be a JSON string.");
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"PROJ001: '{propertyName}' must be non-empty.");
        }

        return value;
    }

    private static int GetRequiredInt32(
        JsonElement element,
        string propertyName)
    {
        var property = element.GetProperty(propertyName);
        if (property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt32(out var value))
        {
            throw new InvalidDataException(
                $"PROJ001: '{propertyName}' must be an integer.");
        }

        return value;
    }
}

internal static class SharedRowProjectionValidator
{
    private const string RepositoryScope = "repository-wide";

    internal static IEnumerable<string> ValidateSourceReport(
        SharedRowProjection projection,
        IReadOnlyList<ScorecardRow> reportRows,
        EvidenceBundle bundle,
        IReadOnlyList<Requirement> requirements)
    {
        var errors = ValidateProjection(
            projection,
            bundle,
            requirements);
        var repositoryRows = reportRows
            .Where(row => row.Scope == RepositoryScope)
            .ToDictionary(row => row.Identifier, StringComparer.Ordinal);

        errors.AddRange(ValidateIdentifierSet(
            projection,
            repositoryRows.Keys,
            "source report"));
        foreach (var expected in projection.Rows)
        {
            if (!repositoryRows.TryGetValue(expected.Identifier, out var actual))
            {
                continue;
            }

            Compare(
                errors,
                expected.Identifier,
                "requirement",
                expected.Requirement,
                actual.Requirement,
                "source report");
            Compare(
                errors,
                expected.Identifier,
                "requirement scope",
                expected.RequirementScope,
                actual.Scope,
                "source report");
            Compare(
                errors,
                expected.Identifier,
                "status",
                expected.Status,
                actual.Status,
                "source report");
            Compare(
                errors,
                expected.Identifier,
                "evidence anchors",
                expected.EvidenceAnchors,
                actual.Evidence,
                "source report");
            Compare(
                errors,
                expected.Identifier,
                "maintainer action",
                expected.MaintainerAction,
                actual.MaintainerAction,
                "source report");
            Compare(
                errors,
                expected.Identifier,
                "reviewer follow-up",
                expected.ReviewerFollowUp,
                actual.ReviewerFollowUp,
                "source report");
        }

        return errors;
    }

    internal static IEnumerable<string> ValidateTracker(
        SharedRowProjection projection,
        ReportSnapshot tracker,
        EvidenceBundle bundle,
        IReadOnlyList<Requirement> requirements)
    {
        var errors = ValidateProjection(
            projection,
            bundle,
            requirements);
        var repositoryRows = ParseTrackerRows(tracker)
            .Where(row => row.RequirementScope == RepositoryScope)
            .ToDictionary(row => row.Identifier, StringComparer.Ordinal);

        errors.AddRange(ValidateIdentifierSet(
            projection,
            repositoryRows.Keys,
            "tracker"));
        foreach (var expected in projection.Rows)
        {
            if (!repositoryRows.TryGetValue(expected.Identifier, out var actual))
            {
                continue;
            }

            Compare(
                errors,
                expected.Identifier,
                "requirement",
                expected.Requirement,
                actual.Requirement,
                "tracker");
            Compare(
                errors,
                expected.Identifier,
                "requirement scope",
                expected.RequirementScope,
                actual.RequirementScope,
                "tracker");
            Compare(
                errors,
                expected.Identifier,
                "status",
                expected.Status,
                actual.Status,
                "tracker");
            Compare(
                errors,
                expected.Identifier,
                "evidence anchors",
                expected.EvidenceAnchors,
                actual.EvidenceAnchors,
                "tracker");
        }

        return errors;
    }

    private static List<string> ValidateProjection(
        SharedRowProjection projection,
        EvidenceBundle bundle,
        IReadOnlyList<Requirement> requirements)
    {
        var errors = new List<string>();
        var repositoryLedgers = bundle.SourceLedgers
            .Where(source => source.Ledger.LedgerKind == "repository")
            .ToArray();
        if (repositoryLedgers.Length != 1)
        {
            errors.Add(
                "PROJ002: evidence bundle must embed exactly one repository ledger.");
        }
        else if (!string.Equals(
            projection.SourceLedgerSha256,
            repositoryLedgers[0].SourceLedgerSha256,
            StringComparison.Ordinal))
        {
            errors.Add(
                "PROJ002: shared-row projection source ledger digest differs " +
                "from the live repository ledger.");
        }

        var requirementById = requirements.ToDictionary(
            requirement => requirement.Identifier,
            StringComparer.Ordinal);
        foreach (var row in projection.Rows)
        {
            if (!requirementById.TryGetValue(row.Identifier, out var requirement))
            {
                errors.Add(
                    $"PROJ003: shared-row projection contains unknown requirement " +
                    $"'{row.Identifier}'.");
                continue;
            }

            if (requirement.Scope != RepositoryScope ||
                row.RequirementScope != RepositoryScope)
            {
                errors.Add(
                    $"PROJ003: shared-row projection row '{row.Identifier}' " +
                    "must be repository-wide.");
            }
        }

        return errors;
    }

    private static IEnumerable<string> ValidateIdentifierSet(
        SharedRowProjection projection,
        IEnumerable<string> actualIdentifiers,
        string documentName)
    {
        var expected = projection.Rows
            .Select(row => row.Identifier)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var actual = actualIdentifiers
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            return [];
        }

        var missing = expected.Except(actual, StringComparer.Ordinal).ToArray();
        var additional = actual.Except(expected, StringComparer.Ordinal).ToArray();
        return
        [
            $"PROJ004: shared-row projection requirement set differs from the " +
            $"{documentName}; missing [{string.Join(", ", missing)}], " +
            $"additional [{string.Join(", ", additional)}].",
        ];
    }

    private static void Compare(
        ICollection<string> errors,
        string identifier,
        string field,
        string expected,
        string actual,
        string documentName)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            errors.Add(
                $"PROJ005: {documentName} row '{identifier}' {field} differs " +
                "from the shared-row projection.");
        }
    }

    private static IReadOnlyList<TrackerProjectionRow> ParseTrackerRows(
        ReportSnapshot tracker)
    {
        var lines = tracker.Content.Split('\n');
        var headerIndex = Array.FindIndex(
            lines,
            line => string.Equals(
                line.TrimEnd('\r'),
                TrackerValidator.PresentedTableHeader,
                StringComparison.Ordinal));
        if (headerIndex < 0)
        {
            return [];
        }

        var rows = new List<TrackerProjectionRow>();
        for (var index = headerIndex + 2; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!line.StartsWith("| ", StringComparison.Ordinal))
            {
                break;
            }

            var cells = line.Trim().Trim('|')
                .Split('|')
                .Select(cell => cell.Trim())
                .ToArray();
            if (cells.Length != 6)
            {
                continue;
            }

            rows.Add(new TrackerProjectionRow(
                cells[0],
                cells[1],
                cells[2],
                cells[3].Trim('`'),
                cells[5]));
        }

        return rows;
    }

    private sealed record TrackerProjectionRow(
        string Identifier,
        string Requirement,
        string RequirementScope,
        string Status,
        string EvidenceAnchors);
}
