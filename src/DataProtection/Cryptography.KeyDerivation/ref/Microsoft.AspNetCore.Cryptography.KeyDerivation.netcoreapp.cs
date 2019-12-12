// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation
{
    public static partial class KeyDerivation
    {
        public static byte[] Pbkdf2(string password, byte[] salt, Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf prf, int iterationCount, int numBytesRequested) { throw null; }
    }
    public enum KeyDerivationPrf
    {
        HMACSHA1 = 0,
        HMACSHA256 = 1,
        HMACSHA512 = 2,
    }
}
