// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace BlazorComponentReadiness.Tests;

public sealed class RevisionValidatorTests
{
    [Fact]
    public void AcceptsSurgicalCorrectionThatPreservesFeedbackAndOtherRows()
    {
        using var directory = new TemporaryDirectory();
        var previousPath = WriteReport(
            directory.DirectoryPath,
            "previous.md",
            firstStatus: "defect",
            firstEvidence: "Documentation was not supplied.",
            includeFeedback: true);
        var revisedPath = WriteReport(
            directory.DirectoryPath,
            "revised.md",
            firstStatus: "verified",
            firstEvidence: "Supplied documentation explicitly establishes support.",
            includeFeedback: true);
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = RevisionCommand.Run(
            [
                "--previous", previousPath,
                "--changed-ids", "BEQ-02",
                revisedPath,
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.Contains(
            "Revision validation passed",
            output.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStatusOnlyCorrection()
    {
        var previous = CreateSnapshot(
            firstStatus: "defect",
            firstEvidence: "Documentation was not supplied.");
        var revised = CreateSnapshot(
            firstStatus: "verified",
            firstEvidence: "Documentation was not supplied.");

        var errors = RevisionValidator.Validate(
            previous,
            revised,
            new HashSet<string>(["BEQ-02"], StringComparer.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains(
                "REV010: BEQ-02 changed status without changing evidence",
                StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsUndeclaredUnrelatedRowChange()
    {
        var previous = CreateSnapshot(
            secondEvidence: "Exact package inspection passed.");
        var revised = CreateSnapshot(
            firstStatus: "verified",
            firstEvidence: "Supplied documentation explicitly establishes support.",
            secondEvidence: "Lower-precedence source inspection passed.");

        var errors = RevisionValidator.Validate(
            previous,
            revised,
            new HashSet<string>(["BEQ-02"], StringComparer.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains(
                "REV008: PI-01 changed without being declared",
                StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsDroppedPartnerFeedback()
    {
        var previous = CreateSnapshot(includeFeedback: true);
        var revised = CreateSnapshot(
            firstStatus: "verified",
            firstEvidence: "Supplied documentation explicitly establishes support.",
            includeFeedback: false);

        var errors = RevisionValidator.Validate(
            previous,
            revised,
            new HashSet<string>(["BEQ-02"], StringComparer.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains(
                "REV011: prior partner feedback disappeared",
                StringComparison.Ordinal));
        Assert.Contains(
            errors,
            error => error.Contains(
                "https://github.com/example/components/issues/42",
                StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsChangedAssessmentIdentity()
    {
        var previous = CreateSnapshot();
        var revised = CreateSnapshot(componentId: "Different component");

        var errors = RevisionValidator.Validate(
            previous,
            revised,
            new HashSet<string>(["BEQ-02"], StringComparer.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains(
                "REV003: exact assessment identity changed",
                StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsReplacingTrackerWithSourceReportShape()
    {
        var previous = CreateSnapshot(trackerShape: true);
        var revised = CreateSnapshot(
            firstStatus: "verified",
            firstEvidence: "Supplied documentation explicitly establishes support.");

        var errors = RevisionValidator.Validate(
            previous,
            revised,
            new HashSet<string>(["BEQ-02"], StringComparer.Ordinal));

        Assert.Contains(
            errors,
            error => error.Contains(
                "REV012: report shape changed during correction",
                StringComparison.Ordinal));
    }

    private static ReportSnapshot CreateSnapshot(
        string firstStatus = "defect",
        string firstEvidence = "Documentation was not supplied.",
        string secondEvidence = "Exact package inspection passed.",
        bool includeFeedback = true,
        string componentId = "Widget",
        bool trackerShape = false)
    {
        var content = CreateReport(
            firstStatus,
            firstEvidence,
            secondEvidence,
            includeFeedback,
            componentId,
            trackerShape);
        return new ReportSnapshot(
            "report.md",
            content,
            Encoding.UTF8.GetBytes(content));
    }

    private static string WriteReport(
        string directory,
        string fileName,
        string firstStatus,
        string firstEvidence,
        bool includeFeedback)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(
            path,
            CreateReport(
                firstStatus,
                firstEvidence,
                "Exact package inspection passed.",
                includeFeedback,
                "Widget",
                trackerShape: false),
            new UTF8Encoding(false));
        return path;
    }

    private static string CreateReport(
        string firstStatus,
        string firstEvidence,
        string secondEvidence,
        bool includeFeedback,
        string componentId,
        bool trackerShape)
    {
        var assessment = Encoding.UTF8.GetString(
            CanonicalEvidenceJson.SerializeAssessment(
                new ExactAssessmentIdentity(
                    new RepositoryIdentity(
                        "https://github.com/example/components",
                        new string('a', 40)),
                    new ArtifactIdentity(
                        "released-package",
                        new PackageIdentity(
                            "example.components",
                            "1.0.0",
                            new Sha256Digest("sha256", new string('b', 64)))),
                    componentId)));
        var feedbackSection = includeFeedback
            ? """
                | Area | Requirement IDs | Feedback after review |
                |---|---|---|
                | Render modes | `BEQ-02` | Partner confirmed the docs in [issue 42](https://github.com/example/components/issues/42). |

                """
            : string.Empty;
        var scorecard = trackerShape
            ? $$"""
                | Requirement ID | Requirement | Requirement scope | Canonical status | Review result | Evidence |
                |---|---|---|---|---|---|
                | BEQ-02 | Document supported render modes. | component-specific | {{firstStatus}} | Needs attention | {{firstEvidence}} |
                | PI-01 | Verify package signing. | repository-wide | verified | Verified | {{secondEvidence}} |
                """
            : $$"""
                | Requirement ID | Requirement | Requirement scope | Status | Evidence | Maintainer action | Reviewer follow-up |
                |---|---|---|---|---|---|---|
                | BEQ-02 | Document supported render modes. | component-specific | {{firstStatus}} | {{firstEvidence}} | - | - |
                | PI-01 | Verify package signing. | repository-wide | verified | {{secondEvidence}} | - | - |
                """;
        return $$"""
            # Widget readiness assessment

            ```bcr-assessment-v1
            {{assessment}}
            ```

            {{feedbackSection}}{{scorecard}}

            End of report.
            """;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"bcr-revision-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
        }

        internal string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
