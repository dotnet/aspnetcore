// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.SourceGenerators.Tests;

public class ToolBlockGeneratorTests
{
    [Fact]
    public void TypedBlock_GeneratesCompilableHandlerAndRegistration()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            public sealed class WeatherInfo
            {
                public int Temperature { get; set; }
            }

            [ToolBlock("get_weather")]
            public partial class WeatherToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter(Name = "location")]
                public string? Location { get; set; }

                [ToolResult]
                public WeatherInfo? Weather { get; set; }
            }
            """;

        var run = RunGenerator(source);

        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        var handler = GetGeneratedSource(run.Result, "WeatherToolBlockHandler.g.cs");
        Assert.Contains("fc.Name == \"get_weather\"", handler);
        Assert.Contains("args.TryGetValue(\"location\"", handler);
        Assert.Contains("JsonSerializer.Deserialize<global::TestApp.WeatherInfo>(__json)", handler);
        Assert.Contains("var shouldEmit = false;", handler);
        Assert.Contains("return shouldEmit", handler);
        var registration = GetGeneratedSource(
            run.Result,
            "GeneratedToolBlockRegistrations.g.cs");
        Assert.Contains("AddGeneratedToolBlocks", registration);
        Assert.Contains("WeatherToolBlockHandler", registration);
    }

    [Theory]
    [InlineData(
        """
        [Microsoft.AspNetCore.Components.AI.ToolBlock("tool")]
        public class InvalidBlock : Microsoft.AspNetCore.Components.AI.FunctionInvocationContentBlock { }
        """,
        "BAIC001")]
    [InlineData(
        """
        [Microsoft.AspNetCore.Components.AI.ToolBlock("tool")]
        public partial class InvalidBlock { }
        """,
        "BAIC002")]
    [InlineData(
        """
        [Microsoft.AspNetCore.Components.AI.ToolBlock("tool")]
        public abstract partial class InvalidBlock : Microsoft.AspNetCore.Components.AI.FunctionInvocationContentBlock { }
        """,
        "BAIC003")]
    [InlineData(
        """
        [Microsoft.AspNetCore.Components.AI.ToolBlock("tool")]
        public partial class InvalidBlock<T> : Microsoft.AspNetCore.Components.AI.FunctionInvocationContentBlock { }
        """,
        "BAIC004")]
    [InlineData(
        """
        [Microsoft.AspNetCore.Components.AI.ToolBlock("")]
        public partial class InvalidBlock : Microsoft.AspNetCore.Components.AI.FunctionInvocationContentBlock { }
        """,
        "BAIC005")]
    public void InvalidBlock_ReportsDeclarationDiagnostic(string source, string diagnosticId)
    {
        var run = RunGenerator(source);

        var diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal(diagnosticId, diagnostic.Id);
        Assert.DoesNotContain(
            run.Result.GeneratedTrees,
            tree => tree.FilePath.EndsWith("InvalidBlockHandler.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void NestedBlock_ReportsDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            public class Container
            {
                [ToolBlock("tool")]
                public partial class NestedBlock : FunctionInvocationContentBlock { }
            }
            """;

        var run = RunGenerator(source);

        var diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("BAIC009", diagnostic.Id);
    }

    [Fact]
    public void InvalidPropertyMappings_ReportDiagnostics()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            [ToolBlock("search")]
            public partial class SearchBlock : FunctionInvocationContentBlock
            {
                [ToolParameter(Name = "q")]
                public string? Query { get; set; }

                [ToolParameter(Name = "q")]
                public string? Duplicate { get; set; }

                [ToolParameter]
                public string? ReadOnly { get; }

                [ToolParameter]
                public string? PrivateParameter { get; private set; }

                [ToolParameter]
                public string? InitParameter { get; init; }

                [ToolResult(Name = "value")]
                public string? PrivateResult { get; private set; }

                [ToolResult(Name = "duplicate")]
                public string? FirstResult { get; set; }

                [ToolResult(Name = "duplicate")]
                public string? DuplicateResult { get; set; }
            }
            """;

        var run = RunGenerator(source);

        Assert.Equal(
            ["BAIC006", "BAIC007", "BAIC007", "BAIC007", "BAIC007", "BAIC010"],
            run.GeneratorDiagnostics
                .Select(diagnostic => diagnostic.Id)
                .Order()
                .ToArray());
        AssertNoCompilationErrors(run.OutputCompilation);
    }

    [Fact]
    public void DuplicateToolNames_ReportDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            [ToolBlock("get_weather")]
            public partial class WeatherBlock : FunctionInvocationContentBlock { }

            [ToolBlock("get_weather")]
            public partial class OtherWeatherBlock : FunctionInvocationContentBlock { }
            """;

        var run = RunGenerator(source);

        Assert.Contains(run.GeneratorDiagnostics, diagnostic => diagnostic.Id == "BAIC008");
    }

    [Fact]
    public void TypedParameters_UseTheirMatchingConversions()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            [ToolBlock("configure")]
            public partial class ConfigureBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public int Count { get; set; }

                [ToolParameter]
                public bool Enabled { get; set; }

                [ToolParameter]
                public double Ratio { get; set; }
            }
            """;

        var run = RunGenerator(source);

        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        var handler = GetGeneratedSource(run.Result, "ConfigureBlockHandler.g.cs");
        Assert.Contains("GetInt32()", handler);
        Assert.Contains("GetBoolean()", handler);
        Assert.Contains("GetDouble()", handler);
    }

    [Fact]
    public void MultipleResultProperties_HandleJsonStringsAndClrObjects()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            [ToolBlock("get_weather")]
            public partial class WeatherBlock : FunctionInvocationContentBlock
            {
                [ToolResult]
                public int Temperature { get; set; }

                [ToolResult]
                public string? Conditions { get; set; }
            }
            """;

        var run = RunGenerator(source);

        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        var handler = GetGeneratedSource(run.Result, "WeatherBlockHandler.g.cs");
        Assert.Contains(
            "JsonSerializer.Deserialize<global::System.Text.Json.JsonElement>(__json)",
            handler);
        Assert.Contains("JsonSerializer.SerializeToElement(resultContent.Result)", handler);
    }

    [Fact]
    public void BlocksWithSameNameInDifferentNamespaces_GetUniqueCompilableHandlers()
    {
        var source = """
            namespace First
            {
                [Microsoft.AspNetCore.Components.AI.ToolBlock("first")]
                public partial class StatusBlock : Microsoft.AspNetCore.Components.AI.FunctionInvocationContentBlock { }
            }

            namespace Second
            {
                [Microsoft.AspNetCore.Components.AI.ToolBlock("second")]
                public partial class StatusBlock : Microsoft.AspNetCore.Components.AI.FunctionInvocationContentBlock { }
            }
            """;

        var run = RunGenerator(source);

        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        var handlerHintNames = run.Result.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .Where(name => name.EndsWith("StatusBlockHandler.g.cs", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, handlerHintNames.Count);
        Assert.Equal(2, handlerHintNames.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SourceWithoutToolBlocks_DoesNotGenerateRegistration()
    {
        var run = RunGenerator("public sealed class OtherType { }");

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.Result.GeneratedTrees);
        AssertNoCompilationErrors(run.OutputCompilation);
    }

    private static GeneratorRun RunGenerator(string source)
    {
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.CSharp13);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ToolBlockGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        return new GeneratorRun(
            driver.GetRunResult(),
            outputCompilation,
            generatorDiagnostics);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
            {
                paths.Add(path);
            }
        }

        paths.Add(typeof(ToolBlockAttribute).Assembly.Location);
        paths.Add(typeof(FunctionCallContent).Assembly.Location);

        return paths.Select(path => MetadataReference.CreateFromFile(path));
    }

    private static void AssertNoCompilationErrors(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.True(
            errors.Count == 0,
            $"Generated consumer compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintName)
    {
        var tree = Assert.Single(
            result.GeneratedTrees,
            tree => tree.FilePath.EndsWith(hintName, StringComparison.OrdinalIgnoreCase));
        return tree.GetText().ToString();
    }

    private sealed record GeneratorRun(
        GeneratorDriverRunResult Result,
        Compilation OutputCompilation,
        ImmutableArray<Diagnostic> GeneratorDiagnostics);
}
