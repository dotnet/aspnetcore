// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBExplicitExpressionTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void VB_Simple_ExplicitExpression()
        {
            ParseBlockTest("@(foo)",
                new ExpressionBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                    Factory.Code("foo").AsExpression(),
                    Factory.MetaCode(")").Accepts(AcceptedCharacters.None)));
        }
    }
}
