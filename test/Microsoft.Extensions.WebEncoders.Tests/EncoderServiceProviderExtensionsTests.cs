// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.WebEncoders
{
    public class EncoderServiceProviderExtensionsTests
    {
        [Fact]
        public void GetHtmlEncoder_ServiceProviderDoesNotHaveEncoder_UsesDefault()
        {
            // Arrange
            var serviceProvider = new TestServiceProvider();

            // Act
            var retVal = serviceProvider.GetHtmlEncoder();

            // Assert
            Assert.Same(HtmlEncoder.Default, retVal);
        }

        [Fact]
        public void GetHtmlEncoder_ServiceProviderHasEncoder_ReturnsRegisteredInstance()
        {
            // Arrange
            var expectedEncoder = new HtmlEncoder();
            var serviceProvider = new TestServiceProvider() { Service = expectedEncoder };

            // Act
            var retVal = serviceProvider.GetHtmlEncoder();

            // Assert
            Assert.Same(expectedEncoder, retVal);
        }

        [Fact]
        public void GetJavaScriptStringEncoder_ServiceProviderDoesNotHaveEncoder_UsesDefault()
        {
            // Arrange
            var serviceProvider = new TestServiceProvider();

            // Act
            var retVal = serviceProvider.GetJavaScriptStringEncoder();

            // Assert
            Assert.Same(JavaScriptStringEncoder.Default, retVal);
        }

        [Fact]
        public void GetJavaScriptStringEncoder_ServiceProviderHasEncoder_ReturnsRegisteredInstance()
        {
            // Arrange
            var expectedEncoder = new JavaScriptStringEncoder();
            var serviceProvider = new TestServiceProvider() { Service = expectedEncoder };

            // Act
            var retVal = serviceProvider.GetJavaScriptStringEncoder();

            // Assert
            Assert.Same(expectedEncoder, retVal);
        }

        [Fact]
        public void GetUrlEncoder_ServiceProviderDoesNotHaveEncoder_UsesDefault()
        {
            // Arrange
            var serviceProvider = new TestServiceProvider();

            // Act
            var retVal = serviceProvider.GetUrlEncoder();

            // Assert
            Assert.Same(UrlEncoder.Default, retVal);
        }

        [Fact]
        public void GetUrlEncoder_ServiceProviderHasEncoder_ReturnsRegisteredInstance()
        {
            // Arrange
            var expectedEncoder = new UrlEncoder();
            var serviceProvider = new TestServiceProvider() { Service = expectedEncoder };

            // Act
            var retVal = serviceProvider.GetUrlEncoder();

            // Assert
            Assert.Same(expectedEncoder, retVal);
        }

        private class TestServiceProvider : IServiceProvider
        {
            public object Service { get; set; }

            public object GetService(Type serviceType)
            {
                return Service;
            }
        }
    }
}
