// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
            var fileProvider = new Mock<IFileProvider>();
            var accessor = Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == fileProvider.Object);

            var templateEngine = new RazorTemplateEngine(
                RazorEngine.Create(),
                new FileProviderRazorProject(accessor));
            var options = Options.Create(new RazorPagesOptions());
            var changeProvider = new PageActionDescriptorChangeProvider(templateEngine, accessor, options);

            // Act
            var changeToken = changeProvider.GetChangeToken();

            // Assert
            fileProvider.Verify(f => f.Watch("/Pages/**/*.cshtml"));
        }

        [Theory]
        [InlineData("/pages-base-dir")]
        [InlineData("/pages-base-dir/")]
        public void GetChangeToken_WatchesAllCshtmlFilesUnderSpecifiedRootDirectory(string rootDirectory)
        {
            // Arrange
            var fileProvider = new Mock<IFileProvider>();
            var accessor = Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == fileProvider.Object);

            var templateEngine = new RazorTemplateEngine(
                RazorEngine.Create(),
                new FileProviderRazorProject(accessor));
            var options = Options.Create(new RazorPagesOptions());
            options.Value.RootDirectory = rootDirectory;

            var changeProvider = new PageActionDescriptorChangeProvider(templateEngine, accessor, options);

            // Act
            var changeToken = changeProvider.GetChangeToken();

            // Assert
            fileProvider.Verify(f => f.Watch("/pages-base-dir/**/*.cshtml"));
        }

        [Fact]
        public void GetChangeToken_WatchesViewImportsOutsidePagesRoot()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var accessor = Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == fileProvider);

            var templateEngine = new RazorTemplateEngine(
                RazorEngine.Create(),
                new FileProviderRazorProject(accessor));
            templateEngine.Options.ImportsFileName = "_ViewImports.cshtml";
            var options = Options.Create(new RazorPagesOptions());
            options.Value.RootDirectory = "/dir1/dir2";

            var changeProvider = new PageActionDescriptorChangeProvider(templateEngine, accessor, options);

            // Act & Assert
            var compositeChangeToken = Assert.IsType<CompositeChangeToken>(changeProvider.GetChangeToken());
            Assert.Collection(compositeChangeToken.ChangeTokens,
                changeToken => Assert.Same(fileProvider.GetChangeToken("/dir1/_ViewImports.cshtml"), changeToken),
                changeToken => Assert.Same(fileProvider.GetChangeToken("/_ViewImports.cshtml"), changeToken),
                changeToken => Assert.Same(fileProvider.GetChangeToken("/dir1/dir2/**/*.cshtml"), changeToken));
        }
    }
}
