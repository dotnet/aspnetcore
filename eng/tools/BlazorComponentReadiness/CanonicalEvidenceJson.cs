// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlazorComponentReadiness;

internal static class CanonicalEvidenceJson
{
    internal const int EvidenceSchemaVersion = 1;
    internal const string EvidenceRecordDomain =
        "blazor-component-readiness/evidence-record/v1";
    internal const string AssessmentDomain =
        "blazor-component-readiness/assessment/v1";
    internal const string ValidationInputsDomain =
        "blazor-component-readiness/validation-inputs/v1";

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private static readonly IEvidenceHasher DefaultHasher = new Sha256EvidenceHasher();

    internal static byte[] SerializeAssessment(ExactAssessmentIdentity assessment)
    {
        EvidenceIdentity.ValidateAssessment(assessment);
        var writer = new CanonicalJsonTextWriter();
        WriteAssessment(writer, assessment);
        return writer.ToArray();
    }

    internal static byte[] SerializeRepositorySubject(
        RepositoryLedgerSubject subject)
    {
        var normalized = EvidenceIdentity.NormalizeRepositorySubject(subject);
        if (normalized != subject)
        {
            throw new InvalidDataException(
                "EVID001: repository ledger subject is not canonical.");
        }

        var writer = new CanonicalJsonTextWriter();
        WriteRepositorySubject(writer, subject);
        return writer.ToArray();
    }

    internal static byte[] SerializeSourceLedger(EvidenceSourceLedger ledger)
    {
        EvidenceLedgerValidator.ValidateSourceLedger(ledger);
        var writer = new CanonicalJsonTextWriter();
        WriteSourceLedger(writer, ledger);
        return writer.ToArray();
    }

    internal static byte[] SerializeBundle(EvidenceBundle bundle)
    {
        EvidenceLedgerValidator.ValidateBundle(bundle);
        var writer = new CanonicalJsonTextWriter();
        WriteBundle(writer, bundle);
        return writer.ToArray();
    }

    internal static byte[] SerializeValidationInputManifest(ValidationInputManifest manifest)
    {
        ValidateValidationInputManifest(manifest);
        var writer = new CanonicalJsonTextWriter();
        WriteValidationInputManifest(writer, manifest);
        return writer.ToArray();
    }

    internal static EvidenceSourceLedger ParseSourceLedger(ReadOnlyMemory<byte> bytes)
    {
        var ledger = ParseDocument(bytes, ParseSourceLedger);
        EvidenceLedgerValidator.ValidateSourceLedger(ledger);
        RequireCanonicalBytes(bytes.Span, SerializeSourceLedger(ledger), "source ledger");
        return ledger;
    }

    internal static EvidenceBundle ParseBundle(ReadOnlyMemory<byte> bytes)
    {
        var bundle = ParseDocument(bytes, ParseBundle);
        EvidenceLedgerValidator.ValidateBundle(bundle);
        RequireCanonicalBytes(bytes.Span, SerializeBundle(bundle), "evidence bundle");
        return bundle;
    }

    internal static ExactAssessmentIdentity ParseAssessment(ReadOnlyMemory<byte> bytes)
    {
        var assessment = ParseDocument(bytes, ParseAssessment);
        EvidenceIdentity.ValidateAssessment(assessment);
        RequireCanonicalBytes(bytes.Span, SerializeAssessment(assessment), "assessment");
        return assessment;
    }

    internal static RepositoryLedgerSubject ParseRepositorySubject(
        ReadOnlyMemory<byte> bytes)
    {
        var subject = ParseDocument(bytes, ParseRepositorySubject);
        var normalized = EvidenceIdentity.NormalizeRepositorySubject(subject);
        if (normalized != subject)
        {
            throw new InvalidDataException(
                "EVID001: repository ledger subject is not canonical.");
        }

        RequireCanonicalBytes(
            bytes.Span,
            SerializeRepositorySubject(subject),
            "repository ledger subject");
        return subject;
    }

