// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class CompositeRazorProjectFileSystem : RazorProjectFileSystem
    {
        public CompositeRazorProjectFileSystem(IReadOnlyList<RazorProjectFileSystem> projects)
        {
            Projects = projects ?? throw new ArgumentNullException(nameof(projects));
        }

        public IReadOnlyList<RazorProjectFileSystem> Projects { get; }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            foreach (var project in Projects)
            {
                foreach (var result in project.EnumerateItems(basePath))
                {
                    yield return result;
                }
            }
        }

        public override RazorProjectItem GetItem(string path)
        {
            RazorProjectItem razorProjectItem = null;
            foreach (var project in Projects)
            {
                razorProjectItem = project.GetItem(path);
                if (razorProjectItem != null && razorProjectItem.Exists)
                {
                    return razorProjectItem;
                }
            }

            return razorProjectItem;
        }
    }
}
