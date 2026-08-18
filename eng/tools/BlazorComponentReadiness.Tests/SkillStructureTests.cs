// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;

namespace BlazorComponentReadiness.Tests;

public sealed class SkillStructureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly SkillLayout Layout = SkillLayout.Create(Path.Combine(
        RepositoryRoot,
        ".github",
        "skills",
        "blazor-component-readiness"));
    private static readonly RubricSnapshot Rubric =
        ScorecardValidator.LoadCoreRubric(Layout.ChecklistPath);
    private static readonly IReadOnlyDictionary<string, string> ExpectedCoreScopes =
        BuildExpectedCoreScopes();
    private static readonly string[] HistoricalScopeDriftIds =
        [
            "BEQ-01", "BEQ-02", "BEQ-03", "BEQ-12", "BEQ-13", "BEQ-14", "BEQ-15", "BEQ-16",
            "BEQ-17", "CI-02", "CI-03", "CI-04", "PERF-06", "PERF-07", "PERF-08", "PERF-09",
            "PERF-10", "TA-04",
        ];
    private static readonly Lazy<string?> WritableMountedVolumeRoot =
        new(FindWritableMountedVolumeRoot);
    private static readonly Lazy<bool> DirectorySymbolicLinkSupport =
        new(CanCreateDirectorySymbolicLink);
    private static readonly Lazy<bool> DirectoryRenameWithOpenFileSupport =
        new(CanRenameDirectoryContainingOpenFile);

    private static bool HasWritableMountedVolumeRoot =>
        WritableMountedVolumeRoot.Value is not null;

    private static bool SupportsDirectorySymbolicLinks =>
        DirectorySymbolicLinkSupport.Value;

    private static bool SupportsDirectoryRenameWithOpenFile =>
        DirectoryRenameWithOpenFileSupport.Value;

    [Fact]
    public void ChecklistHas110CoreIdsAnd12OverlayIds()
    {
        var rubric = ScorecardValidator.LoadCoreRubric(Layout.ChecklistPath);
        var requirements = rubric.Requirements;
        var identifiers = requirements
            .Select(requirement => requirement.Identifier)
            .ToArray();

        Assert.Equal(110, identifiers.Length);
        Assert.Equal(110, CanonicalRequirementSchema.RequirementScopes.Count);
        Assert.Equal("1.3.0", rubric.Version);
        Assert.Equal(1, rubric.ScopeSchemaVersion);
        Assert.Equal(64, rubric.Sha256.Length);
        Assert.Equal(identifiers.Length, identifiers.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ExpectedCoreScopes.Keys.Order(), identifiers.Order());
        Assert.All(ExpectedCoreScopes, expected =>
            Assert.Equal(
                expected.Value,
                CanonicalRequirementSchema.RequirementScopes[expected.Key]));
        Assert.All(requirements, requirement =>
            Assert.Equal(ExpectedCoreScopes[requirement.Identifier], requirement.Scope));
        Assert.Equal(46, requirements.Count(requirement =>
            string.Equals(requirement.Scope, "repository-wide", StringComparison.Ordinal)));
        Assert.Equal(64, requirements.Count(requirement =>
            string.Equals(requirement.Scope, "component-specific", StringComparison.Ordinal)));
        Assert.Contains("TA-08", identifiers);
        Assert.DoesNotContain("SCF-01", identifiers);
        Assert.DoesNotContain("AI-01", identifiers);

        var allRequirements = ScorecardValidator.LoadRequirementSet(
            Layout,
            Layout.OverlayPaths.Keys);
        var allIdentifiers = allRequirements
            .Select(requirement => requirement.Identifier)
            .ToArray();
        Assert.Equal(122, allIdentifiers.Length);
        Assert.Contains("SCF-01", allIdentifiers);
        Assert.Contains("AI-01", allIdentifiers);
        Assert.Contains(
            "**Rubric version:** 1.3.0",
            File.ReadAllText(Layout.ChecklistPath, Encoding.UTF8));

        foreach (var values in allIdentifiers
            .GroupBy(identifier => identifier[..identifier.LastIndexOf('-')])
            .Select(group => group
                .Select(identifier => int.Parse(
                    identifier[(identifier.LastIndexOf('-') + 1)..],
                    CultureInfo.InvariantCulture))
                .ToArray()))
        {
            Assert.Equal(values.Order().ToArray(), values);
        }
    }

    [Fact]
    public void CompleteScorecardPasses()
    {
        var requirements = ScorecardValidator.LoadRequirements(Layout.ChecklistPath);
        var rows = requirements
            .Select((requirement, index) => new ScorecardRow(
                requirement.Identifier,
                requirement.Text,
                requirement.Scope!,
                "not applicable",
                "The requirement does not apply to this bounded component.",
                "-",
                "-",
                index + 1))
            .ToArray();

        Assert.Empty(ScorecardValidator.ValidateRows(requirements, rows));
    }

    [Fact]
    public void GeneratedMarkdownTableIsParseable()
    {
        var requirements = ScorecardValidator.LoadRequirements(Layout.ChecklistPath);
        var report = CompleteTemplate(requirements);
        using var directory = new TemporaryDirectory();
        var reportPath = Path.Combine(directory.DirectoryPath, "report.md");
        File.WriteAllText(reportPath, report, new UTF8Encoding(false));

        var rows = ScorecardValidator.ParseScorecard(reportPath);

        Assert.Equal(110, rows.Count);
        Assert.Empty(ScorecardValidator.ValidateRows(requirements, rows));
    }

    [Fact]
    public void NonScorecardTablesAreIgnored()
    {
        var requirements = ScorecardValidator.LoadRequirements(Layout.ChecklistPath);
        var report =
            "| Finding ID | Title | Scope | Status | Evidence | Owner | Follow-up |\n" +
            "|---|---|---|---|---|---|---|\n" +
            "| FAIL-01 | Example | component | open | proof | maintainer | retest |\n\n" +
            CompleteTemplate(requirements);
        using var directory = new TemporaryDirectory();
        var reportPath = Path.Combine(directory.DirectoryPath, "report.md");
        File.WriteAllText(reportPath, report, new UTF8Encoding(false));

        var rows = ScorecardValidator.ParseScorecard(reportPath);

        Assert.Equal(110, rows.Count);
    }

    [Fact]
    public void MissingDuplicateAndInvalidRowsFail()
    {
        var requirements = ScorecardValidator.LoadRequirements(Layout.ChecklistPath);
        var first = requirements[0];
        var rows = new[]
        {
            new ScorecardRow(
                first.Identifier,
                first.Text,
                "wrong-scope",
                "pass",
                "TBD",
                "-",
                "-",
                1),
            new ScorecardRow(
                first.Identifier,
                first.Text,
                "repository-wide",
                "verified",
                "Exact public license.",
                "-",
                "-",
                2),
        };

        var errors = string.Join('\n', ScorecardValidator.ValidateRows(requirements, rows));

        Assert.Contains("Missing requirement rows", errors);
        Assert.Contains("Duplicate requirement rows", errors);
        Assert.Contains("invalid status", errors);
        Assert.Contains("invalid scope", errors);
        Assert.Contains("evidence must explain", errors);
    }

    [Fact]
    public void OnlyFiveStatusValuesExist()
    {
        var expected = new HashSet<string>(
            [
                "verified",
                "defect",
                "maintainer evidence required",
                "not tested",
                "not applicable",
            ],
            StringComparer.Ordinal);

        Assert.True(expected.SetEquals(ScorecardValidator.StatusValues));
    }

    [Fact]
    public void ShuffledScorecardFails()
    {
        var requirements = ScorecardValidator.LoadRequirements(Layout.ChecklistPath);
        var rows = requirements
            .Reverse()
            .Select((requirement, index) => new ScorecardRow(
                requirement.Identifier,
                requirement.Text,
                requirement.Scope!,
                "not applicable",
                "Not part of the bounded deliverable.",
                "-",
                "-",
                index + 1))
            .ToArray();

        Assert.Contains(
            "Requirement rows are not in canonical checklist order",
            ScorecardValidator.ValidateRows(requirements, rows));
    }

    [Fact]
    public void EvidenceAnchorMustResolve()
    {
        var requirements = ScorecardValidator.LoadRequirements(Layout.ChecklistPath);
        var rows = requirements
            .Select((requirement, index) => new ScorecardRow(
                requirement.Identifier,
                requirement.Text,
                requirement.Scope!,
                "not applicable",
                index == 0 ? "[E-001]" : "Not part of the bounded deliverable.",
                "-",
                "-",
                index + 1))
            .ToArray();

        Assert.Contains(
            ScorecardValidator.ValidateRows(requirements, rows, new Dictionary<string, int>()),
            error => error.Contains(
                "unresolved evidence reference [E-001]",
                StringComparison.Ordinal));
        Assert.Empty(ScorecardValidator.ValidateRows(
            requirements,
            rows,
            new Dictionary<string, int>
            {
                ["E-001"] = 200,
            }));
    }

    [Fact]
    public void EvidenceLedgerRejectsDuplicateIds()
    {
        const string Report =
            "| Evidence ID | Claim | Repository/SHA or package | Evidence type | " +
            "Reproduction/source | Rechecked now? |\n" +
            "|---|---|---|---|---|---|\n" +
            "| E-001 | claim one | owner/repo@abc | source | LICENSE | yes |\n" +
            "| E-001 | claim two | package 1.0 | artifact | nupkg | yes |\n";
        using var directory = new TemporaryDirectory();
        var reportPath = Path.Combine(directory.DirectoryPath, "report.md");
        File.WriteAllText(reportPath, Report, new UTF8Encoding(false));

        var ledger = ScorecardValidator.ParseEvidenceLedger(reportPath);

        Assert.Equal(3, ledger.Identifiers["E-001"]);
        Assert.Contains(
            ledger.Errors,
            error => error.Contains(
                "duplicate evidence ledger ID E-001",
                StringComparison.Ordinal));
    }

    [Fact]
    public void StructuralValidationReceiptRecordsSelectionAndDigest()
    {
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var report = ScorecardValidator.RenderTemplate(requirements)
            .Replace("[scope]", "component-specific", StringComparison.Ordinal)
            .Replace("[status]", "not tested", StringComparison.Ordinal)
            .Replace(
                "[evidence]",
                "The bounded probe was not run.",
                StringComparison.Ordinal)
            .Replace("[maintainer action]", "-", StringComparison.Ordinal)
            .Replace(
                "[reviewer follow-up]",
                "Run the bounded probe.",
                StringComparison.Ordinal);
        using var directory = new TemporaryDirectory();
        var reportPath = Path.Combine(directory.DirectoryPath, "targeted.md");
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        File.WriteAllText(reportPath, report, new UTF8Encoding(false));
        var rows = ScorecardValidator.ParseScorecard(reportPath);
        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            [],
            new DateTimeOffset(2026, 8, 13, 18, 0, 0, TimeSpan.Zero));
        ScorecardValidator.WriteValidationReceipt(receiptPath, reportPath, receipt);

        using var document = JsonDocument.Parse(File.ReadAllText(receiptPath, Encoding.UTF8));
        var root = document.RootElement;
        Assert.Equal("1.3.0", root.GetProperty("rubric_version").GetString());
        Assert.Equal(2, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(1, root.GetProperty("scope_schema_version").GetInt32());
        Assert.Equal(64, root.GetProperty("checklist_sha256").GetString()!.Length);
        Assert.Contains(
            root.GetProperty("checklist_sha256").GetString()!,
            root.GetProperty("rubric_identity").GetString()!);
        Assert.Equal("targeted", root.GetProperty("mode").GetString());
        Assert.Equal(
            requirements.Select(requirement => requirement.Identifier),
            root.GetProperty("selected_ids")
                .EnumerateArray()
                .Select(element => element.GetString()));
        Assert.Equal(2, root.GetProperty("canonical_row_count").GetInt32());
        Assert.Equal(2, root.GetProperty("valid_row_count").GetInt32());
        Assert.Equal(
            "2026-08-13T18:00:00Z",
            root.GetProperty("validated_at_utc").GetString());
        Assert.Equal(64, root.GetProperty("report_sha256").GetString()!.Length);
        Assert.Contains(
            "does not establish factual evidence",
            root.GetProperty("limitation").GetString()!);
    }

    [Fact]
    public void SameVersionChecklistTamperingChangesReceiptIdentity()
    {
        var originalRubric = ScorecardValidator.LoadCoreRubric(Layout.ChecklistPath);
        using var skill = CopySkill();
        var copiedLayout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(copiedLayout.ChecklistPath, Encoding.UTF8)
            .Replace(
                "Uses an OSI-approved, non-copyleft license.",
                "Uses an approved license.",
                StringComparison.Ordinal);
        File.WriteAllText(copiedLayout.ChecklistPath, content, new UTF8Encoding(false));
        var changedRubric = ScorecardValidator.LoadCoreRubric(copiedLayout.ChecklistPath);
        var report = new ReportSnapshot(
            "report.md",
            "report",
            Encoding.UTF8.GetBytes("report"));
        var originalReceipt = ScorecardValidator.BuildValidationReceipt(
            originalRubric,
            report,
            "complete",
            originalRubric.Requirements,
            [],
            []);
        var changedReceipt = ScorecardValidator.BuildValidationReceipt(
            changedRubric,
            report,
            "complete",
            changedRubric.Requirements,
            [],
            []);

        Assert.Equal(originalRubric.Version, changedRubric.Version);
        Assert.NotEqual(
            originalReceipt["checklist_sha256"],
            changedReceipt["checklist_sha256"]);
        Assert.NotEqual(
            originalReceipt["rubric_identity"],
            changedReceipt["rubric_identity"]);
    }

    [Fact]
    public void ReceiptUsesLoadedRubricSnapshotAfterChecklistMutation()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var rubric = ScorecardValidator.LoadCoreRubric(layout.ChecklistPath);
        File.WriteAllText(
            layout.ChecklistPath,
            "concurrently replaced\n",
            new UTF8Encoding(false));
        var report = new ReportSnapshot(
            "report.md",
            "report",
            Encoding.UTF8.GetBytes("report"));

        var receipt = ScorecardValidator.BuildValidationReceipt(
            rubric,
            report,
            "complete",
            rubric.Requirements,
            [],
            []);

        Assert.Equal(rubric.Sha256, receipt["checklist_sha256"]);
        Assert.Equal(rubric.Version, receipt["rubric_version"]);
        Assert.Contains(
            rubric.Sha256,
            Assert.IsType<string>(receipt["rubric_identity"]));
    }

    [Fact]
    public void ReceiptValidationAndDigestUseOneReportSnapshot()
    {
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var original = CompleteTemplate(requirements);
        using var directory = new TemporaryDirectory();
        var reportPath = Path.Combine(directory.DirectoryPath, "report.md");
        File.WriteAllText(reportPath, original, new UTF8Encoding(false));
        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);

        File.WriteAllText(reportPath, "concurrently replaced\n", new UTF8Encoding(false));

        var rows = ScorecardValidator.ParseScorecard(snapshot);
        var ledger = ScorecardValidator.ParseEvidenceLedger(snapshot);
        var errors = ledger.Errors.Concat(ScorecardValidator.ValidateRows(
            requirements,
            rows,
            ledger.Identifiers));
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            []);
        var expectedDigest = Convert.ToHexStringLower(
            SHA256.HashData(new UTF8Encoding(false).GetBytes(original)));

        Assert.Empty(errors);
        Assert.Equal(expectedDigest, receipt["report_sha256"]);
        Assert.Equal(2, receipt["valid_row_count"]);
    }

    [Fact]
    public void ReceiptCaseAliasDoesNotOverwriteReportWhenSupported()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "REPORT.MD");
        if (!File.Exists(receiptPath))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test filesystem is case-sensitive.");
        }

        var original = File.ReadAllText(reportPath, Encoding.UTF8);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ScorecardArguments(reportPath, receiptPath),
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("--receipt must not overwrite the report", error.ToString());
        Assert.Equal(original, File.ReadAllText(reportPath, Encoding.UTF8));
    }

    [Fact]
    public void ReceiptSymlinkAliasDoesNotOverwriteReportWhenSupported()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        if (!TryCreateSymbolicLink(receiptPath, reportPath))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test environment cannot create file symbolic links.");
        }

        Assert.Equal(
            FileSystemUtilities.ResolveExistingPath(reportPath),
            FileSystemUtilities.ResolveExistingPath(receiptPath));
        var original = File.ReadAllText(reportPath, Encoding.UTF8);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ScorecardArguments(reportPath, receiptPath),
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("--receipt must not overwrite the report", error.ToString());
        Assert.Equal(original, File.ReadAllText(reportPath, Encoding.UTF8));
    }

    [Fact]
    public void ReceiptHardlinkAliasIsRejectedWithoutOverwritingReportWhenSupported()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        if (!TryCreateHardLink(receiptPath, reportPath))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test environment cannot create hard links.");
        }

        var original = File.ReadAllText(reportPath, Encoding.UTF8);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ScorecardArguments(reportPath, receiptPath),
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("ERROR:", error.ToString());
        Assert.Equal(original, File.ReadAllText(reportPath, Encoding.UTF8));
    }

    [Fact]
    public void ReceiptUnicodeNormalizationAliasDoesNotOverwriteReportWhenSupported()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteTargetedReport(
            directory.DirectoryPath,
            "r\u00e9port.md");
        var receiptPath = Path.Combine(
            directory.DirectoryPath,
            "re\u0301port.md");
        if (!File.Exists(receiptPath))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test filesystem is Unicode-normalization-sensitive.");
        }

        Assert.True(FileSystemUtilities.PathsReferToSameEntry(reportPath, receiptPath));
        var original = File.ReadAllText(reportPath, Encoding.UTF8);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ScorecardArguments(reportPath, receiptPath),
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("--receipt must not overwrite the report", error.ToString());
        Assert.Equal(original, File.ReadAllText(reportPath, Encoding.UTF8));
    }

    [Fact]
    public void ExistingReceiptIsNotOverwritten()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        const string ExistingReceipt = "existing receipt\n";
        File.WriteAllText(receiptPath, ExistingReceipt, Encoding.UTF8);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ScorecardArguments(reportPath, receiptPath),
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("ERROR:", error.ToString());
        Assert.Equal(ExistingReceipt, File.ReadAllText(receiptPath, Encoding.UTF8));
    }

    [Fact]
    public void ReceiptParentLinkSwapCannotOverwriteReportWhenSupported()
    {
        using var directory = new TemporaryDirectory();
        var realDirectory = Path.Combine(directory.DirectoryPath, "real");
        var decoyDirectory = Path.Combine(directory.DirectoryPath, "decoy");
        Directory.CreateDirectory(realDirectory);
        Directory.CreateDirectory(decoyDirectory);
        var reportPath = WriteTargetedReport(realDirectory);
        var parentLink = Path.Combine(directory.DirectoryPath, "parent");
        if (!TryCreateDirectorySymbolicLink(parentLink, decoyDirectory))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test environment cannot create directory symbolic links.");
        }

        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var rows = ScorecardValidator.ParseScorecard(snapshot);
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            []);
        var receiptPath = Path.Combine(parentLink, Path.GetFileName(reportPath));
        var original = File.ReadAllText(reportPath, Encoding.UTF8);

        Assert.Throws<IOException>(() => ScorecardValidator.WriteValidationReceipt(
            receiptPath,
            reportPath,
            receipt,
            beforePublish: () =>
            {
                Directory.Delete(parentLink);
                Directory.CreateSymbolicLink(parentLink, realDirectory);
            }));
        Assert.Equal(original, File.ReadAllText(reportPath, Encoding.UTF8));
        Assert.Empty(Directory.EnumerateFiles(decoyDirectory));
    }

    [Fact]
    public void ReceiptParentLinkSwapToEmptyDirectoryFailsClosedWhenSupported()
    {
        using var directory = new TemporaryDirectory();
        var originalDirectory = Path.Combine(directory.DirectoryPath, "original");
        var alternateDirectory = Path.Combine(directory.DirectoryPath, "alternate");
        Directory.CreateDirectory(originalDirectory);
        Directory.CreateDirectory(alternateDirectory);
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var parentLink = Path.Combine(directory.DirectoryPath, "parent");
        if (!TryCreateDirectorySymbolicLink(parentLink, originalDirectory))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test environment cannot create directory symbolic links.");
        }

        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var rows = ScorecardValidator.ParseScorecard(snapshot);
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            []);
        var receiptPath = Path.Combine(parentLink, "receipt.json");

        Assert.Throws<IOException>(() => ScorecardValidator.WriteValidationReceipt(
            receiptPath,
            reportPath,
            receipt,
            beforePublish: () =>
            {
                Directory.Delete(parentLink);
                Directory.CreateSymbolicLink(parentLink, alternateDirectory);
            }));
        Assert.Empty(Directory.EnumerateFiles(originalDirectory));
        Assert.Empty(Directory.EnumerateFiles(alternateDirectory));
    }

    [Fact]
    public void ReceiptWriteFailureLeavesNoFinalOrTemporaryFile()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var rows = ScorecardValidator.ParseScorecard(snapshot);
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            []);

        Assert.Throws<IOException>(() => ScorecardValidator.WriteValidationReceipt(
            receiptPath,
            reportPath,
            receipt,
            writeContent: (stream, content) =>
            {
                stream.Write(content.Span[..Math.Min(16, content.Length)]);
                throw new IOException("Injected receipt write failure.");
            }));
        Assert.False(File.Exists(receiptPath));
        Assert.DoesNotContain(
            Directory.EnumerateFiles(directory.DirectoryPath),
            path => Path.GetFileName(path).StartsWith(
                ".receipt.json.",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ReceiptStagesBesideFinalDestination()
    {
        using var directory = new TemporaryDirectory();
        var receiptDirectory = Path.Combine(directory.DirectoryPath, "receipts");
        Directory.CreateDirectory(receiptDirectory);
        var receiptPath = Path.Combine(receiptDirectory, "receipt.json");
        string? temporaryPath = null;

        FileSystemUtilities.WriteAllTextNew(
            receiptPath,
            "receipt\n",
            beforePublish: () =>
            {
                temporaryPath = Assert.Single(
                    Directory.EnumerateFiles(
                        receiptDirectory,
                        ".receipt.json.*.tmp"));
                Assert.Equal(
                    Path.GetFullPath(receiptDirectory),
                    Path.GetDirectoryName(temporaryPath));
                Assert.False(File.Exists(receiptPath));
            });

        Assert.Equal("receipt\n", File.ReadAllText(receiptPath, Encoding.UTF8));
        Assert.NotNull(temporaryPath);
        Assert.False(File.Exists(temporaryPath));
    }

    [ConditionalFact]
    [WritableMountedVolumeRoot]
    public void ReceiptAtWritableMountedVolumeRootUsesColocatedStaging()
    {
        var volumeRoot = WritableMountedVolumeRoot.Value!;
        var fileName = $"receipt-{Guid.NewGuid():N}.json";
        var receiptPath = Path.Combine(volumeRoot, fileName);
        string? temporaryPath = null;
        try
        {
            FileSystemUtilities.WriteAllTextNew(
                receiptPath,
                "receipt\n",
                beforePublish: () =>
                {
                    temporaryPath = Assert.Single(
                        Directory.EnumerateFiles(
                            volumeRoot,
                            $".{fileName}.*.tmp"));
                    Assert.Equal(
                        Path.TrimEndingDirectorySeparator(volumeRoot),
                        Path.TrimEndingDirectorySeparator(
                            Path.GetDirectoryName(temporaryPath)!));
                });

            Assert.Equal("receipt\n", File.ReadAllText(receiptPath, Encoding.UTF8));
        }
        finally
        {
            File.Delete(receiptPath);
            if (temporaryPath is not null)
            {
                File.Delete(temporaryPath);
            }
        }
    }

    [Fact]
    public void HistoricalScopeDriftsFailIndividuallyAndTogether()
    {
        var requirements = ScorecardValidator.LoadRequirements(Layout.ChecklistPath);
        var canonicalReport = CompleteTemplate(requirements);
        foreach (var identifier in HistoricalScopeDriftIds)
        {
            var actualScope = OppositeScope(ExpectedCoreScopes[identifier]);
            var rows = ParseScorecard(ReplaceScorecardCell(
                canonicalReport,
                identifier,
                cellIndex: 2,
                actualScope));

            var error = Assert.Single(
                ScorecardValidator.ValidateRows(requirements, rows),
                value => value.Contains(identifier, StringComparison.Ordinal) &&
                    value.Contains($"scope '{actualScope}'", StringComparison.Ordinal) &&
                    value.Contains($"expected '{ExpectedCoreScopes[identifier]}'", StringComparison.Ordinal));
            Assert.Contains("differs from the canonical rubric", error);
        }

        var allDrifted = HistoricalScopeDriftIds.Aggregate(
            canonicalReport,
            (report, identifier) => ReplaceScorecardCell(
                report,
                identifier,
                cellIndex: 2,
                OppositeScope(ExpectedCoreScopes[identifier])));
        var allErrors = ScorecardValidator.ValidateRows(
            requirements,
            ParseScorecard(allDrifted));

        Assert.All(HistoricalScopeDriftIds, identifier =>
            Assert.Contains(allErrors, error =>
                error.Contains(identifier, StringComparison.Ordinal) &&
                error.Contains($"expected '{ExpectedCoreScopes[identifier]}'", StringComparison.Ordinal)));
    }

    [Fact]
    public void SourceStatusesAreExactAndCaseSensitive()
    {
        var requirement = ScorecardValidator.LoadRequirements(Layout.ChecklistPath)[0];
        var canonicalReport = CompleteTemplate([requirement]);

        Assert.Empty(ScorecardValidator.ValidateRows(
            [requirement],
            ParseScorecard(canonicalReport)));

        foreach (var invalid in new[]
        {
            "N/A",
            "n/a",
            "Not Applicable",
            "not-applicable",
            "not aplicable",
        })
        {
            Assert.Contains(
                ScorecardValidator.ValidateRows(
                    [requirement],
                    ParseScorecard(ReplaceScorecardCell(
                        canonicalReport,
                        requirement.Identifier,
                        cellIndex: 3,
                        invalid))),
                error => error.Contains($"invalid status '{invalid}'", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CoreSyntaxRejectsSpacingWhitespaceAndMalformedMarkers()
    {
        var original = File.ReadAllText(Layout.ChecklistPath, Encoding.UTF8);
        var canonical =
            "- **LP-01** (`repository-wide`) Uses an OSI-approved, non-copyleft license.";
        var mutations = new[]
        {
            "- **LP-01**(`repository-wide`) Uses an OSI-approved, non-copyleft license.",
            "- **LP-01**  (`repository-wide`) Uses an OSI-approved, non-copyleft license.",
            "- **LP-01** (`repository-wide`)  Uses an OSI-approved, non-copyleft license.",
            "- **LP-01** (`repository-wide`) Uses an OSI-approved, non-copyleft license. ",
            "- **LP-01** (`repository-wide`) (`repository-wide`) Uses an OSI-approved, non-copyleft license.",
            "- **LP-01** (repository-wide) Uses an OSI-approved, non-copyleft license.",
        };

        foreach (var mutation in mutations)
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.DirectoryPath, "checklist.md");
            File.WriteAllText(
                path,
                original.Replace(canonical, mutation, StringComparison.Ordinal),
                new UTF8Encoding(false));

            Assert.Throws<InvalidDataException>(() =>
                ScorecardValidator.LoadCoreRubric(path));
        }
    }

    [ConditionalFact]
    [DirectoryRenameWithOpenFileSupported]
    public void ReplacedReceiptDirectoryCannotPublishForgedTemporaryBytes()
    {
        using var directory = new TemporaryDirectory();
        var receiptDirectory = Path.Combine(directory.DirectoryPath, "receipts");
        var relocatedDirectory = Path.Combine(directory.DirectoryPath, "relocated");
        Directory.CreateDirectory(receiptDirectory);
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(receiptDirectory, "receipt.json");
        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var rows = ScorecardValidator.ParseScorecard(snapshot);
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            []);
        var originalReport = File.ReadAllText(reportPath, Encoding.UTF8);

        var exception = Assert.Throws<IOException>(
            () => ScorecardValidator.WriteValidationReceipt(
                receiptPath,
                reportPath,
                receipt,
                beforePublish: () =>
                {
                    Directory.Move(receiptDirectory, relocatedDirectory);
                    Directory.CreateDirectory(receiptDirectory);
                    var relocatedTemporaryPath = Assert.Single(
                        Directory.EnumerateFiles(
                            relocatedDirectory,
                            ".receipt.json.*.tmp"));
                    File.WriteAllText(
                        Path.Combine(
                            receiptDirectory,
                            Path.GetFileName(relocatedTemporaryPath)),
                        "forged receipt\n",
                        Encoding.UTF8);
                }));

        Assert.Contains("Receipt content changed", exception.Message);
        Assert.False(File.Exists(receiptPath));
        Assert.False(File.Exists(Path.Combine(relocatedDirectory, "receipt.json")));
        Assert.Equal(originalReport, File.ReadAllText(reportPath, Encoding.UTF8));
        Assert.Empty(Directory.EnumerateFiles(receiptDirectory));
    }

    [Fact]
    public void ReceiptUsesWritableDestinationWhenParentIsNotWritable()
    {
        if (OperatingSystem.IsWindows())
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "Unix directory permissions are required for this test.");
        }

        using var directory = new TemporaryDirectory();
        var receiptDirectory = Path.Combine(directory.DirectoryPath, "receipts");
        Directory.CreateDirectory(receiptDirectory);
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(receiptDirectory, "receipt.json");
        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var rows = ScorecardValidator.ParseScorecard(snapshot);
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            []);

        var originalMode = File.GetUnixFileMode(directory.DirectoryPath);
        try
        {
            MakeDirectoryNonWritable(directory.DirectoryPath);
            SkipIfDirectoryRemainsWritable(directory.DirectoryPath);

            ScorecardValidator.WriteValidationReceipt(
                receiptPath,
                reportPath,
                receipt);
        }
        finally
        {
            File.SetUnixFileMode(directory.DirectoryPath, originalMode);
        }

        Assert.Contains(
            "\"structural_validation\": \"passed\"",
            File.ReadAllText(receiptPath, Encoding.UTF8));
        Assert.DoesNotContain(
            Directory.EnumerateFiles(receiptDirectory),
            path => Path.GetFileName(path).StartsWith(
                ".receipt.json.",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ReceiptInWritableDestinationRejectsForgedTemporaryBytes()
    {
        if (OperatingSystem.IsWindows())
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "Unix directory permissions are required for this test.");
        }

        using var directory = new TemporaryDirectory();
        var receiptDirectory = Path.Combine(directory.DirectoryPath, "receipts");
        Directory.CreateDirectory(receiptDirectory);
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(receiptDirectory, "receipt.json");
        var snapshot = ScorecardValidator.ReadReportSnapshot(reportPath);
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var rows = ScorecardValidator.ParseScorecard(snapshot);
        var receipt = ScorecardValidator.BuildValidationReceipt(
            Rubric,
            snapshot,
            "targeted",
            requirements,
            rows,
            []);
        var originalReport = File.ReadAllText(reportPath, Encoding.UTF8);

        var originalMode = File.GetUnixFileMode(directory.DirectoryPath);
        try
        {
            MakeDirectoryNonWritable(directory.DirectoryPath);
            SkipIfDirectoryRemainsWritable(directory.DirectoryPath);

            Assert.Throws<IOException>(() => ScorecardValidator.WriteValidationReceipt(
                receiptPath,
                reportPath,
                receipt,
                beforePublish: () =>
                {
                    var temporaryPath = Assert.Single(
                        Directory.EnumerateFiles(
                            receiptDirectory,
                            ".receipt.json.*.tmp"));
                    File.WriteAllText(
                        temporaryPath,
                        "forged receipt\n",
                        Encoding.UTF8);
                }));
        }
        finally
        {
            File.SetUnixFileMode(directory.DirectoryPath, originalMode);
        }

        Assert.False(File.Exists(receiptPath));
        Assert.Equal(originalReport, File.ReadAllText(reportPath, Encoding.UTF8));
        Assert.DoesNotContain(
            Directory.EnumerateFiles(receiptDirectory),
            path => Path.GetFileName(path).StartsWith(
                ".receipt.json.",
                StringComparison.Ordinal));
    }

    [Fact]
    public void TargetedScorecardSelectsOnlyNamedIdsInCanonicalOrder()
    {
        var allRequirements = ScorecardValidator.LoadRequirementSet(
            Layout,
            Layout.OverlayPaths.Keys);

        var targeted = ScorecardValidator.SelectRequirements(
            allRequirements,
            "BEQ-15,BEQ-12,SCF-02");

        Assert.Equal(
            ["BEQ-12", "BEQ-15", "SCF-02"],
            targeted.Select(requirement => requirement.Identifier));
        Assert.Equal("component-specific", targeted[0].Scope);
        Assert.Equal("component-specific", targeted[1].Scope);
        Assert.Null(targeted[2].Scope);
        var report = ScorecardValidator.RenderTemplate(targeted)
            .Replace("[scope]", "component-specific", StringComparison.Ordinal)
            .Replace("[status]", "not tested", StringComparison.Ordinal)
            .Replace(
                "[evidence]",
                "The targeted probe was not run.",
                StringComparison.Ordinal)
            .Replace("[maintainer action]", "-", StringComparison.Ordinal)
            .Replace(
                "[reviewer follow-up]",
                "Run the named deterministic probe.",
                StringComparison.Ordinal);
        using var directory = new TemporaryDirectory();
        var reportPath = Path.Combine(directory.DirectoryPath, "targeted.md");
        File.WriteAllText(reportPath, report, new UTF8Encoding(false));
        var rows = ScorecardValidator.ParseScorecard(reportPath);

        Assert.Empty(ScorecardValidator.ValidateRows(targeted, rows));
    }

    [Fact]
    public void TargetedScorecardRejectsUnknownAndDuplicateIds()
    {
        var requirements = ScorecardValidator.LoadRequirementSet(
            Layout,
            Layout.OverlayPaths.Keys);

        var unknown = Assert.Throws<InvalidDataException>(() =>
            ScorecardValidator.SelectRequirements(requirements, "BEQ-12,NOPE-01"));
        var duplicate = Assert.Throws<InvalidDataException>(() =>
            ScorecardValidator.SelectRequirements(requirements, "BEQ-12,BEQ-12"));

        Assert.Contains("Unknown targeted IDs", unknown.Message);
        Assert.Contains("Duplicate targeted IDs", duplicate.Message);
    }

    [Fact]
    public void VallySuiteCoversEveryPrefix()
    {
        var requirements = ScorecardValidator.LoadRequirementSet(
            Layout,
            Layout.OverlayPaths.Keys);
        var expectedPrefixes = requirements
            .Select(requirement =>
                requirement.Identifier[..requirement.Identifier.LastIndexOf('-')])
            .ToHashSet(StringComparer.Ordinal);
        var actualPrefixes = SkillValidator
            .ParseVallyStimuli(Layout.VallyPath)
            .SelectMany(stimulus =>
                stimulus.Tags["requirement_prefixes"]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(expectedPrefixes.SetEquals(actualPrefixes));
    }

    [Fact]
    public void EvalAssetsUseRepositoryEvalLayout()
    {
        var expectedRoot = Path.Combine(
            RepositoryRoot,
            "eng",
            "skill-evals",
            "blazor-component-readiness");

        Assert.Equal(expectedRoot, Layout.EvalRoot);
        Assert.True(File.Exists(Layout.VallyPath));
        Assert.True(File.Exists(Layout.EvalPolicyPath));
        Assert.True(File.Exists(Path.Combine(
            Layout.EvalRoot,
            "fixtures",
            "mixed-evidence-component.md")));
        Assert.False(Directory.Exists(Path.Combine(Layout.Root, "evals")));
    }

    [Fact]
    public void VallySuiteIsPinnedAndGoverned()
    {
        var content = File.ReadAllText(Layout.VallyPath, Encoding.UTF8);
        var stimuli = SkillValidator.ParseVallyStimuli(Layout.VallyPath);

        Assert.True(stimuli.Count >= 14);
        Assert.Contains($"# Validated with {SkillValidator.VallyPackage}.", content);
        Assert.Contains("  runs: 5", content);
        Assert.Contains("  judge_model: claude-opus-5", content);
        Assert.Contains("dest: \"eval-input/evidence.md\"", content);
        Assert.All(stimuli, stimulus =>
        {
            Assert.True(stimulus.RubricCount >= 4);
            Assert.False(string.IsNullOrEmpty(stimulus.Tags["provenance_source"]));
            Assert.False(string.IsNullOrEmpty(stimulus.Tags["positive_controls"]));
            Assert.False(string.IsNullOrEmpty(stimulus.Tags["negative_controls"]));
        });
    }

    [Fact]
    public void BlankSkillDirectoryIsReportedWithoutThrowing()
    {
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = SkillValidationCommand.Run(
            ["--skill-dir", ""],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: --skill-dir requires a non-empty value.",
            error.ToString());
    }

    [Fact]
    public void BlankScorecardSkillDirectoryIsReportedWithoutThrowing()
    {
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ["--skill-dir", "", "--emit-template"],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: --skill-dir requires a non-empty value.",
            error.ToString());
    }

    [Fact]
    public void BlankScorecardReportPathIsReportedWithoutThrowing()
    {
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ["   "],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: report path requires a non-empty value.",
            error.ToString());
    }

    [Fact]
    public void BlankScorecardChecklistPathIsReportedWithoutThrowing()
    {
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            ["--checklist", "", "--emit-template"],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: --checklist requires a non-empty value.",
            error.ToString());
    }

    [Fact]
    public void CustomScorecardChecklistCannotProduceReceipt()
    {
        using var directory = new TemporaryDirectory();
        var checklistPath = Path.Combine(directory.DirectoryPath, "checklist.md");
        File.Copy(Layout.ChecklistPath, checklistPath);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--checklist",
                checklistPath,
                "--receipt",
                Path.Combine(directory.DirectoryPath, "receipt.json"),
            ],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "--receipt cannot be combined with a custom --checklist.",
            error.ToString());
    }

    [Fact]
    public void MissingCoreRequirementFailsTemplateAndValidationCommands()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var checklist = File.ReadAllText(layout.ChecklistPath, Encoding.UTF8);
        checklist = checklist.Replace(
            "- **SUP-10** (`repository-wide`) The release process defines how readiness regressions " +
            "suspend a release or supported status and how revalidation restores it.\n",
            string.Empty,
            StringComparison.Ordinal);
        File.WriteAllText(layout.ChecklistPath, checklist, new UTF8Encoding(false));
        var reportPath = Path.Combine(skill.DirectoryPath, "report.md");
        File.WriteAllText(reportPath, "not a valid report", new UTF8Encoding(false));

        var templateError = new StringWriter(CultureInfo.InvariantCulture);
        var templateExitCode = ScorecardCommand.Run(
            ["--skill-dir", skill.DirectoryPath, "--emit-template"],
            TextWriter.Null,
            templateError);
        var validationError = new StringWriter(CultureInfo.InvariantCulture);
        var validationExitCode = ScorecardCommand.Run(
            ["--skill-dir", skill.DirectoryPath, reportPath],
            TextWriter.Null,
            validationError);

        Assert.Equal(1, templateExitCode);
        Assert.Equal(1, validationExitCode);
        Assert.Contains("Expected 110 canonical core requirements", templateError.ToString());
        Assert.Contains("Expected 110 canonical core requirements", validationError.ToString());
    }

    [Fact]
    public void ValidLookingCoreIdSubstitutionsFailAllCommands()
    {
        foreach (var replacement in new[] { "TA-09", "SCF-01", "AI-01" })
        {
            AssertCoreIdSubstitutionRejected(replacement);
        }
    }

    [Fact]
    public void BlankScorecardReceiptPathIsReportedWithoutThrowing()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteTargetedReport(directory.DirectoryPath);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,LP-02",
                "--legacy-evidence",
                reportPath,
                "--receipt",
                "\t",
            ],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: --receipt requires a non-empty value.",
            error.ToString());
    }

    [Fact]
    public void DuplicateVallyTagsAreReportedWithoutThrowing()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            "      eval_id: \"1\"\n",
            "      eval_id: \"1\"\n      eval_id: \"duplicate\"\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = SkillValidationCommand.Run(
            ["--skill-dir", skill.DirectoryPath],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: eval-01-artifact-provenance: duplicate Vally tag eval_id",
            error.ToString());
    }

    [Fact]
    public void CompleteForgedVallyTagsBeforeRealMappingAreRejected()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(content, "      tier: \"train\"\n", string.Empty);
        content = ReplaceFirst(
            content,
            "    tags:\n",
            "    tags:\n" +
            "      eval_id: \"forged\"\n" +
            "      area: \"forged\"\n" +
            "      score_family: \"forged\"\n" +
            "      tier: \"train\"\n" +
            "      requirement_prefixes: \"LP,PI\"\n" +
            "      provenance_kind: \"forged\"\n" +
            "      provenance_source: \"forged\"\n" +
            "      positive_controls: \"0\"\n" +
            "      negative_controls: \"1\"\n" +
            "    tags:\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "eval-01-artifact-provenance: expected exactly one stimulus-level " +
                "Vally tags mapping; found 2",
                StringComparison.Ordinal));
    }

    [Fact]
    public void TrailingSpaceRealVallyTagsWithEarlierForgeryAreRejected()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(content, "    tags:\n", "    tags: \n");
        content = ReplaceFirst(
            content,
            "      Timebox one published Blazor grid-component review",
            "    tags:\n" +
            "      eval_id: \"forged\"\n" +
            "      area: \"forged\"\n" +
            "      score_family: \"forged\"\n" +
            "      tier: \"train\"\n" +
            "      requirement_prefixes: \"LP,PI\"\n" +
            "      provenance_kind: \"forged\"\n" +
            "      provenance_source: \"forged\"\n" +
            "      positive_controls: \"0\"\n" +
            "      negative_controls: \"1\"\n" +
            "      Timebox one published Blazor grid-component review");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "eval-01-artifact-provenance: expected exactly one stimulus-level " +
                "Vally tags mapping; found 2",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("  # stimulus metadata")]
    public void VallyTagsMarkerAcceptsYamlTrailingWhitespace(string suffix)
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(content, "    tags:\n", $"    tags:{suffix}\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Empty(SkillValidator.Validate(layout));
    }

    [Theory]
    [InlineData("'")]
    [InlineData("\"")]
    public void MultilineQuotedVallyPromptCannotSupplyTags(string quote)
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            "    prompt: |-\n",
            $"    prompt: {quote}ambiguous prompt\n" +
            "    tags:\n" +
            "      eval_id: \"forged\"\n" +
            "      area: \"forged\"\n" +
            "      score_family: \"forged\"\n" +
            "      tier: \"train\"\n" +
            "      requirement_prefixes: \"LP,PI\"\n" +
            "      provenance_kind: \"forged\"\n" +
            "      provenance_source: \"forged\"\n" +
            "      positive_controls: \"0\"\n" +
            "      negative_controls: \"1\"\n" +
            $"      end{quote}\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "eval-01-artifact-provenance: prompt must use the supported " +
                "'prompt: |-' block scalar form",
                StringComparison.Ordinal));
    }

    [Fact]
    public void CanonicalVallyPromptBlockScalarsAreAccepted()
    {
        Assert.Empty(SkillValidator.Validate(Layout));
    }

    [Fact]
    public void ArchitecturePortabilityRegressionCannotBeReplaced()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = content.Replace(
            "eval-23-architecture-portability",
            "eval-23-replaced-portability",
            StringComparison.Ordinal);
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "missing architecture portability regression",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ArchitecturePortabilityRegressionRequiresScenarioContent()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        var stimulusStart = content.IndexOf(
            "  - name: \"eval-23-architecture-portability\"\n",
            StringComparison.Ordinal);
        Assert.True(stimulusStart >= 0);
        var promptStart = content.IndexOf(
            "    prompt: |-\n",
            stimulusStart,
            StringComparison.Ordinal) + "    prompt: |-\n".Length;
        var tagsStart = content.IndexOf(
            "    tags:\n",
            promptStart,
            StringComparison.Ordinal);
        content =
            content[..promptStart] +
            "      Review an ordinary unrelated component using general guidance.\n" +
            content[tagsStart..];
        var rubricStart = content.IndexOf(
            "    rubric:\n",
            stimulusStart,
            StringComparison.Ordinal) + "    rubric:\n".Length;
        content =
            content[..rubricStart] +
            string.Concat(
                Enumerable.Range(1, 9).Select(index =>
                    $"      - \"Generic unrelated check {index}.\"\n"));
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        var errors = SkillValidator.Validate(layout);

        Assert.Contains(
            errors,
            error => error.Contains(
                "prompt no longer exercises a synthetic handwritten standalone component",
                StringComparison.Ordinal));
        Assert.Contains(
            errors,
            error => error.Contains(
                "negative-controlled rubric items no longer state",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ArchitecturePortabilityRegressionRejectsDuplicateName()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        var stimulusStart = content.IndexOf(
            "  - name: \"eval-23-architecture-portability\"\n",
            StringComparison.Ordinal);
        Assert.True(stimulusStart >= 0);
        var duplicate = content[stimulusStart..].Replace(
            "      eval_id: \"23\"\n",
            "      eval_id: \"24\"\n",
            StringComparison.Ordinal);
        File.WriteAllText(
            layout.VallyPath,
            content + duplicate,
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = SkillValidationCommand.Run(
            ["--skill-dir", skill.DirectoryPath],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "duplicate stimulus name 'eval-23-architecture-portability'",
            error.ToString());
        Assert.DoesNotContain(
            nameof(InvalidOperationException),
            error.ToString());
    }

    [Fact]
    public void ArchitecturePortabilityRequiresPositiveControlledContent()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        var positiveItems = new[]
        {
            "The target remains one handwritten standalone date selector; generated ownership and a shared component runtime are not assumed.",
            "Artifact acquisition uses the target's configured public source and binds the exact package ID, version, digest, and repository snapshot rather than borrowing another suite's feed or package records.",
            "The same bundled core, applicable overlays, exact status vocabulary, and evidence hierarchy apply without changing the rubric for the new architecture.",
            "Open-source release evidence is evaluated as observed; absent commercial release machinery or a paid support program is not invented or treated as a defect merely because unrelated components have them.",
            "Unknown support or governance claims remain bounded evidence requests, and unperformed applicable probes remain not tested rather than inferred from unrelated components.",
        };
        for (var index = 0; index < positiveItems.Length; index++)
        {
            content = content.Replace(
                $"      - \"{positiveItems[index]}\"\n",
                $"      - \"Unrelated positive control filler {index + 1}.\"\n",
                StringComparison.Ordinal);
        }

        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        var errors = SkillValidator.Validate(layout);

        Assert.Contains(
            errors,
            error => error.Contains(
                "positive-controlled rubric items no longer affirm",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            errors,
            error => error.Contains(
                "negative-controlled rubric items no longer state",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ArchitecturePortabilityRejectsInvertedPositiveBinding()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = content.Replace(
            "      - \"Artifact acquisition uses the target's configured public source " +
            "and binds the exact package ID, version, digest, and repository snapshot " +
            "rather than borrowing another suite's feed or package records.\"\n",
            "      - \"Do not bind the exact package ID or repository snapshot; " +
            "borrowing another suite's feed or package records is acceptable.\"\n",
            StringComparison.Ordinal);
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        var errors = SkillValidator.Validate(layout);

        Assert.Contains(
            errors,
            error => error.Contains(
                "positive-controlled rubric items no longer affirm",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            errors,
            error => error.Contains(
                "negative-controlled rubric items no longer state",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ArchitecturePortabilityRejectsInvertedNegativeFailure()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = content.Replace(
            "      - \"A response that assumes generated wrappers, a shared runtime, " +
            "private feeds, commercial release machinery, or unrelated-component evidence " +
            "fails this portability case.\"\n",
            "      - \"A response that avoids generated wrappers, a shared runtime, " +
            "commercial release machinery, and unrelated-component evidence fails this portability case.\"\n",
            StringComparison.Ordinal);
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        var errors = SkillValidator.Validate(layout);

        Assert.Contains(
            errors,
            error => error.Contains(
                "negative-controlled rubric items no longer state",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            errors,
            error => error.Contains(
                "positive-controlled rubric items no longer affirm",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("eval-01-artifact-provenance")]
    [InlineData("eval-10-cross-area-adjudication")]
    [InlineData("eval-20-evidence-only-tracker-result")]
    public void VallyStimulusNamesAcceptInlineComments(string stimulusName)
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            $"  - name: \"{stimulusName}\"\n",
            $"  - name: \"{stimulusName}\" # governed stimulus\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        var stimuli = SkillValidator.ParseVallyStimuli(layout.VallyPath);

        Assert.Equal(23, stimuli.Count);
        Assert.Contains(stimuli, stimulus => stimulus.Name == stimulusName);
        Assert.Empty(SkillValidator.Validate(layout));
    }

    [Fact]
    public void CanonicalVallySuiteWithCrLfIsValid()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        ConvertToCrLf(layout.VallyPath);

        var stimuli = SkillValidator.ParseVallyStimuli(layout.VallyPath);

        Assert.Equal(23, stimuli.Count);
        Assert.Empty(SkillValidator.Validate(layout));
    }

    [Theory]
    [InlineData("eval-01-artifact-provenance")]
    [InlineData("eval-10-cross-area-adjudication")]
    [InlineData("eval-20-evidence-only-tracker-result")]
    public void VallyStimulusInlineCommentsWithCrLfAreValid(string stimulusName)
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            $"  - name: \"{stimulusName}\"\n",
            $"  - name: \"{stimulusName}\" # governed stimulus\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));
        ConvertToCrLf(layout.VallyPath);

        var stimuli = SkillValidator.ParseVallyStimuli(layout.VallyPath);

        Assert.Equal(23, stimuli.Count);
        Assert.Empty(SkillValidator.Validate(layout));
    }

    [Theory]
    [InlineData("eval-01-artifact-provenance")]
    [InlineData("eval-10-cross-area-adjudication")]
    [InlineData("eval-20-evidence-only-tracker-result")]
    public void MalformedVallyStimulusNamesAreRejected(string stimulusName)
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            $"  - name: \"{stimulusName}\"\n",
            $"  - name: {stimulusName}\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "unsupported stimulus declaration",
                StringComparison.Ordinal));
    }

    [Fact]
    public void MalformedVallyStimulusNameWithCrLfIsRejected()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            "  - name: \"eval-01-artifact-provenance\"\n",
            "  - name: eval-01-artifact-provenance\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));
        ConvertToCrLf(layout.VallyPath);

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "unsupported stimulus declaration",
                StringComparison.Ordinal));
    }

    [Fact]
    public void UnparsedContentBeforeFirstVallyStimulusIsRejected()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            "stimuli:\n",
            "stimuli:\n  ungoverned: true\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "unparsed content appears before the first stimulus",
                StringComparison.Ordinal));
    }

    [Fact]
    public void UnparsedContentAfterLastVallyStimulusIsRejected()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        File.AppendAllText(
            layout.VallyPath,
            "\nungoverned_suffix: true\n",
            new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "unparsed content appears after stimulus " +
                "eval-23-architecture-portability",
                StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateStimulusLevelVallyTagsMappingsAreRejected()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            "    tags:\n",
            "    tags:\n      extra: \"value\"\n    tags:\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "eval-01-artifact-provenance: expected exactly one stimulus-level " +
                "Vally tags mapping; found 2",
                StringComparison.Ordinal));
    }

    [Fact]
    public void RequiredVallyTagInPromptTextIsStillReportedMissing()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(content, "      tier: \"train\"\n", string.Empty);
        content = ReplaceFirst(
            content,
            "    prompt: |-\n",
            "    prompt: |-\n      tier: \"train\"\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        var errors = SkillValidator.Validate(layout);

        Assert.Contains(
            errors,
            error => error.Contains(
                "eval-01-artifact-provenance: missing Vally tags tier",
                StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidAreaPlaybookPathIsReportedWithoutThrowing()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        File.AppendAllText(
            layout.AreasIndexPath,
            "\nInvalid: `bad\0path.md`\n",
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = SkillValidationCommand.Run(
            ["--skill-dir", skill.DirectoryPath],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: Invalid area playbook reference 'bad\\0path.md': contains NUL",
            error.ToString());
    }

    [Theory]
    [InlineData("2147483648")]
    [InlineData("not-a-number")]
    [InlineData("-1")]
    [InlineData("1,,2")]
    public void InvalidVallyControlIndexesAreReportedWithoutThrowing(string value)
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            "      positive_controls: \"1,2,3\"\n",
            $"      positive_controls: \"{value}\"\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = SkillValidationCommand.Run(
            ["--skill-dir", skill.DirectoryPath],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "ERROR: eval-01-artifact-provenance: positive_controls contains " +
            "invalid control index",
            error.ToString());
    }

    [Fact]
    public void VallyControlIndexOutsideRubricIsReported()
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var content = File.ReadAllText(layout.VallyPath, Encoding.UTF8);
        content = ReplaceFirst(
            content,
            "      positive_controls: \"1,2,3\"\n",
            "      positive_controls: \"2147483647\"\n");
        File.WriteAllText(layout.VallyPath, content, new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error == "eval-01-artifact-provenance: control index is out of range");
    }

    [Theory]
    [InlineData("../outside.md")]
    [InlineData("ABSOLUTE")]
    public void AreaPlaybooksOutsideExpectedDirectoryAreRejected(string reference)
    {
        using var skill = CopySkill();
        using var outside = new TemporaryDirectory();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var outsidePath = Path.Combine(outside.DirectoryPath, "outside.md");
        File.WriteAllText(outsidePath, "outside\n", Encoding.UTF8);
        if (reference == "../outside.md")
        {
            File.Copy(
                outsidePath,
                Path.Combine(skill.DirectoryPath, "references", "outside.md"));
        }
        else
        {
            reference = outsidePath;
        }

        File.AppendAllText(
            layout.AreasIndexPath,
            $"\nExternal: `{reference}`\n",
            new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "Area playbook must remain under",
                StringComparison.Ordinal));
    }

    [Fact]
    public void AreaPlaybookSymlinkOutsideExpectedDirectoryIsRejectedWhenSupported()
    {
        using var skill = CopySkill();
        using var outside = new TemporaryDirectory();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var outsidePath = Path.Combine(outside.DirectoryPath, "outside.md");
        File.WriteAllText(outsidePath, "outside\n", Encoding.UTF8);
        var linkPath = Path.Combine(
            skill.DirectoryPath,
            "references",
            "areas",
            "linked.md");
        if (!TryCreateSymbolicLink(linkPath, outsidePath))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test environment cannot create file symbolic links.");
        }

        File.AppendAllText(
            layout.AreasIndexPath,
            "\nExternal: `linked.md`\n",
            new UTF8Encoding(false));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "Area playbook must remain under",
                StringComparison.Ordinal));
    }

    [ConditionalFact]
    [DirectorySymbolicLinksSupported]
    public void AreaDirectorySymlinkOutsideSkillIsRejectedWhenSupported()
    {
        using var skill = CopySkill();
        using var outside = new TemporaryDirectory();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var areaDirectory = Path.GetDirectoryName(layout.AreasIndexPath)!;
        var externalAreaDirectory = Path.Combine(outside.DirectoryPath, "areas");
        Directory.Move(areaDirectory, externalAreaDirectory);
        Assert.True(TryCreateDirectorySymbolicLink(
            areaDirectory,
            externalAreaDirectory));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "Area directory must remain under skill root",
                StringComparison.Ordinal));
    }

    [ConditionalFact]
    [DirectorySymbolicLinksSupported]
    public void AreaDirectorySymlinkedAncestorOutsideSkillIsRejectedWhenSupported()
    {
        using var skill = CopySkill();
        using var outside = new TemporaryDirectory();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var referencesDirectory = Path.Combine(skill.DirectoryPath, "references");
        var externalReferencesDirectory = Path.Combine(
            outside.DirectoryPath,
            "references");
        Directory.Move(referencesDirectory, externalReferencesDirectory);
        Assert.True(TryCreateDirectorySymbolicLink(
            referencesDirectory,
            externalReferencesDirectory));

        Assert.Contains(
            SkillValidator.Validate(layout),
            error => error.Contains(
                "Area directory must remain under skill root",
                StringComparison.Ordinal));
    }

    [Fact]
    public void TrackerOutputStructureIsWired()
    {
        var skill = File.ReadAllText(Layout.SkillPath, Encoding.UTF8);
        var report = File.ReadAllText(Layout.ReportTemplatePath, Encoding.UTF8);
        var vally = File.ReadAllText(Layout.VallyPath, Encoding.UTF8);

        Assert.Contains("evidence-only evaluation result", skill);
        Assert.Contains("Areas we believe need to be fixed", skill);
        Assert.Contains("false positives", skill);
        Assert.Contains("do not claim", skill);
        Assert.Contains("requirement-level crosswalk", skill);
        Assert.Contains("## Evidence-only evaluation result", report);
        Assert.Contains("### Areas we believe need to be fixed", report);
        Assert.Contains("### Full report", report);
        Assert.Contains("Canonical status", report);
        Assert.Contains("eval-20-evidence-only-tracker-result", vally);
    }

    [Fact]
    public void SkillStructureIsValid()
    {
        Assert.Empty(SkillValidator.Validate(Layout));
    }

    private static IReadOnlyDictionary<string, string> BuildExpectedCoreScopes()
    {
        var repositoryWide = new[]
        {
            "LP-01", "LP-02", "LP-03", "LP-04", "LP-05", "LP-06", "LP-07", "LP-08", "LP-09", "LP-10",
            "PI-01", "PI-02", "PI-03", "PI-04", "PI-05", "PI-06", "PI-07", "PI-08", "PI-09", "PI-10",
            "PI-11", "PI-12",
            "SEC-04", "SEC-05", "SEC-06", "SEC-07", "SEC-08", "SEC-09",
            "BEQ-21", "BEQ-24",
            "TA-07", "TA-08",
            "CI-01", "CI-05", "CI-06", "CI-07", "CI-08",
            "SUP-01", "SUP-02", "SUP-03", "SUP-04", "SUP-05", "SUP-06", "SUP-07", "SUP-08", "SUP-10",
        };
        var componentSpecific = new[]
        {
            "SEC-01", "SEC-02", "SEC-03", "SEC-10", "SEC-11", "SEC-12", "SEC-13",
            "A11Y-01", "A11Y-02", "A11Y-03", "A11Y-04", "A11Y-05", "A11Y-06", "A11Y-07",
            "A11Y-08", "A11Y-09", "A11Y-10", "A11Y-11", "A11Y-12",
            "BEQ-01", "BEQ-02", "BEQ-03", "BEQ-04", "BEQ-05", "BEQ-06", "BEQ-07", "BEQ-08",
            "BEQ-09", "BEQ-10", "BEQ-11", "BEQ-12", "BEQ-13", "BEQ-14", "BEQ-15", "BEQ-16",
            "BEQ-17", "BEQ-18", "BEQ-19", "BEQ-20", "BEQ-22", "BEQ-23",
            "TA-01", "TA-02", "TA-03", "TA-04", "TA-05", "TA-06",
            "PERF-01", "PERF-02", "PERF-03", "PERF-04", "PERF-05", "PERF-06", "PERF-07",
            "PERF-08", "PERF-09", "PERF-10",
            "CI-02", "CI-03", "CI-04", "CI-09", "CI-10", "CI-11",
            "SUP-09",
        };

        return repositoryWide
            .Select(identifier => KeyValuePair.Create(identifier, "repository-wide"))
            .Concat(componentSpecific.Select(identifier =>
                KeyValuePair.Create(identifier, "component-specific")))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    private static IReadOnlyList<ScorecardRow> ParseScorecard(string report)
    {
        var bytes = Encoding.UTF8.GetBytes(report);
        return ScorecardValidator.ParseScorecard(
            new ReportSnapshot("report.md", report, bytes));
    }

    private static string ReplaceScorecardCell(
        string report,
        string identifier,
        int cellIndex,
        string value)
    {
        var lines = report.Split('\n');
        var lineIndex = Array.FindIndex(
            lines,
            line => line.StartsWith($"| {identifier} |", StringComparison.Ordinal));
        Assert.True(lineIndex >= 0);
        var cells = ScorecardValidator.SplitMarkdownRow(lines[lineIndex]).ToArray();
        cells[cellIndex] = value;
        lines[lineIndex] = "| " + string.Join(" | ", cells) + " |";

        return string.Join('\n', lines);
    }

    private static string OppositeScope(string scope)
    {
        return scope switch
        {
            "repository-wide" => "component-specific",
            "component-specific" => "repository-wide",
            _ => throw new InvalidOperationException($"Unexpected scope '{scope}'."),
        };
    }

    private static void AssertCoreIdSubstitutionRejected(string replacement)
    {
        using var skill = CopySkill();
        var layout = SkillLayout.Create(skill.DirectoryPath);
        var checklist = File.ReadAllText(layout.ChecklistPath, Encoding.UTF8)
            .Replace(
                "- **TA-08** (`repository-wide`)",
                $"- **{replacement}** (`repository-wide`)",
                StringComparison.Ordinal);
        File.WriteAllText(layout.ChecklistPath, checklist, new UTF8Encoding(false));
        var reportPath = Path.Combine(skill.DirectoryPath, "report.md");
        File.WriteAllText(reportPath, "not a valid report", new UTF8Encoding(false));

        var templateError = new StringWriter(CultureInfo.InvariantCulture);
        var templateExitCode = ScorecardCommand.Run(
            ["--skill-dir", skill.DirectoryPath, "--emit-template"],
            TextWriter.Null,
            templateError);
        var validationError = new StringWriter(CultureInfo.InvariantCulture);
        var validationExitCode = ScorecardCommand.Run(
            ["--skill-dir", skill.DirectoryPath, reportPath],
            TextWriter.Null,
            validationError);
        var skillError = new StringWriter(CultureInfo.InvariantCulture);
        var skillExitCode = SkillValidationCommand.Run(
            ["--skill-dir", skill.DirectoryPath],
            TextWriter.Null,
            skillError);

        Assert.Equal(1, templateExitCode);
        Assert.Equal(1, validationExitCode);
        Assert.Equal(1, skillExitCode);
        foreach (var output in new[]
        {
            templateError.ToString(),
            validationError.ToString(),
            skillError.ToString(),
        })
        {
            Assert.Contains("Core requirement IDs differ from the canonical schema", output);
            Assert.Contains("TA-08", output);
            Assert.Contains(replacement, output);
        }
    }

    private static string CompleteTemplate(IReadOnlyList<Requirement> requirements)
    {
        return ScorecardValidator.RenderTemplate(requirements)
            .Replace("[scope]", "component-specific", StringComparison.Ordinal)
            .Replace("[status]", "not applicable", StringComparison.Ordinal)
            .Replace(
                "[evidence]",
                "This requirement does not apply to the bounded component.",
                StringComparison.Ordinal)
            .Replace("[maintainer action]", "-", StringComparison.Ordinal)
            .Replace("[reviewer follow-up]", "-", StringComparison.Ordinal);
    }

    private static string WriteTargetedReport(
        string directoryPath,
        string fileName = "report.md")
    {
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var reportPath = Path.Combine(directoryPath, fileName);
        File.WriteAllText(
            reportPath,
            ScorecardValidator.RenderTemplate(requirements)
                .Replace("[scope]", "component-specific", StringComparison.Ordinal)
                .Replace("[status]", "not applicable", StringComparison.Ordinal)
                .Replace(
                    "[evidence]",
                    "This requirement does not apply to the bounded component.",
                    StringComparison.Ordinal)
                .Replace("[maintainer action]", "-", StringComparison.Ordinal)
                .Replace("[reviewer follow-up]", "-", StringComparison.Ordinal),
            new UTF8Encoding(false));

        return reportPath;
    }

    private static string[] ScorecardArguments(string reportPath, string receiptPath)
    {
        return
        [
            "--skill-dir",
            Layout.Root,
            "--ids",
            "LP-01,LP-02",
            "--legacy-evidence",
            reportPath,
            "--receipt",
            receiptPath,
        ];
    }

    private static TemporarySkillCopy CopySkill()
    {
        return new TemporarySkillCopy();
    }

    private sealed class TemporarySkillCopy : IDisposable
    {
        private readonly TemporaryDirectory _repository = new();

        internal TemporarySkillCopy()
        {
            DirectoryPath = Path.Combine(
                _repository.DirectoryPath,
                ".github",
                "skills",
                "blazor-component-readiness");
            CopyDirectory(Layout.Root, DirectoryPath);
            CopyDirectory(
                Layout.EvalRoot,
                Path.Combine(
                    _repository.DirectoryPath,
                    "eng",
                    "skill-evals",
                    "blazor-component-readiness"));
        }

        internal string DirectoryPath { get; }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);
        foreach (var file in Directory.EnumerateFiles(sourcePath))
        {
            File.Copy(file, Path.Combine(destinationPath, Path.GetFileName(file)));
        }

        foreach (var directory in Directory.EnumerateDirectories(sourcePath))
        {
            CopyDirectory(
                directory,
                Path.Combine(destinationPath, Path.GetFileName(directory)));
        }
    }

    private static string ReplaceFirst(string content, string oldValue, string newValue)
    {
        var index = content.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Expected to find '{oldValue}'.");

        return string.Concat(
            content.AsSpan(0, index),
            newValue,
            content.AsSpan(index + oldValue.Length));
    }

    private static void ConvertToCrLf(string path)
    {
        var content = File.ReadAllText(path, Encoding.UTF8)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", "\r\n", StringComparison.Ordinal);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static void MakeDirectoryNonWritable(string path)
    {
        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead |
            UnixFileMode.UserExecute |
            UnixFileMode.GroupRead |
            UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead |
            UnixFileMode.OtherExecute);
    }

    private static void SkipIfDirectoryRemainsWritable(string path)
    {
        var probePath = Path.Combine(path, $"write-probe-{Guid.NewGuid():N}");
        try
        {
            using var stream = new FileStream(probePath, FileMode.CreateNew);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        File.Delete(probePath);
        throw Xunit.Sdk.SkipException.ForSkip(
            "The current user can write through non-writable Unix mode bits.");
    }

    private static string? FindWritableMountedVolumeRoot()
    {
        var defaultRoot = Path.GetPathRoot(Path.GetTempPath());
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady ||
                string.Equals(
                    Path.TrimEndingDirectorySeparator(drive.RootDirectory.FullName),
                    Path.TrimEndingDirectorySeparator(defaultRoot!),
                    StringComparison.Ordinal))
            {
                continue;
            }

            var probePath = Path.Combine(
                drive.RootDirectory.FullName,
                $".receipt-write-probe-{Guid.NewGuid():N}");
            var created = false;
            try
            {
                using var stream = new FileStream(probePath, FileMode.CreateNew);
                created = true;
                return drive.RootDirectory.FullName;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
            }
            finally
            {
                if (created)
                {
                    File.Delete(probePath);
                }
            }
        }

        return null;
    }

    private static bool CanCreateDirectorySymbolicLink()
    {
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.DirectoryPath, "target");
        var linkPath = Path.Combine(directory.DirectoryPath, "link");
        Directory.CreateDirectory(targetPath);

        return TryCreateDirectorySymbolicLink(linkPath, targetPath);
    }

    private static bool CanRenameDirectoryContainingOpenFile()
    {
        using var directory = new TemporaryDirectory();
        var sourcePath = Path.Combine(directory.DirectoryPath, "source");
        var destinationPath = Path.Combine(directory.DirectoryPath, "destination");
        Directory.CreateDirectory(sourcePath);
        using var stream = new FileStream(
            Path.Combine(sourcePath, "open.tmp"),
            new FileStreamOptions
            {
                Access = FileAccess.ReadWrite,
                Mode = FileMode.CreateNew,
                Share = FileShare.Read | FileShare.Delete,
            });
        try
        {
            Directory.Move(sourcePath, destinationPath);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class DirectoryRenameWithOpenFileSupportedAttribute :
        Attribute,
        ITestCondition
    {
        public bool IsMet => SupportsDirectoryRenameWithOpenFile;

        public string SkipReason =>
            "The filesystem cannot rename a directory containing an open file.";
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class DirectorySymbolicLinksSupportedAttribute :
        Attribute,
        ITestCondition
    {
        public bool IsMet => SupportsDirectorySymbolicLinks;

        public string SkipReason =>
            "The test environment cannot create directory symbolic links.";
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class WritableMountedVolumeRootAttribute : Attribute, ITestCondition
    {
        public bool IsMet => HasWritableMountedVolumeRoot;

        public string SkipReason =>
            "No writable non-default mounted volume root is available.";
    }

    private static bool TryCreateSymbolicLink(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static bool TryCreateHardLink(string linkPath, string targetPath)
    {
        try
        {
            File.CreateHardLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static bool TryCreateDirectorySymbolicLink(
        string linkPath,
        string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            PlatformNotSupportedException)
        {
            return false;
        }
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

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"blazor-component-readiness-{Guid.NewGuid():N}");
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
