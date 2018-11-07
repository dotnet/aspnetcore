// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpVerbatimBlockTest : CsHtmlCodeParserTestBase
    {
        private const string TestExtraKeyword = "model";

        [Fact]
        public void VerbatimBlock()
        {
            ParseBlockTest("@{ foo(); }");
        }

        [Fact]
        public void InnerImplicitExprWithOnlySingleAtOutputsZeroLengthCodeSpan()
        {
            ParseBlockTest("{@}");
        }

        [Fact]
        public void InnerImplicitExprDoesNotAcceptDotAfterAt()
        {
            ParseBlockTest("{@.}");
        }

        [Fact]
        public void InnerImplicitExprWithOnlySingleAtAcceptsSingleSpaceOrNewlineAtDesignTime()
        {
            ParseBlockTest("{" + Environment.NewLine + "    @" + Environment.NewLine + "}", designTime: true);
        }

        [Fact]
        public void InnerImplicitExprDoesNotAcceptTrailingNewlineInRunTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine + "}");
        }

        [Fact]
        public void InnerImplicitExprAcceptsTrailingNewlineInDesignTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine + "}", designTime: true);
        }
    }
}
