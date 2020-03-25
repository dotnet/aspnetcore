// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    public class WebAssemblyLoggerFactoryTest
    {
        [Fact]
        public void CreateLogger_ThrowsAfterDisposed()
        {
            // Arrange
            var factory = new WebAssemblyLoggerFactory();

            // Act
            factory.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateLogger("d"));
        }

        [Fact]
        public void AddProvider_ThrowsAfterDisposed()
        {
            // Arrange
            var factory = new WebAssemblyLoggerFactory();
            var provider = new Mock<ILoggerProvider>();

            // Act
            factory.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => ((ILoggerFactory)factory).AddProvider(provider.Object));
        }

        [Fact]
        public void CanAddProviders()
        {
            // Arrange
            var factory = new WebAssemblyLoggerFactory();
            var provider1 = new Mock<ILoggerProvider>();
            var provider2 = new Mock<ILoggerProvider>();

            // Act
            var exception1 = Record.Exception(() => factory.AddProvider(provider1.Object));
            var exception2 = Record.Exception(() => factory.AddProvider(provider2.Object));

            // Assert
            Assert.Null(exception1);
            Assert.Null(exception2);
        }
    }
}
