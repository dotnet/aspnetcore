// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.Runtime;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class RazorViewEngineOptionsSetupTest
    {
        [Fact]
        public void RazorViewEngineOptionsSetup_SetsUpFileProvider()
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var appEnv = new Mock<IApplicationEnvironment>();
            appEnv.SetupGet(e => e.ApplicationBasePath)
                  .Returns(Directory.GetCurrentDirectory());
            var optionsSetup = new RazorViewEngineOptionsSetup(appEnv.Object);

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.NotNull(options.FileProvider);
            Assert.IsType<PhysicalFileProvider>(options.FileProvider);
        }
    }
}