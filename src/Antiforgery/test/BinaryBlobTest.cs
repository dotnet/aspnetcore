// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class BinaryBlobTest
{
    [Fact]
    public void Ctor_BitLength()
    {
        // Act
        var blob = new BinaryBlob(bitLength: 64);
        var data = blob.GetData();

        // Assert
        Assert.Equal(64, blob.BitLength);
        Assert.Equal(64 / 8, data.Length);
        Assert.NotEqual(new byte[64 / 8], data); // should not be a zero-filled array
    }

    [Theory]
    [InlineData(24)]
    [InlineData(33)]
    public void Ctor_BitLength_Bad(int bitLength)
    {
        // Act & assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryBlob(bitLength));
        Assert.Equal("bitLength", ex.ParamName);
    }

    [Fact]
    public void Ctor_BitLength_ProducesDifferentValues()
    {
        // Act
        var blobA = new BinaryBlob(bitLength: 64);
        var blobB = new BinaryBlob(bitLength: 64);

        // Assert
        Assert.NotEqual(blobA.GetData(), blobB.GetData());
    }

    [Fact]
    public void Ctor_Data()
    {
        // Arrange
        var expectedData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var blob = new BinaryBlob(32, expectedData);

        // Assert
        Assert.Equal(32, blob.BitLength);
        Assert.Equal(expectedData, blob.GetData());
    }

    [Theory]
    [InlineData((object[]?)null)]
    [InlineData(new byte[] { 0x01, 0x02, 0x03 })]
    public void Ctor_Data_Bad(byte[]? data)
    {
        // Act & assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryBlob(32, data!));
        Assert.Equal("data", ex.ParamName);
    }

    [Fact]
    public void Equals_DifferentData_ReturnsFalse()
    {
        // Arrange
        object blobA = new BinaryBlob(32, new byte[] { 0x01, 0x02, 0x03, 0x04 });
        object blobB = new BinaryBlob(32, new byte[] { 0x04, 0x03, 0x02, 0x01 });

        // Act & assert
        Assert.NotEqual(blobA, blobB);
    }

    [Fact]
    public void Equals_NotABlob_ReturnsFalse()
    {
        // Arrange
        object blobA = new BinaryBlob(32);
        object blobB = "hello";

        // Act & assert
        Assert.NotEqual(blobA, blobB);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        object blobA = new BinaryBlob(32);
        object? blobB = null;

        // Act & assert
        Assert.NotEqual(blobA, blobB);
    }

    [Fact]
    public void Equals_SameData_ReturnsTrue()
    {
        // Arrange
        object blobA = new BinaryBlob(32, new byte[] { 0x01, 0x02, 0x03, 0x04 });
        object blobB = new BinaryBlob(32, new byte[] { 0x01, 0x02, 0x03, 0x04 });

        // Act & assert
        Assert.Equal(blobA, blobB);
    }

    [Fact]
    public void GetHashCodeTest()
    {
        // Arrange
        var blobData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var expectedHashCode = BitConverter.ToInt32(blobData, 0);

        var blob = new BinaryBlob(32, blobData);

        // Act
        var actualHashCode = blob.GetHashCode();

        // Assert
        Assert.Equal(expectedHashCode, actualHashCode);
    }
}
