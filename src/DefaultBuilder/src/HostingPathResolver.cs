// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore;

internal static class HostingPathResolver
{
    public static string ResolvePath(string? contentRootPath) =>
        PathWithDirectorySeperatorAtEnd(ResolvePathNonCononical(contentRootPath, AppContext.BaseDirectory));

    public static string ResolvePath(string? contentRootPath, string basePath) =>
        PathWithDirectorySeperatorAtEnd(ResolvePathNonCononical(contentRootPath, basePath));

    private static string PathWithDirectorySeperatorAtEnd(string path) =>
        Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;

    private static string ResolvePathNonCononical(string? contentRootPath, string basePath)
    {
        if (string.IsNullOrEmpty(contentRootPath))
        {
            return Path.GetFullPath(basePath);
        }
        if (Path.IsPathRooted(contentRootPath))
        {
            return Path.GetFullPath(contentRootPath);
        }
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(basePath), contentRootPath));
    }
}
