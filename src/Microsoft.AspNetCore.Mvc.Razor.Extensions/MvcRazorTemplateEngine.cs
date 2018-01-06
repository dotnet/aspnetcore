// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    /// <summary>
    /// A <see cref="RazorTemplateEngine"/> for Mvc Razor views.
    /// </summary>
    public class MvcRazorTemplateEngine : RazorTemplateEngine
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorTemplateEngine"/>.
        /// </summary>
        /// <param name="engine">The <see cref="RazorEngine"/>.</param>
        /// <param name="project">The <see cref="RazorProject"/>.</param>
        public MvcRazorTemplateEngine(
            RazorEngine engine,
            RazorProject project)
            : base(engine, project)
        {
            Options.ImportsFileName = "_ViewImports.cshtml";
            Options.DefaultImports = GetDefaultImports();
        }

        public override RazorCodeDocument CreateCodeDocument(RazorProjectItem projectItem)
        {
            return base.CreateCodeDocument(projectItem);
        }

        // Internal for testing.
        internal static RazorSourceDocument GetDefaultImports()
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
                return RazorSourceDocument.ReadFrom(stream, fileName: null, encoding: Encoding.UTF8);
            }
        }
    }
}
