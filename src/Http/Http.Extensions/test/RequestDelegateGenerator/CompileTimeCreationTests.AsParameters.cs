// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class CompileTimeCreationTests : RequestDelegateCreationTests
{
    [Theory]
    [InlineData("ParameterListRecordStruct")]
    [InlineData("ParameterListRecordClass")]
    [InlineData("ParameterListStruct")]
    [InlineData("ParameterListClass")]
    public async Task RequestDelegateThrowsWhenNullableParameterList(string parameterType)
    {
        var source = $$"""
void TestAction(HttpContext context, [AsParameters] {{parameterType}}? args)
{
    context.Items.Add("value", args);
}
app.MapGet("/", TestAction);
""";

        var (generatorRunResult, compilation) = await RunGeneratorAsync(source);

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.InvalidAsParametersNullable.Id, diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    public static object[][] BadArgumentListActions
    {
        get
        {
            static string GetAbstractTypeError(Type type)
                => $"The abstract type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}' is not supported. For more information, please see https://aka.ms/aspnet/rdg-known-issues";

            static string GetMultipleContructorsError(Type type)
                => $"Only a single public parameterized constructor is allowed for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'. For more information, please see https://aka.ms/aspnet/rdg-known-issues";

            static string GetNoContructorsError(Type type)
                => $"No public parameterless constructor found for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'. For more information, please see https://aka.ms/aspnet/rdg-known-issues";

            static string GetInvalidConstructorError(Type type)
                => $"The public parameterized constructor must contain only parameters that match the declared public properties for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'. For more information, please see https://aka.ms/aspnet/rdg-known-issues";

            static string GetEnumerableTypeError(Type type)
                => $"The enumerable type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}' is not supported. For more information, please see https://aka.ms/aspnet/rdg-known-issues";

            return new []
            {
                    new object[] { "BadArgumentListRecord", DiagnosticDescriptors.InvalidAsParametersSingleConstructorOnly.Id, GetMultipleContructorsError(typeof(BadArgumentListRecord)) },
                    new object[] { "BadArgumentListClass", DiagnosticDescriptors.InvalidAsParametersSignature.Id, GetInvalidConstructorError(typeof(BadArgumentListClass)) },
                    new object[] { "BadArgumentListClassMultipleCtors", DiagnosticDescriptors.InvalidAsParametersSingleConstructorOnly.Id, GetMultipleContructorsError(typeof(BadArgumentListClassMultipleCtors))  },
                    new object[] { "BadAbstractArgumentListClass", DiagnosticDescriptors.InvalidAsParametersAbstractType.Id, GetAbstractTypeError(typeof(BadAbstractArgumentListClass)) },
                    new object[] { "BadNoPublicConstructorArgumentListClass", DiagnosticDescriptors.InvalidAsParametersNoConstructorFound.Id, GetNoContructorsError(typeof(BadNoPublicConstructorArgumentListClass)) },
                    new object[] { "List<string>", DiagnosticDescriptors.InvalidAsParametersEnumerableType.Id, GetEnumerableTypeError(typeof(List<string>)) },
                    new object[] { "List<IFormFile>", DiagnosticDescriptors.InvalidAsParametersEnumerableType.Id, GetEnumerableTypeError(typeof(List<IFormFile>)) },
                    new object[] { "Dictionary<string, string>", DiagnosticDescriptors.InvalidAsParametersEnumerableType.Id, GetEnumerableTypeError(typeof(Dictionary<string, string>)) }
            };
        }
    }

    [Theory]
    [MemberData(nameof(BadArgumentListActions))]
    public async Task BuildRequestDelegateEmitsDiagnosticForInvalidParameterListConstructor(
        string parameterType,
        string diagnosticId,
         string message)
    {
        var source = $$"""
void TestAction(HttpContext context, [AsParameters] {{parameterType}} args)
{
    context.Items.Add("value", args);
}
app.MapGet("/", TestAction);
""";

        var (generatorRunResult, _) = await RunGeneratorAsync(source);

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(diagnosticId, diagnostic.Id);
        Assert.Equal(message, diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Empty(result.GeneratedSources);
    }

    [Theory]
    [InlineData("NestedArgumentListRecord")]
    [InlineData("ClassWithParametersConstructor")]
    public async Task BuildRequestDelegateThrowsNotSupportedExceptionForNestedParametersList(string parameterType)
    {
        var source = $$"""
void TestAction([AsParameters] {{parameterType}} req) { }
app.MapGet("/", TestAction);
""";
        var (generatorRunResult, _) = await RunGeneratorAsync(source);

        // Emits diagnostic but generates no source
        var result = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.InvalidAsParametersNested.Id, diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromParameterListWithInheritedPropertiesDeclaredInSource()
    {
        // A metadata reference hides private and internal members from the generator, so these types are declared
        // in source. Static properties and indexers are tested only here, because RequestDelegateFactory fails on them.
        var project = CreateProject();
        var source = GetMapActionString("""
void TestAction(HttpContext context, [AsParameters] SourceDerivedParameterList args)
{
    context.Items.Add("Hidden", args.Hidden);
    context.Items.Add("PrivateStaticHidden", args.PrivateStaticHidden);
    context.Items.Add("DerivedHidden", args.DerivedHidden);
    context.Items.Add("PrivateSetValue", args.PrivateSetValue);
    context.Items.Add("InternalSetValue", args.InternalSetValue);
    context.Items.Add("RequiredValue", args.RequiredValue);
    context.Items.Add("StaticValue", SourceBaseParameterList.StaticValue);
}
app.MapGet("/", TestAction);
""");
        var types = """
internal class SourceGrandparentParameterList
{
    public int Hidden { get; set; }
}

internal class SourceBaseParameterList : SourceGrandparentParameterList
{
    public static int StaticValue { get; set; }
    private new int Hidden { get; set; }
    public int PrivateStaticHidden { get; set; }
    public int DerivedHidden { get; set; } = 70;
    public int PrivateSetValue { get; private set; } = 20;
    public int InternalSetValue { get; internal set; } = 50;
    public required int RequiredValue { get; internal set; }
    public string this[string key] { get => key; set { } }
}

internal class SourceDerivedParameterList : SourceBaseParameterList
{
    private static new int PrivateStaticHidden => 0;
    private new int DerivedHidden { get; set; }
}
""";
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        project = project.AddDocument("SourceParameterLists.cs", SourceText.From(types, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        var generator = new RequestDelegateGenerator.RequestDelegateGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true),
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

        Assert.Empty(updatedCompilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning));
        var endpoint = GetEndpointFromCompilation(updatedCompilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>()
        {
            ["Hidden"] = "2",
            ["PrivateStaticHidden"] = "3",
            ["DerivedHidden"] = "9",
            ["PrivateSetValue"] = "9",
            ["InternalSetValue"] = "9",
            ["RequiredValue"] = "4",
            ["StaticValue"] = "9",
        });

        await endpoint.RequestDelegate(httpContext);

        // A private property of a base type, or a private static property, does not hide an inherited property.
        Assert.Equal(2, httpContext.Items["Hidden"]);
        Assert.Equal(3, httpContext.Items["PrivateStaticHidden"]);
        // A private instance property of the type itself hides the inherited property, as in Type.GetProperties().
        Assert.Equal(70, httpContext.Items["DerivedHidden"]);
        // Like RequestDelegateFactory, only properties with a public setter are bound.
        Assert.Equal(20, httpContext.Items["PrivateSetValue"]);
        Assert.Equal(50, httpContext.Items["InternalSetValue"]);
        Assert.Equal(0, httpContext.Items["StaticValue"]);
        // The object initializer must set a required property, so it is bound although its setter is internal.
        Assert.Equal(4, httpContext.Items["RequiredValue"]);
    }
}