    internal static EvidenceDraftDocument ParseDraftDocument(
        ReadOnlyMemory<byte> bytes)
    {
        var draft = ParseDocument(bytes, ParseDraftDocument);
        if (draft.SchemaVersion != EvidenceSchemaVersion ||
            draft.Records.Count == 0)
        {
            throw new InvalidDataException(
                "EVID001: evidence draft requires schema 1 and records.");
        }

        return draft with
        {
            Records = draft.Records
                .Select(EvidenceIdentity.NormalizeRecordDraft)
                .ToArray(),
        };
    }

    internal static string ComputeStableId(
        string ledgerKind,
        RepositoryLedgerSubject? repositorySubject,
        ExactAssessmentIdentity? componentSubject,
        EvidenceRecordDraft record,
        IEvidenceHasher? hasher = null)
    {
        record = EvidenceIdentity.NormalizeRecordDraft(record);
        (repositorySubject, componentSubject) = NormalizeSubject(
            ledgerKind,
            repositorySubject,
            componentSubject);
        var subjectWriter = new CanonicalJsonTextWriter();
        WriteSubjectEnvelope(
            subjectWriter,
            ledgerKind,
            repositorySubject,
            componentSubject);
        var recordWriter = new CanonicalJsonTextWriter();
        WriteRecordIdentityPayload(recordWriter, record);
        var preimage = BuildDomainPreimage(
            EvidenceRecordDomain,
            subjectWriter.ToArray(),
            recordWriter.ToArray());
        var digest = (hasher ?? DefaultHasher).Hash(preimage);
        if (digest.Length != SHA256.HashSizeInBytes)
        {
            throw new InvalidDataException(
                "EVID003: evidence hasher must return exactly 32 bytes.");
        }

        return "EV1-" + Convert.ToHexStringLower(digest);
    }

    internal static string ComputeSourceLedgerSha256(EvidenceSourceLedger ledger)
    {
        return ComputeSha256(SerializeSourceLedger(ledger));
    }

    internal static string ComputeBundleSha256(EvidenceBundle bundle)
    {
        return ComputeSha256(SerializeBundle(bundle));
    }

    internal static string ComputeAssessmentSha256(ExactAssessmentIdentity assessment)
    {
        return ComputeDomainDigest(AssessmentDomain, SerializeAssessment(assessment));
    }

    internal static string ComputeValidationInputsSha256(ValidationInputManifest manifest)
    {
        return ComputeDomainDigest(
            ValidationInputsDomain,
            SerializeValidationInputManifest(manifest));
    }

    internal static string ComputeSha256(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    internal static byte[] GetRecordIdentityPreimage(
        string ledgerKind,
        RepositoryLedgerSubject? repositorySubject,
        ExactAssessmentIdentity? componentSubject,
        EvidenceRecordDraft record)
    {
        record = EvidenceIdentity.NormalizeRecordDraft(record);
        (repositorySubject, componentSubject) = NormalizeSubject(
            ledgerKind,
            repositorySubject,
            componentSubject);
        var subjectWriter = new CanonicalJsonTextWriter();
        WriteSubjectEnvelope(
            subjectWriter,
            ledgerKind,
            repositorySubject,
            componentSubject);
        var recordWriter = new CanonicalJsonTextWriter();
        WriteRecordIdentityPayload(recordWriter, record);
        return BuildDomainPreimage(
            EvidenceRecordDomain,
            subjectWriter.ToArray(),
            recordWriter.ToArray());
    }

    private static T ParseDocument<T>(
        ReadOnlyMemory<byte> bytes,
        Func<JsonElement, T> parser)
    {
        try
        {
            StrictUtf8.GetString(bytes.Span);
            using var document = JsonDocument.Parse(
                bytes,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 64,
                });
            return parser(document.RootElement);
        }
        catch (Exception exception) when (
            exception is JsonException or
            DecoderFallbackException or
            FormatException or
            OverflowException)
        {
            throw new InvalidDataException(
                $"EVID001: invalid canonical evidence JSON: {exception.Message}",
                exception);
        }
    }

    private static EvidenceSourceLedger ParseSourceLedger(JsonElement element)
    {
        RequireObjectProperties(
            element,
            "schema_version",
            "ledger_kind",
            "repository_subject",
            "component_subject",
            "records");
        var records = GetRequiredArray(element, "records")
            .EnumerateArray()
            .Select(ParseEvidenceRecord)
            .ToArray();
        return new EvidenceSourceLedger(
            GetRequiredInt32(element, "schema_version"),
            GetRequiredString(element, "ledger_kind"),
            GetNullableObject(element, "repository_subject", ParseRepositorySubject),
            GetNullableObject(element, "component_subject", ParseAssessment),
            records);
    }

