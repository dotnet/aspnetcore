// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentImportsIntegrationTest : RazorIntegrationTestBase
{
    internal override string FileKind => FileKinds.ComponentImport;

    [Fact]
    public void NoErrorsForUsingStatements()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@using System.Text
@using System.Reflection
@* This is allowed in imports *@
");

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void NoErrorsForRazorComments()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@* This is allowed in imports *@
");

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void NoErrorsForSupportedDirectives()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@inject FooService Foo
@typeparam TItem
@implements ISomeInterface
@inherits SomeNamespace.SomeBaseType
");

        // Assert
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ErrorsForPageDirective()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@page ""/""
");

        // Assert
        Assert.Collection(result.Diagnostics,
            item =>
            {
                Assert.Equal("RZ9987", item.Id);
                Assert.Equal(@"The '@page' directive specified in _Imports.razor file will not be imported. The directive must appear at the top of each Razor file", item.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    [Fact]
    public void ErrorsForTagHelperDirectives()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@addTagHelper *, TestAssembly
@removeTagHelper *, TestAssembly
@tagHelperPrefix th:
");

        // Assert
        Assert.Collection(result.Diagnostics,
            item =>
            {
                Assert.Equal("RZ9978", item.Id);
                Assert.Equal(0, item.Span.LineIndex);
                Assert.Equal(@"The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. Use '@using <namespace>' directive instead.", item.GetMessage(CultureInfo.CurrentCulture));
            },
            item =>
            {
                Assert.Equal("RZ9978", item.Id);
                Assert.Equal(1, item.Span.LineIndex);
                Assert.Equal(@"The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. Use '@using <namespace>' directive instead.", item.GetMessage(CultureInfo.CurrentCulture));
            },
            item =>
            {
                Assert.Equal("RZ9978", item.Id);
                Assert.Equal(2, item.Span.LineIndex);
                Assert.Equal(@"The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. Use '@using <namespace>' directive instead.", item.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    [Fact]
    public void ErrorsForFunctionsDirective()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@functions {
    public class Test
    {
    }
}
");

        // Assert
        Assert.Collection(result.Diagnostics,
            item =>
            {
                Assert.Equal("RZ10003", item.Id);
                Assert.Equal(@"Markup, code and block directives are not valid in component imports.", item.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    [Fact]
    public void ErrorsForSectionDirective()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@section Foo {
}
");

        // Assert
        Assert.Collection(result.Diagnostics,
            item =>
            {
                Assert.Equal("RZ10003", item.Id);
                Assert.Equal(@"Markup, code and block directives are not valid in component imports.", item.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    [Fact]
    public void ErrorsForMarkup()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
<div>asdf</div>
");

        // Assert
        Assert.Collection(result.Diagnostics,
            item =>
            {
                Assert.Equal("RZ10003", item.Id);
                Assert.Equal(@"Markup, code and block directives are not valid in component imports.", item.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    [Fact]
    public void ErrorsForCode()
    {
        // Arrange/Act
        var result = CompileToCSharp("_Imports.razor", @"
@Foo
@(Bar)
@{
    var x = Foo;
}");

        // Assert
        Assert.Collection(result.Diagnostics,
            item =>
            {
                Assert.Equal("RZ10003", item.Id);
                Assert.Equal(0, item.Span.LineIndex);
                Assert.Equal(@"Markup, code and block directives are not valid in component imports.", item.GetMessage(CultureInfo.CurrentCulture));
            },
            item =>
            {
                Assert.Equal("RZ10003", item.Id);
                Assert.Equal(1, item.Span.LineIndex);
                Assert.Equal(@"Markup, code and block directives are not valid in component imports.", item.GetMessage(CultureInfo.CurrentCulture));
            },
            item =>
            {
                Assert.Equal("RZ10003", item.Id);
                Assert.Equal(2, item.Span.LineIndex);
                Assert.Equal(@"Markup, code and block directives are not valid in component imports.", item.GetMessage(CultureInfo.CurrentCulture));
            });
    }
}
