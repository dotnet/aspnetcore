// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.NativeAot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Testing.Tests.Generators;

public class NativeAotTestHarnessGeneratorTests
{
    [Fact]
    public void Generator_WhenDisabled_EmitsNoSource()
    {
        var result = RunGenerator(enabled: false);

        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Generator_WhenActivationPropertyIsMissing_EmitsNoSource()
    {
        var result = RunGenerator(enabled: false, includeProperty: false);

        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Generator_WhenEnabled_EmitsCompiledHarness()
    {
        var result = RunGenerator(enabled: true);

        var generatedTree = Assert.Single(result.GeneratedTrees);
        Assert.EndsWith("NativeAotTestHarness.g.cs", generatedTree.FilePath);

        var source = generatedTree.GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("HostingStartupAttribute", source);
        Assert.Contains("NativeAotTestHarnessHostingStartup", source);
        Assert.Contains("AddE2ETestHarness", source);
        Assert.Contains("\"E2E_READY_URL\"", source);
        Assert.Contains("\"TEST_PARENT_PID\"", source);
    }

    [Fact]
    public void Generator_WhenEnabled_ProducesStandaloneAotSafeSource()
    {
        var source = Assert.Single(RunGenerator(enabled: true).GeneratedTrees)
            .GetText(TestContext.Current.CancellationToken)
            .ToString();

        Assert.Contains("global::Microsoft.AspNetCore.Hosting.IHostingStartup", source);
        Assert.Contains("global::Microsoft.Extensions.Hosting.IHostedService", source);
        Assert.Contains("global::System.Diagnostics.Process.GetProcessById", source);
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Testing.Infrastructure", source);
        Assert.DoesNotContain("Microsoft.VisualStudio.TestTools", source);
        Assert.DoesNotContain("Microsoft.Playwright", source);
        Assert.DoesNotContain("Yarp", source);
        Assert.DoesNotContain("System.Reflection", source);
    }

    private static GeneratorDriverRunResult RunGenerator(bool enabled, bool includeProperty = true)
    {
        var compilation = CSharpCompilation.Create(
            "TestApp",
            [CSharpSyntaxTree.ParseText("internal static class Program { }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var generator = new NativeAotTestHarnessGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            optionsProvider: new TestAnalyzerConfigOptionsProvider(enabled, includeProperty));
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private sealed class TestAnalyzerConfigOptionsProvider(bool enabled, bool includeProperty) : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions = new TestAnalyzerConfigOptions(enabled, includeProperty);

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => EmptyAnalyzerConfigOptions.Instance;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => EmptyAnalyzerConfigOptions.Instance;
    }

    private sealed class TestAnalyzerConfigOptions(bool enabled, bool includeProperty) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (includeProperty && key == "build_property.E2ECompileTestHarness")
            {
                value = enabled ? "true" : "false";
                return true;
            }

            value = "";
            return false;
        }
    }

    private sealed class EmptyAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static EmptyAnalyzerConfigOptions Instance { get; } = new();

        public override bool TryGetValue(string key, out string value)
        {
            value = "";
            return false;
        }
    }
}
