// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Security.Claims;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

public partial class FrameworkParametersCompletionProviderTests
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RoutePatternAnalyzer());

    [Fact]
    public async Task Insertion_Space_Int_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (int $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_DateTime_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (DateTime $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_NullableInt_CloseParen_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (int? $$)
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_NullableInt_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (int? $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (HttpContext context, int $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_ParameterAlreadyUsed_EndpointMapGet_HasDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (string id, int $$
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_ParameterAlreadyUsed_EndpointMapGet_HasCompleteDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (string id, int $$) => { });
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_ParameterAlreadyUsed_DifferentCase_EndpointMapGet_HasCompleteDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{ID}"", (string id, int $$) => { });
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Theory]
    [InlineData("HttpContext")]
    [InlineData("CancellationToken")]
    [InlineData("HttpRequest")]
    [InlineData("HttpResponse")]
    [InlineData("ClaimsPrincipal")]
    [InlineData("IFormFileCollection")]
    [InlineData("IFormFile")]
    [InlineData("Stream")]
    [InlineData("PipeReader")]
    public async Task Insertion_Space_SpecialType_EndpointMapGet_HasDelegate_ReturnRouteParameterItem(string parameterType)
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (" + parameterType + @" $$
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_Space_EndpointMapGet_HasMethod_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", ExecuteGet $$);
    }

    static string ExecuteGet(string id)
    {
        return """";
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_Space_EndpointMapGet_HasMethod_NamedParameters_ReturnDelegateParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(pattern: @""{id}"", endpoints: null, handler: (string blah, int $$)
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Theory]
    [InlineData("AsParameters")]
    [InlineData("FromQuery")]
    [InlineData("FromForm")]
    [InlineData("FromHeader")]
    [InlineData("FromQuery")]
    [InlineData("FromServices")]
    public async Task Insertion_Space_EndpointMapGet_AsParameters_NoItem(string attributeName)
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", ([" + attributeName + @"] int $$) => {});
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_Space_EndpointMapGet_UnknownAttribute_NoItem()
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", ([PurpleMonkeyDishwasher] int $$) => {});
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_EndpointMapGet_NullDelegate_NoResults()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", null $$
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_Space_EndpointMapGet_Incomplete_NoResults()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", $$
    }
}
");

        // Assert
        var item = result.Completions.Items.FirstOrDefault(i => i.DisplayText == "id");
        Assert.Null(item);
    }

    [Fact]
    public async Task Insertion_Space_CustomMapGet_ReturnDelegateParameterItem()
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
        MapCustomThing(null, @""{id}"", (string $$) => "");
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
    public async Task Insertion_Space_CustomMapGet_NoRouteSyntax_NoItems()
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
        MapCustomThing(null, @""{id}"", (string $$) => "");
    }

    static void MapCustomThing(IEndpointRouteBuilder endpoints, string pattern, Delegate delegate)
    {
    }
}
");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    [Fact]
    public async Task Insertion_Space_ControllerAction_HasParameter_ReturnActionParameterItem()
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
        [HttpGet(@""{id}"")]
        public object TestAction(int $$)
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

    [Fact]
    public async Task Insertion_Space_ControllerAction_HasParameter_Incomplete_ReturnActionParameterItem()
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
        [HttpGet(@""{id}"")]
        public object TestAction(int $$
    }
    ");

        // Assert
        Assert.Collection(
            result.Completions.Items,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_ControllerAction_HasParameter_BeforeComma_ReturnActionParameterItem()
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
        [HttpGet(@""{id}"")]
        public object TestAction(int $$, string blah)
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

    [Fact]
    public async Task Insertion_Space_NonControllerAction_HasParameter_ReturnActionParameterItem()
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
        [HttpGet(@""{id}"")]
        internal object TestAction(int $$)
        {
            return null;
        }
    }
    ");

        // Assert
        Assert.Empty(result.Completions.Items);
    }

    private async Task<CompletionResult> GetCompletionsAndServiceAsync(string source)
    {
        MarkupTestFile.GetPosition(source, out var output, out int cursorPosition);

        var completions = await Runner.GetCompletionsAndServiceAsync(cursorPosition, output);

        return completions;
    }
}
