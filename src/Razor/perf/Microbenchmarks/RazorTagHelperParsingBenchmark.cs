// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;
using static Microsoft.AspNetCore.Razor.Language.DefaultRazorTagHelperBinderPhase;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class RazorTagHelperParsingBenchmark
{
    public RazorTagHelperParsingBenchmark()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "taghelpers.json")))
        {
            current = current.Parent;
        }

        var root = current;

        var tagHelpers = ReadTagHelpers(Path.Combine(root.FullName, "taghelpers.json"));
        var tagHelperFeature = new StaticTagHelperFeature(tagHelpers);

        var blazorServerTagHelpersFilePath = Path.Combine(root.FullName, "BlazorServerTagHelpers.razor");

        var fileSystem = RazorProjectFileSystem.Create(root.FullName);
        ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem,
            b =>
            {
                RazorExtensions.Register(b);
                b.Features.Add(tagHelperFeature);
            });
        BlazorServerTagHelpersDemoFile = fileSystem.GetItem(Path.Combine(blazorServerTagHelpersFilePath), FileKinds.Component);

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

    [Benchmark(Description = "Component Directive Parsing")]
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

    private sealed class StaticTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
    {
        public StaticTagHelperFeature(IReadOnlyList<TagHelperDescriptor> descriptors) => Descriptors = descriptors;

        public IReadOnlyList<TagHelperDescriptor> Descriptors { get; }

        public IReadOnlyList<TagHelperDescriptor> GetDescriptors() => Descriptors;
    }
}
