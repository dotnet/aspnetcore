// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
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
        [InlineData("NestedHelpers")]
        [InlineData("LayoutDirective")]
        [InlineData("ConditionalAttributes")]
        [InlineData("ResolveUrl")]
        public void CSharpCodeGeneratorCorrectlyGeneratesRunTimeCode(string testType)
        {
            RunTest(testType);
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMappingsForSimpleUnspacedIf()
        {
            RunTest("SimpleUnspacedIf",
                    "SimpleUnspacedIf.DesignTime.Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
                    {
                        /* 01 */ new GeneratedCodeMapping(1, 2, 1, 15),
                        /* 02 */ new GeneratedCodeMapping(3, 13, 7, 3),
                    });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMappingsForRazorCommentsAtDesignTime()
        {
            RunTest("RazorComments", "RazorComments.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(4, 3, 3, 6),
                /* 02 */ new GeneratedCodeMapping(5, 40, 39, 22),
                /* 03 */ new GeneratedCodeMapping(6, 50, 49, 58),
                /* 04 */ new GeneratedCodeMapping(12, 3, 3, 24),
                /* 05 */ new GeneratedCodeMapping(13, 46, 46, 3),
                /* 06 */ new GeneratedCodeMapping(15, 3, 7, 1),
                /* 07 */ new GeneratedCodeMapping(15, 8, 8, 1)
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

        private void OpenedIf(bool withTabs)
        {
            int tabOffsetForMapping = 0;

            // where the test is running with tabs, the offset into the CS buffer changes for the whitespace mapping
            // with spaces we get 7xspace -> offset of 8 (column = offset+1)
            // with tabs we get tab + 3 spaces -> offset of 4 chars + 1 = 5
            if (withTabs)
            {
                tabOffsetForMapping = 3;
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
            expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 2, 1, 14),
                /* 02 */ new GeneratedCodeMapping(4, 8, 8 - tabOffsetForMapping, 2),
                /* 03 */ new GeneratedCodeMapping(5, 8, 8 - tabOffsetForMapping, 0),
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesImportStatementsAtDesignTime()
        {
            RunTest("Imports", "Imports.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 2, 1, 15),
                /* 02 */ new GeneratedCodeMapping(2, 2, 1, 32),
                /* 03 */ new GeneratedCodeMapping(3, 2, 1, 12),
                /* 04 */ new GeneratedCodeMapping(5, 30, 30, 21),
                /* 05 */ new GeneratedCodeMapping(6, 36, 36, 20),
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTime()
        {
            RunTest("FunctionsBlock",
                    "FunctionsBlock.DesignTime",
                    designTimeMode: true,
                    tabTest: TabTest.NoTabs,
                    expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 13, 13, 4),
                /* 02 */ new GeneratedCodeMapping(5, 13, 13, 104),
                /* 03 */ new GeneratedCodeMapping(12, 26, 26, 11)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTimeTabs()
        {
            RunTest("FunctionsBlock",
                    "FunctionsBlock.DesignTime" + ".Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 13, 4, 4),
                /* 02 */ new GeneratedCodeMapping(5, 13, 4, 104),
                /* 03 */ new GeneratedCodeMapping(12, 26, 14, 11)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesMinimalFunctionsBlocksAtDesignTimeTabs()
        {
            RunTest("FunctionsBlockMinimal",
                    "FunctionsBlockMinimal.DesignTime" + ".Tabs",
                    designTimeMode: true,
                    tabTest: TabTest.Tabs,
                    expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 13, 7, 55),
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesHiddenSpansWithinCode()
        {
            RunTest("HiddenSpansInCode", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>
            {
                /* 01 */ new GeneratedCodeMapping(1, 3, 3, 6),
                /* 02 */ new GeneratedCodeMapping(2, 6, 6, 5)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorGeneratesCodeWithParserErrorsInDesignTimeMode()
        {
            RunTest("ParserError", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 3, 3, 31)
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
            RunTest("Inherits", baselineName: "Inherits.Designtime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 2, 7, 5),
                /* 02 */ new GeneratedCodeMapping(3, 11, 11, 25),
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCode()
        {
            RunTest("UnfinishedExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 3, 3, 2),
                /* 02 */ new GeneratedCodeMapping(2, 2, 7, 9),
                /* 03 */ new GeneratedCodeMapping(2, 11, 11, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCodeTabs()
        {
            RunTest("UnfinishedExpressionInCode",
                    "UnfinishedExpressionInCode.Tabs",
                    tabTest: TabTest.Tabs,
                    designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 3, 3, 2),
                /* 02 */ new GeneratedCodeMapping(2, 2, 7, 9),
                /* 03 */ new GeneratedCodeMapping(2, 11, 5, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasMarkupAndExpressions()
        {
            RunTest("DesignTime",
                designTimeMode: true,
                tabTest: TabTest.NoTabs,
                expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(2, 14, 13, 36),
                /* 02 */ new GeneratedCodeMapping(3, 23, 23, 1),
                /* 03 */ new GeneratedCodeMapping(3, 28, 28, 15),
                /* 04 */ new GeneratedCodeMapping(8, 3, 7, 12),
                /* 05 */ new GeneratedCodeMapping(9, 2, 7, 4),
                /* 06 */ new GeneratedCodeMapping(9, 15, 15, 3),
                /* 07 */ new GeneratedCodeMapping(9, 26, 26, 1),
                /* 08 */ new GeneratedCodeMapping(14, 6, 7, 3),
                /* 09 */ new GeneratedCodeMapping(17, 9, 24, 7),
                /* 10 */ new GeneratedCodeMapping(17, 16, 16, 26),
                /* 11 */ new GeneratedCodeMapping(19, 19, 19, 9),
                /* 12 */ new GeneratedCodeMapping(21, 1, 1, 1)
            });
        }


        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForImplicitExpressionStartedAtEOF()
        {
            RunTest("ImplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 2, 7, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForExplicitExpressionStartedAtEOF()
        {
            RunTest("ExplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 3, 7, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForCodeBlockStartedAtEOF()
        {
            RunTest("CodeBlockAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 3, 3, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpression()
        {
            RunTest("EmptyImplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 2, 7, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCode()
        {
            RunTest("EmptyImplicitExpressionInCode", tabTest: TabTest.NoTabs, designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 3, 3, 6),
                /* 02 */ new GeneratedCodeMapping(2, 6, 7, 0),
                /* 03 */ new GeneratedCodeMapping(2, 6, 6, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCodeTabs()
        {
            RunTest("EmptyImplicitExpressionInCode",
                    "EmptyImplicitExpressionInCode.Tabs",
                    tabTest: TabTest.Tabs,
                    designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 3, 3, 6),
                /* 02 */ new GeneratedCodeMapping(2, 6, 7, 0),
                /* 03 */ new GeneratedCodeMapping(2, 6, 3, 2)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyExplicitExpression()
        {
            RunTest("EmptyExplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 3, 7, 0)
            });
        }

        [Fact]
        public void CSharpCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyCodeBlock()
        {
            RunTest("EmptyCodeBlock", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 3, 3, 0)
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

        [Fact]
        public void CSharpCodeGeneratorCorrectlyInstrumentsRazorCodeWhenInstrumentationRequested()
        {
            RunTest("Instrumented", hostConfig: host =>
            {
                host.EnableInstrumentation = true;
                host.InstrumentedSourceFilePath = String.Format("~/{0}.cshtml", host.DefaultClassName);
            });
        }

        [Fact]
        public void CSharpCodeGeneratorGeneratesUrlsCorrectlyWithCommentsAndQuotes()
        {
            RunTest("HtmlCommentWithQuote_Single",
                    tabTest: TabTest.NoTabs);

            RunTest("HtmlCommentWithQuote_Double",
                    tabTest: TabTest.NoTabs);

        }
    }
}
