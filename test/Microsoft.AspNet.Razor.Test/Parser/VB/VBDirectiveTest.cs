// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBDirectiveTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void VB_Code_Directive()
        {
            ParseBlockTest("@Code" + Environment.NewLine
                         + "    foo()" + Environment.NewLine
                         + "End Code" + Environment.NewLine
                         + "' Not part of the block",
                new StatementBlock(
                    Factory.CodeTransition(SyntaxConstants.TransitionString)
                           .Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("Code")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    foo()\r\n")
                           .AsStatement()
                           .With(new AutoCompleteEditHandler(VBLanguageCharacteristics.Instance.TokenizeString)),
                    Factory.MetaCode("End Code")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void VB_Functions_Directive()
        {
            ParseBlockTest("@Functions" + Environment.NewLine
                         + "    Public Function Foo() As String" + Environment.NewLine
                         + "        Return \"Foo\"" + Environment.NewLine
                         + "    End Function" + Environment.NewLine
                         + Environment.NewLine
                         + "    Public Sub Bar()" + Environment.NewLine
                         + "    End Sub" + Environment.NewLine
                         + "End Functions" + Environment.NewLine
                         + "' Not part of the block",
                new FunctionsBlock(
                    Factory.CodeTransition(SyntaxConstants.TransitionString)
                           .Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("Functions")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    Public Function Foo() As String\r\n        Return \"Foo\"\r\n    End Function\r\n\r\n    Public Sub Bar()\r\n    End Sub\r\n")
                           .AsFunctionsBody(),
                    Factory.MetaCode("End Functions")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void VB_Section_Directive()
        {
            ParseBlockTest("@Section Header" + Environment.NewLine
                         + "    <p>Foo</p>" + Environment.NewLine
                         + "End Section",
                new SectionBlock(new SectionCodeGenerator("Header"),
                    Factory.CodeTransition(SyntaxConstants.TransitionString),
                    Factory.MetaCode("Section Header"),
                    new MarkupBlock(
                        Factory.Markup("\r\n    <p>Foo</p>\r\n")),
                    Factory.MetaCode("End Section")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void SessionStateDirectiveWorks()
        {
            ParseBlockTest("@SessionState InProc" + Environment.NewLine,
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("SessionState ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("InProc\r\n")
                        .Accepts(AcceptedCharacters.None)
                        .With(new RazorDirectiveAttributeCodeGenerator("SessionState", "InProc"))
                )
            );
        }

        [Fact]
        public void SessionStateDirectiveIsCaseInsensitive()
        {
            ParseBlockTest("@sessionstate disabled" + Environment.NewLine,
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("sessionstate ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("disabled\r\n")
                        .Accepts(AcceptedCharacters.None)
                        .With(new RazorDirectiveAttributeCodeGenerator("SessionState", "disabled"))
                )
            );
        }

        [Fact]
        public void VB_Helper_Directive()
        {
            ParseBlockTest("@Helper Strong(s as String)" + Environment.NewLine
                         + "    s = s.ToUpperCase()" + Environment.NewLine
                         + "    @<strong>s</strong>" + Environment.NewLine
                         + "End Helper",
                new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Strong(s as String)", 8, 0, 8), headerComplete: true),
                    Factory.CodeTransition(SyntaxConstants.TransitionString),
                    Factory.MetaCode("Helper ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Strong(s as String)").Hidden(),
                    new StatementBlock(
                        Factory.Code("\r\n    s = s.ToUpperCase()\r\n")
                               .AsStatement(),
                        new MarkupBlock(
                            Factory.Markup("    "),
                            Factory.MarkupTransition(SyntaxConstants.TransitionString),
                            Factory.Markup("<strong>s</strong>\r\n")
                                   .Accepts(AcceptedCharacters.None)),
                        Factory.EmptyVB()
                               .AsStatement(),
                        Factory.MetaCode("End Helper")
                               .Accepts(AcceptedCharacters.None))));
        }
    }
}
