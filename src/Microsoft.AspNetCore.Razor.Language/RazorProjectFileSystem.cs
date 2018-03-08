// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorProjectFileSystem : RazorProject
    {
        internal static readonly RazorProjectFileSystem Empty = new EmptyProjectFileSystem();

        /// <summary>
        /// Create a Razor project file system based off of a root directory.
        /// </summary>
        /// <param name="rootDirectoryPath">The directory to root the file system at.</param>
        /// <returns>A <see cref="RazorProjectFileSystem"/></returns>
        public new static RazorProjectFileSystem Create(string rootDirectoryPath)
        {
            if (string.IsNullOrEmpty(rootDirectoryPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(rootDirectoryPath));
            }

            return new DefaultRazorProjectFileSystem(rootDirectoryPath);
        }
    }
}
