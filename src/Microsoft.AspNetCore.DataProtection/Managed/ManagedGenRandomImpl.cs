// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.Managed
{
    internal unsafe sealed class ManagedGenRandomImpl : IManagedGenRandom
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        public static readonly ManagedGenRandomImpl Instance = new ManagedGenRandomImpl();

        private ManagedGenRandomImpl()
        {
        }

        public byte[] GenRandom(int numBytes)
        {
            var bytes = new byte[numBytes];
            _rng.GetBytes(bytes);
            return bytes;
        }
    }
}
