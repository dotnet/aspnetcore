// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpVerbatimBlockTest : ParserTestBase
    {
        private const string TestExtraKeyword = "model";

        [Fact]
        public void VerbatimBlock()
        {
            ParseDocumentTest("@{ foo(); }");
        }

        [Fact]
        public void InnerImplicitExprWithOnlySingleAtOutputsZeroLengthCodeSpan()
        {
            ParseDocumentTest("@{@}");
        }

        [Fact]
        public void InnerImplicitExprDoesNotAcceptDotAfterAt()
        {
            ParseDocumentTest("@{@.}");
        }

        [Fact]
        public void InnerImplicitExprWithOnlySingleAtAcceptsSingleSpaceOrNewlineAtDesignTime()
        {
            ParseDocumentTest("@{" + Environment.NewLine + "    @" + Environment.NewLine + "}", designTime: true);
        }

        [Fact]
        public void InnerImplicitExprDoesNotAcceptTrailingNewlineInRunTimeMode()
        {
            ParseDocumentTest("@{@foo." + Environment.NewLine + "}");
        }

        [Fact]
        public void InnerImplicitExprAcceptsTrailingNewlineInDesignTimeMode()
        {
            ParseDocumentTest("@{@foo." + Environment.NewLine + "}", designTime: true);
        }
    }
}