    private static EvidenceBundle ParseBundle(JsonElement element)
    {
        RequireObjectProperties(
            element,
            "schema_version",
            "assessment",
            "source_ledgers",
            "selection");
        return new EvidenceBundle(
            GetRequiredInt32(element, "schema_version"),
            ParseAssessment(GetRequiredObject(element, "assessment")),
            GetRequiredArray(element, "source_ledgers")
                .EnumerateArray()
                .Select(ParseEmbeddedSourceLedger)
                .ToArray(),
            GetRequiredArray(element, "selection")
                .EnumerateArray()
                .Select(ParseSelection)
                .ToArray());
    }

    private static EvidenceDraftDocument ParseDraftDocument(JsonElement element)
    {
        RequireObjectProperties(element, "schema_version", "records");
        return new EvidenceDraftDocument(
            GetRequiredInt32(element, "schema_version"),
            GetRequiredArray(element, "records")
                .EnumerateArray()
                .Select(ParseEvidenceRecordDraft)
                .ToArray());
    }

    private static ExactAssessmentIdentity ParseAssessment(JsonElement element)
    {
        RequireObjectProperties(element, "repository", "artifact", "component_id");
        return new ExactAssessmentIdentity(
            ParseRepository(GetRequiredObject(element, "repository")),
            ParseArtifact(GetRequiredObject(element, "artifact")),
            GetRequiredString(element, "component_id"));
    }

    private static RepositoryLedgerSubject ParseRepositorySubject(JsonElement element)
    {
        RequireObjectProperties(element, "repository", "artifact", "component_id");
        return new RepositoryLedgerSubject(
            ParseRepository(GetRequiredObject(element, "repository")),
            ParseArtifact(GetRequiredObject(element, "artifact")),
            GetNullableString(element, "component_id"));
    }

    private static RepositoryIdentity ParseRepository(JsonElement element)
    {
        RequireObjectProperties(element, "repository_uri", "commit");
        return new RepositoryIdentity(
            GetRequiredString(element, "repository_uri"),
            GetRequiredString(element, "commit"));
    }

    private static ArtifactIdentity ParseArtifact(JsonElement element)
    {
        RequireObjectProperties(element, "mode", "package");
        return new ArtifactIdentity(
            GetRequiredString(element, "mode"),
            GetNullableObject(element, "package", ParsePackage));
    }

    private static PackageIdentity ParsePackage(JsonElement element)
    {
        RequireObjectProperties(element, "package_id", "version", "nupkg_sha256");
        return new PackageIdentity(
            GetRequiredString(element, "package_id"),
            GetRequiredString(element, "version"),
            ParseDigest(GetRequiredObject(element, "nupkg_sha256")));
    }

    private static Sha256Digest ParseDigest(JsonElement element)
    {
        RequireObjectProperties(element, "algorithm", "value");
        return new Sha256Digest(
            GetRequiredString(element, "algorithm"),
            GetRequiredString(element, "value"));
    }

    private static EvidenceRecord ParseEvidenceRecord(JsonElement element)
    {
        RequireObjectProperties(
            element,
            "stable_id",
            "claim",
            "applicability",
            "provenance",
            "supersedes");
        return new EvidenceRecord(
            GetRequiredString(element, "stable_id"),
            GetRequiredString(element, "claim"),
            ParseApplicability(GetRequiredObject(element, "applicability")),
            ParseProvenance(GetRequiredObject(element, "provenance")),
            GetRequiredArray(element, "supersedes")
                .EnumerateArray()
                .Select(GetStringValue)
                .ToArray());
    }

    private static EvidenceRecordDraft ParseEvidenceRecordDraft(JsonElement element)
    {
        RequireObjectProperties(
            element,
            "claim",
            "applicability",
            "provenance",
            "supersedes");
        return new EvidenceRecordDraft(
            GetRequiredString(element, "claim"),
            ParseApplicability(GetRequiredObject(element, "applicability")),
            ParseProvenance(GetRequiredObject(element, "provenance")),
            GetRequiredArray(element, "supersedes")
                .EnumerateArray()
                .Select(GetStringValue)
                .ToArray());
    }

