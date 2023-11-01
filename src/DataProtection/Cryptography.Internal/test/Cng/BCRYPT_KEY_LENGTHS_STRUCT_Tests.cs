// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cryptography.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography.Cng;

public class BCRYPT_KEY_LENGTHS_STRUCT_Tests
{
    [Theory]
    [InlineData(128, 128, 0, 128)]
    [InlineData(128, 256, 64, 128)]
    [InlineData(128, 256, 64, 192)]
    [InlineData(128, 256, 64, 256)]
    public void EnsureValidKeyLength_SuccessCases(int minLength, int maxLength, int increment, int testValue)
    {
        // Arrange
        var keyLengthsStruct = new BCRYPT_KEY_LENGTHS_STRUCT
        {
            dwMinLength = (uint)minLength,
            dwMaxLength = (uint)maxLength,
            dwIncrement = (uint)increment
        };

        // Act
        keyLengthsStruct.EnsureValidKeyLength((uint)testValue);

        // Assert
        // Nothing to do - if we got this far without throwing, success!
    }

    [Theory]
    [InlineData(128, 128, 0, 192)]
    [InlineData(128, 256, 64, 64)]
    [InlineData(128, 256, 64, 512)]
    [InlineData(128, 256, 64, 160)]
    [InlineData(128, 256, 64, 129)]
    public void EnsureValidKeyLength_FailureCases(int minLength, int maxLength, int increment, int testValue)
    {
        // Arrange
        var keyLengthsStruct = new BCRYPT_KEY_LENGTHS_STRUCT
        {
            dwMinLength = (uint)minLength,
            dwMaxLength = (uint)maxLength,
            dwIncrement = (uint)increment
        };

        // Act & assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () => keyLengthsStruct.EnsureValidKeyLength((uint)testValue),
            paramName: "keyLengthInBits",
            exceptionMessage: Resources.FormatBCRYPT_KEY_LENGTHS_STRUCT_InvalidKeyLength(testValue, minLength, maxLength, increment));
    }
}
