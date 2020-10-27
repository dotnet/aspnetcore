// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;
using static Microsoft.AspNetCore.Razor.Language.DefaultRazorTagHelperBinderPhase;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class RazorTagHelperParsingBenchmark
    {
        public RazorTagHelperParsingBenchmark()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while  (current != null && !File.Exists(Path.Combine(current.FullName, "taghelpers.json")))
            {
                current = current.Parent;
            }

            var root = current;

            var tagHelpers = ReadTagHelpers(Path.Combine(root.FullName, "taghelpers.json"));
            var blazorServerTagHelpersFilePath = Path.Combine(root.FullName, "BlazorServerTagHelpers.razor");

            var fileSystem = RazorProjectFileSystem.Create(root.FullName);
            ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, b => RazorExtensions.Register(b));
            BlazorServerTagHelpersDemoFile = fileSystem.GetItem(Path.Combine(blazorServerTagHelpersFilePath), FileKinds.Legacy);

            ComponentDirectiveVisitor = new ComponentDirectiveVisitor(blazorServerTagHelpersFilePath, tagHelpers, currentNamespace: null);
            var codeDocument = ProjectEngine.ProcessDesignTime(BlazorServerTagHelpersDemoFile);
            SyntaxTree = codeDocument.GetSyntaxTree();
        }

        private RazorProjectEngine ProjectEngine { get; }
        private RazorProjectItem BlazorServerTagHelpersDemoFile { get; }
        private ComponentDirectiveVisitor ComponentDirectiveVisitor { get; }
        private RazorSyntaxTree SyntaxTree { get; }

        [Benchmark(Description = "TagHelper Design Time Processing")]
        public void TagHelper_ProcessDesignTime()
        {
            _ = ProjectEngine.ProcessDesignTime(BlazorServerTagHelpersDemoFile);
        }

        [Benchmark(Description = "TagHelper Component Directive Parsing")]
        public void TagHelper_ComponentDirectiveVisitor()
        {
            ComponentDirectiveVisitor.Visit(SyntaxTree);
        }

        private static IReadOnlyList<TagHelperDescriptor> ReadTagHelpers(string filePath)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new RazorDiagnosticJsonConverter());
            serializer.Converters.Add(new TagHelperDescriptorJsonConverter());

            using (var reader = new JsonTextReader(File.OpenText(filePath)))
            {
                return serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }
        }
    }
}
