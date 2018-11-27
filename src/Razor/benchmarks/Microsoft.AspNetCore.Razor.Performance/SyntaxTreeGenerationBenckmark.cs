// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class SyntaxTreeGenerationBenchmark
    {
        public SyntaxTreeGenerationBenchmark()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "MSN.cshtml")))
            {
                current = current.Parent;
            }

            var root = current;
            var fileSystem = RazorProjectFileSystem.Create(root.FullName);

            ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, b => RazorExtensions.Register(b)); ;

            var projectItem = fileSystem.GetItem(Path.Combine(root.FullName, "MSN.cshtml"));
            MSN = RazorSourceDocument.ReadFrom(projectItem);

            var directiveFeature = ProjectEngine.EngineFeatures.OfType<IRazorDirectiveFeature>().FirstOrDefault();
            Directives = directiveFeature?.Directives.ToArray() ?? Array.Empty<DirectiveDescriptor>();
        }

        public RazorProjectEngine ProjectEngine { get; }

        public RazorSourceDocument MSN { get; }

        public DirectiveDescriptor[] Directives { get; }

        [Benchmark(Description = "Razor Design Time Syntax Tree Generation of MSN.com")]
        public void SyntaxTreeGeneration_DesignTime_LargeStaticFile()
        {
            var options = RazorParserOptions.CreateDesignTime(o =>
            {
                foreach (var directive in Directives)
                {
                    o.Directives.Add(directive);
                }
            });
            var syntaxTree = RazorSyntaxTree.Parse(MSN, options);

            if (syntaxTree.Diagnostics.Count != 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, syntaxTree.Diagnostics));
            }
        }

        [Benchmark(Description = "Razor Runtime Syntax Tree Generation of MSN.com")]
        public void SyntaxTreeGeneration_Runtime_LargeStaticFile()
        {
            var options = RazorParserOptions.Create(o =>
            {
                foreach (var directive in Directives)
                {
                    o.Directives.Add(directive);
                }
            });
            var syntaxTree = RazorSyntaxTree.Parse(MSN, options);

            if (syntaxTree.Diagnostics.Count != 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, syntaxTree.Diagnostics));
            }
        }
    }
}
