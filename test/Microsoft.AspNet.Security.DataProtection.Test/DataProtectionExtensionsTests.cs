// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Security.DataProtection.Test
{
    public class DataProtectionExtensionsTests
    {
        [Fact]
        public void Protect_InvalidUtf_Failure()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();

            // Act & assert
            var ex = Assert.Throws<CryptographicException>(() =>
            {
                DataProtectionExtensions.Protect(mockProtector.Object, "Hello\ud800");
            });
            Assert.IsAssignableFrom(typeof(EncoderFallbackException), ex.InnerException);
        }

        [Fact]
        public void Protect_Success()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
            mockProtector.Setup(p => p.Protect(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f })).Returns(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            // Act
            string retVal = DataProtectionExtensions.Protect(mockProtector.Object, "Hello");

            // Assert
            Assert.Equal("AQIDBAU", retVal);
        }

        [Fact]
        public void Unprotect_InvalidBase64BeforeDecryption_Failure()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();

            // Act & assert
            var ex = Assert.Throws<CryptographicException>(() =>
            {
                DataProtectionExtensions.Unprotect(mockProtector.Object, "A");
            });
            Assert.IsAssignableFrom(typeof(FormatException), ex.InnerException);
        }

        [Fact]
        public void Unprotect_InvalidUtfAfterDecryption_Failure()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
            mockProtector.Setup(p => p.Unprotect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0xff });

            // Act & assert
            var ex = Assert.Throws<CryptographicException>(() =>
            {
                DataProtectionExtensions.Unprotect(mockProtector.Object, "AQIDBAU");
            });
            Assert.IsAssignableFrom(typeof(DecoderFallbackException), ex.InnerException);
        }

        [Fact]
        public void Unprotect_Success()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
            mockProtector.Setup(p => p.Unprotect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f });

            // Act
            string retVal = DataProtectionExtensions.Unprotect(mockProtector.Object, "AQIDBAU");

            // Assert
            Assert.Equal("Hello", retVal);
        }
    }
}
