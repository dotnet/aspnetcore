// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.Managed;

internal sealed unsafe class ManagedGenRandomImpl : IManagedGenRandom
{
#if NETSTANDARD2_0 || NETFRAMEWORK
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
#endif
    public static readonly ManagedGenRandomImpl Instance = new ManagedGenRandomImpl();

    private ManagedGenRandomImpl()
    {
    }

#if NET10_0_OR_GREATER
    public void GenRandom(Span<byte> target) => RandomNumberGenerator.Fill(target);
#endif

    public byte[] GenRandom(int numBytes)
    {
        var bytes = new byte[numBytes];
#if NETSTANDARD2_0 || NETFRAMEWORK
        _rng.GetBytes(bytes);
#else
        RandomNumberGenerator.Fill(bytes);
#endif
        return bytes;
    }
}
