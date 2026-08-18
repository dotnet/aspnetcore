// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BlazorComponentReadiness.Tests;

public sealed class EvidenceIntegrationTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly SkillLayout Layout = SkillLayout.Create(Path.Combine(
        RepositoryRoot,
        ".github",
        "skills",
        "blazor-component-readiness"));
    private static readonly string FixtureRoot = Path.Combine(
        RepositoryRoot,
        "eng",
        "tools",
        "BlazorComponentReadiness.Tests",
        "Fixtures",
        "EvidenceKnownAnswers");

    [Theory]
    [InlineData("--skill-dir")]
    [InlineData("--report")]
    [InlineData("--evidence-bundle")]
    [InlineData("--producer-validator")]
    [InlineData("--shared-row-projection")]
    public void ReceiptMissingOptionValuesReturnOneWithoutThrowing(string option)
    {
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ReceiptCommand.Run(
            ["validate", option],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("ERROR:", error.ToString());
        Assert.Single(
            error.ToString().Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void ReceiptRequiresExplicitHistoricalSkillDirectoryInBothModes()
    {
        foreach (var args in new[]
        {
            new[]
            {
                "validate", "--evidence-bundle", "evidence.json",
                "--report", "report.md", "receipt.json",
            },
            new[]
            {
                "validate", "--legacy-evidence",
                "--report", "report.md", "receipt.json",
            },
        })
        {
            var error = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = ReceiptCommand.Run(
                args,
                TextWriter.Null,
                error);

            Assert.Equal(1, exitCode);
            Assert.Contains(
                "requires non-empty --skill-dir",
                error.ToString());
        }
    }

    [Fact]
    public void StableScorecardReceiptRoundTripsAndVerifiesProducerBytes()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,BEQ-01",
                "--evidence-bundle",
                bundlePath,
                reportPath,
                "--receipt",
                receiptPath,
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.True(File.Exists(receiptPath));
        using (var receipt = JsonDocument.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8)))
        {
            Assert.Equal(3, receipt.RootElement
                .GetProperty("schema_version")
                .GetInt32());
            Assert.Equal(
                Path.GetFileName(bundlePath),
                receipt.RootElement
                    .GetProperty("evidence_bundle_filename")
                    .GetString());
        }

        output.GetStringBuilder().Clear();
        exitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                receiptPath,
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            "Valid structural artifact bindings.\n" +
            "Producer-byte correspondence not checked; producer execution and " +
            "authenticity are not established.\n",
            output.ToString());

        output.GetStringBuilder().Clear();
        exitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                "--producer-validator",
                typeof(ScorecardCommand).Assembly.Location,
                receiptPath,
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains(
            "Supplied archived assembly bytes match",
            output.ToString());
        Assert.Contains(
            "producer execution and authenticity are not established",
            output.ToString());

        var mismatchedAssembly = Path.Combine(
            directory.DirectoryPath,
            "mismatched-validator.dll");
        File.WriteAllText(
            mismatchedAssembly,
            "not the validator",
            new UTF8Encoding(false));
        error.GetStringBuilder().Clear();
        var mismatchExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                "--producer-validator",
                mismatchedAssembly,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, mismatchExitCode);
        Assert.Contains("RECEIPT006", error.ToString());
    }

    [Fact]
    public void StableCompleteMultiOverlayReceiptRoundTripsInCanonicalOrder()
    {
        using var directory = new TemporaryDirectory();
        string[] reverseOverlayOrder = ["scaffolder", "ai-skill"];
        var (reportPath, bundlePath) = WriteStableCompleteArtifacts(
            directory.DirectoryPath,
            ["ai-skill", "scaffolder"]);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var template = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Equal(
            0,
            ScorecardCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--overlay",
                    reverseOverlayOrder[0],
                    "--overlay",
                    reverseOverlayOrder[1],
                    "--emit-template",
                ],
                template,
                error));
        Assert.True(
            template.ToString().IndexOf("| AI-01 |", StringComparison.Ordinal) <
            template.ToString().IndexOf("| SCF-01 |", StringComparison.Ordinal));

        var createExitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--overlay",
                reverseOverlayOrder[0],
                "--overlay",
                reverseOverlayOrder[1],
                "--evidence-bundle",
                bundlePath,
                reportPath,
                "--receipt",
                receiptPath,
            ],
            TextWriter.Null,
            error);
        var validateExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(0, createExitCode);
        Assert.Equal(0, validateExitCode);
        Assert.Empty(error.ToString());
        using var receipt = JsonDocument.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8));
        Assert.Equal(
            ["ai-skill", "scaffolder"],
            receipt.RootElement
                .GetProperty("selected_overlays")
                .EnumerateArray()
                .Select(value => value.GetString()));
    }

    [Fact]
    public void Schema3ReceiptRequiresValidationInputFilesArray()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        Assert.Equal(0, RunStableScorecard(reportPath, bundlePath, receiptPath));
        var root = JsonNode.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8))!.AsObject();
        root["validation_inputs"]!["files"] = "not-an-array";
        File.WriteAllText(
            receiptPath,
            root.ToJsonString(
                new JsonSerializerOptions { WriteIndented = true }) + "\n",
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "RECEIPT003: validation_inputs.files must be an array.",
            error.ToString());
    }

    [Fact]
    public void StableReceiptDetectsReportBundleAndHistoricalInputMutation()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        Assert.Equal(
            0,
            RunStableScorecard(reportPath, bundlePath, receiptPath));

        var originalReport = File.ReadAllText(reportPath, Encoding.UTF8);
        File.WriteAllText(
            reportPath,
            originalReport + "\nmutation",
            new UTF8Encoding(false));
        var reportError = ValidateStableReceipt(
            reportPath,
            bundlePath,
            receiptPath,
            Layout.Root);
        Assert.Contains("report digest mismatch", reportError);
        File.WriteAllText(
            reportPath,
            originalReport,
            new UTF8Encoding(false));

        var originalBundle = File.ReadAllBytes(bundlePath);
        var mutatedBundle = originalBundle.ToArray();
        mutatedBundle[^1] ^= 1;
        File.WriteAllBytes(bundlePath, mutatedBundle);
        var bundleError = ValidateStableReceipt(
            reportPath,
            bundlePath,
            receiptPath,
            Layout.Root);
        Assert.Contains("evidence", bundleError, StringComparison.OrdinalIgnoreCase);
        File.WriteAllBytes(bundlePath, originalBundle);

        using var skill = CopySkill();
        var copiedLayout = SkillLayout.Create(skill.DirectoryPath);
        File.AppendAllText(
            copiedLayout.ChecklistPath,
            "\nmutation",
            new UTF8Encoding(false));
        var skillError = ValidateStableReceipt(
            reportPath,
            bundlePath,
            receiptPath,
            skill.DirectoryPath);
        Assert.Contains("references/checklist.md", skillError);
        Assert.Contains("changed", skillError);
    }

    [Theory]
    [InlineData(
        "Structural validation does not establish factual evidence or classification quality. Extra claim.")]
    [InlineData(
        "Structural validation establishes factual evidence and classification quality.")]
    public void ReceiptRejectsAnyAlteredOrAppendedLimitation(string limitation)
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        Assert.Equal(0, RunStableScorecard(reportPath, bundlePath, receiptPath));
        var root = JsonNode.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8))!.AsObject();
        root["limitation"] = limitation;
        File.WriteAllText(
            receiptPath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n",
            new UTF8Encoding(false));
        var error = ValidateStableReceipt(
            reportPath,
            bundlePath,
            receiptPath,
            Layout.Root);

        Assert.Contains("receipt limitation mismatch", error);
    }

    [Fact]
    public void StableReceiptRequiresColocatedArtifactsButLocalValidationDoesNot()
    {
        using var directory = new TemporaryDirectory();
        var reportDirectory = Path.Combine(directory.DirectoryPath, "report");
        var bundleDirectory = Path.Combine(directory.DirectoryPath, "bundle");
        Directory.CreateDirectory(reportDirectory);
        Directory.CreateDirectory(bundleDirectory);
        var (reportPath, originalBundlePath) = WriteStableTargetedArtifacts(
            reportDirectory);
        var bundlePath = Path.Combine(bundleDirectory, "evidence.json");
        File.Move(originalBundlePath, bundlePath);
        var receiptPath = Path.Combine(reportDirectory, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var localExitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,BEQ-01",
                "--evidence-bundle",
                bundlePath,
                reportPath,
            ],
            TextWriter.Null,
            error);
        var publishExitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,BEQ-01",
                "--evidence-bundle",
                bundlePath,
                reportPath,
                "--receipt",
                receiptPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(0, localExitCode);
        Assert.Equal(1, publishExitCode);
        Assert.Contains("share one resolved artifact directory", error.ToString());
        Assert.False(File.Exists(receiptPath));
    }

    [Fact]
    public void ResolvedLeafSymlinkEscapeFailsStableGenerationAndValidation()
    {
        using var directory = new TemporaryDirectory();
        var artifacts = Path.Combine(directory.DirectoryPath, "artifacts");
        var outside = Path.Combine(directory.DirectoryPath, "outside");
        Directory.CreateDirectory(artifacts);
        Directory.CreateDirectory(outside);
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(artifacts);
        var outsideBundle = Path.Combine(outside, "evidence.json");
        File.Move(bundlePath, outsideBundle);
        if (!TryCreateSymbolicLink(bundlePath, outsideBundle))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test environment cannot create file symbolic links.");
        }

        var receiptPath = Path.Combine(artifacts, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);
        var generationExitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath,
            error);
        Assert.Equal(1, generationExitCode);
        Assert.Contains("RECEIPT004", error.ToString());
        Assert.False(File.Exists(receiptPath));

        File.Delete(bundlePath);
        File.Copy(outsideBundle, bundlePath);
        Assert.Equal(0, RunStableScorecard(reportPath, bundlePath, receiptPath));
        File.Delete(bundlePath);
        Assert.True(TryCreateSymbolicLink(bundlePath, outsideBundle));
        error.GetStringBuilder().Clear();
        var validationExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, validationExitCode);
        Assert.Contains("RECEIPT004", error.ToString());
    }

    [Fact]
    public void LegacyGenerationAndValidationRequireResolvedColocation()
    {
        using var directory = new TemporaryDirectory();
        var reports = Path.Combine(directory.DirectoryPath, "reports");
        var outside = Path.Combine(directory.DirectoryPath, "outside");
        Directory.CreateDirectory(reports);
        Directory.CreateDirectory(outside);
        var reportPath = WriteLegacyTargetedReport(reports);
        var outsideReceipt = Path.Combine(outside, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);
        var generationExitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,LP-02",
                "--legacy-evidence",
                reportPath,
                "--receipt",
                outsideReceipt,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, generationExitCode);
        Assert.Contains("RECEIPT004", error.ToString());
        Assert.False(File.Exists(outsideReceipt));

        var receiptPath = Path.Combine(reports, "receipt.json");
        Assert.Equal(
            0,
            ScorecardCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--ids",
                    "LP-01,LP-02",
                    "--legacy-evidence",
                    reportPath,
                    "--receipt",
                    receiptPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
        File.Copy(receiptPath, outsideReceipt);
        error.GetStringBuilder().Clear();
        var validationExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--legacy-evidence",
                "--report",
                reportPath,
                outsideReceipt,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, validationExitCode);
        Assert.Contains("RECEIPT004", error.ToString());
    }

    [Fact]
    public void LeafSymlinkSwapBeforePublishLeavesNoReceipt()
    {
        using var directory = new TemporaryDirectory();
        var artifacts = Path.Combine(directory.DirectoryPath, "artifacts");
        var outside = Path.Combine(directory.DirectoryPath, "outside");
        Directory.CreateDirectory(artifacts);
        Directory.CreateDirectory(outside);
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(artifacts);
        var outsideBundle = Path.Combine(outside, "evidence.json");
        File.Copy(bundlePath, outsideBundle);
        var probeLink = Path.Combine(artifacts, "probe-link");
        if (!TryCreateSymbolicLink(probeLink, outsideBundle))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "The test environment cannot create file symbolic links.");
        }

        File.Delete(probeLink);
        var receiptPath = Path.Combine(artifacts, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath,
            error,
            beforeReceiptPublish: () =>
            {
                File.Delete(bundlePath);
                Assert.True(TryCreateSymbolicLink(bundlePath, outsideBundle));
            });

        Assert.Equal(1, exitCode);
        Assert.Contains("RECEIPT004", error.ToString());
        Assert.False(File.Exists(receiptPath));
    }

    [Fact]
    public void TargetedOverlayReceiptBindsOnlyChecklistAndSelectedOverlay()
    {
        using var directory = new TemporaryDirectory();
        var known = KnownBundle();
        var componentLedger = Assert.Single(
            known.SourceLedgers,
            source => source.Ledger.LedgerKind == "component").Ledger;
        var componentId = componentLedger.Records[0].StableId;
        var bundle = EvidenceLedgerBuilder.BuildBundle(
            known.Assessment,
            [componentLedger],
            [componentId]);
        var bundlePath = Path.Combine(directory.DirectoryPath, "evidence.json");
        File.WriteAllBytes(
            bundlePath,
            CanonicalEvidenceJson.SerializeBundle(bundle));
        var requirement = Assert.Single(
            ScorecardValidator.SelectRequirements(
                ScorecardValidator.LoadRequirementSet(Layout, ["scaffolder"]),
                "SCF-01"));
        var reportPath = Path.Combine(directory.DirectoryPath, "report.md");
        File.WriteAllText(
            reportPath,
            StableEvidenceValidator.RenderAssessmentBlock(bundle.Assessment) +
            "\n\n| Requirement ID | Requirement | Requirement scope | Status | " +
            "Evidence | Maintainer action | Reviewer follow-up |\n" +
            "|---|---|---|---|---|---|---|\n" +
            $"| {requirement.Identifier} | {requirement.Text} | component-specific | " +
            $"verified | [{componentId}] | - | - |\n\n## Evidence ledger\n\n" +
            StableEvidenceValidator.RenderProjection(bundle) +
            "\n",
            new UTF8Encoding(false));
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "SCF-01",
                "--evidence-bundle",
                bundlePath,
                reportPath,
                "--receipt",
                receiptPath,
            ],
            TextWriter.Null,
            error);
        exitCode |= ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        using var receipt = JsonDocument.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8));
        Assert.Equal(
            ["scaffolder"],
            receipt.RootElement
                .GetProperty("selected_overlays")
                .EnumerateArray()
                .Select(value => value.GetString()));
        Assert.Equal(
            [
                "references/checklist.md",
                "references/overlays/scaffolder.md",
            ],
            receipt.RootElement
                .GetProperty("validation_inputs")
                .GetProperty("files")
                .EnumerateArray()
                .Select(file => file.GetProperty("path").GetString()));

        var root = JsonNode.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8))!.AsObject();
        root["selected_overlays"] = new JsonArray("ai-skill", "scaffolder");
        var aiSkillBytes = File.ReadAllBytes(Layout.OverlayPaths["ai-skill"]);
        var manifest = new ValidationInputManifest(
            1,
            [
                new ValidationInput(
                    "references/checklist.md",
                    new Sha256Digest(
                        "sha256",
                        ScorecardValidator.LoadCoreRubric(
                            Layout.ChecklistPath).Sha256)),
                new ValidationInput(
                    "references/overlays/ai-skill.md",
                    new Sha256Digest(
                        "sha256",
                        CanonicalEvidenceJson.ComputeSha256(aiSkillBytes))),
                new ValidationInput(
                    "references/overlays/scaffolder.md",
                    new Sha256Digest(
                        "sha256",
                        CanonicalEvidenceJson.ComputeSha256(
                            File.ReadAllBytes(
                                Layout.OverlayPaths["scaffolder"])))),
            ]);
        root["validation_inputs"] = JsonNode.Parse(
            Encoding.UTF8.GetString(
                CanonicalEvidenceJson.SerializeValidationInputManifest(
                    manifest)));
        root["validation_inputs_sha256"] =
            CanonicalEvidenceJson.ComputeValidationInputsSha256(manifest);
        File.WriteAllText(
            receiptPath,
            root.ToJsonString(
                new JsonSerializerOptions { WriteIndented = true }) + "\n",
            new UTF8Encoding(false));
        error.GetStringBuilder().Clear();
        var forgedExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, forgedExitCode);
        Assert.Contains(
            "selected overlays represented by scorecard rows mismatch",
            error.ToString());
    }

    [Fact]
    public void StableReceiptCannotOverwriteReportOrEvidenceBundle()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);

        foreach (var receiptPath in new[] { reportPath, bundlePath })
        {
            var error = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath,
                error);

            Assert.Equal(1, exitCode);
            Assert.Contains(
                receiptPath == reportPath
                    ? "--receipt must not overwrite the report"
                    : "distinct artifacts",
                error.ToString());
        }

        Assert.Contains(
            "bcr-assessment-v1",
            File.ReadAllText(reportPath, Encoding.UTF8));
        Assert.Equal(
            CanonicalEvidenceJson.ComputeBundleSha256(KnownBundle()),
            CanonicalEvidenceJson.ComputeBundleSha256(
                CanonicalEvidenceJson.ParseBundle(File.ReadAllBytes(bundlePath))));
    }

    [Fact]
    public void LegacySchema2ReceiptRoundTripsWithLimitedProvenance()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteLegacyTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
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
                receiptPath,
            ],
            TextWriter.Null,
            error);
        var output = new StringWriter(CultureInfo.InvariantCulture);
        exitCode |= ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--legacy-evidence",
                "--report",
                reportPath,
                receiptPath,
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.Equal(
            "Legacy schema-2 structural revalidation passed against the supplied " +
            "skill inputs; exact historical overlay/input provenance is not established.\n",
            output.ToString());
        using var receipt = JsonDocument.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8));
        Assert.Equal(2, receipt.RootElement
            .GetProperty("schema_version")
            .GetInt32());
        Assert.False(receipt.RootElement.TryGetProperty(
            "validation_inputs",
            out _));

        var legacyReceipt = File.ReadAllText(receiptPath, Encoding.UTF8);
        var receiptNode = JsonNode.Parse(legacyReceipt)!.AsObject();
        receiptNode["limitation"] =
            "Structural validation does not establish factual evidence or " +
            "classification quality. Extra claim.";
        File.WriteAllText(
            receiptPath,
            receiptNode.ToJsonString(
                new JsonSerializerOptions { WriteIndented = true }) + "\n",
            new UTF8Encoding(false));
        error.GetStringBuilder().Clear();
        var limitationExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--legacy-evidence",
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, limitationExitCode);
        Assert.Contains("receipt limitation mismatch", error.ToString());
        File.WriteAllText(
            receiptPath,
            legacyReceipt,
            new UTF8Encoding(false));

        var forgedReceipt = JsonNode.Parse(legacyReceipt)!.AsObject();
        forgedReceipt["selected_overlays"] = new JsonArray("scaffolder");
        File.WriteAllText(
            receiptPath,
            forgedReceipt.ToJsonString(
                new JsonSerializerOptions { WriteIndented = true }) + "\n",
            new UTF8Encoding(false));
        error.GetStringBuilder().Clear();
        var forgedOverlayExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--legacy-evidence",
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, forgedOverlayExitCode);
        Assert.Contains("RECEIPT007", error.ToString());
        Assert.Contains(
            "targeted legacy selected overlays mismatch",
            error.ToString());
        File.WriteAllText(
            receiptPath,
            legacyReceipt,
            new UTF8Encoding(false));

        var legacyReport = File.ReadAllText(reportPath, Encoding.UTF8);
        File.WriteAllText(
            reportPath,
            legacyReport + "\n[EV1-" + new string('0', 64) + "]",
            new UTF8Encoding(false));
        error.GetStringBuilder().Clear();
        var stableAnchorExitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,LP-02",
                "--legacy-evidence",
                reportPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, stableAnchorExitCode);
        Assert.Contains("EVID011", error.ToString());
        File.WriteAllText(
            reportPath,
            legacyReport,
            new UTF8Encoding(false));

        using var skill = CopySkill();
        var copiedLayout = SkillLayout.Create(skill.DirectoryPath);
        File.AppendAllText(
            copiedLayout.ChecklistPath,
            "\nmutation",
            new UTF8Encoding(false));
        error.GetStringBuilder().Clear();
        var mismatchExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                skill.DirectoryPath,
                "--legacy-evidence",
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, mismatchExitCode);
        Assert.Contains("checklist digest mismatch", error.ToString());
    }

    [Fact]
    public void LegacySchema2ReceiptBytesRemainExact()
    {
        var rubric = ScorecardValidator.LoadCoreRubric(Layout.ChecklistPath);
        var requirements = rubric.Requirements.Take(2).ToArray();
        var rows = requirements.Select((requirement, index) => new ScorecardRow(
            requirement.Identifier,
            requirement.Text,
            requirement.Scope!,
            "verified",
            "[E-001]",
            "-",
            "-",
            index + 1)).ToArray();
        var report = new ReportSnapshot(
            "legacy-report.md",
            "legacy report",
            Encoding.UTF8.GetBytes("legacy report"));
        var receipt = ScorecardValidator.BuildValidationReceipt(
            rubric,
            report,
            "targeted",
            requirements,
            rows,
            [],
            new DateTimeOffset(2026, 8, 13, 18, 0, 0, TimeSpan.Zero));
        var bytes = JsonSerializer.Serialize(
            receipt,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            }) + "\n";

        Assert.Equal(
            File.ReadAllText(
                Path.Combine(FixtureRoot, "legacy-schema2-receipt.json"),
                Encoding.UTF8),
            bytes);
    }

    [Fact]
    public void LegacyTargetedOverlayKeepsHistoricalEmptySelectedOverlays()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteLegacyTargetedOverlayReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "SCF-01",
                "--legacy-evidence",
                reportPath,
                "--receipt",
                receiptPath,
            ],
            TextWriter.Null,
            error);
        exitCode |= ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--legacy-evidence",
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        using var receipt = JsonDocument.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8));
        Assert.Equal("targeted", receipt.RootElement.GetProperty("mode").GetString());
        Assert.Equal(
            ["SCF-01"],
            receipt.RootElement
                .GetProperty("selected_ids")
                .EnumerateArray()
                .Select(value => value.GetString()));
        // Base 8df7e458 passes the CLI overlays list unchanged. Targeted mode cannot
        // combine --ids with --overlay, so the historical literal is exactly [].
        Assert.Equal(
            "[]",
            receipt.RootElement
                .GetProperty("selected_overlays")
                .GetRawText());
        Assert.Empty(receipt.RootElement
            .GetProperty("selected_overlays")
            .EnumerateArray());
    }

    [Fact]
    public void LegacyCompleteReceiptPreservesCliOverlayOrderAndRevalidatesAsSet()
    {
        using var directory = new TemporaryDirectory();
        string[] overlayOrder = ["scaffolder", "ai-skill"];
        var reportPath = WriteLegacyCompleteReport(
            directory.DirectoryPath,
            overlayOrder);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--overlay",
                "scaffolder",
                "--overlay",
                "ai-skill",
                "--legacy-evidence",
                reportPath,
                "--receipt",
                receiptPath,
            ],
            TextWriter.Null,
            error);
        exitCode |= ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--legacy-evidence",
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        using var receipt = JsonDocument.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8));
        Assert.Equal(
            overlayOrder,
            receipt.RootElement
                .GetProperty("selected_overlays")
                .EnumerateArray()
                .Select(value => value.GetString()));
    }

    [Fact]
    public void LegacyReceiptFailuresUseReceipt007WithPreciseContext()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteLegacyTargetedReport(directory.DirectoryPath);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        Assert.Equal(
            0,
            ScorecardCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--ids",
                    "LP-01,LP-02",
                    "--legacy-evidence",
                    reportPath,
                    "--receipt",
                    receiptPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
        var originalReceipt = File.ReadAllText(receiptPath, Encoding.UTF8);
        var originalReport = File.ReadAllText(reportPath, Encoding.UTF8);
        var mutations = new Action<JsonObject>[]
        {
            root => root["report_sha256"] = new string('0', 64),
            root => root["checklist_sha256"] = new string('0', 64),
            root => root["selected_ids"] = new JsonArray("LP-01"),
            root => root["selected_overlays"] = new JsonArray("scaffolder"),
            root => root["valid_row_count"] = 99,
            root => root["mode"] = "complete",
        };

        foreach (var mutate in mutations)
        {
            var root = JsonNode.Parse(originalReceipt)!.AsObject();
            mutate(root);
            File.WriteAllText(
                receiptPath,
                root.ToJsonString(
                    new JsonSerializerOptions { WriteIndented = true }) + "\n",
                new UTF8Encoding(false));
            var error = ValidateLegacyReceipt(
                reportPath,
                receiptPath,
                Layout.Root);
            Assert.StartsWith("ERROR: RECEIPT007:", error);
        }

        File.WriteAllText(
            reportPath,
            originalReport.Replace("[E-001]", "[E-999]", StringComparison.Ordinal),
            new UTF8Encoding(false));
        var structuralReceipt = JsonNode.Parse(originalReceipt)!.AsObject();
        structuralReceipt["report_sha256"] =
            CanonicalEvidenceJson.ComputeSha256(
                File.ReadAllBytes(reportPath));
        File.WriteAllText(
            receiptPath,
            structuralReceipt.ToJsonString(
                new JsonSerializerOptions { WriteIndented = true }) + "\n",
            new UTF8Encoding(false));
        var structuralError = ValidateLegacyReceipt(
            reportPath,
            receiptPath,
            Layout.Root);
        Assert.StartsWith("ERROR: RECEIPT007:", structuralError);
        Assert.Contains("structural revalidation failed", structuralError);
    }

    [Fact]
    public void EvidenceModesAreExplicitAndCannotBeCombined()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var omitted = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,BEQ-01",
                reportPath,
            ],
            TextWriter.Null,
            error);
        var combined = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,BEQ-01",
                "--legacy-evidence",
                "--evidence-bundle",
                bundlePath,
                reportPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(1, omitted);
        Assert.Equal(1, combined);
        Assert.Equal(
            2,
            error.ToString().Split("MODE001", StringSplitOptions.None).Length - 1);
        Assert.Contains(
            "Existing callers must add --legacy-evidence",
            error.ToString());
    }

    [Fact]
    public void StableScorecardRejectsAnchorlessAndRewrittenProjection()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var original = File.ReadAllText(reportPath, Encoding.UTF8);
        var anchorless = original.Replace(
            "[EV1-",
            "[NOPE-",
            StringComparison.Ordinal);
        File.WriteAllText(reportPath, anchorless, new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var anchorlessExitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath: null,
            error);
        File.WriteAllText(
            reportPath,
            original.Replace(
                "Repository license is MIT.",
                "Repository license might be MIT.",
                StringComparison.Ordinal),
            new UTF8Encoding(false));
        var rewrittenExitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath: null,
            error);
        File.WriteAllText(
            reportPath,
            original + "\n[E-001]",
            new UTF8Encoding(false));
        var legacyAnchorExitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath: null,
            error);

        Assert.Equal(1, anchorlessExitCode);
        Assert.Equal(1, rewrittenExitCode);
        Assert.Equal(1, legacyAnchorExitCode);
        Assert.Contains("requires at least one full selected EV1", error.ToString());
        Assert.Contains("selected-evidence projection", error.ToString());
        Assert.Contains("EVID011", error.ToString());
    }

    [Theory]
    [InlineData("[EV1-deadbeef]")]
    [InlineData("[EV1-AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA]")]
    [InlineData("[EV1-00000000000000000000000000000000000000000000000000000000000000000]")]
    public void StableScorecardRejectsMalformedTokenBesideValidAnchor(
        string malformed)
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var bundle = KnownBundle();
        var valid = $"[{bundle.Selection[0].EvidenceId}]";
        var report = File.ReadAllText(reportPath, Encoding.UTF8);
        File.WriteAllText(
            reportPath,
            ReplaceFirst(report, valid, valid + " " + malformed),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath: null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("malformed stable evidence token", error.ToString());
    }

    [Fact]
    public void AssessmentFenceMustBeOneExactLineDelimitedBlock()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var original = File.ReadAllText(reportPath, Encoding.UTF8);
        var block = StableEvidenceValidator.RenderAssessmentBlock(
            KnownBundle().Assessment);
        var mutations = new[]
        {
            original + "\n```bcr-assessment-v1x\n{}\n```",
            original.Replace(
                "```bcr-assessment-v1",
                "prefix ```bcr-assessment-v1",
                StringComparison.Ordinal),
            original.Replace(
                "```bcr-assessment-v1\n",
                "```bcr-assessment-v1\r\n",
                StringComparison.Ordinal),
            ReplaceFirst(original, "\n```\n", "\n``` \n"),
            original + "\n" + block,
        };

        foreach (var mutation in mutations)
        {
            File.WriteAllText(
                reportPath,
                mutation,
                new UTF8Encoding(false));
            var error = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                error);
            Assert.Equal(1, exitCode);
            Assert.Contains("bcr-assessment-v1", error.ToString());
        }
    }

    [Fact]
    public void AssessmentPayloadMayContainReservedMarkerAndBackticks()
    {
        using var directory = new TemporaryDirectory();
        const string ComponentId = "Tree```bcr-assessment-v1";
        var assessment = KnownAssessment() with
        {
            ComponentId = ComponentId,
        };
        var componentLedger = EvidenceLedgerBuilder.BuildComponentLedger(
            assessment,
            [
                new EvidenceRecordDraft(
                    "Component interaction was verified.",
                    new EvidenceApplicability("component-specific", ComponentId),
                    new EvidenceProvenance(
                        "command-probe",
                        "probe: component interaction",
                        "Run deterministic component probe.",
                        "2026-08-16T20:03:00Z",
                        new Sha256Digest("sha256", new string('e', 64)),
                        "commitment-only"),
                    []),
            ]);
        var repositoryLedger = KnownRepositoryLedger();
        var bundle = EvidenceLedgerBuilder.BuildBundle(
            assessment,
            [repositoryLedger, componentLedger],
            [
                repositoryLedger.Records[0].StableId,
                repositoryLedger.Records[1].StableId,
                componentLedger.Records[0].StableId,
            ]);
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath,
            bundle);
        var trackerPath = Path.Combine(directory.DirectoryPath, "tracker.md");
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(bundle),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var scorecardExitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath: null,
            error);
        var trackerExitCode = TrackerCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                trackerPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(0, scorecardExitCode);
        Assert.Equal(0, trackerExitCode);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void ProjectionMustBeOneExactSelectedOnlyLineBlock()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var original = File.ReadAllText(reportPath, Encoding.UTF8);
        var forgedRow =
            "| 99 | EV1-" + new string('f', 64) +
            " | Forged. | repository-wide |  | repository | " +
            new string('a', 64) +
            " | repository-path | `FORGED` | 2026-08-16T20:00:00Z | " +
            new string('b', 64) + " |";
        var mutations = new[]
        {
            original.Replace(
                StableEvidenceValidator.ProjectionHeader,
                "prefix " + StableEvidenceValidator.ProjectionHeader,
                StringComparison.Ordinal),
            original.Replace(
                StableEvidenceValidator.ProjectionHeader,
                StableEvidenceValidator.ProjectionHeader + " suffix",
                StringComparison.Ordinal),
            original + "\n" + forgedRow,
            original + "\n" + StableEvidenceValidator.ProjectionHeader,
        };

        foreach (var mutation in mutations)
        {
            File.WriteAllText(
                reportPath,
                mutation,
                new UTF8Encoding(false));
            var error = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                error);
            Assert.Equal(1, exitCode);
            Assert.Contains("EVID012", error.ToString());
        }
    }

    [Fact]
    public void LedgerCommandsBuildValidateAndBundleExactNupkgIdentity()
    {
        using var directory = new TemporaryDirectory();
        var nupkgPath = Path.Combine(directory.DirectoryPath, "package.nupkg");
        var nupkg = Convert.FromBase64String(
            Encoding.ASCII.GetString(ReadFixture("nupkg.base64")));
        File.WriteAllBytes(nupkgPath, nupkg);
        PackageIdentity package;
        using (var stream = File.OpenRead(nupkgPath))
        {
            package = EvidenceIdentity.ReadPackageIdentity(stream);
        }

        var assessment = KnownAssessment() with
        {
            Artifact = new ArtifactIdentity("released-package", package),
        };
        var subject = new RepositoryLedgerSubject(
            assessment.Repository,
            assessment.Artifact,
            null);
        var subjectPath = Path.Combine(directory.DirectoryPath, "subject.json");
        File.WriteAllBytes(
            subjectPath,
            CanonicalEvidenceJson.SerializeRepositorySubject(subject));
        var draftPath = Path.Combine(directory.DirectoryPath, "draft.json");
        File.WriteAllText(
            draftPath,
            BuildDraftJson(),
            new UTF8Encoding(false));
        var ledgerPath = Path.Combine(directory.DirectoryPath, "ledger.json");
        var bundlePath = Path.Combine(directory.DirectoryPath, "bundle.json");
        var assessmentPath = Path.Combine(
            directory.DirectoryPath,
            "assessment.json");
        File.WriteAllBytes(
            assessmentPath,
            CanonicalEvidenceJson.SerializeAssessment(assessment));
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var buildExitCode = EvidenceLedgerCommand.Run(
            [
                "build",
                "--kind",
                "repository",
                "--subject",
                subjectPath,
                "--nupkg",
                nupkgPath,
                draftPath,
                "--output",
                ledgerPath,
            ],
            output,
            error);
        var validateExitCode = EvidenceLedgerCommand.Run(
            ["validate", ledgerPath],
            output,
            error);
        var ledger = CanonicalEvidenceJson.ParseSourceLedger(
            File.ReadAllBytes(ledgerPath));
        var bundleExitCode = EvidenceLedgerCommand.Run(
            [
                "bundle",
                "--assessment",
                assessmentPath,
                "--source-ledger",
                ledgerPath,
                "--ids",
                ledger.Records[0].StableId,
                "--output",
                bundlePath,
            ],
            output,
            error);

        Assert.Equal(0, buildExitCode);
        Assert.Equal(0, validateExitCode);
        Assert.Equal(0, bundleExitCode);
        Assert.Empty(error.ToString());
        Assert.Single(
            CanonicalEvidenceJson.ParseBundle(
                File.ReadAllBytes(bundlePath)).Selection);
    }

    [Fact]
    public void RoutedNupkgAcquisitionHasTotalInputBound()
    {
        using var directory = new TemporaryDirectory();
        var nupkgPath = Path.Combine(directory.DirectoryPath, "oversized.nupkg");
        using (var stream = new FileStream(
            nupkgPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            stream.SetLength(EvidenceIdentity.MaximumNupkgBytes + 1);
        }

        var subjectPath = Path.Combine(directory.DirectoryPath, "subject.json");
        File.WriteAllBytes(
            subjectPath,
            CanonicalEvidenceJson.SerializeRepositorySubject(
                KnownRepositoryLedger().RepositorySubject!));
        var draftPath = Path.Combine(directory.DirectoryPath, "draft.json");
        File.WriteAllText(
            draftPath,
            BuildDraftJson(),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);
        var exitCode = EvidenceLedgerCommand.Run(
            [
                "build",
                "--kind",
                "repository",
                "--subject",
                subjectPath,
                "--nupkg",
                nupkgPath,
                draftPath,
                "--output",
                Path.Combine(directory.DirectoryPath, "ledger.json"),
            ],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            $"exceeds {EvidenceIdentity.MaximumNupkgBytes} bytes",
            error.ToString());
    }

    [Fact]
    public void SerializedArtifactReaderRejectsGrowthAfterLengthPrecheck()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.DirectoryPath, "artifact.json");
        File.WriteAllBytes(path, new byte[8]);

        var exception = Assert.Throws<InvalidDataException>(() =>
            FileSystemUtilities.ReadAllBytesBounded(
                path,
                maximumBytes: 8,
                afterLengthRead: _ =>
                {
                    using var append = new FileStream(
                        path,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite | FileShare.Delete);
                    append.WriteByte(1);
                }));

        Assert.Contains("grew while it was read", exception.Message);
    }

    [Fact]
    public void PublicConsumersRejectOversizedSerializedArtifacts()
    {
        using var directory = new TemporaryDirectory();
        var (validReport, validBundle) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var oversized = Path.Combine(directory.DirectoryPath, "oversized.json");
        using (var stream = new FileStream(
            oversized,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            stream.SetLength(
                FileSystemUtilities.MaximumSerializedArtifactBytes + 1);
        }

        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var errors = new StringWriter(CultureInfo.InvariantCulture);
        var scorecardReportExit = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,BEQ-01",
                "--evidence-bundle",
                validBundle,
                oversized,
                "--receipt",
                receiptPath,
            ],
            TextWriter.Null,
            errors);
        var scorecardBundleExit = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,BEQ-01",
                "--evidence-bundle",
                oversized,
                validReport,
                "--receipt",
                receiptPath,
            ],
            TextWriter.Null,
            errors);
        var trackerExit = TrackerCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                validBundle,
                oversized,
            ],
            TextWriter.Null,
            errors);
        var receiptExit = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                validBundle,
                "--report",
                validReport,
                oversized,
            ],
            TextWriter.Null,
            errors);

        Assert.Equal(1, scorecardReportExit);
        Assert.Equal(1, scorecardBundleExit);
        Assert.Equal(1, trackerExit);
        Assert.Equal(1, receiptExit);
        Assert.Equal(
            4,
            errors.ToString()
                .Split(
                    "exceeds the 67108864-byte limit",
                    StringSplitOptions.None)
                .Length - 1);
        Assert.False(File.Exists(receiptPath));
    }

    [Fact]
    public void BundleProducerRejectsAggregateAboveConsumerCeiling()
    {
        var sourceLengths = Enumerable.Repeat(4L * 1024 * 1024, 17).ToArray();

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerCommand.ValidateBundleAggregateSize(
                assessmentLength: 1024,
                sourceLedgerLengths: sourceLengths,
                selectionCount: 1));

        Assert.Contains("aggregate inputs exceed", exception.Message);
    }

    [Fact]
    public void RepositoryAuthorityRequiresPublicDnsOrIdnHost()
    {
        foreach (var uri in new[]
        {
            "https://127.0.0.1/owner/repo",
            "https://[::1]/owner/repo",
            "https://localhost/owner/repo",
            "https://foo.localhost/owner/repo",
            "https://internalgit/owner/repo",
            "https://git.internal/owner/repo",
            "https://myhost.local/owner/repo",
            "https://foo_bar.com/owner/repo",
            "https://-foo.com/owner/repo",
            "https://foo-.com/owner/repo",
            "https://example.123/owner/repo",
        })
        {
            Assert.Contains(
                "EVID005",
                Assert.Throws<InvalidDataException>(() =>
                    EvidenceIdentity.NormalizeRepositorySubject(
                        KnownRepositoryLedger().RepositorySubject! with
                        {
                            Repository = new RepositoryIdentity(
                                uri,
                                new string('1', 40)),
                        })).Message);
        }
    }

    [Fact]
    public void PublicLocatorAuthorityUsesSameDnsLabelPolicy()
    {
        foreach (var host in new[]
        {
            "foo_bar.com",
            "-foo.com",
            "foo-.com",
            "example.123",
            "foo.localhost",
        })
        {
            var draft = RepositoryDraftWithPublicLocator(
                $"https://{host}/evidence");
            Assert.Contains(
                "EVID005",
                Assert.Throws<InvalidDataException>(() =>
                    EvidenceLedgerBuilder.BuildRepositoryLedger(
                        KnownRepositoryLedger().RepositorySubject!,
                        [draft])).Message);
        }
    }

    [Fact]
    public void LegitimateIdnHostsCanonicalizeToDnsAscii()
    {
        var subject = EvidenceIdentity.NormalizeRepositorySubject(
            KnownRepositoryLedger().RepositorySubject! with
            {
                Repository = new RepositoryIdentity(
                    "https://bücher.example.com/Owner/Repo",
                    new string('1', 40)),
            });
        var ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            KnownRepositoryLedger().RepositorySubject!,
            [RepositoryDraftWithPublicLocator(
                "https://bücher.example.com/evidence")]);

        Assert.Equal(
            "https://xn--bcher-kva.example.com/Owner/Repo",
            subject.Repository.RepositoryUri);
        Assert.Equal(
            "https://xn--bcher-kva.example.com/evidence",
            ledger.Records[0].Provenance.Locator);
    }

    [Fact]
    public void SharedProjectionBindsSourceActionsAndReceiptInput()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var bundle = KnownBundle();
        var projectionPath = WriteSharedRowProjectionFromReport(
            directory.DirectoryPath,
            reportPath,
            bundle);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");

        Assert.Equal(
            0,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath,
                sharedRowProjectionPath: projectionPath));
        var receiptError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                receiptError));
        Assert.Equal(
            0,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--shared-row-projection",
                    projectionPath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                TextWriter.Null));

        var root = JsonNode.Parse(
            File.ReadAllText(projectionPath, Encoding.UTF8))!.AsObject();
        var row = root["rows"]!.AsArray()[0]!.AsObject();
        row["maintainer_action"] = "Supply the missing release record.";
        File.WriteAllText(
            projectionPath,
            root.ToJsonString(),
            new UTF8Encoding(false));
        var actionError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                actionError,
                sharedRowProjectionPath: projectionPath));
        Assert.Contains(
            "maintainer action differs",
            actionError.ToString());

        row["maintainer_action"] = "-";
        row["notes"] = "Re-run the bounded release probe.";
        File.WriteAllText(
            projectionPath,
            root.ToJsonString(),
            new UTF8Encoding(false));
        var followUpError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                followUpError,
                sharedRowProjectionPath: projectionPath));
        Assert.Contains(
            "reviewer follow-up differs",
            followUpError.ToString());
    }

    [Fact]
    public void SharedProjectionPreservesLegacyShapeCompatibility()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var bundle = KnownBundle();
        var legacyRows = new JsonArray();
        var report = ScorecardValidator.ReadReportSnapshot(reportPath);
        foreach (var row in ScorecardValidator.ParseScorecard(report)
            .Where(row => row.Scope == "repository-wide"))
        {
            legacyRows.Add(new JsonObject
            {
                ["requirement_id"] = row.Identifier,
                ["requirement"] = row.Requirement,
                ["requirement_scope"] = row.Scope,
                ["status"] = row.Status,
                ["evidence_anchors"] = row.Evidence,
                ["maintainer_action"] = row.MaintainerAction,
                ["reviewer_follow_up"] = row.ReviewerFollowUp,
            });
        }

        var repository = Assert.Single(
            bundle.SourceLedgers,
            source => source.Ledger.LedgerKind == "repository");
        var legacyProjectionPath = Path.Combine(
            directory.DirectoryPath,
            "legacy-shared-row-projection.json");
        File.WriteAllText(
            legacyProjectionPath,
            new JsonObject
            {
                ["schema_version"] = 1,
                ["source_ledger_sha256"] = repository.SourceLedgerSha256,
                ["rows"] = legacyRows,
            }.ToJsonString(),
            new UTF8Encoding(false));

        Assert.Equal(
            0,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                sharedRowProjectionPath: legacyProjectionPath));

        var repositoryEvidence = string.Join(
            ' ',
            bundle.Selection
                .Where(selection =>
                    selection.SourceLedgerSha256 ==
                    repository.SourceLedgerSha256)
                .Select(selection => $"[{selection.EvidenceId}]"));
        repositoryEvidence =
            "The bcr-assessment-v1 projection was reviewed. " +
            "Display order and Evidence ID confirmed. " +
            repositoryEvidence;
        var completeLegacyRows = new JsonArray();
        foreach (var requirement in ScorecardValidator
            .LoadRequirementSet(Layout, [])
            .Where(requirement => requirement.Scope == "repository-wide"))
        {
            completeLegacyRows.Add(new JsonObject
            {
                ["requirement_id"] = requirement.Identifier,
                ["requirement"] = requirement.Text.Replace(
                    "|",
                    "\\|",
                    StringComparison.Ordinal),
                ["requirement_scope"] = requirement.Scope,
                ["status"] = "verified",
                ["evidence_anchors"] = repositoryEvidence,
                ["maintainer_action"] = "-",
                ["reviewer_follow_up"] = "-",
            });
        }

        File.WriteAllText(
            legacyProjectionPath,
            new JsonObject
            {
                ["schema_version"] = 1,
                ["source_ledger_sha256"] = repository.SourceLedgerSha256,
                ["rows"] = completeLegacyRows,
            }.ToJsonString(),
            new UTF8Encoding(false));
        var trackerPath = Path.Combine(
            directory.DirectoryPath,
            "tracker.md");
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(bundle),
            new UTF8Encoding(false));
        var trackerError = new StringWriter(CultureInfo.InvariantCulture);
        var trackerExitCode = TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--shared-row-projection",
                    legacyProjectionPath,
                    trackerPath,
                ],
                TextWriter.Null,
                trackerError);
        Assert.True(trackerExitCode == 0, trackerError.ToString());
    }

    [Fact]
    public void SharedProjectionRejectsContradictorySchemaDeclarations()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var projectionPath = WriteSharedRowProjectionFromReport(
            directory.DirectoryPath,
            reportPath,
            KnownBundle());
        var root = JsonNode.Parse(
            File.ReadAllText(projectionPath, Encoding.UTF8))!.AsObject();
        root["schema_version"] = 1;
        root["schema"] = "example-readiness/shared-row-projection/v2";
        File.WriteAllText(
            projectionPath,
            root.ToJsonString(),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                error,
                sharedRowProjectionPath: projectionPath));
        Assert.Contains("PROJ001", error.ToString());
    }

    [Fact]
    public void MalformedSharedProjectionReturnsStructuredScorecardError()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var projectionPath = Path.Combine(
            directory.DirectoryPath,
            "malformed-shared-row-projection.json");
        File.WriteAllText(
            projectionPath,
            "{",
            new UTF8Encoding(false));
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath,
            error,
            sharedRowProjectionPath: projectionPath);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "PROJ001: shared-row projection contains invalid JSON.",
            error.ToString());
        Assert.False(File.Exists(receiptPath));
    }

    [Fact]
    public void SharedProjectionRejectsDuplicateProperties()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var projectionPath = WriteSharedRowProjectionFromReport(
            directory.DirectoryPath,
            reportPath,
            KnownBundle());
        var projection = File.ReadAllText(projectionPath, Encoding.UTF8);
        projection = projection.Replace(
            "\"schema\":\"example-readiness/shared-row-projection/v1\"",
            "\"schema\":\"example-readiness/shared-row-projection/v2\"," +
            "\"schema\":\"example-readiness/shared-row-projection/v1\"",
            StringComparison.Ordinal);
        File.WriteAllText(
            projectionPath,
            projection,
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                error,
                sharedRowProjectionPath: projectionPath));
        Assert.Contains("repeats property 'schema'", error.ToString());
    }

    [Fact]
    public void SharedProjectionRejectsMissingRowsWithoutCrashing()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var projectionPath = WriteSharedRowProjectionFromReport(
            directory.DirectoryPath,
            reportPath,
            KnownBundle());
        var root = JsonNode.Parse(
            File.ReadAllText(projectionPath, Encoding.UTF8))!.AsObject();
        root.Remove("rows");
        File.WriteAllText(
            projectionPath,
            root.ToJsonString(),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                error,
                sharedRowProjectionPath: projectionPath));
        Assert.Contains("PROJ001", error.ToString());
    }

    [Fact]
    public void SharedProjectionRejectsDuplicateRepositoryRowsWithoutCrashing()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var projectionPath = WriteSharedRowProjectionFromReport(
            directory.DirectoryPath,
            reportPath,
            KnownBundle());
        var report = File.ReadAllText(reportPath, Encoding.UTF8);
        var duplicate = report
            .Split('\n')
            .Single(line => line.StartsWith("| LP-01 |", StringComparison.Ordinal));
        File.WriteAllText(
            reportPath,
            report.Replace(
                duplicate,
                duplicate + "\n" + duplicate,
                StringComparison.Ordinal),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath: null,
            error,
            sharedRowProjectionPath: projectionPath);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "PROJ004: source report repeats repository-wide requirement 'LP-01'.",
            error.ToString());
    }

    [Fact]
    public void SharedProjectionRejectsPartialTrackerImport()
    {
        using var directory = new TemporaryDirectory();
        var bundle = KnownBundle();
        var bundlePath = Path.Combine(directory.DirectoryPath, "evidence.json");
        File.WriteAllBytes(
            bundlePath,
            CanonicalEvidenceJson.SerializeBundle(bundle));
        var trackerPath = Path.Combine(directory.DirectoryPath, "tracker.md");
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(bundle),
            new UTF8Encoding(false));
        var projectionPath = WriteCompleteSharedRowProjection(
            directory.DirectoryPath,
            bundle);
        var projection = SharedRowProjectionParser.Parse(
            ScorecardValidator.ReadReportSnapshot(projectionPath));
        var noisyTrackerPath = Path.Combine(
            directory.DirectoryPath,
            "noisy-tracker.md");
        File.WriteAllText(
            noisyTrackerPath,
            "| Noise | Noise | repository-wide | verified | Noise | Noise |\n" +
            "|---|---|---|---|---|---|\n" +
            BuildStableTrackerBody(bundle),
            new UTF8Encoding(false));
        Assert.Empty(SharedRowProjectionValidator.ValidateTracker(
            projection,
            ScorecardValidator.ReadReportSnapshot(noisyTrackerPath),
            bundle,
            ScorecardValidator.LoadCoreRubric(
                Layout.ChecklistPath).Requirements));
        var validError = new StringWriter(CultureInfo.InvariantCulture);

        var validExitCode = TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--shared-row-projection",
                    projectionPath,
                    trackerPath,
                ],
                TextWriter.Null,
                validError);
        Assert.True(validExitCode == 0, validError.ToString());

        var root = JsonNode.Parse(
            File.ReadAllText(projectionPath, Encoding.UTF8))!.AsObject();
        root["rows"]!.AsArray().RemoveAt(0);
        File.WriteAllText(
            projectionPath,
            root.ToJsonString(),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--shared-row-projection",
                    projectionPath,
                    trackerPath,
                ],
                TextWriter.Null,
                error));
        Assert.Contains(
            "requirement set differs from the tracker",
            error.ToString());
    }

    [Fact]
    public void SharedProjectionSupportsSelectedRepositoryWideOverlayRows()
    {
        using var directory = new TemporaryDirectory();
        var repositoryWideOverlayIds = new HashSet<string>(
            ["SCF-01"],
            StringComparer.Ordinal);
        var (reportPath, bundlePath) = WriteStableCompleteArtifacts(
            directory.DirectoryPath,
            ["scaffolder"],
            repositoryWideOverlayIds);
        var bundle = KnownBundle();
        var projectionPath = WriteSharedRowProjectionFromReport(
            directory.DirectoryPath,
            reportPath,
            bundle);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var trackerPath = Path.Combine(directory.DirectoryPath, "tracker.md");
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(
                bundle,
                ["scaffolder"],
                repositoryWideOverlayIds),
            new UTF8Encoding(false));

        Assert.Equal(
            0,
            ScorecardCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--overlay",
                    "scaffolder",
                    "--evidence-bundle",
                    bundlePath,
                    "--shared-row-projection",
                    projectionPath,
                    reportPath,
                    "--receipt",
                    receiptPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
        Assert.Equal(
            0,
            TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--overlay",
                    "scaffolder",
                    "--evidence-bundle",
                    bundlePath,
                    "--source-report",
                    reportPath,
                    "--shared-row-projection",
                    projectionPath,
                    trackerPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
        Assert.Equal(
            0,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--shared-row-projection",
                    projectionPath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                TextWriter.Null));

        var unselectedErrors =
            SharedRowProjectionValidator.ValidateSourceReport(
                SharedRowProjectionParser.Parse(
                    ScorecardValidator.ReadReportSnapshot(projectionPath)),
                ScorecardValidator.ParseScorecard(
                    ScorecardValidator.ReadReportSnapshot(reportPath)),
                bundle,
                ScorecardValidator.LoadRequirementSet(Layout, []));
        Assert.Contains(
            unselectedErrors,
            error => error.Contains(
                "PROJ003: shared-row projection contains unknown requirement 'SCF-01'",
                StringComparison.Ordinal));
    }

    [Fact]
    public void EmbeddedProvenanceDigestsMustResolveToLiveInputs()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var bundleDigest = CanonicalEvidenceJson.ComputeSha256(
            File.ReadAllBytes(bundlePath));
        File.AppendAllText(
            reportPath,
            $"\nEvidence bundle SHA-256: `{bundleDigest}`.\n",
            new UTF8Encoding(false));
        Assert.Equal(
            0,
            RunStableScorecard(reportPath, bundlePath, receiptPath: null));

        var staleDigest =
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef";
        File.WriteAllText(
            reportPath,
            File.ReadAllText(reportPath, Encoding.UTF8).Replace(
                bundleDigest,
                staleDigest,
                StringComparison.Ordinal),
            new UTF8Encoding(false));
        var reportError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                reportError));
        Assert.Contains("PROV001", reportError.ToString());

        var trackerPath = Path.Combine(directory.DirectoryPath, "tracker.md");
        var cleanDirectory = Path.Combine(directory.DirectoryPath, "clean");
        Directory.CreateDirectory(cleanDirectory);
        var cleanReportPath = WriteStableCompleteArtifacts(
            cleanDirectory,
            []).ReportPath;
        var reportDigest = CanonicalEvidenceJson.ComputeSha256(
            File.ReadAllBytes(cleanReportPath));
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(KnownBundle()) +
            $"\n\nSource report SHA-256: `{reportDigest}`.",
            new UTF8Encoding(false));
        Assert.Equal(
            0,
            TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--source-report",
                    cleanReportPath,
                    trackerPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
        var trackerError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    trackerPath,
                ],
                TextWriter.Null,
                trackerError));
        Assert.Contains("PROV001", trackerError.ToString());
    }

    [Fact]
    public void StableArtifactsAcceptBound64HexRepositoryCommitOnly()
    {
        using var directory = new TemporaryDirectory();
        var bundle = BuildBundleWithRepositoryCommit(new string('9', 64));
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath,
            bundle);
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var trackerPath = Path.Combine(directory.DirectoryPath, "tracker.md");
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(bundle),
            new UTF8Encoding(false));

        Assert.Equal(
            0,
            RunStableScorecard(reportPath, bundlePath, receiptPath));
        Assert.Equal(
            0,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
        Assert.Equal(
            0,
            TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    trackerPath,
                ],
                TextWriter.Null,
                TextWriter.Null));

        const string UnboundDigest =
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef";
        File.AppendAllText(
            reportPath,
            $"\nUnbound SHA-256: `{UnboundDigest}`.\n",
            new UTF8Encoding(false));
        var scorecardError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                scorecardError));
        Assert.Contains("PROV001", scorecardError.ToString());

        File.AppendAllText(
            trackerPath,
            $"\nUnbound SHA-256: `{UnboundDigest}`.\n",
            new UTF8Encoding(false));
        var trackerError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    trackerPath,
                ],
                TextWriter.Null,
                trackerError));
        Assert.Contains("PROV001", trackerError.ToString());
    }

    [Fact]
    public void ProvenanceInputsDoNotRecursivelyAuthorizeEmbeddedDigests()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var nestedDigest =
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef";
        var provenanceInputPath = Path.Combine(
            directory.DirectoryPath,
            "target-manifest.json");
        File.WriteAllText(
            provenanceInputPath,
            $"{{\"nested_sha256\":\"{nestedDigest}\"}}\n",
            new UTF8Encoding(false));
        var provenanceInputDigest = CanonicalEvidenceJson.ComputeSha256(
            File.ReadAllBytes(provenanceInputPath));
        File.AppendAllText(
            reportPath,
            $"\nTarget manifest SHA-256: `{provenanceInputDigest}`.\n",
            new UTF8Encoding(false));

        Assert.Equal(
            0,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                provenanceInputPaths: [provenanceInputPath]));

        File.WriteAllText(
            reportPath,
            File.ReadAllText(reportPath, Encoding.UTF8).Replace(
                provenanceInputDigest,
                nestedDigest,
                StringComparison.Ordinal),
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                error,
                provenanceInputPaths: [provenanceInputPath]));
        Assert.Contains("PROV001", error.ToString());
    }

    [Fact]
    public void ProvenanceInputsEnforceSharedResourceLimits()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var paths = Enumerable.Range(
            0,
            ValidationProvenance.MaximumProvenanceInputCount + 1)
            .Select(index =>
            {
                var path = Path.Combine(
                    directory.DirectoryPath,
                    $"input-{index:D2}.json");
                File.WriteAllText(path, "{}", new UTF8Encoding(false));
                return path;
            })
            .ToArray();
        var countError = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Equal(
            1,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath: null,
                countError,
                provenanceInputPaths: paths));
        Assert.Contains("PROV003", countError.ToString());

        var aggregateError = Assert.Throws<InvalidDataException>(() =>
            ValidationProvenance.ReadProvenanceInputSnapshots(
                paths[..2],
                maximumCount: 2,
                maximumAggregateBytes: 3));
        Assert.Contains("PROV003", aggregateError.Message);
    }

    [Fact]
    public void ProvenanceInputsBindDigestsAndRoundTripThroughReceipt()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var firstInputPath = Path.Combine(
            directory.DirectoryPath,
            "target-manifest.json");
        var secondInputPath = Path.Combine(
            directory.DirectoryPath,
            "probe-receipt.json");
        File.WriteAllText(
            firstInputPath,
            "{\"target\":\"Tree\"}\n",
            new UTF8Encoding(false));
        File.WriteAllText(
            secondInputPath,
            "{\"result\":\"passed\"}\n",
            new UTF8Encoding(false));
        var firstDigest = CanonicalEvidenceJson.ComputeSha256(
            File.ReadAllBytes(firstInputPath));
        var secondDigest = CanonicalEvidenceJson.ComputeSha256(
            File.ReadAllBytes(secondInputPath));
        File.AppendAllText(
            reportPath,
            $"\nTarget manifest SHA-256: `{firstDigest}`.\n" +
            $"Probe receipt SHA-256: `{secondDigest}`.\n",
            new UTF8Encoding(false));
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var provenanceInputPaths = new[] { firstInputPath, secondInputPath };

        Assert.Equal(
            0,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath,
                provenanceInputPaths: provenanceInputPaths));
        using (var receipt = JsonDocument.Parse(
            File.ReadAllText(receiptPath, Encoding.UTF8)))
        {
            Assert.Equal(
                [
                    "provenance-inputs/0000",
                    "provenance-inputs/0001",
                    "references/checklist.md",
                ],
                receipt.RootElement
                    .GetProperty("validation_inputs")
                    .GetProperty("files")
                    .EnumerateArray()
                    .Select(file => file.GetProperty("path").GetString()));
        }

        Assert.Equal(
            0,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--provenance-input",
                    firstInputPath,
                    "--provenance-input",
                    secondInputPath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
        var reversedError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--provenance-input",
                    secondInputPath,
                    "--provenance-input",
                    firstInputPath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                reversedError));
        Assert.Contains("RECEIPT003", reversedError.ToString());

        var trackerPath = Path.Combine(directory.DirectoryPath, "tracker.md");
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(KnownBundle()) +
            $"\n\nTarget manifest SHA-256: `{firstDigest}`.",
            new UTF8Encoding(false));
        Assert.Equal(
            0,
            TrackerCommand.Run(
                [
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--provenance-input",
                    firstInputPath,
                    trackerPath,
                ],
                TextWriter.Null,
                TextWriter.Null));
    }

    [Fact]
    public void ProvenanceInputsFailClosedWhenOmittedChangedOrAliased()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var provenanceInputPath = Path.Combine(
            directory.DirectoryPath,
            "target-manifest.json");
        File.WriteAllText(
            provenanceInputPath,
            "{\"target\":\"Tree\"}\n",
            new UTF8Encoding(false));
        var provenanceDigest = CanonicalEvidenceJson.ComputeSha256(
            File.ReadAllBytes(provenanceInputPath));
        File.AppendAllText(
            reportPath,
            $"\nTarget manifest SHA-256: `{provenanceDigest}`.\n",
            new UTF8Encoding(false));
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        Assert.Equal(
            0,
            RunStableScorecard(
                reportPath,
                bundlePath,
                receiptPath,
                provenanceInputPaths: [provenanceInputPath]));

        var omittedError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                omittedError));
        Assert.Contains("RECEIPT003", omittedError.ToString());

        File.AppendAllText(
            provenanceInputPath,
            "{\"changed\":true}\n",
            new UTF8Encoding(false));
        var changedError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            ReceiptCommand.Run(
                [
                    "validate",
                    "--skill-dir",
                    Layout.Root,
                    "--evidence-bundle",
                    bundlePath,
                    "--provenance-input",
                    provenanceInputPath,
                    "--report",
                    reportPath,
                    receiptPath,
                ],
                TextWriter.Null,
                changedError));
        Assert.Contains("RECEIPT003", changedError.ToString());

        var aliasDirectory = Path.Combine(directory.DirectoryPath, "alias");
        Directory.CreateDirectory(aliasDirectory);
        var (aliasReportPath, aliasBundlePath) = WriteStableTargetedArtifacts(
            aliasDirectory);
        var aliasError = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(
            1,
            RunStableScorecard(
                aliasReportPath,
                aliasBundlePath,
                Path.Combine(aliasDirectory, "receipt.json"),
                aliasError,
                provenanceInputPaths: [aliasBundlePath]));
        Assert.Contains("distinct artifacts", aliasError.ToString());
    }

    [Fact]
    public void ProvenanceInputMutationBeforePublishLeavesNoReceipt()
    {
        using var directory = new TemporaryDirectory();
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath);
        var provenanceInputPath = Path.Combine(
            directory.DirectoryPath,
            "target-manifest.json");
        File.WriteAllText(
            provenanceInputPath,
            "{\"target\":\"Tree\"}\n",
            new UTF8Encoding(false));
        var receiptPath = Path.Combine(directory.DirectoryPath, "receipt.json");
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = RunStableScorecard(
            reportPath,
            bundlePath,
            receiptPath,
            error,
            beforeReceiptPublish: () => File.AppendAllText(
                provenanceInputPath,
                "{\"changed\":true}\n",
                new UTF8Encoding(false)),
            provenanceInputPaths: [provenanceInputPath]);

        Assert.Equal(1, exitCode);
        Assert.Contains("RECEIPT004", error.ToString());
        Assert.Contains("target-manifest.json", error.ToString());
        Assert.False(File.Exists(receiptPath));
    }

    [Fact]
    public void LegacyModeRejectsProvenanceInputs()
    {
        using var directory = new TemporaryDirectory();
        var reportPath = WriteLegacyTargetedReport(directory.DirectoryPath);
        var provenanceInputPath = Path.Combine(
            directory.DirectoryPath,
            "target-manifest.json");
        File.WriteAllText(
            provenanceInputPath,
            "{}\n",
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var scorecardExitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--ids",
                "LP-01,LP-02",
                "--legacy-evidence",
                "--provenance-input",
                provenanceInputPath,
                reportPath,
            ],
            TextWriter.Null,
            error);
        var receiptExitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                Layout.Root,
                "--legacy-evidence",
                "--provenance-input",
                provenanceInputPath,
                "--report",
                reportPath,
                Path.Combine(directory.DirectoryPath, "receipt.json"),
            ],
            TextWriter.Null,
            error);

        Assert.Equal(1, scorecardExitCode);
        Assert.Equal(1, receiptExitCode);
        Assert.Equal(
            2,
            error.ToString().Split("PROV002", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void TemplateEmissionRejectsProvenanceInputs()
    {
        using var directory = new TemporaryDirectory();
        var provenanceInputPath = Path.Combine(
            directory.DirectoryPath,
            "target-manifest.json");
        File.WriteAllText(
            provenanceInputPath,
            "{}\n",
            new UTF8Encoding(false));
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = ScorecardCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--emit-template",
                "--provenance-input",
                provenanceInputPath,
            ],
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "--provenance-input cannot be combined with --emit-template",
            error.ToString());
    }

    [Fact]
    public void UnselectedComponentLedgerRecordsRemainValidHistory()
    {
        using var directory = new TemporaryDirectory();
        var assessment = KnownAssessment();
        var componentLedger = EvidenceLedgerBuilder.BuildComponentLedger(
            assessment,
            [
                ComponentDraft("Current component evidence.", 'e'),
                ComponentDraft("Superseded component context.", 'f'),
            ]);
        var repositoryLedger = KnownRepositoryLedger();
        var bundle = EvidenceLedgerBuilder.BuildBundle(
            assessment,
            [repositoryLedger, componentLedger],
            [
                .. repositoryLedger.Records.Select(record => record.StableId),
                componentLedger.Records[0].StableId,
            ]);
        var (reportPath, bundlePath) = WriteStableTargetedArtifacts(
            directory.DirectoryPath,
            bundle);

        Assert.Equal(
            0,
            RunStableScorecard(reportPath, bundlePath, receiptPath: null));
        Assert.DoesNotContain(
            componentLedger.Records[1].StableId,
            File.ReadAllText(reportPath, Encoding.UTF8));
    }

    [Fact]
    public void StableTrackerValidatesAssessmentProjectionAndModes()
    {
        using var directory = new TemporaryDirectory();
        var bundle = KnownBundle();
        var bundlePath = Path.Combine(directory.DirectoryPath, "evidence.json");
        File.WriteAllBytes(
            bundlePath,
            CanonicalEvidenceJson.SerializeBundle(bundle));
        var trackerPath = Path.Combine(directory.DirectoryPath, "tracker.md");
        File.WriteAllText(
            trackerPath,
            BuildStableTrackerBody(bundle),
            new UTF8Encoding(false));
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = TrackerCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                trackerPath,
            ],
            output,
            error);
        var omitted = TrackerCommand.Run(
            ["--skill-dir", Layout.Root, trackerPath],
            output,
            error);
        File.WriteAllText(
            trackerPath,
            File.ReadAllText(trackerPath, Encoding.UTF8).Replace(
                "\"component_id\":\"Tree\"",
                "\"component_id\":\"Radio\"",
                StringComparison.Ordinal),
            new UTF8Encoding(false));
        var mismatched = TrackerCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                trackerPath,
            ],
            output,
            error);
        var original = BuildStableTrackerBody(bundle);
        var valid = $"[{bundle.Selection[0].EvidenceId}]";
        File.WriteAllText(
            trackerPath,
            ReplaceFirst(original, valid, valid + " [EV1-deadbeef]"),
            new UTF8Encoding(false));
        var malformedReference = TrackerCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                trackerPath,
            ],
            output,
            error);
        File.WriteAllText(
            trackerPath,
            original + "\n| 99 | EV1-" + new string('f', 64) +
            " | Forged. | repository-wide |  | repository | " +
            new string('a', 64) +
            " | repository-path | `FORGED` | 2026-08-16T20:00:00Z | " +
            new string('b', 64) + " |",
            new UTF8Encoding(false));
        var forgedProjection = TrackerCommand.Run(
            [
                "--skill-dir",
                Layout.Root,
                "--evidence-bundle",
                bundlePath,
                trackerPath,
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal(1, omitted);
        Assert.Equal(1, mismatched);
        Assert.Equal(1, malformedReference);
        Assert.Equal(1, forgedProjection);
        Assert.Contains("MODE001", error.ToString());
        Assert.Contains("assessment differs", error.ToString());
        Assert.Contains("malformed stable evidence token", error.ToString());
        Assert.Contains("additional projection-shaped row", error.ToString());
    }

    private static int RunStableScorecard(
        string reportPath,
        string bundlePath,
        string? receiptPath,
        StringWriter? error = null,
        Action? beforeReceiptPublish = null,
        string? sharedRowProjectionPath = null,
        IReadOnlyList<string>? provenanceInputPaths = null)
    {
        var args = new List<string>
        {
            "--skill-dir",
            Layout.Root,
            "--ids",
            "LP-01,BEQ-01",
            "--evidence-bundle",
            bundlePath,
        };
        if (sharedRowProjectionPath is not null)
        {
            args.Add("--shared-row-projection");
            args.Add(sharedRowProjectionPath);
        }

        foreach (var provenanceInputPath in provenanceInputPaths ?? [])
        {
            args.Add("--provenance-input");
            args.Add(provenanceInputPath);
        }

        args.Add(reportPath);
        if (receiptPath is not null)
        {
            args.Add("--receipt");
            args.Add(receiptPath);
        }

        return ScorecardCommand.Run(
            args.ToArray(),
            TextWriter.Null,
            error ?? new StringWriter(CultureInfo.InvariantCulture),
            beforeReceiptPublish);
    }

    private static string ValidateStableReceipt(
        string reportPath,
        string bundlePath,
        string receiptPath,
        string skillDirectory)
    {
        var error = new StringWriter(CultureInfo.InvariantCulture);
        var exitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                skillDirectory,
                "--evidence-bundle",
                bundlePath,
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, exitCode);
        return error.ToString();
    }

    private static string ValidateLegacyReceipt(
        string reportPath,
        string receiptPath,
        string skillDirectory)
    {
        var error = new StringWriter(CultureInfo.InvariantCulture);
        var exitCode = ReceiptCommand.Run(
            [
                "validate",
                "--skill-dir",
                skillDirectory,
                "--legacy-evidence",
                "--report",
                reportPath,
                receiptPath,
            ],
            TextWriter.Null,
            error);
        Assert.Equal(1, exitCode);
        return error.ToString();
    }

    private static (
        string ReportPath,
        string BundlePath) WriteStableTargetedArtifacts(
            string directory,
            EvidenceBundle? bundle = null)
    {
        bundle ??= KnownBundle();
        var bundlePath = Path.Combine(directory, "evidence.json");
        File.WriteAllBytes(
            bundlePath,
            CanonicalEvidenceJson.SerializeBundle(bundle));
        var requirements = ScorecardValidator.LoadRequirementSet(
            Layout,
            []);
        requirements = ScorecardValidator.SelectRequirements(
            requirements,
            "LP-01,BEQ-01");
        var sources = bundle.SourceLedgers.ToDictionary(
            source => source.Ledger.LedgerKind,
            StringComparer.Ordinal);
        var repositoryIds = bundle.Selection
            .Where(selection =>
                sources["repository"].SourceLedgerSha256 ==
                selection.SourceLedgerSha256)
            .Select(selection => selection.EvidenceId)
            .ToArray();
        var componentId = Assert.Single(
            bundle.Selection,
            selection =>
                sources["component"].SourceLedgerSha256 ==
                selection.SourceLedgerSha256).EvidenceId;
        var builder = new StringBuilder();
        builder.Append(StableEvidenceValidator.RenderAssessmentBlock(
            bundle.Assessment));
        builder.Append("\n\n");
        builder.Append("| Requirement ID | Requirement | Requirement scope | Status | Evidence | Maintainer action | Reviewer follow-up |\n");
        builder.Append("|---|---|---|---|---|---|---|\n");
        foreach (var requirement in requirements)
        {
            var evidence = requirement.Scope == "repository-wide"
                ? string.Join(' ', repositoryIds.Select(id => $"[{id}]"))
                : $"[{componentId}]";
            evidence =
                "The bcr-assessment-v1 projection was reviewed. " +
                "Display order and Evidence ID confirmed. " +
                evidence;
            builder.Append(
                CultureInfo.InvariantCulture,
                $"| {requirement.Identifier} | {requirement.Text.Replace("|", "\\|", StringComparison.Ordinal)} | " +
                $"{requirement.Scope} | verified | {evidence} | - | - |\n");
        }

        builder.Append("\n## Evidence ledger\n\n");
        builder.Append(StableEvidenceValidator.RenderProjection(bundle));
        builder.Append('\n');
        var reportPath = Path.Combine(directory, "report.md");
        File.WriteAllText(
            reportPath,
            builder.ToString(),
            new UTF8Encoding(false));
        return (reportPath, bundlePath);
    }

    private static (
        string ReportPath,
        string BundlePath) WriteStableCompleteArtifacts(
            string directory,
            IReadOnlyList<string> overlays,
            IReadOnlySet<string>? repositoryWideOverlayIds = null)
    {
        repositoryWideOverlayIds ??= new HashSet<string>(StringComparer.Ordinal);
        var bundle = KnownBundle();
        var bundlePath = Path.Combine(directory, "evidence.json");
        File.WriteAllBytes(
            bundlePath,
            CanonicalEvidenceJson.SerializeBundle(bundle));
        var requirements = ScorecardValidator.LoadRequirementSet(Layout, overlays);
        var sources = bundle.SourceLedgers.ToDictionary(
            source => source.Ledger.LedgerKind,
            StringComparer.Ordinal);
        var repositoryIds = bundle.Selection
            .Where(selection =>
                sources["repository"].SourceLedgerSha256 ==
                selection.SourceLedgerSha256)
            .Select(selection => selection.EvidenceId)
            .ToArray();
        var componentId = Assert.Single(
            bundle.Selection,
            selection =>
                sources["component"].SourceLedgerSha256 ==
                selection.SourceLedgerSha256).EvidenceId;
        var builder = new StringBuilder();
        builder.Append(StableEvidenceValidator.RenderAssessmentBlock(
            bundle.Assessment));
        builder.Append("\n\n");
        builder.Append("| Requirement ID | Requirement | Requirement scope | Status | ");
        builder.Append("Evidence | Maintainer action | Reviewer follow-up |\n");
        builder.Append("|---|---|---|---|---|---|---|\n");
        foreach (var requirement in requirements)
        {
            var scope = requirement.Scope ??
                (repositoryWideOverlayIds.Contains(requirement.Identifier)
                    ? "repository-wide"
                    : "component-specific");
            var evidence = scope == "repository-wide"
                ? string.Join(' ', repositoryIds.Select(id => $"[{id}]"))
                : $"[{componentId}]";
            builder.Append(
                CultureInfo.InvariantCulture,
                $"| {requirement.Identifier} | " +
                $"{requirement.Text.Replace("|", "\\|", StringComparison.Ordinal)} | " +
                $"{scope} | verified | " +
                $"{evidence} | - | - |\n");
        }

        builder.Append("\n## Evidence ledger\n\n");
        builder.Append(StableEvidenceValidator.RenderProjection(bundle));
        builder.Append('\n');
        var reportPath = Path.Combine(directory, "complete-report.md");
        File.WriteAllText(
            reportPath,
            builder.ToString(),
            new UTF8Encoding(false));
        return (reportPath, bundlePath);
    }

    private static string WriteSharedRowProjectionFromReport(
        string directory,
        string reportPath,
        EvidenceBundle bundle)
    {
        var report = ScorecardValidator.ReadReportSnapshot(reportPath);
        var rows = ScorecardValidator.ParseScorecard(report)
            .Where(row => row.Scope == "repository-wide")
            .Select(row => new SharedRowProjectionRow(
                row.Identifier,
                row.Requirement,
                row.Scope,
                row.Status,
                ExtractEvidenceAnchors(row.Evidence),
                row.MaintainerAction,
                row.ReviewerFollowUp))
            .ToArray();
        return WriteSharedRowProjection(directory, bundle, rows);
    }

    private static string WriteCompleteSharedRowProjection(
        string directory,
        EvidenceBundle bundle)
    {
        var sources = bundle.SourceLedgers.ToDictionary(
            source => source.Ledger.LedgerKind,
            StringComparer.Ordinal);
        var repositoryIds = bundle.Selection
            .Where(selection =>
                selection.SourceLedgerSha256 ==
                sources["repository"].SourceLedgerSha256)
            .Select(selection => selection.EvidenceId)
            .ToArray();
        var evidence = string.Join(
            ' ',
            repositoryIds.Select(id => $"[{id}]"));
        var rows = ScorecardValidator.LoadRequirementSet(Layout, [])
            .Where(requirement => requirement.Scope == "repository-wide")
            .Select(requirement => new SharedRowProjectionRow(
                requirement.Identifier,
                requirement.Text.Replace("|", "\\|", StringComparison.Ordinal),
                "repository-wide",
                "verified",
                evidence,
                "-",
                "-"))
            .ToArray();
        return WriteSharedRowProjection(directory, bundle, rows);
    }

    private static string WriteSharedRowProjection(
        string directory,
        EvidenceBundle bundle,
        IReadOnlyList<SharedRowProjectionRow> rows)
    {
        var repository = Assert.Single(
            bundle.SourceLedgers,
            source => source.Ledger.LedgerKind == "repository");
        var jsonRows = new JsonArray();
        foreach (var row in rows)
        {
            var evidenceIds = Regex.Matches(
                row.EvidenceAnchors,
                @"\[([^\]]+)\]",
                RegexOptions.CultureInvariant)
                .Select(match => match.Groups[1].Value)
                .ToArray();
            jsonRows.Add(new JsonObject
            {
                ["requirement_id"] = row.Identifier,
                ["requirement"] = row.Requirement,
                ["requirement_scope"] = row.RequirementScope,
                ["status"] = row.Status,
                ["evidence"] = new JsonArray(
                    evidenceIds
                        .Select(identifier =>
                            (JsonNode?)JsonValue.Create(identifier))
                        .ToArray()),
                ["evidence_anchors"] = row.EvidenceAnchors,
                ["maintainer_action"] = row.MaintainerAction,
                ["notes"] = row.ReviewerFollowUp,
            });
        }

        var projection = new JsonObject
        {
            ["schema"] = "example-readiness/shared-row-projection/v1",
            ["purpose"] = "Canonical repository-wide projection for a batch.",
            ["owner"] = "coordinator",
            ["import_rule"] = "Copy every projected field unchanged.",
            ["identity"] = new JsonObject
            {
                ["repository_uri"] =
                    bundle.Assessment.Repository.RepositoryUri,
                ["reviewed_assessment_commit"] =
                    bundle.Assessment.Repository.Commit,
            },
            ["bound_artifacts"] = new JsonObject
            {
                ["repository_ledger_path"] = "repository-ledger.json",
                ["repository_ledger_sha256"] =
                    repository.SourceLedgerSha256,
            },
            ["rubric"] = new JsonObject
            {
                ["version"] = "1.3.0",
                ["scope_schema_version"] = 1,
                ["row_count"] = rows.Count,
            },
            ["rows"] = jsonRows,
        };
        var path = Path.Combine(directory, "shared-row-projection.json");
        File.WriteAllText(
            path,
            projection.ToJsonString(),
            new UTF8Encoding(false));
        return path;
    }

    private static string ExtractEvidenceAnchors(string evidence)
    {
        return string.Join(
            ' ',
            Regex.Matches(
                evidence,
                @"\[EV1-[0-9a-f]{64}\]",
                RegexOptions.CultureInvariant)
                .Select(match => match.Value));
    }

    private static EvidenceRecordDraft ComponentDraft(
        string claim,
        char digestCharacter)
    {
        return new EvidenceRecordDraft(
            claim,
            new EvidenceApplicability("component-specific", "Tree"),
            new EvidenceProvenance(
                "command-probe",
                $"probe: {claim}",
                "Run bounded component probe.",
                "2026-08-16T20:03:00Z",
                new Sha256Digest(
                    "sha256",
                    new string(digestCharacter, 64)),
                "commitment-only"),
            []);
    }

    private static string WriteLegacyTargetedReport(string directory)
    {
        var requirements = ScorecardValidator
            .LoadRequirements(Layout.ChecklistPath)
            .Take(2)
            .ToArray();
        var report = ScorecardValidator.RenderTemplate(requirements)
            .Replace("[status]", "verified", StringComparison.Ordinal)
            .Replace("[evidence]", "[E-001]", StringComparison.Ordinal)
            .Replace("[maintainer action]", "-", StringComparison.Ordinal)
            .Replace("[reviewer follow-up]", "-", StringComparison.Ordinal) +
            "\n| Evidence ID | Claim | Repository/SHA or package | Evidence type | " +
            "Reproduction/source | Rechecked now? |\n" +
            "|---|---|---|---|---|---|\n" +
            "| E-001 | Repository license is MIT. | owner/repo@sha | source | " +
            "LICENSE | yes |\n";
        var path = Path.Combine(directory, "legacy-report.md");
        File.WriteAllText(path, report, new UTF8Encoding(false));
        return path;
    }

    private static string WriteLegacyTargetedOverlayReport(string directory)
    {
        var requirement = Assert.Single(
            ScorecardValidator.SelectRequirements(
                ScorecardValidator.LoadRequirementSet(Layout, ["scaffolder"]),
                "SCF-01"));
        var report =
            "| Requirement ID | Requirement | Requirement scope | Status | Evidence | " +
            "Maintainer action | Reviewer follow-up |\n" +
            "|---|---|---|---|---|---|---|\n" +
            $"| {requirement.Identifier} | {requirement.Text} | component-specific | " +
            "verified | [E-001] | - | - |\n\n" +
            "| Evidence ID | Claim | Repository/SHA or package | Evidence type | " +
            "Reproduction/source | Rechecked now? |\n" +
            "|---|---|---|---|---|---|\n" +
            "| E-001 | Scaffolder uses documented integration. | owner/repo@sha | " +
            "source | scaffold.cs | yes |\n";
        var path = Path.Combine(directory, "legacy-overlay-report.md");
        File.WriteAllText(path, report, new UTF8Encoding(false));
        return path;
    }

    private static string WriteLegacyCompleteReport(
        string directory,
        IReadOnlyList<string> overlays)
    {
        var requirements = ScorecardValidator.LoadRequirementSet(Layout, overlays);
        var builder = new StringBuilder();
        builder.Append("| Requirement ID | Requirement | Requirement scope | Status | ");
        builder.Append("Evidence | Maintainer action | Reviewer follow-up |\n");
        builder.Append("|---|---|---|---|---|---|---|\n");
        foreach (var requirement in requirements)
        {
            builder.Append(
                CultureInfo.InvariantCulture,
                $"| {requirement.Identifier} | " +
                $"{requirement.Text.Replace("|", "\\|", StringComparison.Ordinal)} | " +
                $"{requirement.Scope ?? "component-specific"} | verified | [E-001] | - | - |\n");
        }

        builder.Append("\n| Evidence ID | Claim | Repository/SHA or package | ");
        builder.Append("Evidence type | Reproduction/source | Rechecked now? |\n");
        builder.Append("|---|---|---|---|---|---|\n");
        builder.Append("| E-001 | Exact evidence exists. | owner/repo@sha | source | ");
        builder.Append("evidence.txt | yes |\n");
        var path = Path.Combine(directory, "legacy-complete-report.md");
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        return path;
    }

    private static string BuildDraftJson()
    {
        return "{\"schema_version\":1,\"records\":[{\"claim\":\"Repository license is MIT.\"," +
            "\"applicability\":{\"scope\":\"repository-wide\",\"component_id\":null}," +
            "\"provenance\":{\"kind\":\"repository-path\",\"locator\":\"LICENSE\"," +
            "\"method\":\"Read exact repository file.\",\"captured_at_utc\":" +
            "\"2026-08-16T20:00:00Z\",\"content_sha256\":{\"algorithm\":\"sha256\"," +
            "\"value\":\"" + new string('b', 64) +
            "\"},\"retention\":\"commitment-only\"},\"supersedes\":[]}]}";
    }

    private static EvidenceRecordDraft RepositoryDraftWithPublicLocator(
        string locator)
    {
        return new EvidenceRecordDraft(
            "Repository license is MIT.",
            new EvidenceApplicability("repository-wide", null),
            new EvidenceProvenance(
                "public-https",
                locator,
                "Read public evidence.",
                "2026-08-16T20:00:00Z",
                new Sha256Digest("sha256", new string('b', 64)),
                "commitment-only"),
            []);
    }

    private static string BuildStableTrackerBody(
        EvidenceBundle bundle,
        IReadOnlyList<string>? overlays = null,
        IReadOnlySet<string>? repositoryWideOverlayIds = null)
    {
        overlays ??= [];
        repositoryWideOverlayIds ??=
            new HashSet<string>(StringComparer.Ordinal);
        var requirements = ScorecardValidator.LoadRequirementSet(Layout, overlays);
        var sources = bundle.SourceLedgers.ToDictionary(
            source => source.Ledger.LedgerKind,
            StringComparer.Ordinal);
        var repositoryIds = bundle.Selection
            .Where(selection =>
                selection.SourceLedgerSha256 ==
                sources["repository"].SourceLedgerSha256)
            .Select(selection => selection.EvidenceId)
            .ToArray();
        var componentId = Assert.Single(
            bundle.Selection,
            selection =>
                selection.SourceLedgerSha256 ==
                sources["component"].SourceLedgerSha256).EvidenceId;
        var builder = new StringBuilder();
        builder.Append("# Sample readiness assessment — Sample.Package 1.0.0\n\n");
        builder.Append(StableEvidenceValidator.RenderAssessmentBlock(
            bundle.Assessment));
        builder.Append("\n\n> **Private project draft:** Scope statement.\n\n");
        builder.Append("> **Review limitation:** AI review statement.\n\n");
        builder.Append("## Areas we believe need to be fixed\n\n");
        builder.Append("The 0 canonical `defect` rows in the full report consolidate into the 0 areas below. These areas are not ordered by priority and require human confirmation. Each should be confirmed against the linked evidence before it is treated as a final product or release determination.\n\n");
        builder.Append(TrackerValidator.FixAreaHeader);
        builder.Append("\n|---|---|---|---|\n\n");
        builder.Append(TrackerValidator.FeedbackCallout);
        builder.Append("\n\n## Full report\n\n");
        builder.Append(TrackerValidator.FullReportSentence);
        builder.Append("\n\n## Exact review scope\n\nReviewed owner/repo@SHA.\n\n");
        builder.Append("## Review-result counts\n\n");
        builder.Append(TrackerValidator.CountsTableHeader);
        builder.Append("\n|---|---|---:|\n");
        foreach (var status in TrackerValidator.StatusOrder)
        {
            var count = status == "verified" ? requirements.Count : 0;
            builder.Append(
                CultureInfo.InvariantCulture,
                $"| `{status}` | {TrackerValidator.DisplayResults[status]} | {count} |\n");
        }

        builder.Append(
            CultureInfo.InvariantCulture,
            $"|  | **Total** | **{requirements.Count}** |\n\n");
        builder.Append("## Status terminology\n\nCanonical statuses are defined by the rubric.\n\n");
        builder.Append("## Complete rubric requirement mapping\n\n");
        builder.Append(TrackerValidator.PresentedTableHeader);
        builder.Append("\n|---|---|---|---|---|---|\n");
        foreach (var requirement in requirements)
        {
            var scope = requirement.Scope ??
                (repositoryWideOverlayIds.Contains(requirement.Identifier)
                    ? "repository-wide"
                    : "component-specific");
            var evidence = scope == "repository-wide"
                ? string.Join(' ', repositoryIds.Select(id => $"[{id}]"))
                : $"[{componentId}]";
            evidence =
                "The bcr-assessment-v1 projection was reviewed. " +
                "Display order and Evidence ID confirmed. " +
                evidence;
            builder.Append(
                CultureInfo.InvariantCulture,
                $"| {requirement.Identifier} | {requirement.Text.Replace("|", "\\|", StringComparison.Ordinal)} | " +
                $"{scope} | `verified` | " +
                $"{TrackerValidator.DisplayResults["verified"]} | {evidence} |\n");
        }

        builder.Append("\n## Evidence ledger\n\n");
        builder.Append(StableEvidenceValidator.RenderProjection(bundle));
        builder.Append("\n\n## Structural validation and limitations\n\n");
        builder.Append("Receipt retained with the review.");
        return builder.ToString();
    }

    private static ExactAssessmentIdentity KnownAssessment()
    {
        return CanonicalEvidenceJson.ParseAssessment(ReadFixture("assessment.json"));
    }

    private static EvidenceBundle BuildBundleWithRepositoryCommit(string commit)
    {
        var original = KnownBundle();
        var assessment = original.Assessment with
        {
            Repository = original.Assessment.Repository with
            {
                Commit = commit,
            },
        };
        var rebuilt = original.SourceLedgers
            .Select(source =>
            {
                var drafts = source.Ledger.Records
                    .Select(record => new EvidenceRecordDraft(
                        record.Claim,
                        record.Applicability,
                        record.Provenance,
                        record.Supersedes))
                    .ToArray();
                var ledger = source.Ledger.LedgerKind switch
                {
                    "repository" => EvidenceLedgerBuilder.BuildRepositoryLedger(
                        source.Ledger.RepositorySubject! with
                        {
                            Repository =
                                source.Ledger.RepositorySubject.Repository with
                                {
                                    Commit = commit,
                                },
                        },
                        drafts),
                    "component" => EvidenceLedgerBuilder.BuildComponentLedger(
                        assessment,
                        drafts),
                    _ => throw new InvalidOperationException(
                        $"Unexpected ledger kind {source.Ledger.LedgerKind}."),
                };
                return (Original: source, Rebuilt: ledger);
            })
            .ToArray();
        var selected = original.Selection
            .OrderBy(selection => selection.DisplayOrder)
            .Select(selection =>
            {
                var pair = Assert.Single(
                    rebuilt,
                    candidate => candidate.Original.SourceLedgerSha256 ==
                        selection.SourceLedgerSha256);
                var originalRecord = Assert.Single(
                    pair.Original.Ledger.Records,
                    record => record.StableId == selection.EvidenceId);
                return Assert.Single(
                    pair.Rebuilt.Records,
                    record =>
                        record.Claim == originalRecord.Claim &&
                        record.Provenance.Locator ==
                            originalRecord.Provenance.Locator).StableId;
            })
            .ToArray();
        return EvidenceLedgerBuilder.BuildBundle(
            assessment,
            rebuilt.Select(pair => pair.Rebuilt).ToArray(),
            selected);
    }

    private static EvidenceSourceLedger KnownRepositoryLedger()
    {
        return CanonicalEvidenceJson.ParseSourceLedger(
            ReadFixture("repository-ledger.json"));
    }

    private static EvidenceBundle KnownBundle()
    {
        return CanonicalEvidenceJson.ParseBundle(ReadFixture("bundle.json"));
    }

    private static byte[] ReadFixture(string name)
    {
        var bytes = File.ReadAllBytes(Path.Combine(FixtureRoot, name));
        Assert.Equal((byte)'\n', bytes[^1]);
        return bytes[..^1];
    }

    private static string ReplaceFirst(
        string content,
        string oldValue,
        string newValue)
    {
        var index = content.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Expected to find '{oldValue}'.");
        return string.Concat(
            content.AsSpan(0, index),
            newValue,
            content.AsSpan(index + oldValue.Length));
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

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var child in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(
                child,
                Path.Combine(destination, Path.GetFileName(child)));
        }
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

        throw new InvalidOperationException("Could not find repository root.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"readiness-evidence-integration-{Guid.NewGuid():N}");
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
