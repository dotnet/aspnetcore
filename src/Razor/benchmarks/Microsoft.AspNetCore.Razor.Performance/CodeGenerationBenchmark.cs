// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Performance
{
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
            var fileSystem = RazorProjectFileSystem.Create(root.FullName);
            
            ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, b => RazorExtensions.Register(b)); ;

            MSN = fileSystem.GetItem(Path.Combine(root.FullName, "MSN.cshtml"));
        }

        public RazorProjectEngine ProjectEngine { get; }

        public RazorProjectItem MSN { get; }

        [Benchmark(Description = "Razor Design Time Code Generation of MSN.com")]
        public void CodeGeneration_DesignTime_LargeStaticFile()
        {
            var codeDocument = ProjectEngine.ProcessDesignTime(MSN);
            var generated = codeDocument.GetCSharpDocument();

            if (generated.Diagnostics.Count != 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, generated.Diagnostics));
            }
        }

        [Benchmark(Description = "Razor Runtime Code Generation of MSN.com")]
        public void CodeGeneration_Runtime_LargeStaticFile()
        {
            var codeDocument = ProjectEngine.Process(MSN);
            var generated = codeDocument.GetCSharpDocument();

            if (generated.Diagnostics.Count != 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, generated.Diagnostics));
            }
        }
    }
}
