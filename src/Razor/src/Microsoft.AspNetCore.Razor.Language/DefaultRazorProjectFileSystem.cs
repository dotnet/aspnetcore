// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorProjectFileSystem : RazorProjectFileSystem
    {
        public DefaultRazorProjectFileSystem(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(root));
            }
            
            Root = root.Replace('\\', '/').TrimEnd('/');
        }

        public string Root { get; }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            var absoluteBasePath = NormalizeAndEnsureValidPath(basePath);

            var directory = new DirectoryInfo(absoluteBasePath);
            if (!directory.Exists)
            {
                return Enumerable.Empty<RazorProjectItem>();
            }

            return directory
                .EnumerateFiles("*.cshtml", SearchOption.AllDirectories)
                .Select(file =>
                {
                    var relativePhysicalPath = file.FullName.Substring(absoluteBasePath.Length + 1); // Include leading separator
                    var filePath = "/" + relativePhysicalPath.Replace(Path.DirectorySeparatorChar, '/');

                    return new DefaultRazorProjectItem(basePath, filePath, relativePhysicalPath, file);
                });
        }

        public override RazorProjectItem GetItem(string path)
        {
            var absoluteBasePath = NormalizeAndEnsureValidPath("/");
            var absolutePath = NormalizeAndEnsureValidPath(path);

            var file = new FileInfo(absolutePath);
            if (!absolutePath.StartsWith(absoluteBasePath))
            {
                throw new InvalidOperationException($"The file '{file.FullName}' is not a descendent of the base path '{absoluteBasePath}'.");
            }

            var relativePhysicalPath = file.FullName.Substring(absoluteBasePath.Length + 1); // Include leading separator
            var filePath = "/" + relativePhysicalPath.Replace(Path.DirectorySeparatorChar, '/');

            return new DefaultRazorProjectItem("/", filePath, relativePhysicalPath, new FileInfo(absolutePath));
        }

        protected override string NormalizeAndEnsureValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(path));
            }

            var absolutePath = path;
            if (!absolutePath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            {
                if (path[0] == '/' || path[0] == '\\')
                {
                    path = path.Substring(1);
                }

                absolutePath = Path.Combine(Root, path);
            }

            absolutePath = absolutePath.Replace('\\', '/');

            return absolutePath;
        }
    }
}
