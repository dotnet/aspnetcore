// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Cryptography.Cng;

// http://msdn.microsoft.com/en-us/library/windows/desktop/cc562981(v=vs.85).aspx
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
{
    public uint cbSize;
    public uint dwInfoVersion;
    public byte* pbNonce;
    public uint cbNonce;
    public byte* pbAuthData;
    public uint cbAuthData;
    public byte* pbTag;
    public uint cbTag;
    public byte* pbMacContext;
    public uint cbMacContext;
    public uint cbAAD;
    public ulong cbData;
    public uint dwFlags;

    // corresponds to the BCRYPT_INIT_AUTH_MODE_INFO macro in bcrypt.h
    public static void Init(out BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO info)
    {
        const uint BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_VERSION = 1;
        info = new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
        {
            cbSize = (uint)sizeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO),
            dwInfoVersion = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_VERSION
        };
    }
}
