// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks
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

            MSN = fileSystem.GetItem(Path.Combine(root.FullName, "MSN.cshtml"), FileKinds.Legacy);
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
