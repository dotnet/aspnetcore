// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP
using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    internal interface ISpanAuthenticatedEncryptor : IAuthenticatedEncryptor
    {
        void Encrypt(Span<byte> output, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> additionalAuthenticatedData);
        Span<byte> Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> additionalAuthenticatedData);
    }
}
#endif
