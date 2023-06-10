// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.Internal;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles;

/// <summary>
/// Represents a handle to a BCrypt algorithm provider from which keys and hashes can be created.
/// </summary>
internal sealed unsafe class BCryptAlgorithmHandle : BCryptHandle
{
    // Called by P/Invoke when returning SafeHandles
    public BCryptAlgorithmHandle() { }

    /// <summary>
    /// Creates an unkeyed hash handle from this hash algorithm.
    /// </summary>
    public BCryptHashHandle CreateHash()
    {
        return CreateHashCore(null, 0);
    }

    private BCryptHashHandle CreateHashCore(byte* pbKey, uint cbKey)
    {
        BCryptHashHandle retVal;
        int ntstatus = UnsafeNativeMethods.BCryptCreateHash(this, out retVal, IntPtr.Zero, 0, pbKey, cbKey, dwFlags: 0);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(retVal);

        retVal.SetAlgorithmProviderHandle(this);
        return retVal;
    }

    /// <summary>
    /// Creates an HMAC hash handle from this hash algorithm.
    /// </summary>
    public BCryptHashHandle CreateHmac(byte* pbKey, uint cbKey)
    {
        Debug.Assert(pbKey != null);
        return CreateHashCore(pbKey, cbKey);
    }

    /// <summary>
    /// Imports a key into a symmetric encryption or KDF algorithm.
    /// </summary>
    public BCryptKeyHandle GenerateSymmetricKey(byte* pbSecret, uint cbSecret)
    {
        BCryptKeyHandle retVal;
        int ntstatus = UnsafeNativeMethods.BCryptGenerateSymmetricKey(this, out retVal, IntPtr.Zero, 0, pbSecret, cbSecret, 0);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(retVal);

        retVal.SetAlgorithmProviderHandle(this);
        return retVal;
    }

    /// <summary>
    /// Gets the name of this BCrypt algorithm.
    /// </summary>
    public string GetAlgorithmName()
    {
        const int StackAllocCharSize = 128;

        // First, calculate how many characters are in the name.
        uint byteLengthOfNameWithTerminatingNull = GetProperty(Constants.BCRYPT_ALGORITHM_NAME, null, 0);
        CryptoUtil.Assert(byteLengthOfNameWithTerminatingNull % sizeof(char) == 0 && byteLengthOfNameWithTerminatingNull > sizeof(char) && byteLengthOfNameWithTerminatingNull <= StackAllocCharSize * sizeof(char), "byteLengthOfNameWithTerminatingNull % sizeof(char) == 0 && byteLengthOfNameWithTerminatingNull > sizeof(char) && byteLengthOfNameWithTerminatingNull <= StackAllocCharSize * sizeof(char)");
        uint numCharsWithoutNull = (byteLengthOfNameWithTerminatingNull - 1) / sizeof(char);

        if (numCharsWithoutNull == 0)
        {
            return string.Empty; // degenerate case
        }

        char* pBuffer = stackalloc char[StackAllocCharSize];
        uint numBytesCopied = GetProperty(Constants.BCRYPT_ALGORITHM_NAME, pBuffer, byteLengthOfNameWithTerminatingNull);
        CryptoUtil.Assert(numBytesCopied == byteLengthOfNameWithTerminatingNull, "numBytesCopied == byteLengthOfNameWithTerminatingNull");
        return new string(pBuffer, 0, (int)numCharsWithoutNull);
    }

    /// <summary>
    /// Gets the cipher block length (in bytes) of this block cipher algorithm.
    /// </summary>
    public uint GetCipherBlockLength()
    {
        uint cipherBlockLength;
        uint numBytesCopied = GetProperty(Constants.BCRYPT_BLOCK_LENGTH, &cipherBlockLength, sizeof(uint));
        CryptoUtil.Assert(numBytesCopied == sizeof(uint), "numBytesCopied == sizeof(uint)");
        return cipherBlockLength;
    }

    /// <summary>
    /// Gets the hash block length (in bytes) of this hash algorithm.
    /// </summary>
    public uint GetHashBlockLength()
    {
        uint hashBlockLength;
        uint numBytesCopied = GetProperty(Constants.BCRYPT_HASH_BLOCK_LENGTH, &hashBlockLength, sizeof(uint));
        CryptoUtil.Assert(numBytesCopied == sizeof(uint), "numBytesCopied == sizeof(uint)");
        return hashBlockLength;
    }

    /// <summary>
    /// Gets the key lengths (in bits) supported by this algorithm.
    /// </summary>
    public BCRYPT_KEY_LENGTHS_STRUCT GetSupportedKeyLengths()
    {
        BCRYPT_KEY_LENGTHS_STRUCT supportedKeyLengths;
        uint numBytesCopied = GetProperty(Constants.BCRYPT_KEY_LENGTHS, &supportedKeyLengths, (uint)sizeof(BCRYPT_KEY_LENGTHS_STRUCT));
        CryptoUtil.Assert(numBytesCopied == sizeof(BCRYPT_KEY_LENGTHS_STRUCT), "numBytesCopied == sizeof(BCRYPT_KEY_LENGTHS_STRUCT)");
        return supportedKeyLengths;
    }

    /// <summary>
    /// Gets the digest length (in bytes) of this hash algorithm provider.
    /// </summary>
    public uint GetHashDigestLength()
    {
        uint digestLength;
        uint numBytesCopied = GetProperty(Constants.BCRYPT_HASH_LENGTH, &digestLength, sizeof(uint));
        CryptoUtil.Assert(numBytesCopied == sizeof(uint), "numBytesCopied == sizeof(uint)");
        return digestLength;
    }

    public static BCryptAlgorithmHandle OpenAlgorithmHandle(string algorithmId, string? implementation = null, bool hmac = false)
    {
        // from bcrypt.h
        const uint BCRYPT_ALG_HANDLE_HMAC_FLAG = 0x00000008;

        // from ntstatus.h
        const int STATUS_NOT_FOUND = unchecked((int)0xC0000225);

        BCryptAlgorithmHandle algHandle;
        int ntstatus = UnsafeNativeMethods.BCryptOpenAlgorithmProvider(out algHandle, algorithmId, implementation, dwFlags: (hmac) ? BCRYPT_ALG_HANDLE_HMAC_FLAG : 0);

        // error checking
        if (ntstatus == STATUS_NOT_FOUND)
        {
            string message = String.Format(CultureInfo.CurrentCulture, Resources.BCryptAlgorithmHandle_ProviderNotFound, algorithmId);
            throw new CryptographicException(message);
        }
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(algHandle);

        return algHandle;
    }

    // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
    protected override bool ReleaseHandle()
    {
        return (UnsafeNativeMethods.BCryptCloseAlgorithmProvider(handle, dwFlags: 0) == 0);
    }

    public void SetChainingMode(string chainingMode)
    {
        fixed (char* pszChainingMode = chainingMode)
        {
            SetProperty(Constants.BCRYPT_CHAINING_MODE, pszChainingMode, checked((uint)(chainingMode.Length + 1 /* null terminator */) * sizeof(char)));
        }
    }
}
