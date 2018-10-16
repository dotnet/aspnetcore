// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class DataProtectionUtilityExtensionsTests
    {
        [Theory]
        [InlineData("app-path", "app-path")]
        [InlineData("app-path ", "app-path")] // normalized trim
        [InlineData("  ", null)] // normalized whitespace -> null
        [InlineData(null, null)] // nothing provided at all
        public void GetApplicationUniqueIdentifierFromHosting(string contentRootPath, string expected)
        {
            // Arrange
            var mockEnvironment = new Mock<IHostingEnvironment>();
            mockEnvironment.Setup(o => o.ContentRootPath).Returns(contentRootPath);

            var services = new ServiceCollection()
                .AddSingleton(mockEnvironment.Object)
                .AddDataProtection()
                .Services
                .BuildServiceProvider();

            // Act
            var actual = services.GetApplicationUniqueIdentifier();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(" discriminator ", "discriminator")]
        [InlineData(" discriminator", "discriminator")] // normalized trim
        [InlineData("  ", null)] // normalized whitespace -> null
        [InlineData(null, null)] // nothing provided at all
        public void GetApplicationIdentifierFromApplicationDiscriminator(string discriminator, string expected)
        {
            // Arrange
            var mockAppDiscriminator = new Mock<IApplicationDiscriminator>();
            mockAppDiscriminator.Setup(o => o.Discriminator).Returns(discriminator);

            var mockEnvironment = new Mock<IHostingEnvironment>();
            mockEnvironment.SetupGet(o => o.ContentRootPath).Throws(new InvalidOperationException("Hosting environment should not be checked"));

            var services = new ServiceCollection()
                .AddSingleton(mockEnvironment.Object)
                .AddSingleton(mockAppDiscriminator.Object)
                .AddDataProtection()
                .Services
                .BuildServiceProvider();

            // Act
            var actual = services.GetApplicationUniqueIdentifier();

            // Assert
            Assert.Equal(expected, actual);
            mockAppDiscriminator.VerifyAll();
        }

        [Fact]
        public void GetApplicationUniqueIdentifier_NoServiceProvider_ReturnsNull()
        {
            Assert.Null(((IServiceProvider)null).GetApplicationUniqueIdentifier());
        }

        [Fact]
        public void GetApplicationUniqueIdentifier_NoHostingEnvironment_ReturnsNull()
        {
            // arrange
            var services = new ServiceCollection()
              .AddDataProtection()
              .Services
              .BuildServiceProvider();

            // act & assert
            Assert.Null(services.GetApplicationUniqueIdentifier());
        }
    }
}
