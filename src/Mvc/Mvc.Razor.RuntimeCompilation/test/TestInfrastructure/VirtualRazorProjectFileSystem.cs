// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class VirtualRazorProjectFileSystem : RazorProjectFileSystem
    {
        private readonly DirectoryNode _root = new DirectoryNode("/");

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            basePath = NormalizeAndEnsureValidPath(basePath);
            var directory = _root.GetDirectory(basePath);
            return directory?.EnumerateItems() ?? Enumerable.Empty<RazorProjectItem>();
        }

        public override RazorProjectItem GetItem(string path)
        {
            path = NormalizeAndEnsureValidPath(path);
            return _root.GetItem(path) ?? new NotFoundProjectItem(string.Empty, path);
        }

        public void Add(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var filePath = NormalizeAndEnsureValidPath(projectItem.FilePath);
            _root.AddFile(new FileNode(filePath, projectItem));
        }
    }
}