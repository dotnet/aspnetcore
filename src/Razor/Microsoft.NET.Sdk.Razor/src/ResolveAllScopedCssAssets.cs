// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ResolveAllScopedCssAssets : Task
    {
        [Required]
        public ITaskItem[] StaticWebAssets { get; set; }

        [Output]
        public ITaskItem[] ScopedCssAssets { get; set; }

        public override bool Execute()
        {
            var scopedCssAssets = new List<ITaskItem>();

            for (var i = 0; i < StaticWebAssets.Length; i++)
            {
                var swa = StaticWebAssets[i];
                var fullPath = swa.GetMetadata("RelativePath");
                if (fullPath.EndsWith(".rz.scp.css", StringComparison.OrdinalIgnoreCase))
                {
                    scopedCssAssets.Add(swa);
                }
            }

            ScopedCssAssets = scopedCssAssets.ToArray();

            return !Log.HasLoggedErrors;
        }
    }
}
