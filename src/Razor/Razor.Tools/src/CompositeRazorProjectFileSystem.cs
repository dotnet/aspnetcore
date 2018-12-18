// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class CompositeRazorProjectFileSystem : RazorProjectFileSystem
    {
        public CompositeRazorProjectFileSystem(IReadOnlyList<RazorProjectFileSystem> fileSystems)
        {
            FileSystems = fileSystems ?? throw new ArgumentNullException(nameof(fileSystems));
        }

        public IReadOnlyList<RazorProjectFileSystem> FileSystems { get; }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            foreach (var fileSystem in FileSystems)
            {
                foreach (var result in fileSystem.EnumerateItems(basePath))
                {
                    yield return result;
                }
            }
        }

        public override RazorProjectItem GetItem(string path)
        {
            RazorProjectItem razorProjectItem = null;
            foreach (var fileSystem in FileSystems)
            {
                razorProjectItem = fileSystem.GetItem(path);
                if (razorProjectItem != null && razorProjectItem.Exists)
                {
                    return razorProjectItem;
                }
            }

            return razorProjectItem;
        }
    }
}
