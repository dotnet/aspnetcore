// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection.PBKDF2
{
    /// <summary>
    /// Internal interface used for abstracting away the PBKDF2 implementation since the implementation is OS-specific.
    /// </summary>
    internal interface IPbkdf2Provider
    {
        byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested);
    }
}
