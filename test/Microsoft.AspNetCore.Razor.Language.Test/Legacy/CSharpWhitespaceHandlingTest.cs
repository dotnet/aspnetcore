// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpWhitespaceHandlingTest : CsHtmlMarkupParserTestBase
    {
        public CSharpWhitespaceHandlingTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void StatementBlockDoesNotAcceptTrailingNewlineIfNewlinesAreSignificantToAncestor()
        {
            ParseBlockTest("@: @if (true) { }" + Environment.NewLine + "}");
        }
    }
}
