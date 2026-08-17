// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace BlazorComponentReadiness;

internal static class ScorecardCommand
{
    private const string DefaultSkillDirectory =
        ".github/skills/blazor-component-readiness";

    internal static int Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        Action? beforeReceiptPublish = null)
    {
        var skillDirectory = DefaultSkillDirectory;
        string? reportPath = null;
        string? identifiers = null;
        string? receiptPath = null;
        string? evidenceBundlePath = null;
        string? sharedRowProjectionPath = null;
        var provenanceInputPaths = new List<string>();
        var emitTemplate = false;
        var legacyEvidence = false;
        var overlays = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--skill-dir":
                    if (!TryReadValue(args, ref index, out skillDirectory))
                    {
                        return MissingValue("--skill-dir", error);
                    }

                    break;
                case "--emit-template":
                    emitTemplate = true;
                    break;
                case "--overlay":
                    if (!TryReadValue(args, ref index, out var overlay))
                    {
                        return MissingValue("--overlay", error);
                    }

                    overlays.Add(overlay);
                    break;
                case "--ids":
                    if (!TryReadValue(args, ref index, out identifiers))
                    {
                        return MissingValue("--ids", error);
                    }

                    break;
                case "--receipt":
                    if (!TryReadValue(args, ref index, out receiptPath))
                    {
                        return MissingValue("--receipt", error);
                    }

                    break;
                case "--evidence-bundle":
                    if (!TryReadValue(args, ref index, out evidenceBundlePath))
                    {
                        return MissingValue("--evidence-bundle", error);
                    }

                    break;
                case "--shared-row-projection":
                    if (!TryReadValue(
                        args,
                        ref index,
                        out sharedRowProjectionPath))
                    {
                        return MissingValue("--shared-row-projection", error);
                    }

                    break;
                case "--provenance-input":
                    if (!TryReadValue(
                        args,
                        ref index,
                        out var provenanceInputPath))
                    {
                        return MissingValue("--provenance-input", error);
                    }

                    provenanceInputPaths.Add(provenanceInputPath);
                    break;
                case "--legacy-evidence":
                    legacyEvidence = true;
                    break;
                default:
                    if (args[index].StartsWith("--", StringComparison.Ordinal))
                    {
                        error.WriteLine($"Unknown option '{args[index]}'.");
                        return 1;
                    }

                    if (reportPath is not null)
                    {
                        error.WriteLine("Only one report path may be supplied.");
                        return 1;
                    }

                    reportPath = args[index];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(skillDirectory))
        {
            error.WriteLine("ERROR: --skill-dir requires a non-empty value.");
            return 1;
        }

        if (receiptPath is not null && string.IsNullOrWhiteSpace(receiptPath))
        {
            error.WriteLine("ERROR: --receipt requires a non-empty value.");
            return 1;
        }

        if (reportPath is not null && string.IsNullOrWhiteSpace(reportPath))
        {
            error.WriteLine("ERROR: report path requires a non-empty value.");
            return 1;
        }

        if (evidenceBundlePath is not null &&
            string.IsNullOrWhiteSpace(evidenceBundlePath))
        {
            error.WriteLine("ERROR: --evidence-bundle requires a non-empty value.");
            return 1;
        }

        if (sharedRowProjectionPath is not null &&
            string.IsNullOrWhiteSpace(sharedRowProjectionPath))
        {
            error.WriteLine(
                "ERROR: --shared-row-projection requires a non-empty value.");
            return 1;
        }

        try
        {
            var layout = SkillLayout.Create(skillDirectory);
            var rubric = ScorecardValidator.LoadCoreRubric(layout.ChecklistPath);
            if (identifiers is not null && overlays.Count > 0)
            {
                error.WriteLine(
                    "--ids cannot be combined with --overlay; name overlay IDs directly.");
                return 1;
            }

            if (emitTemplate && receiptPath is not null)
            {
                error.WriteLine("--receipt cannot be combined with --emit-template.");
                return 1;
            }

            if (emitTemplate && sharedRowProjectionPath is not null)
            {
                error.WriteLine(
                    "--shared-row-projection cannot be combined with --emit-template.");
                return 1;
            }

            if (emitTemplate && provenanceInputPaths.Count > 0)
            {
                error.WriteLine(
                    "--provenance-input cannot be combined with --emit-template.");
                return 1;
            }

            IReadOnlyList<string> targetedOverlayNames = identifiers is null
                ? []
                : RequirementSelection.OverlayNames(
                    identifiers.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries),
                    layout);
            var overlaySnapshotNames = identifiers is null
                ? overlays
                : targetedOverlayNames;
            var overlaySnapshots = ValidationProvenance.ReadOverlaySnapshots(
                layout,
                overlaySnapshotNames);
            var mode = "complete";
            IReadOnlyList<Requirement> requirements;
            if (identifiers is not null)
            {
                var allRequirements = ScorecardValidator.LoadRequirementSet(
                    layout,
                    targetedOverlayNames,
                    rubric,
                    overlaySnapshots);
                requirements = ScorecardValidator.SelectRequirements(
                    allRequirements,
                    identifiers);
                mode = "targeted";
            }
            else
            {
                requirements = ScorecardValidator.LoadRequirementSet(
                    layout,
                    overlays,
                    rubric,
                    overlaySnapshots);
            }

            if (emitTemplate)
            {
                if (evidenceBundlePath is not null)
                {
                    error.WriteLine(
                        "--evidence-bundle cannot be combined with --emit-template.");
                    return 1;
                }

                if (reportPath is not null)
                {
                    error.WriteLine("A report cannot be supplied with --emit-template.");
                    return 1;
                }

                output.Write(legacyEvidence
                    ? ScorecardValidator.RenderTemplate(requirements)
                    : RenderStableTemplate(requirements));
                return 0;
            }

            if (reportPath is null)
            {
                error.WriteLine("A report is required unless --emit-template is used.");
                return 1;
            }

            if (legacyEvidence == (evidenceBundlePath is not null))
            {
                error.WriteLine(
                    "ERROR: MODE001: Choose one evidence mode. Existing callers must add " +
                    "--legacy-evidence for schema-2 reports or --evidence-bundle <path> " +
                    "for stable reports.");
                return 1;
            }

            if (legacyEvidence &&
                (sharedRowProjectionPath is not null ||
                 provenanceInputPaths.Count > 0))
            {
                error.WriteLine(
                    "ERROR: PROV002: --shared-row-projection and " +
                    "--provenance-input are available only in stable evidence mode.");
                return 1;
            }

            reportPath = Path.GetFullPath(reportPath);
            if (evidenceBundlePath is not null)
            {
                evidenceBundlePath = Path.GetFullPath(evidenceBundlePath);
            }

            ReportSnapshot? sharedRowProjectionSnapshot = null;
            SharedRowProjection? sharedRowProjection = null;
            if (sharedRowProjectionPath is not null)
            {
                sharedRowProjectionPath =
                    Path.GetFullPath(sharedRowProjectionPath);
                sharedRowProjectionSnapshot =
                    ScorecardValidator.ReadReportSnapshot(
                        sharedRowProjectionPath);
                sharedRowProjection = SharedRowProjectionParser.Parse(
                    sharedRowProjectionSnapshot);
            }

            var provenanceInputSnapshots =
                ValidationProvenance.ReadProvenanceInputSnapshots(
                    provenanceInputPaths);

            if (receiptPath is not null)
            {
                receiptPath = Path.GetFullPath(receiptPath);
            }

            var report = ScorecardValidator.ReadReportSnapshot(reportPath);
            var rows = ScorecardValidator.ParseScorecard(report);
            EvidenceBundle? evidenceBundle = null;
            ReportSnapshot? evidenceBundleSnapshot = null;
            IReadOnlyList<string> errors;
            if (legacyEvidence)
            {
                var evidenceLedger = ScorecardValidator.ParseEvidenceLedger(report);
                errors = evidenceLedger.Errors
                    .Concat(ScorecardValidator.ValidateRows(
                        requirements,
                        rows,
                        evidenceLedger.Identifiers))
                    .Concat(StableEvidenceValidator.ValidateLegacyDocument(report))
                    .ToArray();
            }
            else
            {
                evidenceBundleSnapshot =
                    ScorecardValidator.ReadReportSnapshot(evidenceBundlePath!);
                evidenceBundle = CanonicalEvidenceJson.ParseBundle(
                    evidenceBundleSnapshot.Bytes);
                errors = ScorecardValidator.ValidateRows(requirements, rows)
                    .Concat(StableEvidenceValidator.ValidateScorecard(
                        report,
                        rows,
                        evidenceBundle))
                    .Concat(sharedRowProjection is null
                        ? []
                        : SharedRowProjectionValidator.ValidateSourceReport(
                            sharedRowProjection,
                            rows,
                            evidenceBundle,
                            rubric.Requirements))
                    .Concat(EmbeddedDigestValidator.Validate(
                        report,
                        evidenceBundleSnapshot,
                        evidenceBundle,
                        rubric,
                        overlaySnapshots.Values.Concat(
                            sharedRowProjectionSnapshot is null
                                ? []
                                : [sharedRowProjectionSnapshot.Bytes])
                            .Concat(provenanceInputSnapshots.Select(
                                snapshot => snapshot.Bytes))))
                    .ToArray();
            }

            if (errors.Count > 0)
            {
                foreach (var validationError in errors)
                {
                    error.WriteLine($"ERROR: {validationError}");
                }

                return 1;
            }

            if (receiptPath is not null)
            {
                SortedDictionary<string, object?> receipt;
                if (legacyEvidence)
                {
                    EnsureReceiptArtifactPaths([reportPath, receiptPath]);
                    receipt = ScorecardValidator.BuildValidationReceipt(
                        rubric,
                        report,
                        mode,
                        requirements,
                        rows,
                        overlays);
                }
                else
                {
                    EnsureReceiptArtifactPaths(
                        new[] { reportPath, evidenceBundlePath! }
                            .Concat(sharedRowProjectionPath is null
                                ? []
                                : [sharedRowProjectionPath])
                            .Concat(provenanceInputSnapshots.Select(
                                snapshot => snapshot.Path))
                            .Append(receiptPath)
                            .ToArray());
                    var selectedOverlays = RequirementSelection.OverlayNames(
                        requirements.Select(requirement => requirement.Identifier),
                        layout);
                    var validationInputs = ValidationProvenance.BuildManifest(
                        rubric,
                        selectedOverlays,
                        overlaySnapshots,
                        sharedRowProjectionSnapshot,
                        provenanceInputSnapshots);
                    receipt = ScorecardValidator.BuildValidationReceiptV3(
                        rubric,
                        report,
                        evidenceBundleSnapshot!,
                        evidenceBundle!,
                        validationInputs,
                        mode,
                        requirements,
                        rows,
                        selectedOverlays);
                }

                ScorecardValidator.WriteValidationReceipt(
                    receiptPath,
                    reportPath,
                    receipt,
                    beforePublish: legacyEvidence
                        ? () =>
                        {
                            beforeReceiptPublish?.Invoke();
                            VerifySnapshotUnchanged(report);
                            EnsureReceiptArtifactPaths([reportPath, receiptPath]);
                        }
                : () =>
                {
                    beforeReceiptPublish?.Invoke();
                    VerifySnapshotUnchanged(report);
                    VerifySnapshotUnchanged(evidenceBundleSnapshot!);
                    if (sharedRowProjectionSnapshot is not null)
                    {
                        VerifySnapshotUnchanged(
                            sharedRowProjectionSnapshot);
                    }

                    foreach (var provenanceInputSnapshot in
                        provenanceInputSnapshots)
                    {
                        VerifySnapshotUnchanged(
                            provenanceInputSnapshot);
                    }

                    EnsureReceiptArtifactPaths(
                        new[] { reportPath, evidenceBundlePath! }
                            .Concat(sharedRowProjectionPath is null
                                ? []
                                : [sharedRowProjectionPath])
                            .Concat(provenanceInputSnapshots.Select(
                                snapshot => snapshot.Path))
                            .Append(receiptPath)
                            .ToArray());
                });
            }

            output.WriteLine(
                $"Structural validation passed: {mode} scorecard has " +
                $"{requirements.Count} canonical requirements and {rows.Count} valid rows.");
            output.WriteLine(
                "Structural validation does not establish factual evidence or " +
                "classification quality.");
            if (mode == "targeted")
            {
                output.WriteLine(
                    "Targeted validation does not establish complete readiness coverage.");
            }

            if (receiptPath is not null)
            {
                output.WriteLine(
                    $"Structural validation receipt written to {receiptPath}.");
            }
        }
        catch (Exception exception) when (
            exception is IOException or
            InvalidDataException or
            UnauthorizedAccessException)
        {
            error.WriteLine($"ERROR: {exception.Message}");
            return 1;
        }

        return 0;
    }

    private static string RenderStableTemplate(
        IReadOnlyList<Requirement> requirements)
    {
        var assessment = new ExactAssessmentIdentity(
            new RepositoryIdentity(
                "https://github.com/owner/repository",
                new string('0', 40)),
            new ArtifactIdentity(
                "released-package",
                new PackageIdentity(
                    "replace.me",
                    "0.0.0",
                    new Sha256Digest("sha256", new string('0', 64)))),
            "ReplaceComponent");
        return StableEvidenceValidator.RenderAssessmentBlock(assessment) +
            "\n\n" +
            ScorecardValidator.RenderTemplate(requirements).Replace(
                "[evidence]",
                "[EV1-" + new string('0', 64) + "]",
                StringComparison.Ordinal) +
            "\n## Evidence ledger\n\n" +
            StableEvidenceValidator.ProjectionHeader +
            "\n" +
            StableEvidenceValidator.ProjectionSeparator +
            "\n";
    }

    private static void EnsureReceiptArtifactPaths(
        IReadOnlyList<string> paths)
    {
        var resolvedPaths = paths
            .Select(FileSystemUtilities.ResolveExistingPath)
            .ToArray();
        var directories = resolvedPaths
            .Select(path => Path.GetDirectoryName(path)!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (directories.Length != 1)
        {
            throw new InvalidDataException(
                "RECEIPT004: receipt artifacts must share one resolved artifact directory.");
        }

        for (var first = 0; first < resolvedPaths.Length; first++)
        {
            for (var second = first + 1; second < resolvedPaths.Length; second++)
            {
                if (string.Equals(
                    resolvedPaths[first],
                    resolvedPaths[second],
                    StringComparison.Ordinal))
                {
                    if (first == 0 && second == resolvedPaths.Length - 1)
                    {
                        throw new InvalidDataException(
                            "--receipt must not overwrite the report.");
                    }

                    throw new InvalidDataException(
                        "RECEIPT004: receipt artifacts must be distinct artifacts.");
                }
            }
        }
    }

    private static void VerifySnapshotUnchanged(ReportSnapshot snapshot)
    {
        var current = FileSystemUtilities.ReadAllBytesBounded(snapshot.Path);
        if (!CryptographicOperations.FixedTimeEquals(
            snapshot.Bytes.Span,
            current))
        {
            throw new IOException(
                $"RECEIPT004: {Path.GetFileName(snapshot.Path)} changed after validation.");
        }
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        out string value)
    {
        if (index + 1 >= args.Count)
        {
            value = string.Empty;
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static int MissingValue(string option, TextWriter error)
    {
        error.WriteLine($"{option} requires a value.");
        return 1;
    }
}
