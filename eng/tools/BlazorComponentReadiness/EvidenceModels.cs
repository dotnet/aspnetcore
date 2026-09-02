// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal sealed record Sha256Digest(
    string Algorithm,
    string Value);

internal sealed record RepositoryIdentity(
    string RepositoryUri,
    string Commit);

internal sealed record PackageIdentity(
    string PackageId,
    string Version,
    Sha256Digest NupkgDigest);

internal sealed record ArtifactIdentity(
    string Mode,
    PackageIdentity? Package);

internal sealed record ExactAssessmentIdentity(
    RepositoryIdentity Repository,
    ArtifactIdentity Artifact,
    string ComponentId);

internal sealed record RepositoryLedgerSubject(
    RepositoryIdentity Repository,
    ArtifactIdentity Artifact,
    string? ComponentId);

internal sealed record EvidenceApplicability(
    string Scope,
    string? ComponentId);

internal sealed record EvidenceProvenance(
    string Kind,
    string Locator,
    string Method,
    string CapturedAtUtc,
    Sha256Digest ContentDigest,
    string Retention);

internal sealed record EvidenceRecordDraft(
    string Claim,
    EvidenceApplicability Applicability,
    EvidenceProvenance Provenance,
    IReadOnlyList<string> Supersedes);

internal sealed record EvidenceDraftDocument(
    int SchemaVersion,
    IReadOnlyList<EvidenceRecordDraft> Records);

internal sealed record EvidenceRecord(
    string StableId,
    string Claim,
    EvidenceApplicability Applicability,
    EvidenceProvenance Provenance,
    IReadOnlyList<string> Supersedes);

internal sealed record EvidenceSourceLedger(
    int SchemaVersion,
    string LedgerKind,
    RepositoryLedgerSubject? RepositorySubject,
    ExactAssessmentIdentity? ComponentSubject,
    IReadOnlyList<EvidenceRecord> Records);

internal sealed record EmbeddedSourceLedger(
    string SourceLedgerSha256,
    EvidenceSourceLedger Ledger);

internal sealed record EvidenceSelection(
    int DisplayOrder,
    string SourceLedgerSha256,
    string EvidenceId);

internal sealed record EvidenceBundle(
    int SchemaVersion,
    ExactAssessmentIdentity Assessment,
    IReadOnlyList<EmbeddedSourceLedger> SourceLedgers,
    IReadOnlyList<EvidenceSelection> Selection);

internal sealed record ValidationInput(
    string Path,
    Sha256Digest Sha256);

internal sealed record ValidationInputManifest(
    int SchemaVersion,
    IReadOnlyList<ValidationInput> Files);

internal interface IEvidenceHasher
{
    byte[] Hash(ReadOnlySpan<byte> content);
}
