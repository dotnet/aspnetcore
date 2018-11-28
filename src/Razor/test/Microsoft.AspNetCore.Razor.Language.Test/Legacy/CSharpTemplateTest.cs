// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTemplateTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void HandlesSingleLineTemplate()
        {
            ParseBlockTest("{ var foo = @: bar" + Environment.NewLine + "; }");
        }

        [Fact]
        public void HandlesSingleLineImmediatelyFollowingStatementChar()
        {
            ParseBlockTest("{i@: bar" + Environment.NewLine + "}");
        }

        [Fact]
        public void HandlesSimpleTemplateInExplicitExpressionParens()
        {
            ParseBlockTest("(Html.Repeat(10, @<p>Foo #@item</p>))");
        }

        [Fact]
        public void HandlesSimpleTemplateInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10, @<p>Foo #@item</p>)");
        }

        [Fact]
        public void HandlesTwoTemplatesInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10, @<p>Foo #@item</p>, @<p>Foo #@item</p>)");
        }

        [Fact]
        public void ProducesErrorButCorrectlyParsesNestedTemplateInImplicitExprParens()
        {
            // ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInImplicitExpressionParens
            ParseBlockTest("Html.Repeat(10, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>)");
        }

        [Fact]
        public void HandlesSimpleTemplateInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void HandlesTwoTemplatesInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@item</p>, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void ProducesErrorButCorrectlyParsesNestedTemplateInStmtWithinCodeBlock()
        {
            // ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinCodeBlock
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>); }");
        }

        [Fact]
        public void HandlesSimpleTemplateInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void HandlessTwoTemplatesInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@item</p>, @<p>Foo #@item</p>); }");
        }

        [Fact]
        public void ProducesErrorButCorrectlyParsesNestedTemplateInStmtWithinStmtBlock()
        {
            // ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinStatementBlock
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>); }");
        }

        [Fact]
        public void _WithDoubleTransition_DoesNotThrow()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo, @<p foo='@@'>Foo #@item</p>); }");
        }
    }
}
