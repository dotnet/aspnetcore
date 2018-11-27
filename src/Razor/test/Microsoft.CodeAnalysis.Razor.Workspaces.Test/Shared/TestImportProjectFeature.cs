// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TestImportProjectFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
    {
        public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var imports = new List<RazorProjectItem>();
            AddHierarchicalImports(projectItem, imports);

            return imports;
        }

        private void AddHierarchicalImports(RazorProjectItem projectItem, List<RazorProjectItem> imports)
        {
            // We want items in descending order. FindHierarchicalItems returns items in ascending order.
            var importProjectItems = ProjectEngine.FileSystem.FindHierarchicalItems(projectItem.FilePath, "_Imports.cshtml").Reverse();
            imports.AddRange(importProjectItems);
        }
    }
}
