// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentImportProjectFeature : IImportProjectFeature
{
    // Using explicit newlines here to avoid fooling our baseline tests
    private const string DefaultUsingImportContent =
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

        // Don't add Component imports for a non-component.
        if (!FileKinds.IsComponent(projectItem.FileKind))
        {
            return Array.Empty<RazorProjectItem>();
        }

        var imports = new List<RazorProjectItem>()
            {
                 new VirtualProjectItem(DefaultUsingImportContent),
            };

        // We add hierarchical imports second so any default directive imports can be overridden.
        imports.AddRange(GetHierarchicalImports(ProjectEngine.FileSystem, projectItem));

        return imports;
    }

    // Temporary API until we fully convert to RazorProjectEngine
    public IEnumerable<RazorProjectItem> GetHierarchicalImports(RazorProject project, RazorProjectItem projectItem)
    {
        // We want items in descending order. FindHierarchicalItems returns items in ascending order.
        return project.FindHierarchicalItems(projectItem.FilePath, ComponentMetadata.ImportsFileName).Reverse();
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

        public override string FileKind => FileKinds.ComponentImport;

        public override Stream Read() => new MemoryStream(_bytes);
    }
}
