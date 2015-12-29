// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.PlatformAbstractions;
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
            var hostingEnv = new Mock<IHostingEnvironment>();
            hostingEnv.SetupGet(e => e.EnvironmentName)
                .Returns("Development");
            var optionsSetup = new RazorViewEngineOptionsSetup(appEnv.Object, hostingEnv.Object);

            // Act
            optionsSetup.Configure(options);

            // Assert
            var fileProvider = Assert.Single(options.FileProviders);
            Assert.IsType<PhysicalFileProvider>(fileProvider);
        }

        [Theory]
        [InlineData("Development", "DEBUG")]
        [InlineData("Staging", "RELEASE")]
        [InlineData("Production", "RELEASE")]
        public void RazorViewEngineOptionsSetup_SetsPreprocessorSymbols(string environment, string expectedConfiguration)
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var appEnv = new Mock<IApplicationEnvironment>();
            appEnv.SetupGet(e => e.ApplicationBasePath)
                  .Returns(Directory.GetCurrentDirectory());
            var hostingEnv = new Mock<IHostingEnvironment>();
            hostingEnv.SetupGet(e => e.EnvironmentName)
                  .Returns(environment);
            var optionsSetup = new RazorViewEngineOptionsSetup(appEnv.Object, hostingEnv.Object);

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Equal(new[] { expectedConfiguration }, options.ParseOptions.PreprocessorSymbolNames);
        }

        [Theory]
        [InlineData("Development", OptimizationLevel.Debug)]
        [InlineData("Staging", OptimizationLevel.Release)]
        [InlineData("Production", OptimizationLevel.Release)]
        public void RazorViewEngineOptionsSetup_SetsOptimizationLevel(
            string environment,
            OptimizationLevel expectedOptimizationLevel)
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var appEnv = new Mock<IApplicationEnvironment>();
            appEnv.SetupGet(e => e.ApplicationBasePath)
                  .Returns(Directory.GetCurrentDirectory());
            var hostingEnv = new Mock<IHostingEnvironment>();
            hostingEnv.SetupGet(e => e.EnvironmentName)
                  .Returns(environment);
            var optionsSetup = new RazorViewEngineOptionsSetup(appEnv.Object, hostingEnv.Object);

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Equal(expectedOptimizationLevel, options.CompilationOptions.OptimizationLevel);
        }
    }
}