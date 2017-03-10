// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    /// <summary>
    /// An implementation of <see cref="RazorProjectItem"/> using <see cref="FileInfo"/>.
    /// </summary>
    public class FileSystemRazorProjectItem : RazorProjectItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileSystemRazorProjectItem"/>.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <param name="path">The path.</param>
        /// <param name="file">The <see cref="FileInfo"/>.</param>
        public FileSystemRazorProjectItem(string basePath, string path, FileInfo file)
        {
            BasePath = basePath;
            Path = path;
            File = file;
        }

        /// <summary>
        /// Gets the <see cref="FileInfo"/>.
        /// </summary>
        public FileInfo File { get; }

        /// <inheritdoc />
        public override string BasePath { get; }

        /// <inheritdoc />
        public override string Path { get; }

        /// <inheritdoc />
        public override bool Exists => File.Exists;

        /// <inheritdoc />
        public override string FileName => File.Name;

        /// <inheritdoc />
        public override string PhysicalPath => File.FullName;

        /// <inheritdoc />
        public override Stream Read() => File.OpenRead();
    }
}