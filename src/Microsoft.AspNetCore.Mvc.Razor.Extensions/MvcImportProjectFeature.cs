// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    internal class MvcImportProjectFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
    {
        private const string ImportsFileName = "_ViewImports.cshtml";

        public IReadOnlyList<RazorSourceDocument> GetImports(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var imports = new List<RazorSourceDocument>();
            AddDefaultDirectivesImport(imports);

            // We add hierarchical imports second so any default directive imports can be overridden.
            AddHierarchicalImports(projectItem, imports);

            return imports;
        }

        // Internal for testing
        internal static void AddDefaultDirectivesImport(List<RazorSourceDocument> imports)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("@using System");
                writer.WriteLine("@using System.Collections.Generic");
                writer.WriteLine("@using System.Linq");
                writer.WriteLine("@using System.Threading.Tasks");
                writer.WriteLine("@using Microsoft.AspNetCore.Mvc");
                writer.WriteLine("@using Microsoft.AspNetCore.Mvc.Rendering");
                writer.WriteLine("@using Microsoft.AspNetCore.Mvc.ViewFeatures");
                writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> Html");
                writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json");
                writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component");
                writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.IUrlHelper Url");
                writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider");
                writer.WriteLine("@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper, Microsoft.AspNetCore.Mvc.Razor");
                writer.WriteLine("@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.HeadTagHelper, Microsoft.AspNetCore.Mvc.Razor");
                writer.WriteLine("@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper, Microsoft.AspNetCore.Mvc.Razor");
                writer.Flush();

                stream.Position = 0;
                var defaultMvcImports = RazorSourceDocument.ReadFrom(stream, fileName: null, encoding: Encoding.UTF8);
                imports.Add(defaultMvcImports);
            }
        }

        // Internal for testing
        internal void AddHierarchicalImports(RazorProjectItem projectItem, List<RazorSourceDocument> imports)
        {
            // We want items in descending order. FindHierarchicalItems returns items in ascending order.
            var importProjectItems = ProjectEngine.FileSystem.FindHierarchicalItems(projectItem.FilePath, ImportsFileName).Reverse();
            foreach (var importProjectItem in importProjectItems)
            {
                RazorSourceDocument importSourceDocument;

                if (importProjectItem.Exists)
                {
                    importSourceDocument = RazorSourceDocument.ReadFrom(importProjectItem);
                }
                else
                {
                    // File doesn't exist on disk so just add a marker source document as an identifier for "there could be something here".
                    var sourceDocumentProperties = new RazorSourceDocumentProperties(importProjectItem.FilePath, importProjectItem.RelativePhysicalPath);
                    importSourceDocument = RazorSourceDocument.Create(string.Empty, sourceDocumentProperties);
                }

                imports.Add(importSourceDocument);
            }
        }
    }
}
