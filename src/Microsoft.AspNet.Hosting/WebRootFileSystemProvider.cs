// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class WebRootFileSystemProvider : IWebRootFileSystemProvider
    {
        private readonly IFileSystem _fileSystem;

        public WebRootFileSystemProvider(IApplicationEnvironment appEnvironment)
        {
            var root = HostingUtilities.GetWebRoot(appEnvironment.ApplicationBasePath);

            if (!string.IsNullOrEmpty(root) &&
                root[root.Length - 1] != Path.DirectorySeparatorChar)
            {
                root += Path.DirectorySeparatorChar;
            }

            WebRoot = root;

            _fileSystem = new PhysicalFileSystem(WebRoot);
        }

        public string WebRoot { get; private set; }

        public IFileSystem GetFileSystem()
        {
            return _fileSystem;
        }

        public string MapPath(string path)
        {
            var fullPath = Path.GetFullPath(Path.Combine(WebRoot, path));

            // Don't allow MapPath to escape the base root directory
            if (!fullPath.StartsWith(WebRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid path: " + path, nameof(path));
            }

            return fullPath;
        }
    }
}