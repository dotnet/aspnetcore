// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;

namespace BlazorComponentReadiness;

internal static partial class StableEvidenceValidator
{
    internal const string ProjectionHeader =
        "| Display order | Evidence ID | Claim | Requirement scope | Component ID | " +
        "Source ledger kind | Source ledger SHA-256 | Provenance kind | " +
        "Reproduction/source | Captured at UTC | Content SHA-256 |";
    internal const string ProjectionSeparator =
        "|---:|---|---|---|---|---|---|---|---|---|---|";

    [GeneratedRegex(
        @"\[EV1-[0-9a-f]{64}\]",
        RegexOptions.CultureInvariant)]
    private static partial Regex StableReferencePattern();

    [GeneratedRegex(
        @"\[EV1-[^\]\r\n]*\]",
        RegexOptions.CultureInvariant)]
    private static partial Regex StableCandidatePattern();

    [GeneratedRegex(
        @"\[(E-\d{3})\]",
        RegexOptions.CultureInvariant)]
    private static partial Regex LegacyReferencePattern();

    internal static IReadOnlyList<string> ValidateScorecard(
        ReportSnapshot report,
        IReadOnlyList<ScorecardRow> rows,
        EvidenceBundle bundle)
    {
        var evidenceRows = rows.Select(row => new EvidenceUse(
            row.Identifier,
            row.Scope,
            row.Evidence,
            row.LineNumber));
        return Validate(report, evidenceRows, bundle);
    }

    private static string[] SplitLines(string content)
    {
        return content
            .Split('\n')
            .Select(static line =>
                line.Length > 0 && line[^1] == '\r' ? line[..^1] : line)
            .ToArray();
    }

    internal static IReadOnlyList<string> ValidateTracker(
        ReportSnapshot report,
        EvidenceBundle bundle)
    {
        var errors = new List<string>();
        var uses = ParseTrackerUses(report.Content, errors);
        errors.AddRange(Validate(report, uses, bundle));
        return errors;
    }

    internal static IReadOnlyList<string> ValidateLegacyDocument(ReportSnapshot report)
    {
        if (StableReferencePattern().IsMatch(report.Content))
        {
            return
            [
                "EVID011: legacy evidence mode rejects stable EV1 references.",
            ];
        }

        return [];
    }

    internal static string RenderAssessmentBlock(ExactAssessmentIdentity assessment)
    {
        return "```bcr-assessment-v1\n" +
            Encoding.UTF8.GetString(
                CanonicalEvidenceJson.SerializeAssessment(assessment)) +
            "\n```";
    }

    internal static string RenderProjection(EvidenceBundle bundle)
    {
        var sources = bundle.SourceLedgers.ToDictionary(
            source => source.SourceLedgerSha256,
            StringComparer.Ordinal);
        var lines = new List<string>
        {
            ProjectionHeader,
            ProjectionSeparator,
        };
        foreach (var selection in bundle.Selection)
        {
            var source = sources[selection.SourceLedgerSha256];
            var record = source.Ledger.Records.Single(candidate =>
                string.Equals(
                    candidate.StableId,
                    selection.EvidenceId,
                    StringComparison.Ordinal));
            var componentId = source.Ledger.LedgerKind == "repository"
                ? source.Ledger.RepositorySubject!.ComponentId ?? string.Empty
                : source.Ledger.ComponentSubject!.ComponentId;
            lines.Add(
                $"| {selection.DisplayOrder} | {record.StableId} | " +
                $"{EscapeMarkdownCell(record.Claim)} | " +
                $"{record.Applicability.Scope} | {EscapeMarkdownCell(componentId)} | " +
                $"{source.Ledger.LedgerKind} | {source.SourceLedgerSha256} | " +
                $"{record.Provenance.Kind} | `{record.Provenance.Locator}` | " +
                $"{record.Provenance.CapturedAtUtc} | " +
                $"{record.Provenance.ContentDigest.Value} |");
        }

        return string.Join('\n', lines);
    }

