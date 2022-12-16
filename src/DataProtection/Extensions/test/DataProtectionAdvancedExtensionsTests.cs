// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection;

public class DataProtectionAdvancedExtensionsTests
{
    private const string SampleEncodedString = "AQI"; // = WebEncoders.Base64UrlEncode({ 0x01, 0x02 })

    [Fact]
    public void Protect_PayloadAsString_WithExplicitExpiration()
    {
        // Arrange
        var plaintextAsBytes = Encoding.UTF8.GetBytes("this is plaintext");
        var expiration = StringToDateTime("2015-01-01 00:00:00Z");
        var mockDataProtector = new Mock<ITimeLimitedDataProtector>();
        mockDataProtector.Setup(o => o.Protect(plaintextAsBytes, expiration)).Returns(new byte[] { 0x01, 0x02 });

        // Act
        string protectedPayload = mockDataProtector.Object.Protect("this is plaintext", expiration);

        // Assert
        Assert.Equal(SampleEncodedString, protectedPayload);
    }

    [Fact]
    public void Protect_PayloadAsString_WithLifetimeAsTimeSpan()
    {
        // Arrange
        var plaintextAsBytes = Encoding.UTF8.GetBytes("this is plaintext");
        DateTimeOffset actualExpiration = default(DateTimeOffset);
        var mockDataProtector = new Mock<ITimeLimitedDataProtector>();
        mockDataProtector.Setup(o => o.Protect(plaintextAsBytes, It.IsAny<DateTimeOffset>()))
            .Returns<byte[], DateTimeOffset>((_, exp) =>
            {
                actualExpiration = exp;
                return new byte[] { 0x01, 0x02 };
            });

        // Act
        DateTimeOffset lowerBound = DateTimeOffset.UtcNow.AddHours(48);
        string protectedPayload = mockDataProtector.Object.Protect("this is plaintext", TimeSpan.FromHours(48));
        DateTimeOffset upperBound = DateTimeOffset.UtcNow.AddHours(48);

        // Assert
        Assert.Equal(SampleEncodedString, protectedPayload);
        Assert.InRange(actualExpiration, lowerBound, upperBound);
    }

    [Fact]
    public void Protect_PayloadAsBytes_WithLifetimeAsTimeSpan()
    {
        // Arrange
        DateTimeOffset actualExpiration = default(DateTimeOffset);
        var mockDataProtector = new Mock<ITimeLimitedDataProtector>();
        mockDataProtector.Setup(o => o.Protect(new byte[] { 0x11, 0x22, 0x33 }, It.IsAny<DateTimeOffset>()))
            .Returns<byte[], DateTimeOffset>((_, exp) =>
            {
                actualExpiration = exp;
                return new byte[] { 0x01, 0x02 };
            });

        // Act
        DateTimeOffset lowerBound = DateTimeOffset.UtcNow.AddHours(48);
        byte[] protectedPayload = mockDataProtector.Object.Protect(new byte[] { 0x11, 0x22, 0x33 }, TimeSpan.FromHours(48));
        DateTimeOffset upperBound = DateTimeOffset.UtcNow.AddHours(48);

        // Assert
        Assert.Equal(new byte[] { 0x01, 0x02 }, protectedPayload);
        Assert.InRange(actualExpiration, lowerBound, upperBound);
    }

    [Fact]
    public void Unprotect_PayloadAsString()
    {
        // Arrange
        var futureDate = DateTimeOffset.UtcNow.AddYears(1);
        var controlExpiration = futureDate;
        var mockDataProtector = new Mock<ITimeLimitedDataProtector>();
        mockDataProtector.Setup(o => o.Unprotect(new byte[] { 0x01, 0x02 }, out controlExpiration)).Returns(Encoding.UTF8.GetBytes("this is plaintext"));

        // Act
        string unprotectedPayload = mockDataProtector.Object.Unprotect(SampleEncodedString, out var testExpiration);

        // Assert
        Assert.Equal("this is plaintext", unprotectedPayload);
        Assert.Equal(futureDate, testExpiration);
    }

    private static DateTime StringToDateTime(string input)
    {
        return DateTimeOffset.ParseExact(input, "u", CultureInfo.InvariantCulture).UtcDateTime;
    }
}
