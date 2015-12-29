// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class DefaultRazorViewEngineFileProviderAccessorTest
    {
        [Fact]
        public void FileProvider_ReturnsInstanceIfExactlyOneFileProviderIsSpecified()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var options = new RazorViewEngineOptions();
            options.FileProviders.Add(fileProvider);
            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);
            var fileProviderAccessor = new DefaultRazorViewEngineFileProviderAccessor(optionsAccessor.Object);

            // Act
            var actual = fileProviderAccessor.FileProvider;

            // Assert
            Assert.Same(fileProvider, actual);
        }

        [Fact]
        public void FileProvider_ReturnsCompositeFileProviderIfNoInstancesAreRegistered()
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);
            var fileProviderAccessor = new DefaultRazorViewEngineFileProviderAccessor(optionsAccessor.Object);

            // Act
            var actual = fileProviderAccessor.FileProvider;

            // Assert
            Assert.IsType<CompositeFileProvider>(actual);
        }

        [Fact]
        public void FileProvider_ReturnsCompositeFileProviderIfMoreThanOneInstanceIsRegistered()
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            options.FileProviders.Add(new TestFileProvider());
            options.FileProviders.Add(new TestFileProvider());
            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);
            var fileProviderAccessor = new DefaultRazorViewEngineFileProviderAccessor(optionsAccessor.Object);

            // Act
            var actual = fileProviderAccessor.FileProvider;

            // Assert
            Assert.IsType<CompositeFileProvider>(actual);
        }
    }
}