// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public readonly struct ParsedPath
{
    private readonly string[] _segments;

    public ParsedPath(string path)
    {
        ArgumentNullThrowHelper.ThrowIfNull(path);

        _segments = ParsePath(PathHelpers.NormalizePath(path));
    }

    public string LastSegment
    {
        get
        {
            if (_segments == null || _segments.Length == 0)
            {
                return null;
            }

            return _segments[_segments.Length - 1];
        }
    }

    public IReadOnlyList<string> Segments => _segments;

    private static string[] ParsePath(string path)
    {
        if (path.Length == 0)
        {
            return Array.Empty<string>();
        }

        if (path[0] != '/')
        {
            // This shouldn't be reachable as the constructor enforces it.
            // But added to clarify and ensure that the Slice call below is always safe.
            throw new JsonPatchException(Resources.FormatInvalidValueForPath(path), null);
        }

        // When we have a path like "/a/b/c//d/e", the expectation is
        // to have the segments be ["a", "b", "c", "", "d", "e"].
        // So, before splitting on "/", we want to slice off the leading "/".
        // Without this slice, we will always have an extra empty string at the beginning.
        var referenceTokens = path.Substring(1).Split('/');

        for (int i = 0; i < referenceTokens.Length; i++)
        {
            referenceTokens[i] = ValidateAndUnescapeReferenceToken(referenceTokens[i]);
        }

        return referenceTokens;
    }

    private static string ValidateAndUnescapeReferenceToken(string referenceToken)
    {
        var hasTilde = false;
        for (int i = 0; i < referenceToken.Length; i++)
        {
            if (referenceToken[i] == '~')
            {
                hasTilde = true;
                if (i + 1 >= referenceToken.Length || (referenceToken[i + 1] != '0' && referenceToken[i + 1] != '1'))
                {
                    throw new JsonPatchException(Resources.FormatInvalidValueForPath(referenceToken), null);
                }
            }
        }

        return hasTilde ? referenceToken.Replace("~1", "/").Replace("~0", "~") : referenceToken;
    }
}
