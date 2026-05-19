// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class EmbeddedFilesManifest
{
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
        .Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar).ToArray();

    private static readonly char[] _separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    private readonly ManifestDirectory _rootDirectory;

    internal EmbeddedFilesManifest(ManifestDirectory rootDirectory)
    {
        ArgumentNullThrowHelper.ThrowIfNull(rootDirectory);

        _rootDirectory = rootDirectory;
    }

    internal ManifestEntry? ResolveEntry(string path)
    {
        if (string.IsNullOrEmpty(path) || HasInvalidPathChars(path))
        {
            return null;
        }

        // trimmed is a string without leading nor trailing path separators
        // so if we find an empty string while iterating over the segments
        // we know for sure the path is invalid and we treat it as the above
        // case by returning null.
        // Examples of invalid paths are: //wwwroot /\wwwroot //wwwroot//jquery.js
        var trimmed = RemoveLeadingAndTrailingDirectorySeparators(path);
        // Paths consisting only of a single path separator like / or \ are ok.
        if (trimmed.Length == 0)
        {
            return _rootDirectory;
        }

        var tokenizer = new StringTokenizer(trimmed, _separators);
        ManifestEntry currentEntry = _rootDirectory;
        foreach (var segment in tokenizer)
        {
            if (segment.Equals(""))
            {
                return null;
            }

            currentEntry = currentEntry.Traverse(segment);
        }

        return currentEntry;
    }

    private static StringSegment RemoveLeadingAndTrailingDirectorySeparators(string path)
    {
        Debug.Assert(path.Length > 0);
        var start = Array.IndexOf(_separators, path[0]) == -1 ? 0 : 1;
        if (start == path.Length)
        {
            return StringSegment.Empty;
        }

        var end = Array.IndexOf(_separators, path[path.Length - 1]) == -1 ? path.Length : path.Length - 1;
        var trimmed = new StringSegment(path, start, end - start);
        return trimmed;
    }

    internal EmbeddedFilesManifest Scope(string path)
    {
        if (ResolveEntry(path) is ManifestDirectory directory && directory != ManifestEntry.UnknownPath)
        {
            return new EmbeddedFilesManifest(directory.ToRootDirectory());
        }

        throw new InvalidOperationException($"Invalid path: '{path}'");
    }

    private static bool HasInvalidPathChars(string path) => path.IndexOfAny(_invalidFileNameChars) != -1;
}
