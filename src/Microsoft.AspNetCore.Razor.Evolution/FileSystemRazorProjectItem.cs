// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class FileSystemRazorProjectItem : RazorProjectItem
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

        public FileInfo File { get; }

        public override string BasePath { get; }

        public override string Path { get; }

        public override bool Exists => File.Exists;

        public override string FileName => File.Name;

        public override string PhysicalPath => File.FullName;

        public override Stream Read() => File.OpenRead();
    }
}