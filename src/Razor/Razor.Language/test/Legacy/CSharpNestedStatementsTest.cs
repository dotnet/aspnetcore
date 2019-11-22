// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpNestedStatementsTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void NestedSimpleStatement()
        {
            ParseBlockTest("@while(true) { foo(); }");
        }

        [Fact]
        public void NestedKeywordStatement()
        {
            ParseBlockTest("@while(true) { for(int i = 0; i < 10; i++) { foo(); } }");
        }

        [Fact]
        public void NestedCodeBlock()
        {
            ParseBlockTest("@while(true) { { { { foo(); } } } }");
        }

        [Fact]
        public void NestedImplicitExpression()
        {
            ParseBlockTest("@while(true) { @foo }");
        }

        [Fact]
        public void NestedExplicitExpression()
        {
            ParseBlockTest("@while(true) { @(foo) }");
        }

        [Fact]
        public void NestedMarkupBlock()
        {
            ParseBlockTest("@while(true) { <p>Hello</p> }");
        }
    }
}
