// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpReservedWordsTest : CsHtmlCodeParserTestBase
    {
        [Theory]
        [InlineData("namespace")]
        [InlineData("class")]
        public void ReservedWords(string word)
        {
            ParseBlockTest(word,
                           new DirectiveBlock(
                               Factory.MetaCode(word).Accepts(AcceptedCharacters.None)
                               ),
                           new RazorError(RazorResources.FormatParseError_ReservedWord(word), SourceLocation.Zero));
        }

        [Theory]
        [InlineData("Namespace")]
        [InlineData("Class")]
        [InlineData("NAMESPACE")]
        [InlineData("CLASS")]
        [InlineData("nameSpace")]
        [InlineData("NameSpace")]
        private void ReservedWordsAreCaseSensitive(string word)
        {
            ParseBlockTest(word,
                           new ExpressionBlock(
                               Factory.Code(word)
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ));
        }
    }
}
