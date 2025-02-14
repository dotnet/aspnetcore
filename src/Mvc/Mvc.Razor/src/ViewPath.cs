// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

internal static class ViewPath
{
    public static string NormalizePath(string path)
    {
        var addLeadingSlash = path[0] != '\\' && path[0] != '/';
        var transformSlashes = path.Contains('\\');

        if (!addLeadingSlash && !transformSlashes)
        {
            return path;
        }

        var length = path.Length;
        if (addLeadingSlash)
        {
            length++;
        }

        return string.Create(length, (path, addLeadingSlash), (span, tuple) =>
        {
            var (pathValue, addLeadingSlashValue) = tuple;
            var spanIndex = 0;

            if (addLeadingSlashValue)
            {
                span[spanIndex++] = '/';
            }

            foreach (var ch in pathValue)
            {
                span[spanIndex++] = ch == '\\' ? '/' : ch;
            }
        });
    }
}
