// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class RevisionValidator
{
    private const string AssessmentOpening = "```bcr-assessment-v1";
    private const string AssessmentClosing = "```";
    private const string FeedbackColumn = "Feedback after review";

    internal static IReadOnlyList<string> Validate(
        ReportSnapshot previous,
        ReportSnapshot revised,
        IReadOnlySet<string> changedIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(revised);
        ArgumentNullException.ThrowIfNull(changedIdentifiers);

        var errors = new List<string>();
        var previousTable = ParseRevisionTable(previous, "previous", errors);
        var revisedTable = ParseRevisionTable(revised, "revised", errors);
        if (previousTable is null || revisedTable is null)
        {
            return errors;
        }

        ValidateAssessmentIdentity(previous, revised, errors);
        if (!previousTable.Header.SequenceEqual(
            revisedTable.Header,
            StringComparer.Ordinal))
        {
            errors.Add(
                "REV012: report shape changed during correction; revise the existing " +
                "tracker or source-report shape in place.");
        }

        ValidateRowSets(
            previousTable.Rows,
            revisedTable.Rows,
            changedIdentifiers,
            errors);
        ValidateFeedbackPreservation(previous, revised, errors);

        return errors;
    }

    private static RevisionTable? ParseRevisionTable(
        ReportSnapshot report,
        string role,
        ICollection<string> errors)
    {
        var rows = new List<RevisionRow>();
        IReadOnlyList<string>? discoveredHeader = null;
        IReadOnlyList<string>? header = null;
        var identifierIndex = -1;
        var requirementIndex = -1;
        var scopeIndex = -1;
        var statusIndex = -1;
        var evidenceIndex = -1;
        var lineNumber = 0;
        using var reader = new StringReader(report.Content);
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (!line.TrimStart().StartsWith('|'))
            {
                header = null;
                continue;
            }

            var cells = ScorecardValidator.SplitMarkdownRow(line);
            var candidateIdentifierIndex = IndexOf(cells, "Requirement ID");
            var candidateRequirementIndex = IndexOf(cells, "Requirement");
            var candidateScopeIndex = IndexOf(cells, "Requirement scope");
            var candidateStatusIndex = IndexOf(cells, "Status");
            if (candidateStatusIndex < 0)
            {
                candidateStatusIndex = IndexOf(cells, "Canonical status");
            }

            var candidateEvidenceIndex = IndexOf(cells, "Evidence");
            if (candidateIdentifierIndex >= 0 &&
                candidateRequirementIndex >= 0 &&
                candidateScopeIndex >= 0 &&
                candidateStatusIndex >= 0 &&
                candidateEvidenceIndex >= 0)
            {
                if (discoveredHeader is not null || rows.Count > 0)
                {
                    errors.Add(
                        $"REV001: {role} report contains more than one scorecard table.");
                    return null;
                }

                header = cells.ToArray();
                discoveredHeader = header;
                identifierIndex = candidateIdentifierIndex;
                requirementIndex = candidateRequirementIndex;
                scopeIndex = candidateScopeIndex;
                statusIndex = candidateStatusIndex;
                evidenceIndex = candidateEvidenceIndex;
                continue;
            }

            if (header is null ||
                cells.Count != header.Count ||
                cells.All(IsTableSeparator))
            {
                continue;
            }

            var identifier = TrimCode(cells[identifierIndex]);
            if (!IsRequirementIdentifier(identifier))
            {
                continue;
            }

            rows.Add(new RevisionRow(
                identifier,
                cells[requirementIndex],
                TrimCode(cells[scopeIndex]),
                TrimCode(cells[statusIndex]),
                cells[evidenceIndex],
                cells.ToArray(),
                lineNumber));
        }

        if (rows.Count == 0)
        {
            errors.Add($"REV001: {role} report contains no scorecard rows.");
            return null;
        }

        var duplicates = rows
            .GroupBy(row => row.Identifier, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
        {
            errors.Add(
                $"REV002: {role} report contains duplicate requirement rows: " +
                string.Join(", ", duplicates));
            return null;
        }

        return new RevisionTable(
            discoveredHeader!,
            rows.ToDictionary(row => row.Identifier, StringComparer.Ordinal));
    }

    private static void ValidateAssessmentIdentity(
        ReportSnapshot previous,
        ReportSnapshot revised,
        ICollection<string> errors)
    {
        var previousAssessment = ExtractAssessment(previous, "previous", errors);
        var revisedAssessment = ExtractAssessment(revised, "revised", errors);
        if (previousAssessment is not null &&
            revisedAssessment is not null &&
            previousAssessment != revisedAssessment)
        {
            errors.Add(
                "REV003: exact assessment identity changed; use a new assessment " +
                "rather than revising an existing one.");
        }
    }

    private static ExactAssessmentIdentity? ExtractAssessment(
        ReportSnapshot report,
        string role,
        ICollection<string> errors)
    {
        var lines = report.Content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
        var openings = lines
            .Select((line, index) => (line, index))
            .Where(item => string.Equals(
                item.line,
                AssessmentOpening,
                StringComparison.Ordinal))
            .Select(item => item.index)
            .ToArray();
        if (openings.Length == 0)
        {
            errors.Add(
                $"REV004: {role} report requires exactly one " +
                $"{AssessmentOpening} block.");
            return null;
        }

        if (openings.Length != 1)
        {
            errors.Add(
                $"REV004: {role} report contains {openings.Length} " +
                $"{AssessmentOpening} blocks; expected one.");
            return null;
        }

        var opening = openings[0];
        if (opening + 2 >= lines.Length ||
            !string.Equals(
                lines[opening + 2],
                AssessmentClosing,
                StringComparison.Ordinal))
        {
            errors.Add(
                $"REV004: {role} report has a malformed " +
                $"{AssessmentOpening} block.");
            return null;
        }

        try
        {
            return CanonicalEvidenceJson.ParseAssessment(
                System.Text.Encoding.UTF8.GetBytes(lines[opening + 1]));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or
            System.Text.Json.JsonException)
        {
            errors.Add(
                $"REV004: {role} report has an invalid " +
                $"{AssessmentOpening} block: {exception.Message}");
            return null;
        }
    }

    private static void ValidateRowSets(
        IReadOnlyDictionary<string, RevisionRow> previousRows,
        IReadOnlyDictionary<string, RevisionRow> revisedRows,
        IReadOnlySet<string> changedIdentifiers,
        ICollection<string> errors)
    {
        var removed = previousRows.Keys
            .Except(revisedRows.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var added = revisedRows.Keys
            .Except(previousRows.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        AddIdentifierError(
            errors,
            "REV005: revised report removed requirement rows",
            removed);
        AddIdentifierError(
            errors,
            "REV005: revised report added requirement rows",
            added);

        var unknownDeclarations = changedIdentifiers
            .Where(identifier =>
                !previousRows.ContainsKey(identifier) ||
                !revisedRows.ContainsKey(identifier))
            .Order(StringComparer.Ordinal)
            .ToArray();
        AddIdentifierError(
            errors,
            "REV006: declared changed IDs are absent from one or both reports",
            unknownDeclarations);

        foreach (var identifier in previousRows.Keys
            .Intersect(revisedRows.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            var previous = previousRows[identifier];
            var revised = revisedRows[identifier];
            if (!string.Equals(previous.Requirement, revised.Requirement, StringComparison.Ordinal) ||
                !string.Equals(previous.Scope, revised.Scope, StringComparison.Ordinal))
            {
                errors.Add(
                    $"REV007: {identifier} changed canonical requirement text or scope.");
                continue;
            }

            var rowChanged = !RowsEqual(previous, revised);
            if (!changedIdentifiers.Contains(identifier))
            {
                if (rowChanged)
                {
                    errors.Add(
                        $"REV008: {identifier} changed without being declared in " +
                        "--changed-ids.");
                }

                continue;
            }

            if (!rowChanged)
            {
                errors.Add(
                    $"REV009: {identifier} was declared in --changed-ids but its row " +
                    "did not change.");
                continue;
            }

            if (!string.Equals(previous.Status, revised.Status, StringComparison.Ordinal) &&
                string.Equals(previous.Evidence, revised.Evidence, StringComparison.Ordinal))
            {
                errors.Add(
                    $"REV010: {identifier} changed status without changing evidence.");
            }
        }
    }

    private static bool RowsEqual(RevisionRow previous, RevisionRow revised)
    {
        return previous.Cells.SequenceEqual(revised.Cells, StringComparer.Ordinal);
    }

    private static void ValidateFeedbackPreservation(
        ReportSnapshot previous,
        ReportSnapshot revised,
        ICollection<string> errors)
    {
        foreach (var feedback in ExtractFeedbackCells(previous))
        {
            if (!revised.Content.Contains(feedback, StringComparison.Ordinal))
            {
                errors.Add(
                    "REV011: prior partner feedback disappeared from the revised " +
                    $"report: '{feedback}'.");
            }
        }
    }

    private static IReadOnlyList<string> ExtractFeedbackCells(ReportSnapshot report)
    {
        var feedback = new List<string>();
        var feedbackColumnIndex = -1;
        using var reader = new StringReader(report.Content);
        while (reader.ReadLine() is { } line)
        {
            if (!line.TrimStart().StartsWith('|'))
            {
                feedbackColumnIndex = -1;
                continue;
            }

            var cells = ScorecardValidator.SplitMarkdownRow(line);
            var declaredFeedbackColumn = -1;
            for (var index = 0; index < cells.Count; index++)
            {
                if (string.Equals(
                    cells[index],
                    FeedbackColumn,
                    StringComparison.Ordinal))
                {
                    declaredFeedbackColumn = index;
                    break;
                }
            }

            if (declaredFeedbackColumn >= 0)
            {
                feedbackColumnIndex = declaredFeedbackColumn;
                continue;
            }

            if (cells.Any(cell => string.Equals(
                cell,
                "Requirement ID",
                StringComparison.Ordinal)))
            {
                feedbackColumnIndex = -1;
                continue;
            }

            if (feedbackColumnIndex < 0)
            {
                continue;
            }

            if (feedbackColumnIndex >= cells.Count ||
                cells.All(IsTableSeparator))
            {
                continue;
            }

            var value = cells[feedbackColumnIndex].Trim();
            if (!string.IsNullOrWhiteSpace(value) && value is not "-")
            {
                feedback.Add(value);
            }
        }

        return feedback.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static bool IsTableSeparator(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length >= 3 &&
            trimmed.All(character => character is '-' or ':');
    }

    private static int IndexOf(IReadOnlyList<string> cells, string value)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            if (string.Equals(cells[index], value, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static string TrimCode(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 &&
            trimmed.StartsWith('`') &&
            trimmed.EndsWith('`'))
        {
            return trimmed[1..^1].Trim();
        }

        return trimmed;
    }

    private static bool IsRequirementIdentifier(string value)
    {
        var separator = value.LastIndexOf('-');
        return separator > 0 &&
            separator < value.Length - 1 &&
            value[..separator].All(character =>
                character is >= 'A' and <= 'Z') &&
            value[(separator + 1)..].All(char.IsAsciiDigit);
    }

    private static void AddIdentifierError(
        ICollection<string> errors,
        string prefix,
        IReadOnlyList<string> identifiers)
    {
        if (identifiers.Count > 0)
        {
            errors.Add($"{prefix}: {string.Join(", ", identifiers)}.");
        }
    }

    private sealed record RevisionTable(
        IReadOnlyList<string> Header,
        IReadOnlyDictionary<string, RevisionRow> Rows);

    private sealed record RevisionRow(
        string Identifier,
        string Requirement,
        string Scope,
        string Status,
        string Evidence,
        IReadOnlyList<string> Cells,
        int LineNumber);
}
