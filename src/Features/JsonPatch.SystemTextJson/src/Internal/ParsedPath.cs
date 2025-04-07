// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Exceptions;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal readonly struct ParsedPath
{
    private readonly string[] _segments;

    public ParsedPath(string path)
    {
        ArgumentNullThrowHelper.ThrowIfNull(path);

        _segments = ParsePath(path);
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
        var strings = new List<string>();
        var sb = new StringBuilder(path.Length);

        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] == '/')
            {
                if (sb.Length > 0)
                {
                    strings.Add(sb.ToString());
                    sb.Length = 0;
                }
            }
            else if (path[i] == '~')
            {
                ++i;
                if (i >= path.Length)
                {
                    throw new JsonPatchException(Resources.FormatInvalidValueForPath(path), null);
                }

                if (path[i] == '0')
                {
                    sb.Append('~');
                }
                else if (path[i] == '1')
                {
                    sb.Append('/');
                }
                else
                {
                    throw new JsonPatchException(Resources.FormatInvalidValueForPath(path), null);
                }
            }
            else
            {
                sb.Append(path[i]);
            }
        }

        if (sb.Length > 0)
        {
            strings.Add(sb.ToString());
        }

        return strings.ToArray();
    }
}
