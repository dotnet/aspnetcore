// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json;

namespace BlazorComponentReadiness;

internal static class ReceiptCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0] != "validate")
        {
            error.WriteLine(
                "Usage: BlazorComponentReadiness receipt validate " +
                "--agent-profile <historical-agent-profile> --report <report> " +
                "(--evidence-bundle <bundle>|--legacy-evidence) " +
                "[--shared-row-projection <projection>] " +
                "[--provenance-input <path>]... <receipt>");
            return 1;
        }

        try
        {
            return RunValidate(args, output, error);
        }
        catch (Exception exception) when (
            exception is IOException or
            InvalidDataException or
            JsonException or
            UnauthorizedAccessException)
        {
            error.WriteLine($"ERROR: {exception.Message}");
            return 1;
        }
    }

    private static int RunValidate(
        string[] args,
        TextWriter output,
        TextWriter error)
    {
        string? layoutPath = null;
        string? reportPath = null;
        string? evidenceBundlePath = null;
        string? producerValidatorPath = null;
        string? sharedRowProjectionPath = null;
        var provenanceInputPaths = new List<string>();
        string? receiptPath = null;
        var legacyEvidence = false;
        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--agent-profile":
                case "--skill-dir":
                    layoutPath = ReadValue(args, ref index, args[index]);
                    break;
                case "--report":
                    reportPath = ReadValue(args, ref index, "--report");
                    break;
                case "--evidence-bundle":
                    evidenceBundlePath =
                        ReadValue(args, ref index, "--evidence-bundle");
                    break;
                case "--producer-validator":
                    producerValidatorPath =
                        ReadValue(args, ref index, "--producer-validator");
                    break;
                case "--shared-row-projection":
                    sharedRowProjectionPath =
                        ReadValue(args, ref index, "--shared-row-projection");
                    break;
                case "--provenance-input":
                    provenanceInputPaths.Add(
                        ReadValue(args, ref index, "--provenance-input"));
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

                    if (receiptPath is not null)
                    {
                        error.WriteLine("Only one receipt path may be supplied.");
                        return 1;
                    }

                    receiptPath = args[index];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(layoutPath) ||
            string.IsNullOrWhiteSpace(reportPath) ||
            string.IsNullOrWhiteSpace(receiptPath))
        {
            error.WriteLine(
                "ERROR: receipt validate requires non-empty --agent-profile, " +
                "--report, and receipt paths.");
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
            (producerValidatorPath is not null ||
             sharedRowProjectionPath is not null ||
             provenanceInputPaths.Count > 0))
        {
            error.WriteLine(
                "ERROR: PROV002: --producer-validator, --shared-row-projection, and " +
                "--provenance-input are available only for schema-3 receipts.");
            return 1;
        }

        var report = ScorecardValidator.ReadReportSnapshot(
            Path.GetFullPath(reportPath));
        var receipt = ScorecardValidator.ReadReportSnapshot(
            Path.GetFullPath(receiptPath));
        using var document = JsonDocument.Parse(receipt.Bytes);
        if (legacyEvidence)
        {
            EnsureArtifactPaths([report.Path, receipt.Path]);
            try
            {
                ValidateLegacy(
                    SkillLayout.Create(layoutPath!),
                    report,
                    receipt,
                    document.RootElement);
            }
            catch (InvalidDataException exception)
            {
                throw CreateLegacyDiagnostic(exception);
            }

            output.WriteLine(
                "Legacy schema-2 structural revalidation passed against the supplied " +
                "agent resources; exact historical overlay/input provenance is not established.");
        }
        else
        {
            var evidenceSnapshot = ScorecardValidator.ReadReportSnapshot(
                Path.GetFullPath(evidenceBundlePath!));
            var sharedRowProjectionSnapshot =
                sharedRowProjectionPath is null
                    ? null
                    : ScorecardValidator.ReadReportSnapshot(
                        Path.GetFullPath(sharedRowProjectionPath));
            var provenanceInputSnapshots =
                ValidationProvenance.ReadProvenanceInputSnapshots(
                    provenanceInputPaths);
            EnsureArtifactPaths(
                new[] { report.Path, evidenceSnapshot.Path }
                    .Concat(sharedRowProjectionSnapshot is null
                        ? []
                        : [sharedRowProjectionSnapshot.Path])
                    .Concat(provenanceInputSnapshots.Select(
                        snapshot => snapshot.Path))
                    .Append(receipt.Path)
                    .ToArray());
            ValidateStable(
                SkillLayout.Create(layoutPath!),
                report,
                evidenceSnapshot,
                sharedRowProjectionSnapshot,
                provenanceInputSnapshots,
                receipt,
                document.RootElement);
            output.WriteLine("Valid structural artifact bindings.");
            if (producerValidatorPath is null)
            {
                output.WriteLine(
                    "Producer-byte correspondence not checked; producer execution and " +
                    "authenticity are not established.");
            }
            else
            {
                ValidateProducerBytes(
                    Path.GetFullPath(producerValidatorPath),
                    GetRequiredString(
                        document.RootElement,
                        "validator_sha256"));
                output.WriteLine(
                    "Supplied archived assembly bytes match the receipt's self-reported " +
                    "validator_sha256; producer execution and authenticity are not established.");
            }
        }

        return 0;
    }

    private static void ValidateStable(
        SkillLayout layout,
        ReportSnapshot report,
        ReportSnapshot evidenceSnapshot,
        ReportSnapshot? sharedRowProjectionSnapshot,
        IReadOnlyList<ReportSnapshot> provenanceInputSnapshots,
        ReportSnapshot receiptSnapshot,
        JsonElement receipt)
    {
        RequireSchema(receipt, 3);
        var expectedProperties = new[]
        {
            "assessment_identity_sha256",
            "canonical_row_count",
            "checklist_sha256",
            "evidence_bundle_filename",
            "evidence_bundle_sha256",
            "evidence_record_count",
            "evidence_schema_version",
            "limitation",
            "mode",
            "report_filename",
            "report_sha256",
            "rubric_identity",
            "rubric_version",
            "schema_version",
            "selected_evidence_ids",
            "selected_ids",
            "selected_overlays",
            "source_ledger_sha256",
            "scope_schema_version",
            "structural_validation",
            "valid_row_count",
            "validated_at_utc",
            "validation_inputs",
            "validation_inputs_sha256",
            "validator_sha256",
        };
        RequireProperties(receipt, expectedProperties);

        var bundle = CanonicalEvidenceJson.ParseBundle(evidenceSnapshot.Bytes);
        RequireEqual(
            Path.GetFileName(report.Path),
            GetRequiredString(receipt, "report_filename"),
            "report filename");
        RequireEqual(
            CanonicalEvidenceJson.ComputeSha256(report.Bytes.Span),
            GetRequiredString(receipt, "report_sha256"),
            "report digest");
        RequireEqual(
            Path.GetFileName(evidenceSnapshot.Path),
            GetRequiredString(receipt, "evidence_bundle_filename"),
            "evidence bundle filename");
        RequireEqual(
            CanonicalEvidenceJson.ComputeSha256(evidenceSnapshot.Bytes.Span),
            GetRequiredString(receipt, "evidence_bundle_sha256"),
            "evidence bundle digest");
        RequireEqual(
            CanonicalEvidenceJson.ComputeAssessmentSha256(bundle.Assessment),
            GetRequiredString(receipt, "assessment_identity_sha256"),
            "assessment identity digest");
        RequireEqual(
            bundle.Selection.Count,
            GetRequiredInt32(receipt, "evidence_record_count"),
            "evidence record count");
        RequireEqual(
            CanonicalEvidenceJson.EvidenceSchemaVersion,
            GetRequiredInt32(receipt, "evidence_schema_version"),
            "evidence schema version");
        RequireSequenceEqual(
            bundle.Selection.Select(selection => selection.EvidenceId),
            GetStringArray(receipt, "selected_evidence_ids"),
            "selected evidence IDs");
        RequireSequenceEqual(
            bundle.SourceLedgers
                .Select(source => source.SourceLedgerSha256)
                .Order(StringComparer.Ordinal),
            GetStringArray(receipt, "source_ledger_sha256"),
            "source ledger digests");

        var manifest = ParseValidationInputs(
            receipt.GetProperty("validation_inputs"));
        RequireEqual(
            CanonicalEvidenceJson.ComputeValidationInputsSha256(manifest),
            GetRequiredString(receipt, "validation_inputs_sha256"),
            "validation input manifest digest");
        var rows = ScorecardValidator.ParseScorecard(report);
        var representedOverlays = RequirementSelection.OverlayNames(
            rows.Select(row => row.Identifier),
            layout);
        var selectedOverlays = GetStringArray(receipt, "selected_overlays");
        RequireSequenceEqual(
            representedOverlays,
            selectedOverlays,
            "selected overlays represented by scorecard rows");
        var expectedInputPaths = new[]
        {
            "references/checklist.md",
        }.Concat(selectedOverlays.Select(
            ValidationProvenance.OverlayRelativePath))
            .Concat(sharedRowProjectionSnapshot is null
                ? []
                : ["shared-row-projection.json"])
            .Concat(provenanceInputSnapshots.Select(
                (_, index) =>
                    ValidationProvenance.ProvenanceInputRelativePath(index)))
            .Order(StringComparer.Ordinal)
            .ToArray();
        RequireSequenceEqual(
            expectedInputPaths,
            manifest.Files.Select(file => file.Path),
            "closed validation input paths");
        var projectionInput = manifest.Files
            .SingleOrDefault(
                input => input.Path == "shared-row-projection.json");
        if (projectionInput is not null)
        {
            RequireEqual(
                CanonicalEvidenceJson.ComputeSha256(
                    sharedRowProjectionSnapshot!.Bytes.Span),
                projectionInput.Sha256.Value,
                "shared-row projection digest");
        }

        for (var index = 0;
            index < provenanceInputSnapshots.Count;
            index++)
        {
            var path =
                ValidationProvenance.ProvenanceInputRelativePath(index);
            var input = AssertSingle(
                manifest.Files,
                candidate => candidate.Path == path,
                $"RECEIPT005: missing validation input '{path}'.");
            RequireEqual(
                CanonicalEvidenceJson.ComputeSha256(
                    provenanceInputSnapshots[index].Bytes.Span),
                input.Sha256.Value,
                $"provenance input {index} digest");
        }

        var (rubric, overlaySnapshots) =
            ValidateHistoricalInputs(layout, manifest);
        var requirements = LoadReceiptRequirements(
            layout,
            rubric,
            overlaySnapshots,
            GetRequiredString(receipt, "mode"),
            GetStringArray(receipt, "selected_ids"),
            selectedOverlays,
            allowMissingOverlaySnapshots: false);
        ValidateRubricReceipt(receipt, rubric);
        var errors = ScorecardValidator.ValidateRows(requirements, rows)
            .Concat(StableEvidenceValidator.ValidateScorecard(
                report,
                rows,
                bundle))
            .Concat(sharedRowProjectionSnapshot is null
                ? []
                : SharedRowProjectionValidator.ValidateSourceReport(
                    SharedRowProjectionParser.Parse(
                        sharedRowProjectionSnapshot),
                    rows,
                    bundle,
                    requirements))
            .Concat(EmbeddedDigestValidator.Validate(
                report,
                evidenceSnapshot,
                bundle,
                rubric,
                overlaySnapshots.Values.Concat(
                    sharedRowProjectionSnapshot is null
                        ? []
                        : [sharedRowProjectionSnapshot.Bytes])
                    .Concat(provenanceInputSnapshots.Select(
                        snapshot => snapshot.Bytes))))
            .ToArray();
        ThrowValidationErrors(errors);
        ValidateRowCounts(receipt, requirements, rows);
        ValidateCommonReceiptFields(receipt);
        EvidenceIdentity.ValidateDigest(
            new Sha256Digest(
                "sha256",
                GetRequiredString(receipt, "validator_sha256")),
            "validator_sha256");
        _ = receiptSnapshot;
    }

    private static void ValidateLegacy(
        SkillLayout layout,
        ReportSnapshot report,
        ReportSnapshot receiptSnapshot,
        JsonElement receipt)
    {
        RequireSchema(receipt, 2);
        RequireProperties(
            receipt,
            [
                "canonical_row_count",
                "checklist_sha256",
                "limitation",
                "mode",
                "report_filename",
                "report_sha256",
                "rubric_identity",
                "rubric_version",
                "schema_version",
                "selected_ids",
                "selected_overlays",
                "scope_schema_version",
                "structural_validation",
                "valid_row_count",
                "validated_at_utc",
            ]);
        RequireEqual(
            Path.GetFileName(report.Path),
            GetRequiredString(receipt, "report_filename"),
            "report filename");
        RequireEqual(
            CanonicalEvidenceJson.ComputeSha256(report.Bytes.Span),
            GetRequiredString(receipt, "report_sha256"),
            "report digest");

        var rows = ScorecardValidator.ParseScorecard(report);
        var representedOverlays = RequirementSelection.OverlayNames(
            rows.Select(row => row.Identifier),
            layout);
        var selectedOverlays = GetStringArray(receipt, "selected_overlays");
        var mode = GetRequiredString(receipt, "mode");
        if (mode == "targeted")
        {
            RequireSequenceEqual(
                [],
                selectedOverlays,
                "targeted legacy selected overlays");
        }
        else
        {
            RequireSetEqual(
                representedOverlays,
                selectedOverlays,
                "complete legacy selected overlays represented by scorecard rows");
        }
        var overlaySnapshots = ValidationProvenance.ReadOverlaySnapshots(
            layout,
            representedOverlays);
        if (!File.Exists(layout.ChecklistPath))
        {
            throw new InvalidDataException(
                "historical validation input 'references/checklist.md' is missing.");
        }

        var checklistBytes = File.ReadAllBytes(layout.ChecklistPath);
        var rubric = ScorecardValidator.LoadCoreRubric(
            layout.ChecklistPath,
            checklistBytes);
        ValidateRubricReceipt(receipt, rubric);
        var requirements = LoadReceiptRequirements(
            layout,
            rubric,
            overlaySnapshots,
            mode,
            GetStringArray(receipt, "selected_ids"),
            mode == "targeted" ? representedOverlays : selectedOverlays,
            allowMissingOverlaySnapshots: true);
        var ledger = ScorecardValidator.ParseEvidenceLedger(report);
        var errors = ledger.Errors
            .Concat(ScorecardValidator.ValidateRows(
                requirements,
                rows,
                ledger.Identifiers))
            .Concat(StableEvidenceValidator.ValidateLegacyDocument(report))
            .ToArray();
        ThrowValidationErrors(errors);
        ValidateRowCounts(receipt, requirements, rows);
        ValidateCommonReceiptFields(receipt);
        _ = receiptSnapshot;
    }

    private static (
        RubricSnapshot Rubric,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> OverlaySnapshots)
        ValidateHistoricalInputs(
            SkillLayout layout,
            ValidationInputManifest manifest)
    {
        var checklist = AssertSingle(
            manifest.Files,
            file => file.Path == "references/checklist.md",
            "RECEIPT005: validation manifest requires one checklist input.");
        var checklistBytes = ReadHistoricalInput(
            layout,
            checklist);
        var overlays = manifest.Files
            .Where(file =>
                file.Path != "references/checklist.md" &&
                file.Path != "shared-row-projection.json" &&
                !ValidationProvenance.IsProvenanceInputRelativePath(
                    file.Path))
            .Select(file => ValidationProvenance.OverlayNameFromRelativePath(
                file.Path))
            .ToArray();
        var overlaySnapshots = new Dictionary<string, ReadOnlyMemory<byte>>(
            StringComparer.Ordinal);
        foreach (var overlay in overlays)
        {
            var file = AssertSingle(
                manifest.Files,
                candidate =>
                    candidate.Path ==
                    ValidationProvenance.OverlayRelativePath(overlay),
                $"RECEIPT005: missing validation input for overlay '{overlay}'.");
            overlaySnapshots.Add(
                overlay,
                ReadHistoricalInput(layout, file));
        }

        return (
            ScorecardValidator.LoadCoreRubric(
                layout.ChecklistPath,
                checklistBytes),
            overlaySnapshots);
    }

    private static byte[] ReadHistoricalInput(
        SkillLayout layout,
        ValidationInput input)
    {
        var path = input.Path switch
        {
            "references/checklist.md" => layout.ChecklistPath,
            _ => layout.OverlayPaths[
                ValidationProvenance.OverlayNameFromRelativePath(input.Path)],
        };
        var resolvedRoot = FileSystemUtilities.ResolveExistingPath(layout.Root);
        var resolvedPath = FileSystemUtilities.ResolveExistingPath(path);
        if (!FileSystemUtilities.IsWithinDirectory(resolvedRoot, resolvedPath) ||
            !File.Exists(resolvedPath))
        {
            throw new InvalidDataException(
                $"RECEIPT005: historical validation input '{input.Path}' is missing.");
        }

        var bytes = File.ReadAllBytes(resolvedPath);
        var digest = CanonicalEvidenceJson.ComputeSha256(bytes);
        if (!string.Equals(digest, input.Sha256.Value, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"RECEIPT005: historical validation input '{input.Path}' changed.");
        }

        return bytes;
    }

    private static IReadOnlyList<Requirement> LoadReceiptRequirements(
        SkillLayout layout,
        RubricSnapshot rubric,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> overlaySnapshots,
        string mode,
        IReadOnlyList<string> selectedIds,
        IReadOnlyList<string> selectedOverlays,
        bool allowMissingOverlaySnapshots)
    {
        if (mode == "complete")
        {
            if (selectedIds.Count != 0)
            {
                throw new InvalidDataException(
                    "RECEIPT003: complete receipt selected_ids must be empty.");
            }

            return ScorecardValidator.LoadRequirementSet(
                layout,
                selectedOverlays,
                rubric,
                overlaySnapshots);
        }

        if (mode != "targeted" || selectedIds.Count == 0)
        {
            throw new InvalidDataException(
                $"RECEIPT003: invalid receipt mode '{mode}' or empty selection.");
        }

        var effectiveOverlays = selectedOverlays.ToHashSet(StringComparer.Ordinal);
        foreach (var identifier in selectedIds)
        {
            foreach (var prefix in layout.OverlayPrefixes)
            {
                if (identifier.StartsWith(prefix.Value + "-", StringComparison.Ordinal))
                {
                    effectiveOverlays.Add(prefix.Key);
                }
            }
        }

        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> effectiveSnapshots =
            overlaySnapshots;
        if (allowMissingOverlaySnapshots &&
            effectiveOverlays.Any(overlay =>
                !overlaySnapshots.ContainsKey(overlay)))
        {
            effectiveSnapshots = ValidationProvenance.ReadOverlaySnapshots(
                layout,
                effectiveOverlays);
        }
        else if (effectiveOverlays.Any(overlay =>
            !overlaySnapshots.ContainsKey(overlay)))
        {
            throw new InvalidDataException(
                "RECEIPT005: validation manifest omits a selected overlay.");
        }

        var all = ScorecardValidator.LoadRequirementSet(
            layout,
            effectiveOverlays,
            rubric,
            effectiveSnapshots);
        return ScorecardValidator.SelectRequirements(
            all,
            string.Join(',', selectedIds));
    }

    private static void ValidateRubricReceipt(
        JsonElement receipt,
        RubricSnapshot rubric)
    {
        RequireEqual(
            rubric.Sha256,
            GetRequiredString(receipt, "checklist_sha256"),
            "checklist digest");
        RequireEqual(
            rubric.Version,
            GetRequiredString(receipt, "rubric_version"),
            "rubric version");
        RequireEqual(
            $"blazor-component-readiness/{rubric.Version}+sha256:{rubric.Sha256}",
            GetRequiredString(receipt, "rubric_identity"),
            "rubric identity");
        RequireEqual(
            rubric.ScopeSchemaVersion,
            GetRequiredInt32(receipt, "scope_schema_version"),
            "scope schema version");
    }

    private static void ValidateRowCounts(
        JsonElement receipt,
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<ScorecardRow> rows)
    {
        RequireEqual(
            requirements.Count,
            GetRequiredInt32(receipt, "canonical_row_count"),
            "canonical row count");
        RequireEqual(
            rows.Count,
            GetRequiredInt32(receipt, "valid_row_count"),
            "valid row count");
    }

    private static void ValidateCommonReceiptFields(JsonElement receipt)
    {
        RequireEqual(
            "passed",
            GetRequiredString(receipt, "structural_validation"),
            "structural validation");
        var timestamp = GetRequiredString(receipt, "validated_at_utc");
        if (!DateTimeOffset.TryParseExact(
            timestamp,
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out _))
        {
            throw new InvalidDataException(
                "RECEIPT003: validated_at_utc is not canonical.");
        }

        RequireEqual(
            "Structural validation does not establish factual evidence or " +
            "classification quality.",
            GetRequiredString(receipt, "limitation"),
            "receipt limitation");
    }

    private static ValidationInputManifest ParseValidationInputs(JsonElement element)
    {
        RequireProperties(element, ["files", "schema_version"]);
        var filesElement = element.GetProperty("files");
        if (filesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "RECEIPT003: validation_inputs.files must be an array.");
        }

        var files = filesElement
            .EnumerateArray()
            .Select(file =>
            {
                RequireProperties(file, ["path", "sha256"]);
                var digest = file.GetProperty("sha256");
                RequireProperties(digest, ["algorithm", "value"]);
                return new ValidationInput(
                    GetRequiredString(file, "path"),
                    new Sha256Digest(
                        GetRequiredString(digest, "algorithm"),
                        GetRequiredString(digest, "value")));
            })
            .ToArray();
        return new ValidationInputManifest(
            GetRequiredInt32(element, "schema_version"),
            files);
    }

    private static void ValidateProducerBytes(string path, string expected)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        var actual = Convert.ToHexStringLower(SHA256.HashData(stream));
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "RECEIPT006: supplied archived assembly bytes do not correspond " +
                "to the receipt's self-reported validator_sha256.");
        }
    }

    private static InvalidDataException CreateLegacyDiagnostic(
        InvalidDataException exception)
    {
        if (exception.Message.StartsWith("RECEIPT007:", StringComparison.Ordinal))
        {
            return exception;
        }

        var message = exception.Message;
        foreach (var prefix in new[]
        {
            "RECEIPT003: ",
            "RECEIPT005: ",
        })
        {
            if (message.StartsWith(prefix, StringComparison.Ordinal))
            {
                message = message[prefix.Length..];
                break;
            }
        }

        return new InvalidDataException(
            "RECEIPT007: " + message,
            exception);
    }

    private static void EnsureArtifactPaths(IReadOnlyList<string> paths)
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
                "RECEIPT004: receipt artifacts must share one resolved directory.");
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
                    throw new InvalidDataException(
                        "RECEIPT004: receipt artifacts must be distinct.");
                }
            }
        }
    }

    private static void RequireSchema(JsonElement receipt, int expected)
    {
        RequireEqual(
            expected,
            GetRequiredInt32(receipt, "schema_version"),
            "receipt schema version");
    }

    private static void RequireProperties(
        JsonElement element,
        IEnumerable<string> expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "RECEIPT003: expected receipt JSON object.");
        }

        var actual = element.EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var orderedExpected = expected.Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(orderedExpected, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                "RECEIPT003: receipt properties are missing, duplicated, or unknown.");
        }
    }

    private static string GetRequiredString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(
                $"RECEIPT003: '{name}' must be a string.");
        }

        return value.GetString()!;
    }

    private static int GetRequiredInt32(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw new InvalidDataException(
                $"RECEIPT003: '{name}' must be an Int32.");
        }

        return result;
    }

    private static string[] GetStringArray(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"RECEIPT003: '{name}' must be an array.");
        }

        return value.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String
                ? item.GetString()!
                : throw new InvalidDataException(
                    $"RECEIPT003: '{name}' entries must be strings."))
            .ToArray();
    }

    private static T AssertSingle<T>(
        IEnumerable<T> values,
        Func<T, bool> predicate,
        string message)
    {
        var matches = values.Where(predicate).ToArray();
        return matches.Length == 1
            ? matches[0]
            : throw new InvalidDataException(message);
    }

    private static void ThrowValidationErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                "RECEIPT003: structural revalidation failed: " +
                string.Join("; ", errors));
        }
    }

    private static void RequireEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidDataException(
                $"RECEIPT003: {name} mismatch; expected '{expected}', " +
                $"found '{actual}'.");
        }
    }

    private static void RequireSequenceEqual(
        IEnumerable<string> expected,
        IEnumerable<string> actual,
        string name)
    {
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"RECEIPT003: {name} mismatch.");
        }
    }

    private static void RequireSetEqual(
        IEnumerable<string> expected,
        IReadOnlyList<string> actual,
        string name)
    {
        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var actualSet = actual.ToHashSet(StringComparer.Ordinal);
        if (actual.Count != actualSet.Count ||
            !expectedSet.SetEquals(actualSet))
        {
            throw new InvalidDataException(
                $"RECEIPT003: {name} mismatch.");
        }
    }

    private static string ReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option)
    {
        if (index + 1 >= args.Count ||
            string.IsNullOrWhiteSpace(args[index + 1]))
        {
            throw new InvalidDataException($"{option} requires a non-empty value.");
        }

        return args[++index];
    }
}
