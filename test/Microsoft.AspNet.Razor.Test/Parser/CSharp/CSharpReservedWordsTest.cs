// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

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
                           new RazorError(RazorResources.ParseError_ReservedWord(word), SourceLocation.Zero));
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
