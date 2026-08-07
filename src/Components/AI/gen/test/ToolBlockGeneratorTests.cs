// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Components.AI.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Components.AI.SourceGenerators.Tests;

public class ToolBlockGeneratorTests
{
    // Stub declarations to satisfy the generator's semantic model without referencing the main library
    private const string StubTypes = """
        namespace Microsoft.Extensions.AI
        {
            public abstract class AIContent { }
            public class FunctionCallContent : AIContent
            {
                public string CallId { get; set; }
                public string Name { get; set; }
                public System.Collections.Generic.IDictionary<string, object> Arguments { get; set; }
            }
            public class FunctionResultContent : AIContent
            {
                public string CallId { get; set; }
                public object Result { get; set; }
            }
        }

        namespace Microsoft.AspNetCore.Components.AI
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class ToolBlockAttribute : System.Attribute
            {
                public string ToolName { get; }
                public ToolBlockAttribute(string toolName) { ToolName = toolName; }
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public sealed class ToolParameterAttribute : System.Attribute
            {
                public string? Name { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public sealed class ToolResultAttribute : System.Attribute
            {
                public string? Name { get; set; }
            }

            public class ContentBlock { }

            public class FunctionInvocationContentBlock : ContentBlock
            {
                public Microsoft.Extensions.AI.FunctionCallContent? Call { get; set; }
                public Microsoft.Extensions.AI.FunctionResultContent? Result { get; set; }
                public string? Id { get; set; }
            }

            public abstract class ContentBlockHandler<TState> where TState : new()
            {
                public abstract BlockMappingResult<TState> Handle(BlockMappingContext context, TState state);
            }

            public struct BlockMappingResult<TState>
            {
                public static BlockMappingResult<TState> Emit(object block, TState state) => default;
                public static BlockMappingResult<TState> Complete() => default;
                public static BlockMappingResult<TState> Pass() => default;
            }

            public class BlockMappingContext
            {
                public UnhandledContentEnumerator UnhandledContents => default;
                public void MarkHandled(Microsoft.Extensions.AI.AIContent content) { }
            }

            public struct UnhandledContentEnumerator
            {
                public UnhandledContentEnumerator GetEnumerator() => this;
                public bool MoveNext() => false;
                public Microsoft.Extensions.AI.AIContent Current => null!;
            }

            public class UIAgentOptions
            {
                public void AddBlockHandler<TState>(ContentBlockHandler<TState> handler) where TState : new() { }
            }
        }
        """;

    [Fact]
    public void HappyPath_GeneratesHandlerAndRegistration()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public partial class WeatherToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string Location { get; set; }

