// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class BlazorImportProjectFeature : IImportProjectFeature
    {
        private const string ImportsFileName = "_ViewImports.cshtml";

        private static readonly char[] PathSeparators = new char[]{ '/', '\\' };

        // Using explicit newlines here to avoid fooling our baseline tests
        private readonly static string DefaultUsingImportContent =
            "\r\n" +
            "@using System\r\n" +
            "@using System.Collections.Generic\r\n" +
            "@using System.Linq\r\n" +
            "@using System.Threading.Tasks\r\n" +
            "@using " + ComponentsApi.RenderFragment.Namespace + "\r\n"; // Microsoft.AspNetCore.Components

        public RazorProjectEngine ProjectEngine { get; set; }

        public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var imports = new List<RazorProjectItem>()
            {
                 new VirtualProjectItem(DefaultUsingImportContent),
                 new VirtualProjectItem(@"@addTagHelper ""*, Microsoft.AspNetCore.Components"""),
            };

            // Try and infer a namespace from the project directory. We don't yet have the ability to pass
            // the namespace through from the project.
            if (projectItem.PhysicalPath != null && projectItem.FilePath != null)
            {
                // Avoiding the path-specific APIs here, we want to handle all styles of paths
                // on all platforms
                var trimLength = projectItem.FilePath.Length + (projectItem.FilePath.StartsWith("/") ? 0 : 1);
                var baseDirectory = projectItem.PhysicalPath.Substring(0, projectItem.PhysicalPath.Length - trimLength);
                
                var lastSlash = baseDirectory.LastIndexOfAny(PathSeparators);
                var baseNamespace = lastSlash == -1 ? baseDirectory : baseDirectory.Substring(lastSlash + 1);
                if (!string.IsNullOrEmpty(baseNamespace))
                {
                    imports.Add(new VirtualProjectItem($@"@addTagHelper ""*, {baseNamespace}"""));
                }
            }

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
            private readonly byte[] _bytes;

            public VirtualProjectItem(string content)
            {
                var preamble = Encoding.UTF8.GetPreamble();
                var contentBytes = Encoding.UTF8.GetBytes(content);

                _bytes = new byte[preamble.Length + contentBytes.Length];
                preamble.CopyTo(_bytes, 0);
                contentBytes.CopyTo(_bytes, preamble.Length);
            }

            public override string BasePath => null;

            public override string FilePath => null;

            public override string PhysicalPath => null;

            public override bool Exists => true;

            public override Stream Read() => new MemoryStream(_bytes);
        }
    }
}