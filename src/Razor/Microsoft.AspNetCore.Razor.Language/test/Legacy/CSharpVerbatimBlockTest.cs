// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpVerbatimBlockTest : ParserTestBase
{
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