                [ToolParameter(Name = "units")]
                public string TemperatureUnits { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.True(result.GeneratedTrees.Length >= 2, $"Expected at least 2 generated files, got {result.GeneratedTrees.Length}");

        var handlerSource = GetGeneratedSource(result, "WeatherToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.Contains("get_weather", handlerSource);
        Assert.Contains("state.Location", handlerSource);
        Assert.Contains("state.TemperatureUnits", handlerSource);
        Assert.Contains("\"Location\"", handlerSource);
        Assert.Contains("\"units\"", handlerSource);

        var registrationSource = GetGeneratedSource(result, "GeneratedToolBlockRegistrations.g.cs");
        Assert.NotNull(registrationSource);
        Assert.Contains("AddGeneratedToolBlocks", registrationSource);
        Assert.Contains("WeatherToolBlockHandler", registrationSource);
    }

    [Fact]
    public void NameOverride_UsesSpecifiedArgumentKey()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("search")]
            public partial class SearchToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter(Name = "q")]
                public string Query { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "SearchToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.Contains("\"q\"", handlerSource);
        Assert.Contains("state.Query", handlerSource);
    }

    [Fact]
    public void MultipleToolBlocks_AllHandlersRegistered()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public partial class WeatherToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string Location { get; set; }
            }

            [ToolBlock("search")]
            public partial class SearchToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string Query { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.True(result.GeneratedTrees.Length >= 3, $"Expected at least 3 generated files (2 handlers + registration), got {result.GeneratedTrees.Length}");

        var registrationSource = GetGeneratedSource(result, "GeneratedToolBlockRegistrations.g.cs");
        Assert.NotNull(registrationSource);
        Assert.Contains("WeatherToolBlockHandler", registrationSource);
        Assert.Contains("SearchToolBlockHandler", registrationSource);
    }

    [Fact]
    public void NonPartialClass_NoHandlerGenerated()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public class WeatherToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string Location { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        var handlerSource = GetGeneratedSource(result, "WeatherToolBlockHandler.g.cs");
        Assert.Null(handlerSource);
    }

    [Fact]
    public void AbstractClass_NoHandlerGenerated()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public abstract partial class WeatherToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string Location { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        var handlerSource = GetGeneratedSource(result, "WeatherToolBlockHandler.g.cs");
        Assert.Null(handlerSource);
    }

    [Fact]
    public void GenericClass_NoHandlerGenerated()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public partial class WeatherToolBlock<T> : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string Location { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        var handlerSource = GetGeneratedSource(result, "WeatherToolBlockHandler.g.cs");
        Assert.Null(handlerSource);
    }

    [Fact]
    public void WrongBaseClass_NoHandlerGenerated()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public partial class WeatherToolBlock : ContentBlock
            {
                [ToolParameter]
                public string Location { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        var handlerSource = GetGeneratedSource(result, "WeatherToolBlockHandler.g.cs");
        Assert.Null(handlerSource);
    }

    [Fact]
    public void IntParameter_UsesGetInt32Deserialization()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("set_count")]
            public partial class CountToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public int Count { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "CountToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.Contains("GetInt32", handlerSource);
    }

    [Fact]
    public void BoolParameter_UsesGetBooleanDeserialization()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("set_flag")]
            public partial class FlagToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public bool IsEnabled { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "FlagToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.Contains("GetBoolean", handlerSource);
    }

    [Fact]
    public void NoParameters_GeneratesHandlerWithoutArgumentDeserialization()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("ping")]
            public partial class PingToolBlock : FunctionInvocationContentBlock
            {
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "PingToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.DoesNotContain("args.TryGetValue", handlerSource);
    }

    [Fact]
    public void PropertyWithoutSetter_IsSkipped()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("test")]
            public partial class TestToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string ReadOnly { get; }

                [ToolParameter]
                public string Writable { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        var handlerSource = GetGeneratedSource(result, "TestToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.DoesNotContain("state.ReadOnly", handlerSource);
        Assert.Contains("state.Writable", handlerSource);
    }

    [Fact]
    public void EmptyRegistration_WhenNoToolBlocks()
    {
        var source = """
            namespace TestApp;

            public class SomeClass { }
            """;

        var (result, diagnostics) = RunGenerator(source);

        var registrationSource = GetGeneratedSource(result, "GeneratedToolBlockRegistrations.g.cs");
        // No registration file should be generated when no tool blocks exist
        Assert.Null(registrationSource);
    }

    [Fact]
    public void DuplicateToolName_ReportsDiagnostic()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public partial class WeatherToolBlock : FunctionInvocationContentBlock
            {
            }

            [ToolBlock("get_weather")]
            public partial class AnotherWeatherToolBlock : FunctionInvocationContentBlock
            {
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "BAIC008");
    }

    [Fact]
    public void SingleToolResult_MapsEntireResultValue()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_weather")]
            public partial class WeatherToolBlock : FunctionInvocationContentBlock
            {
                [ToolParameter]
                public string Location { get; set; }

                [ToolResult]
                public string Forecast { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "WeatherToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.Contains("state.Forecast", handlerSource);
        Assert.Contains("resultContent.Result", handlerSource);
        Assert.Contains("GetString", handlerSource);
    }

    [Fact]
    public void ToolResult_NameOverride_UsesSpecifiedKey()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_data")]
            public partial class DataToolBlock : FunctionInvocationContentBlock
            {
                [ToolResult(Name = "temp")]
                public double Temperature { get; set; }

                [ToolResult(Name = "desc")]
                public string Description { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "DataToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.Contains("\"temp\"", handlerSource);
        Assert.Contains("\"desc\"", handlerSource);
        Assert.Contains("state.Temperature", handlerSource);
        Assert.Contains("state.Description", handlerSource);
    }

    [Fact]
    public void MultipleToolResults_DeserializesFromJsonObject()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("get_data")]
            public partial class DataToolBlock : FunctionInvocationContentBlock
            {
                [ToolResult]
                public double Temperature { get; set; }

                [ToolResult]
                public string Conditions { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "DataToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.Contains("TryGetProperty", handlerSource);
        Assert.Contains("\"Temperature\"", handlerSource);
        Assert.Contains("\"Conditions\"", handlerSource);
        Assert.Contains("state.Temperature", handlerSource);
        Assert.Contains("state.Conditions", handlerSource);
    }

    [Fact]
    public void ToolResultWithoutSetter_IsSkipped()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("test")]
            public partial class TestToolBlock : FunctionInvocationContentBlock
            {
                [ToolResult]
                public string ReadOnly { get; }

                [ToolResult]
                public string Writable { get; set; }
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        var handlerSource = GetGeneratedSource(result, "TestToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.DoesNotContain("state.ReadOnly", handlerSource);
        Assert.Contains("state.Writable", handlerSource);
    }

    [Fact]
    public void NoToolResults_HandlerHasNoResultDeserialization()
    {
        var source = """
            using Microsoft.AspNetCore.Components.AI;

            namespace TestApp;

            [ToolBlock("ping")]
            public partial class PingToolBlock : FunctionInvocationContentBlock
            {
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var handlerSource = GetGeneratedSource(result, "PingToolBlockHandler.g.cs");
        Assert.NotNull(handlerSource);
        Assert.DoesNotContain("resultContent.Result is not null", handlerSource);
    }

    private static (GeneratorDriverRunResult result, ImmutableArray<Diagnostic> diagnostics) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var stubTree = CSharpSyntaxTree.ParseText(StubTypes);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IDictionary<,>).Assembly.Location),
        };

        // Add the runtime assembly for netcoreapp
        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var runtimeRefs = new List<MetadataReference>(references);
        var runtimePath = System.IO.Path.Combine(runtimeDir, "System.Runtime.dll");
        if (System.IO.File.Exists(runtimePath))
        {
            runtimeRefs.Add(MetadataReference.CreateFromFile(runtimePath));
        }

        var collectionsPath = System.IO.Path.Combine(runtimeDir, "System.Collections.dll");
        if (System.IO.File.Exists(collectionsPath))
        {
            runtimeRefs.Add(MetadataReference.CreateFromFile(collectionsPath));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree, stubTree],
            runtimeRefs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new ToolBlockGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var generatorDiagnostics);

        var result = driver.GetRunResult();
        return (result, generatorDiagnostics);
    }

    private static string? GetGeneratedSource(GeneratorDriverRunResult result, string hintName)
    {
        foreach (var tree in result.GeneratedTrees)
        {
            if (tree.FilePath.EndsWith(hintName, StringComparison.OrdinalIgnoreCase))
            {
                return tree.GetText().ToString();
            }
        }

        return null;
    }
}
