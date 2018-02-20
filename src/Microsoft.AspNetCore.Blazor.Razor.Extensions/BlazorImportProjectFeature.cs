// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class BlazorImportProjectFeature : IImportProjectFeature
    {
        private const string ImportsFileName = "_ViewImports.cshtml";

        public RazorProjectItem DefaultImports => VirtualProjectItem.Instance;

        public RazorProjectEngine ProjectEngine { get; set; }

        public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var imports = new List<RazorProjectItem>()
            {
                VirtualProjectItem.Instance,
            };

            // We add hierarchical imports second so any default directive imports can be overridden.
            imports.AddRange(GetHierarchicalImports(ProjectEngine.FileSystem, projectItem));

            return imports;
        }

        // Temporary API until we fully convert to RazorProjectEngine
        public IEnumerable<RazorProjectItem> GetHierarchicalImports(RazorProject project, RazorProjectItem projectItem)
        {
            // We want items in descending order. FindHierarchicalItems returns items in ascending order.
            return project.FindHierarchicalItems(projectItem.FilePath, ImportsFileName).Reverse();
        }

        private class VirtualProjectItem : RazorProjectItem
        {
            private readonly byte[] _defaultImportBytes;

            private VirtualProjectItem()
            {
                var preamble = Encoding.UTF8.GetPreamble();
                var content = @"
@using System
@using System.Collections.Generic
@using System.Linq
@using System.Threading.Tasks
";
                var contentBytes = Encoding.UTF8.GetBytes(content);

                _defaultImportBytes = new byte[preamble.Length + contentBytes.Length];
                preamble.CopyTo(_defaultImportBytes, 0);
                contentBytes.CopyTo(_defaultImportBytes, preamble.Length);
            }

            public override string BasePath => null;

            public override string FilePath => null;

            public override string PhysicalPath => null;

            public override bool Exists => true;

            public static VirtualProjectItem Instance { get; } = new VirtualProjectItem();

            public override Stream Read() => new MemoryStream(_defaultImportBytes);
        }
    }
}