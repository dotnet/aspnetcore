// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class DataProtectionUtilityExtensionsTests
    {
        [Theory]
        [InlineData(" discriminator", "discriminator")] // normalized trim
        [InlineData("", null)] // app discriminator not null -> overrides app base path
        [InlineData(null, null)] // nothing provided at all
        public void GetApplicationUniqueIdentifier(string appDiscriminator, string expected)
        {
            // Arrange
            var mockAppDiscriminator = new Mock<IApplicationDiscriminator>();
            mockAppDiscriminator.Setup(o => o.Discriminator).Returns(appDiscriminator);
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(o => o.GetService(typeof(IApplicationDiscriminator))).Returns(mockAppDiscriminator.Object);

            // Act
            string actual = mockServiceProvider.Object.GetApplicationUniqueIdentifier();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetApplicationUniqueIdentifier_NoServiceProvider_ReturnsNull()
        {
            Assert.Null(((IServiceProvider)null).GetApplicationUniqueIdentifier());
        }
    }
}
