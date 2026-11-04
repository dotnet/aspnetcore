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

    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    private static byte[] GetRandomByteArray(int length)
    {
        byte[] bytes = new byte[length];
        _rng.GetBytes(bytes);
        return bytes;
    }
}
