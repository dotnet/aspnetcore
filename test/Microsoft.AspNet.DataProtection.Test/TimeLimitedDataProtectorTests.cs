// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Moq;
using Xunit;

namespace Microsoft.AspNet.DataProtection.Test
{
    public class TimeLimitedDataProtectorTests
    {
        [Fact]
        public void CreateProtector_And_Protect()
        {
            // Arrange
            // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
            DateTimeOffset expiration = new DateTimeOffset(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Mock<IDataProtector> innerProtectorMock = new Mock<IDataProtector>();
            innerProtectorMock.Setup(o => o.Protect(new byte[] { 0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0x10, 0x11 });
            Mock<IDataProtector> outerProtectorMock = new Mock<IDataProtector>();
            outerProtectorMock.Setup(p => p.CreateProtector("new purpose")).Returns(innerProtectorMock.Object);

            // Act
            var timeLimitedProtector = new TimeLimitedDataProtector(outerProtectorMock.Object);
            var subProtector = timeLimitedProtector.CreateProtector("new purpose");
            var protectedPayload = subProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, expiration);

            // Assert
            Assert.Equal(new byte[] { 0x10, 0x11 }, protectedPayload);
        }

        [Fact]
        public void ExpiredData_Fails()
        {
            // Arrange
            var timeLimitedProtector = CreateEphemeralTimeLimitedProtector();
            var expiration = DateTimeOffset.UtcNow.AddYears(-1);

            // Act & assert
            var protectedData = timeLimitedProtector.Protect(new byte[] { 0x04, 0x08, 0x0c }, expiration);
            Assert.Throws<CryptographicException>(() =>
            {
                timeLimitedProtector.Unprotect(protectedData);
            });
        }

        [Fact]
        public void GoodData_RoundTrips()
        {
            // Arrange
            var timeLimitedProtector = CreateEphemeralTimeLimitedProtector();
            var expectedExpiration = DateTimeOffset.UtcNow.AddYears(1);

            // Act
            var protectedData = timeLimitedProtector.Protect(new byte[] { 0x04, 0x08, 0x0c }, expectedExpiration);
            DateTimeOffset actualExpiration;
            var unprotectedData = timeLimitedProtector.Unprotect(protectedData, out actualExpiration);

            // Assert
            Assert.Equal(new byte[] { 0x04, 0x08, 0x0c }, unprotectedData);
            Assert.Equal(expectedExpiration, actualExpiration);
        }

        [Fact]
        public void Protect_NoExpiration_UsesDateTimeOffsetMaxValue()
        {
            // Should pass DateTimeOffset.MaxValue (utc ticks = 0x2bca2875f4373fff) if no expiration date specified

            // Arrange
            Mock<IDataProtector> innerProtectorMock = new Mock<IDataProtector>();
            innerProtectorMock.Setup(o => o.Protect(new byte[] { 0x2b, 0xca, 0x28, 0x75, 0xf4, 0x37, 0x3f, 0xff,0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0x10, 0x11 });

            // Act
            var timeLimitedProtector = new TimeLimitedDataProtector(innerProtectorMock.Object);
            var protectedPayload = timeLimitedProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            // Assert
            Assert.Equal(new byte[] { 0x10, 0x11 }, protectedPayload);
        }

        private static TimeLimitedDataProtector CreateEphemeralTimeLimitedProtector()
        {
            return new TimeLimitedDataProtector(new EphemeralDataProtectionProvider().CreateProtector("purpose"));
        }
    }
}
