// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CSharpRazorCodeGeneratorTest : RazorCodeGeneratorTest<CSharpRazorCodeLanguage>
    {
        protected override string FileExtension
        {
            get { return "cshtml"; }
        }

        protected override string LanguageName
        {
            get { return "CS"; }
        }

        protected override string BaselineExtension
        {
            get { return "cs"; }
        }

        private const string TestPhysicalPath = @"C:\Bar.cshtml";
        private const string TestVirtualPath = "~/Foo/Bar.cshtml";

        [Fact]
        public void ConstructorRequiresNonNullClassName()
        {
            Assert.ThrowsArgumentNullOrEmptyString(() => new CSharpRazorCodeGenerator(null, TestRootNamespaceName, TestPhysicalPath, CreateHost()), "className");
        }

        [Fact]
        public void ConstructorRequiresNonEmptyClassName()
        {
            Assert.ThrowsArgumentNullOrEmptyString(() => new CSharpRazorCodeGenerator(String.Empty, TestRootNamespaceName, TestPhysicalPath, CreateHost()), "className");
        }

        [Fact]
        public void ConstructorRequiresNonNullRootNamespaceName()
        {
            Assert.ThrowsArgumentNull(() => new CSharpRazorCodeGenerator("Foo", null, TestPhysicalPath, CreateHost()), "rootNamespaceName");
        }

        [Fact]
        public void ConstructorAllowsEmptyRootNamespaceName()
        {
            new CSharpRazorCodeGenerator("Foo", String.Empty, TestPhysicalPath, CreateHost());
        }

        [Fact]
        public void ConstructorRequiresNonNullHost()
        {
            Assert.ThrowsArgumentNull(() => new CSharpRazorCodeGenerator("Foo", TestRootNamespaceName, TestPhysicalPath, null), "host");
        }

        [Theory]
        [InlineData("NestedCodeBlocks")]
        [InlineData("CodeBlock")]
        [InlineData("ExplicitExpression")]
        [InlineData("MarkupInCodeBlock")]
        [InlineData("Blocks")]
        [InlineData("ImplicitExpression")]
        [InlineData("Imports")]
        [InlineData("ExpressionsInCode")]
        [InlineData("FunctionsBlock")]
        [InlineData("FunctionsBlock_Tabs")]
        [InlineData("Templates")]
        [InlineData("Sections")]
        [InlineData("RazorComments")]
        [InlineData("Helpers")]
        [InlineData("HelpersMissingCloseParen")]
        [InlineData("HelpersMissingOpenBrace")]
        [InlineData("HelpersMissingOpenParen")]
        [InlineData("NestedHelpers")]
        [InlineData("InlineBlocks")]
        [InlineData("LayoutDirective")]
        [InlineData("ConditionalAttributes")]
        [InlineData("ResolveUrl")]
        [InlineData("Await")]
        public void CSharpCodeGeneratorCorrectlyGeneratesRunTimeCode(string testType)
        {
            RunTest(testType);
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMappingsForAwait()
        {
            RunTest("Await",
                    "Await.DesignTime",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
                    {
                        BuildLineMapping(12, 0, 12, 173, 9, 0, 76),
                        BuildLineMapping(192, 9, 39, 637, 30, 15, 11),
                        BuildLineMapping(247, 10, 38, 750, 35, 14, 11),
                        BuildLineMapping(304, 11, 39, 832, 40, 12, 14),
                        BuildLineMapping(371, 12, 46, 919, 46, 13, 1),
                        BuildLineMapping(376, 12, 51, 1027, 52, 18, 11),
                        BuildLineMapping(391, 12, 66, 1115, 57, 18, 1),
                        BuildLineMapping(448, 13, 49, 1224, 63, 19, 5),
                        BuildLineMapping(578, 18, 42, 1332, 68, 15, 15),
                        BuildLineMapping(640, 19, 41, 1452, 73, 17, 22),
                        BuildLineMapping(711, 20, 42, 1545, 78, 12, 39),
                        BuildLineMapping(806, 21, 49, 1657, 84, 13, 1),
                        BuildLineMapping(811, 21, 54, 1765, 90, 18, 27),
                        BuildLineMapping(842, 21, 85, 1873, 95, 22, 1),
                        BuildLineMapping(902, 22, 52, 1982, 101, 19, 19)
                    });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMappingsForSimpleUnspacedIf()
        {
            RunTest("SimpleUnspacedIf",
                    "SimpleUnspacedIf.DesignTime.Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
                    {
                        BuildLineMapping(1, 0, 1, 494, 21, 0, 15),
                        BuildLineMapping(27, 2, 12, 585, 29, 6, 3)
                    });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMappingsForRazorCommentsAtDesignTime()
        {
            RunTest("RazorComments", "RazorComments.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs,
                expectedDesignTimePragmas: new List<LineMapping>()
                {
                    BuildLineMapping(81, 3, 487, 21, 2, 6),
                    BuildLineMapping(122, 4, 39, 598, 28, 38, 22),
                    BuildLineMapping(173, 5, 49, 735, 35, 48, 58),
                    BuildLineMapping(238, 11, 861, 44, 2, 24),
                    BuildLineMapping(310, 12, 1019, 50, 45, 3),
                    BuildLineMapping(323, 14, 2, 1116, 55, 6, 1),
                    BuildLineMapping(328, 14, 1159, 57, 7, 1),

            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGenerateMappingForOpenedCurlyIf()
        {
            OpenedIf(withTabs: true);
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGenerateMappingForOpenedCurlyIfSpaces()
        {
            OpenedIf(withTabs: false);
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesImportStatementsAtDesignTime()
        {
            RunTest("Imports", "Imports.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(1, 0, 1, 51, 3, 0, 15),
                BuildLineMapping(19, 1, 1, 132, 9, 0, 32),
                BuildLineMapping(54, 2, 1, 230, 15, 0, 12),
                BuildLineMapping(99, 4, 762, 38, 29, 21),
                BuildLineMapping(161, 5, 906, 43, 35, 20),
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTime()
        {
            RunTest("FunctionsBlock",
                    "FunctionsBlock.DesignTime",
                    designTimeMode: true,
                    tabTest: TabTest.NoTabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(12, 0, 12, 191, 9, 0, 4),
                BuildLineMapping(33, 4, 12, 259, 15, 0, 104),
                BuildLineMapping(167, 11, 770, 36, 25, 11)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTimeTabs()
        {
            RunTest("FunctionsBlock",
                    "FunctionsBlock.DesignTime.Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(12, 0, 12, 191, 9, 0, 4),
                BuildLineMapping(33, 4, 12, 259, 15, 0, 104),
                BuildLineMapping(167, 11, 25, 758, 36, 13, 11)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMinimalFunctionsBlocksAtDesignTimeTabs()
        {
            RunTest("FunctionsBlockMinimal",
                    "FunctionsBlockMinimal.DesignTime.Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(16, 2, 12, 205, 9, 0, 55)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesHiddenSpansWithinCode()
        {
            RunTest("HiddenSpansInCode", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<LineMapping>
            {
                BuildLineMapping(2, 0, 499, 21, 2, 6),
                BuildLineMapping(9, 1, 581, 28, 5, 5)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorGeneratesCodeWithParserErrorsInDesignTimeMode()
        {
            RunTest("ParserError", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 481, 21, 2, 31)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesInheritsAtRuntime()
        {
            RunTest("Inherits", baselineName: "Inherits.Runtime");
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesInheritsAtDesigntime()
        {
            RunTest("Inherits", baselineName: "Inherits.Designtime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 321, 12, 10, 25),
                BuildLineMapping(1, 0, 1, 651, 26, 6, 5)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCode()
        {
            RunTest("UnfinishedExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 526, 21, 2, 2),
                BuildLineMapping(5, 1, 1, 621, 27, 6, 9),
                BuildLineMapping(14, 1, 719, 32, 10, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCodeTabs()
        {
            RunTest("UnfinishedExpressionInCode",
                    "UnfinishedExpressionInCode.Tabs",
                    tabTest: TabTest.Tabs,
                    designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 526, 21, 2, 2),
                BuildLineMapping(5, 1, 1, 621, 27, 6, 9),
                BuildLineMapping(14, 1, 10, 713, 32, 4, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasMarkupAndExpressions()
        {
            RunTest("DesignTime",
                designTimeMode: true,
                tabTest: TabTest.NoTabs,
                expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(222, 16, 8, 209, 10, 0, 7),
                BuildLineMapping(229, 16, 352, 16, 15, 26),
                BuildLineMapping(265, 18, 461, 24, 18, 9),
                BuildLineMapping(274, 20, 556, 33, 0, 1),
                BuildLineMapping(20, 1, 13, 926, 51, 12, 36),
                BuildLineMapping(74, 2, 1073, 58, 22, 1),
                BuildLineMapping(79, 2, 1164, 63, 27, 15),
                BuildLineMapping(113, 7, 2, 1274, 70, 6, 12),
                BuildLineMapping(129, 8, 1, 1380, 75, 6, 4),
                BuildLineMapping(142, 8, 1505, 77, 14, 3),
                BuildLineMapping(153, 8, 1602, 84, 25, 1),
                BuildLineMapping(204, 13, 5, 1811, 94, 6, 3)
            });
        }


        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForImplicitExpressionStartedAtEOF()
        {
            RunTest("ImplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(19, 2, 1, 533, 21, 6, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForExplicitExpressionStartedAtEOF()
        {
            RunTest("ExplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 2, 533, 21, 6, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForCodeBlockStartedAtEOF()
        {
            RunTest("CodeBlockAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 490, 21, 2, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpression()
        {
            RunTest("EmptyImplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(19, 2, 1, 533, 21, 6, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCode()
        {
            RunTest("EmptyImplicitExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 535, 21, 2, 6),
                BuildLineMapping(9, 1, 5, 636, 28, 6, 0),
                BuildLineMapping(9, 1, 723, 33, 5, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCodeTabs()
        {
            RunTest("EmptyImplicitExpressionInCode",
                    "EmptyImplicitExpressionInCode.Tabs",
                    tabTest: TabTest.Tabs,
                    designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 535, 21, 2, 6),
                BuildLineMapping(9, 1, 5, 636, 28, 6, 0),
                BuildLineMapping(9, 1, 5, 720, 33, 2, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyExplicitExpression()
        {
            RunTest("EmptyExplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 2, 533, 21, 6, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyCodeBlock()
        {
            RunTest("EmptyCodeBlock", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 490, 21, 2, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorDoesNotRenderLinePragmasIfGenerateLinePragmasIsSetToFalse()
        {
            RunTest("NoLinePragmas", generatePragmas: false);
        }

        [Fact]
        public void CSharpCodeGeneratorRendersHelpersBlockCorrectlyWhenInstanceHelperRequested()
        {
            RunTest("Helpers", baselineName: "Helpers.Instance", hostConfig: h => h.StaticHelpers = false);
        }

        // TODO: This should be re-added once instrumentation support has been added
        //[Fact]
        //public void CSharpCodeGeneratorCorrectlyInstrumentsRazorCodeWhenInstrumentationRequested()
        //{
        //    RunTest("Instrumented", hostConfig: host =>
        //    {
        //        host.EnableInstrumentation = true;
        //        host.InstrumentedSourceFilePath = String.Format("~/{0}.cshtml", host.DefaultClassName);
        //    });
        //}

        [Fact]
        public void CSharpCodeGeneratorGeneratesUrlsCorrectlyWithCommentsAndQuotes()
        {
            RunTest("HtmlCommentWithQuote_Single",
                    tabTest: TabTest.NoTabs);

            RunTest("HtmlCommentWithQuote_Double",
                    tabTest: TabTest.NoTabs);
        }

        private void OpenedIf(bool withTabs)
        {
            int tabOffsetForMapping = 7;

            // where the test is running with tabs, the offset into the CS buffer changes for the whitespace mapping
            // with spaces we get 7xspace -> offset of 8 (column = offset+1)
            // with tabs we get tab + 3 spaces -> offset of 4 chars + 1 = 5
            if (withTabs)
            {
                tabOffsetForMapping -= 3;
            }

            RunTest("OpenedIf",
                "OpenedIf.DesignTime" + (withTabs ? ".Tabs" : ""),
                    designTimeMode: true,
                    tabTest: withTabs ? TabTest.Tabs : TabTest.NoTabs,
                    spans: new TestSpan[]
            {
                new TestSpan(SpanKind.Markup, 0, 16),
                new TestSpan(SpanKind.Transition, 16, 17),
                new TestSpan(SpanKind.Code, 17, 31),
                new TestSpan(SpanKind.Markup, 31, 38),
                new TestSpan(SpanKind.Code, 38, 40),
                new TestSpan(SpanKind.Markup, 40, 47),
                new TestSpan(SpanKind.Code, 47, 47),
            },
            expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(17, 2, 1, 470, 21, 0, 14),
                BuildLineMapping(38, 3, 7, 544 + tabOffsetForMapping, 27, tabOffsetForMapping, 2),
                // Multiply the tab offset absolute index by 2 to account for the first mapping
                BuildLineMapping(47, 4, 7, 606 + tabOffsetForMapping * 2, 33, tabOffsetForMapping, 0)
            });
        }

        private static LineMapping BuildLineMapping(int documentAbsoluteIndex, int documentLineIndex, int generatedAbsoluteIndex, int generatedLineIndex, int characterOffsetIndex, int contentLength)
        {
            return BuildLineMapping(documentAbsoluteIndex, documentLineIndex, characterOffsetIndex, generatedAbsoluteIndex, generatedLineIndex, characterOffsetIndex, contentLength);
        }

        private static LineMapping BuildLineMapping(int documentAbsoluteIndex, int documentLineIndex, int documentCharacterOffsetIndex, int generatedAbsoluteIndex, int generatedLineIndex, int generatedCharacterOffsetIndex, int contentLength)
        {
            return new LineMapping(
                        documentLocation: new MappingLocation(new SourceLocation(documentAbsoluteIndex, documentLineIndex, documentCharacterOffsetIndex), contentLength),
                        generatedLocation: new MappingLocation(new SourceLocation(generatedAbsoluteIndex, generatedLineIndex, generatedCharacterOffsetIndex), contentLength)
                    );
        }
    }
}
