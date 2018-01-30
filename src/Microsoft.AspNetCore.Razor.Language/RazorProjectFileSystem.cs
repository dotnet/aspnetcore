// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorProjectFileSystem : RazorProject
    {
        /// <summary>
        /// Create a Razor project based on a physical file system.
        /// </summary>
        /// <param name="rootDirectoryPath">The directory to root the file system at.</param>
        /// <returns>A <see cref="RazorProject"/></returns>
        public static new RazorProjectFileSystem Create(string rootDirectoryPath)
        {
            return new FileSystemRazorProject(rootDirectoryPath);
        }
    }
}
