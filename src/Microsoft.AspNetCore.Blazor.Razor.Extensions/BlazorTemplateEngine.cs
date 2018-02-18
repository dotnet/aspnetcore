// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using System.Text;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// A <see cref="RazorTemplateEngine"/> for Blazor components.
    /// </summary>
    public class BlazorTemplateEngine : RazorTemplateEngine
    {
        public BlazorTemplateEngine(RazorEngine engine, RazorProject project)
            : base(engine, project)
        {
            Options.ImportsFileName = "_ViewImports.cshtml";
            Options.DefaultImports = GetDefaultImports();
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
