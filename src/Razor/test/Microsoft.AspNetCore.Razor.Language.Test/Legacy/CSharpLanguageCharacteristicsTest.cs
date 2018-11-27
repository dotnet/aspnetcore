// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpLanguageCharacteristicsTest
    {
        [Fact]
        public void GetSample_RightShiftAssign_ReturnsCorrectSymbol()
        {
            // Arrange & Act
            var symbol = CSharpLanguageCharacteristics.Instance.GetSample(CSharpSymbolType.RightShiftAssign);

            // Assert
            Assert.Equal(">>=", symbol);
        }
    }
}
