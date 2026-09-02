// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlazorComponentReadiness;

internal sealed record SharedRowProjection(
    int SchemaVersion,
    string SourceLedgerSha256,
    bool UsesLegacyEvidenceCells,
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
    private const string SchemaSuffix = "shared-row-projection/v1";

    internal static SharedRowProjection Parse(ReportSnapshot snapshot)
    {
        using var document = ParseDocument(snapshot.Bytes);
        var root = document.RootElement;
        RequireObject(root, "shared-row projection");
        var version = ReadSchemaVersion(root);
        var usesLegacyEvidenceCells = !root.TryGetProperty("schema", out _);
        var sourceLedgerSha256 = ReadSourceLedgerSha256(root);
        EvidenceIdentity.ValidateDigest(
            new Sha256Digest("sha256", sourceLedgerSha256),
            "shared-row projection source_ledger_sha256");

        if (!root.TryGetProperty("rows", out var rowsElement) ||
            rowsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection 'rows' must be an array.");
        }

        var rows = new List<SharedRowProjectionRow>();
        foreach (var element in rowsElement.EnumerateArray())
        {
            RequireObject(element, "shared-row projection row");
            var reviewerFollowUp = ReadAliasedString(
                element,
                "reviewer_follow_up",
                "notes");
            var evidenceAnchors = GetRequiredString(
                element,
                "evidence_anchors");
            ValidateEvidenceArray(element, evidenceAnchors);
            rows.Add(new SharedRowProjectionRow(
                GetRequiredString(element, "requirement_id"),
                GetRequiredString(element, "requirement"),
                GetRequiredString(element, "requirement_scope"),
                GetRequiredString(element, "status"),
                evidenceAnchors,
                GetRequiredString(element, "maintainer_action"),
                reviewerFollowUp));
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
            usesLegacyEvidenceCells,
            rows);
    }

    private static JsonDocument ParseDocument(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            return JsonDocument.Parse(bytes);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection contains invalid JSON.",
                exception);
        }
    }

    private static int ReadSchemaVersion(JsonElement root)
    {
        var foundVersion = false;
        if (root.TryGetProperty("schema_version", out var versionElement))
        {
            if (versionElement.ValueKind != JsonValueKind.Number ||
                !versionElement.TryGetInt32(out var version) ||
                version != SchemaVersion)
            {
                throw new InvalidDataException(
                    $"PROJ001: shared-row projection schema version must be {SchemaVersion}.");
            }

            foundVersion = true;
        }

        var schema = root.TryGetProperty("schema", out _)
            ? GetRequiredString(root, "schema")
            : null;
        if (schema is not null &&
            !schema.EndsWith(SchemaSuffix, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"PROJ001: shared-row projection schema must end with '{SchemaSuffix}'.");
        }

        if (!foundVersion && schema is null)
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection requires 'schema_version' or 'schema'.");
        }

        return SchemaVersion;
    }

    private static string ReadSourceLedgerSha256(JsonElement root)
    {
        var direct = TryGetString(root, "source_ledger_sha256");
        string? bound = null;
        if (root.TryGetProperty("bound_artifacts", out var boundArtifacts))
        {
            RequireObject(boundArtifacts, "shared-row projection bound_artifacts");
            bound = TryGetString(
                boundArtifacts,
                "repository_ledger_sha256");
        }

        if (direct is not null &&
            bound is not null &&
            !string.Equals(direct, bound, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection source ledger digests disagree.");
        }

        return direct ??
            bound ??
            throw new InvalidDataException(
                "PROJ001: shared-row projection requires a source repository " +
                "ledger digest.");
    }

    private static string ReadAliasedString(
        JsonElement element,
        string primaryName,
        string alternateName)
    {
        var primary = TryGetString(element, primaryName);
        var alternate = TryGetString(element, alternateName);
        if (primary is not null &&
            alternate is not null &&
            !string.Equals(primary, alternate, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"PROJ001: '{primaryName}' and '{alternateName}' disagree.");
        }

        return primary ??
            alternate ??
            throw new InvalidDataException(
                $"PROJ001: shared-row projection row requires '{primaryName}' " +
                $"or '{alternateName}'.");
    }

    private static void ValidateEvidenceArray(
        JsonElement element,
        string evidenceAnchors)
    {
        if (!element.TryGetProperty("evidence", out var evidence))
        {
            return;
        }

        if (evidence.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection row 'evidence' must be an array.");
        }

        var identifiers = evidence.EnumerateArray()
            .Select(item =>
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidDataException(
                        "PROJ001: shared-row projection evidence IDs must be strings.");
                }

                return item.GetString()!;
            })
            .ToArray();
        var expected = string.Join(
            ' ',
            identifiers.Select(identifier => $"[{identifier}]"));
        if (!string.Equals(expected, evidenceAnchors, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "PROJ001: shared-row projection evidence array differs from " +
                "evidence_anchors.");
        }
    }

    private static void RequireObject(JsonElement element, string context)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"PROJ001: {context} must be a JSON object.");
        }

        var duplicate = element.EnumerateObject()
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"PROJ001: {context} repeats property '{duplicate.Key}'.");
        }
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName)
    {
        return TryGetString(element, propertyName) ??
            throw new InvalidDataException(
                $"PROJ001: '{propertyName}' must be a non-empty JSON string.");
    }

    private static string? TryGetString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

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
}

internal static partial class SharedRowProjectionValidator
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
        var repositoryRows = BuildRepositoryRowMap(
            reportRows.Where(row => row.Scope == RepositoryScope),
            row => row.Identifier,
            "source report",
            errors);

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
                projection.UsesLegacyEvidenceCells
                    ? actual.Evidence
                    : ExtractEvidenceAnchors(actual.Evidence),
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
        var repositoryRows = BuildRepositoryRowMap(
            ParseTrackerRows(tracker)
                .Where(row => row.RequirementScope == RepositoryScope),
            row => row.Identifier,
            "tracker",
            errors);

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
                projection.UsesLegacyEvidenceCells
                    ? actual.EvidenceAnchors
                    : ExtractEvidenceAnchors(actual.EvidenceAnchors),
                "tracker");
        }

        return errors;
    }

    private static Dictionary<string, T> BuildRepositoryRowMap<T>(
        IEnumerable<T> rows,
        Func<T, string> identifier,
        string documentName,
        ICollection<string> errors)
    {
        var groups = rows
            .GroupBy(identifier, StringComparer.Ordinal)
            .ToArray();
        foreach (var group in groups.Where(group => group.Count() > 1))
        {
            errors.Add(
                $"PROJ004: {documentName} repeats repository-wide requirement " +
                $"'{group.Key}'.");
        }

        return groups.ToDictionary(
            group => group.Key,
            group => group.First(),
            StringComparer.Ordinal);
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

            if ((requirement.Scope is not null &&
                    requirement.Scope != RepositoryScope) ||
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

    private static string ExtractEvidenceAnchors(string value)
    {
        return string.Join(
            ' ',
            EvidenceAnchorRegex().Matches(value).Select(match => match.Value));
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

    [GeneratedRegex(
        @"\[EV1-[0-9a-f]{64}\]",
        RegexOptions.CultureInvariant)]
    private static partial Regex EvidenceAnchorRegex();
}
