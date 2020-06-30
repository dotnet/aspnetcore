// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class Base32Test
    {
        [Fact]
        public void ConversionTest()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            Assert.Equal<byte>(data, Base32.FromBase32(Base32.ToBase32(data)));

            int length;
            do {
                length = GetRandomByteArray(1)[0];
            } while (length % 5 == 0);
            data = GetRandomByteArray(length);
            Assert.Equal<byte>(data, Base32.FromBase32(Base32.ToBase32(data)));

            length = (int)(GetRandomByteArray(1)[0]) * 5;
            data = GetRandomByteArray(length);
            Assert.Equal<byte>(data, Base32.FromBase32(Base32.ToBase32(data)));
        }


        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        private static byte[] GetRandomByteArray(int length) {
            byte[] bytes = new byte[length];
            _rng.GetBytes(bytes);
            return bytes;
        }
    }

}
