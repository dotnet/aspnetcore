// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.DataProtection;

public unsafe class SecretTests
{
    [Fact]
    public void Ctor_ArraySegment_Default_Throws()
    {
        // Act & assert
        ExceptionAssert.ThrowsArgument(
            testCode: () => new Secret(default(ArraySegment<byte>)),
            paramName: "array",
            exceptionMessage: null);
    }

    [Fact]
    public void Ctor_ArraySegment_Success()
    {
        // Arrange
        var input = new ArraySegment<byte>(new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 }, 1, 3);

        // Act
        var secret = new Secret(input);
        input.Array[2] = 0xFF; // mutate original array - secret shouldn't be modified

        // Assert - length
        Assert.Equal(3, secret.Length);

        // Assert - managed buffer
        var outputSegment = new ArraySegment<byte>(new byte[7], 2, 3);
        secret.WriteSecretIntoBuffer(outputSegment);
        Assert.Equal(new byte[] { 0x20, 0x30, 0x40 }, outputSegment.AsStandaloneArray());

        // Assert - unmanaged buffer
        var outputBuffer = new byte[3];
        fixed (byte* pOutputBuffer = outputBuffer)
        {
            secret.WriteSecretIntoBuffer(pOutputBuffer, 3);
        }
        Assert.Equal(new byte[] { 0x20, 0x30, 0x40 }, outputBuffer);
    }

    [Fact]
    public void Ctor_Buffer_Success()
    {
        // Arrange
        var input = new byte[] { 0x20, 0x30, 0x40 };

        // Act
        var secret = new Secret(input);
        input[1] = 0xFF; // mutate original array - secret shouldn't be modified

        // Assert - length
        Assert.Equal(3, secret.Length);

        // Assert - managed buffer
        var outputSegment = new ArraySegment<byte>(new byte[7], 2, 3);
        secret.WriteSecretIntoBuffer(outputSegment);
        Assert.Equal(new byte[] { 0x20, 0x30, 0x40 }, outputSegment.AsStandaloneArray());

        // Assert - unmanaged buffer
        var outputBuffer = new byte[3];
        fixed (byte* pOutputBuffer = outputBuffer)
        {
            secret.WriteSecretIntoBuffer(pOutputBuffer, 3);
        }
        Assert.Equal(new byte[] { 0x20, 0x30, 0x40 }, outputBuffer);
    }

    [Fact]
    public void Ctor_Buffer_ZeroLength_Success()
    {
        // Act
        var secret = new Secret(new byte[0]);

        // Assert - none of these methods should throw
        Assert.Equal(0, secret.Length);
        secret.WriteSecretIntoBuffer(new ArraySegment<byte>(new byte[0]));
        byte dummy;
        secret.WriteSecretIntoBuffer(&dummy, 0);
    }

    [Fact]
    public void Ctor_Pointer_WithNullPointer_ThrowsArgumentNull()
    {
        // Act & assert
        ExceptionAssert.ThrowsArgumentNull(
            testCode: () => new Secret(null, 0),
            paramName: "secret");
    }

    [Fact]
    public void Ctor_Pointer_WithNegativeLength_ThrowsArgumentOutOfRange()
    {
        // Act & assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            testCode: () =>
            {
                byte dummy;
                new Secret(&dummy, -1);
            },
            paramName: "secretLength",
            exceptionMessage: Resources.Common_ValueMustBeNonNegative);
    }

    [Fact]
    public void Ctor_Pointer_ZeroLength_Success()
    {
        // Arrange
        byte input;

        // Act
        var secret = new Secret(&input, 0);

        // Assert - none of these methods should throw
        Assert.Equal(0, secret.Length);
        secret.WriteSecretIntoBuffer(new ArraySegment<byte>(new byte[0]));
        byte dummy;
        secret.WriteSecretIntoBuffer(&dummy, 0);
    }

    [Fact]
    public void Ctor_Pointer_Success()
    {
        // Arrange
        byte* input = stackalloc byte[3];
        input[0] = 0x20;
        input[1] = 0x30;
        input[2] = 0x40;

        // Act
        var secret = new Secret(input, 3);
        input[1] = 0xFF; // mutate original buffer - secret shouldn't be modified

        // Assert - length
        Assert.Equal(3, secret.Length);

        // Assert - managed buffer
        var outputSegment = new ArraySegment<byte>(new byte[7], 2, 3);
        secret.WriteSecretIntoBuffer(outputSegment);
        Assert.Equal(new byte[] { 0x20, 0x30, 0x40 }, outputSegment.AsStandaloneArray());

        // Assert - unmanaged buffer
        var outputBuffer = new byte[3];
        fixed (byte* pOutputBuffer = outputBuffer)
        {
            secret.WriteSecretIntoBuffer(pOutputBuffer, 3);
        }
        Assert.Equal(new byte[] { 0x20, 0x30, 0x40 }, outputBuffer);
    }

    [Fact]
    public void Random_ZeroLength_Success()
    {
        // Act
        var secret = Secret.Random(0);

        // Assert
        Assert.Equal(0, secret.Length);
    }

    [Fact]
    public void Random_LengthIsMultipleOf16_Success()
    {
        // Act
        var secret = Secret.Random(32);

        // Assert
        Assert.Equal(32, secret.Length);
        Guid* pGuids = stackalloc Guid[2];
        secret.WriteSecretIntoBuffer((byte*)pGuids, 32);
        Assert.NotEqual(Guid.Empty, pGuids[0]);
        Assert.NotEqual(Guid.Empty, pGuids[1]);
        Assert.NotEqual(pGuids[0], pGuids[1]);
    }

    [Fact]
    public void Random_LengthIsNotMultipleOf16_Success()
    {
        // Act
        var secret = Secret.Random(31);

        // Assert
        Assert.Equal(31, secret.Length);
        Guid* pGuids = stackalloc Guid[2];
        secret.WriteSecretIntoBuffer((byte*)pGuids, 31);
        Assert.NotEqual(Guid.Empty, pGuids[0]);
        Assert.NotEqual(Guid.Empty, pGuids[1]);
        Assert.NotEqual(pGuids[0], pGuids[1]);
        Assert.Equal(0, ((byte*)pGuids)[31]); // last byte shouldn't have been overwritten
    }

    [Fact]
    public void WriteSecretIntoBuffer_ArraySegment_IncorrectlySizedBuffer_Throws()
    {
        // Arrange
        var secret = Secret.Random(16);

        // Act & assert
        ExceptionAssert.ThrowsArgument(
            testCode: () => secret.WriteSecretIntoBuffer(new ArraySegment<byte>(new byte[100])),
            paramName: "buffer",
            exceptionMessage: Resources.FormatCommon_BufferIncorrectlySized(100, 16));
    }

    [Fact]
    public void WriteSecretIntoBuffer_ArraySegment_Disposed_Throws()
    {
        // Arrange
        var secret = Secret.Random(16);
        secret.Dispose();

        // Act & assert
        Assert.Throws<ObjectDisposedException>(
            testCode: () => secret.WriteSecretIntoBuffer(new ArraySegment<byte>(new byte[16])));
    }

    [Fact]
    public void WriteSecretIntoBuffer_Pointer_NullBuffer_Throws()
    {
        // Arrange
        var secret = Secret.Random(16);

        // Act & assert
        ExceptionAssert.ThrowsArgumentNull(
            testCode: () => secret.WriteSecretIntoBuffer(null, 100),
            paramName: "buffer");
    }

    [Fact]
    public void WriteSecretIntoBuffer_Pointer_IncorrectlySizedBuffer_Throws()
    {
        // Arrange
        var secret = Secret.Random(16);

        // Act & assert
        ExceptionAssert.ThrowsArgument(
            testCode: () =>
            {
                byte* pBuffer = stackalloc byte[100];
                secret.WriteSecretIntoBuffer(pBuffer, 100);
            },
            paramName: "bufferLength",
            exceptionMessage: Resources.FormatCommon_BufferIncorrectlySized(100, 16));
    }

    [Fact]
    public void WriteSecretIntoBuffer_Pointer_Disposed_Throws()
    {
        // Arrange
        var secret = Secret.Random(16);
        secret.Dispose();

        // Act & assert
        Assert.Throws<ObjectDisposedException>(
            testCode: () =>
            {
                byte* pBuffer = stackalloc byte[16];
                secret.WriteSecretIntoBuffer(pBuffer, 16);
            });
    }
}
