// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Internal;

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

            return new []
            {
                    new object[] { "BadArgumentListRecord", DiagnosticDescriptors.InvalidAsParametersSingleConstructorOnly.Id, GetMultipleContructorsError(typeof(BadArgumentListRecord)) },
                    new object[] { "BadArgumentListClass", DiagnosticDescriptors.InvalidAsParametersSignature.Id, GetInvalidConstructorError(typeof(BadArgumentListClass)) },
                    new object[] { "BadArgumentListClassMultipleCtors", DiagnosticDescriptors.InvalidAsParametersSingleConstructorOnly.Id, GetMultipleContructorsError(typeof(BadArgumentListClassMultipleCtors))  },
                    new object[] { "BadAbstractArgumentListClass", DiagnosticDescriptors.InvalidAsParametersAbstractType.Id, GetAbstractTypeError(typeof(BadAbstractArgumentListClass)) },
                    new object[] { "BadNoPublicConstructorArgumentListClass", DiagnosticDescriptors.InvalidAsParametersNoConstructorFound.Id, GetNoContructorsError(typeof(BadNoPublicConstructorArgumentListClass)) },
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
}
