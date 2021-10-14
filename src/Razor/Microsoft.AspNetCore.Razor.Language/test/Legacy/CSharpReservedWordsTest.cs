// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpReservedWordsTest : ParserTestBase
    {
        [Fact]
        public void ReservedWord()
        {
            ParseDocumentTest("@namespace");
        }

        [Fact]
        private void ReservedWordIsCaseSensitive()
        {
            ParseDocumentTest("@NameSpace");
        }
    }
}
