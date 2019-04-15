// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpNestedStatementsTest : ParserTestBase
    {
        [Fact]
        public void NestedSimpleStatement()
        {
            ParseDocumentTest("@while(true) { foo(); }");
        }

        [Fact]
        public void NestedKeywordStatement()
        {
            ParseDocumentTest("@while(true) { for(int i = 0; i < 10; i++) { foo(); } }");
        }

        [Fact]
        public void NestedCodeBlock()
        {
            ParseDocumentTest("@while(true) { { { { foo(); } } } }");
        }

        [Fact]
        public void NestedImplicitExpression()
        {
            ParseDocumentTest("@while(true) { @foo }");
        }

        [Fact]
        public void NestedExplicitExpression()
        {
            ParseDocumentTest("@while(true) { @(foo) }");
        }

        [Fact]
        public void NestedMarkupBlock()
        {
            ParseDocumentTest("@while(true) { <p>Hello</p> }");
        }
    }
}
