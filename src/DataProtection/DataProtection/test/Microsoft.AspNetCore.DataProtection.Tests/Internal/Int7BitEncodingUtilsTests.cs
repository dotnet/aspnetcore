// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.Tests.Internal;

public class Int7BitEncodingUtilsTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(0b0_1111111, 1)]
    [InlineData(0b1_0000000, 2)]
    [InlineData(0b1111111_1111111, 2)]
    [InlineData(0b1_0000000_0000000, 3)]
    [InlineData(0b1111111_1111111_1111111, 3)]
    [InlineData(0b1_0000000_0000000_0000000, 4)]
    [InlineData(0b1111111_1111111_1111111_1111111, 4)]
    [InlineData(0b1_0000000_0000000_0000000_0000000, 5)]
    [InlineData(uint.MaxValue, 5)]
    public void Measure7BitEncodedUIntLength_ReturnsExceptedLength(uint value, int expectedSize)
    {
        var actualSize = value.Measure7BitEncodedUIntLength();
        Assert.Equal(expectedSize, actualSize);
    }
}
