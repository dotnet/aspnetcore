// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Performance
{
    [Config(typeof(CoreConfig))]
    public class CodeGenerationBenchmark
    {
        public CodeGenerationBenchmark()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while  (current != null && !File.Exists(Path.Combine(current.FullName, "MSN.cshtml")))
            {
                current = current.Parent;
            }

            var root = current;

            var engine = RazorEngine.Create(b => { RazorExtensions.Register(b); });

            var project = RazorProject.Create(root.FullName);

            DesignTimeTemplateEngine = new MvcRazorTemplateEngine(RazorEngine.CreateDesignTime(b => { RazorExtensions.Register(b); }), project);
            RuntimeTemplateEngine = new MvcRazorTemplateEngine(RazorEngine.Create(b => { RazorExtensions.Register(b); }), project);

            var codeDocument = RuntimeTemplateEngine.CreateCodeDocument(Path.Combine(root.FullName, "MSN.cshtml"));

            Imports = codeDocument.Imports;
            MSN = codeDocument.Source;
        }

        public RazorTemplateEngine DesignTimeTemplateEngine { get; }

        public RazorTemplateEngine RuntimeTemplateEngine { get; }

        public IReadOnlyList<RazorSourceDocument> Imports { get; }

        public RazorSourceDocument MSN { get; }

        [Benchmark(Description = "Razor Design Time Code Generation of MSN.com")]
        public void CodeGeneration_DesignTime_LargeStaticFile()
        {
            var codeDocument = RazorCodeDocument.Create(MSN, Imports);
            var generated = DesignTimeTemplateEngine.GenerateCode(codeDocument);

            if (generated.Diagnostics.Count != 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, generated.Diagnostics));
            }
        }

        [Benchmark(Description = "Razor Runtime Code Generation of MSN.com")]
        public void CodeGeneration_Runtime_LargeStaticFile()
        {
            var codeDocument = RazorCodeDocument.Create(MSN, Imports);
            var generated = RuntimeTemplateEngine.GenerateCode(codeDocument);

            if (generated.Diagnostics.Count != 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, generated.Diagnostics));
            }
        }
    }
}
