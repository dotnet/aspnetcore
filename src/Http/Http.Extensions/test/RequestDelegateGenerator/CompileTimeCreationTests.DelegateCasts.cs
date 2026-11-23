// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class CompileTimeCreationTests
{
    [Theory]
    [InlineData("lambda")]
    [InlineData("method")]
    [InlineData("localFunction")]
    public async Task MapGet_DelegateCast_ParamsHandler(string handlerKind)
    {
        var handler = handlerKind switch
        {
            "lambda" => """(params int[] ids) => $"params {ids.Length}" """,
            "method" => "DelegateCastHandlers.Params",
            _ => "LocalHandler",
        };
        var localFunction = handlerKind == "localFunction" ? """string LocalHandler(params int[] ids) => $"params {ids.Length}";""" : "";
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
{{localFunction}}
app.MapGet("/", {{handler}});
""");
        Assert.Empty(result.Diagnostics);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?ids=1&ids=2");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "params 2");
    }

    [Theory]
    [InlineData("cast")]
    [InlineData("constructor")]
    [InlineData("readonlyField")]
    [InlineData("named")]
    [InlineData("namedDefault")]
    [InlineData("lambda")]
    public async Task MapGet_DelegateCast_ConvertedOptionalHandler(string handlerKind)
    {
        var handler = handlerKind switch
        {
            "cast" => "(Func<int, string>)DelegateCastHandlers.Page",
            "constructor" => "new Func<int, string>(DelegateCastHandlers.Page)",
            "readonlyField" => "DelegateCastHandlers.PageField",
            "named" => "(SizeHandler)DelegateCastHandlers.Page",
            "namedDefault" => "(PageHandler)DelegateCastHandlers.Page",
            _ => """(Func<int, string>)((int page = 1) => $"page {page}")""",
        };
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
#pragma warning disable CS9099
app.MapGet("/", {{handler}});
""");
        Assert.Empty(result.Diagnostics);
        var endpoint = GetEndpointFromCompilation(compilation);

        var defaultContext = CreateHttpContext();
        await endpoint.RequestDelegate(defaultContext);
        await VerifyResponseBodyAsync(defaultContext, "page 1");

        var explicitContext = CreateHttpContext();
        explicitContext.Request.QueryString = new QueryString("?page=7");
        await endpoint.RequestDelegate(explicitContext);
        await VerifyResponseBodyAsync(explicitContext, "page 7");
    }

    [Theory]
    [InlineData("cast")]
    [InlineData("constructor")]
    [InlineData("readonlyField")]
    [InlineData("generic")]
    [InlineData("nestedGeneric")]
    [InlineData("keyword")]
    public async Task MapGet_DelegateCast_NamedHandler(string handlerKind)
    {
        var handler = handlerKind switch
        {
            "cast" => "(SizeHandler)DelegateCastHandlers.Size",
            "constructor" => "new SizeHandler(DelegateCastHandlers.Size)",
            "generic" => "(GenericHandler<string>)DelegateCastHandlers.Size",
            "nestedGeneric" => "(NestedDelegateCastHandlers<string>.SizeHandler)DelegateCastHandlers.Size",
            "keyword" => "(@class)DelegateCastHandlers.Size",
            _ => "DelegateCastHandlers.SizeField",
        };
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""app.MapGet("/", {{handler}});""");
        Assert.Empty(result.Diagnostics);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?size=5");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "size 5");
    }

    [Fact]
    public async Task MapGet_DelegateCast_InaccessibleTypeArgumentFallsBack()
    {
        var (result, compilation) = await RunDelegateCastGeneratorAsync("GenericDelegateCastHandlers.Map(app);", additionalSource: """
internal static class GenericDelegateCastHandlers
{
    private sealed class PrivateType;

    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (GenericHandler<PrivateType>)DelegateCastHandlers.Size);
    }
}
""");
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?size=5");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "size 5");
    }

    [Theory]
    [InlineData("typeArgument")]
    [InlineData("arrayTypeArgument")]
    [InlineData("containingType")]
    public async Task MapGet_DelegateCast_OpenGenericTypeFallsBack(string handlerKind)
    {
        var handler = handlerKind switch
        {
            "typeArgument" => "GenericHandler<T>",
            "arrayTypeArgument" => "GenericHandler<T[]>",
            _ => "GenericDelegateCastHandlers<T>.NestedHandler",
        };
        var (result, compilation) = await RunDelegateCastGeneratorAsync("GenericDelegateCastHandlers.Map<string>(app);", additionalSource: $$"""
internal static class GenericDelegateCastHandlers
{
    internal static void Map<T>(IEndpointRouteBuilder app)
    {
        app.MapGet("/", ({{handler}})DelegateCastHandlers.Size);
    }
}

