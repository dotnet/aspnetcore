// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBLayoutDirectiveTest : VBHtmlCodeParserTestBase
    {
        [Theory]
        [InlineData("layout")]
        [InlineData("Layout")]
        [InlineData("LAYOUT")]
        [InlineData("layOut")]
        [InlineData("LayOut")]
        [InlineData("LaYoUt")]
        [InlineData("lAyOuT")]
        public void LayoutDirectiveSupportsAnyCasingOfKeyword(string keyword)
        {
            ParseBlockTest("@" + keyword,
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(keyword)
                )
            );
        }

        [Fact]
        public void LayoutDirectiveAcceptsAllTextToEndOfLine()
        {
            ParseBlockTest("@Layout Foo Bar Baz",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Layout ").Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("Foo Bar Baz")
                           .With(new SetLayoutCodeGenerator("Foo Bar Baz"))
                           .WithEditorHints(EditorHints.VirtualPath | EditorHints.LayoutPage)
                )
            );
        }

        [Fact]
        public void LayoutDirectiveAcceptsAnyIfNoWhitespaceFollowingLayoutKeyword()
        {
            ParseBlockTest("@Layout",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Layout")
                )
            );
        }

        [Fact]
        public void LayoutDirectiveOutputsMarkerSpanIfAnyWhitespaceAfterLayoutKeyword()
        {
            ParseBlockTest("@Layout ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Layout ").Accepts(AcceptedCharacters.None),
                    Factory.EmptyVB()
                           .AsMetaCode()
                           .With(new SetLayoutCodeGenerator(String.Empty))
                           .WithEditorHints(EditorHints.VirtualPath | EditorHints.LayoutPage)
                )
            );
        }

        [Fact]
        public void LayoutDirectiveAcceptsTrailingNewlineButDoesNotIncludeItInLayoutPath()
        {
            ParseBlockTest("@Layout Foo" + Environment.NewLine,
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Layout ").Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("Foo\r\n")
                           .With(new SetLayoutCodeGenerator("Foo"))
                           .Accepts(AcceptedCharacters.None)
                           .WithEditorHints(EditorHints.VirtualPath | EditorHints.LayoutPage)
                )
            );
        }
    }
}
