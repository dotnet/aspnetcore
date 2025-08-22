// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Antiforgery;

// Represents a binary blob (token) that contains random data.
// Useful for binary data inside a serialized stream.
[DebuggerDisplay("{DebuggerString}")]
internal sealed class BinaryBlob : IEquatable<BinaryBlob>
{
    private readonly byte[] _data;

    // Generates a new token using a specified bit length.
    public BinaryBlob(int bitLength)
        : this(bitLength, GenerateNewToken(bitLength))
    {
    }

    // Generates a token using an existing binary value.
    public BinaryBlob(int bitLength, byte[] data)
    {
        if (bitLength < 32 || bitLength % 8 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitLength));
        }
        if (data == null || data.Length != bitLength / 8)
        {
            throw new ArgumentOutOfRangeException(nameof(data));
        }

        _data = data;
    }

    public int BitLength
    {
        get
        {
            return checked(_data.Length * 8);
        }
    }

    private string DebuggerString => $"0x{Convert.ToHexStringLower(_data)}";

    public override bool Equals(object? obj)
    {
        return Equals(obj as BinaryBlob);
    }

    public bool Equals(BinaryBlob? other)
    {
        if (other == null)
        {
            return false;
        }

        Debug.Assert(_data.Length == other._data.Length);
        return AreByteArraysEqual(_data, other._data);
    }

    public byte[] GetData()
    {
        return _data;
    }

    public override int GetHashCode()
    {
        // Since data should contain uniformly-distributed entropy, the
        // first 32 bits can serve as the hash code.
        Debug.Assert(_data != null && _data.Length >= (32 / 8));
        return BitConverter.ToInt32(_data, 0);
    }

    private static byte[] GenerateNewToken(int bitLength)
    {
        var data = new byte[bitLength / 8];
        RandomNumberGenerator.Fill(data);
        return data;
    }

    // Need to mark it with NoInlining and NoOptimization attributes to ensure that the
    // operation runs in constant time.
    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static bool AreByteArraysEqual(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        var areEqual = true;
        for (var i = 0; i < a.Length; i++)
        {
            areEqual &= (a[i] == b[i]);
        }
        return areEqual;
    }
}
