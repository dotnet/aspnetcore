// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser
{
    public class CSharpLanguageCharacteristicsTest
    {
        [Fact]
        public void GetSymbolSample_RightShiftAssign_ReturnsCorrectSymbol()
        {
            // Arrange & Act
            var symbol = CSharpLanguageCharacteristics.GetSymbolSample(CSharpSymbolType.RightShiftAssign);

            // Assert
            Assert.Equal(">>=", symbol);
        }
    }
}
