// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Generators.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Http.Microbenchmarks;

public class RequestDelegateGeneratorBenchmarks : RequestDelegateCreationTestBase
{
    protected override bool IsGeneratorEnabled => true;

    [Params(10, 100, 1000, 10000)]
    public int EndpointCount { get; set; }

    private GeneratorDriver _driver;
    private Compilation _compilation;

    [GlobalSetup]
    public async Task Setup()
    {
        var project = CreateProject();
        var innerSource = "";
        for (var i = 0; i < EndpointCount; i++)
        {
            innerSource += $"""app.MapGet("/route{i}", (int? id) => "Hello World!");""";
        }
        var source = GetMapActionString(innerSource);
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        _compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        _driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
    }

    [Benchmark]
    public void CreateRequestDelegate()
    {
        _driver.RunGeneratorsAndUpdateCompilation(_compilation, out var _, out var _);
    }
}
