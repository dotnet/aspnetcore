// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class DataProtectionUtilityExtensionsTests
    {
        [Theory]
        [InlineData(" discriminator", "app-path ", "discriminator")] // normalized trim
        [InlineData("", "app-path", null)] // app discriminator not null -> overrides app base path
        [InlineData(null, "app-path ", "app-path")] // normalized trim
        [InlineData(null, "  ", null)] // normalized whitespace -> null
        [InlineData(null, null, null)] // nothing provided at all
        public void GetApplicationUniqueIdentifier(string appDiscriminator, string appBasePath, string expected)
        {
            // Arrange
            var mockAppDiscriminator = new Mock<IApplicationDiscriminator>();
            mockAppDiscriminator.Setup(o => o.Discriminator).Returns(appDiscriminator);
            var mockEnvironment = new Mock<IHostingEnvironment>();
            mockEnvironment.Setup(o => o.ContentRootPath).Returns(appBasePath);
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(o => o.GetService(typeof(IApplicationDiscriminator))).Returns(mockAppDiscriminator.Object);
            mockServiceProvider.Setup(o => o.GetService(typeof(IHostingEnvironment))).Returns(mockEnvironment.Object);

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
