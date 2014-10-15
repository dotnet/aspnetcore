// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.Managed;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection
{
    public unsafe sealed class ProtectedMemoryBlob : IDisposable, ISecret
    {
        // from wincrypt.h
        private const uint CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;

        private readonly SecureLocalAllocHandle _localAllocHandle;
        private readonly uint _plaintextLength;

        public ProtectedMemoryBlob(ArraySegment<byte> plaintext)
        {
            plaintext.Validate();

            _localAllocHandle = Protect(plaintext);
            _plaintextLength = (uint)plaintext.Count;
        }

        public ProtectedMemoryBlob(byte[] plaintext)
            : this(new ArraySegment<byte>(plaintext))
        {
        }

        public ProtectedMemoryBlob(byte* plaintext, int plaintextLength)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException("plaintext");
            }
            if (plaintextLength < 0)
            {
                throw new ArgumentOutOfRangeException("plaintextLength");
            }

            _localAllocHandle = Protect(plaintext, (uint)plaintextLength);
            _plaintextLength = (uint)plaintextLength;
        }

        public ProtectedMemoryBlob(ISecret secret)
        {
            if (secret == null)
            {
                throw new ArgumentNullException("secret");
            }

            ProtectedMemoryBlob other = secret as ProtectedMemoryBlob;
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
                byte[] tempPlaintextBuffer = new byte[secret.Length];
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

        public int Length
        {
            get
            {
                return (int)_plaintextLength; // ctor guarantees the length fits into a signed int
            }
        }

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
            if (!OSVersionUtil.IsBCryptOnWin7OrLaterAvailable())
            {
                SecureLocalAllocHandle handle = SecureLocalAllocHandle.Allocate((IntPtr)checked((int)cbPlaintext));
                UnsafeBufferUtil.BlockCopy(from: pbPlaintext, to: handle, byteCount: cbPlaintext);
                return handle;
            }

            // We need to make sure we're a multiple of CRYPTPROTECTMEMORY_BLOCK_SIZE.
            uint numTotalBytesToAllocate = cbPlaintext;
            uint numBytesPaddingRequired = CRYPTPROTECTMEMORY_BLOCK_SIZE - (numTotalBytesToAllocate % CRYPTPROTECTMEMORY_BLOCK_SIZE);
            if (numBytesPaddingRequired == CRYPTPROTECTMEMORY_BLOCK_SIZE)
            {
                numBytesPaddingRequired = 0; // we're already a proper multiple of the block size
            }
            checked { numTotalBytesToAllocate += numBytesPaddingRequired; }
            CryptoUtil.Assert(numTotalBytesToAllocate % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0, "numTotalBytesToAllocate % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0");

            // Allocate and copy plaintext data; padding is uninitialized / undefined.
            SecureLocalAllocHandle encryptedMemoryHandle = SecureLocalAllocHandle.Allocate((IntPtr)numTotalBytesToAllocate);
            UnsafeBufferUtil.BlockCopy(from: pbPlaintext, to: encryptedMemoryHandle, byteCount: cbPlaintext);

            // Finally, CryptProtectMemory the whole mess.
            if (numTotalBytesToAllocate != 0)
            {
                MemoryProtection.CryptProtectMemory(encryptedMemoryHandle, byteCount: numTotalBytesToAllocate);
            }
            return encryptedMemoryHandle;
        }

        public static ProtectedMemoryBlob Random(int numBytes)
        {
            CryptoUtil.Assert(numBytes >= 0, "numBytes >= 0");

            if (numBytes == 0)
            {
                byte dummy;
                return new ProtectedMemoryBlob(&dummy, 0);
            }
            else
            {
                // Don't use CNG if we're not on Windows.
                if (!OSVersionUtil.IsBCryptOnWin7OrLaterAvailable())
                {
                    return new ProtectedMemoryBlob(ManagedGenRandomImpl.Instance.GenRandom(numBytes));
                }

                byte[] bytes = new byte[numBytes];
                fixed (byte* pbBytes = bytes)
                {
                    try
                    {
                        BCryptUtil.GenRandom(pbBytes, (uint)numBytes);
                        return new ProtectedMemoryBlob(pbBytes, numBytes);
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
            if (!OSVersionUtil.IsBCryptOnWin7OrLaterAvailable())
            {
                UnsafeBufferUtil.BlockCopy(from: _localAllocHandle, to: pbBuffer, byteCount: _plaintextLength);
                return;
            }

            if (_plaintextLength % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0)
            {
                // Case 1: Secret length is an exact multiple of the block size. Copy directly to the buffer and decrypt there.
                // We go through this code path even for empty plaintexts since we still want SafeHandle dispose semantics.
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

        public void WriteSecretIntoBuffer(ArraySegment<byte> buffer)
        {
            // Parameter checking
            buffer.Validate();
            if (buffer.Count != Length)
            {
                throw Error.Common_BufferIncorrectlySized("buffer", actualSize: buffer.Count, expectedSize: Length);
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

        public void WriteSecretIntoBuffer(byte* buffer, int bufferLength)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (bufferLength < 0)
            {
                throw new ArgumentOutOfRangeException("bufferLength");
            }
            if (bufferLength != Length)
            {
                throw Error.Common_BufferIncorrectlySized("bufferLength", actualSize: bufferLength, expectedSize: Length);
            }

            UnprotectInto(buffer);
        }
    }
}
