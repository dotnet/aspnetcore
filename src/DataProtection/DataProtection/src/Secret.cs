// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.Managed;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Represents a secret value stored in memory.
/// </summary>
public sealed unsafe class Secret : IDisposable, ISecret
{
    // from wincrypt.h
    private const uint CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;

    private readonly SecureLocalAllocHandle _localAllocHandle;
    private readonly uint _plaintextLength;

    /// <summary>
    /// Creates a new Secret from the provided input value, where the input value
    /// is specified as an array segment.
    /// </summary>
    public Secret(ArraySegment<byte> value)
    {
        value.Validate();

        _localAllocHandle = Protect(value);
        _plaintextLength = (uint)value.Count;
    }

    /// <summary>
    /// Creates a new Secret from the provided input value, where the input value
    /// is specified as an array.
    /// </summary>
    public Secret(byte[] value)
        : this(new ArraySegment<byte>(value))
    {
        ArgumentNullThrowHelper.ThrowIfNull(value);
    }

    /// <summary>
    /// Creates a new Secret from the provided input value, where the input value
    /// is specified as a pointer to unmanaged memory.
    /// </summary>
    public Secret(byte* secret, int secretLength)
    {
        if (secret == null)
        {
            throw new ArgumentNullException(nameof(secret));
        }
        if (secretLength < 0)
        {
            throw Error.Common_ValueMustBeNonNegative(nameof(secretLength));
        }

        _localAllocHandle = Protect(secret, (uint)secretLength);
        _plaintextLength = (uint)secretLength;
    }

    /// <summary>
    /// Creates a new Secret from another secret object.
    /// </summary>
    public Secret(ISecret secret)
    {
        ArgumentNullThrowHelper.ThrowIfNull(secret);

        var other = secret as Secret;
        if (other != null)
        {
            // Fast-track: simple deep copy scenario.
            this._localAllocHandle = other._localAllocHandle.Duplicate();
            this._plaintextLength = other._plaintextLength;
        }
        else
        {
            // Copy the secret to a temporary managed buffer, then protect the buffer.
            // We pin the temp buffer and zero it out when we're finished to limit exposure of the secret.
            var tempPlaintextBuffer = new byte[secret.Length];
            fixed (byte* pbTempPlaintextBuffer = tempPlaintextBuffer)
            {
                try
                {
                    secret.WriteSecretIntoBuffer(new ArraySegment<byte>(tempPlaintextBuffer));
                    _localAllocHandle = Protect(pbTempPlaintextBuffer, (uint)tempPlaintextBuffer.Length);
                    _plaintextLength = (uint)tempPlaintextBuffer.Length;
                }
                finally
                {
                    UnsafeBufferUtil.SecureZeroMemory(pbTempPlaintextBuffer, tempPlaintextBuffer.Length);
                }
            }
        }
    }

    /// <summary>
    /// The length (in bytes) of the secret value.
    /// </summary>
    public int Length
    {
        get
        {
            return (int)_plaintextLength; // ctor guarantees the length fits into a signed int
        }
    }

    /// <summary>
    /// Wipes the secret from memory.
    /// </summary>
    public void Dispose()
    {
        _localAllocHandle.Dispose();
    }

    private static SecureLocalAllocHandle Protect(ArraySegment<byte> plaintext)
    {
        fixed (byte* pbPlaintextArray = plaintext.Array)
        {
            return Protect(&pbPlaintextArray[plaintext.Offset], (uint)plaintext.Count);
        }
    }

    private static SecureLocalAllocHandle Protect(byte* pbPlaintext, uint cbPlaintext)
    {
        // If we're not running on a platform that supports CryptProtectMemory,
        // shove the plaintext directly into a LocalAlloc handle. Ideally we'd
        // mark this memory page as non-pageable, but this is fraught with peril.
        if (!OSVersionUtil.IsWindows())
        {
            var handle = SecureLocalAllocHandle.Allocate((IntPtr)checked((int)cbPlaintext));
            UnsafeBufferUtil.BlockCopy(from: pbPlaintext, to: handle, byteCount: cbPlaintext);
            return handle;
        }

        // We need to make sure we're a multiple of CRYPTPROTECTMEMORY_BLOCK_SIZE.
        var numTotalBytesToAllocate = cbPlaintext;
        var numBytesPaddingRequired = CRYPTPROTECTMEMORY_BLOCK_SIZE - (numTotalBytesToAllocate % CRYPTPROTECTMEMORY_BLOCK_SIZE);
        if (numBytesPaddingRequired == CRYPTPROTECTMEMORY_BLOCK_SIZE)
        {
            numBytesPaddingRequired = 0; // we're already a proper multiple of the block size
        }
        checked { numTotalBytesToAllocate += numBytesPaddingRequired; }
        CryptoUtil.Assert(numTotalBytesToAllocate % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0, "numTotalBytesToAllocate % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0");

        // Allocate and copy plaintext data; padding is uninitialized / undefined.
        var encryptedMemoryHandle = SecureLocalAllocHandle.Allocate((IntPtr)numTotalBytesToAllocate);
        UnsafeBufferUtil.BlockCopy(from: pbPlaintext, to: encryptedMemoryHandle, byteCount: cbPlaintext);

        // Finally, CryptProtectMemory the whole mess.
        if (numTotalBytesToAllocate != 0)
        {
            MemoryProtection.CryptProtectMemory(encryptedMemoryHandle, byteCount: numTotalBytesToAllocate);
        }
        return encryptedMemoryHandle;
    }

