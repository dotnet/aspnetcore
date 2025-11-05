// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// Provides an authenticated encryption and decryption routine via a span-based API.
/// </summary>
public interface ISpanAuthenticatedEncryptor : IAuthenticatedEncryptor
{
    void Encrypt<TWriter>(ReadOnlySpan<byte> plainttext, ReadOnlySpan<byte> additionalAuthenticatedData, TWriter destination)
        where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
        ;

    void Decrypt<TWriter>(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> additionalAuthenticatedData, TWriter destination)
        where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
        ;
}
