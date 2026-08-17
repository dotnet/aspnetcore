// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlazorComponentReadiness;

internal static class ScorecardValidator
{
    internal const int ExpectedCoreRequirementCount = 110;
    internal const int ExpectedScopeSchemaVersion = 1;

    private static readonly Regex RequirementLinePattern = new(
        @"^- \*\*([A-Z][A-Z0-9]*-\d{2})\*\*.*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex CoreRequirementPattern = new(
        @"^- \*\*([A-Z][A-Z0-9]*-\d{2})\*\* \(`(repository-wide|component-specific)`\) (?!\(`(?:repository-wide|component-specific)`\) )(\S(?:.*\S)?)$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex OverlayRequirementPattern = new(
        @"^- \*\*([A-Z][A-Z0-9]*-\d{2})\*\* (\S(?:.*\S)?)$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex RubricVersionPattern = new(
        @"^\*\*Rubric version:\*\*\s+(\S+)\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex ScopeSchemaVersionPattern = new(
        @"^\*\*Scope schema version:\*\*\s+(\d+)\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex RequirementIdentifierPattern = new(
        @"^[A-Z][A-Z0-9]*-\d{2}$",
        RegexOptions.CultureInvariant);
    private static readonly Regex EvidenceIdentifierPattern = new(
        @"^E-\d{3}$",
        RegexOptions.CultureInvariant);
    private static readonly Regex EvidenceReferencePattern = new(
        @"\[(E-\d{3})\]",
        RegexOptions.CultureInvariant);
    private static readonly Regex TableSeparatorPattern = new(
        @"^:?-{3,}:?$",
        RegexOptions.CultureInvariant);
    private static readonly HashSet<string> Placeholders = new(
        [
            string.Empty,
            "-",
            "n/a",
            "na",
            "none",
            "tbd",
            "todo",
            "[evidence]",
            "[maintainer action]",
            "[reviewer follow-up]",
        ],
        StringComparer.OrdinalIgnoreCase);

    internal static readonly IReadOnlyList<string> StatusOrder =
        [
            "verified",
            "defect",
            "maintainer evidence required",
            "not tested",
            "not applicable",
        ];

    internal static readonly HashSet<string> StatusValues = new(
        StatusOrder,
        StringComparer.Ordinal);

    internal static RubricSnapshot LoadCoreRubric(
        string checklistPath,
        ReadOnlyMemory<byte>? snapshot = null)
    {
        if (CanonicalRequirementSchema.RequirementScopes.Count != ExpectedCoreRequirementCount)
        {
            throw new InvalidDataException(
                $"Canonical runtime schema must contain {ExpectedCoreRequirementCount} " +
                $"requirements; found {CanonicalRequirementSchema.RequirementScopes.Count}");
        }

        var fullPath = Path.GetFullPath(checklistPath);
        var bytes = snapshot?.ToArray() ?? File.ReadAllBytes(fullPath);
        var content = Encoding.UTF8.GetString(bytes);
        var declarationCount = RequirementLinePattern.Matches(content).Count;
        var matches = CoreRequirementPattern.Matches(content);
        if (matches.Count != declarationCount)
        {
            throw new InvalidDataException(
                $"{fullPath} contains {declarationCount} requirement declarations but only " +
                $"{matches.Count} use the exact scoped core syntax");
        }

        var requirements = matches
            .Select(match => new Requirement(
                match.Groups[1].Value,
                match.Groups[3].Value,
                match.Groups[2].Value,
                IsCore: true))
            .ToArray();
        if (requirements.Length != ExpectedCoreRequirementCount)
        {
            throw new InvalidDataException(
                $"Expected {ExpectedCoreRequirementCount} canonical core requirements in " +
                $"{fullPath}; found {requirements.Length}");
        }

        var duplicates = FindDuplicates(requirements.Select(requirement => requirement.Identifier));
        if (duplicates.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate canonical requirement IDs: {string.Join(", ", duplicates)}");
        }

        var parsed = requirements.ToDictionary(
            requirement => requirement.Identifier,
            StringComparer.Ordinal);
        var missing = CanonicalRequirementSchema.RequirementScopes.Keys
            .Where(identifier => !parsed.ContainsKey(identifier))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var unknown = parsed.Keys
            .Where(identifier =>
                !CanonicalRequirementSchema.RequirementScopes.ContainsKey(identifier))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (missing.Length > 0 || unknown.Length > 0)
        {
            throw new InvalidDataException(
                "Core requirement IDs differ from the canonical schema: " +
                $"missing [{string.Join(", ", missing)}], " +
                $"unknown [{string.Join(", ", unknown)}]");
        }

        var scopeDrift = requirements
            .Where(requirement => !string.Equals(
                requirement.Scope,
                CanonicalRequirementSchema.RequirementScopes[requirement.Identifier],
                StringComparison.Ordinal))
            .Select(requirement =>
                $"{requirement.Identifier} actual '{requirement.Scope}' expected " +
                $"'{CanonicalRequirementSchema.RequirementScopes[requirement.Identifier]}'")
            .ToArray();
        if (scopeDrift.Length > 0)
        {
            throw new InvalidDataException(
                "Core requirement scopes differ from the canonical schema: " +
                string.Join("; ", scopeDrift));
        }

        var version = ParseRequiredValue(
            RubricVersionPattern,
            content,
            "Rubric version",
            fullPath);
        var scopeSchemaVersionText = ParseRequiredValue(
            ScopeSchemaVersionPattern,
            content,
            "Scope schema version",
            fullPath);
        if (!int.TryParse(
            scopeSchemaVersionText,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var scopeSchemaVersion) ||
            scopeSchemaVersion != ExpectedScopeSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported scope schema version '{scopeSchemaVersionText}' in {fullPath}; " +
                $"expected {ExpectedScopeSchemaVersion}");
        }

