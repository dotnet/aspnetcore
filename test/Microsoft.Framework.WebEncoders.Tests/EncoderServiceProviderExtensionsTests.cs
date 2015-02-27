// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Moq;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class EncoderServiceProviderExtensionsTests
    {
        [Fact]
        public void GetHtmlEncoder_ServiceProviderDoesNotHaveEncoder_UsesDefault()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;

            // Act
            var retVal = serviceProvider.GetHtmlEncoder();

            // Assert
            Assert.Same(HtmlEncoder.Default, retVal);
        }

        [Fact]
        public void GetHtmlEncoder_ServiceProviderHasEncoder_ReturnsRegisteredInstance()
        {
            // Arrange
            var expectedEncoder = new Mock<IHtmlEncoder>().Object;
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(o => o.GetService(typeof(IHtmlEncoder))).Returns(expectedEncoder);

            // Act
            var retVal = mockServiceProvider.Object.GetHtmlEncoder();

            // Assert
            Assert.Same(expectedEncoder, retVal);
        }

        [Fact]
        public void GetJavaScriptStringEncoder_ServiceProviderDoesNotHaveEncoder_UsesDefault()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;

            // Act
            var retVal = serviceProvider.GetJavaScriptStringEncoder();

            // Assert
            Assert.Same(JavaScriptStringEncoder.Default, retVal);
        }

        [Fact]
        public void GetJavaScriptStringEncoder_ServiceProviderHasEncoder_ReturnsRegisteredInstance()
        {
            // Arrange
            var expectedEncoder = new Mock<IJavaScriptStringEncoder>().Object;
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(o => o.GetService(typeof(IJavaScriptStringEncoder))).Returns(expectedEncoder);

            // Act
            var retVal = mockServiceProvider.Object.GetJavaScriptStringEncoder();

            // Assert
            Assert.Same(expectedEncoder, retVal);
        }

        [Fact]
        public void GetUrlEncoder_ServiceProviderDoesNotHaveEncoder_UsesDefault()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;

            // Act
            var retVal = serviceProvider.GetUrlEncoder();

            // Assert
            Assert.Same(UrlEncoder.Default, retVal);
        }

        [Fact]
        public void GetUrlEncoder_ServiceProviderHasEncoder_ReturnsRegisteredInstance()
        {
            // Arrange
            var expectedEncoder = new Mock<IUrlEncoder>().Object;
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(o => o.GetService(typeof(IUrlEncoder))).Returns(expectedEncoder);

            // Act
            var retVal = mockServiceProvider.Object.GetUrlEncoder();

            // Assert
            Assert.Same(expectedEncoder, retVal);
        }
    }
}
