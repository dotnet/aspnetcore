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

        public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var imports = new List<RazorProjectItem>();
            AddDefaultDirectivesImport(imports);

            // We add hierarchical imports second so any default directive imports can be overridden.
            AddHierarchicalImports(projectItem, imports);

            return imports;
        }

        // Internal for testing
        internal static void AddDefaultDirectivesImport(List<RazorProjectItem> imports)
        {
            imports.Add(DefaultDirectivesProjectItem.Instance);
        }

        // Internal for testing
        internal void AddHierarchicalImports(RazorProjectItem projectItem, List<RazorProjectItem> imports)
        {
            // We want items in descending order. FindHierarchicalItems returns items in ascending order.
            var importProjectItems = ProjectEngine.FileSystem.FindHierarchicalItems(projectItem.FilePath, ImportsFileName).Reverse();
            imports.AddRange(importProjectItems);
        }

        private class DefaultDirectivesProjectItem : RazorProjectItem
        {
            private readonly byte[] _defaultImportBytes;

            private DefaultDirectivesProjectItem()
            {
                var preamble = Encoding.UTF8.GetPreamble();
                var content = @"
@using System
@using System.Collections.Generic
@using System.Linq
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Mvc
@using Microsoft.AspNetCore.Mvc.Rendering
@using Microsoft.AspNetCore.Mvc.ViewFeatures
@inject global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> Html
@inject global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json
@inject global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component
@inject global::Microsoft.AspNetCore.Mvc.IUrlHelper Url
@inject global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider
@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper, Microsoft.AspNetCore.Mvc.Razor
@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.HeadTagHelper, Microsoft.AspNetCore.Mvc.Razor
@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper, Microsoft.AspNetCore.Mvc.Razor
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

            public static DefaultDirectivesProjectItem Instance { get; } = new DefaultDirectivesProjectItem();

            public override Stream Read() => new MemoryStream(_defaultImportBytes);
        }
    }
}