    private static IReadOnlyList<string> Validate(
        ReportSnapshot report,
        IEnumerable<EvidenceUse> evidenceUses,
        EvidenceBundle bundle)
    {
        var errors = new List<string>();
        if (LegacyReferencePattern().IsMatch(report.Content))
        {
            errors.Add(
                "EVID011: stable evidence mode rejects legacy E-### references.");
        }

        ValidateAssessmentProjection(report.Content, bundle, errors);
        ValidateEvidenceProjection(report.Content, bundle, errors);

        var selected = bundle.Selection.ToDictionary(
            selection => selection.EvidenceId,
            StringComparer.Ordinal);
        var sources = bundle.SourceLedgers.ToDictionary(
            source => source.SourceLedgerSha256,
            StringComparer.Ordinal);
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var use in evidenceUses)
        {
            var candidates = StableCandidatePattern().Matches(use.Evidence);
            foreach (Match candidate in candidates)
            {
                if (!StableReferencePattern().IsMatch(candidate.Value))
                {
                    errors.Add(
                        $"EVID008: line {use.LineNumber} ({use.Identifier}) contains " +
                        $"malformed stable evidence token {candidate.Value}.");
                }
            }

            var matches = candidates
                .Where(candidate => StableReferencePattern().IsMatch(candidate.Value))
                .ToArray();
            if (matches.Length == 0)
            {
                errors.Add(
                    $"EVID008: line {use.LineNumber} ({use.Identifier}) evidence " +
                    "requires at least one full selected EV1 reference.");
                continue;
            }

            foreach (var match in matches)
            {
                var identifier = match.Value[1..^1];
                if (!selected.TryGetValue(identifier, out var selection))
                {
                    errors.Add(
                        $"EVID008: line {use.LineNumber} ({use.Identifier}) references " +
                        $"unselected evidence {identifier}.");
                    continue;
                }

                referenced.Add(identifier);
                var source = sources[selection.SourceLedgerSha256];
                var record = source.Ledger.Records.Single(candidate =>
                    string.Equals(
                        candidate.StableId,
                        identifier,
                        StringComparison.Ordinal));
                if (!string.Equals(
                    record.Applicability.Scope,
                    use.Scope,
                    StringComparison.Ordinal))
                {
                    errors.Add(
                        $"EVID007: line {use.LineNumber} ({use.Identifier}) scope " +
                        $"'{use.Scope}' cannot cite {record.Applicability.Scope} " +
                        $"evidence {identifier}.");
                }
            }
        }

        foreach (var unreferenced in selected.Keys
            .Where(identifier => !referenced.Contains(identifier))
            .Order(StringComparer.Ordinal))
        {
            errors.Add(
                $"EVID008: selected evidence {unreferenced} is not referenced by " +
                "a requirement evidence cell.");
        }

