// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class CompileTimeCreationTests : RequestDelegateCreationTests
{
    protected override bool IsGeneratorEnabled { get; } = true;

    [Fact]
    public async Task MapGet_WithRequestDelegate_DoesNotGenerateSources()
    {
        var (generatorRunResult, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (HttpContext context) => Task.CompletedTask);
""");
        var results = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        Assert.Empty(GetStaticEndpoints(results, GeneratorSteps.EndpointModelStep));

        var endpoint = GetEndpointFromCompilation(compilation, false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "");
    }

    [Fact]
    public async Task MapAction_ExplicitRouteParamWithInvalidName_SimpleReturn()
    {
        var source = $$"""app.MapGet("/{routeValue}", ([FromRoute(Name = "invalidName" )] string parameterName) => parameterName);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => endpoint.RequestDelegate(httpContext));
        Assert.Equal("'invalidName' is not a route parameter.", exception.Message);
    }

    [Fact]
    public async Task SupportsSameInterceptorsFromDifferentFiles()
    {
        var project = CreateProject();
        var source = GetMapActionString("""app.MapGet("/", (string name) => "Hello {name}!");app.MapGet("/bye", (string name) => "Bye {name}!");""");
        var otherSource = GetMapActionString("""app.MapGet("/", (string name) => "Hello {name}!");""", "OtherTestMapActions");
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        project = project.AddDocument("OtherTestMapActions.cs", SourceText.From(otherSource, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var _);

        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));

        await VerifyAgainstBaselineUsingFile(updatedCompilation);
    }

    [Fact]
    public async Task SupportsDifferentInterceptorsFromSameLocation()
    {
        var project = CreateProject();
        var source = GetMapActionString("""app.MapGet("/", (string name) => "Hello {name}!");""");
        var otherSource = GetMapActionString("""app.MapGet("/", (int age) => "Hello {age}!");""", "OtherTestMapActions");
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        project = project.AddDocument("OtherTestMapActions.cs", SourceText.From(otherSource, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var _);

        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));

        await VerifyAgainstBaselineUsingFile(updatedCompilation);
    }

    [Fact]
    public async Task SupportsMapCallOnNewLine()
    {
        var source = """
app
     .MapGet("/hello1/{id}", (int id) => $"Hello {id}!");
EndpointRouteBuilderExtensions
    .MapGet(app, "/hello2/{id}", (int id) => $"Hello {id}!");
app.
    MapGet("/hello1/{id}", (int id) => $"Hello {id}!");
EndpointRouteBuilderExtensions.
   MapGet(app, "/hello2/{id}", (int id) => $"Hello {id}!");
app.
MapGet("/hello1/{id}", (int id) => $"Hello {id}!");
EndpointRouteBuilderExtensions.
MapGet(app, "/hello2/{id}", (int id) => $"Hello {id}!");
app.


MapGet("/hello1/{id}", (int id) => $"Hello {id}!");
EndpointRouteBuilderExtensions.


MapGet(app, "/hello2/{id}", (int id) => $"Hello {id}!");
app.
MapGet
("/hello1/{id}", (int id) => $"Hello {id}!");
EndpointRouteBuilderExtensions.
   MapGet
(app, "/hello2/{id}", (int id) => $"Hello {id}!");
app
.
MapGet
("/hello1/{id}", (int id) => $"Hello {id}!");
EndpointRouteBuilderExtensions
.
   MapGet
(app, "/hello2/{id}", (int id) => $"Hello {id}!");
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        for (int i = 0; i < endpoints.Length; i++)
        {
            var httpContext = CreateHttpContext();
            httpContext.Request.RouteValues["id"] = i.ToString(CultureInfo.InvariantCulture);
            await endpoints[i].RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, $"Hello {i}!");
        }
    }

    [Fact]
    public async Task SourceMapsAllPathsInAttribute()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var mappedDirectory = Path.Combine(currentDirectory, "path", "mapped");
        var project = CreateProject(modifyCompilationOptions:
            (options) =>
            {
                return options.WithSourceReferenceResolver(
                    new SourceFileResolver(ImmutableArray<string>.Empty, currentDirectory, ImmutableArray.Create(new KeyValuePair<string, string>(currentDirectory, mappedDirectory))));
            });
        var source = GetMapActionString("""app.MapGet("/", () => "Hello world!");""");
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8), filePath: Path.Combine(currentDirectory, "TestMapActions.cs")).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diags);

        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));

        var endpoint = GetEndpointFromCompilation(updatedCompilation);
        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmitsDiagnosticForUnsupportedAnonymousMethod(bool isAsync)
    {
        var source = isAsync
            ? @"app.MapGet(""/hello"", async (int value) => await Task.FromResult(new { Delay = value }));"
            : @"app.MapGet(""/hello"", (int value) => new { Delay = value });";
        var (generatorRunResult, compilation) = await RunGeneratorAsync(source);

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveAnonymousReturnType.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public async Task EmitsDiagnosticForGenericTypeParam()
    {
        var source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class RouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTestEndpoints<T>(this IEndpointRouteBuilder app) where T : class
    {
        app.MapGet("/", (T value) => "Hello world!");
        app.MapGet("/", () => new T());
        app.MapGet("/", (Wrapper<T> value) => "Hello world!");
        app.MapGet("/", async () =>
        {
            await Task.CompletedTask;
            return new T();
        });
        return app;
    }
}

file class Wrapper<T> { }
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diagnostics);
        var generatorRunResult = driver.GetRunResult();

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(Assert.Single(generatorRunResult.Results));
        Assert.Empty(result.GeneratedSources);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal(DiagnosticDescriptors.TypeParametersNotSupported.Id, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        });
    }

    [Theory]
    [InlineData("protected")]
    [InlineData("private")]
    public async Task EmitsDiagnosticForPrivateOrProtectedTypes(string accessibility)
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class RouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTestEndpoints<T>(this IEndpointRouteBuilder app) where T : class
    {
        app.MapGet("/", (MyType value) => "Hello world!");
        app.MapGet("/", () => new MyType());
        app.MapGet("/", (Wrapper<MyType> value) => "Hello world!");
        app.MapGet("/", async () =>
        {
            await Task.CompletedTask;
            return new MyType();
        });
        return app;
    }

    {{accessibility}} class MyType { }
}

public class Wrapper<T> { }
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diagnostics);
        var generatorRunResult = driver.GetRunResult();

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(Assert.Single(generatorRunResult.Results));
        Assert.Empty(result.GeneratedSources);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal(DiagnosticDescriptors.InaccessibleTypesNotSupported.Id, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        });
    }

    [Fact]
    public async Task HandlesEndpointsWithAndWithoutDiagnostics()
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class TestMapActions
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/a", (MyType? value) => "Hello world!");
        app.MapGet("/b", () => "Hello world!");
        app.MapPost("/c", (Wrapper<MyType>? value) => "Hello world!");
        return app;
    }

    private class MyType { }
}

public class Wrapper<T> { }
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diagnostics);
        var generatorRunResult = driver.GetRunResult();

        // Emits diagnostic and generates source for all endpoints
        var result = Assert.IsType<GeneratorRunResult>(Assert.Single(generatorRunResult.Results));
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal(DiagnosticDescriptors.InaccessibleTypesNotSupported.Id, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        });

        // All endpoints can be invoked
        var endpoints = GetEndpointsFromCompilation(updatedCompilation, skipGeneratedCodeCheck: true);
        foreach (var endpoint in endpoints)
        {
            var httpContext = CreateHttpContext();
            await endpoint.RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, "Hello world!");
        }

        await VerifyAgainstBaselineUsingFile(updatedCompilation);
    }

    [Fact]
    public async Task MapAction_BindAsync_NullableReturn()
    {
        var source = $$"""
app.MapGet("/class", (BindableClassWithNullReturn param) => "Hello world!");
app.MapGet("/class-with-filter", (BindableClassWithNullReturn param) => "Hello world!")
    .AddEndpointFilter((c, n) => n(c));
app.MapGet("/null-struct", (BindableStructWithNullReturn param) => "Hello world!");
app.MapGet("/null-struct-with-filter", (BindableStructWithNullReturn param) => "Hello world!")
    .AddEndpointFilter((c, n) => n(c));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        foreach (var endpoint in endpoints)
        {
            var httpContext = CreateHttpContext();
            await endpoint.RequestDelegate(httpContext);

            Assert.Equal(400, httpContext.Response.StatusCode);
        }

        Assert.All(TestSink.Writes, context => Assert.Equal("RequiredParameterNotProvided", context.EventId.Name));
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_BindAsync_StructType()
    {
        var source = $$"""
app.MapGet("/struct", (BindableStruct param) => $"Hello {param.Value}!");
app.MapGet("/struct-with-filter", (BindableStruct param) => $"Hello {param.Value}!")
     .AddEndpointFilter((c, n) => n(c));
app.MapGet("/optional-struct", (BindableStruct? param) => $"Hello {param?.Value}!");
app.MapGet("/optional-struct-with-filter", (BindableStruct? param) => $"Hello {param?.Value}!")
     .AddEndpointFilter((c, n) => n(c));
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        foreach (var endpoint in endpoints)
        {
            var httpContext = CreateHttpContext();
            httpContext.Request.QueryString = QueryString.Create("value", endpoint.DisplayName);
            await endpoint.RequestDelegate(httpContext);

            await VerifyResponseBodyAsync(httpContext, $"Hello {endpoint.DisplayName}!");
        }
    }

    [Fact]
    public async Task MapAction_NoJsonTypeInfoResolver_ThrowsException()
    {
        var source = """
app.MapGet("/", () => "Hello world!");
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolver = null);
        });
        var exception = Assert.Throws<InvalidOperationException>(() => GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider));
        Assert.Equal("JsonSerializerOptions instance must specify a TypeInfoResolver setting before being marked as read-only.", exception.Message);
    }

    public static IEnumerable<object[]> NullResultWithNullAnnotation
    {
        get
        {
            return new List<object[]>
            {
                new object[] { "IResult? () => null", "The IResult returned by the Delegate must not be null." },
                new object[] { "Task<IResult?>? () => null", "The Task returned by the Delegate must not be null." },
                new object[] { "Task<bool?>? () => null", "The Task returned by the Delegate must not be null." },
                new object[] { "Task<IResult?> () => Task.FromResult<IResult?>(null)", "The IResult returned by the Delegate must not be null." },
                new object[] { "ValueTask<IResult?> () => ValueTask.FromResult<IResult?>(null)", "The IResult returned by the Delegate must not be null." },
            };
        }
    }

    [Theory]
    [MemberData(nameof(NullResultWithNullAnnotation))]
    public async Task RequestDelegateThrowsInvalidOperationExceptionOnNullDelegate(string innerSource, string message)
    {
        var source = $"""
app.MapGet("/", {innerSource});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endpoint.RequestDelegate(httpContext));

        Assert.Equal(message, exception.Message);
    }

    [Theory]
    [InlineData("Task<bool> () => null!")]
    [InlineData("Task<bool?> () => null!")]
    public async Task AwaitableRequestDelegateThrowsNullReferenceExceptionOnUnannotatedNullDelegate(string innerSource)
    {
        var source = $"""
app.MapGet("/", {innerSource});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        _ = await Assert.ThrowsAsync<NullReferenceException>(async () => await endpoint.RequestDelegate(httpContext));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[FromRoute]")]
    [InlineData("[FromQuery]")]
    [InlineData("[FromHeader]")]
    public async Task SupportsHandlersWithSameSignatureButDifferentParameterNames(string sourceAttribute)
    {
        // Arrange
        var source = $$"""
app.MapGet("/camera/archive/{cameraId}/{indexName}", ({{sourceAttribute}}string? cameraId, {{sourceAttribute}}string? indexName) =>
{
    return "Your id: " + cameraId + " and index name: " + indexName;
});

app.MapGet("/camera/archive/{cameraId}/chunk/{chunkName}", ({{sourceAttribute}}string? cameraId, {{sourceAttribute}}string? chunkName) =>
{
    return "Your id: " + cameraId + " and chunk name: " + chunkName;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        // Act - 1
        var httpContext1 = CreateHttpContext();
        PopulateHttpContext(httpContext1, sourceAttribute, "cameraId");
        PopulateHttpContext(httpContext1, sourceAttribute, "indexName");
        var endpoint1 = endpoints[0];
        await endpoint1.RequestDelegate(httpContext1);

        // Act - 2
        var httpContext2 = CreateHttpContext();
        PopulateHttpContext(httpContext2, sourceAttribute, "cameraId");
        PopulateHttpContext(httpContext2, sourceAttribute, "chunkName");
        var endpoint2 = endpoints[1];
        await endpoint2.RequestDelegate(httpContext2);

        // Assert - 1
        await VerifyResponseBodyAsync(httpContext1, "Your id: cameraId and index name: indexName");

        // Assert - 2
        await VerifyResponseBodyAsync(httpContext2, "Your id: cameraId and chunk name: chunkName");

        void PopulateHttpContext(HttpContext httpContext, string sourceAttribute, string value)
        {
            switch (sourceAttribute)
            {
                case "[FromQuery]":
                    httpContext.Request.QueryString = httpContext.Request.QueryString.Add(QueryString.Create(value, value));
                    break;
                case "[FromHeader]":
                    httpContext.Request.Headers[value] = value;
                    break;
                default:
                    httpContext.Request.RouteValues[value] = value;
                    break;
            }
        }
    }

    [Fact]
    public async Task SupportsHandlersWithSameSignatureButDifferentParameterNamesFromInferredJsonBody()
    {
        // Arrange
        var source = """
app.MapPost("/todo", (Todo todo) => todo.Id.ToString());
app.MapPost("/todo1", (Todo todo1) => todo1.Id.ToString());
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        // Act - 1
        var httpContext1 = CreateHttpContext();
        var endpoint1 = endpoints[0];
        await endpoint1.RequestDelegate(httpContext1);

        // Act - 2
        var httpContext2 = CreateHttpContext();
        var endpoint2 = endpoints[1];
        await endpoint2.RequestDelegate(httpContext2);

        var logs = TestSink.Writes.ToArray();

        // Assert - 1
        Assert.Equal(400, httpContext1.Response.StatusCode);
        var log1 = logs.FirstOrDefault();
        Assert.NotNull(log1);
        Assert.Equal(LogLevel.Debug, log1.LogLevel);
        Assert.Equal(new EventId(5, "ImplicitBodyNotProvided"), log1.EventId);
        Assert.Equal(@"Implicit body inferred for parameter ""todo"" but no body was provided. Did you mean to use a Service instead?", log1.Message);

        // Assert - 2
        Assert.Equal(400, httpContext2.Response.StatusCode);
        var log2 = logs.LastOrDefault();
        Assert.NotNull(log2);
        Assert.Equal(LogLevel.Debug, log2.LogLevel);
        Assert.Equal(new EventId(5, "ImplicitBodyNotProvided"), log2.EventId);
        Assert.Equal(@"Implicit body inferred for parameter ""todo1"" but no body was provided. Did you mean to use a Service instead?", log2.Message);
    }

    [Theory]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return("Hello"));""")]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new Todo { Name = "Hello" }));""")]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(TypedResults.Ok(new Todo { Name = "Hello" })));""")]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(default(Microsoft.FSharp.Core.Unit)!));""")]
    public async Task MapAction_NoParam_FSharpAsyncReturn_NotCoercedToTaskAtCompileTime(string source)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            Assert.False(endpointModel.Response.IsAwaitable);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody: "{}");
    }

    [Theory]
    [InlineData("""app.MapGet("/", () => Task.FromResult(default(Microsoft.FSharp.Core.Unit)!));""")]
    [InlineData("""app.MapGet("/", () => ValueTask.FromResult(default(Microsoft.FSharp.Core.Unit)!));""")]
    public async Task MapAction_NoParam_TaskLikeOfUnitReturn_NotConvertedToVoidReturningAtCompileTime(string source)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            Assert.True(endpointModel.Response.IsAwaitable);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody: "null");
    }

    [Fact]
    public async Task SkipsMapWithIncorrectNamespaceAndAssembly()
    {
        var source = """
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace TestApp
{
    public static class TestMapActions
    {
        public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
        {
            app.ServiceProvider.Map(1, (string test) => "Hello world!");
            app.ServiceProvider.MapPost(2, (string test) => "Hello world!");
            app.Map(3, (string test) => "Hello world!");
            app.MapPost(4, (string test) => "Hello world!");
            return app;
        }
    }

    public static class EndpointRouteBuilderExtensions
    {
        public static IServiceProvider Map(this IServiceProvider app, int id, Delegate requestDelegate)
        {
            return app;
        }

        public static IEndpointRouteBuilder Map(this IEndpointRouteBuilder app, int id, Delegate requestDelegate)
        {
            return app;
        }
    }
}
namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IServiceProvider MapPost(this IServiceProvider app, int id, Delegate requestDelegate)
        {
            return app;
        }

        public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder app, int id, Delegate requestDelegate)
        {
            return app;
        }
    }
}
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var diagnostics);
        var generatorRunResult = driver.GetRunResult();

        // Emits diagnostic and generates source for all endpoints
        var result = Assert.IsType<GeneratorRunResult>(Assert.Single(generatorRunResult.Results));
        Assert.Empty(GetStaticEndpoints(result, GeneratorSteps.EndpointModelStep));
    }

    [Fact]
    public async Task TestHandlingOfGenericWithNullableReferenceTypes()
    {
        var source = """
#nullable enable
using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace TestApp
{
    public static class TestMapActions
    {
        public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/", (IGenericService<SomeInput, string?> service)
                => "Maybe? " + service.Get(new SomeInput(Random.Shared.Next())));
            return app;
        }
    }

    public interface IInputConstraint<TOutput>;

    public interface IGenericService<TInput, TOutput> where TInput : IInputConstraint<TOutput>
    {
        TOutput Get(TInput input);
    }

    public record SomeInput(int Value) : IInputConstraint<string?>;

    public class ConcreteService : IGenericService<SomeInput, string?>
    {
        public string? Get(SomeInput input) => input.Value % 2 == 0 ? input.Value.ToString(CultureInfo.InvariantCulture) : null;
    }
}
""";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[]
            {
                generator
            },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var _);

        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
    }

    [Fact]
    public async Task RequestDelegateGenerator_SkipsComplexFormParameter()
    {
        var source = """
app.MapPost("/", ([FromForm] Todo todo) => { });
app.MapPost("/", ([FromForm] Todo todo, IFormFile formFile) => { });
app.MapPost("/", ([FromForm] Todo todo, [FromForm] int[] ids) => { });
""";
        var (generatorRunResult, _) = await RunGeneratorAsync(source);

        // Emits diagnostics but no generated sources
        var result = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        Assert.Empty(result.GeneratedSources);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal(DiagnosticDescriptors.UnableToResolveParameterDescriptor.Id, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        });
    }

    // Test for https://github.com/dotnet/aspnetcore/issues/55840
    [Fact]
    public async Task RequestDelegatePopulatesFromOptionalFormParameterStringArray()
    {
        var source = """
app.MapPost("/", ([FromForm] string[]? message, HttpContext httpContext) =>
{
    httpContext.Items["message"] = message;
});
""";
        var (generatorRunResult, compilation) = await RunGeneratorAsync(source);
        var results = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        Assert.Single(GetStaticEndpoints(results, GeneratorSteps.EndpointModelStep));

        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["message"] = new(["hello", "bye"])
        });
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal<string[]>(["hello", "bye"], (string[])httpContext.Items["message"]);
    }
}
