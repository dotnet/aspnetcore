// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// A <see cref="RazorTemplateEngine"/> for Blazor components.
    /// </summary>
    public class BlazorTemplateEngine : RazorTemplateEngine
    {
        // We need to implement and register this feature for tooling support to work. Subclassing TemplateEngine
        // doesn't work inside visual studio.
        private readonly BlazorImportProjectFeature _feature;

        public BlazorTemplateEngine(RazorEngine engine, RazorProject project)
            : base(engine, project)
        {
            _feature = new BlazorImportProjectFeature();
            
            Options.DefaultImports = RazorSourceDocument.ReadFrom(_feature.DefaultImports);
        }

        public override IEnumerable<RazorProjectItem> GetImportItems(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new System.ArgumentNullException(nameof(projectItem));
            }

            return _feature.GetHierarchicalImports(Project, projectItem);
        }

        private static RazorSourceDocument GetDefaultImports()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                // TODO: Add other commonly-used Blazor namespaces here. Can't do so yet
                // because the tooling wouldn't know about it, so it would still look like
                // an error if you hadn't explicitly imported them.
                writer.WriteLine("@using System");
                writer.WriteLine("@using System.Collections.Generic");
                writer.WriteLine("@using System.Linq");
                writer.WriteLine("@using System.Threading.Tasks");
                writer.Flush();

                stream.Position = 0;
                return RazorSourceDocument.ReadFrom(stream, fileName: null, encoding: Encoding.UTF8);
            }
        }
    }
}
