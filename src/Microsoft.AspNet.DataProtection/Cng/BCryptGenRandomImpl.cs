// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography.Cng;

namespace Microsoft.AspNet.DataProtection.Cng
{
    internal unsafe sealed class BCryptGenRandomImpl : IBCryptGenRandom
    {
        public static readonly BCryptGenRandomImpl Instance = new BCryptGenRandomImpl();

        private BCryptGenRandomImpl()
        {
        }

        public void GenRandom(byte* pbBuffer, uint cbBuffer)
        {
            BCryptUtil.GenRandom(pbBuffer, cbBuffer);
        }
    }
}