    /// <summary>
    /// Returns a Secret made entirely of random bytes retrieved from
    /// a cryptographically secure RNG.
    /// </summary>
    public static Secret Random(int numBytes)
    {
        if (numBytes < 0)
        {
            throw Error.Common_ValueMustBeNonNegative(nameof(numBytes));
        }

        if (numBytes == 0)
        {
            byte dummy;
            return new Secret(&dummy, 0);
        }
        else
        {
            // Don't use CNG if we're not on Windows.
            if (!OSVersionUtil.IsWindows())
            {
                return new Secret(ManagedGenRandomImpl.Instance.GenRandom(numBytes));
            }

            var bytes = new byte[numBytes];
            fixed (byte* pbBytes = bytes)
            {
                try
                {
                    BCryptUtil.GenRandom(pbBytes, (uint)numBytes);
                    return new Secret(pbBytes, numBytes);
                }
                finally
                {
                    UnsafeBufferUtil.SecureZeroMemory(pbBytes, numBytes);
                }
            }
        }
    }

    private void UnprotectInto(byte* pbBuffer)
    {
        // If we're not running on a platform that supports CryptProtectMemory,
        // the handle contains plaintext bytes.
        if (!OSVersionUtil.IsWindows())
        {
            UnsafeBufferUtil.BlockCopy(from: _localAllocHandle, to: pbBuffer, byteCount: _plaintextLength);
            return;
        }

        if (_plaintextLength % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0)
        {
            // Case 1: Secret length is an exact multiple of the block size. Copy directly to the buffer and decrypt there.
            UnsafeBufferUtil.BlockCopy(from: _localAllocHandle, to: pbBuffer, byteCount: _plaintextLength);
            MemoryProtection.CryptUnprotectMemory(pbBuffer, _plaintextLength);
        }
        else
        {
            // Case 2: Secret length is not a multiple of the block size. We'll need to duplicate the data and
            // perform the decryption in the duplicate buffer, then copy the plaintext data over.
            using (var duplicateHandle = _localAllocHandle.Duplicate())
            {
                MemoryProtection.CryptUnprotectMemory(duplicateHandle, checked((uint)duplicateHandle.Length));
                UnsafeBufferUtil.BlockCopy(from: duplicateHandle, to: pbBuffer, byteCount: _plaintextLength);
            }
        }
    }

    /// <summary>
    /// Writes the secret value to the specified buffer.
    /// </summary>
    /// <remarks>
    /// The buffer size must exactly match the length of the secret value.
    /// </remarks>
    public void WriteSecretIntoBuffer(ArraySegment<byte> buffer)
    {
        // Parameter checking
        buffer.Validate();
        if (buffer.Count != Length)
        {
            throw Error.Common_BufferIncorrectlySized(nameof(buffer), actualSize: buffer.Count, expectedSize: Length);
        }

        // only unprotect if the secret is zero-length, as CLR doesn't like pinning zero-length buffers
        if (Length != 0)
        {
            fixed (byte* pbBufferArray = buffer.Array)
            {
                UnprotectInto(&pbBufferArray[buffer.Offset]);
            }
        }
    }

    /// <summary>
    /// Writes the secret value to the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer into which to write the secret value.</param>
    /// <param name="bufferLength">The size (in bytes) of the provided buffer.</param>
    /// <remarks>
    /// The 'bufferLength' parameter must exactly match the length of the secret value.
    /// </remarks>
    public void WriteSecretIntoBuffer(byte* buffer, int bufferLength)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
        if (bufferLength != Length)
        {
            throw Error.Common_BufferIncorrectlySized(nameof(bufferLength), actualSize: bufferLength, expectedSize: Length);
        }

        if (Length != 0)
        {
            UnprotectInto(buffer);
        }
    }
}
