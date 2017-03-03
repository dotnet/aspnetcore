// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionDescriptorChangeProviderTest
    {
        [Fact]
        public void GetChangeToken_WatchesAllCshtmlFilesUnderFileSystemRoot()
        {
            // Arrange
            var options = new TestOptionsManager<RazorPagesOptions>();
            var fileProvider = new Mock<IFileProvider>();
            var fileProviderAccessor = new Mock<IRazorViewEngineFileProviderAccessor>();
            fileProviderAccessor
                .Setup(f => f.FileProvider)
                .Returns(fileProvider.Object);
            var changeProvider = new PageActionDescriptorChangeProvider(fileProviderAccessor.Object, options);

            // Act
            var changeToken = changeProvider.GetChangeToken();

            // Assert
            fileProvider.Verify(f => f.Watch("/**/*.cshtml"));
        }

        [Theory]
        [InlineData("/pages-base-dir")]
        [InlineData("/pages-base-dir/")]
        public void GetChangeToken_WatchesAllCshtmlFilesUnderSpecifiedRootDirectory(string rootDirectory)
        {
            // Arrange
            var options = new TestOptionsManager<RazorPagesOptions>();
            options.Value.RootDirectory = rootDirectory;
            var fileProvider = new Mock<IFileProvider>();
            var fileProviderAccessor = new Mock<IRazorViewEngineFileProviderAccessor>();
            fileProviderAccessor
                .Setup(f => f.FileProvider)
                .Returns(fileProvider.Object);
            var changeProvider = new PageActionDescriptorChangeProvider(fileProviderAccessor.Object, options);

            // Act
            var changeToken = changeProvider.GetChangeToken();

            // Assert
            fileProvider.Verify(f => f.Watch("/pages-base-dir/**/*.cshtml"));
        }
    }
}
