// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/49126")]
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
        Assert.False(result.ShouldTriggerCompletion);
        Assert.Null(result.Completions);
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
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);

        var description = await result.Service.GetDescriptionAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("Matches a string that contains only lowercase or uppercase letters A through Z in the English alphabet.", description.Text);

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("alpha", change.TextChange.NewText);
    }

    [Fact]
    public async Task Invoke_PolicyColon_ReturnPolicies()
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
", CompletionTrigger.Invoke);

        // Assert
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);
    }

    [Fact]
    public async Task Invoke_Policy_HasText_ReturnPolicies()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:[|re|]$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("alpha", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Invoke_Policy_InText_ReturnPolicies()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:[|alp$$ha|](1)"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("alpha", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Invoke_MultiplePolicy_HasText_ReturnPolicies()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:alpha:[|re|]$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("alpha", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
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
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);
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
        Assert.Empty(result.Completions.ItemsList);
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_HasDelegate_FromRouteAttribute_ReturnDelegateParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$"", ([FromRoute(Name = ""id1"")]string id) => "");
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id1", i.DisplayText));
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_HasMethod_HasStarted_ReturnDelegateParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{[|i|]$$"", ExecuteGet);
    }

    static string ExecuteGet(string id)
    {
        return """";
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_EndpointMapGet_HasMethod_NamedParameters_ReturnDelegateParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(handler: ExecuteGet, pattern: @""{$$"", endpoints: null);
    }

    static string ExecuteGet(string id)
    {
        return """";
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
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
            result.Completions.ItemsList,
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
            result.Completions.ItemsList,
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
        Assert.Empty(result.Completions.ItemsList);
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
        Assert.Empty(result.Completions.ItemsList);
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_ParameterInUse_NoResults()
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
        MapCustomThing(null, @""{id}/{$$"", (string id) => "");
    }

    static void MapCustomThing(IEndpointRouteBuilder endpoints, [StringSyntax(""Route"")] string pattern, Delegate delegate)
    {
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_ParameterInUse_DifferentCase_NoResults()
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
        MapCustomThing(null, @""{ID}/{$$"", (string id) => "");
    }

    static void MapCustomThing(IEndpointRouteBuilder endpoints, [StringSyntax(""Route"")] string pattern, Delegate delegate)
    {
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_ParameterOpenBrace_OtherParameters_ReturnDelegateParameterItem()
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
        MapCustomThing(null, @""{id}/{$$"", (string id, string id2) => "");
    }

    static void MapCustomThing(IEndpointRouteBuilder endpoints, [StringSyntax(""Route"")] string pattern, Delegate delegate)
    {
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id2", i.DisplayText));
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Invoke_Comment_PolicyColon_ReturnHttpPolicies()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        // lang=Route
        var s = @""{hi:$$"";
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);
    }

    [Fact]
    public async Task Invoke_Comment_Http_PolicyColon_ReturnHttpPolicies()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        // lang=Route,Http
        var s = @""{hi:$$"";
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("alpha", result.Completions.ItemsList[0].DisplayText);
    }

    [Fact]
    public async Task Invoke_Comment_Component_PolicyColon_ReturnComponentPolicies()
    {
        // Note: This test adds #line pragma comment to simulate that situation in generated Razor source code.
        // See example in https://github.com/dotnet/razor/pull/6997

        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        // lang=Route,Component
        #line 1 ""/user/foo/index.razor""
        var s = @""{hi:$$"";
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.NotEmpty(result.Completions.ItemsList);
        Assert.Equal("bool", result.Completions.ItemsList[0].DisplayText);
    }

    private Task<CompletionResult> GetCompletionsAndServiceAsync(string source, CompletionTrigger? completionTrigger = null)
    {
        return CompletionTestHelpers.GetCompletionsAndServiceAsync(Runner, source, completionTrigger);
    }
}
