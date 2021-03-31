// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.Managed
{
    internal unsafe sealed class ManagedGenRandomImpl : IManagedGenRandom
    {
#if NETSTANDARD2_0 || NET461
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
#endif
        public static readonly ManagedGenRandomImpl Instance = new ManagedGenRandomImpl();

        private ManagedGenRandomImpl()
        {
        }

        public byte[] GenRandom(int numBytes)
        {
            var bytes = new byte[numBytes];
#if NETSTANDARD2_0 || NET461
            _rng.GetBytes(bytes);
#else
            RandomNumberGenerator.Fill(bytes);
#endif
            return bytes;
        }
    }
}
