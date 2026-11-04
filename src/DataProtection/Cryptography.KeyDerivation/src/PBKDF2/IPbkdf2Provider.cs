// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;

/// <summary>
/// Internal interface used for abstracting away the PBKDF2 implementation since the implementation is OS-specific.
/// </summary>
internal interface IPbkdf2Provider
{
    byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested);
}
