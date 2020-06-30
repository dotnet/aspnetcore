// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
