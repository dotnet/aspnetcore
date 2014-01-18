// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class VBRazorCodeGeneratorTest : RazorCodeGeneratorTest<VBRazorCodeLanguage>
    {
        private const string TestPhysicalPath = @"C:\Bar.vbhtml";
        private const string TestVirtualPath = "~/Foo/Bar.vbhtml";

        protected override string FileExtension
        {
            get { return "vbhtml"; }
        }

        protected override string LanguageName
        {
            get { return "VB"; }
        }

        protected override string BaselineExtension
        {
            get { return "vb"; }
        }

        [Fact]
        public void ConstructorRequiresNonNullClassName()
        {
            Assert.ThrowsArgumentNullOrEmptyString(() => new VBRazorCodeGenerator(null, TestRootNamespaceName, TestPhysicalPath, CreateHost()), "className");
        }

        [Fact]
        public void ConstructorRequiresNonEmptyClassName()
        {
            Assert.ThrowsArgumentNullOrEmptyString(() => new VBRazorCodeGenerator(String.Empty, TestRootNamespaceName, TestPhysicalPath, CreateHost()), "className");
        }

        [Fact]
        public void ConstructorRequiresNonNullRootNamespaceName()
        {
            Assert.ThrowsArgumentNull(() => new VBRazorCodeGenerator("Foo", null, TestPhysicalPath, CreateHost()), "rootNamespaceName");
        }

        [Fact]
        public void ConstructorAllowsEmptyRootNamespaceName()
        {
            new VBRazorCodeGenerator("Foo", String.Empty, TestPhysicalPath, CreateHost());
        }

        [Fact]
        public void ConstructorRequiresNonNullHost()
        {
            Assert.ThrowsArgumentNull(() => new VBRazorCodeGenerator("Foo", TestRootNamespaceName, TestPhysicalPath, null), "host");
        }

        [Theory]
        [InlineData("NestedCodeBlocks")]
        [InlineData("NestedCodeBlocks")]
        [InlineData("CodeBlock")]
        [InlineData("ExplicitExpression")]
        [InlineData("MarkupInCodeBlock")]
        [InlineData("Blocks")]
        [InlineData("ImplicitExpression")]
        [InlineData("Imports")]
        [InlineData("ExpressionsInCode")]
        [InlineData("FunctionsBlock")]
        [InlineData("Options")]
        [InlineData("Templates")]
        [InlineData("RazorComments")]
        [InlineData("Sections")]
        [InlineData("EmptySection")] // this scenario causes a crash in Razor V2.0
        [InlineData("Helpers")]
        [InlineData("HelpersMissingCloseParen")]
        [InlineData("HelpersMissingOpenParen")]
        [InlineData("NestedHelpers")]
        [InlineData("LayoutDirective")]
        [InlineData("ConditionalAttributes")]
        [InlineData("ResolveUrl")]
        public void VBCodeGeneratorCorrectlyGeneratesRunTimeCode(string testName)
        {
            RunTest(testName);
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesMappingsForRazorCommentsAtDesignTime()
        {
            // (4, 6) -> (?, 6) [6]
            // ( 5, 40) -> (?, 39) [2]
            // ( 8, 6) -> (?, 6) [33]
            // ( 9, 46) -> (?, 46) [3]
            // ( 12, 3) -> (?, 7) [3]
            // ( 12, 8) -> (?, 8) [1]
            RunTest("RazorComments", "RazorComments.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(4, 6, 6, 6),
                /* 02 */ new GeneratedCodeMapping(5, 40, 39, 2),
                /* 03 */ new GeneratedCodeMapping(8, 6, 6, 33),
                /* 04 */ new GeneratedCodeMapping(9, 46, 46, 3),
                /* 05 */ new GeneratedCodeMapping(12, 3, 7, 1),
                /* 06 */ new GeneratedCodeMapping(12, 8, 8, 1)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesHelperMissingNameAtDesignTime()
        {
            RunTest("HelpersMissingName", designTimeMode: true);
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesImportStatementsAtDesignTimeButCannotWrapPragmasAroundImportStatement()
        {
            RunTest("Imports", "Imports.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 2, 1, 19),
                /* 02 */ new GeneratedCodeMapping(2, 2, 1, 36),
                /* 03 */ new GeneratedCodeMapping(3, 2, 1, 16),
                /* 04 */ new GeneratedCodeMapping(5, 30, 30, 22),
                /* 05 */ new GeneratedCodeMapping(6, 36, 36, 21),
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTime()
        {
            RunTest("FunctionsBlock", "FunctionsBlock.DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 11, 11, 4),
                /* 02 */ new GeneratedCodeMapping(5, 11, 11, 129),
                /* 03 */ new GeneratedCodeMapping(12, 26, 26, 11)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesFunctionsBlocksAtDesignTimeTabs()
        {
            RunTest("FunctionsBlock", "FunctionsBlock.DesignTime.Tabs", designTimeMode: true, tabTest: TabTest.Tabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 11, 5, 4),
                /* 02 */ new GeneratedCodeMapping(5, 11, 5, 129),
                /* 03 */ new GeneratedCodeMapping(12, 26, 14, 11)
            });
        }

        [Fact]
        public void VBCodeGeneratorGeneratesCodeWithParserErrorsInDesignTimeMode()
        {
            RunTest("ParserError", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 6, 6, 16)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesInheritsAtRuntime()
        {
            RunTest("Inherits", baselineName: "Inherits.Runtime");
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesInheritsAtDesigntime()
        {
            RunTest("Inherits", baselineName: "Inherits.Designtime", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 11, 25, 27)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasForUnfinishedExpressionsInCode()
        {
            RunTest("UnfinishedExpressionInCode", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 6, 6, 2),
                /* 02 */ new GeneratedCodeMapping(2, 2, 7, 9),
                /* 03 */ new GeneratedCodeMapping(2, 11, 11, 2)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasMarkupAndExpressions()
        {
            RunTest("DesignTime", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(2, 14, 13, 17),
                /* 02 */ new GeneratedCodeMapping(3, 20, 20, 1),
                /* 03 */ new GeneratedCodeMapping(3, 25, 25, 20),
                /* 04 */ new GeneratedCodeMapping(8, 3, 7, 12),
                /* 05 */ new GeneratedCodeMapping(9, 2, 7, 4),
                /* 06 */ new GeneratedCodeMapping(9, 16, 16, 3),
                /* 07 */ new GeneratedCodeMapping(9, 27, 27, 1),
                /* 08 */ new GeneratedCodeMapping(14, 6, 7, 3),
                /* 09 */ new GeneratedCodeMapping(17, 9, 24, 5),
                /* 10 */ new GeneratedCodeMapping(17, 14, 14, 28),
                /* 11 */ new GeneratedCodeMapping(19, 20, 20, 14)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasForImplicitExpressionStartedAtEOF()
        {
            RunTest("ImplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 2, 7, 0)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasForExplicitExpressionStartedAtEOF()
        {
            RunTest("ExplicitExpressionAtEOF", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 3, 7, 0)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasForCodeBlockStartedAtEOF()
        {
            RunTest("CodeBlockAtEOF", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 6, 6, 0)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpression()
        {
            RunTest("EmptyImplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 2, 7, 0)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyImplicitExpressionInCode()
        {
            RunTest("EmptyImplicitExpressionInCode", designTimeMode: true, tabTest: TabTest.NoTabs, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(1, 6, 6, 6),
                /* 02 */ new GeneratedCodeMapping(2, 6, 7, 0),
                /* 03 */ new GeneratedCodeMapping(2, 6, 6, 2)
            });
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyGeneratesDesignTimePragmasForEmptyExplicitExpression()
        {
            RunTest("EmptyExplicitExpression", designTimeMode: true, expectedDesignTimePragmas: new List<GeneratedCodeMapping>()
            {
                /* 01 */ new GeneratedCodeMapping(3, 3, 7, 0)
            });
        }

        [Fact]
        public void VBCodeGeneratorDoesNotRenderLinePragmasIfGenerateLinePragmasIsSetToFalse()
        {
            RunTest("NoLinePragmas", generatePragmas: false);
        }

        [Fact]
        public void VBCodeGeneratorRendersHelpersBlockCorrectlyWhenInstanceHelperRequested()
        {
            RunTest("Helpers", baselineName: "Helpers.Instance", hostConfig: h => h.StaticHelpers = false);
        }

        [Fact]
        public void VBCodeGeneratorCorrectlyInstrumentsRazorCodeWhenInstrumentationRequested()
        {
            RunTest("Instrumented", hostConfig: host =>
            {
                host.EnableInstrumentation = true;
                host.InstrumentedSourceFilePath = String.Format("~/{0}.vbhtml", host.DefaultClassName);
            });
        }
    }
}
