// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.CodeGenerators;
using Xunit;

namespace Microsoft.AspNetCore.Razor.CodeGenerators
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

        [Fact]
        public void ConstructorRequiresNonNullClassName()
        {
            Assert.Throws<ArgumentException>(
                "className",
                () => new RazorChunkGenerator(null, TestRootNamespaceName, TestPhysicalPath, CreateHost()));
        }

        [Fact]
        public void ConstructorRequiresNonEmptyClassName()
        {
            Assert.Throws<ArgumentException>(
                "className",
                () => new RazorChunkGenerator(string.Empty, TestRootNamespaceName, TestPhysicalPath, CreateHost()));
        }

        [Fact]
        public void ConstructorAllowsEmptyRootNamespaceName()
        {
            new RazorChunkGenerator("Foo", string.Empty, TestPhysicalPath, CreateHost());
        }

        [Theory]
        [InlineData("StringLiterals")]
        [InlineData("NestedCSharp")]
        [InlineData("NullConditionalExpressions")]
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
        [InlineData("InlineBlocks")]
        [InlineData("ConditionalAttributes")]
        [InlineData("Await")]
        [InlineData("CodeBlockWithTextElement")]
        public void CSharpChunkGeneratorCorrectlyGeneratesRunTimeCode(string testType)
        {
            RunTest(testType);
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesMappingsForNestedCSharp()
        {
            RunTest(
                "NestedCSharp",
                "NestedCSharp.DesignTime",
                designTimeMode: true,
                tabTest: TabTest.NoTabs,
                expectedDesignTimePragmas: new List<LineMapping>
                {
                    BuildLineMapping(
                        documentAbsoluteIndex: 2,
                        documentLineIndex: 0,
                        generatedAbsoluteIndex: 522,
                        generatedLineIndex: 22,
                        characterOffsetIndex: 2,
                        contentLength: 6),
                    BuildLineMapping(
                        documentAbsoluteIndex: 9,
                        documentLineIndex: 1,
                        documentCharacterOffsetIndex: 5,
                        generatedAbsoluteIndex: 598,
                        generatedLineIndex: 29,
                        generatedCharacterOffsetIndex: 4,
                        contentLength: 53),
                    BuildLineMapping(
                        documentAbsoluteIndex: 82,
                        documentLineIndex: 4,
                        generatedAbsoluteIndex: 730,
                        generatedLineIndex: 37,
                        characterOffsetIndex: 13,
                        contentLength: 16),
                    BuildLineMapping(
                        documentAbsoluteIndex: 115,
                        documentLineIndex: 5,
                        generatedAbsoluteIndex: 825,
                        generatedLineIndex: 42,
                        characterOffsetIndex: 14,
                        contentLength: 7),
                    BuildLineMapping(
                        documentAbsoluteIndex: 122,
                        documentLineIndex: 6,
                        generatedAbsoluteIndex: 903,
                        generatedLineIndex: 49,
                        characterOffsetIndex: 5,
                        contentLength: 2),
                });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesMappingsForNullConditionalOperator()
        {
            RunTest("NullConditionalExpressions",
                    "NullConditionalExpressions.DesignTime",
                    designTimeMode: true,
                    tabTest: TabTest.NoTabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
                    {
                        BuildLineMapping(2, 0, 564, 22, 2, 6),
                        BuildLineMapping(9, 1, 5, 656, 29, 6, 13),
                        BuildLineMapping(22, 1, 766, 34, 18, 6),
                        BuildLineMapping(29, 2, 5, 858, 41, 6, 22),
                        BuildLineMapping(51, 2, 986, 46, 27, 6),
                        BuildLineMapping(58, 3, 5, 1078, 53, 6, 26),
                        BuildLineMapping(84, 3, 1214, 58, 31, 6),
                        BuildLineMapping(91, 4, 5, 1306, 65, 6, 41),
                        BuildLineMapping(132, 4, 1472, 70, 46, 2),
                        BuildLineMapping(140, 7, 1, 1558, 76, 6, 13),
                        BuildLineMapping(156, 8, 1, 1656, 81, 6, 22),
                        BuildLineMapping(181, 9, 1, 1764, 86, 6, 26),
                        BuildLineMapping(210, 10, 1, 1876, 91, 6, 41)
                    });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesMappingsForAwait()
        {
            RunTest("Await",
                    "Await.DesignTime",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
                    {
                        BuildLineMapping(12, 0, 12, 173, 9, 0, 76),
                        BuildLineMapping(192, 9, 39, 646, 31, 15, 11),
                        BuildLineMapping(247, 10, 38, 730, 36, 14, 11),
                        BuildLineMapping(304, 11, 39, 812, 41, 12, 14),
                        BuildLineMapping(371, 12, 46, 899, 47, 13, 1),
                        BuildLineMapping(376, 12, 51, 978, 53, 18, 11),
                        BuildLineMapping(391, 12, 66, 1066, 58, 18, 1),
                        BuildLineMapping(448, 13, 49, 1146, 64, 19, 5),
                        BuildLineMapping(578, 18, 42, 1225, 69, 15, 15),
                        BuildLineMapping(650, 19, 51, 1317, 74, 18, 19),
                        BuildLineMapping(716, 20, 41, 1412, 79, 17, 22),
                        BuildLineMapping(787, 21, 42, 1505, 84, 12, 39),
                        BuildLineMapping(884, 22, 51, 1619, 90, 15, 21),
                        BuildLineMapping(961, 23, 49, 1713, 96, 13, 1),
                        BuildLineMapping(966, 23, 54, 1792, 102, 18, 27),
                        BuildLineMapping(997, 23, 85, 1900, 107, 22, 1),
                        BuildLineMapping(1057, 24, 52, 1980, 113, 19, 19)
                    });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesMappingsForSimpleUnspacedIf()
        {
            RunTest("SimpleUnspacedIf",
                    "SimpleUnspacedIf.DesignTime.Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
                    {
                        BuildLineMapping(1, 0, 1, 532, 22, 0, 15),
                        BuildLineMapping(27, 2, 12, 623, 30, 6, 3)
                    });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesMappingsForRazorCommentsAtDesignTime()
        {
            RunTest("RazorComments", "RazorComments.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs,
                expectedDesignTimePragmas: new List<LineMapping>()
                {
                    BuildLineMapping(81, 3, 525, 22, 2, 6),
                    BuildLineMapping(122, 4, 39, 636, 29, 38, 22),
                    BuildLineMapping(173, 5, 49, 773, 36, 48, 58),
                    BuildLineMapping(238, 11, 899, 45, 2, 24),
                    BuildLineMapping(310, 12, 1036, 51, 45, 3),
                    BuildLineMapping(323, 14, 2, 1112, 56, 6, 1),
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGenerateMappingForOpenedCurlyIf()
        {
            OpenedIf(withTabs: true);
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGenerateMappingForOpenedCurlyIfSpaces()
        {
            OpenedIf(withTabs: false);
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesImportStatementsAtDesignTime()
        {
            RunTest("Imports", "Imports.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(1, 0, 1, 51, 3, 0, 15),
                BuildLineMapping(19, 1, 1, 132, 9, 0, 32),
                BuildLineMapping(54, 2, 1, 230, 15, 0, 12),
                BuildLineMapping(71, 4, 1, 308, 21, 0, 19),
                BuildLineMapping(93, 5, 1, 393, 27, 0, 27),
                BuildLineMapping(123, 6, 1, 486, 33, 0, 41),
                BuildLineMapping(197, 8, 1057, 57, 29, 21),
                BuildLineMapping(259, 9, 1174, 62, 35, 20)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTime()
        {
            RunTest("FunctionsBlock",
                    "FunctionsBlock.DesignTime",
                    designTimeMode: true,
                    tabTest: TabTest.NoTabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(12, 0, 12, 191, 9, 0, 4),
                BuildLineMapping(33, 4, 12, 259, 15, 0, 104),
                BuildLineMapping(167, 11, 788, 37, 25, 11)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTimeTabs()
        {
            RunTest("FunctionsBlock",
                    "FunctionsBlock.DesignTime.Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(12, 0, 12, 191, 9, 0, 4),
                BuildLineMapping(33, 4, 12, 259, 15, 0, 104),
                BuildLineMapping(167, 11, 25, 776, 37, 13, 11)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesMinimalFunctionsBlocksAtDesignTimeTabs()
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
        public void CSharpChunkGeneratorCorrectlyGeneratesHiddenSpansWithinCode()
        {
            RunTest("HiddenSpansInCode", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<LineMapping>
            {
                BuildLineMapping(2, 0, 537, 22, 2, 6),
                BuildLineMapping(9, 1, 619, 29, 5, 5)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorGeneratesCodeWithParserErrorsInDesignTimeMode()
        {
            RunTest("ParserError", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 519, 22, 2, 31)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesInheritsAtRuntime()
        {
            RunTest("Inherits", baselineName: "Inherits.Runtime");
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesInheritsAtDesigntime()
        {
            RunTest("Inherits", baselineName: "Inherits.Designtime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 321, 12, 10, 25),
                BuildLineMapping(1, 0, 1, 662, 27, 6, 5)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCode()
        {
            RunTest("UnfinishedExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 564, 22, 2, 2),
                BuildLineMapping(5, 1, 1, 650, 28, 6, 9),
                BuildLineMapping(14, 1, 748, 33, 10, 2)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCodeTabs()
        {
            RunTest("UnfinishedExpressionInCode",
                    "UnfinishedExpressionInCode.Tabs",
                    tabTest: TabTest.Tabs,
                    designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 564, 22, 2, 2),
                BuildLineMapping(5, 1, 1, 650, 28, 6, 9),
                BuildLineMapping(14, 1, 10, 742, 33, 4, 2)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasMarkupAndExpressions()
        {
            RunTest("DesignTime",
                designTimeMode: true,
                tabTest: TabTest.NoTabs,
                expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 20,
                    documentLineIndex: 1,
                    documentCharacterOffsetIndex: 13,
                    generatedAbsoluteIndex: 526,
                    generatedLineIndex: 22,
                    generatedCharacterOffsetIndex: 12,
                    contentLength: 36),
                BuildLineMapping(
                    documentAbsoluteIndex: 74,
                    documentLineIndex: 2,
                    generatedAbsoluteIndex: 648,
                    generatedLineIndex: 29,
                    characterOffsetIndex: 22,
                    contentLength: 1),
                BuildLineMapping(
                    documentAbsoluteIndex: 79,
                    documentLineIndex: 2,
                    generatedAbsoluteIndex: 739,
                    generatedLineIndex: 34,
                    characterOffsetIndex: 27,
                    contentLength: 15),
                BuildLineMapping(
                    documentAbsoluteIndex: 113,
                    documentLineIndex: 7,
                    documentCharacterOffsetIndex: 2,
                    generatedAbsoluteIndex: 824,
                    generatedLineIndex: 41,
                    generatedCharacterOffsetIndex: 6,
                    contentLength: 12),
                BuildLineMapping(
                    documentAbsoluteIndex: 129,
                    documentLineIndex: 8,
                    documentCharacterOffsetIndex: 1,
                    generatedAbsoluteIndex: 905,
                    generatedLineIndex: 46,
                    generatedCharacterOffsetIndex: 6,
                    contentLength: 4),
                BuildLineMapping(
                    documentAbsoluteIndex: 142,
                    documentLineIndex: 8,
                    generatedAbsoluteIndex: 1010,
                    generatedLineIndex: 48,
                    characterOffsetIndex: 14,
                    contentLength: 3),
                BuildLineMapping(
                    documentAbsoluteIndex: 204,
                    documentLineIndex: 13,
                    documentCharacterOffsetIndex: 5,
                    generatedAbsoluteIndex: 1196,
                    generatedLineIndex: 60,
                    generatedCharacterOffsetIndex: 6,
                    contentLength: 3),
            });
        }


        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForImplicitExpressionStartedAtEOF()
        {
            RunTest("ImplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(19, 2, 1, 559, 22, 6, 0)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForExplicitExpressionStartedAtEOF()
        {
            RunTest("ExplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 2, 559, 22, 6, 0)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForCodeBlockStartedAtEOF()
        {
            RunTest("CodeBlockAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 528, 22, 2, 0)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpression()
        {
            RunTest("EmptyImplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(19, 2, 1, 559, 22, 6, 0)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCode()
        {
            RunTest("EmptyImplicitExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 573, 22, 2, 6),
                BuildLineMapping(9, 1, 5, 668, 29, 6, 0),
                BuildLineMapping(9, 1, 755, 34, 5, 2)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCodeTabs()
        {
            RunTest("EmptyImplicitExpressionInCode",
                    "EmptyImplicitExpressionInCode.Tabs",
                    tabTest: TabTest.Tabs,
                    designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(2, 0, 573, 22, 2, 6),
                BuildLineMapping(9, 1, 5, 668, 29, 6, 0),
                BuildLineMapping(9, 1, 5, 752, 34, 2, 2)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyExplicitExpression()
        {
            RunTest("EmptyExplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 2, 559, 22, 6, 0)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyCodeBlock()
        {
            RunTest("EmptyCodeBlock", designTimeMode: true, expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(20, 2, 528, 22, 2, 0)
            });
        }

        [Fact]
        public void CSharpChunkGeneratorDoesNotRenderLinePragmasIfGenerateLinePragmasIsSetToFalse()
        {
            RunTest("NoLinePragmas", generatePragmas: false);
        }

        [Fact]
        public void CSharpChunkGeneratorCorrectlyInstrumentsRazorCodeWhenInstrumentationRequested()
        {
            RunTest("Instrumented", hostConfig: host =>
            {
                host.InstrumentedSourceFilePath = string.Format("~/{0}.cshtml", host.DefaultClassName);

                return host;
            });
        }

        [Fact]
        public void CSharpChunkGeneratorGeneratesUrlsCorrectlyWithCommentsAndQuotes()
        {
            RunTest("HtmlCommentWithQuote_Single",
                    tabTest: TabTest.NoTabs);

            RunTest("HtmlCommentWithQuote_Double",
                    tabTest: TabTest.NoTabs);
        }

        [Fact]
        public void CSharpChunkGenerator_CorrectlyGeneratesAttributes_AtDesignTime()
        {
            var expectedDesignTimePragmas = new[]
            {
                BuildLineMapping(2, 0, 549, 22, 2, 48),
                BuildLineMapping(66, 3, 692, 31, 20, 6),
                BuildLineMapping(83, 4, 788, 38, 15, 3),
                BuildLineMapping(90, 4, 887, 43, 22, 6),
                BuildLineMapping(111, 5, 987, 50, 19, 3),
                BuildLineMapping(118, 5, 1090, 55, 26, 6),
                BuildLineMapping(135, 6, 1186, 62, 15, 3),
                BuildLineMapping(146, 6, 1289, 67, 26, 6),
                BuildLineMapping(185, 7, 1407, 74, 37, 2),
                BuildLineMapping(191, 7, 1526, 79, 43, 6),
                BuildLineMapping(234, 8, 1648, 86, 41, 2),
                BuildLineMapping(240, 8, 1771, 91, 47, 6),
                BuildLineMapping(257, 9, 15, 1867, 98, 14, 18),
                BuildLineMapping(276, 9, 1995, 104, 34, 3),
                BuildLineMapping(279, 9, 2110, 109, 37, 2),
                BuildLineMapping(285, 9, 2231, 115, 43, 6),
                BuildLineMapping(309, 10, 2335, 122, 22, 6),
                BuildLineMapping(329, 11, 2435, 129, 18, 44),
                BuildLineMapping(407, 11, 2650, 134, 96, 6),
                BuildLineMapping(427, 12, 2750, 141, 18, 60),
                BuildLineMapping(521, 12, 2997, 146, 112, 6),
                BuildLineMapping(638, 13, 3194, 153, 115, 2)
            };

            RunTest("ConditionalAttributes",
                    baselineName: "ConditionalAttributes.DesignTime",
                    designTimeMode: true,
                    tabTest: TabTest.NoTabs,
                    expectedDesignTimePragmas: expectedDesignTimePragmas);
        }

        private void OpenedIf(bool withTabs)
        {
            var tabOffsetForMapping = 7;

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
                new TestSpan(SpanKind.Markup, 0, 6),
                new TestSpan(SpanKind.Markup, 6, 8),
                new TestSpan(SpanKind.Markup, 8, 14),
                new TestSpan(SpanKind.Markup, 14, 16),
                new TestSpan(SpanKind.Transition, 16, 17),
                new TestSpan(SpanKind.Code, 17, 31),
                new TestSpan(SpanKind.Markup, 31, 38),
                new TestSpan(SpanKind.Code, 38, 40),
                new TestSpan(SpanKind.Markup, 40, 47),
                new TestSpan(SpanKind.Code, 47, 47),
            },
            expectedDesignTimePragmas: new List<LineMapping>()
            {
                BuildLineMapping(17, 2, 1, 508, 22, 0, 14),
                BuildLineMapping(38, 3, 7, 582 + tabOffsetForMapping, 28, tabOffsetForMapping, 2),
                // Multiply the tab offset absolute index by 2 to account for the first mapping
                BuildLineMapping(47, 4, 7, 644 + tabOffsetForMapping * 2, 34, tabOffsetForMapping, 0)
            });
        }

        protected static LineMapping BuildLineMapping(int documentAbsoluteIndex,
                                                      int documentLineIndex,
                                                      int generatedAbsoluteIndex,
                                                      int generatedLineIndex,
                                                      int characterOffsetIndex,
                                                      int contentLength)
        {
            return BuildLineMapping(documentAbsoluteIndex,
                                    documentLineIndex,
                                    characterOffsetIndex,
                                    generatedAbsoluteIndex,
                                    generatedLineIndex,
                                    characterOffsetIndex,
                                    contentLength);
        }

        protected static LineMapping BuildLineMapping(int documentAbsoluteIndex,
                                                      int documentLineIndex,
                                                      int documentCharacterOffsetIndex,
                                                      int generatedAbsoluteIndex,
                                                      int generatedLineIndex,
                                                      int generatedCharacterOffsetIndex,
                                                      int contentLength)
        {
            return new LineMapping(
                        documentLocation: new MappingLocation(
                            new SourceLocation(documentAbsoluteIndex,
                                               documentLineIndex,
                                               documentCharacterOffsetIndex),
                            contentLength),
                        generatedLocation: new MappingLocation(
                            new SourceLocation(generatedAbsoluteIndex,
                                               generatedLineIndex,
                                               generatedCharacterOffsetIndex),
                        contentLength)
                    );
        }
    }
}