    private static EvidenceApplicability ParseApplicability(JsonElement element)
    {
        RequireObjectProperties(element, "scope", "component_id");
        return new EvidenceApplicability(
            GetRequiredString(element, "scope"),
            GetNullableString(element, "component_id"));
    }

    private static EvidenceProvenance ParseProvenance(JsonElement element)
    {
        RequireObjectProperties(
            element,
            "kind",
            "locator",
            "method",
            "captured_at_utc",
            "content_sha256",
            "retention");
        return new EvidenceProvenance(
            GetRequiredString(element, "kind"),
            GetRequiredString(element, "locator"),
            GetRequiredString(element, "method"),
            GetRequiredString(element, "captured_at_utc"),
            ParseDigest(GetRequiredObject(element, "content_sha256")),
            GetRequiredString(element, "retention"));
    }

    private static EmbeddedSourceLedger ParseEmbeddedSourceLedger(JsonElement element)
    {
        RequireObjectProperties(element, "source_ledger_sha256", "ledger");
        return new EmbeddedSourceLedger(
            GetRequiredString(element, "source_ledger_sha256"),
            ParseSourceLedger(GetRequiredObject(element, "ledger")));
    }

    private static EvidenceSelection ParseSelection(JsonElement element)
    {
        RequireObjectProperties(
            element,
            "display_order",
            "source_ledger_sha256",
            "evidence_id");
        return new EvidenceSelection(
            GetRequiredInt32(element, "display_order"),
            GetRequiredString(element, "source_ledger_sha256"),
            GetRequiredString(element, "evidence_id"));
    }

    private static void WriteAssessment(
        CanonicalJsonTextWriter writer,
        ExactAssessmentIdentity assessment)
    {
        writer.Raw("{\"repository\":");
        WriteRepository(writer, assessment.Repository);
        writer.Raw(",\"artifact\":");
        WriteArtifact(writer, assessment.Artifact);
        writer.Raw(",\"component_id\":");
        writer.String(assessment.ComponentId);
        writer.Raw("}");
    }

    private static void WriteRepository(
        CanonicalJsonTextWriter writer,
        RepositoryIdentity repository)
    {
        writer.Raw("{\"repository_uri\":");
        writer.String(repository.RepositoryUri);
        writer.Raw(",\"commit\":");
        writer.String(repository.Commit);
        writer.Raw("}");
    }

    private static void WriteArtifact(
        CanonicalJsonTextWriter writer,
        ArtifactIdentity artifact)
    {
        writer.Raw("{\"mode\":");
        writer.String(artifact.Mode);
        writer.Raw(",\"package\":");
        if (artifact.Package is null)
        {
            writer.Raw("null");
        }
        else
        {
            WritePackage(writer, artifact.Package);
        }

        writer.Raw("}");
    }

    private static void WritePackage(
        CanonicalJsonTextWriter writer,
        PackageIdentity package)
    {
        writer.Raw("{\"package_id\":");
        writer.String(package.PackageId);
        writer.Raw(",\"version\":");
        writer.String(package.Version);
        writer.Raw(",\"nupkg_sha256\":");
        WriteDigest(writer, package.NupkgDigest);
        writer.Raw("}");
    }

    private static void WriteDigest(
        CanonicalJsonTextWriter writer,
        Sha256Digest digest)
    {
        writer.Raw("{\"algorithm\":");
        writer.String(digest.Algorithm);
        writer.Raw(",\"value\":");
        writer.String(digest.Value);
        writer.Raw("}");
    }