internal static class GenericDelegateCastHandlers<T>
{
    internal delegate string NestedHandler(int size);
}
""");
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?size=5");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "size 5");
    }

    [Theory]
    [InlineData("delegate")]
    [InlineData("containingType")]
    [InlineData("typeArgument")]
    [InlineData("arrayTypeArgument")]
    public async Task MapGet_DelegateCast_FileLocalTypeFallsBack(string handlerKind)
    {
        var handler = handlerKind switch
        {
            "delegate" => "FileLocalHandler",
            "containingType" => "FileLocalType.NestedHandler",
            "typeArgument" => "GenericHandler<FileLocalType>",
            _ => "GenericHandler<FileLocalType[]>",
        };
        var (result, compilation) = await RunDelegateCastGeneratorAsync("FileLocalDelegateCastHandlers.Map(app);", additionalSource: $$"""
internal static class FileLocalDelegateCastHandlers
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", ({{handler}})DelegateCastHandlers.Size);
    }
}

file delegate string FileLocalHandler(int size);

file class FileLocalType
{
    internal delegate string NestedHandler(int size);
}
""");
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?size=5");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "size 5");
    }

    [Theory]
    [InlineData("params", false)]
    [InlineData("params", true)]
    [InlineData("named", false)]
    [InlineData("named", true)]
    [InlineData("optional", false)]
    [InlineData("optional", true)]
    public async Task MapGet_DelegateCast_DifferentDelegateTypes(string handlerKind, bool reverseOrder)
    {
        var (first, second, query, expectedFirst, expectedSecond) = handlerKind switch
        {
            "params" => (
                """app.MapGet("/first", (int[] ids) => $"array {ids.Length}");""",
                """app.MapGet("/second", (params int[] ids) => $"params {ids.Length}");""",
                "?ids=1&ids=2", "array 2", "params 2"),
            "named" => (
                """app.MapGet("/first", (Func<int, string>)DelegateCastHandlers.Size);""",
                """app.MapGet("/second", (SizeHandler)DelegateCastHandlers.Size);""",
                "?size=5", "size 5", "size 5"),
            _ => (
                """app.MapGet("/first", (int? page) => $"nullable {page}");""",
                """app.MapGet("/second", (Func<int?, string>)DelegateCastHandlers.NullablePage);""",
                "", "nullable ", "nullable "),
        };
        var source = reverseOrder ? $"{second}\n{first}" : $"{first}\n{second}";
        var (result, compilation) = await RunDelegateCastGeneratorAsync(source);
        Assert.Empty(result.Diagnostics);
        var models = GetStaticEndpoints(result, GeneratorSteps.EndpointModelStep);
        Assert.Equal(2, models.Length);
        Assert.False(RequestDelegateGenerator.StaticRouteHandlerModel.Endpoint.SignatureEquals(models[0], models[1]));
        var endpoints = GetEndpointsFromCompilation(compilation);
        Assert.Equal(2, endpoints.Length);

        for (var i = 0; i < endpoints.Length; i++)
        {
            var httpContext = CreateHttpContext();
            httpContext.Request.QueryString = new QueryString(query);
            await endpoints[i].RequestDelegate(httpContext);
            var expected = (i == 0) != reverseOrder ? expectedFirst : expectedSecond;
            await VerifyResponseBodyAsync(httpContext, expected);
        }
    }

    [Theory]
    [InlineData("inaccessible")]
    [InlineData("variantSignature")]
    public async Task MapGet_DelegateCast_UnsupportedDelegateFallsBack(string handlerKind)
    {
        var source = handlerKind == "inaccessible"
            ? "DelegateCastHandlers.MapPrivate(app);"
            : """app.MapGet("/", (Func<int, object>)DelegateCastHandlers.Size);""";
        var (result, compilation) = await RunDelegateCastGeneratorAsync(source, includeInaccessibleHandler: handlerKind == "inaccessible");
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?size=5");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "size 5");
    }

    private static async Task<(GeneratorRunResult Result, Compilation Compilation)> RunDelegateCastGeneratorAsync(string source, bool includeInaccessibleHandler = false, string additionalSource = "")
    {
        var inaccessibleHandler = includeInaccessibleHandler ? """
    private delegate string PrivateHandler(int size);

    internal static void MapPrivate(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (PrivateHandler)Size);
    }
""" : "";
        var project = CreateProject();
        project = project.AddDocument("TestMapActions.cs", SourceText.From(GetMapActionString(source), Encoding.UTF8)).Project;
        project = project.AddDocument("DelegateCastHandlers.cs", SourceText.From($$"""
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

internal delegate string SizeHandler(int size);
internal delegate string PageHandler(int page = 1);
internal delegate string GenericHandler<T>(int size);
internal delegate string @class(int size);

internal static class NestedDelegateCastHandlers<T>
{
    internal delegate string SizeHandler(int size);
}

internal static class DelegateCastHandlers
{
    internal static readonly Func<int, string> PageField = Page;
    internal static readonly SizeHandler SizeField = Size;

    internal static string Params(params int[] ids) => $"params {ids.Length}";
    internal static string Page(int page = 1) => $"page {page}";
    internal static string NullablePage(int? page = null) => $"nullable {page}";
    internal static string Size(int size) => $"size {size}";

{{inaccessibleHandler}}
}

{{additionalSource}}
""", Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);
        Assert.Empty(updatedCompilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning));

        return (Assert.Single(driver.GetRunResult().Results), updatedCompilation);
    }
}
