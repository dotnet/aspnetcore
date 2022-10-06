// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Extensions.Primitives;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.AspNetCore.Analyzers.Http.HeaderDictionaryIndexerAnalyzer>;

namespace Microsoft.AspNetCore.Analyzers.Http;

public class HeaderDictionaryIndexerAnalyzerTests
{
    [Fact]
    public async Task IHeaderDictionary_Get_MismatchCase_ReturnDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    var s = {|#0:context.Request.Headers[""content-type""]|};
    await next();
});
",
        new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_HeaderDictionaryIndexer_Message("content-type", "ContentType")));
    }

    [Fact]
    public async Task IHeaderDictionary_Set_MismatchCase_ReturnDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    {|#0:context.Request.Headers[""content-type""]|} = """";
    await next();
});
",
        new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_HeaderDictionaryIndexer_Message("content-type", "ContentType")));
    }

    [Fact]
    public async Task IHeaderDictionary_Get_UnknownProperty_NoDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.Request.Headers[""content-type1""] = """";
    await next();
});
");
    }

    [Fact]
    public async Task IHeaderDictionary_Get_NullProperty_NoDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.Request.Headers[null] = """";
    await next();
});
");
    }

    [Fact]
    public async Task IHeaderDictionary_Get_StronglyTypeProperty_NoDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.Request.Headers.ContentType = """";
    await next();
});
");
    }

    [Fact]
    public async Task IHeaderDictionary_Get_VariableProperty_NoDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    var s = """";
    context.Request.Headers[s] = """";
    await next();
});
");
    }

    [Fact]
    public async Task HeaderDictionary_Get_KnownProperty_NoDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Http;
var headers = new HeaderDictionary();
headers[""Content-Type""] = """";
");
    }

    [Fact]
    public async Task HeaderDictionary_CastToIHeaderDictionary_Get_KnownProperty_ReturnDiagnostic()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Http;
IHeaderDictionary headers = new HeaderDictionary();
{|#0:headers[""Content-Type""]|} = """";
",
        new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_HeaderDictionaryIndexer_Message("Content-Type", "ContentType")));
    }

    [Fact]
    public void ValidatePropertyMappingContainsOnAllHeaderProperties()
    {
        // We want to keep IHeaderDictionary.Keyed.cs and HeaderDictionaryIndexerAnalyzer.PropertyMapping in sync.
        // This test verifies all IHeaderDictionary properties are present in PropertyMapping.

        // Arrange
        var headerDictionaryPropertyNames = typeof(IHeaderDictionary)
            .GetProperties()
            .Where(p => p.CanWrite && p.CanRead && p.PropertyType == typeof(StringValues) && p.GetIndexParameters().Length == 0)
            .Select(p => p.Name)
            .ToList();

        Assert.NotEmpty(headerDictionaryPropertyNames);

        // Make copy
        var propertyMapping = new Dictionary<string, string>(HeaderDictionaryIndexerAnalyzer.PropertyMapping);

        // Act
        foreach (var propertyName in headerDictionaryPropertyNames)
        {
            if (propertyName == "WebSocketSubProtocols")
            {
                // SecWebSocketProtocol and WebSocketSubProtocols map to 'Sec-WebSocket-Protocol'.
                continue;
            }

            var mapping = propertyMapping.SingleOrDefault(kvp => kvp.Value == propertyName);

            // KeyValuePair is a struct so default struct is returned when there isn't a match.
            // Check if key has a value for success.
            if (mapping.Key is null)
            {
                // Assert
                Assert.Fail($"A mapping for property '{propertyName}' on IHeaderDictionary must be added to {nameof(HeaderDictionaryIndexerAnalyzer)}.{nameof(HeaderDictionaryIndexerAnalyzer.PropertyMapping)}.");
            }

            propertyMapping.Remove(mapping.Key);
        }

        // Check that all property mappings are properties on IHeaderDictionary.
        Assert.Empty(propertyMapping);
    }
}
