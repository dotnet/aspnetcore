// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using ExtResources = Microsoft.AspNetCore.DataProtection.Extensions.Resources;

namespace Microsoft.AspNetCore.DataProtection;

public class TimeLimitedDataProtectorTests
{
    private const string TimeLimitedPurposeString = "Microsoft.AspNetCore.DataProtection.TimeLimitedDataProtector.v1";

    [Fact]
    public void Protect_LifetimeSpecified()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        DateTimeOffset expiration = StringToDateTime("2000-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector("new purpose").CreateProtector(TimeLimitedPurposeString).Protect(
            new byte[] {
                    0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            })).Returns(new byte[] { 0x10, 0x11 });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act
        var subProtector = timeLimitedProtector.CreateProtector("new purpose");
        var protectedPayload = subProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, expiration);

        // Assert
        Assert.Equal(new byte[] { 0x10, 0x11 }, protectedPayload);
    }

    [Fact]
    public void Protect_LifetimeNotSpecified_UsesInfiniteLifetime()
    {
        // Arrange
        // 0x2bca2875f4373fff is the representation of DateTimeOffset.MaxValue.
        DateTimeOffset expiration = StringToDateTime("2000-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector("new purpose").CreateProtector(TimeLimitedPurposeString).Protect(
            new byte[] {
                    0x2b, 0xca, 0x28, 0x75, 0xf4, 0x37, 0x3f, 0xff, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            })).Returns(new byte[] { 0x10, 0x11 });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act
        var subProtector = timeLimitedProtector.CreateProtector("new purpose");
        var protectedPayload = subProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

        // Assert
        Assert.Equal(new byte[] { 0x10, 0x11 }, protectedPayload);
    }

    [Fact]
    public void Unprotect_WithinPayloadValidityPeriod_Success()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        DateTimeOffset expectedExpiration = StringToDateTime("2000-01-01 00:00:00Z");
        DateTimeOffset now = StringToDateTime("1999-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Returns(
            new byte[] {
                    0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act
        var retVal = timeLimitedProtector.UnprotectCore(new byte[] { 0x10, 0x11 }, now, out var actualExpiration);

        // Assert
        Assert.Equal(expectedExpiration, actualExpiration);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, retVal);
    }

    [Fact]
    public void Unprotect_PayloadHasExpired_Fails()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        DateTimeOffset expectedExpiration = StringToDateTime("2000-01-01 00:00:00Z");
        DateTimeOffset now = StringToDateTime("2001-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Returns(
            new byte[] {
                    0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.UnprotectCore(new byte[] { 0x10, 0x11 }, now, out var _));

        // Assert
        Assert.Equal(ExtResources.FormatTimeLimitedDataProtector_PayloadExpired(expectedExpiration), ex.Message);
    }

    [Fact]
    public void Unprotect_ProtectedDataMalformed_Fails()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Returns(
            new byte[] {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 /* header too short */
            });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.Unprotect(new byte[] { 0x10, 0x11 }, out var _));

        // Assert
        Assert.Equal(ExtResources.TimeLimitedDataProtector_PayloadInvalid, ex.Message);
    }

    [Fact]
    public void Unprotect_UnprotectOperationFails_HomogenizesExceptionToCryptographicException()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Throws(new Exception("How exceptional!"));
        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.Unprotect(new byte[] { 0x10, 0x11 }, out var _));

        // Assert
        Assert.Equal(Resources.CryptCommon_GenericError, ex.Message);
        Assert.Equal("How exceptional!", ex.InnerException.Message);
    }

    [Fact]
    public void RoundTrip_ProtectedData()
    {
        // Arrange
        var ephemeralProtector = new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("my purpose");
        var timeLimitedProtector = new TimeLimitedDataProtector(ephemeralProtector);
        var expectedExpiration = StringToDateTime("2020-01-01 00:00:00Z");

        // Act
        byte[] ephemeralProtectedPayload = ephemeralProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        byte[] timeLimitedProtectedPayload = timeLimitedProtector.Protect(new byte[] { 0x11, 0x22, 0x33, 0x44 }, expectedExpiration);

        // Assert
        Assert.Equal(
            new byte[] { 0x11, 0x22, 0x33, 0x44 },
            timeLimitedProtector.UnprotectCore(timeLimitedProtectedPayload, StringToDateTime("2010-01-01 00:00:00Z"), out var actualExpiration));
        Assert.Equal(expectedExpiration, actualExpiration);

        // the two providers shouldn't be able to talk to one another (due to the purpose chaining)
        Assert.Throws<CryptographicException>(() => ephemeralProtector.Unprotect(timeLimitedProtectedPayload));
        Assert.Throws<CryptographicException>(() => timeLimitedProtector.Unprotect(ephemeralProtectedPayload, out actualExpiration));
    }

    private static DateTime StringToDateTime(string input)
    {
        return DateTimeOffset.ParseExact(input, "u", CultureInfo.InvariantCulture).UtcDateTime;
    }
}
