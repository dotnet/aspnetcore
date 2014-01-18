// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBReservedWordsTest : VBHtmlCodeParserTestBase
    {
        [Theory]
        [InlineData("Namespace")]
        [InlineData("Class")]
        [InlineData("NAMESPACE")]
        [InlineData("CLASS")]
        [InlineData("NameSpace")]
        [InlineData("nameSpace")]
        private void ReservedWords(string word)
        {
            ParseBlockTest(word,
                new DirectiveBlock(
                    Factory.MetaCode(word).Accepts(AcceptedCharacters.None)),
                new RazorError(
                    String.Format(RazorResources.ParseError_ReservedWord, word),
                    SourceLocation.Zero));
        }
    }
}
