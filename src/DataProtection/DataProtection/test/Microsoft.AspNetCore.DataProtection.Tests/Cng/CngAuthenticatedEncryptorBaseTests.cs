// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.Cng.Internal;

public unsafe class CngAuthenticatedEncryptorBaseTests
{
    [Fact]
    public void Decrypt_ForwardsArraySegment()
    {
        // Arrange
        var ciphertext = new ArraySegment<byte>(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 }, 3, 2);
        var aad = new ArraySegment<byte>(new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14 }, 1, 4);

        var encryptorMock = new Mock<MockableEncryptor>();
        encryptorMock
            .Setup(o => o.DecryptHook(It.IsAny<IntPtr>(), 2, It.IsAny<IntPtr>(), 4))
            .Returns((IntPtr pbCiphertext, uint cbCiphertext, IntPtr pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData) =>
            {
                // ensure that pointers started at the right place
                Assert.Equal((byte)0x03, *(byte*)pbCiphertext);
                Assert.Equal((byte)0x11, *(byte*)pbAdditionalAuthenticatedData);
                return new byte[] { 0x20, 0x21, 0x22 };
            });

        // Act
        var retVal = encryptorMock.Object.Decrypt(ciphertext, aad);

        // Assert
        Assert.Equal(new byte[] { 0x20, 0x21, 0x22 }, retVal);
    }

    [Fact]
    public void Decrypt_HandlesEmptyAADPointerFixup()
    {
        // Arrange
        var ciphertext = new ArraySegment<byte>(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 }, 3, 2);
        var aad = new ArraySegment<byte>(new byte[0]);

        var encryptorMock = new Mock<MockableEncryptor>();
        encryptorMock
            .Setup(o => o.DecryptHook(It.IsAny<IntPtr>(), 2, It.IsAny<IntPtr>(), 0))
            .Returns((IntPtr pbCiphertext, uint cbCiphertext, IntPtr pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData) =>
            {
                // ensure that pointers started at the right place
                Assert.Equal((byte)0x03, *(byte*)pbCiphertext);
                Assert.NotEqual(IntPtr.Zero, pbAdditionalAuthenticatedData); // CNG will complain if this pointer is zero
                return new byte[] { 0x20, 0x21, 0x22 };
            });

        // Act
        var retVal = encryptorMock.Object.Decrypt(ciphertext, aad);

        // Assert
        Assert.Equal(new byte[] { 0x20, 0x21, 0x22 }, retVal);
    }

    [Fact]
    public void Decrypt_HandlesEmptyCiphertextPointerFixup()
    {
        // Arrange
        var ciphertext = new ArraySegment<byte>(new byte[0]);
        var aad = new ArraySegment<byte>(new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14 }, 1, 4);

        var encryptorMock = new Mock<MockableEncryptor>();
        encryptorMock
            .Setup(o => o.DecryptHook(It.IsAny<IntPtr>(), 0, It.IsAny<IntPtr>(), 4))
            .Returns((IntPtr pbCiphertext, uint cbCiphertext, IntPtr pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData) =>
            {
                // ensure that pointers started at the right place
                Assert.NotEqual(IntPtr.Zero, pbCiphertext); // CNG will complain if this pointer is zero
                Assert.Equal((byte)0x11, *(byte*)pbAdditionalAuthenticatedData);
                return new byte[] { 0x20, 0x21, 0x22 };
            });

        // Act
        var retVal = encryptorMock.Object.Decrypt(ciphertext, aad);

        // Assert
        Assert.Equal(new byte[] { 0x20, 0x21, 0x22 }, retVal);
    }

    internal abstract unsafe class MockableEncryptor : IOptimizedAuthenticatedEncryptor, ISpanAuthenticatedEncryptor, IDisposable
    {
        public abstract byte[] DecryptHook(IntPtr pbCiphertext, uint cbCiphertext, IntPtr pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData);
        public abstract byte[] EncryptHook(IntPtr pbPlaintext, uint cbPlaintext, IntPtr pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer);

        public int GetEncryptedSize(int plainTextLength) => 1000;
        public int GetDecryptedSize(int cipherTextLength) => 1000;

        public byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> additionalAuthenticatedData)
        {
            fixed (byte* pbCiphertext = ciphertext.Array)
            fixed (byte* pbAAD = additionalAuthenticatedData.Array)
            {
                IntPtr ptrCiphertext = (IntPtr)(pbCiphertext + ciphertext.Offset);
                IntPtr ptrAAD = (IntPtr)(pbAAD + additionalAuthenticatedData.Offset);

                return DecryptHook(ptrCiphertext, (uint)ciphertext.Count, ptrAAD, (uint)additionalAuthenticatedData.Count);
            }
        }

        public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize)
        {
            fixed (byte* pbPlaintext = plaintext.Array)
            fixed (byte* pbAAD = additionalAuthenticatedData.Array)
            {
                IntPtr ptrPlaintext = (IntPtr)(pbPlaintext + plaintext.Offset);
                IntPtr ptrAAD = (IntPtr)(pbAAD + additionalAuthenticatedData.Offset);

                return EncryptHook(ptrPlaintext, (uint)plaintext.Count, ptrAAD, (uint)additionalAuthenticatedData.Count, preBufferSize, postBufferSize);
            }
        }

        public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData)
            => Encrypt(plaintext, additionalAuthenticatedData, 0, 0);

        public bool TryEncrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> additionalAuthenticatedData, Span<byte> destination, out int bytesWritten)
        {
            var encrypted = Encrypt(ToArraySegment(plaintext), ToArraySegment(additionalAuthenticatedData));
            encrypted.CopyTo(destination);
            bytesWritten = encrypted.Length;
            return true;
        }

        public bool TryDecrypt(ReadOnlySpan<byte> cipherText, ReadOnlySpan<byte> additionalAuthenticatedData, Span<byte> destination, out int bytesWritten)
        {
            var encrypted = Decrypt(ToArraySegment(cipherText), ToArraySegment(additionalAuthenticatedData));
            encrypted.CopyTo(destination);
            bytesWritten = encrypted.Length;
            return true;
        }

        public void Dispose() { }

        ArraySegment<byte> ToArraySegment(ReadOnlySpan<byte> span)
        {
            var array = span.ToArray();
            return new ArraySegment<byte>(array);
        }
    }
}