        return errors;
    }

    private static void ValidateAssessmentProjection(
        string content,
        EvidenceBundle bundle,
        ICollection<string> errors)
    {
        const string Marker = "bcr-assessment-v1";
        const string Opening = "```bcr-assessment-v1";
        var lines = SplitLines(content);
        var openingIndexes = lines
            .Select((line, index) => (line, index))
            .Where(entry => string.Equals(entry.line, Opening, StringComparison.Ordinal))
            .Select(entry => entry.index)
            .ToArray();
        if (openingIndexes.Length != 1)
        {
            errors.Add(
                "EVID012: stable report requires exactly one reserved " +
                "bcr-assessment-v1 opening fence on its own exact line.");
            return;
        }

        var openingIndex = openingIndexes[0];
        var payloadIndex = openingIndex + 1;
        var fenceCandidates = lines
            .Select((line, index) => (line, index))
            .Where(entry =>
                entry.index != payloadIndex &&
                entry.line.Contains(Marker, StringComparison.Ordinal) &&
                (entry.line.Contains("```", StringComparison.Ordinal) ||
                entry.line.Contains("~~~", StringComparison.Ordinal)))
            .ToArray();
        if (fenceCandidates.Length != 1 ||
            fenceCandidates[0].index != openingIndex)
        {
            errors.Add(
                "EVID012: stable report contains an additional or malformed " +
                "bcr-assessment-v1 fence-shaped marker line.");
            return;
        }

        if (openingIndex + 2 >= lines.Length ||
            lines[openingIndex + 1].Length == 0 ||
            lines[openingIndex + 1].Contains('\r', StringComparison.Ordinal) ||
            !string.Equals(lines[openingIndex + 2], "```", StringComparison.Ordinal))
        {
            errors.Add(
                "EVID012: bcr-assessment-v1 requires one canonical JSON line " +
                "and one exact closing fence line.");
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(lines[openingIndex + 1]);
            var assessment = CanonicalEvidenceJson.ParseAssessment(bytes);
            if (assessment != bundle.Assessment)
            {
                errors.Add(
                    "EVID012: report assessment differs from evidence bundle.");
            }
        }
        catch (InvalidDataException exception)
        {
            errors.Add($"EVID012: invalid report assessment: {exception.Message}");
        }
    }

    private static void ValidateEvidenceProjection(
        string content,
        EvidenceBundle bundle,
        ICollection<string> errors)
    {
        var lines = SplitLines(content);
        var exactHeaderIndexes = lines
            .Select((line, index) => (line, index))
            .Where(entry => string.Equals(
                entry.line,
                ProjectionHeader,
                StringComparison.Ordinal))
            .Select(entry => entry.index)
            .ToArray();
        var malformedHeaders = lines.Where(line =>
        {
            if (string.Equals(line, ProjectionHeader, StringComparison.Ordinal))
            {
                return false;
            }

            if (line.Contains(ProjectionHeader, StringComparison.Ordinal))
            {
                return true;
            }

            if (!line.StartsWith('|'))
            {
                return false;
            }

            var cells = ScorecardValidator.SplitMarkdownRow(line);
            return cells.Count == 11 &&
                (cells[0].Contains("Display order", StringComparison.Ordinal) ||
                cells[1].Contains("Evidence ID", StringComparison.Ordinal));
        }).ToArray();
        if (exactHeaderIndexes.Length != 1 || malformedHeaders.Length > 0)
        {
            errors.Add(
                "EVID012: evidence projection header is missing, duplicated, " +
                "prefixed, suffixed, or altered.");
            return;
        }

        var expectedLines = RenderProjection(bundle).Split('\n');
        var start = exactHeaderIndexes[0];
        if (start + expectedLines.Length > lines.Length ||
            !lines
                .Skip(start)
                .Take(expectedLines.Length)
                .SequenceEqual(expectedLines, StringComparer.Ordinal))
        {
            errors.Add(
                "EVID012: selected-evidence projection lines differ from the " +
                "canonical companion projection.");
        }

        var expectedIndexes = Enumerable
            .Range(start, Math.Min(expectedLines.Length, lines.Length - start))
            .ToHashSet();
        for (var index = 0; index < lines.Length; index++)
        {
            if (expectedIndexes.Contains(index) ||
                !lines[index].StartsWith('|'))
            {
                continue;
            }

            var cells = ScorecardValidator.SplitMarkdownRow(lines[index]);
            if (IsProjectionShapedRow(cells))
            {
                errors.Add(
                    $"EVID012: additional projection-shaped row appears at " +
                    $"line {index + 1}.");
            }
        }
    }

    private static bool IsProjectionShapedRow(IReadOnlyList<string> cells)
    {
        return cells.Count == 11 &&
            int.TryParse(
                cells[0],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out _) &&
            cells[1].StartsWith("EV1-", StringComparison.Ordinal);
    }

    private static IReadOnlyList<EvidenceUse> ParseTrackerUses(
        string content,
        ICollection<string> errors)
    {
        var uses = new List<EvidenceUse>();
        var inTable = false;
        var lineNumber = 0;
        foreach (var line in SplitLines(content))
        {
            lineNumber++;
            if (string.Equals(
                line,
                TrackerValidator.PresentedTableHeader,
                StringComparison.Ordinal))
            {
                inTable = true;
                continue;
            }

            if (!inTable || !line.StartsWith('|'))
            {
                if (inTable && line.Length > 0)
                {
                    inTable = false;
                }

                continue;
            }

            var cells = ScorecardValidator.SplitMarkdownRow(line);
            if (cells.Count == 0 ||
                cells.All(cell => cell.All(character => character is '-' or ':')))
            {
                continue;
            }

            if (cells.Count != 6)
            {
                continue;
            }

            var identifier = ScorecardValidator.TrimCode(cells[0]);
            if (!identifier.Contains('-', StringComparison.Ordinal))
            {
                continue;
            }

            uses.Add(new EvidenceUse(
                identifier,
                ScorecardValidator.TrimCode(cells[2]),
                cells[5],
                lineNumber));
        }

        if (uses.Count == 0)
        {
            errors.Add("EVID008: tracker contains no presented requirement evidence cells.");
        }

        return uses;
    }

    private static string EscapeMarkdownCell(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
    }

    private sealed record EvidenceUse(
        string Identifier,
        string Scope,
        string Evidence,
        int LineNumber);
}
