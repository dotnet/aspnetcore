// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.Cng;

namespace Microsoft.AspNet.Security.DataProtection.Test.Cng
{
    internal unsafe class SequentialGenRandom : IBCryptGenRandom
    {
        public void GenRandom(byte* pbBuffer, uint cbBuffer)
        {
            for (uint i = 0; i < cbBuffer; i++)
            {
                pbBuffer[i] = (byte)i;
            }
        }
    }
}
