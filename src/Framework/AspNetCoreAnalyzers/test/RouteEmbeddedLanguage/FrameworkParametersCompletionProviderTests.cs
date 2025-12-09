// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.CodeAnalysis.Completion;

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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Insertion_Space_OutInt_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        // Out parameters are not supported by Minimal API.
        // It's useful to provide completion here and then allow dev to fix parameter later.
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (out int $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Insertion_Space_Generic_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (Nullable<int> $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Invoke_Space_Generic_EndpointMapGet_HasDelegate_HasText_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (int [|i|]$$
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Invoke_Space_Generic_EndpointMapGet_HasDelegate_InText_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (int [|i$$d|]
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Invoke_Space_Generic_EndpointMapGet_HasCompleteDelegate_InText_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{ids}"", (int [|i$$d|]) => {});
    }
}
", CompletionTrigger.Invoke);

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("ids", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("ids", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Insertion_FirstArgument_SpaceAfterIdentifer_EndpointMapGet_HasDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (int i $$
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_SecondArgument_SpaceAfterIdentifer_EndpointMapGet_HasDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (int o, string i $$
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (HttpContext context, int $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_SystemString_EndpointMapGet_HasDelegate_ReturnRouteParameterItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (String $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_ParameterAlreadyUsed_EndpointMapGet_HasDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (string id, int $$
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_OneParameterAlreadyUsed_EndpointMapGet_HasDelegate_HasItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}/{id2}"", (string id, int $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id2", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_MultipleParameters_EndpointMapGet_HasDelegate_HasItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}/{id2}"", (string $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText),
            i => Assert.Equal("id2", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_DuplicateParameters_EndpointMapGet_HasDelegate_HasItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}/{id}"", (string $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_ParameterAlreadyUsed_EndpointMapGet_HasCompleteDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (string id, int $$) => { });
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_Space_MultipleArgs_ParameterAlreadyUsed_DifferentCase_EndpointMapGet_HasCompleteDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{ID}"", (string id, int $$) => { });
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_Space_CustomParsableType_EndpointMapGet_HasDelegate_HasItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (CustomParsableType $$
    }
}

public class CustomParsableType
{
    public static bool TryParse(string s, out CustomParsableType result)
    {
        result = new CustomParsableType();
        return true;
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_CustomParsableWithFormatType_EndpointMapGet_HasDelegate_HasItem()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (CustomParsableType $$
    }
}

public class CustomParsableType
{
    public static bool TryParse(string s, IFormatProvider provider, out CustomParsableType result)
    {
        result = new CustomParsableType();
        return true;
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_CustomParsableWithFormatType_NonPublic_EndpointMapGet_HasDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (CustomParsableType $$
    }
}

public class CustomParsableType
{
    private static bool TryParse(string s, IFormatProvider provider, out CustomParsableType result)
    {
        result = new CustomParsableType();
        return true;
    }
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_Space_NonParsableType_EndpointMapGet_HasDelegate_NoItems()
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (NonParsableType $$
    }
}

public interface NonParsableType
{
}
");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("decimal")]
    [InlineData("DateTime")]
    [InlineData("Guid")]
    [InlineData("TimeSpan")]
    [InlineData("string")]
    [InlineData("String")]
    [InlineData("string?")]
    [InlineData("String?")]
    [InlineData("Int32")]
    [InlineData("int?")]
    [InlineData("Nullable<int>")]
    [InlineData("Nullable<Int32>")]
    [InlineData("StringComparison")]
    [InlineData("Uri")]
    public async Task Insertion_Space_SupportedBuiltinTypes_EndpointMapGet_HasDelegate_HasItem(string parameterType)
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
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (" + parameterType + @" $$
    }
}
");

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
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
    public async Task Insertion_Space_SpecialType_EndpointMapGet_HasDelegate_NoItems(string parameterType)
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
        Assert.Empty(result.Completions.ItemsList);
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
        Assert.Empty(result.Completions.ItemsList);
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Theory]
    [InlineData("AsParameters")]
    [InlineData("FromQuery")]
    [InlineData("FromForm")]
    [InlineData("FromHeader")]
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
        Assert.Empty(result.Completions.ItemsList);
    }

    [Fact]
    public async Task Insertion_Space_EndpointMapGet_UnknownAttribute_ReturnItems()
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
            result.Completions.ItemsList,
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
        Assert.Empty(result.Completions.ItemsList);
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
        var item = result.Completions.ItemsList.FirstOrDefault(i => i.DisplayText == "id");
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
            result.Completions.ItemsList,
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
        Assert.Empty(result.Completions.ItemsList);
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
            result.Completions.ItemsList,
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Invoke_ControllerAction_HasParameter_Incomplete_ReturnActionParameterItem()
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
        public object TestAction(int [|i|]$$
    }
    ", CompletionTrigger.Invoke);

        // Assert
        Assert.Collection(
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));

        var change = await result.Service.GetChangeAsync(result.Document, result.Completions.ItemsList[0]);
        Assert.Equal("id", change.TextChange.NewText);
        Assert.Equal(result.CompletionListSpan, change.TextChange.Span);
    }

    [Fact]
    public async Task Insertion_ControllerAction_HasParameter_Incomplete_NoItems()
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
        public object TestAction(int i $$
    }
    ");

        // Assert
        Assert.Empty(result.Completions.ItemsList);
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
            result.Completions.ItemsList,
            i => Assert.Equal("id", i.DisplayText));
    }

    [Fact]
    public async Task Insertion_Space_NonControllerAction_HasParameter_NoItems()
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
        Assert.Empty(result.Completions.ItemsList);
    }

    private Task<CompletionResult> GetCompletionsAndServiceAsync(string source, CompletionTrigger? completionTrigger = null)
    {
        return CompletionTestHelpers.GetCompletionsAndServiceAsync(Runner, source, completionTrigger);
    }
}
