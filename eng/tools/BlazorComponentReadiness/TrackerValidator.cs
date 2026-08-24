// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;

namespace BlazorComponentReadiness;

/// <summary>
/// Validates a published tracker body (issue, project draft, or comparable tracker item)
/// against the single canonical evidence-only presentation contract.
/// </summary>
/// <remarks>
/// The scorecard validator checks the seven-column source report. This validator checks the
/// derived body that readers use, which previously had no machine gate at all.
/// </remarks>
internal static class TrackerValidator
{
    internal const string FixAreaHeader =
        "| Area | What we believe needs attention | Requirement IDs | Evidence |";

    internal const string FeedbackCallout =
        "> **Feedback requested:** Please let us know if any item above is a false positive, " +
        "misses important context, or is not useful. Specific examples will help us correct " +
        "this report and improve future reviews.";

    internal const string FullReportSentence =
        "The complete 110-requirement assessment and evidence ledger follow unchanged.";

    internal const string PresentedTableHeader =
        "| Requirement ID | Requirement | Requirement scope | Canonical status | " +
        "Review result | Evidence |";

    internal const string CountsTableHeader =
        "| Canonical rubric status | Cautious display result | Count |";

    /// <summary>
    /// Maps each canonical status to its single approved cautious display label. The mapping is
    /// total and fixed so the display column carries no independent judgement.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> DisplayResults =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["verified"] = "Copilot-reviewed positive evidence",
            ["defect"] = "Potential issue identified",
            ["maintainer evidence required"] = "Maintainer confirmation requested",
            ["not tested"] = "Not tested by this review",
            ["not applicable"] = "Not applicable to reviewed scope",
        };

    /// <summary>
    /// Canonical status order used by the review-result counts table.
    /// </summary>
    internal static readonly IReadOnlyList<string> StatusOrder =
        ScorecardValidator.StatusOrder;

    internal static readonly IReadOnlyList<string> RequiredSections =
        [
            "## Areas we believe need to be fixed",
            "## Full report",
            "## Exact review scope",
            "## Review-result counts",
            "## Status terminology",
            "## Complete rubric requirement mapping",
            "## Evidence ledger",
            "## Structural validation and limitations",
        ];

    private static readonly Regex IntroPattern = new(
        @"^The (\d+) canonical `defect` rows in the full report consolidate into the (\d+) areas below\. These areas are not ordered by priority and require human confirmation\. Each should be confirmed against the linked evidence before it is treated as a final product or release determination\.$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex RequirementIdentifierPattern = new(
        @"^[A-Z][A-Z0-9]*-\d{2}$",
        RegexOptions.CultureInvariant);

    private static readonly Regex EvidenceReferencePattern = new(
        @"\[(E-\d{3})\]",
        RegexOptions.CultureInvariant);
    private static readonly Regex StableEvidenceReferencePattern = new(
        @"\[EV1-[0-9a-f]{64}\]",
        RegexOptions.CultureInvariant);

    private static readonly Regex EvidenceLedgerRowPattern = new(
        @"^\|\s*(E-\d{3})\s*\|",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex SummaryIdentifierPattern = new(
        @"`([A-Z][A-Z0-9]*-\d{2})`",
        RegexOptions.CultureInvariant);

    private static readonly Regex LocalPathPattern = new(
        @"(/(?:Users|home)/[A-Za-z0-9._-]+/|[A-Za-z]:\\Users\\)",
        RegexOptions.CultureInvariant);

    private static readonly Regex TableSeparatorPattern = new(
        @"^:?-{3,}:?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates <paramref name="report"/> against the canonical tracker contract.
    /// </summary>
    /// <returns>Every contract violation found, in document order where practical.</returns>
    internal static IReadOnlyList<string> Validate(
        ReportSnapshot report,
        IReadOnlyList<Requirement> requirements,
        EvidenceBundle? evidenceBundle = null,
        bool legacyEvidence = true)
    {
        var errors = new List<string>();
        var content = report.Content;

        ValidateEncoding(report, errors);
        ValidateSectionOrder(content, errors);

        var rows = ParsePresentedRows(content, errors);
        ValidateRowCoverage(requirements, rows, errors);

        var defects = rows
            .Where(row => string.Equals(row.Status, "defect", StringComparison.Ordinal))
            .Select(row => row.Identifier)
            .ToHashSet(StringComparer.Ordinal);

        ValidateFixAreaSummary(content, defects, errors);
        ValidateCountsTable(content, rows, errors);
        ValidateFixedText(content, errors);
        if (evidenceBundle is null)
        {
            ValidateEvidenceAnchors(content, errors);
            if (legacyEvidence)
            {
                errors.AddRange(
                    StableEvidenceValidator.ValidateLegacyDocument(report));
            }
        }
        else
        {
            errors.AddRange(
                StableEvidenceValidator.ValidateTracker(report, evidenceBundle));
        }

        ValidateNoLocalPaths(content, errors);

        return errors;
    }

    internal static IReadOnlyList<string> ValidateSourceReport(
        ReportSnapshot tracker,
        IReadOnlyList<ScorecardRow> sourceRows,
        bool stableEvidence = false)
    {
        var errors = new List<string>();
        var trackerRows = ParsePresentedRows(tracker.Content, errors);
        var trackerById = trackerRows
            .GroupBy(row => row.Identifier, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal);
        var sourceById = sourceRows
            .GroupBy(row => row.Identifier, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal);
        var trackerIdentifiers = trackerById.Keys.Order(StringComparer.Ordinal).ToArray();
        var sourceIdentifiers = sourceById.Keys.Order(StringComparer.Ordinal).ToArray();
        if (!trackerIdentifiers.SequenceEqual(sourceIdentifiers, StringComparer.Ordinal))
        {
            var missing = sourceIdentifiers
                .Except(trackerIdentifiers, StringComparer.Ordinal)
                .ToArray();
            var additional = trackerIdentifiers
                .Except(sourceIdentifiers, StringComparer.Ordinal)
                .ToArray();
            errors.Add(
                "SOURCE001: tracker requirement set differs from the source report; " +
                $"missing [{string.Join(", ", missing)}], " +
                $"additional [{string.Join(", ", additional)}].");
        }

        foreach (var sourceRow in sourceRows)
        {
            if (!trackerById.TryGetValue(sourceRow.Identifier, out var trackerRow))
            {
                continue;
            }

            CompareSourceField(
                errors,
                sourceRow.Identifier,
                "requirement",
                sourceRow.Requirement,
                trackerRow.Requirement);
            CompareSourceField(
                errors,
                sourceRow.Identifier,
                "requirement scope",
                sourceRow.Scope,
                trackerRow.Scope);
            CompareSourceField(
                errors,
                sourceRow.Identifier,
                "status",
                sourceRow.Status,
                trackerRow.Status);
            CompareSourceField(
                errors,
                sourceRow.Identifier,
                "evidence references",
                NormalizeEvidenceReferences(sourceRow.Evidence, stableEvidence),
                NormalizeEvidenceReferences(trackerRow.Evidence, stableEvidence));
        }

        return errors;
    }

    private static void ValidateEncoding(ReportSnapshot report, List<string> errors)
    {
        var bytes = report.Bytes.Span;
        if (bytes.Length == 0)
        {
            errors.Add("tracker body is empty");
            return;
        }

        // GitHub persists tracker bodies without a terminal newline. A stray trailing LF makes a
        // local artifact differ from the live body by one byte and defeats byte-identical proof.
        if (bytes[^1] == (byte)'\n')
        {
            errors.Add(
                "tracker body must not end with a newline; GitHub stores the persisted body " +
                "without one and the extra byte breaks byte-identical comparison");
        }

        if (report.Content.Contains('\r', StringComparison.Ordinal))
        {
            errors.Add("tracker body must use LF line endings; found CR");
        }

        if (!report.Content.StartsWith("# ", StringComparison.Ordinal))
        {
            errors.Add("tracker body must begin with a single '# ' title line");
        }

        var titleCount = report.Content
            .Split('\n')
            .Count(line => line.StartsWith("# ", StringComparison.Ordinal));
        if (titleCount != 1)
        {
            errors.Add($"tracker body must contain exactly one '# ' title; found {titleCount}");
        }
    }

    private static void ValidateSectionOrder(string content, List<string> errors)
    {
        var previousIndex = -1;
        string? previousSection = null;
        foreach (var section in RequiredSections)
        {
            var index = IndexOfLine(content, section);
            if (index < 0)
            {
                errors.Add($"missing required section '{section}'");
                continue;
            }

            if (index < previousIndex)
            {
                errors.Add(
                    $"section '{section}' must appear after '{previousSection}'");
            }

            previousIndex = index;
            previousSection = section;
        }

        var actualSections = content
            .Split('\n')
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line.TrimEnd())
            .ToArray();
        var unexpected = actualSections
            .Where(section => !RequiredSections.Contains(section, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (unexpected.Length > 0)
        {
            errors.Add(
                "unexpected top-level sections: " +
                string.Join(", ", unexpected.Select(section => $"'{section}'")));
        }
    }

    private static List<PresentedRow> ParsePresentedRows(string content, List<string> errors)
    {
        var rows = new List<PresentedRow>();
        var headerSeen = false;
        var lineNumber = 0;
        foreach (var rawLine in content.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.TrimEnd();
            if (string.Equals(line, PresentedTableHeader, StringComparison.Ordinal))
            {
                headerSeen = true;
                continue;
            }

            if (!headerSeen || !line.StartsWith("| ", StringComparison.Ordinal))
            {
                continue;
            }

            var cells = ScorecardValidator.SplitMarkdownRow(line);
            if (cells.Count > 0 && cells.All(cell => TableSeparatorPattern.IsMatch(cell)))
            {
                continue;
            }

            if (cells.Count == 0 ||
                !RequirementIdentifierPattern.IsMatch(ScorecardValidator.TrimCode(cells[0])))
            {
                continue;
            }

            if (cells.Count != 6)
            {
                errors.Add(
                    $"line {lineNumber}: presented requirement row must have 6 columns; " +
                    $"found {cells.Count}. Use the canonical header exactly: {PresentedTableHeader}");
                continue;
            }

            var identifier = ScorecardValidator.TrimCode(cells[0]);
            var rawStatus = cells[3];
            if (!rawStatus.StartsWith('`') || !rawStatus.EndsWith('`'))
            {
                errors.Add(
                    $"line {lineNumber}: {identifier} canonical status must be enclosed in " +
                    $"backticks; found '{rawStatus}'");
            }

            var status = ScorecardValidator.TrimCode(rawStatus);
            if (!ScorecardValidator.StatusValues.Contains(status))
            {
                errors.Add(
                    $"line {lineNumber}: {identifier} has unknown canonical status '{status}'");
                continue;
            }

            var display = cells[4].Trim();
            var expectedDisplay = DisplayResults[status];
            if (!string.Equals(display, expectedDisplay, StringComparison.Ordinal))
            {
                errors.Add(
                    $"line {lineNumber}: {identifier} review result must be derived from its " +
                    $"canonical status; expected '{expectedDisplay}' for '{status}' but found " +
                    $"'{display}'");
            }

            rows.Add(new PresentedRow(
                identifier,
                cells[1],
                ScorecardValidator.TrimCode(cells[2]),
                status,
                cells[5],
                lineNumber));
        }

        if (!headerSeen)
        {
            errors.Add(
                $"missing canonical presented requirement table header: {PresentedTableHeader}");
        }

        return rows;
    }

    private static void ValidateRowCoverage(
        IReadOnlyList<Requirement> requirements,
        List<PresentedRow> rows,
        List<string> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!seen.Add(row.Identifier))
            {
                errors.Add($"line {row.LineNumber}: duplicate requirement row {row.Identifier}");
            }
        }

        var expected = requirements.Select(requirement => requirement.Identifier).ToArray();
        var missing = expected.Where(identifier => !seen.Contains(identifier)).ToArray();
        if (missing.Length > 0)
        {
            errors.Add($"missing requirement rows: {string.Join(", ", missing)}");
        }

        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var unknown = seen.Where(identifier => !expectedSet.Contains(identifier)).ToArray();
        if (unknown.Length > 0)
        {
            errors.Add($"unknown requirement rows: {string.Join(", ", unknown.Order(StringComparer.Ordinal))}");
        }

        if (missing.Length == 0 && unknown.Length == 0)
        {
            var actualOrder = rows.Select(row => row.Identifier).ToArray();
            for (var index = 0; index < expected.Length && index < actualOrder.Length; index++)
            {
                if (!string.Equals(actualOrder[index], expected[index], StringComparison.Ordinal))
                {
                    errors.Add(
                        $"requirement rows must follow checklist order; position {index + 1} " +
                        $"expected {expected[index]} but found {actualOrder[index]}");
                    break;
                }
            }
        }

        var canonical = requirements.ToDictionary(
            requirement => requirement.Identifier,
            StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!canonical.TryGetValue(row.Identifier, out var requirement))
            {
                continue;
            }

            if (!string.Equals(
                row.Requirement,
                requirement.Text,
                StringComparison.Ordinal))
            {
                errors.Add(
                    $"line {row.LineNumber}: {row.Identifier} requirement text differs from " +
                    "the canonical checklist");
            }

            if (row.Scope is not ("repository-wide" or "component-specific"))
            {
                errors.Add(
                    $"line {row.LineNumber}: {row.Identifier} has invalid scope '{row.Scope}'; " +
                    "expected 'repository-wide' or 'component-specific'");
            }
            else if (requirement.Scope is not null &&
                !string.Equals(row.Scope, requirement.Scope, StringComparison.Ordinal))
            {
                errors.Add(
                    $"line {row.LineNumber}: {row.Identifier} scope '{row.Scope}' differs from " +
                    $"the canonical rubric; expected '{requirement.Scope}'");
            }
        }
    }

    private static void ValidateFixAreaSummary(
        string content,
        IReadOnlySet<string> defects,
        List<string> errors)
    {
        var sectionStart = IndexOfLine(content, RequiredSections[0]);
        var sectionEnd = IndexOfLine(content, RequiredSections[1]);
        if (sectionStart < 0 || sectionEnd < 0 || sectionEnd < sectionStart)
        {
            return;
        }

        var section = content[sectionStart..sectionEnd];
        if (!section.Contains(FixAreaHeader, StringComparison.Ordinal))
        {
            errors.Add($"missing fix-area table header: {FixAreaHeader}");
            return;
        }

        var introMatch = IntroPattern.Match(section);
        if (!introMatch.Success)
        {
            errors.Add(
                "fix-area section must use the canonical intro sentence with digit counts: " +
                "\"The {defects} canonical `defect` rows in the full report consolidate into " +
                "the {areas} areas below. These areas are not ordered by priority and require " +
                "human confirmation. Each should be confirmed against the linked evidence " +
                "before it is treated as a final product or release determination.\"");
        }

        var tableStart = section.IndexOf(FixAreaHeader, StringComparison.Ordinal);
        var tableBody = section[tableStart..];
        var areaRows = tableBody
            .Split('\n')
            .Skip(1)
            .TakeWhile(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => line.StartsWith("| ", StringComparison.Ordinal))
            .Where(line =>
            {
                var cells = ScorecardValidator.SplitMarkdownRow(line);
                return cells.Count > 0 && !cells.All(cell => TableSeparatorPattern.IsMatch(cell));
            })
            .ToArray();

        var declaredIdentifiers = areaRows
            .SelectMany(line =>
            {
                var cells = ScorecardValidator.SplitMarkdownRow(line);
                return cells.Count >= 3
                    ? SummaryIdentifierPattern.Matches(cells[2]).Select(match => match.Groups[1].Value)
                    : [];
            })
            .ToArray();

        var duplicates = declaredIdentifiers
            .GroupBy(identifier => identifier, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
        {
            errors.Add(
                $"fix-area summary repeats requirement IDs: {string.Join(", ", duplicates)}");
        }

        var declaredSet = declaredIdentifiers.ToHashSet(StringComparer.Ordinal);
        var notDefects = declaredSet.Except(defects, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (notDefects.Length > 0)
        {
            errors.Add(
                "fix-area summary lists requirement IDs that are not canonical defects: " +
                string.Join(", ", notDefects));
        }

        var absent = defects.Except(declaredSet, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (absent.Length > 0)
        {
            errors.Add(
                $"fix-area summary omits canonical defect IDs: {string.Join(", ", absent)}");
        }

        if (introMatch.Success)
        {
            var declaredDefectCount = int.Parse(
                introMatch.Groups[1].Value,
                CultureInfo.InvariantCulture);
            if (declaredDefectCount != defects.Count)
            {
                errors.Add(
                    $"fix-area intro declares {declaredDefectCount} defect rows but the " +
                    $"presented table contains {defects.Count}");
            }

            var declaredAreaCount = int.Parse(
                introMatch.Groups[2].Value,
                CultureInfo.InvariantCulture);
            if (declaredAreaCount != areaRows.Length)
            {
                errors.Add(
                    $"fix-area intro declares {declaredAreaCount} areas but the table contains " +
                    $"{areaRows.Length}");
            }
        }
    }

    private static void ValidateCountsTable(
        string content,
        List<PresentedRow> rows,
        List<string> errors)
    {
        if (!content.Contains(CountsTableHeader, StringComparison.Ordinal))
        {
            errors.Add($"missing review-result counts header: {CountsTableHeader}");
            return;
        }

        var actual = StatusOrder.ToDictionary(
            status => status,
            status => rows.Count(row => string.Equals(row.Status, status, StringComparison.Ordinal)),
            StringComparer.Ordinal);

        var tableStart = content.IndexOf(CountsTableHeader, StringComparison.Ordinal);
        var declaredOrder = new List<string>();
        foreach (var rawLine in content[tableStart..].Split('\n').Skip(1))
        {
            var line = rawLine.TrimEnd();
            if (!line.StartsWith('|'))
            {
                break;
            }

            var cells = ScorecardValidator.SplitMarkdownRow(line);
            if (cells.Count > 0 && cells.All(cell => TableSeparatorPattern.IsMatch(cell)))
            {
                continue;
            }

            if (cells.Count != 3)
            {
                errors.Add($"review-result counts row must have 3 columns; found {cells.Count}");
                continue;
            }

            var status = ScorecardValidator.TrimCode(cells[0]);
            if (status.Length == 0 &&
                string.Equals(cells[1].Trim(), "**Total**", StringComparison.Ordinal))
            {
                continue;
            }

            if (!ScorecardValidator.StatusValues.Contains(status))
            {
                errors.Add(
                    $"review-result counts row has invalid canonical status '{status}'; expected " +
                    string.Join(", ", StatusOrder));
                continue;
            }

            declaredOrder.Add(status);
            if (!string.Equals(cells[1].Trim(), DisplayResults[status], StringComparison.Ordinal))
            {
                errors.Add(
                    $"review-result counts row '{status}' must use display label " +
                    $"'{DisplayResults[status]}'");
            }

            if (!int.TryParse(cells[2].Trim(), CultureInfo.InvariantCulture, out var declared))
            {
                errors.Add($"review-result counts row '{status}' has a non-numeric count");
                continue;
            }

            if (declared != actual[status])
            {
                errors.Add(
                    $"review-result counts declare {declared} '{status}' rows but the presented " +
                    $"table contains {actual[status]}");
            }
        }

        if (!declaredOrder.SequenceEqual(StatusOrder, StringComparer.Ordinal))
        {
            errors.Add(
                "review-result counts must list statuses in canonical order: " +
                string.Join(", ", StatusOrder));
        }
    }

    private static void ValidateFixedText(string content, List<string> errors)
    {
        if (!content.Contains(FeedbackCallout, StringComparison.Ordinal))
        {
            errors.Add("missing or altered feedback-requested callout");
        }

        if (!content.Contains(FullReportSentence, StringComparison.Ordinal))
        {
            errors.Add($"missing '## Full report' lead sentence: {FullReportSentence}");
        }

        var calloutIndex = content.IndexOf(FeedbackCallout, StringComparison.Ordinal);
        var fullReportIndex = IndexOfLine(content, RequiredSections[1]);
        if (calloutIndex >= 0 && fullReportIndex >= 0 && calloutIndex > fullReportIndex)
        {
            errors.Add("feedback callout must appear before '## Full report'");
        }
    }

    private static void ValidateEvidenceAnchors(string content, List<string> errors)
    {
        var ledger = EvidenceLedgerRowPattern
            .Matches(content)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        var referenced = EvidenceReferencePattern
            .Matches(content)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        var dangling = referenced.Except(ledger, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (dangling.Length > 0)
        {
            errors.Add(
                $"evidence anchors do not resolve to a ledger row: {string.Join(", ", dangling)}");
        }
    }

    private static void ValidateNoLocalPaths(string content, List<string> errors)
    {
        var matches = LocalPathPattern
            .Matches(content)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (matches.Length > 0)
        {
            errors.Add(
                "tracker body leaks local absolute paths: " + string.Join(", ", matches));
        }
    }

    private static void CompareSourceField(
        ICollection<string> errors,
        string identifier,
        string field,
        string expected,
        string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            errors.Add(
                $"SOURCE001: tracker row '{identifier}' {field} differs from the source report.");
        }
    }

    private static string NormalizeEvidenceReferences(
        string value,
        bool stableEvidence)
    {
        if (!stableEvidence)
        {
            return value;
        }

        return string.Join(
            ' ',
            StableEvidenceReferencePattern
                .Matches(value)
                .Select(match => match.Value));
    }

    private static int IndexOfLine(string content, string line)
    {
        if (content.StartsWith(line + "\n", StringComparison.Ordinal) ||
            string.Equals(content, line, StringComparison.Ordinal))
        {
            return 0;
        }

        var needle = "\n" + line + "\n";
        var index = content.IndexOf(needle, StringComparison.Ordinal);
        if (index >= 0)
        {
            return index + 1;
        }

        needle = "\n" + line;
        index = content.IndexOf(needle, StringComparison.Ordinal);
        return content.EndsWith(needle, StringComparison.Ordinal) && index >= 0 ? index + 1 : -1;
    }

    private sealed record PresentedRow(
        string Identifier,
        string Requirement,
        string Scope,
        string Status,
        string Evidence,
        int LineNumber);
}
