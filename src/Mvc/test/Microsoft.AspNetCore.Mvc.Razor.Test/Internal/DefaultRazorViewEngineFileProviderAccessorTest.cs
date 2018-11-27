// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DefaultRazorViewEngineFileProviderAccessorTest
    {
        [Fact]
        public void FileProvider_ReturnsInstance_IfExactlyOneFileProviderIsRegistered()
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
        public void FileProvider_ReturnsNullFileProvider_IfNoInstancesAreRegistered()
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);
            var fileProviderAccessor = new DefaultRazorViewEngineFileProviderAccessor(optionsAccessor.Object);

            // Act
            var actual = fileProviderAccessor.FileProvider;

            // Assert
            Assert.IsType<NullFileProvider>(actual);
        }

        [Fact]
        public void FileProvider_ReturnsCompositeFileProvider_IfMoreThanOneInstanceIsRegistered()
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