// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class EphemeralDataProtectionProviderTests
    {
        [Fact]
        public void DifferentProvider_SamePurpose_DoesNotRoundTripData()
        {
            // Arrange
            var dataProtector1 = new EphemeralDataProtectionProvider().CreateProtector("purpose");
            var dataProtector2 = new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("purpose");
            byte[] bytes = Encoding.UTF8.GetBytes("Hello there!");

            // Act & assert
            // Each instance of the EphemeralDataProtectionProvider has its own unique KDK, so payloads can't be shared.
            byte[] protectedBytes = dataProtector1.Protect(bytes);
            Assert.ThrowsAny<CryptographicException>(() =>
            {
                byte[] unprotectedBytes = dataProtector2.Unprotect(protectedBytes);
            });
        }

        [Fact]
        public void CanTryProtectUnprotect()
        {
            // Arrange
            var dataProtector1 = new EphemeralDataProtectionProvider().CreateProtector("purpose");

            byte[] bytes = Encoding.UTF8.GetBytes("Hello there!");
            Span<byte> results = new byte[bytes.Length];
            Span<byte> encrypted = new byte[100];

            // Act & assert
            Assert.True(dataProtector1.TryProtect(encrypted, bytes, out var encryptWritten));
            Assert.Equal(76, encryptWritten);
            Assert.True(dataProtector1.TryUnprotect(results, encrypted.Slice(0, encryptWritten), out var decryptWritten));
            Assert.Equal(bytes.Length, decryptWritten);

            Assert.Equal("Hello there!", Encoding.UTF8.GetString(results));
        }

        [Fact]
        public void TryProtectReturnsFalseIfOutputTooSmall()
        {
            // Arrange
            var dataProtector1 = new EphemeralDataProtectionProvider().CreateProtector("purpose");

            byte[] bytes = Encoding.UTF8.GetBytes("Hello there!");
            Span<byte> encrypted = new byte[10];

            // Act & assert
            Assert.False(dataProtector1.TryProtect(encrypted, bytes, out var encryptWritten));
            Assert.Equal(0, encryptWritten);
        }

        [Fact]
        public void TryUnprotectReturnsFalseIfOutputTooSmall()
        {
            // Arrange
            var dataProtector1 = new EphemeralDataProtectionProvider().CreateProtector("purpose");

            byte[] bytes = Encoding.UTF8.GetBytes("Hello there!");
            Span<byte> results = new byte[10];
            Span<byte> encrypted = new byte[100];

            // Act & assert
            Assert.True(dataProtector1.TryProtect(encrypted, bytes, out var encryptWritten));
            Assert.Equal(76, encryptWritten);
            Assert.False(dataProtector1.TryUnprotect(results, encrypted.Slice(0, encryptWritten), out var decryptWritten));
            Assert.Equal(0, decryptWritten);
        }


        [Fact]
        public void SingleProvider_DifferentPurpose_DoesNotRoundTripData()
        {
            // Arrange
            var dataProtectionProvider = new EphemeralDataProtectionProvider(NullLoggerFactory.Instance);
            var dataProtector1 = dataProtectionProvider.CreateProtector("purpose");
            var dataProtector2 = dataProtectionProvider.CreateProtector("different purpose");
            byte[] bytes = Encoding.UTF8.GetBytes("Hello there!");

            // Act & assert
            byte[] protectedBytes = dataProtector1.Protect(bytes);
            Assert.ThrowsAny<CryptographicException>(() =>
            {
                byte[] unprotectedBytes = dataProtector2.Unprotect(protectedBytes);
            });
        }

        [Fact]
        public void SingleProvider_SamePurpose_RoundTripsData()
        {
            // Arrange
            var dataProtectionProvider = new EphemeralDataProtectionProvider(NullLoggerFactory.Instance);
            var dataProtector1 = dataProtectionProvider.CreateProtector("purpose");
            var dataProtector2 = dataProtectionProvider.CreateProtector("purpose"); // should be equivalent to the previous instance
            byte[] bytes = Encoding.UTF8.GetBytes("Hello there!");

            // Act
            byte[] protectedBytes = dataProtector1.Protect(bytes);
            byte[] unprotectedBytes = dataProtector2.Unprotect(protectedBytes);

            // Assert
            Assert.Equal(bytes, unprotectedBytes);
        }
    }
}
