// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Cng.Internal
{
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

        internal abstract class MockableEncryptor : CngAuthenticatedEncryptorBase
        {
            public override void Dispose()
            {
            }

            public abstract byte[] DecryptHook(IntPtr pbCiphertext, uint cbCiphertext, IntPtr pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData);

            protected override sealed unsafe byte[] DecryptImpl(byte* pbCiphertext, uint cbCiphertext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData)
            {
                return DecryptHook((IntPtr)pbCiphertext, cbCiphertext, (IntPtr)pbAdditionalAuthenticatedData, cbAdditionalAuthenticatedData);
            }

            public abstract byte[] EncryptHook(IntPtr pbPlaintext, uint cbPlaintext, IntPtr pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer);

            protected override sealed unsafe byte[] EncryptImpl(byte* pbPlaintext, uint cbPlaintext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer)
            {
                return EncryptHook((IntPtr)pbPlaintext, cbPlaintext, (IntPtr)pbAdditionalAuthenticatedData, cbAdditionalAuthenticatedData, cbPreBuffer, cbPostBuffer);
            }
        }
    }
}