    private static void WriteSourceLedger(
        CanonicalJsonTextWriter writer,
        EvidenceSourceLedger ledger)
    {
        writer.Raw("{\"schema_version\":");
        writer.Integer(ledger.SchemaVersion);
        writer.Raw(",\"ledger_kind\":");
        writer.String(ledger.LedgerKind);
        writer.Raw(",\"repository_subject\":");
        if (ledger.RepositorySubject is null)
        {
            writer.Raw("null");
        }
        else
        {
            WriteRepositorySubject(writer, ledger.RepositorySubject);
        }

        writer.Raw(",\"component_subject\":");
        if (ledger.ComponentSubject is null)
        {
            writer.Raw("null");
        }
        else
        {
            WriteAssessment(writer, ledger.ComponentSubject);
        }

        writer.Raw(",\"records\":[");
        for (var index = 0; index < ledger.Records.Count; index++)
        {
            if (index > 0)
            {
                writer.Raw(",");
            }

            WriteEvidenceRecord(writer, ledger.Records[index]);
        }

        writer.Raw("]}");
    }

    private static void WriteRepositorySubject(
        CanonicalJsonTextWriter writer,
        RepositoryLedgerSubject subject)
    {
        writer.Raw("{\"repository\":");
        WriteRepository(writer, subject.Repository);
        writer.Raw(",\"artifact\":");
        WriteArtifact(writer, subject.Artifact);
        writer.Raw(",\"component_id\":");
        if (subject.ComponentId is null)
        {
            writer.Raw("null");
        }
        else
        {
            writer.String(subject.ComponentId);
        }

        writer.Raw("}");
    }

    private static void WriteEvidenceRecord(
        CanonicalJsonTextWriter writer,
        EvidenceRecord record)
    {
        writer.Raw("{\"stable_id\":");
        writer.String(record.StableId);
        writer.Raw(",\"claim\":");
        writer.String(record.Claim);
        writer.Raw(",\"applicability\":");
        WriteApplicability(writer, record.Applicability);
        writer.Raw(",\"provenance\":");
        WriteProvenance(writer, record.Provenance);
        writer.Raw(",\"supersedes\":");
        WriteStringArray(writer, record.Supersedes);
        writer.Raw("}");
    }

    private static void WriteRecordIdentityPayload(
        CanonicalJsonTextWriter writer,
        EvidenceRecordDraft record)
    {
        writer.Raw("{\"claim\":");
        writer.String(record.Claim);
        writer.Raw(",\"applicability\":");
        WriteApplicability(writer, record.Applicability);
        writer.Raw(",\"provenance\":");
        WriteProvenance(writer, record.Provenance);
        writer.Raw(",\"supersedes\":");
        WriteStringArray(writer, record.Supersedes);
        writer.Raw("}");
    }

    private static void WriteApplicability(
        CanonicalJsonTextWriter writer,
        EvidenceApplicability applicability)
    {
        writer.Raw("{\"scope\":");
        writer.String(applicability.Scope);
        writer.Raw(",\"component_id\":");
        if (applicability.ComponentId is null)
        {
            writer.Raw("null");
        }
        else
        {
            writer.String(applicability.ComponentId);
        }

        writer.Raw("}");
    }

    private static void WriteProvenance(
        CanonicalJsonTextWriter writer,
        EvidenceProvenance provenance)
    {
        writer.Raw("{\"kind\":");
        writer.String(provenance.Kind);
        writer.Raw(",\"locator\":");
        writer.String(provenance.Locator);
        writer.Raw(",\"method\":");
        writer.String(provenance.Method);
        writer.Raw(",\"captured_at_utc\":");
        writer.String(provenance.CapturedAtUtc);
        writer.Raw(",\"content_sha256\":");
        WriteDigest(writer, provenance.ContentDigest);
        writer.Raw(",\"retention\":");
        writer.String(provenance.Retention);
        writer.Raw("}");
    }

    private static void WriteSubjectEnvelope(
        CanonicalJsonTextWriter writer,
        string ledgerKind,
        RepositoryLedgerSubject? repositorySubject,
        ExactAssessmentIdentity? componentSubject)
    {
        writer.Raw("{\"ledger_kind\":");
        writer.String(ledgerKind);
        writer.Raw(",\"repository_subject\":");
        if (repositorySubject is null)
        {
            writer.Raw("null");
        }
        else
        {
            WriteRepositorySubject(writer, repositorySubject);
        }

        writer.Raw(",\"component_subject\":");
        if (componentSubject is null)
        {
            writer.Raw("null");
        }
        else
        {
            WriteAssessment(writer, componentSubject);
        }

        writer.Raw("}");
    }

