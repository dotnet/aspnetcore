// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        public override RazorProjectItem GetItem(string path)
        {
            NormalizeAndEnsureValidPath(path);
            return new NotFoundProjectItem(string.Empty, path);
        }
    }
}
