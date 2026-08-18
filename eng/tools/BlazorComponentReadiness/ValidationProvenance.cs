// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace BlazorComponentReadiness;

internal static class ValidationProvenance
{
    internal const int MaximumProvenanceInputCount = 32;
    internal const long MaximumProvenanceInputBytes =
        FileSystemUtilities.MaximumSerializedArtifactBytes;

    internal static IReadOnlyDictionary<string, ReadOnlyMemory<byte>>
        ReadOverlaySnapshots(
            SkillLayout layout,
            IEnumerable<string> overlayNames)
    {
        return overlayNames
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                name => name,
                name =>
                {
                    if (!layout.OverlayPaths.TryGetValue(name, out var path))
                    {
                        throw new InvalidDataException(
                            $"Unknown overlay '{name}'.");
                    }

                    return (ReadOnlyMemory<byte>)File.ReadAllBytes(path);
                },
                StringComparer.Ordinal);
    }

    internal static ValidationInputManifest BuildManifest(
        RubricSnapshot rubric,
        IEnumerable<string> selectedOverlays,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> overlaySnapshots,
        ReportSnapshot? sharedRowProjectionSnapshot = null,
        IReadOnlyList<ReportSnapshot>? provenanceInputSnapshots = null)
    {
        var files = new List<ValidationInput>
        {
            new(
                "references/checklist.md",
                new Sha256Digest("sha256", rubric.Sha256)),
        };
        foreach (var overlay in selectedOverlays
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            if (!overlaySnapshots.TryGetValue(overlay, out var bytes))
            {
                throw new InvalidDataException(
                    $"Missing validation snapshot for overlay '{overlay}'.");
            }

            files.Add(new ValidationInput(
                OverlayRelativePath(overlay),
                new Sha256Digest(
                    "sha256",
                    CanonicalEvidenceJson.ComputeSha256(bytes.Span))));
        }

        if (sharedRowProjectionSnapshot is not null)
        {
            files.Add(new ValidationInput(
                "shared-row-projection.json",
                new Sha256Digest(
                    "sha256",
                    CanonicalEvidenceJson.ComputeSha256(
                        sharedRowProjectionSnapshot.Bytes.Span))));
        }

        for (var index = 0;
            index < (provenanceInputSnapshots?.Count ?? 0);
            index++)
        {
            var snapshot = provenanceInputSnapshots![index];
            files.Add(new ValidationInput(
                ProvenanceInputRelativePath(index),
                new Sha256Digest(
                        "sha256",
                        CanonicalEvidenceJson.ComputeSha256(snapshot.Bytes.Span))));
        }

        return new ValidationInputManifest(
            CanonicalEvidenceJson.EvidenceSchemaVersion,
            files.OrderBy(file => file.Path, StringComparer.Ordinal).ToArray());
    }

    internal static IReadOnlyList<ReportSnapshot>
        ReadProvenanceInputSnapshots(
            IReadOnlyList<string> paths,
            int maximumCount = MaximumProvenanceInputCount,
            long maximumAggregateBytes = MaximumProvenanceInputBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumCount);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumAggregateBytes);
        if (paths.Count > maximumCount)
        {
            throw new InvalidDataException(
                $"PROV003: at most {maximumCount} provenance inputs may be supplied.");
        }

        var snapshots = new List<ReportSnapshot>(paths.Count);
        var remainingBytes = maximumAggregateBytes;
        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (new FileInfo(fullPath).Length > remainingBytes)
            {
                throw new InvalidDataException(
                    $"PROV003: provenance inputs exceed the aggregate " +
                    $"{maximumAggregateBytes}-byte limit.");
            }

            var snapshot = ScorecardValidator.ReadReportSnapshot(
                fullPath,
                remainingBytes);
            snapshots.Add(snapshot);
            remainingBytes -= snapshot.Bytes.Length;
        }

        return snapshots;
    }

    internal static string ComputeValidatorSha256()
    {
        var location = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrWhiteSpace(location) || !File.Exists(location))
        {
            throw new InvalidDataException(
                "RECEIPT003: running validator assembly bytes are unavailable.");
        }

        using var stream = new FileStream(
            location,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(stream));
    }

    internal static string OverlayRelativePath(string overlay)
    {
        return overlay switch
        {
            "scaffolder" => "references/overlays/scaffolder.md",
            "ai-skill" => "references/overlays/ai-skill.md",
            _ => throw new InvalidDataException($"Unknown overlay '{overlay}'."),
        };
    }

    internal static string OverlayNameFromRelativePath(string path)
    {
        return path switch
        {
            "references/overlays/scaffolder.md" => "scaffolder",
            "references/overlays/ai-skill.md" => "ai-skill",
            _ => throw new InvalidDataException(
                $"RECEIPT005: unknown validation input path '{path}'."),
        };
    }

    internal static string ProvenanceInputRelativePath(int index)
    {
        return $"provenance-inputs/{index:D4}";
    }

    internal static bool IsProvenanceInputRelativePath(string path)
    {
        return path.StartsWith(
            "provenance-inputs/",
            StringComparison.Ordinal);
    }
}
