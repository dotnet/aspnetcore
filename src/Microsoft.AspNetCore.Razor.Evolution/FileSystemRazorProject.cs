// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    /// <summary>
    /// A <see cref="RazorProject"/> implementation over the physical file system.
    /// </summary>
    public class FileSystemRazorProject : RazorProject
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileSystemRazorProject"/>.
        /// </summary>
        /// <param name="root">The directory to root the file system at.</param>
        public FileSystemRazorProject(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(root));
            }

            Root = root.TrimEnd('/', '\\');
        }

        /// <summary>
        /// The root of the file system.
        /// </summary>
        public string Root { get; }

        /// <inheritdoc />
        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            EnsureValidPath(basePath);

            Debug.Assert(basePath.StartsWith("/"));
            var absoluteBasePath = Path.Combine(Root, basePath.Substring(1));

            var directory = new DirectoryInfo(absoluteBasePath);
            if (!directory.Exists)
            {
                return Enumerable.Empty<RazorProjectItem>();
            }

            return directory
                .EnumerateFiles("*.cshtml", SearchOption.AllDirectories)
                .Select(file =>
                {
                    var relativePath = file.FullName.Substring(absoluteBasePath.Length).Replace(Path.DirectorySeparatorChar, '/');
                    return new FileSystemRazorProjectItem(basePath, relativePath, file);
                 });
        }

        /// <inheritdoc />
        public override RazorProjectItem GetItem(string path)
        {
            EnsureValidPath(path);

            Debug.Assert(path.StartsWith("/"));
            var absolutePath = Path.Combine(Root, path.Substring(1));
            return new FileSystemRazorProjectItem("/", path, new FileInfo(absolutePath));
        }
    }
}
