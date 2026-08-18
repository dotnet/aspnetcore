// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class EvidenceLedgerCommand
{
    private const int MaximumJsonInputBytes = 4 * 1024 * 1024;

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            WriteUsage(error);
            return 1;
        }

        try
        {
            return args[0] switch
            {
                "build" => Build(args[1..], output, error),
                "validate" => Validate(args[1..], output, error),
                "bundle" => Bundle(args[1..], output, error),
                _ => UnknownOperation(args[0], error),
            };
        }
        catch (Exception exception) when (
            exception is IOException or
            InvalidDataException or
            UnauthorizedAccessException)
        {
            error.WriteLine($"ERROR: {exception.Message}");
            return 1;
        }
    }

    private static int Build(
        string[] args,
        TextWriter output,
        TextWriter error)
    {
        string? kind = null;
        string? subjectPath = null;
        string? draftPath = null;
        string? outputPath = null;
        string? nupkgPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--kind":
                    kind = ReadValue(args, ref index, "--kind");
                    break;
                case "--subject":
                    subjectPath = ReadValue(args, ref index, "--subject");
                    break;
                case "--output":
                    outputPath = ReadValue(args, ref index, "--output");
                    break;
                case "--nupkg":
                    nupkgPath = ReadValue(args, ref index, "--nupkg");
                    break;
                default:
                    if (args[index].StartsWith("--", StringComparison.Ordinal))
                    {
                        error.WriteLine($"Unknown option '{args[index]}'.");
                        return 1;
                    }

                    if (draftPath is not null)
                    {
                        error.WriteLine("Only one evidence draft path may be supplied.");
                        return 1;
                    }

                    draftPath = args[index];
                    break;
            }
        }

        if (kind is null ||
            subjectPath is null ||
            draftPath is null ||
            outputPath is null)
        {
            error.WriteLine(
                "ledger build requires --kind, --subject, one draft path, and --output.");
            return 1;
        }

        var draft = CanonicalEvidenceJson.ParseDraftDocument(
            ReadBoundedFile(draftPath, MaximumJsonInputBytes));
        EvidenceSourceLedger ledger;
        switch (kind)
        {
            case "repository":
                var repositorySubject =
                    CanonicalEvidenceJson.ParseRepositorySubject(
                        ReadBoundedFile(subjectPath, MaximumJsonInputBytes));
                ValidateNupkg(repositorySubject.Artifact, nupkgPath);
                ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
                    repositorySubject,
                    draft.Records);
                break;
            case "component":
                var componentSubject = CanonicalEvidenceJson.ParseAssessment(
                    ReadBoundedFile(subjectPath, MaximumJsonInputBytes));
                ValidateNupkg(componentSubject.Artifact, nupkgPath);
                ledger = EvidenceLedgerBuilder.BuildComponentLedger(
                    componentSubject,
                    draft.Records);
                break;
            default:
                error.WriteLine(
                    $"Unknown ledger kind '{kind}'. Expected repository or component.");
                return 1;
        }

        var bytes = CanonicalEvidenceJson.SerializeSourceLedger(ledger);
        RequireOutputWithinLimit(
            bytes,
            MaximumJsonInputBytes,
            "Evidence ledger");
        FileSystemUtilities.WriteAllBytesNew(
            Path.GetFullPath(outputPath),
            bytes,
            "Evidence ledger");
        output.WriteLine(
            $"Evidence ledger built: {ledger.LedgerKind}, {ledger.Records.Count} " +
            $"records, sha256:{CanonicalEvidenceJson.ComputeSha256(bytes)}.");
        return 0;
    }

    private static int Validate(
        string[] args,
        TextWriter output,
        TextWriter error)
    {
        if (args.Length != 1 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            error.WriteLine("ledger validate requires exactly one ledger path.");
            return 1;
        }

        var bytes = ReadBoundedFile(args[0], MaximumJsonInputBytes);
        var ledger = CanonicalEvidenceJson.ParseSourceLedger(bytes);
        output.WriteLine(
            $"Evidence ledger valid: {ledger.LedgerKind}, {ledger.Records.Count} " +
            $"records, sha256:{CanonicalEvidenceJson.ComputeSha256(bytes)}.");
        return 0;
    }

    private static int Bundle(
        string[] args,
        TextWriter output,
        TextWriter error)
    {
        string? assessmentPath = null;
        string? identifiers = null;
        string? outputPath = null;
        var sourcePaths = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--assessment":
                    assessmentPath = ReadValue(args, ref index, "--assessment");
                    break;
                case "--source-ledger":
                    sourcePaths.Add(
                        ReadValue(args, ref index, "--source-ledger"));
                    break;
                case "--ids":
                    identifiers = ReadValue(args, ref index, "--ids");
                    break;
                case "--output":
                    outputPath = ReadValue(args, ref index, "--output");
                    break;
                default:
                    error.WriteLine($"Unknown option '{args[index]}'.");
                    return 1;
            }
        }

        if (assessmentPath is null ||
            sourcePaths.Count == 0 ||
            identifiers is null ||
            outputPath is null)
        {
            error.WriteLine(
                "ledger bundle requires --assessment, one or more --source-ledger, " +
                "--ids, and --output.");
            return 1;
        }

        var selected = identifiers.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        ValidateBundleAggregateSize(
            new FileInfo(Path.GetFullPath(assessmentPath)).Length,
            sourcePaths.Select(path =>
                new FileInfo(Path.GetFullPath(path)).Length),
            selected.Length);
        var assessmentBytes = ReadBoundedFile(
            assessmentPath,
            MaximumJsonInputBytes);
        var assessment = CanonicalEvidenceJson.ParseAssessment(assessmentBytes);
        var ledgers = sourcePaths
            .Select(path => CanonicalEvidenceJson.ParseSourceLedger(
                ReadBoundedFile(path, MaximumJsonInputBytes)))
            .ToArray();

        var bundle = EvidenceLedgerBuilder.BuildBundle(
            assessment,
            ledgers,
            selected);
        var bytes = CanonicalEvidenceJson.SerializeBundle(bundle);
        RequireOutputWithinLimit(
            bytes,
            FileSystemUtilities.MaximumSerializedArtifactBytes,
            "Evidence bundle");
        FileSystemUtilities.WriteAllBytesNew(
            Path.GetFullPath(outputPath),
            bytes,
            "Evidence bundle");
        output.WriteLine(
            $"Evidence bundle built: {bundle.SourceLedgers.Count} source ledgers, " +
            $"{bundle.Selection.Count} selected records, " +
            $"sha256:{CanonicalEvidenceJson.ComputeSha256(bytes)}.");
        return 0;
    }

    private static void ValidateNupkg(
        ArtifactIdentity artifact,
        string? nupkgPath)
    {
        if (artifact.Mode == "source-only")
        {
            if (nupkgPath is not null)
            {
                throw new InvalidDataException(
                    "EVID006: --nupkg is invalid for source-only identity.");
            }

            return;
        }

        if (nupkgPath is null)
        {
            throw new InvalidDataException(
                "EVID006: released-package ledger build requires --nupkg.");
        }

        using var stream = new FileStream(
            Path.GetFullPath(nupkgPath),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        var actual = EvidenceIdentity.ReadPackageIdentity(stream);
        if (actual != artifact.Package)
        {
            throw new InvalidDataException(
                "EVID006: exact nupkg nuspec identity/digest differs from subject.");
        }
    }

    private static byte[] ReadBoundedFile(string path, int maximumBytes)
    {
        using var stream = new FileStream(
            Path.GetFullPath(path),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException(
                $"Input {Path.GetFileName(path)} exceeds {maximumBytes} bytes.");
        }

        return EvidenceIdentity.ReadBounded(stream, maximumBytes);
    }

    private static void RequireOutputWithinLimit(
        ReadOnlyMemory<byte> bytes,
        long maximumBytes,
        string artifactName)
    {
        if (bytes.Length > maximumBytes)
        {
            throw new InvalidDataException(
                $"{artifactName} exceeds the {maximumBytes}-byte output limit.");
        }
    }

    internal static void ValidateBundleAggregateSize(
        long assessmentLength,
        IEnumerable<long> sourceLedgerLengths,
        int selectionCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(assessmentLength);
        ArgumentOutOfRangeException.ThrowIfNegative(selectionCount);
        long estimate = checked(assessmentLength + 1024L + (selectionCount * 256L));
        if (estimate > FileSystemUtilities.MaximumSerializedArtifactBytes)
        {
            throw new InvalidDataException(
                "Evidence bundle aggregate inputs exceed the " +
                $"{FileSystemUtilities.MaximumSerializedArtifactBytes}-byte " +
                "serialized-artifact limit.");
        }

        foreach (var length in sourceLedgerLengths)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            estimate = checked(estimate + length + 256L);
            if (estimate > FileSystemUtilities.MaximumSerializedArtifactBytes)
            {
                throw new InvalidDataException(
                    "Evidence bundle aggregate inputs exceed the " +
                    $"{FileSystemUtilities.MaximumSerializedArtifactBytes}-byte " +
                    "serialized-artifact limit.");
            }
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

    private static int UnknownOperation(string operation, TextWriter error)
    {
        error.WriteLine(
            $"Unknown ledger operation '{operation}'. Expected build, validate, or bundle.");
        return 1;
    }

    private static void WriteUsage(TextWriter error)
    {
        error.WriteLine(
            "Usage: BlazorComponentReadiness ledger <build|validate|bundle> [options]");
    }
}
