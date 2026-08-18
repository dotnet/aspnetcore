// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class EvidenceLedgerValidator
{
    internal static void ValidateSourceLedger(EvidenceSourceLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        if (ledger.SchemaVersion != CanonicalEvidenceJson.EvidenceSchemaVersion)
        {
            throw new InvalidDataException(
                $"EVID001: unsupported evidence schema version {ledger.SchemaVersion}.");
        }

        switch (ledger.LedgerKind)
        {
            case "repository":
                if (ledger.RepositorySubject is null ||
                    ledger.ComponentSubject is not null)
                {
                    throw new InvalidDataException(
                        "EVID007: repository ledger requires only repository_subject.");
                }

                var repositorySubject =
                    EvidenceIdentity.NormalizeRepositorySubject(ledger.RepositorySubject);
                if (repositorySubject != ledger.RepositorySubject)
                {
                    throw new InvalidDataException(
                        "EVID001: repository ledger subject is not canonical.");
                }

                break;
            case "component":
                if (ledger.RepositorySubject is not null ||
                    ledger.ComponentSubject is null)
                {
                    throw new InvalidDataException(
                        "EVID007: component ledger requires only component_subject.");
                }

                EvidenceIdentity.ValidateAssessment(ledger.ComponentSubject);
                break;
            default:
                throw new InvalidDataException(
                    $"EVID007: invalid source-ledger kind '{ledger.LedgerKind}'.");
        }

        if (ledger.Records.Count == 0)
        {
            throw new InvalidDataException(
                "EVID008: source ledger requires at least one evidence record.");
        }

        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        string? previousIdentifier = null;
        foreach (var record in ledger.Records)
        {
            EvidenceIdentity.ValidateStableIdentifier(record.StableId);
            if (!identifiers.Add(record.StableId))
            {
                throw new InvalidDataException(
                    $"EVID003: duplicate stable evidence ID {record.StableId}.");
            }

            if (previousIdentifier is not null &&
                string.CompareOrdinal(previousIdentifier, record.StableId) >= 0)
            {
                throw new InvalidDataException(
                    "EVID001: source-ledger records must be sorted by stable ID.");
            }

            previousIdentifier = record.StableId;
            var draft = new EvidenceRecordDraft(
                record.Claim,
                record.Applicability,
                record.Provenance,
                record.Supersedes);
            var normalized = EvidenceIdentity.NormalizeRecordDraft(draft);
            if (!RecordPayloadEquals(draft, normalized))
            {
                throw new InvalidDataException(
                    $"EVID001: evidence record {record.StableId} is not canonical.");
            }

            ValidateRecordScope(ledger, record);
            var expectedIdentifier = CanonicalEvidenceJson.ComputeStableId(
                ledger.LedgerKind,
                ledger.RepositorySubject,
                ledger.ComponentSubject,
                normalized);
            if (!string.Equals(
                expectedIdentifier,
                record.StableId,
                StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"EVID002: record ID {record.StableId} differs from " +
                    $"recomputed {expectedIdentifier}.");
            }
        }

        ValidateSupersession(ledger);
    }

    internal static void ValidateBundle(EvidenceBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        if (bundle.SchemaVersion != CanonicalEvidenceJson.EvidenceSchemaVersion)
        {
            throw new InvalidDataException(
                $"EVID001: unsupported evidence bundle version {bundle.SchemaVersion}.");
        }

        EvidenceIdentity.ValidateAssessment(bundle.Assessment);
        if (bundle.SourceLedgers.Count == 0)
        {
            throw new InvalidDataException(
                "EVID008: evidence bundle requires source ledgers.");
        }

        var sourceByDigest = new Dictionary<string, EmbeddedSourceLedger>(
            StringComparer.Ordinal);
        var evidenceSources = new Dictionary<string, string>(StringComparer.Ordinal);
        string? previousDigest = null;
        foreach (var source in bundle.SourceLedgers)
        {
            EvidenceIdentity.ValidateDigest(
                new Sha256Digest("sha256", source.SourceLedgerSha256),
                "source_ledger_sha256");
            ValidateSourceLedger(source.Ledger);
            ValidateCompatibility(bundle.Assessment, source.Ledger);
            var expectedDigest =
                CanonicalEvidenceJson.ComputeSourceLedgerSha256(source.Ledger);
            if (!string.Equals(
                source.SourceLedgerSha256,
                expectedDigest,
                StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"EVID009: source-ledger digest {source.SourceLedgerSha256} " +
                    $"differs from recomputed {expectedDigest}.");
            }

            if (!sourceByDigest.TryAdd(source.SourceLedgerSha256, source))
            {
                throw new InvalidDataException(
                    $"EVID008: duplicate source-ledger digest " +
                    source.SourceLedgerSha256);
            }

            if (previousDigest is not null &&
                string.CompareOrdinal(previousDigest, source.SourceLedgerSha256) >= 0)
            {
                throw new InvalidDataException(
                    "EVID001: embedded source ledgers must be sorted by digest.");
            }

            previousDigest = source.SourceLedgerSha256;
            foreach (var record in source.Ledger.Records)
            {
                if (!evidenceSources.TryAdd(
                    record.StableId,
                    source.SourceLedgerSha256))
                {
                    throw new InvalidDataException(
                        $"EVID003: stable evidence ID {record.StableId} appears in " +
                        "multiple embedded source ledgers.");
                }
            }
        }

        if (bundle.Selection.Count == 0)
        {
            throw new InvalidDataException(
                "EVID008: evidence bundle selection must not be empty.");
        }

        var pairs = new HashSet<string>(StringComparer.Ordinal);
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < bundle.Selection.Count; index++)
        {
            var selection = bundle.Selection[index];
            if (selection.DisplayOrder != index + 1)
            {
                throw new InvalidDataException(
                    "EVID008: display_order must be contiguous from 1.");
            }

            EvidenceIdentity.ValidateStableIdentifier(selection.EvidenceId);
            var pair = selection.SourceLedgerSha256 + "\0" + selection.EvidenceId;
            if (!pairs.Add(pair) || !identifiers.Add(selection.EvidenceId))
            {
                throw new InvalidDataException(
                    "EVID008: duplicate evidence selection pair or ID.");
            }

            if (!sourceByDigest.TryGetValue(
                selection.SourceLedgerSha256,
                out var source))
            {
                throw new InvalidDataException(
                    $"EVID008: selection references unknown source ledger " +
                    selection.SourceLedgerSha256);
            }

            var matches = source.Ledger.Records.Count(record =>
                string.Equals(
                    record.StableId,
                    selection.EvidenceId,
                    StringComparison.Ordinal));
            if (matches != 1)
            {
                throw new InvalidDataException(
                    $"EVID008: selection {selection.EvidenceId} resolves to " +
                    $"{matches} records in its source ledger.");
            }
        }

        ValidateSelectionSupersession(bundle, identifiers);
    }

    internal static void ValidateCompatibility(
        ExactAssessmentIdentity assessment,
        EvidenceSourceLedger ledger)
    {
        switch (ledger.LedgerKind)
        {
            case "repository":
                var repositorySubject = ledger.RepositorySubject!;
                if (repositorySubject.Repository != assessment.Repository)
                {
                    throw new InvalidDataException(
                        "EVID006: repository identity differs from assessment.");
                }

                if (repositorySubject.Artifact != assessment.Artifact)
                {
                    throw new InvalidDataException(
                        "EVID006: artifact/package identity differs from assessment.");
                }

                if (repositorySubject.Artifact.Mode == "source-only" &&
                    !string.Equals(
                        repositorySubject.ComponentId,
                        assessment.ComponentId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "EVID007: source-only repository ledger component_id " +
                        "differs from assessment.");
                }

                break;
            case "component":
                if (ledger.ComponentSubject != assessment)
                {
                    throw new InvalidDataException(
                        "EVID007: component ledger identity differs from assessment.");
                }

                break;
            default:
                throw new InvalidDataException(
                    $"EVID007: invalid source-ledger kind '{ledger.LedgerKind}'.");
        }
    }

    private static void ValidateRecordScope(
        EvidenceSourceLedger ledger,
        EvidenceRecord record)
    {
        if (ledger.LedgerKind == "repository" &&
            (record.Applicability.Scope != "repository-wide" ||
            record.Applicability.ComponentId is not null))
        {
            throw new InvalidDataException(
                $"EVID007: repository ledger record {record.StableId} must be " +
                "repository-wide.");
        }

        if (ledger.LedgerKind == "component" &&
            (record.Applicability.Scope != "component-specific" ||
            !string.Equals(
                record.Applicability.ComponentId,
                ledger.ComponentSubject!.ComponentId,
                StringComparison.Ordinal)))
        {
            throw new InvalidDataException(
                $"EVID007: component ledger record {record.StableId} must match " +
                "the exact component ID.");
        }
    }

    private static void ValidateSupersession(EvidenceSourceLedger ledger)
    {
        var records = ledger.Records.ToDictionary(
            record => record.StableId,
            StringComparer.Ordinal);
        foreach (var record in ledger.Records)
        {
            foreach (var predecessorId in record.Supersedes)
            {
                if (!records.TryGetValue(predecessorId, out var predecessor))
                {
                    throw new InvalidDataException(
                        $"EVID010: {record.StableId} supersedes missing " +
                        predecessorId);
                }

                if (record.Applicability != predecessor.Applicability)
                {
                    throw new InvalidDataException(
                        $"EVID010: {record.StableId} supersedes evidence with " +
                        "different applicability.");
                }

            }
        }

        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in ledger.Records)
        {
            Visit(record.StableId);
        }

        void Visit(string identifier)
        {
            if (visited.Contains(identifier))
            {
                return;
            }

            if (!visiting.Add(identifier))
            {
                throw new InvalidDataException(
                    $"EVID010: supersession cycle includes {identifier}.");
            }

            foreach (var predecessor in records[identifier].Supersedes)
            {
                Visit(predecessor);
            }

            visiting.Remove(identifier);
            visited.Add(identifier);
        }
    }

    private static void ValidateSelectionSupersession(
        EvidenceBundle bundle,
        IReadOnlySet<string> selectedIdentifiers)
    {
        foreach (var source in bundle.SourceLedgers)
        {
            foreach (var record in source.Ledger.Records)
            {
                if (!selectedIdentifiers.Contains(record.StableId))
                {
                    continue;
                }

                var pending = new Stack<string>(record.Supersedes);
                while (pending.TryPop(out var predecessorId))
                {
                    if (selectedIdentifiers.Contains(predecessorId))
                    {
                        throw new InvalidDataException(
                            $"EVID010: selection contains {record.StableId} and " +
                            $"superseded {predecessorId}.");
                    }

                    var predecessor = source.Ledger.Records.Single(candidate =>
                        string.Equals(
                            candidate.StableId,
                            predecessorId,
                            StringComparison.Ordinal));
                    foreach (var ancestor in predecessor.Supersedes)
                    {
                        pending.Push(ancestor);
                    }
                }
            }
        }
    }

    private static bool RecordPayloadEquals(
        EvidenceRecordDraft left,
        EvidenceRecordDraft right)
    {
        return string.Equals(left.Claim, right.Claim, StringComparison.Ordinal) &&
            left.Applicability == right.Applicability &&
            left.Provenance == right.Provenance &&
            left.Supersedes.SequenceEqual(right.Supersedes, StringComparer.Ordinal);
    }
}
