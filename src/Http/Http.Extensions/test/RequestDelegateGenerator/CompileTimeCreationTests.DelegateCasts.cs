// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;

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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MapPost_DelegateCast_NamedHandlerWithNullableBody(bool isAsync, bool hasFilter)
    {
        var returnType = isAsync ? "Task<string>" : "string";
        var response = isAsync ? "Task.FromResult(input is null ? \"empty\" : \"present\")" : "input is null ? \"empty\" : \"present\"";
        var filter = hasFilter ? ".AddEndpointFilter((context, next) => next(context))" : "";
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
        app.MapPost("/", (PostHandler)Post){{filter}};
        static {{returnType}} Post(Input? input) => {{response}};
        """, additionalSource: $$"""
#nullable enable
public delegate {{(isAsync ? "System.Threading.Tasks.Task<string>" : "string")}} PostHandler(Input input);
public sealed class Input
{
    public string? Value { get; set; }
}
""", nullableEnabled: true);
        Assert.Empty(result.Diagnostics);
        var endpoint = GetEndpointFromCompilation(compilation);

        var emptyContext = CreateHttpContext();
        await endpoint.RequestDelegate(emptyContext);
        await VerifyResponseBodyAsync(emptyContext, "empty");

        var bodyContext = CreateHttpContextWithJson("""{"Value":"value"}""");
        await endpoint.RequestDelegate(bodyContext);
        await VerifyResponseBodyAsync(bodyContext, "present");
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

    [Theory]
    [InlineData("string?", false)]
    [InlineData("string?", true)]
    [InlineData("Task<string>?", false)]
    [InlineData("Task<string>?", true)]
    public async Task MapGet_DelegateCast_NullableReturnMismatchFallsBack(string returnType, bool hasFilter)
    {
        var response = returnType == "string?" ? "\"value\"" : "Task.FromResult(\"value\")";
        var methodReturnType = returnType.TrimEnd('?');
        var filter = hasFilter ? ".AddEndpointFilter((context, next) => next(context))" : "";
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
app.MapGet("/", (NullableReturnHandler)Handler){{filter}};
static {{methodReturnType}} Handler() => {{response}};
""", additionalSource: $$"""
internal delegate {{returnType.Replace("Task<", "System.Threading.Tasks.Task<")}} NullableReturnHandler();
""", nullableEnabled: true);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "value");
    }

    [Theory]
    [InlineData("List<string>", "List<string?>", false)]
    [InlineData("List<string>", "List<string?>", true)]
    [InlineData("List<string?>", "List<string>", false)]
    [InlineData("List<string?>", "List<string>", true)]
    public async Task MapPost_DelegateCast_NestedParameterNullabilityMismatchFallsBack(string delegateParameterType, string methodParameterType, bool hasFilter)
    {
        var filter = hasFilter ? ".AddEndpointFilter((context, next) => next(context))" : "";
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
#pragma warning disable CS8622
app.MapPost("/", (NestedNullableHandler)Handler){{filter}};
#pragma warning restore CS8622
static string Handler([Microsoft.AspNetCore.Mvc.FromServices] System.Collections.Generic.{{methodParameterType}} input) => "bound";
""", additionalSource: $$"""
internal delegate string NestedNullableHandler(System.Collections.Generic.{{delegateParameterType}} input);
""", nullableEnabled: true);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(new List<string> { "value" })
            .BuildServiceProvider();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "bound");
    }

    [Theory]
    [InlineData("BindableWithMismatchedNullability", false)]
    [InlineData("BindableWithMismatchedNullability", true)]
    [InlineData("BindableStructWithMismatchedNullability", false)]
    [InlineData("BindableStructWithMismatchedNullability", true)]
    public async Task MapGet_DelegateCast_NamedHandlerWithMismatchedBindAsyncNullabilityFallsBack(string bindableType, bool hasFilter)
    {
        var filter = hasFilter ? ".AddEndpointFilter((context, next) => next(context))" : "";
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
app.MapGet("/", (BindableHandler)Handler){{filter}};
static string Handler({{bindableType}}<Todo> input) => "bound";
""", additionalSource: $$"""
internal delegate string BindableHandler(Microsoft.AspNetCore.Http.Generators.Tests.{{bindableType}}<Microsoft.AspNetCore.Http.Generators.Tests.Todo> input);
""", nullableEnabled: true);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "bound");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("out")]
    [InlineData("in")]
    public async Task MapGet_DelegateCast_ByRefParameterFallsBack(string modifier)
    {
        var assignment = modifier == "out" ? "size = 5;" : "";
        var (result, _) = await RunDelegateCastGeneratorAsync($$"""
app.MapGet("/", (ByRefHandler)Handler);
static string Handler({{modifier}} int size)
{
    {{assignment}}
    return $"size {size}";
}
""", additionalSource: $$"""
internal delegate string ByRefHandler({{modifier}} int size);
""");
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
    }

    [Theory]
    [InlineData("Delegate")]
    [InlineData("Func<object>")]
    public async Task MapGet_DelegateCast_ReadonlyFieldWithInitializerOnly(string fieldType)
    {
        var (result, compilation) = await RunDelegateCastGeneratorAsync("""
app.MapGet("/", InitializerOnlyDelegateCastHandlers.Handler);
""", additionalSource: $$"""
internal static class InitializerOnlyDelegateCastHandlers
{
    internal static readonly {{fieldType}} Handler = (Func<string>)Page;

    private static string Page() => "ok";
}
""", nullableEnabled: true);
        Assert.Empty(result.Diagnostics);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "ok");
        Assert.Equal("text/plain; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task MapGet_DelegateCast_ReadonlyDelegateFieldReassignedInStaticConstructorFallsBack()
    {
        var (result, compilation) = await RunDelegateCastGeneratorAsync("""
app.MapGet("/", ReassignedDelegateCastHandlers.Handler);
""", additionalSource: """
internal static class ReassignedDelegateCastHandlers
{
    internal static readonly Delegate Handler = (SizeHandler)Initial;

    static ReassignedDelegateCastHandlers()
    {
        Handler = (Func<int, string>)Replacement;
    }

    private static string Initial(int size) => $"initial {size}";
    private static string Replacement(int size) => $"replacement {size}";
}
""");
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?size=5");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "replacement 5");
    }

    [Theory]
    [InlineData("assignment")]
    [InlineData("deconstruction")]
    [InlineData("ref")]
    [InlineData("out")]
    [InlineData("init")]
    public async Task MapGet_DelegateCast_ReadonlyDelegateFieldReassignedInInstanceConstructorFallsBack(string writeKind)
    {
        var assignment = writeKind switch
        {
            "assignment" => "Handler = (Func<object>)Replacement;",
            "deconstruction" => "(Handler, _) = ((Delegate)(Func<object>)Replacement, 0);",
            "init" => "",
            _ => $"Replace({writeKind} Handler);",
        };
        var handlerExpression = writeKind == "init"
            ? "new ReassignedInstanceDelegateCastHandlers { Reset = true }.Handler"
            : "new ReassignedInstanceDelegateCastHandlers().Handler";
        var helper = writeKind switch
        {
            "ref" or "out" => $"private static void Replace({writeKind} Delegate handler) => handler = (Func<object>)Replacement;",
            "init" => "internal bool Reset { init { Handler = (Func<object>)Replacement; } }",
            _ => "",
        };
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
app.MapGet("/", {{handlerExpression}});
""", additionalSource: $$"""
internal partial class ReassignedInstanceDelegateCastHandlers
{
    internal readonly Delegate Handler = (Func<string>)Initial;
    private static string Initial() => "initial";
    private static string Replacement() => "replacement";
}

internal partial class ReassignedInstanceDelegateCastHandlers
{
    internal ReassignedInstanceDelegateCastHandlers()
    {
        {{assignment}}
    }

    {{helper}}
}
""");
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveMethod.Id, diagnostic.Id);
        var endpoint = GetEndpointFromCompilation(compilation, expectGeneratedCodeOverride: false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "replacement");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MapGet_DelegateCast_NestedSystemDelegate(bool isAction, bool hasFilter)
    {
        var handler = isAction
            ? "(global::System.Host.Action<HttpContext>)Handler"
            : "(global::System.Host.Func<int, string>)DelegateCastHandlers.Size";
        var filter = hasFilter ? ".AddEndpointFilter((context, next) => next(context))" : "";
        var method = isAction ? "static void Handler(HttpContext context) => context.Response.StatusCode = 202;" : "";
        var (result, compilation) = await RunDelegateCastGeneratorAsync($$"""
app.MapGet("/", {{handler}}){{filter}};
{{method}}
""", additionalSource: """
namespace System
{
    internal static class Host
    {
        internal delegate TResult Func<T, TResult>(T value);
        internal delegate void Action<T>(T value);
    }
}
""");
        Assert.Empty(result.Diagnostics);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?size=5");
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, isAction ? "" : "size 5", expectedStatusCode: isAction ? 202 : 200);
    }

    private static async Task<(GeneratorRunResult Result, Compilation Compilation)> RunDelegateCastGeneratorAsync(string source, bool includeInaccessibleHandler = false, string additionalSource = "", bool nullableEnabled = false)
    {
        var inaccessibleHandler = includeInaccessibleHandler ? """
    private delegate string PrivateHandler(int size);

    internal static void MapPrivate(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (PrivateHandler)Size);
    }
""" : "";
        var project = CreateProject(options => nullableEnabled
            ? options.WithNullableContextOptions(NullableContextOptions.Enable).WithGeneralDiagnosticOption(ReportDiagnostic.Error)
            : options);
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
