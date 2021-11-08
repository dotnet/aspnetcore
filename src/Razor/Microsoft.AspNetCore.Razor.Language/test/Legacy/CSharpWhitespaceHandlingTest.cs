// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpWhitespaceHandlingTest : ParserTestBase
{
    [Fact]
    public void StmtBlockDoesNotAcceptTrailingNewlineIfTheyAreSignificantToAncestor()
    {
        ParseDocumentTest("@{@: @if (true) { }" + Environment.NewLine + "}");
    }
}
