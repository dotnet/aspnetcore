// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.HttpSys.Internal;

// From Schannel.h
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct SecPkgContext_CipherInfo
{
    private const int SZ_ALG_MAX_SIZE = 64;

    private readonly int dwVersion;
    private readonly int dwProtocol;
    public readonly int dwCipherSuite;
    private readonly int dwBaseCipherSuite;
    private fixed char szCipherSuite[SZ_ALG_MAX_SIZE];
    private fixed char szCipher[SZ_ALG_MAX_SIZE];
    private readonly int dwCipherLen;
    private readonly int dwCipherBlockLen; // in bytes
    private fixed char szHash[SZ_ALG_MAX_SIZE];
    private readonly int dwHashLen;
    private fixed char szExchange[SZ_ALG_MAX_SIZE];
    private readonly int dwMinExchangeLen;
    private readonly int dwMaxExchangeLen;
    private fixed char szCertificate[SZ_ALG_MAX_SIZE];
    private readonly int dwKeyType;
}