    private static void WriteBundle(
        CanonicalJsonTextWriter writer,
        EvidenceBundle bundle)
    {
        writer.Raw("{\"schema_version\":");
        writer.Integer(bundle.SchemaVersion);
        writer.Raw(",\"assessment\":");
        WriteAssessment(writer, bundle.Assessment);
        writer.Raw(",\"source_ledgers\":[");
        for (var index = 0; index < bundle.SourceLedgers.Count; index++)
        {
            if (index > 0)
            {
                writer.Raw(",");
            }

            var source = bundle.SourceLedgers[index];
            writer.Raw("{\"source_ledger_sha256\":");
            writer.String(source.SourceLedgerSha256);
            writer.Raw(",\"ledger\":");
            WriteSourceLedger(writer, source.Ledger);
            writer.Raw("}");
        }

        writer.Raw("],\"selection\":[");
        for (var index = 0; index < bundle.Selection.Count; index++)
        {
            if (index > 0)
            {
                writer.Raw(",");
            }

            var selection = bundle.Selection[index];
            writer.Raw("{\"display_order\":");
            writer.Integer(selection.DisplayOrder);
            writer.Raw(",\"source_ledger_sha256\":");
            writer.String(selection.SourceLedgerSha256);
            writer.Raw(",\"evidence_id\":");
            writer.String(selection.EvidenceId);
            writer.Raw("}");
        }

        writer.Raw("]}");
    }

    private static void WriteValidationInputManifest(
        CanonicalJsonTextWriter writer,
        ValidationInputManifest manifest)
    {
        writer.Raw("{\"schema_version\":");
        writer.Integer(manifest.SchemaVersion);
        writer.Raw(",\"files\":[");
        for (var index = 0; index < manifest.Files.Count; index++)
        {
            if (index > 0)
            {
                writer.Raw(",");
            }

            var file = manifest.Files[index];
            writer.Raw("{\"path\":");
            writer.String(file.Path);
            writer.Raw(",\"sha256\":");
            WriteDigest(writer, file.Sha256);
            writer.Raw("}");
        }

        writer.Raw("]}");
    }

    private static void WriteStringArray(
        CanonicalJsonTextWriter writer,
        IReadOnlyList<string> values)
    {
        writer.Raw("[");
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                writer.Raw(",");
            }

