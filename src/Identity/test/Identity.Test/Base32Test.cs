// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Identity.Test;

public class Base32Test
{
    [Fact]
    public void ConversionTest()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6 };
        Assert.Equal<byte>(data, Base32.FromBase32(Base32.ToBase32(data)));

        int length;
        do
        {
            length = GetRandomByteArray(1)[0];
        } while (length % 5 == 0);
        data = GetRandomByteArray(length);
        Assert.Equal<byte>(data, Base32.FromBase32(Base32.ToBase32(data)));

        length = (int)(GetRandomByteArray(1)[0]) * 5;
        data = GetRandomByteArray(length);
        Assert.Equal<byte>(data, Base32.FromBase32(Base32.ToBase32(data)));
    }

    [Fact]
    public void GenerateBase32IsValid()
    {
        var output = Base32.FromBase32(Base32.GenerateBase32());
        Assert.Equal(20, output.Length);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("MY======", "66")]
    [InlineData("MZXQ====", "666F")]
    [InlineData("MZXW6===", "666F6F")]
    [InlineData("MZXW6YQ=", "666F6F62")]
    [InlineData("MZXW6YTB", "666F6F6261")]
    [InlineData("MZXW6YTBOI======", "666F6F626172")]
    [InlineData("mzxw6ytboi", "666F6F626172")]
    [InlineData("JBSWY3DPEHPK3PXP", "48656C6C6F21DEADBEEF")]
    [InlineData("77777777", "FFFFFFFFFF")]
    [InlineData("AAAAAAAA", "0000000000")]
    [InlineData("A", "")]
    [InlineData("MZ!", "66")]
    public void FromBase32_DecodesExpectedBytes(string input, string expectedHex)
    {
        Assert.Equal(Convert.FromHexString(expectedHex), Base32.FromBase32(input));
    }

    [Theory]
    [InlineData("M!")]
    [InlineData("M1")]
    [InlineData("M8")]
    [InlineData("M0")]
    [InlineData("M@")]
    [InlineData("M[")]
    [InlineData("M`")]
    [InlineData("M{")]
    [InlineData("M ")]
    [InlineData("M\u00e9")]
    [InlineData("M\u212a")]
    [InlineData("M\uff21")]
    [InlineData("M=M=")]
    public void FromBase32_ThrowsForInvalidCharacters(string input)
    {
        Assert.Throws<FormatException>(() => Base32.FromBase32(input));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("======", "")]
    [InlineData("MY======", "66")]
    [InlineData("MZXW6YTB", "666F6F6261")]
    [InlineData("MZXW6YTBOI======", "666F6F626172")]
    [InlineData("mzxw6ytboi", "666F6F626172")]
    [InlineData("A", "")]
    [InlineData("MZ!", "66")]
    public void TryFromBase32_DecodesExpectedBytes(string input, string expectedHex)
    {
        var destination = new byte[16];

        Assert.True(Base32.TryFromBase32(input, destination, out var bytesWritten));

        Assert.Equal(Convert.FromHexString(expectedHex), destination.AsSpan(0, bytesWritten).ToArray());
    }

    [Fact]
    public void TryFromBase32_RoundTripsRandomData()
    {
        for (var length = 0; length <= 64; length++)
        {
            var data = GetRandomByteArray(length);
            var destination = new byte[length];

            Assert.True(Base32.TryFromBase32(Base32.ToBase32(data), destination, out var bytesWritten));

            Assert.Equal(length, bytesWritten);
            Assert.Equal<byte>(data, destination);
        }
    }

    [Theory]
    [InlineData("M!")]
    [InlineData("M1")]
    [InlineData("M8")]
    [InlineData("M@")]
    [InlineData("M ")]
    [InlineData("M\u00e9")]
    [InlineData("M=M=")]
    [InlineData("=MZXW6YTB")]
    [InlineData("MZXW6YT!")]
    public void TryFromBase32_ReturnsFalseForInvalidInput(string input)
    {
        var destination = new byte[16];
        destination.AsSpan().Fill(0xAA);

        Assert.False(Base32.TryFromBase32(input, destination, out var bytesWritten));

        Assert.Equal(0, bytesWritten);
        // Partially decoded bytes are cleared; untouched bytes keep their original value.
        Assert.All(destination, b => Assert.True(b is 0 or 0xAA));
    }

    [Theory]
    [InlineData("MY======", 0)]
    [InlineData("MZXW6YTB", 4)]
    [InlineData("MZXW6YTBOI======", 5)]
    public void TryFromBase32_ReturnsFalseWhenDestinationTooSmall(string input, int destinationLength)
    {
        var destination = new byte[destinationLength];

        Assert.False(Base32.TryFromBase32(input, destination, out var bytesWritten));

        Assert.Equal(0, bytesWritten);
        Assert.All(destination, b => Assert.Equal(0, b));
    }

    [Fact]
    public void TryFromBase32_AcceptsLargerDestination()
    {
        var destination = new byte[10];
        destination.AsSpan().Fill(0xAA);

        Assert.True(Base32.TryFromBase32("MZXW6===", destination, out var bytesWritten));

        Assert.Equal(3, bytesWritten);
        Assert.Equal(new byte[] { 0x66, 0x6F, 0x6F, 0xAA }, destination.AsSpan(0, 4).ToArray());
    }

    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    private static byte[] GetRandomByteArray(int length)
    {
        byte[] bytes = new byte[length];
        _rng.GetBytes(bytes);
        return bytes;
    }
}