        return new RubricSnapshot(
            fullPath,
            version,
            scopeSchemaVersion,
            ComputeSha256(bytes),
            requirements,
            bytes);
    }

    internal static IReadOnlyList<Requirement> LoadRequirements(string checklistPath)
    {
        return LoadCoreRubric(checklistPath).Requirements;
    }

    internal static IReadOnlyList<Requirement> LoadOverlayRequirements(
        string overlayPath,
        string expectedPrefix,
        ReadOnlyMemory<byte>? snapshot = null)
    {
        var fullPath = Path.GetFullPath(overlayPath);
        var content = snapshot is null
            ? File.ReadAllText(fullPath, Encoding.UTF8)
            : Encoding.UTF8.GetString(snapshot.Value.Span);
        var declarationCount = RequirementLinePattern.Matches(content).Count;
        var matches = OverlayRequirementPattern.Matches(content);
        if (matches.Count != declarationCount)
        {
            throw new InvalidDataException(
                $"{fullPath} contains {declarationCount} requirement declarations but only " +
                $"{matches.Count} use the exact overlay syntax");
        }

        var requirements = matches
            .Select(match => new Requirement(
                match.Groups[1].Value,
                match.Groups[2].Value,
                Scope: null,
                IsCore: false))
            .ToArray();
        if (requirements.Length == 0)
        {
            throw new InvalidDataException($"No overlay requirements found in {fullPath}");
        }

        var invalidPrefixes = requirements
            .Where(requirement => !requirement.Identifier.StartsWith(
                expectedPrefix + "-",
                StringComparison.Ordinal))
            .Select(requirement => requirement.Identifier)
            .ToArray();
        if (invalidPrefixes.Length > 0)
        {
            throw new InvalidDataException(
                $"{fullPath} contains requirement IDs outside the {expectedPrefix} overlay: " +
                string.Join(", ", invalidPrefixes));
        }

        var duplicates = FindDuplicates(requirements.Select(requirement => requirement.Identifier));
        if (duplicates.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate overlay requirement IDs: {string.Join(", ", duplicates)}");
        }

        return requirements;
    }

    internal static IReadOnlyList<Requirement> LoadRequirementSet(
        SkillLayout layout,
        IEnumerable<string> overlays,
        RubricSnapshot? rubric = null,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? overlaySnapshots = null)
    {
        rubric ??= LoadCoreRubric(layout.ChecklistPath);
        var requirements = rubric.Requirements.ToList();
        foreach (var overlay in overlays)
        {
            if (!layout.OverlayPaths.TryGetValue(overlay, out var overlayPath))
            {
                throw new InvalidDataException($"Unknown overlay '{overlay}'");
            }

            ReadOnlyMemory<byte>? overlaySnapshot = null;
            if (overlaySnapshots is not null &&
                overlaySnapshots.TryGetValue(overlay, out var snapshot))
            {
                overlaySnapshot = snapshot;
            }

            requirements.AddRange(LoadOverlayRequirements(
                overlayPath,
                layout.OverlayPrefixes[overlay],
                overlaySnapshot));
        }

        var duplicates = FindDuplicates(requirements.Select(requirement => requirement.Identifier));
        if (duplicates.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate IDs across core and overlays: {string.Join(", ", duplicates)}");
        }

        return requirements;
    }

    internal static IReadOnlyList<Requirement> SelectRequirements(
        IReadOnlyList<Requirement> requirements,
        string identifiers)
    {
        var requested = identifiers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requested.Length == 0)
        {
            throw new InvalidDataException(
                "--ids must contain at least one requirement ID");
        }

        var duplicates = FindDuplicates(requested);
        if (duplicates.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate targeted IDs: {string.Join(", ", duplicates)}");
        }

        var canonical = requirements.ToDictionary(
            requirement => requirement.Identifier,
            StringComparer.Ordinal);
        var unknown = requested
            .Where(identifier => !canonical.ContainsKey(identifier))
            .ToArray();
        if (unknown.Length > 0)
        {
            throw new InvalidDataException(
                $"Unknown targeted IDs: {string.Join(", ", unknown)}");
        }

        var requestedSet = requested.ToHashSet(StringComparer.Ordinal);
        return requirements
            .Where(requirement => requestedSet.Contains(requirement.Identifier))
            .ToArray();
    }

    internal static IReadOnlyList<string> SplitMarkdownRow(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var escaped = false;
        foreach (var character in line.Trim())
        {
            if (escaped)
            {
                current.Append(character);
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else if (character == '|')
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        cells.Add(current.ToString().Trim());
        if (cells.Count > 0 && cells[0].Length == 0)
        {
            cells.RemoveAt(0);
        }

        if (cells.Count > 0 && cells[^1].Length == 0)
        {
            cells.RemoveAt(cells.Count - 1);
        }

        return cells;
    }

    internal static IReadOnlyList<ScorecardRow> ParseScorecard(string reportPath)
    {
        return ParseScorecard(ReadReportSnapshot(reportPath));
    }

    internal static IReadOnlyList<ScorecardRow> ParseScorecard(ReportSnapshot report)
    {
        var rows = new List<ScorecardRow>();
        var expectedHeader = new[]
        {
            "Requirement ID",
            "Requirement",
            "Requirement scope",
            "Status",
            "Evidence",
            "Maintainer action",
            "Reviewer follow-up",
        };
        var inScorecard = false;
        var lineNumber = 0;
        using var reader = new StringReader(report.Content);
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (!line.TrimStart().StartsWith('|'))
            {
                inScorecard = false;
                continue;
            }

            var cells = SplitMarkdownRow(line);
            if (cells.SequenceEqual(expectedHeader, StringComparer.Ordinal))
            {
                inScorecard = true;
                continue;
            }

            if (!inScorecard)
            {
                continue;
            }

            if (cells.Count > 0 &&
                cells.All(cell => TableSeparatorPattern.IsMatch(cell)))
            {
                continue;
            }

            if (cells.Count == 0 ||
                !RequirementIdentifierPattern.IsMatch(TrimCode(cells[0])))
            {
                continue;
            }

            if (cells.Count != 7)
            {
                throw new InvalidDataException(
                    $"{report.Path}:{lineNumber}: requirement row must have 7 columns; " +
                    $"found {cells.Count}");
            }

            rows.Add(new ScorecardRow(
                TrimCode(cells[0]),
                cells[1],
                TrimCode(cells[2]),
                TrimCode(cells[3]),
                cells[4],
                cells[5],
                cells[6],
                lineNumber));
        }

        return rows;
    }

    internal static EvidenceLedger ParseEvidenceLedger(string reportPath)
    {
        return ParseEvidenceLedger(ReadReportSnapshot(reportPath));
    }

    internal static EvidenceLedger ParseEvidenceLedger(ReportSnapshot report)
    {
        var identifiers = new Dictionary<string, int>(StringComparer.Ordinal);
        var errors = new List<string>();
        var expectedHeader = new[]
        {
            "Evidence ID",
            "Claim",
            "Repository/SHA or package",
            "Evidence type",
            "Reproduction/source",
            "Rechecked now?",
        };
        var inLedger = false;
        var lineNumber = 0;
        using var reader = new StringReader(report.Content);
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (!line.TrimStart().StartsWith('|'))
            {
                inLedger = false;
                continue;
            }

            var cells = SplitMarkdownRow(line);
            if (cells.SequenceEqual(expectedHeader, StringComparer.Ordinal))
            {
                inLedger = true;
                continue;
            }

            if (!inLedger)
            {
                continue;
            }

            if (cells.Count > 0 &&
                cells.All(cell => TableSeparatorPattern.IsMatch(cell)))
            {
                continue;
            }

            if (cells.Count != 6)
            {
                continue;
            }

            var identifier = TrimCode(cells[0]);
            if (!EvidenceIdentifierPattern.IsMatch(identifier))
            {
                continue;
            }

            if (!identifiers.TryAdd(identifier, lineNumber))
            {
                errors.Add($"Line {lineNumber}: duplicate evidence ledger ID {identifier}");
            }

            for (var index = 1; index < expectedHeader.Length; index++)
            {
                if (IsPlaceholder(cells[index]))
                {
                    errors.Add(
                        $"Line {lineNumber} ({identifier}): " +
                        $"{expectedHeader[index]} must be substantive");
                }
            }
        }

        return new EvidenceLedger(identifiers, errors);
    }

    internal static IReadOnlyList<string> ValidateRows(
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<ScorecardRow> rows,
        IReadOnlyDictionary<string, int>? evidenceLedger = null)
    {
        var errors = new List<string>();
        var canonical = requirements.ToDictionary(
            requirement => requirement.Identifier,
            StringComparer.Ordinal);
        var counts = rows
            .GroupBy(row => row.Identifier, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var missing = requirements
            .Where(requirement => !counts.ContainsKey(requirement.Identifier))
            .Select(requirement => requirement.Identifier)
            .ToArray();
        var duplicates = counts
            .Where(entry => entry.Value > 1)
            .Select(entry => entry.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var extras = counts.Keys
            .Where(identifier => !canonical.ContainsKey(identifier))
            .Order(StringComparer.Ordinal)
            .ToArray();

        AddListError(errors, "Missing requirement rows", missing);
        AddListError(errors, "Duplicate requirement rows", duplicates);
        AddListError(errors, "Unknown requirement rows", extras);

        var expectedOrder = requirements.Select(requirement => requirement.Identifier);
        var actualOrder = rows
            .Where(row =>
                canonical.ContainsKey(row.Identifier) &&
                counts[row.Identifier] == 1)
            .Select(row => row.Identifier);
        if (missing.Length == 0 &&
            duplicates.Length == 0 &&
            extras.Length == 0 &&
            !actualOrder.SequenceEqual(expectedOrder, StringComparer.Ordinal))
        {
            errors.Add("Requirement rows are not in canonical checklist order");
        }

        foreach (var row in rows)
        {
            if (!canonical.TryGetValue(row.Identifier, out var requirement))
            {
                continue;
            }

            var location = $"Line {row.LineNumber} ({row.Identifier})";
            if (!StatusValues.Contains(row.Status))
            {
                errors.Add(
                    $"{location}: invalid status '{row.Status}'; expected one of " +
                    string.Join(", ", StatusValues.Order(StringComparer.Ordinal)));
            }

            if (row.Scope is not ("repository-wide" or "component-specific"))
            {
                errors.Add(
                    $"{location}: invalid scope '{row.Scope}'; expected " +
                    "'repository-wide' or 'component-specific'");
            }
            else if (requirement.Scope is not null &&
                !string.Equals(row.Scope, requirement.Scope, StringComparison.Ordinal))
            {
                errors.Add(
                    $"{location}: scope '{row.Scope}' differs from the canonical rubric; " +
                    $"expected '{requirement.Scope}'");
            }

            if (IsPlaceholder(row.Requirement))
            {
                errors.Add($"{location}: requirement text is empty or a placeholder");
            }
            else if (!string.Equals(
                row.Requirement,
                requirement.Text,
                StringComparison.Ordinal))
            {
                errors.Add(
                    $"{location}: requirement text differs from the canonical checklist");
            }

            if (IsPlaceholder(row.Evidence))
            {
                errors.Add(
                    $"{location}: evidence must explain the proof, gap, test omission, " +
                    "or not-applicable rationale");
            }

            foreach (Match match in EvidenceReferencePattern.Matches(row.Evidence))
            {
                var evidenceIdentifier = match.Groups[1].Value;
                if (evidenceLedger is null ||
                    !evidenceLedger.ContainsKey(evidenceIdentifier))
                {
                    errors.Add(
                        $"{location}: unresolved evidence reference [{evidenceIdentifier}]");
                }
            }

            if (row.Status == "maintainer evidence required" &&
                IsPlaceholder(row.MaintainerAction))
            {
                errors.Add(
                    $"{location}: maintainer evidence required needs a concrete " +
                    "maintainer action");
            }

            if (row.Status == "not tested" &&
                IsPlaceholder(row.ReviewerFollowUp))
            {
                errors.Add(
                    $"{location}: not tested needs a bounded reviewer follow-up");
            }
        }

        return errors;
    }

    internal static string RenderTemplate(IReadOnlyList<Requirement> requirements)
    {
        var lines = new List<string>
        {
            "| Requirement ID | Requirement | Requirement scope | Status | Evidence | Maintainer action | Reviewer follow-up |",
            "|---|---|---|---|---|---|---|",
        };
        lines.AddRange(requirements.Select(requirement =>
            $"| {requirement.Identifier} | {requirement.Text.Replace("|", "\\|", StringComparison.Ordinal)} | " +
            $"{requirement.Scope ?? "[scope]"} | [status] | [evidence] | " +
            "[maintainer action] | [reviewer follow-up] |"));

        return string.Join('\n', lines) + '\n';
    }

    internal static ReportSnapshot ReadReportSnapshot(string reportPath)
    {
        var fullPath = Path.GetFullPath(reportPath);
        var bytes = FileSystemUtilities.ReadAllBytesBounded(fullPath);
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        var content = reader.ReadToEnd();

        return new ReportSnapshot(fullPath, content, bytes);
    }

    internal static SortedDictionary<string, object?> BuildValidationReceipt(
        RubricSnapshot rubric,
        ReportSnapshot report,
        string mode,
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<ScorecardRow> rows,
        IReadOnlyList<string> overlays,
        DateTimeOffset? validatedAt = null)
    {
        var timestamp = (validatedAt ?? DateTimeOffset.UtcNow).ToUniversalTime();
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["canonical_row_count"] = requirements.Count,
            ["checklist_sha256"] = rubric.Sha256,
            ["limitation"] =
                "Structural validation does not establish factual evidence or " +
                "classification quality.",
            ["mode"] = mode,
            ["report_filename"] = Path.GetFileName(report.Path),
            ["report_sha256"] = ComputeSha256(report.Bytes.Span),
            ["rubric_identity"] =
                $"blazor-component-readiness/{rubric.Version}+sha256:{rubric.Sha256}",
            ["rubric_version"] = rubric.Version,
            ["schema_version"] = 2,
            ["selected_ids"] = mode == "targeted"
                ? requirements.Select(requirement => requirement.Identifier).ToArray()
                : [],
            ["selected_overlays"] = overlays.ToArray(),
            ["scope_schema_version"] = rubric.ScopeSchemaVersion,
            ["structural_validation"] = "passed",
            ["valid_row_count"] = rows.Count,
            ["validated_at_utc"] = timestamp.ToString(
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture),
        };
    }

    internal static SortedDictionary<string, object?> BuildValidationReceiptV3(
        RubricSnapshot rubric,
        ReportSnapshot report,
        ReportSnapshot evidenceBundleSnapshot,
        EvidenceBundle evidenceBundle,
        ValidationInputManifest validationInputs,
        string mode,
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<ScorecardRow> rows,
        IReadOnlyList<string> overlays,
        DateTimeOffset? validatedAt = null)
    {
        var receipt = BuildValidationReceipt(
            rubric,
            report,
            mode,
            requirements,
            rows,
            overlays,
            validatedAt);
        receipt["assessment_identity_sha256"] =
            CanonicalEvidenceJson.ComputeAssessmentSha256(evidenceBundle.Assessment);
        receipt["evidence_bundle_filename"] =
            Path.GetFileName(evidenceBundleSnapshot.Path);
        receipt["evidence_bundle_sha256"] =
            CanonicalEvidenceJson.ComputeSha256(evidenceBundleSnapshot.Bytes.Span);
        receipt["evidence_record_count"] = evidenceBundle.Selection.Count;
        receipt["evidence_schema_version"] =
            CanonicalEvidenceJson.EvidenceSchemaVersion;
        receipt["schema_version"] = 3;
        receipt["selected_evidence_ids"] = evidenceBundle.Selection
            .Select(selection => selection.EvidenceId)
            .ToArray();
        receipt["source_ledger_sha256"] = evidenceBundle.SourceLedgers
            .Select(source => source.SourceLedgerSha256)
            .Order(StringComparer.Ordinal)
            .ToArray();
        receipt["validation_inputs"] = new SortedDictionary<string, object?>(
            StringComparer.Ordinal)
        {
            ["files"] = validationInputs.Files
                .Select(file => new SortedDictionary<string, object?>(
                    StringComparer.Ordinal)
                {
                    ["path"] = file.Path,
                    ["sha256"] = new SortedDictionary<string, object?>(
                        StringComparer.Ordinal)
                    {
                        ["algorithm"] = file.Sha256.Algorithm,
                        ["value"] = file.Sha256.Value,
                    },
                })
                .ToArray(),
            ["schema_version"] = validationInputs.SchemaVersion,
        };
        receipt["validation_inputs_sha256"] =
            CanonicalEvidenceJson.ComputeValidationInputsSha256(validationInputs);
        receipt["validator_sha256"] =
            ValidationProvenance.ComputeValidatorSha256();
        return receipt;
    }

    internal static void WriteValidationReceipt(
        string receiptPath,
        string reportPath,
        SortedDictionary<string, object?> receipt,
        Action<FileStream, ReadOnlyMemory<byte>>? writeContent = null,
        Action? beforePublish = null)
    {
        if (FileSystemUtilities.PathsReferToSameEntry(receiptPath, reportPath))
        {
            throw new InvalidDataException("--receipt must not overwrite the report.");
        }

        var content = JsonSerializer.Serialize(
            receipt,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });
        if (Encoding.UTF8.GetByteCount(content) + 1 >
            FileSystemUtilities.MaximumSerializedArtifactBytes)
        {
            throw new InvalidDataException(
                "Validation receipt exceeds the 67108864-byte serialized-artifact limit.");
        }

        FileSystemUtilities.WriteAllTextNew(
            receiptPath,
            content + '\n',
            writeContent,
            beforePublish);
    }

    private static string[] FindDuplicates(IEnumerable<string> values)
    {
        return values
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    internal static string TrimCode(string value)
    {
        return value.Trim().Trim('`').Trim();
    }

    private static bool IsPlaceholder(string value)
    {
        return Placeholders.Contains(TrimCode(value));
    }

    private static void AddListError(
        ICollection<string> errors,
        string message,
        IReadOnlyList<string> values)
    {
        if (values.Count > 0)
        {
            errors.Add($"{message}: {string.Join(", ", values)}");
        }
    }

    private static string ComputeSha256(ReadOnlySpan<byte> content)
    {
        return Convert.ToHexStringLower(SHA256.HashData(content));
    }

    private static string ParseRequiredValue(
        Regex pattern,
        string content,
        string name,
        string path)
    {
        var match = pattern.Match(content);
        if (!match.Success)
        {
            throw new InvalidDataException($"{name} not found in {path}");
        }

        return match.Groups[1].Value;
    }
}