            writer.String(values[index]);
        }

        writer.Raw("]");
    }

    private static byte[] BuildDomainPreimage(
        string domain,
        ReadOnlySpan<byte> first,
        ReadOnlySpan<byte> second = default)
    {
        var domainBytes = Encoding.ASCII.GetBytes(domain);
        var length = domainBytes.Length + 1 + first.Length;
        if (!second.IsEmpty)
        {
            length += 1 + second.Length;
        }

        var result = new byte[length];
        var offset = 0;
        domainBytes.CopyTo(result, offset);
        offset += domainBytes.Length + 1;
        first.CopyTo(result.AsSpan(offset));
        offset += first.Length;
        if (!second.IsEmpty)
        {
            offset++;
            second.CopyTo(result.AsSpan(offset));
        }

        return result;
    }

    private static string ComputeDomainDigest(string domain, ReadOnlySpan<byte> content)
    {
        return ComputeSha256(BuildDomainPreimage(domain, content));
    }

    private static (
        RepositoryLedgerSubject? RepositorySubject,
        ExactAssessmentIdentity? ComponentSubject) NormalizeSubject(
            string ledgerKind,
            RepositoryLedgerSubject? repositorySubject,
            ExactAssessmentIdentity? componentSubject)
    {
        return ledgerKind switch
        {
            "repository" when repositorySubject is not null &&
                componentSubject is null =>
                (EvidenceIdentity.NormalizeRepositorySubject(repositorySubject), null),
            "component" when repositorySubject is null &&
                componentSubject is not null =>
                (null, EvidenceIdentity.NormalizeAssessment(componentSubject)),
            _ => throw new InvalidDataException(
                "EVID007: stable evidence identity requires one canonical ledger subject."),
        };
    }

    private static void RequireCanonicalBytes(
        ReadOnlySpan<byte> actual,
        ReadOnlySpan<byte> expected,
        string artifact)
    {
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidDataException(
                $"EVID001: persisted {artifact} bytes are not canonical.");
        }
    }

    private static void ValidateValidationInputManifest(
        ValidationInputManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (manifest.SchemaVersion != EvidenceSchemaVersion ||
            manifest.Files.Count == 0)
        {
            throw new InvalidDataException(
                "EVID001: validation-input manifest requires schema 1 and files.");
        }

        string? previousPath = null;
        foreach (var file in manifest.Files)
        {
            if (file.Path.Length == 0 ||
                !file.Path.IsNormalized(NormalizationForm.FormC) ||
                !string.Equals(file.Path, file.Path.Trim(), StringComparison.Ordinal) ||
                file.Path.StartsWith('/', StringComparison.Ordinal) ||
                file.Path.Contains('\\', StringComparison.Ordinal) ||
                file.Path.Contains("//", StringComparison.Ordinal) ||
                file.Path.Split('/').Any(segment =>
                    segment.Length == 0 ||
                    segment is "." or ".."))
            {
                throw new InvalidDataException(
                    $"EVID001: validation-input path '{file.Path}' is not canonical.");
            }

            if (previousPath is not null &&
                string.CompareOrdinal(previousPath, file.Path) >= 0)
            {
                throw new InvalidDataException(
                    "EVID001: validation-input files must be sorted and unique.");
            }

            EvidenceIdentity.ValidateDigest(file.Sha256, "validation input sha256");
            previousPath = file.Path;
        }
    }

    private static void RequireObjectProperties(
        JsonElement element,
        params string[] expectedNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("EVID001: expected a JSON object.");
        }

        var actual = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (!actual.SequenceEqual(expectedNames, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                "EVID001: object properties are missing, duplicated, unknown, or out of order. " +
                $"Expected [{string.Join(", ", expectedNames)}], " +
                $"found [{string.Join(", ", actual)}].");
        }
    }

    private static JsonElement GetRequiredObject(JsonElement element, string name)
    {
        var value = element.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"EVID001: '{name}' must be an object.");
        }

        return value;
    }

    private static JsonElement GetRequiredArray(JsonElement element, string name)
    {
        var value = element.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"EVID001: '{name}' must be an array.");
        }

        return value;
    }

    private static string GetRequiredString(JsonElement element, string name)
    {
        return GetStringValue(element.GetProperty(name));
    }

    private static string GetStringValue(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException("EVID001: expected a JSON string.");
        }

        return element.GetString()!;
    }

    private static string? GetNullableString(JsonElement element, string name)
    {
        var value = element.GetProperty(name);
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => throw new InvalidDataException(
                $"EVID001: '{name}' must be a string or null."),
        };
    }

    private static T? GetNullableObject<T>(
        JsonElement element,
        string name,
        Func<JsonElement, T> parser)
        where T : class
    {
        var value = element.GetProperty(name);
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Object => parser(value),
            _ => throw new InvalidDataException(
                $"EVID001: '{name}' must be an object or null."),
        };
    }

    private static int GetRequiredInt32(JsonElement element, string name)
    {
        var value = element.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw new InvalidDataException($"EVID001: '{name}' must be an Int32.");
        }

        return result;
    }

    private sealed class Sha256EvidenceHasher : IEvidenceHasher
    {
        public byte[] Hash(ReadOnlySpan<byte> content)
        {
            return SHA256.HashData(content);
        }
    }

    private sealed class CanonicalJsonTextWriter
    {
        private readonly StringBuilder _builder = new();

        internal void Raw(string value)
        {
            _builder.Append(value);
        }

        internal void Integer(int value)
        {
            _builder.Append(
                value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        internal void String(string value)
        {
            _builder.Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"':
                        _builder.Append("\\\"");
                        break;
                    case '\\':
                        _builder.Append("\\\\");
                        break;
                    case <= '\u001f':
                        _builder.Append("\\u00");
                        _builder.Append(
                            ((int)character).ToString(
                                "x2",
                                System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    default:
                        _builder.Append(character);
                        break;
                }
            }

            _builder.Append('"');
        }

        internal byte[] ToArray()
        {
            return StrictUtf8.GetBytes(_builder.ToString());
        }
    }
}
