// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class TrackerCommand
{
    private const string DefaultSkillDirectory =
        ".github/skills/blazor-component-readiness";

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var skillDirectory = DefaultSkillDirectory;
        string? bodyPath = null;
        string? evidenceBundlePath = null;
        string? sharedRowProjectionPath = null;
        string? sourceReportPath = null;
        var provenanceInputPaths = new List<string>();
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
                case "--overlay":
                    if (!TryReadValue(args, ref index, out var overlay))
                    {
                        return MissingValue("--overlay", error);
                    }

                    overlays.Add(overlay);
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
                case "--source-report":
                    if (!TryReadValue(args, ref index, out sourceReportPath))
                    {
                        return MissingValue("--source-report", error);
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

                    if (bodyPath is not null)
                    {
                        error.WriteLine("Only one tracker body path may be supplied.");
                        return 1;
                    }

                    bodyPath = args[index];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(skillDirectory))
        {
            error.WriteLine("ERROR: --skill-dir requires a non-empty value.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(bodyPath))
        {
            error.WriteLine("ERROR: a tracker body path is required.");
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
             sourceReportPath is not null ||
             provenanceInputPaths.Count > 0))
        {
            error.WriteLine(
                "ERROR: PROV002: --shared-row-projection, --source-report, and " +
                "--provenance-input are available only in stable evidence mode.");
            return 1;
        }

        try
        {
            var layout = SkillLayout.Create(skillDirectory);
            var rubric = ScorecardValidator.LoadCoreRubric(layout.ChecklistPath);
            IReadOnlyList<string> effectiveOverlays = legacyEvidence
                ? overlays
                : RequirementSelection.CanonicalOverlayNames(overlays);
            var overlaySnapshots = ValidationProvenance.ReadOverlaySnapshots(
                layout,
                effectiveOverlays);
            var requirements = ScorecardValidator.LoadRequirementSet(
                layout,
                effectiveOverlays,
                rubric,
                overlaySnapshots);
            var body = ScorecardValidator.ReadReportSnapshot(Path.GetFullPath(bodyPath));
            var evidenceBundleSnapshot = evidenceBundlePath is null
                ? null
                : ScorecardValidator.ReadReportSnapshot(
                    Path.GetFullPath(evidenceBundlePath));
            var evidenceBundle = evidenceBundleSnapshot is null
                ? null
                : CanonicalEvidenceJson.ParseBundle(
                    evidenceBundleSnapshot.Bytes);
            var sharedRowProjectionSnapshot =
                sharedRowProjectionPath is null
                    ? null
                    : ScorecardValidator.ReadReportSnapshot(
                        Path.GetFullPath(sharedRowProjectionPath));
            var sharedRowProjection =
                sharedRowProjectionSnapshot is null
                    ? null
                    : SharedRowProjectionParser.Parse(
                        sharedRowProjectionSnapshot);
            var sourceReport = sourceReportPath is null
                ? null
                : ScorecardValidator.ReadReportSnapshot(
                    Path.GetFullPath(sourceReportPath));
            var sourceReportRows = sourceReport is null
                ? null
                : ScorecardValidator.ParseScorecard(sourceReport);
            var provenanceInputSnapshots =
                ValidationProvenance.ReadProvenanceInputSnapshots(
                    provenanceInputPaths);
            var errors = TrackerValidator.Validate(
                body,
                requirements,
                evidenceBundle,
                legacyEvidence)
                .Concat(
                    sharedRowProjection is null
                        ? []
                        : SharedRowProjectionValidator.ValidateTracker(
                            sharedRowProjection,
                            body,
                            evidenceBundle!,
                            requirements))
                .Concat(
                    sourceReportRows is null
                        ? []
                        : ScorecardValidator.ValidateRows(
                            requirements,
                            sourceReportRows)
                            .Concat(StableEvidenceValidator.ValidateScorecard(
                                sourceReport!,
                                sourceReportRows,
                                evidenceBundle!))
                            .Concat(TrackerValidator.ValidateSourceReport(
                                body,
                                sourceReportRows,
                                stableEvidence: true))
                            .Concat(
                                sharedRowProjection is null
                                    ? []
                                    : SharedRowProjectionValidator.ValidateSourceReport(
                                        sharedRowProjection,
                                        sourceReportRows,
                                        evidenceBundle!,
                                        requirements)))
                .Concat(
                    legacyEvidence
                        ? []
                        : EmbeddedDigestValidator.Validate(
                            body,
                            evidenceBundleSnapshot!,
                            evidenceBundle!,
                            rubric,
                            overlaySnapshots.Values.Concat(
                                sharedRowProjectionSnapshot is null
                                    ? []
                                    : [sharedRowProjectionSnapshot.Bytes])
                                .Concat(provenanceInputSnapshots.Select(
                                    snapshot => snapshot.Bytes)),
                            sourceReport))
                .ToArray();
            if (errors.Length > 0)
            {
                foreach (var validationError in errors)
                {
                    error.WriteLine($"ERROR: {validationError}");
                }

                return 1;
            }

            output.WriteLine(
                $"Tracker presentation valid: {requirements.Count} canonical requirements, " +
                "derived review-result labels, and an exact defect-to-summary bijection.");
            output.WriteLine(
                "Presentation validation does not establish factual evidence or " +
                "classification quality.");
        }
        catch (Exception exception) when (
            exception is IOException or
            InvalidDataException or
            System.Text.Json.JsonException or
            UnauthorizedAccessException)
        {
            error.WriteLine($"ERROR: {exception.Message}");
            return 1;
        }

        return 0;
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
