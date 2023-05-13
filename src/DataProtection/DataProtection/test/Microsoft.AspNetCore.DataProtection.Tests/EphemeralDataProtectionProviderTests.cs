// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection;

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
