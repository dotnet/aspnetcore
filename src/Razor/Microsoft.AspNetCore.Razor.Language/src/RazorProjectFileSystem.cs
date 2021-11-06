// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorProjectFileSystem : RazorProject
{
    internal static readonly RazorProjectFileSystem Empty = new EmptyProjectFileSystem();

    /// <summary>
    /// Create a Razor project file system based off of a root directory.
    /// </summary>
    /// <param name="rootDirectoryPath">The directory to root the file system at.</param>
    /// <returns>A <see cref="RazorProjectFileSystem"/></returns>
    public static RazorProjectFileSystem Create(string rootDirectoryPath)
    {
        if (string.IsNullOrEmpty(rootDirectoryPath))
        {
            throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(rootDirectoryPath));
        }

        return new DefaultRazorProjectFileSystem(rootDirectoryPath);
    }
}
