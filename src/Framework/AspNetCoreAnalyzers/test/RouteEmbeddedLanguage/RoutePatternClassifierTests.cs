// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

public class RoutePatternClassifierTests
{
    private readonly ITestOutputHelper _output;

    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RenderTreeBuilderAnalyzer());

    protected async Task TestAsync(
        string code,
        params FormattedClassification[] expected)
    {
        MarkupTestFile.GetSpans(code, out var rewrittenCode, out ImmutableArray<TextSpan> spans);
        Assert.True(spans.Length == 1);

        var actual = await Runner.GetClassificationSpansAsync(spans.Single(), rewrittenCode);
        var actualOrdered = actual.OrderBy(t1 => t1.TextSpan.Start).ToList();
        var actualFormatted = actualOrdered.Select(a => new FormattedClassification(rewrittenCode.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType)).ToArray();

        Assert.Equal(expected, actualFormatted);
    }

    public RoutePatternClassifierTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CommentOnString_Classified()
    {
        await TestAsync(
@"
class Program
{
    void Goo()
    {
        // language=Route
        var s = [|@""{id?}""|];
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
Verbatim(@"@""{id?}"""),
Regex.CharacterClass("{"),
Parameter("id"),
Regex.Anchor("?"),
Regex.CharacterClass("}"));
    }

    [Fact]
    public async Task AttributeOnField_Classified()
    {
        await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    [StringSyntax(""Route"")]
    private string field;

    void Goo()
    {
        this.field = [|@""{id?}""|];
    }
}" + EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharp,
Verbatim(@"@""{id?}"""),
Regex.CharacterClass("{"),
Parameter("id"),
Regex.Anchor("?"),
Regex.CharacterClass("}"));
    }

    [Fact]
    public async Task AttributeOnField_TokenReplacementText_TokenReplacementNotClassified()
    {
        await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

class Program
{
    [StringSyntax(""Route"")]
    private string field;

    void Goo()
    {
        this.field = [|@""[one]/{id}""|];
    }
}",
Verbatim(@"@""[one]/{id}"""),
Regex.CharacterClass("{"),
Parameter("id"),
Regex.CharacterClass("}"));
    }

    [Fact]
    public async Task AttributeOnAction_TokenReplacementText_TokenReplacementClassified()
    {
        await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

public class TestController
{
    [HttpGet([|@""[one]""|])]
    public void TestAction()
    {
    }
}",
Verbatim(@"@""[one]"""),
Regex.CharacterClass("["),
Regex.CharacterClass("one"),
Regex.CharacterClass("]"));
    }

    [Fact]
    public async Task AttributeOnController_TokenReplacementText_TokenReplacementClassified()
    {
        await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

[Route([|@""[one]""|])]
public class TestController
{
    public void TestAction()
    {
    }
}",
Verbatim(@"@""[one]"""),
Regex.CharacterClass("["),
Regex.CharacterClass("one"),
Regex.CharacterClass("]"));
    }

    private static FormattedClassification Parameter(string name) => new FormattedClassification(name, "json - object");
}
