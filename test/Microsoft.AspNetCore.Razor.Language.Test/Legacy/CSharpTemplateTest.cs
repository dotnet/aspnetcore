// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTemplateTest : CsHtmlCodeParserTestBase
    {
        public CSharpTemplateTest()
        {
            UseBaselineTests = true;
        }


        [Fact]
        public void ParseBlockHandlesSingleLineTemplate()
        {
            ParseBlockTest("{ var foo = @: bar" + Environment.NewLine + "; }");
        }

        [Fact]
        public void ParseBlockHandlesSingleLineImmediatelyFollowingStatementChar()
        {
            ParseBlockTest("{i@: bar" + Environment.NewLine + "}");
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInExplicitExpressionParens()
        {
            ParseBlockTest("(Html.Repeat(10, @<p>Foo #@item</p>))");
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10, @<p>Foo #@item</p>)");
        }

        [Fact]
        public void ParseBlockHandlesTwoTemplatesInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10, @<p>Foo #@item</p>, @<p>Foo #@item</p>)");
        }

        [Fact]
        public void ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>)");
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void ParseBlockHandlesTwoTemplatesInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@item</p>, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>); }");
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void ParseBlockHandlessTwoTemplatesInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@item</p>, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>); }");
        }

        [Fact]
        public void ParseBlock_WithDoubleTransition_DoesNotThrow()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p foo='@@'>Foo #@item</p>); }");
        }
    }
}
