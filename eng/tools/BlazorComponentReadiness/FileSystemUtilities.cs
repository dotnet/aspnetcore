// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace BlazorComponentReadiness;

internal static class FileSystemUtilities
{
    internal const long MaximumSerializedArtifactBytes = 64L * 1024 * 1024;

    internal static bool PathsReferToSameEntry(string firstPath, string secondPath)
    {
        var first = ResolveExistingPath(firstPath);
        var second = ResolveExistingPath(secondPath);

        return string.Equals(first, second, StringComparison.Ordinal);
    }

    internal static string ResolveExistingPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)
            ?? throw new InvalidDataException($"Path has no root: {path}");
        var current = root;
        var segments = fullPath[root.Length..].Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var candidate = Path.Combine(current, segment);
            var entry = FindExistingEntry(current, segment);
            if (entry is null)
            {
                current = candidate;
                continue;
            }

            current = entry.ResolveLinkTarget(returnFinalTarget: true) is { } target
                ? ResolveExistingPath(target.FullName)
                : entry.FullName;
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(current));
    }

    internal static bool IsWithinDirectory(string directoryPath, string candidatePath)
    {
        var relative = Path.GetRelativePath(directoryPath, candidatePath);
        return !Path.IsPathRooted(relative) &&
            relative != ".." &&
            !relative.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) &&
            !relative.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal);
    }

    internal static void WriteAllTextNew(
        string path,
        string content,
        Action<FileStream, ReadOnlyMemory<byte>>? writeContent = null,
        Action? beforePublish = null)
    {
        WriteAllBytesNew(
            path,
            new UTF8Encoding(false).GetBytes(content),
            "Receipt",
            writeContent,
            beforePublish);
    }

    internal static void WriteAllBytesNew(
        string path,
        ReadOnlyMemory<byte> bytes,
        string artifactName = "Artifact",
        Action<FileStream, ReadOnlyMemory<byte>>? writeContent = null,
        Action? beforePublish = null)
    {
        var fullPath = Path.GetFullPath(path);
        var requestedDirectory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidDataException($"Path has no directory: {path}");
        var resolvedDirectory = ResolveExistingPath(requestedDirectory);
        var finalPath = Path.Combine(resolvedDirectory, Path.GetFileName(fullPath));
        var temporaryPath = Path.Combine(
            resolvedDirectory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        FileStream? stream = null;
        try
        {
            stream = CreateTemporaryFile(temporaryPath);
            if (writeContent is null)
            {
                stream.Write(bytes.Span);
            }

            else
            {
                writeContent(stream, bytes);
            }

            stream.Flush(flushToDisk: true);
            beforePublish?.Invoke();
            var currentDirectory = ResolveExistingPath(requestedDirectory);
            if (!string.Equals(
                currentDirectory,
                resolvedDirectory,
                StringComparison.Ordinal))
            {
                throw new IOException(
                    $"{artifactName} directory changed while writing {path}.");
            }

            VerifyStagedContent(
                stream,
                temporaryPath,
                bytes.Span,
                path,
                artifactName);
            File.Move(temporaryPath, finalPath, overwrite: false);
        }
        finally
        {
            stream?.Dispose();
            File.Delete(temporaryPath);
        }
    }

    internal static byte[] ReadAllBytesBounded(
        string path,
        long maximumBytes = MaximumSerializedArtifactBytes,
        Action<FileStream>? afterLengthRead = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
        var fullPath = Path.GetFullPath(path);
        using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        var length = stream.Length;
        if (length > maximumBytes || length > int.MaxValue)
        {
            throw new InvalidDataException(
                $"Serialized artifact '{Path.GetFileName(path)}' exceeds the " +
                $"{maximumBytes}-byte limit.");
        }

        afterLengthRead?.Invoke(stream);
        var bytes = new byte[(int)length];
        var offset = 0;
        while (offset < bytes.Length)
        {
            var read = stream.Read(bytes, offset, bytes.Length - offset);
            if (read == 0)
            {
                throw new InvalidDataException(
                    $"Serialized artifact '{Path.GetFileName(path)}' changed " +
                    "while it was read.");
            }

            offset += read;
        }

        if (stream.ReadByte() != -1)
        {
            throw new InvalidDataException(
                $"Serialized artifact '{Path.GetFileName(path)}' grew while it " +
                $"was read or exceeds the {maximumBytes}-byte limit.");
        }

        return bytes;
    }

    private static void VerifyStagedContent(
        FileStream stream,
        string temporaryPath,
        ReadOnlySpan<byte> expectedBytes,
        string requestedPath,
        string artifactName)
    {
        if (stream.Length != expectedBytes.Length)
        {
            throw new IOException(
                $"{artifactName} content changed while writing {requestedPath}.");
        }

        var handleBytes = new byte[expectedBytes.Length];
        stream.Position = 0;
        stream.ReadExactly(handleBytes);
        var pathBytes = ReadAllBytesShared(temporaryPath);
        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, handleBytes) ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, pathBytes))
        {
            throw new IOException(
                $"{artifactName} content changed while writing {requestedPath}.");
        }
    }

    private static byte[] ReadAllBytesShared(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        return bytes;
    }

    private static FileStream CreateTemporaryFile(string path)
    {
        return new FileStream(
            path,
            new FileStreamOptions
            {
                Access = FileAccess.ReadWrite,
                Mode = FileMode.CreateNew,
                Options = FileOptions.WriteThrough,
                Share = FileShare.Read | FileShare.Delete,
            });
    }

    private static FileSystemInfo? FindExistingEntry(string directoryPath, string name)
    {
        if (!Directory.Exists(directoryPath))
        {
            return null;
        }

        FileSystemInfo? caseInsensitiveMatch = null;
        FileSystemInfo? normalizationMatch = null;
        foreach (var entry in new DirectoryInfo(directoryPath).EnumerateFileSystemInfos())
        {
            if (string.Equals(entry.Name, name, StringComparison.Ordinal))
            {
                return entry;
            }

            if (caseInsensitiveMatch is null &&
                string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                caseInsensitiveMatch = entry;
            }

            if (normalizationMatch is null &&
                string.Equals(
                    entry.Name.Normalize(NormalizationForm.FormC),
                    name.Normalize(NormalizationForm.FormC),
                    StringComparison.Ordinal))
            {
                normalizationMatch = entry;
            }
        }

        var candidate = Path.Combine(directoryPath, name);
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
        {
            return null;
        }

        return caseInsensitiveMatch ?? normalizationMatch;
    }
}
