// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.DataProtection.Managed;

namespace Microsoft.AspNetCore.DataProtection
{
    internal unsafe class SequentialGenRandom : IBCryptGenRandom, IManagedGenRandom
    {
        private byte _value;

        public byte[] GenRandom(int numBytes)
        {
            byte[] bytes = new byte[numBytes];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = _value++;
            }
            return bytes;
        }

        public void GenRandom(byte* pbBuffer, uint cbBuffer)
        {
            for (uint i = 0; i < cbBuffer; i++)
            {
                pbBuffer[i] = _value++;
            }
        }
    }
}
