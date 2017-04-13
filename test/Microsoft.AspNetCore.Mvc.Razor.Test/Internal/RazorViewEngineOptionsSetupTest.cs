// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
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
            hostingEnv.SetupGet(e => e.EnvironmentName)
                .Returns("Development");
#pragma warning disable 0618
            var optionsSetup = new RazorViewEngineOptionsSetup(hostingEnv.Object);
#pragma warning restore 0618

            // Act
            optionsSetup.Configure(options);

            // Assert
            var fileProvider = Assert.Single(options.FileProviders);
            Assert.Same(expected, fileProvider);
        }

        [Theory]
        [InlineData("Development", "DEBUG")]
        [InlineData("Staging", "RELEASE")]
        [InlineData("Production", "RELEASE")]
        public void RazorViewEngineOptionsSetup_SetsPreprocessorSymbols(string environment, string expectedConfiguration)
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var hostingEnv = new Mock<IHostingEnvironment>();
            hostingEnv.SetupGet(e => e.EnvironmentName)
                  .Returns(environment);
#pragma warning disable 0618
            var optionsSetup = new RazorViewEngineOptionsSetup(hostingEnv.Object);
#pragma warning restore 0618

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
            var hostingEnv = new Mock<IHostingEnvironment>();
            hostingEnv.SetupGet(e => e.EnvironmentName)
                  .Returns(environment);
#pragma warning disable 0618
            var optionsSetup = new RazorViewEngineOptionsSetup(hostingEnv.Object);
#pragma warning restore 0618

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Equal(expectedOptimizationLevel, options.CompilationOptions.OptimizationLevel);
        }
    }
}