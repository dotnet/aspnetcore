// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace BlazorComponentReadiness;

internal static partial class EvidenceIdentity
{
    internal const int MaximumNuspecBytes = 1024 * 1024;
    internal const long MaximumNupkgBytes = 256L * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    [GeneratedRegex("^[0-9a-f]{40}(?:[0-9a-f]{24})?$", RegexOptions.CultureInvariant)]
    private static partial Regex CommitPattern();

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9._-]{0,98}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex PackageIdentifierPattern();

    [GeneratedRegex("^[A-Za-z0-9](?:[A-Za-z0-9._+\\-]{0,254}[A-Za-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex PackageVersionPattern();

    [GeneratedRegex("^EV1-[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex EvidenceIdentifierPattern();

    [GeneratedRegex("^[A-Za-z0-9 ._\\-:/+=,@()]{1,256}$", RegexOptions.CultureInvariant)]
    private static partial Regex CommandProbePattern();

    [GeneratedRegex("^\\d+[.)]\\s", RegexOptions.CultureInvariant)]
    private static partial Regex OrderedListPattern();

    internal static ExactAssessmentIdentity NormalizeAssessment(
        ExactAssessmentIdentity assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        return new ExactAssessmentIdentity(
            NormalizeRepository(assessment.Repository),
            NormalizeArtifact(assessment.Artifact),
            NormalizeText(
                assessment.ComponentId,
                "component_id",
                maximumUtf8Bytes: 256));
    }

    internal static RepositoryLedgerSubject NormalizeRepositorySubject(
        RepositoryLedgerSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var artifact = NormalizeArtifact(subject.Artifact);
        var componentId = artifact.Mode switch
        {
            "released-package" when subject.ComponentId is null => null,
            "released-package" => throw new InvalidDataException(
                "EVID007: released-package repository ledger requires component_id null."),
            "source-only" when subject.ComponentId is not null => NormalizeText(
                subject.ComponentId,
                "component_id",
                maximumUtf8Bytes: 256),
            "source-only" => throw new InvalidDataException(
                "EVID007: source-only repository ledger requires component_id."),
            _ => throw new InvalidOperationException(
                $"Unexpected normalized artifact mode '{artifact.Mode}'."),
        };
        return new RepositoryLedgerSubject(
            NormalizeRepository(subject.Repository),
            artifact,
            componentId);
    }

    internal static EvidenceRecordDraft NormalizeRecordDraft(
        EvidenceRecordDraft record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var supersedes = record.Supersedes
            .Select(NormalizeEvidenceIdentifier)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (supersedes.Length != supersedes.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidDataException(
                "EVID010: supersedes contains duplicate evidence IDs.");
        }

        var claim = NormalizeText(
            record.Claim,
            "claim",
            maximumUtf8Bytes: 512);
        if (claim.Contains('|', StringComparison.Ordinal) ||
            claim.StartsWith("- ", StringComparison.Ordinal) ||
            claim.StartsWith("* ", StringComparison.Ordinal) ||
            claim.StartsWith("+ ", StringComparison.Ordinal) ||
            claim.StartsWith("#", StringComparison.Ordinal) ||
            OrderedListPattern().IsMatch(claim))
        {
            throw new InvalidDataException(
                "EVID004: claim must be one syntactically atomic non-Markdown sentence.");
        }

        return new EvidenceRecordDraft(
            claim,
            NormalizeApplicability(record.Applicability),
            NormalizeProvenance(record.Provenance),
            supersedes);
    }

    internal static void ValidateAssessment(ExactAssessmentIdentity assessment)
    {
        var normalized = NormalizeAssessment(assessment);
        if (normalized != assessment)
        {
            throw new InvalidDataException(
                "EVID001: assessment identity is not canonical.");
        }
    }

    internal static string CanonicalizeRepositoryUri(string value)
    {
        value = NormalizeText(value, "repository_uri", maximumUtf8Bytes: 2048);
        RejectMarkdownAndPathHazards(value, "repository_uri");
        var (host, path) = ParseRawHttps(value, allowRootPath: false);
        ValidatePublicDnsHost(host, "repository_uri");
        var segments = path[1..].Split('/');
        if (segments.Length < 2 ||
            segments.Any(segment =>
                segment.Length == 0 ||
                segment is "." or ".."))
        {
            throw new InvalidDataException(
                "EVID005: repository_uri has an invalid path.");
        }

        if (string.Equals(host, "github.com", StringComparison.Ordinal))
        {
            if (segments.Length != 2)
            {
                throw new InvalidDataException(
                    "EVID005: github.com repositories require owner/repository.");
            }

            if (segments[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                segments[1] = segments[1][..^4];
            }

            segments = segments
                .Select(segment => segment.ToLowerInvariant())
                .ToArray();
        }

        if (segments.Any(segment => segment.Length == 0))
        {
            throw new InvalidDataException(
                "EVID005: repository_uri has an empty canonical segment.");
        }

        return $"https://{host}/{string.Join('/', segments)}";
    }

    internal static PackageIdentity ReadPackageIdentity(
        Stream nupkgStream,
        long maximumNupkgBytes = MaximumNupkgBytes)
    {
        ArgumentNullException.ThrowIfNull(nupkgStream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumNupkgBytes);
        if (!nupkgStream.CanRead || !nupkgStream.CanSeek)
        {
            throw new InvalidDataException(
                "EVID006: nupkg input must be a readable seekable stream.");
        }

        if (nupkgStream.Length > maximumNupkgBytes)
        {
            throw new InvalidDataException(
                $"EVID006: nupkg input exceeds {maximumNupkgBytes} bytes.");
        }

        nupkgStream.Position = 0;
        var digest = new Sha256Digest(
            "sha256",
            Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(nupkgStream)));
        nupkgStream.Position = 0;
        using var archive = new ZipArchive(
            nupkgStream,
            ZipArchiveMode.Read,
            leaveOpen: true);
        var nuspecEntries = archive.Entries
            .Where(entry =>
                entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
                !entry.FullName.EndsWith('/', StringComparison.Ordinal))
            .ToArray();
        if (nuspecEntries.Length != 1)
        {
            throw new InvalidDataException(
                $"EVID006: exact nupkg must contain one nuspec; found {nuspecEntries.Length}.");
        }

        var entry = nuspecEntries[0];
        if (entry.Length < 0 || entry.Length > MaximumNuspecBytes)
        {
            throw new InvalidDataException(
                $"EVID006: nuspec exceeds {MaximumNuspecBytes} bytes.");
        }

        using (var stream = entry.Open())
        {
            var nuspecBytes = ReadBounded(stream, MaximumNuspecBytes);
            return ReadPackageIdentityFromNuspec(nuspecBytes, digest);
        }
    }

    internal static PackageIdentity ReadPackageIdentity(byte[] nupkgBytes)
    {
        ArgumentNullException.ThrowIfNull(nupkgBytes);
        using var stream = new MemoryStream(nupkgBytes, writable: false);
        return ReadPackageIdentity(stream);
    }

    internal static byte[] ReadBounded(Stream stream, int maximumBytes)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
        var buffer = new byte[Math.Min(81920, maximumBytes + 1)];
        using var output = new MemoryStream(Math.Min(maximumBytes, 81920));
        var total = 0;
        while (true)
        {
            var requested = Math.Min(buffer.Length, maximumBytes - total + 1);
            var read = stream.Read(buffer, 0, requested);
            if (read == 0)
            {
                return output.ToArray();
            }

            total += read;
            if (total > maximumBytes)
            {
                throw new InvalidDataException(
                    $"EVID006: expanded nuspec exceeds {maximumBytes} bytes.");
            }

            output.Write(buffer, 0, read);
        }
    }

    private static PackageIdentity ReadPackageIdentityFromNuspec(
        byte[] nuspecBytes,
        Sha256Digest digest)
    {
        XDocument document;
        try
        {
            using var memory = new MemoryStream(nuspecBytes, writable: false);
            using var reader = XmlReader.Create(
                memory,
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    MaxCharactersInDocument = MaximumNuspecBytes,
                    XmlResolver = null,
                });
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException(
                $"EVID006: invalid or unsafe nuspec XML: {exception.Message}",
                exception);
        }

        var metadataElements = document.Root?
            .Elements()
            .Where(element =>
                string.Equals(element.Name.LocalName, "metadata", StringComparison.Ordinal))
            .ToArray() ?? [];
        if (metadataElements.Length != 1)
        {
            throw new InvalidDataException(
                $"EVID006: nuspec must contain exactly one metadata element; " +
                $"found {metadataElements.Length}.");
        }

        var metadata = metadataElements[0];
        var packageId = GetSingleMetadataValue(metadata, "id");
        var version = GetSingleMetadataValue(metadata, "version");
        return new PackageIdentity(
            NormalizePackageIdentifier(packageId),
            NormalizePackageVersion(version),
            digest);
    }

    internal static void ValidateDigest(Sha256Digest digest, string name)
    {
        ArgumentNullException.ThrowIfNull(digest);
        if (!string.Equals(digest.Algorithm, "sha256", StringComparison.Ordinal) ||
            digest.Value.Length != 64 ||
            digest.Value.Any(character =>
                character is not (>= '0' and <= '9') and
                not (>= 'a' and <= 'f')))
        {
            throw new InvalidDataException(
                $"EVID005: {name} must be canonical lowercase SHA-256.");
        }
    }

    internal static void ValidateStableIdentifier(string identifier)
    {
        if (!EvidenceIdentifierPattern().IsMatch(identifier))
        {
            throw new InvalidDataException(
                $"EVID002: invalid stable evidence ID '{identifier}'.");
        }
    }

    private static RepositoryIdentity NormalizeRepository(
        RepositoryIdentity repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        var commit = NormalizeText(
            repository.Commit,
            "commit",
            maximumUtf8Bytes: 64);
        if (!CommitPattern().IsMatch(commit))
        {
            throw new InvalidDataException(
                "EVID006: commit must be full lowercase 40- or 64-hex.");
        }

        return new RepositoryIdentity(
            CanonicalizeRepositoryUri(repository.RepositoryUri),
            commit);
    }

    private static ArtifactIdentity NormalizeArtifact(ArtifactIdentity artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        return artifact.Mode switch
        {
            "source-only" when artifact.Package is null =>
                new ArtifactIdentity("source-only", null),
            "released-package" when artifact.Package is not null =>
                new ArtifactIdentity(
                    "released-package",
                    NormalizePackage(artifact.Package)),
            "source-only" => throw new InvalidDataException(
                "EVID006: source-only artifact identity requires package null."),
            "released-package" => throw new InvalidDataException(
                "EVID006: released-package artifact identity requires package."),
            _ => throw new InvalidDataException(
                $"EVID006: invalid artifact mode '{artifact.Mode}'."),
        };
    }

    private static PackageIdentity NormalizePackage(PackageIdentity package)
    {
        ValidateDigest(package.NupkgDigest, "nupkg_sha256");
        return new PackageIdentity(
            NormalizePackageIdentifier(package.PackageId),
            NormalizePackageVersion(package.Version),
            package.NupkgDigest);
    }

    private static string NormalizePackageIdentifier(string value)
    {
        value = NormalizeText(value, "package_id", maximumUtf8Bytes: 100)
            .ToLowerInvariant();
        if (!PackageIdentifierPattern().IsMatch(value))
        {
            throw new InvalidDataException(
                "EVID006: package_id has invalid canonical NuGet ID syntax.");
        }

        return value;
    }

    private static string NormalizePackageVersion(string value)
    {
        value = NormalizeText(value, "version", maximumUtf8Bytes: 256);
        if (!PackageVersionPattern().IsMatch(value))
        {
            throw new InvalidDataException(
                "EVID006: version has invalid bounded nuspec syntax.");
        }

        return value;
    }

    private static EvidenceApplicability NormalizeApplicability(
        EvidenceApplicability applicability)
    {
        ArgumentNullException.ThrowIfNull(applicability);
        return applicability.Scope switch
        {
            "repository-wide" when applicability.ComponentId is null =>
                new EvidenceApplicability("repository-wide", null),
            "component-specific" when applicability.ComponentId is not null =>
                new EvidenceApplicability(
                    "component-specific",
                    NormalizeText(
                        applicability.ComponentId,
                        "component_id",
                        maximumUtf8Bytes: 256)),
            "repository-wide" => throw new InvalidDataException(
                "EVID007: repository-wide evidence requires component_id null."),
            "component-specific" => throw new InvalidDataException(
                "EVID007: component-specific evidence requires component_id."),
            _ => throw new InvalidDataException(
                $"EVID007: invalid evidence scope '{applicability.Scope}'."),
        };
    }

    private static EvidenceProvenance NormalizeProvenance(
        EvidenceProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        if (!string.Equals(
            provenance.Retention,
            "commitment-only",
            StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "EVID005: retention must be exactly 'commitment-only'.");
        }

        ValidateDigest(provenance.ContentDigest, "content_sha256");
        return new EvidenceProvenance(
            provenance.Kind,
            CanonicalizeLocator(provenance.Kind, provenance.Locator),
            NormalizeText(
                provenance.Method,
                "method",
                maximumUtf8Bytes: 512),
            NormalizeTimestamp(provenance.CapturedAtUtc),
            provenance.ContentDigest,
            "commitment-only");
    }

    private static string CanonicalizeLocator(string kind, string value)
    {
        value = NormalizeText(value, "locator", maximumUtf8Bytes: 2048);
        RejectMarkdownAndPathHazards(value, "locator");
        return kind switch
        {
            "public-https" => CanonicalizePublicHttpsLocator(value),
            "repository-path" => CanonicalizeRepositoryPath(value),
            "command-probe" => CanonicalizeCommandProbe(value),
            _ => throw new InvalidDataException(
                $"EVID005: invalid provenance kind '{kind}'."),
        };
    }

    private static string CanonicalizePublicHttpsLocator(string value)
    {
        var (host, path) = ParseRawHttps(value, allowRootPath: true);
        ValidatePublicDnsHost(host, "public-https locator");

        return $"https://{host}{path}";
    }

    private static string CanonicalizeRepositoryPath(string value)
    {
        if (value.StartsWith('/', StringComparison.Ordinal) ||
            value.Contains(':', StringComparison.Ordinal) ||
            value.Contains("//", StringComparison.Ordinal) ||
            value.Split('/').Any(segment =>
                segment.Length == 0 ||
                segment is "." or ".."))
        {
            throw new InvalidDataException(
                "EVID005: repository-path locator must be a normalized relative POSIX path.");
        }

        return value;
    }

    private static string CanonicalizeCommandProbe(string value)
    {
        if (!CommandProbePattern().IsMatch(value))
        {
            throw new InvalidDataException(
                "EVID005: command-probe locator contains unsupported characters.");
        }

        return value;
    }

    private static string NormalizeTimestamp(string value)
    {
        value = NormalizeText(
            value,
            "captured_at_utc",
            maximumUtf8Bytes: 20);
        if (!DateTimeOffset.TryParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var timestamp) ||
            !string.Equals(
                timestamp.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss'Z'",
                    CultureInfo.InvariantCulture),
                value,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "EVID005: captured_at_utc must use canonical UTC-second format.");
        }

        return value;
    }

    private static string NormalizeEvidenceIdentifier(string value)
    {
        value = NormalizeText(
            value,
            "evidence_id",
            maximumUtf8Bytes: 68);
        ValidateStableIdentifier(value);
        return value;
    }

    private static string NormalizeText(
        string value,
        string name,
        int maximumUtf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(value);
        _ = StrictUtf8.GetBytes(value);
        if (value.Length == 0 ||
            !value.IsNormalized(NormalizationForm.FormC) ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            StrictUtf8.GetByteCount(value) > maximumUtf8Bytes ||
            ContainsDisallowedCharacter(value))
        {
            throw new InvalidDataException(
                $"EVID005: {name} is empty, non-NFC, untrimmed, too long, or contains controls.");
        }

        return value;
    }

    private static void RejectMarkdownAndPathHazards(string value, string name)
    {
        if (value.Contains('`', StringComparison.Ordinal) ||
            value.Contains('|', StringComparison.Ordinal) ||
            value.Contains('\\', StringComparison.Ordinal) ||
            value.Contains('\r', StringComparison.Ordinal) ||
            value.Contains('\n', StringComparison.Ordinal) ||
            value.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(
                value,
                "^(?:[A-Za-z]:/|/)",
                RegexOptions.CultureInvariant))
        {
            throw new InvalidDataException(
                $"EVID005: {name} contains a forbidden delimiter, path, or URI form.");
        }
    }

    private static (string Host, string Path) ParseRawHttps(
        string value,
        bool allowRootPath)
    {
        if (value.Contains('%', StringComparison.Ordinal) ||
            value.Contains('?', StringComparison.Ordinal) ||
            value.Contains('#', StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "EVID005: canonical HTTPS identity forbids percent encoding, query, and fragment.");
        }

        var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd <= 0 ||
            !string.Equals(
                value[..schemeEnd],
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "EVID005: canonical HTTPS identity requires the HTTPS scheme.");
        }

        var authorityStart = schemeEnd + 3;
        var authorityEnd = value.IndexOf('/', authorityStart);
        if (authorityEnd < 0)
        {
            authorityEnd = value.Length;
        }

        var authority = value[authorityStart..authorityEnd];
        if (authority.Length == 0 ||
            authority.Contains('@', StringComparison.Ordinal) ||
            authority.Contains(':', StringComparison.Ordinal) ||
            !Uri.TryCreate(
                $"https://{authority}/",
                UriKind.Absolute,
                out var authorityUri))
        {
            throw new InvalidDataException(
                "EVID005: canonical HTTPS identity has an invalid authority.");
        }

        var host = authorityUri.IdnHost.ToLowerInvariant();
        if (host.Length == 0 ||
            host.EndsWith('.', StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "EVID005: canonical HTTPS host is empty or ends with a root-label dot.");
        }

        var path = authorityEnd == value.Length
            ? string.Empty
            : value[authorityEnd..];
        if (path.Length == 0)
        {
            if (!allowRootPath)
            {
                throw new InvalidDataException(
                    "EVID005: repository_uri requires a nonempty path.");
            }

            path = "/";
        }

        ValidateRawHttpsPathCharacters(path);
        if (!path.StartsWith('/', StringComparison.Ordinal) ||
            (!allowRootPath && path == "/") ||
            (path.Length > 1 && path.EndsWith('/', StringComparison.Ordinal)) ||
            path.Contains("//", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "EVID005: canonical HTTPS path is empty, repeated, or has a trailing slash.");
        }

        if (path.Length > 1 &&
            path[1..].Split('/').Any(segment =>
                segment.Length == 0 ||
                segment is "." or ".."))
        {
            throw new InvalidDataException(
                "EVID005: canonical HTTPS path contains an empty or dot segment.");
        }

        return (host, path);
    }

    private static void ValidateRawHttpsPathCharacters(string path)
    {
        const string AllowedAsciiPunctuation = "-._~!$&'()*+,;=:@/";
        foreach (var rune in path.EnumerateRunes())
        {
            if (rune.IsAscii)
            {
                var character = (char)rune.Value;
                if (!char.IsAsciiLetterOrDigit(character) &&
                    !AllowedAsciiPunctuation.Contains(
                        character,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"EVID005: canonical HTTPS path contains forbidden " +
                        $"ASCII character U+{rune.Value:X4}.");
                }

                continue;
            }

            if (Rune.IsWhiteSpace(rune) ||
                Rune.GetUnicodeCategory(rune) is
                    UnicodeCategory.SpaceSeparator or
                    UnicodeCategory.LineSeparator or
                    UnicodeCategory.ParagraphSeparator)
            {
                throw new InvalidDataException(
                    $"EVID005: canonical HTTPS path contains Unicode whitespace " +
                    $"or separator U+{rune.Value:X4}.");
            }
        }
    }

    private static void ValidatePublicDnsHost(string host, string name)
    {
        if (IPAddress.TryParse(host, out _) ||
            !host.Contains('.', StringComparison.Ordinal) ||
            host.Length > 253 ||
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"EVID005: {name} requires a public DNS or IDN hostname.");
        }

        var labels = host.Split('.');
        if (labels.Any(label =>
                label.Length is < 1 or > 63 ||
                !char.IsAsciiLetterOrDigit(label[0]) ||
                !char.IsAsciiLetterOrDigit(label[^1]) ||
                label.Any(character =>
                    !char.IsAsciiLetterOrDigit(character) &&
                    character != '-')) ||
            labels[^1].All(char.IsAsciiDigit))
        {
            throw new InvalidDataException(
                $"EVID005: {name} contains an invalid DNS label.");
        }
    }

    private static bool ContainsDisallowedCharacter(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (rune.Value <= 0x1f ||
                rune.Value is >= 0x7f and <= 0x9f ||
                (Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format &&
                rune.Value is not 0x200c and not 0x200d))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSingleMetadataValue(XElement metadata, string name)
    {
        var elements = metadata
            .Elements()
            .Where(element =>
                string.Equals(element.Name.LocalName, name, StringComparison.Ordinal))
            .ToArray();
        if (elements.Length != 1)
        {
            throw new InvalidDataException(
                $"EVID006: nuspec metadata must contain exactly one {name}.");
        }

        return elements[0].Value.Trim().Normalize(NormalizationForm.FormC);
    }
}
