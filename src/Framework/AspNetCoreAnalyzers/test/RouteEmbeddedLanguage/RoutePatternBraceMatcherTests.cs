// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

public partial class RoutePatternBraceMatcherTests
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RoutePatternAnalyzer());

    [Fact]
    public async Task AfterLiteral_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
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
    }

    [Fact]
    public async Task BeforeParameterStart_CompleteParameter_HighlightBraces()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""$$[|{|]hi[|}|]"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task BeforeParameterStart_IncompleteParameter_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""$${hi"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task BeforeArgumentStart_CompleteParenAndParameter_HighlightParens()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:regex$$[|(|]aaa[|)|]}"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task BeforeArgumentStart_CompleteParenIncompleteParameter_HighlightParens()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:regex$$[|(|]aaa[|)|]"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task AfterParameterStart_CompleteParameter_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{$$hi}"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task BeforeReplacementTokenStart_NotUsedWithMvc_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""$$[aaa]"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task BeforeReplacementTokenStart_MvcAction_HighlightReplacementTokenBrackets()
    {
        // Arrange & Act & Assert
        await TestBraceMatchesAsync(@"
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

[Route(@""$$[|[|]aaa[|]|]"")]
public class TestController
{
    public void TestAction()
    {
    }
}
");
    }

    private async Task TestBraceMatchesAsync(string source)
    {
        MarkupTestFile.GetPositionAndSpans(source, out var output, out int cursorPosition, out var spans);

        var result = await Runner.GetBraceMatchesAsync(cursorPosition, output);
        if (result == null)
        {
            if (spans.IsDefaultOrEmpty)
            {
                return;
            }

            throw new Exception("No result doesn't match spans.");
        }

        if (!spans.Contains(result.Value.LeftSpan) || !spans.Contains(result.Value.RightSpan))
        {
            throw new Exception("Not found.");
        }
    }
}
