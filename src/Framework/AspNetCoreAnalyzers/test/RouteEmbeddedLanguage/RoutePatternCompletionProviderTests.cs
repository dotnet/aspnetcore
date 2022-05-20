// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

public partial class RoutePatternCompletionProviderTests
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RoutePatternAnalyzer());

    [Fact]
    public async Task Insertion_Literal_NoItems()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""hi$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_PolicyColon_ReturnPolicies()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");

        // Assert
        Assert.NotEmpty(result.Completions.Items);
        Assert.Equal("alpha", result.Completions.Items[0].DisplayText);

        // Getting description is currently broken in Roslyn.
        //var description = await result.Service.GetDescriptionAsync(result.Document, result.Completions.Items[0]);
        //Assert.Equal("int", description.Text);
    }

    [Fact]
    public async Task Insertion_PolicyColon_MultipleOverloads_ReturnPolicies()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
    static void M([StringSyntax(""Route"")] string p, int i)
    {
    }
}
");

        // Assert
        Assert.NotEmpty(result.Completions.Items);
        Assert.Equal("alpha", result.Completions.Items[0].DisplayText);
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_UnsupportedMethod_NoItems()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);

        //var description = await result.Service.GetDescriptionAsync(result.Document, result.Completions.Items[0]);
        //Assert.Equal("int", description.Text);
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_HasDelegate_ReturnDelegateParameterItem()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$"", (string id) => "");
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_HasMethod_ReturnDelegateParameterItem()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$"", ExecuteGet);
    }

    static string ExecuteGet(string id)
    {
        return """";
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_HasSpecialTypes_ExcludeSpecialTypes()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$"", ExecuteGet);
    }

    static string ExecuteGet(string id, CancellationToken cancellationToken, HttpContext context,
        HttpRequest request, HttpResponse response, ClaimsPrincipal claimsPrincipal,
        IFormFileCollection formFiles, IFormFile formFile, Stream stream, PipeReader pipeReader)
    {
        return """";
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_AsParameters_ReturnObjectParameterItem()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$"", ExecuteGet);
    }

    static string ExecuteGet([AsParameters] PageData id)
    {
        return """";
    }

    class PageData
    {
        public int PageNumber { get; set; }
        [FromRoute]
        public int PageIndex { get; set; }
        [FromServices]
        public object Service { get; set; }
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("PageIndex", i.DisplayText),
            i => Assert.Equal("PageNumber", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_NullDelegate_NoResults()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$"", null);
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_Incomplete_NoResults()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$"";
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_CustomMapGet_ReturnDelegateParameterItem()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

class Program
{
    static void Main()
    {
        MapCustomThing(null, @""{$$"", (string id) => "");
    }

    static void MapCustomThing(IEndpointRouteBuilder endpoints, [StringSyntax(""Route"")] string pattern, Delegate delegate)
    {
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_ControllerAction_HasParameter_ReturnActionParameterItem()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(@""{$$"")]
    public object TestAction(int id)
    {
        return null;
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    private async Task<CompletionResult> GetCompletionsAndServiceAsync(string source)
    {
        MarkupTestFile.GetPosition(source, out var output, out int cursorPosition);

        var completions = await Runner.GetCompletionsAndServiceAsync(cursorPosition, output);

        return completions;
    }
}
