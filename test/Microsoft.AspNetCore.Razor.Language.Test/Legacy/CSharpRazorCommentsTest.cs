// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpRazorCommentsTest : CsHtmlMarkupParserTestBase
    {
        public CSharpRazorCommentsTest()
        {
            UseBaselineTests = true;
        }

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
        public void RazorCommentInVerbatimBlock()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                            + "    <text" + Environment.NewLine
                            + "    @**@" + Environment.NewLine
                            + "}");
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
}
