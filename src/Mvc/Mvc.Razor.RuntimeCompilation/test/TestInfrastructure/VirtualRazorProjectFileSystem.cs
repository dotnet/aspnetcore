// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

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

        [Obsolete("Use GetItem(string path, string fileKind) instead.")]
        public override RazorProjectItem GetItem(string path)
        {
            return GetItem(path, fileKind: null);
        }

        public override RazorProjectItem GetItem(string path, string fileKind)
        {
            // We ignore fileKind here because the _root is pre-filled with project items that already have fileKinds defined. This is
            // a unique circumstance where the RazorProjectFileSystem is actually pre-filled with all of its project items on construction.

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