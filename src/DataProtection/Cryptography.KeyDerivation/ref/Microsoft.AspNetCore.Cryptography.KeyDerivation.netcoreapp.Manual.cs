// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2
{
    internal sealed partial class NetCorePbkdf2Provider : Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2.IPbkdf2Provider
    {
        public NetCorePbkdf2Provider() { }
        public byte[] DeriveKey(string password, byte[] salt, Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf prf, int iterationCount, int numBytesRequested) { throw null; }
    }
}
