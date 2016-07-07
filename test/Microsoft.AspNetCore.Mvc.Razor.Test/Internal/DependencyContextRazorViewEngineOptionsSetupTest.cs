// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;
using DependencyContextOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DependencyContextRazorViewEngineOptionsSetupTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetCompilationOptions_ReturnsDefaultOptionsIfApplicationNameIsNullOrEmpty(string name)
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.ApplicationName)
                .Returns(name);
            var setup = new TestableDependencyContextOptionsSetup(hostingEnvironment.Object);

            // Act
            var options = setup.GetCompilationOptionsPublic();

            // Assert
            Assert.Same(DependencyContextOptions.Default, options);
        }

        [Fact]
        public void GetCompilationOptions_ReturnsDefaultOptionsIfApplicationDoesNotHaveDependencyContext()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.ApplicationName)
                .Returns(typeof(Controller).GetTypeInfo().Assembly.GetName().Name);
            var setup = new TestableDependencyContextOptionsSetup(hostingEnvironment.Object);

            // Act
            var options = setup.GetCompilationOptionsPublic();

            // Assert
            Assert.Same(DependencyContextOptions.Default, options);
        }

        [Fact]
        public void GetCompilationOptions_ReturnsCompilationOptionsFromDependencyContext()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.ApplicationName)
                .Returns(GetType().GetTypeInfo().Assembly.GetName().Name);
            var setup = new TestableDependencyContextOptionsSetup(hostingEnvironment.Object);

            // Act
            var options = setup.GetCompilationOptionsPublic();

            // Assert
            Assert.Contains("SOME_TEST_DEFINE", options.Defines);
        }

        [Fact]
        public void Configure_UsesDefaultCompilationOptions()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            var setup = new DependencyContextRazorViewEngineOptionsSetup(hostingEnvironment.Object);
            var options = new RazorViewEngineOptions();

            // Act
            setup.Configure(options);

            // Assert
            var compilationOptions = options.CompilationOptions;
            var parseOptions = options.ParseOptions;
            Assert.False(compilationOptions.AllowUnsafe);
            Assert.Equal(ReportDiagnostic.Default, compilationOptions.GeneralDiagnosticOption);
            Assert.Equal(OptimizationLevel.Debug, compilationOptions.OptimizationLevel);
            Assert.Collection(compilationOptions.SpecificDiagnosticOptions.OrderBy(d => d.Key),
                item =>
                {
                    Assert.Equal("CS1701", item.Key);
                    Assert.Equal(ReportDiagnostic.Suppress, item.Value);
                },
                item =>
                {
                    Assert.Equal("CS1702", item.Key);
                    Assert.Equal(ReportDiagnostic.Suppress, item.Value);
                },
                item =>
                {
                    Assert.Equal("CS1705", item.Key);
                    Assert.Equal(ReportDiagnostic.Suppress, item.Value);
                });

            Assert.Empty(parseOptions.PreprocessorSymbolNames);
            Assert.Equal(LanguageVersion.CSharp6, parseOptions.LanguageVersion);
        }

        [Fact]
        public void Configure_SetsAllowUnsafe()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextOptions(
                new[] { "MyDefine" },
                languageVersion: null,
                platform: null,
                allowUnsafe: true,
                warningsAsErrors: null,
                optimize: null,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: null,
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var setup = new TestableDependencyContextOptionsSetup(dependencyContextOptions);
            var options = new RazorViewEngineOptions();

            // Act
            setup.Configure(options);

            // Assert
            Assert.True(options.CompilationOptions.AllowUnsafe);
            Assert.Equal(ReportDiagnostic.Default, options.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(OptimizationLevel.Debug, options.CompilationOptions.OptimizationLevel);
        }

        [Fact]
        public void Configure_SetsDiagnosticOption()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextOptions(
                new[] { "MyDefine" },
                languageVersion: null,
                platform: null,
                allowUnsafe: null,
                warningsAsErrors: true,
                optimize: null,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: null,
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var setup = new TestableDependencyContextOptionsSetup(dependencyContextOptions);
            var options = new RazorViewEngineOptions();

            // Act
            setup.Configure(options);

            // Assert
            Assert.False(options.CompilationOptions.AllowUnsafe);
            Assert.Equal(ReportDiagnostic.Error, options.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(OptimizationLevel.Debug, options.CompilationOptions.OptimizationLevel);
        }

        [Fact]
        public void Configure_SetsOptimizationLevel()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextOptions(
                new[] { "MyDefine" },
                languageVersion: null,
                platform: null,
                allowUnsafe: null,
                warningsAsErrors: null,
                optimize: true,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: null,
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var setup = new TestableDependencyContextOptionsSetup(dependencyContextOptions);
            var options = new RazorViewEngineOptions();

            // Act
            setup.Configure(options);

            // Assert
            Assert.False(options.CompilationOptions.AllowUnsafe);
            Assert.Equal(ReportDiagnostic.Default, options.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(OptimizationLevel.Release, options.CompilationOptions.OptimizationLevel);
        }

        [Fact]
        public void Configure_SetsLanguageVersion()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextOptions(
                new[] { "MyDefine" },
                languageVersion: "csharp4",
                platform: null,
                allowUnsafe: null,
                warningsAsErrors: null,
                optimize: true,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: null,
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var setup = new TestableDependencyContextOptionsSetup(dependencyContextOptions);
            var options = new RazorViewEngineOptions();

            // Act
            setup.Configure(options);

            // Assert
            Assert.Equal(LanguageVersion.CSharp4, options.ParseOptions.LanguageVersion);
        }

        [Fact]
        public void Configure_SetsDefines()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextOptions(
                new[] { "MyDefine" },
                languageVersion: "csharp4",
                platform: null,
                allowUnsafe: null,
                warningsAsErrors: null,
                optimize: true,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: null,
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var setup = new TestableDependencyContextOptionsSetup(dependencyContextOptions);
            var options = new RazorViewEngineOptions();

            // Act
            setup.Configure(options);

            // Assert
            Assert.Equal(new[] { "MyDefine" }, options.ParseOptions.PreprocessorSymbolNames);
        }

        [Fact]
        public void ConfigureAfterRazorViewEngineOptionsSetupIsExecuted_CorrectlySetsUpOptimizationLevel()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextOptions(
                new[] { "MyDefine" },
                languageVersion: null,
                platform: null,
                allowUnsafe: null,
                warningsAsErrors: null,
                optimize: true,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: null,
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var dependencyContextSetup = new TestableDependencyContextOptionsSetup(dependencyContextOptions);
            var options = new RazorViewEngineOptions();
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.EnvironmentName)
                .Returns("Development");
            var viewEngineSetup = new RazorViewEngineOptionsSetup(hostingEnvironment.Object);

            // Act
            viewEngineSetup.Configure(options);
            dependencyContextSetup.Configure(options);

            // Assert
            Assert.Equal(OptimizationLevel.Release, options.CompilationOptions.OptimizationLevel);
        }

        private class TestableDependencyContextOptionsSetup : DependencyContextRazorViewEngineOptionsSetup
        {
            private readonly DependencyContextOptions _options;

            public TestableDependencyContextOptionsSetup(IHostingEnvironment hostingEnvironment)
                : base(hostingEnvironment)
            {
            }

            public TestableDependencyContextOptionsSetup(DependencyContextOptions options)
                : base(Mock.Of<IHostingEnvironment>())
            {
                _options = options;
            }

            protected internal override DependencyContextOptions GetCompilationOptions()
            {
                return _options ?? base.GetCompilationOptions();
            }

            public DependencyContextOptions GetCompilationOptionsPublic() => base.GetCompilationOptions();
        }
    }
}
