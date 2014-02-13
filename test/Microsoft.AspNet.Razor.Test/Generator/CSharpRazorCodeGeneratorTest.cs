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
        public void CSharpCodeGeneratorCorrectlyGeneratesRunTimeCode(string testType)
        {
            if (!Directory.Exists("./tests"))
            {
                Directory.CreateDirectory("./tests");
            }

            RunTest(testType, onResults: (results) =>
            {
                File.WriteAllText(String.Format("./tests/{0}.cs", testType), results.GeneratedCode);
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
                        BuildLineMapping(1, 0,449, 20, 1, 15),
                        BuildLineMapping(27, 2, 12, 538, 27, 6, 3)
                    });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMappingsForRazorCommentsAtDesignTime()
        {
            RunTest("RazorComments", "RazorComments.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs,
                expectedDesignTimePragmas: new List<LineMapping>()
                {
                    BuildLineMapping(81, 3, 441, 20, 2, 6),
                    BuildLineMapping(122, 4, 551, 26, 39, 22),
                    BuildLineMapping(173, 5, 687, 32, 49, 58),
                    BuildLineMapping(238, 11, 811, 40, 2, 24),
                    BuildLineMapping(310, 12, 966, 46, 45, 3),
                    BuildLineMapping(323, 14, 1070, 52, 2, 1),
                    BuildLineMapping(328, 14, 1142, 56, 7, 1),

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
                BuildLineMapping(19, 1, 1, 130, 8, 0, 32),
                BuildLineMapping(54, 2, 1, 226, 13, 0, 12),
                BuildLineMapping(99, 4, 702, 35, 29, 21),
                BuildLineMapping(161, 5, 850, 41, 35, 20),
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
                BuildLineMapping(12, 0, 168, 8, 12, 4),
                BuildLineMapping(33, 4, 246, 13, 12, 104),
                BuildLineMapping(167, 11, 744, 34, 25, 11)
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
                BuildLineMapping(12, 0, 12, 159, 8, 3, 4),
                BuildLineMapping(33, 4, 12, 228, 13, 3, 104),
                BuildLineMapping(167, 11, 25, 708, 34, 7, 11)
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
                BuildLineMapping(16, 2, 12, 176, 8, 6, 55)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesHiddenSpansWithinCode()
        {
            RunTest("HiddenSpansInCode", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<LineMapping>
            {
                BuildLineMapping(2, 0, 453, 20, 2, 6),
                BuildLineMapping(9, 1, 533, 26, 5, 5)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorGeneratesCodeWithParserErrorsInDesignTimeMode()
        {
            RunTest("ParserError", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 435, 20, 2, 31)
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
                BuildLineMapping(20, 2, 286, 11, 10, 25),
                BuildLineMapping(1, 0, 591, 25, 1, 5)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCode()
        {
            RunTest("UnfinishedExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 480, 20, 2, 2),
                BuildLineMapping(5, 1, 579, 26, 1, 9),
                BuildLineMapping(14, 1, 689, 31, 10, 2)
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
                BuildLineMapping(2, 0, 480, 20, 2, 2),
                BuildLineMapping(5, 1, 579, 26, 1, 9),
                BuildLineMapping(14, 1, 10, 683, 31, 4, 2)
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
                BuildLineMapping(222, 16, 8, 182, 9, 0, 7),
                BuildLineMapping(229, 16, 323, 14, 15, 26),
                BuildLineMapping(265, 18, 430, 21, 18, 9),
                BuildLineMapping(274, 20, 523, 29, 0, 1),
                BuildLineMapping(20, 1, 881, 46, 13, 36),
                BuildLineMapping(74, 2, 1021, 53, 22, 1),
                BuildLineMapping(79, 2, 1124, 58, 27, 15),
                BuildLineMapping(113, 7, 1223, 65, 2, 12),
                BuildLineMapping(129, 8, 1331, 71, 1, 4),
                BuildLineMapping(142, 8, 1498, 77, 14, 3),
                BuildLineMapping(153, 8, 1635, 84, 25, 1),
                BuildLineMapping(204, 13, 1786, 91, 5, 3)
            });
        }


        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForImplicitExpressionStartedAtEOF()
        {
            RunTest("ImplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(19, 2, 490, 21, 1, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForExplicitExpressionStartedAtEOF()
        {
            RunTest("ExplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 491, 21, 2, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForCodeBlockStartedAtEOF()
        {
            RunTest("CodeBlockAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 444, 20, 2, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpression()
        {
            RunTest("EmptyImplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(19, 2, 490, 21, 1, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCode()
        {
            RunTest("EmptyImplicitExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 489, 20, 2, 6),
                BuildLineMapping(9, 1, 601, 27, 5, 0),
                BuildLineMapping(9, 1, 700, 32, 5, 2)
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
                BuildLineMapping(2, 0, 489, 20, 2, 6),
                BuildLineMapping(9, 1, 5, 598, 27, 2, 0),
                BuildLineMapping(9, 1, 5, 694, 32, 2, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyExplicitExpression()
        {
            RunTest("EmptyExplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 491, 21, 2, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyCodeBlock()
        {
            RunTest("EmptyCodeBlock", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 444, 20, 2, 0)
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
                BuildLineMapping(17, 2, 425, 20, 1, 14),
                BuildLineMapping(38, 3, 7, 497 + tabOffsetForMapping, 25, tabOffsetForMapping, 2),
                // Multiply the tab offset absolute index by 2 to account for the first mapping
                BuildLineMapping(47, 4, 7, 557 + tabOffsetForMapping * 2, 30, tabOffsetForMapping, 0)
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
