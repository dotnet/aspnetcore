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
        // The generator cannot see private members of types from a metadata reference, so the
        // base types are declared in source to cover non-public setters, statics and indexers.
        var project = CreateProject();
        var source = GetMapActionString("""
app.MapGet("/", (HttpContext context, [AsParameters] SourceDerivedParameterList args) =>
{
    context.Items.Add("input", $"{args.GrandparentValue},{args.Hidden},{args.Value},{args.PrivateSetValue},{args.ProtectedSetValue},{args.PrivateProtectedSetValue},{args.InternalSetValue},{args.ProtectedInternalSetValue},{args.OverriddenGetterValue},{SourceBaseParameterList.StaticValue}");
});
""");
        var types = """
public class SourceGrandparentParameterList
{
    public int GrandparentValue { get; set; }
    public int Hidden { get; set; }
}

public class SourceBaseParameterList : SourceGrandparentParameterList
{
    public static int StaticValue { get; set; }
    private new int Hidden { get; set; }
    public int PrivateSetValue { get; private set; } = 20;
    public int ProtectedSetValue { get; protected set; } = 30;
    public int PrivateProtectedSetValue { get; private protected set; } = 40;
    public int InternalSetValue { get; internal set; } = 50;
    public int ProtectedInternalSetValue { get; protected internal set; } = 60;
    public virtual int OverriddenGetterValue { get; set; } = 6;
    public string this[string key] { get => key; set { } }
    public int Value { get; set; }
}

public class SourceDerivedParameterList : SourceBaseParameterList
{
    public override int OverriddenGetterValue => base.OverriddenGetterValue;
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
            ["GrandparentValue"] = "1",
            ["Hidden"] = "2",
            ["Value"] = "3",
            ["PrivateSetValue"] = "9",
            ["ProtectedSetValue"] = "9",
            ["PrivateProtectedSetValue"] = "9",
            ["InternalSetValue"] = "9",
            ["ProtectedInternalSetValue"] = "9",
            ["OverriddenGetterValue"] = "9",
            ["StaticValue"] = "9",
        });

        await endpoint.RequestDelegate(httpContext);

        // Only properties with a public setter are bound, as in RequestDelegateFactory. The grandparent
        // Hidden property is bound because the private Hidden property of the base type does not hide it.
        Assert.Equal("1,2,3,20,30,40,50,60,6,0", httpContext.Items["input"]);
    }
}
