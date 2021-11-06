// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpTemplateTest : ParserTestBase
{
    [Fact]
    public void HandlesSingleLineTemplate()
    {
        ParseDocumentTest("@{ var foo = @: bar" + Environment.NewLine + "; }");
    }

    [Fact]
    public void HandlesSingleLineImmediatelyFollowingStatementChar()
    {
        ParseDocumentTest("@{i@: bar" + Environment.NewLine + "}");
    }

    [Fact]
    public void HandlesSimpleTemplateInExplicitExpressionParens()
    {
        ParseDocumentTest("@(Html.Repeat(10, @<p>Foo #@item</p>))");
    }

    [Fact]
    public void HandlesSimpleTemplateInImplicitExpressionParens()
    {
        ParseDocumentTest("@Html.Repeat(10, @<p>Foo #@item</p>)");
    }

    [Fact]
    public void HandlesTwoTemplatesInImplicitExpressionParens()
    {
        ParseDocumentTest("@Html.Repeat(10, @<p>Foo #@item</p>, @<p>Foo #@item</p>)");
    }

    [Fact]
    public void ProducesErrorButCorrectlyParsesNestedTemplateInImplicitExprParens()
    {
        // ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInImplicitExpressionParens
        ParseDocumentTest("@Html.Repeat(10, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>)");
    }

    [Fact]
    public void HandlesSimpleTemplateInStatementWithinCodeBlock()
    {
        ParseDocumentTest("@foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@item</p>); }");
    }

    [Fact]
    public void HandlesTwoTemplatesInStatementWithinCodeBlock()
    {
        ParseDocumentTest("@foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@item</p>, @<p>Foo #@item</p>); }");
    }

    [Fact]
    public void ProducesErrorButCorrectlyParsesNestedTemplateInStmtWithinCodeBlock()
    {
        // ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinCodeBlock
        ParseDocumentTest("@foreach(foo in Bar) { Html.ExecuteTemplate(foo, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>); }");
    }

    [Fact]
    public void HandlesSimpleTemplateInStatementWithinStatementBlock()
    {
        ParseDocumentTest("@{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@item</p>); }");
    }

    [Fact]
    public void HandlessTwoTemplatesInStatementWithinStatementBlock()
    {
        ParseDocumentTest("@{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@item</p>, @<p>Foo #@item</p>); }");
    }

    [Fact]
    public void ProducesErrorButCorrectlyParsesNestedTemplateInStmtWithinStmtBlock()
    {
        // ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinStatementBlock
        ParseDocumentTest("@{ var foo = bar; Html.ExecuteTemplate(foo, @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>); }");
    }

    [Fact]
    public void _WithDoubleTransition_DoesNotThrow()
    {
        ParseDocumentTest("@{ var foo = bar; Html.ExecuteTemplate(foo, @<p foo='@@'>Foo #@item</p>); }");
    }
}
