// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser.Internal
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
                           new RazorError(
                               RazorResources.FormatParseError_ReservedWord(word),
                               SourceLocation.Zero,
                               word.Length));
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
