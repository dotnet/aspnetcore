// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace BlazorComponentReadiness;

internal static partial class EmbeddedDigestValidator
{
    internal static IEnumerable<string> Validate(
        ReportSnapshot document,
        ReportSnapshot evidenceBundleSnapshot,
        EvidenceBundle bundle,
        RubricSnapshot rubric,
        IEnumerable<ReadOnlyMemory<byte>> validationInputs,
        ReportSnapshot? sourceReport = null)
    {
        var allowed = BuildAllowedDigests(
            evidenceBundleSnapshot,
            bundle,
            rubric,
            validationInputs,
            sourceReport);
        foreach (Match match in Sha256Regex().Matches(document.Content))
        {
            if (!allowed.Contains(match.Value))
            {
                yield return
                    $"PROV001: {Path.GetFileName(document.Path)} embeds unbound " +
                    $"SHA-256 digest {match.Value}.";
            }
        }
    }

    private static HashSet<string> BuildAllowedDigests(
        ReportSnapshot evidenceBundleSnapshot,
        EvidenceBundle bundle,
        RubricSnapshot rubric,
        IEnumerable<ReadOnlyMemory<byte>> validationInputs,
        ReportSnapshot? sourceReport)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            CanonicalEvidenceJson.ComputeSha256(evidenceBundleSnapshot.Bytes.Span),
            rubric.Sha256,
            CanonicalEvidenceJson.ComputeAssessmentSha256(bundle.Assessment),
            CanonicalEvidenceJson.ComputeSha256(
                CanonicalEvidenceJson.SerializeAssessment(bundle.Assessment)),
        };
        if (sourceReport is not null)
        {
            allowed.Add(CanonicalEvidenceJson.ComputeSha256(sourceReport.Bytes.Span));
        }

        foreach (var bytes in validationInputs)
        {
            allowed.Add(CanonicalEvidenceJson.ComputeSha256(bytes.Span));
        }

        var packageDigest =
            bundle.Assessment.Artifact.Package?.NupkgDigest.Value;
        if (packageDigest is not null)
        {
            allowed.Add(packageDigest);
        }

        foreach (var source in bundle.SourceLedgers)
        {
            allowed.Add(source.SourceLedgerSha256);
            foreach (var record in source.Ledger.Records)
            {
                allowed.Add(record.StableId[4..]);
                allowed.Add(record.Provenance.ContentDigest.Value);
                foreach (var superseded in record.Supersedes)
                {
                    allowed.Add(superseded[4..]);
                }
            }
        }

        return allowed;
    }

    [GeneratedRegex(
        @"(?<![0-9a-f])(?<digest>[0-9a-f]{64})(?![0-9a-f])",
        RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
