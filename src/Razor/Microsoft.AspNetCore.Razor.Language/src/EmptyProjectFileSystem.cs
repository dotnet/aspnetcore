// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class EmptyProjectFileSystem : RazorProjectFileSystem
    {
        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            NormalizeAndEnsureValidPath(basePath);
            return Enumerable.Empty<RazorProjectItem>();
        }

        [Obsolete("Use GetItem(string path, string fileKind) instead.")] // Remove after .NET 6.
        public override RazorProjectItem GetItem(string path)
        {
            return GetItem(path, fileKind: null);
        }

        public override RazorProjectItem GetItem(string path, string fileKind)
        {
            NormalizeAndEnsureValidPath(path);
            return new NotFoundProjectItem(string.Empty, path, fileKind);
        }
    }
}
