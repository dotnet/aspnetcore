// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace BlazorComponentReadiness.Tests;

/// <summary>
/// Covers the tracker presentation contract. Every negative case here is a defect that shipped to a
/// real partner-facing project board before the gate existed.
/// </summary>
public sealed class TrackerValidatorTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly SkillLayout Layout = SkillLayout.Create(Path.Combine(
        RepositoryRoot,
        ".github",
        "skills",
        "blazor-component-readiness"));

    [Fact]
    public void CanonicalBodyPassesValidation()
    {
        Assert.Empty(Validate(BuildBody()));
    }

    [Fact]
    public void MissingFixAreaSectionFails()
    {
        var body = BuildBody();
        var start = body.IndexOf("## Areas we believe need to be fixed", StringComparison.Ordinal);
        var end = body.IndexOf("## Full report", StringComparison.Ordinal);

        var errors = Validate(body[..start] + body[end..]);

        Assert.Contains(
            errors,
            error => error.Contains("missing required section", StringComparison.Ordinal));
    }

    [Fact]
    public void NonDefectIdInFixAreaSummaryFails()
    {
        var requirements = Requirements();
        var verified = requirements.First(requirement =>
            string.Equals(StatusFor(requirements, requirement), "verified", StringComparison.Ordinal));
        var body = BuildBody().Replace(
            "| Grouped defects | Observed in evidence. | ",
            $"| Grouped defects | Observed in evidence. | `{verified.Identifier}`, ",
            StringComparison.Ordinal);

        var errors = Validate(body);

        Assert.Contains(
            errors,
            error => error.Contains("not canonical defects", StringComparison.Ordinal) &&
                error.Contains(verified.Identifier, StringComparison.Ordinal));
    }

    [Fact]
    public void OmittedDefectInFixAreaSummaryFails()
    {
        var requirements = Requirements();
        var defect = requirements.First(requirement =>
            string.Equals(StatusFor(requirements, requirement), "defect", StringComparison.Ordinal));
        var body = BuildBody().Replace(
            $"`{defect.Identifier}`, ",
            string.Empty,
            StringComparison.Ordinal);

        var errors = Validate(body);

        Assert.Contains(
            errors,
            error => error.Contains("omits canonical defect IDs", StringComparison.Ordinal) &&
                error.Contains(defect.Identifier, StringComparison.Ordinal));
    }

    [Fact]
    public void IntroDefectCountThatDisagreesWithTheTableFails()
    {
        var body = BuildBody();
        var actual = DefectCount().ToString(CultureInfo.InvariantCulture);
        var wrong = (DefectCount() + 2).ToString(CultureInfo.InvariantCulture);

        var errors = Validate(body.Replace(
            $"The {actual} canonical",
            $"The {wrong} canonical",
            StringComparison.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains("defect rows but the presented table contains", StringComparison.Ordinal));
    }

    [Fact]
    public void FiveColumnRequirementRowFails()
    {
        var body = BuildBody().Replace(
            "| LP-01 | Requirement text. | repository-wide | `verified` | " +
            "Copilot-reviewed positive evidence | [E-001] |",
            "| LP-01 | Requirement text. | repository-wide | `verified` | [E-001] |",
            StringComparison.Ordinal);

        var errors = Validate(body);

        Assert.Contains(
            errors,
            error => error.Contains("must have 6 columns", StringComparison.Ordinal));
    }

    [Fact]
    public void ReviewResultThatDoesNotMatchCanonicalStatusFails()
    {
        var body = BuildBody().Replace(
            "| LP-01 | Requirement text. | repository-wide | `verified` | " +
            "Copilot-reviewed positive evidence | [E-001] |",
            "| LP-01 | Requirement text. | repository-wide | `verified` | " +
            "Potential issue identified | [E-001] |",
            StringComparison.Ordinal);

        var errors = Validate(body);

        Assert.Contains(
            errors,
            error => error.Contains("must be derived from its canonical status", StringComparison.Ordinal));
    }

    [Fact]
    public void UnbackticikedCanonicalStatusFails()
    {
        var body = BuildBody().Replace(
            "| LP-01 | Requirement text. | repository-wide | `verified` |",
            "| LP-01 | Requirement text. | repository-wide | verified |",
            StringComparison.Ordinal);

        var errors = Validate(body);

        Assert.Contains(
            errors,
            error => error.Contains("must be enclosed in backticks", StringComparison.Ordinal));
    }

    [Fact]
    public void HandWrittenCountThatDisagreesWithTheRowsFails()
    {
        var body = BuildBody();
        var defects = DefectCount();
        var errors = Validate(body.Replace(
            $"| `defect` | Potential issue identified | {defects} |",
            $"| `defect` | Potential issue identified | {defects + 1} |",
            StringComparison.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains("review-result counts declare", StringComparison.Ordinal));
    }

    [Fact]
    public void TerminalNewlineFails()
    {
        var errors = Validate(BuildBody() + "\n");

        Assert.Contains(
            errors,
            error => error.Contains("must not end with a newline", StringComparison.Ordinal));
    }

    [Fact]
    public void RenamedSectionFails()
    {
        var errors = Validate(BuildBody().Replace(
            "## Structural validation and limitations",
            "## Structural coverage",
            StringComparison.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains("unexpected top-level sections", StringComparison.Ordinal));
        Assert.Contains(
            errors,
            error => error.Contains("missing required section", StringComparison.Ordinal));
    }

    [Fact]
    public void DanglingEvidenceAnchorFails()
    {
        var errors = Validate(BuildBody().Replace(
            "| LP-01 | Requirement text. | repository-wide | `verified` | " +
            "Copilot-reviewed positive evidence | [E-001] |",
            "| LP-01 | Requirement text. | repository-wide | `verified` | " +
            "Copilot-reviewed positive evidence | [E-999] |",
            StringComparison.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains("do not resolve to a ledger row", StringComparison.Ordinal) &&
                error.Contains("E-999", StringComparison.Ordinal));
    }

    [Fact]
    public void LocalAbsolutePathFails()
    {
        var errors = Validate(BuildBody().Replace(
            "Receipt retained with the review.",
            "Receipt retained at /Users/reviewer/reports/receipt.json.",
            StringComparison.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains("leaks local absolute paths", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingRequirementRowFails()
    {
        var body = BuildBody();
        var lines = body.Split('\n')
            .Where(line => !line.StartsWith("| SUP-10 |", StringComparison.Ordinal))
            .ToArray();

        var errors = Validate(string.Join('\n', lines));

        Assert.Contains(
            errors,
            error => error.Contains("missing requirement rows", StringComparison.Ordinal) &&
                error.Contains("SUP-10", StringComparison.Ordinal));
    }

    [Fact]
    public void ReportAuthoredScopeOverrideFails()
    {
        var body = ReplaceRequirementCell(
            BuildBody(),
            "CI-02",
            cellIndex: 2,
            "repository-wide");

        var errors = Validate(body);

        Assert.Contains(
            errors,
            error => error.Contains("CI-02", StringComparison.Ordinal) &&
                error.Contains("scope 'repository-wide'", StringComparison.Ordinal) &&
                error.Contains("expected 'component-specific'", StringComparison.Ordinal));
    }

    [Fact]
    public void RequirementStatusesAreExactAndCaseSensitive()
    {
        var requirements = Requirements();
        var requirement = requirements.First(candidate =>
            string.Equals(
                StatusFor(requirements, candidate),
                "not applicable",
                StringComparison.Ordinal));

        foreach (var invalid in new[]
        {
            "N/A",
            "n/a",
            "Not Applicable",
            "not-applicable",
            "not aplicable",
        })
        {
            var errors = Validate(ReplaceRequirementCell(
                BuildBody(),
                requirement.Identifier,
                cellIndex: 3,
                $"`{invalid}`"));

            Assert.Contains(
                errors,
                error => error.Contains(
                    $"unknown canonical status '{invalid}'",
                    StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CountRowStatusesReportExplicitInvalidCanonicalStatus()
    {
        foreach (var invalid in new[] { "N/A", "Not Applicable", "not aplicable" })
        {
            var body = BuildBody().Replace(
                "| `not applicable` | Not applicable to reviewed scope |",
                $"| `{invalid}` | Not applicable to reviewed scope |",
                StringComparison.Ordinal);

            var errors = Validate(body);

            Assert.Contains(
                errors,
                error => error.Contains(
                    $"invalid canonical status '{invalid}'",
                    StringComparison.Ordinal));
        }
    }

    [Fact]
    public void DuplicateRequirementRowFails()
    {
        var body = BuildBody();
        var row = body
            .Split('\n')
            .Single(line => line.StartsWith("| SUP-10 |", StringComparison.Ordinal));
        body = body.Replace(row, row + "\n" + row, StringComparison.Ordinal);

        var errors = Validate(body);

        Assert.Contains(
            errors,
            error => error.Contains(
                "duplicate requirement row SUP-10",
                StringComparison.Ordinal));
    }

    private static IReadOnlyList<Requirement> Requirements()
    {
        return ScorecardValidator.LoadRequirementSet(Layout, []);
    }

    private static string StatusFor(IReadOnlyList<Requirement> requirements, Requirement requirement)
    {
        var index = requirements.ToList().FindIndex(candidate =>
            string.Equals(candidate.Identifier, requirement.Identifier, StringComparison.Ordinal));

        return TrackerValidator.StatusOrder[index % TrackerValidator.StatusOrder.Count];
    }

    private static int DefectCount()
    {
        var requirements = Requirements();

        return requirements.Count(requirement =>
            string.Equals(StatusFor(requirements, requirement), "defect", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> Validate(string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var snapshot = new ReportSnapshot("tracker-body.md", body, bytes);

        return TrackerValidator.Validate(snapshot, Requirements());
    }

    private static string ReplaceRequirementCell(
        string body,
        string identifier,
        int cellIndex,
        string value)
    {
        var lines = body.Split('\n');
        var lineIndex = Array.FindIndex(
            lines,
            line => line.StartsWith($"| {identifier} |", StringComparison.Ordinal));
        Assert.True(lineIndex >= 0);
        var cells = ScorecardValidator.SplitMarkdownRow(lines[lineIndex]).ToArray();
        cells[cellIndex] = value;
        lines[lineIndex] = "| " + string.Join(" | ", cells) + " |";

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Builds a body that satisfies the canonical contract so each test can break exactly one rule.
    /// </summary>
    private static string BuildBody()
    {
        var requirements = Requirements();
        var defects = requirements
            .Where(requirement =>
                string.Equals(StatusFor(requirements, requirement), "defect", StringComparison.Ordinal))
            .Select(requirement => $"`{requirement.Identifier}`")
            .ToArray();

        var builder = new StringBuilder();
        builder.Append("# Sample readiness assessment — Sample.Package 1.0.0\n\n");
        builder.Append("> **Private project draft:** Scope statement.\n\n");
        builder.Append("> **Review limitation:** AI review statement.\n\n");
        builder.Append("## Areas we believe need to be fixed\n\n");
        builder.Append(CultureInfo.InvariantCulture, $"The {defects.Length} canonical `defect` rows in the full report consolidate into the 1 areas below. These areas are not ordered by priority and require human confirmation. Each should be confirmed against the linked evidence before it is treated as a final product or release determination.\n\n");
        builder.Append(TrackerValidator.FixAreaHeader);
        builder.Append("\n|---|---|---|---|\n");
        builder.Append(CultureInfo.InvariantCulture, $"| Grouped defects | Observed in evidence. | {string.Join(", ", defects)} | [E-001] |\n\n");
        builder.Append(TrackerValidator.FeedbackCallout);
        builder.Append("\n\n## Full report\n\n");
        builder.Append(TrackerValidator.FullReportSentence);
        builder.Append("\n\n## Exact review scope\n\nReviewed owner/repo@SHA.\n\n");
        builder.Append("## Review-result counts\n\n");
        builder.Append(TrackerValidator.CountsTableHeader);
        builder.Append("\n|---|---|---:|\n");
        foreach (var status in TrackerValidator.StatusOrder)
        {
            var count = requirements.Count(requirement =>
                string.Equals(StatusFor(requirements, requirement), status, StringComparison.Ordinal));
            builder.Append(CultureInfo.InvariantCulture, $"| `{status}` | {TrackerValidator.DisplayResults[status]} | {count} |\n");
        }

        builder.Append(CultureInfo.InvariantCulture, $"|  | **Total** | **{requirements.Count}** |\n\n");
        builder.Append("## Status terminology\n\nCanonical statuses are defined by the rubric.\n\n");
        builder.Append("## Complete rubric requirement mapping\n\n");
        builder.Append(TrackerValidator.PresentedTableHeader);
        builder.Append("\n|---|---|---|---|---|---|\n");
        foreach (var requirement in requirements)
        {
            var status = StatusFor(requirements, requirement);
            builder.Append(
                CultureInfo.InvariantCulture,
                $"| {requirement.Identifier} | Requirement text. | " +
                $"{requirement.Scope ?? "component-specific"} | `{status}` | " +
                $"{TrackerValidator.DisplayResults[status]} | [E-001] |\n");
        }

        builder.Append("\n## Evidence ledger\n\n");
        builder.Append("| Evidence ID | Claim | Repository/SHA or package | Evidence type | Reproduction/source | Rechecked now? |\n");
        builder.Append("|---|---|---|---|---|---|\n");
        builder.Append("| E-001 | Sample claim. | owner/repo@SHA | source | `LICENSE` | yes |\n\n");
        builder.Append("## Structural validation and limitations\n\n");
        builder.Append("Receipt retained with the review.");

        return builder.ToString();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "activate.sh")) &&
                Directory.Exists(Path.Combine(
                    directory.FullName,
                    ".github",
                    "skills",
                    "blazor-component-readiness")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root.");
    }
}
