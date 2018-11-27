// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorViewEngineOptionsSetupTest
    {
        [Fact]
        public void RazorViewEngineOptionsSetup_SetsUpFileProvider()
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var expected = Mock.Of<IFileProvider>();
            var hostingEnv = new Mock<IHostingEnvironment>();
            hostingEnv.SetupGet(e => e.ContentRootFileProvider)
                .Returns(expected);
            var optionsSetup = new RazorViewEngineOptionsSetup(hostingEnv.Object);

            // Act
            optionsSetup.Configure(options);

            // Assert
            var fileProvider = Assert.Single(options.FileProviders);
            Assert.Same(expected, fileProvider);
        }
    }
}
