// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpLanguageCharacteristicsTest
{
    [Fact]
    public void GetSample_RightShiftAssign_ReturnsCorrectToken()
    {
        // Arrange & Act
        var token = CSharpLanguageCharacteristics.Instance.GetSample(SyntaxKind.RightShiftAssign);

        // Assert
        Assert.Equal(">>=", token);
    }
}
