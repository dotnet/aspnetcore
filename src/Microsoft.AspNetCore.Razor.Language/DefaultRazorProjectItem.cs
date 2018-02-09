// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorProjectItem : RazorProjectItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultRazorProjectItem"/>.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <param name="relativePhysicalPath">The physical path of the base path.</param>
        /// <param name="filePath">The path.</param>
        /// <param name="file">The <see cref="FileInfo"/>.</param>
        public DefaultRazorProjectItem(string basePath, string filePath, string relativePhysicalPath, FileInfo file)
        {
            BasePath = basePath;
            FilePath = filePath;
            RelativePhysicalPath = relativePhysicalPath;
            File = file;
        }

        public FileInfo File { get; }

        public override string BasePath { get; }

        public override string FilePath { get; }

        public override bool Exists => File.Exists;

        public override string PhysicalPath => File.FullName;

        public override string RelativePhysicalPath { get; }

        public override Stream Read() => new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }
}