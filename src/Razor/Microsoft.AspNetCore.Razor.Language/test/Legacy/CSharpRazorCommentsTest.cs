// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpRazorCommentsTest : ParserTestBase
{
    [Fact]
    public void UnterminatedRazorComment()
    {
        ParseDocumentTest("@*");
    }

    [Fact]
    public void EmptyRazorComment()
    {
        ParseDocumentTest("@**@");
    }

    [Fact]
    public void RazorCommentInImplicitExpressionMethodCall()
    {
        ParseDocumentTest("@foo(" + Environment.NewLine
                        + "@**@" + Environment.NewLine);
    }

    [Fact]
    public void UnterminatedRazorCommentInImplicitExpressionMethodCall()
    {
        ParseDocumentTest("@foo(@*");
    }

    [Fact]
    public void RazorMultilineCommentInBlock()
    {
        ParseDocumentTest(@"
@{
    @*
This is a comment
    *@
}
");
    }

    [Fact]
    public void RazorCommentInVerbatimBlock()
    {
        ParseDocumentTest("@{" + Environment.NewLine
                        + "    <text" + Environment.NewLine
                        + "    @**@" + Environment.NewLine
                        + "}");
    }

    [Fact]
    public void RazorCommentInOpeningTagBlock()
    {
        ParseDocumentTest("<text @* razor comment *@></text>");
    }

    [Fact]
    public void RazorCommentInClosingTagBlock()
    {
        ParseDocumentTest("<text></text @* razor comment *@>");
    }

    [Fact]
    public void UnterminatedRazorCommentInVerbatimBlock()
    {
        ParseDocumentTest("@{@*");
    }

    [Fact]
    public void RazorCommentInMarkup()
    {
        ParseDocumentTest(
            "<p>" + Environment.NewLine
            + "@**@" + Environment.NewLine
            + "</p>");
    }

    [Fact]
    public void MultipleRazorCommentInMarkup()
    {
        ParseDocumentTest(
            "<p>" + Environment.NewLine
            + "  @**@  " + Environment.NewLine
            + "@**@" + Environment.NewLine
            + "</p>");
    }

    [Fact]
    public void MultipleRazorCommentsInSameLineInMarkup()
    {
        ParseDocumentTest(
            "<p>" + Environment.NewLine
            + "@**@  @**@" + Environment.NewLine
            + "</p>");
    }

    [Fact]
    public void RazorCommentsSurroundingMarkup()
    {
        ParseDocumentTest(
            "<p>" + Environment.NewLine
            + "@* hello *@ content @* world *@" + Environment.NewLine
            + "</p>");
    }

    [Fact]
    public void RazorCommentBetweenCodeBlockAndMarkup()
    {
        ParseDocumentTest(
            "@{ }" + Environment.NewLine +
            "@* Hello World *@" + Environment.NewLine +
            "<div>Foo</div>"
        );
    }

    [Fact]
    public void RazorCommentWithExtraNewLineInMarkup()
    {
        ParseDocumentTest(
            "<p>" + Environment.NewLine + Environment.NewLine
            + "@* content *@" + Environment.NewLine
            + "@*" + Environment.NewLine
            + "content" + Environment.NewLine
            + "*@" + Environment.NewLine + Environment.NewLine
            + "</p>");
    }
}
