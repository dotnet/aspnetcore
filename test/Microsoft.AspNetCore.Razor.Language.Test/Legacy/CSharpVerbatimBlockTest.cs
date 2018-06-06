// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpVerbatimBlockTest : CsHtmlCodeParserTestBase
    {
        private const string TestExtraKeyword = "model";

        public CSharpVerbatimBlockTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void VerbatimBlock()
        {
            ParseBlockTest("@{ foo(); }");
        }

        [Fact]
        public void InnerImplicitExpressionWithOnlySingleAtOutputsZeroLengthCodeSpan()
        {
            ParseBlockTest("{@}");
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptDotAfterAt()
        {
            ParseBlockTest("{@.}");
        }

        [Fact]
        public void InnerImplicitExpressionWithOnlySingleAtAcceptsSingleSpaceOrNewlineAtDesignTime()
        {
            ParseBlockTest("{" + Environment.NewLine + "    @" + Environment.NewLine + "}", designTime: true);
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptTrailingNewlineInRunTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine + "}");
        }

        [Fact]
        public void InnerImplicitExpressionAcceptsTrailingNewlineInDesignTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine + "}", designTime: true);
        }
    }
}
