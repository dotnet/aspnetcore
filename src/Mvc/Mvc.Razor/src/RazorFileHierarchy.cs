// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Mvc.Razor;

internal static class RazorFileHierarchy
{
    private const string ViewStartFileName = "_ViewStart.cshtml";

    public static IEnumerable<string> GetViewStartPaths(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (path[0] != '/')
        {
            throw new ArgumentException(Resources.RazorProject_PathMustStartWithForwardSlash, nameof(path));
        }

        if (path.Length == 1)
        {
            yield break;
        }

        var builder = new StringBuilder(path);
        var maxIterations = 255;
        var index = path.Length;
        while (maxIterations-- > 0 && index > 1 && (index = path.LastIndexOf('/', index - 1)) != -1)
        {
            builder.Length = index + 1;
            builder.Append(ViewStartFileName);

            var itemPath = builder.ToString();
            yield return itemPath;
        }
    }
}
