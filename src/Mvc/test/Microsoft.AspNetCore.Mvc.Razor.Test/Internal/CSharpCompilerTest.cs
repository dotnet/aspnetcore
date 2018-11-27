// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;
using DependencyContextCompilationOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class CSharpCompilerTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetCompilationOptions_ReturnsDefaultOptionsIfApplicationNameIsNullOrEmpty(string name)
        {
            // Arrange
            var hostingEnvironment = Mock.Of<IHostingEnvironment>(e => e.ApplicationName == name);
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var compiler = new CSharpCompiler(referenceManager, hostingEnvironment);

            // Act
            var options = compiler.GetDependencyContextCompilationOptions();

            // Assert
            Assert.Same(DependencyContextCompilationOptions.Default, options);
        }

        [Fact]
        public void GetCompilationOptions_ReturnsDefaultOptionsIfApplicationDoesNotHaveDependencyContext()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.ApplicationName)
                .Returns(typeof(Controller).GetTypeInfo().Assembly.GetName().Name);
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var compiler = new CSharpCompiler(referenceManager, hostingEnvironment.Object);

            // Act
            var options = compiler.GetDependencyContextCompilationOptions();

            // Assert
            Assert.Same(DependencyContextCompilationOptions.Default, options);
        }

        [Fact]
        public void Constructor_SetsCompilationOptionsFromDependencyContext()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.ApplicationName)
                .Returns(GetType().GetTypeInfo().Assembly.GetName().Name);
            var compiler = new CSharpCompiler(Mock.Of<RazorReferenceManager>(), hostingEnvironment.Object);

            // Act & Assert
            var parseOptions = compiler.ParseOptions;
            Assert.Contains("SOME_TEST_DEFINE", parseOptions.PreprocessorSymbolNames);
        }

        [Theory]
        [InlineData("Development", OptimizationLevel.Debug)]
        [InlineData("Staging", OptimizationLevel.Release)]
        [InlineData("Production", OptimizationLevel.Release)]
        public void Constructor_SetsOptimizationLevelBasedOnEnvironment(
            string environment,
            OptimizationLevel expected)
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.EnvironmentName)
                  .Returns(environment);
            var compiler = new CSharpCompiler(Mock.Of<RazorReferenceManager>(), hostingEnvironment.Object);

            // Act & Assert
            var compilationOptions = compiler.CSharpCompilationOptions;
            Assert.Equal(expected, compilationOptions.OptimizationLevel);
        }

        [Theory]
        [InlineData("Development", "DEBUG")]
        [InlineData("Staging", "RELEASE")]
        [InlineData("Production", "RELEASE")]
        public void EnsureOptions_SetsPreprocessorSymbols(string environment, string expectedConfiguration)
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(e => e.EnvironmentName)
                  .Returns(environment);
            var compiler = new CSharpCompiler(Mock.Of<RazorReferenceManager>(), hostingEnvironment.Object);

            // Act & Assert
            var parseOptions = compiler.ParseOptions;
            Assert.Equal(new[] { expectedConfiguration }, parseOptions.PreprocessorSymbolNames);
        }

        [Fact]
        public void EnsureOptions_ConfiguresDefaultCompilationOptions()
        {
            // Arrange
            var hostingEnvironment = Mock.Of<IHostingEnvironment>(h => h.EnvironmentName == "Development");
            var compiler = new CSharpCompiler(Mock.Of<RazorReferenceManager>(), hostingEnvironment);

            // Act & Assert
            var compilationOptions = compiler.CSharpCompilationOptions;
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
        }

        [Fact]
        public void EnsureOptions_ConfiguresDefaultParseOptions()
        {
            // Arrange
            var hostingEnvironment = Mock.Of<IHostingEnvironment>(h => h.EnvironmentName == "Development");
            var compiler = new CSharpCompiler(Mock.Of<RazorReferenceManager>(), hostingEnvironment);

            // Act & Assert
            var parseOptions = compiler.ParseOptions;
            Assert.Equal(LanguageVersion.CSharp7, parseOptions.LanguageVersion);
            Assert.Equal(new[] { "DEBUG" }, parseOptions.PreprocessorSymbolNames);
        }

        [Fact]
        public void Constructor_ConfiguresLanguageVersion()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
                new[] { "MyDefine" },
                languageVersion: "7.1",
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
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            var compilationOptions = compiler.ParseOptions;
            Assert.Equal(LanguageVersion.CSharp7_1, compilationOptions.LanguageVersion);
        }


        [Fact]
        public void EmitOptions_ReadsDebugTypeFromDependencyContext()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
                new[] { "MyDefine" },
                languageVersion: "7.1",
                platform: null,
                allowUnsafe: true,
                warningsAsErrors: null,
                optimize: null,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: "portable",
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            var emitOptions = compiler.EmitOptions;
            Assert.Equal(DebugInformationFormat.PortablePdb, emitOptions.DebugInformationFormat);
            Assert.True(compiler.EmitPdb);
        }

        [Fact]
        public void EmitOptions_SetsDebugInformationFormatToPortable_WhenDebugTypeIsEmbedded()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
                new[] { "MyDefine" },
                languageVersion: "7.1",
                platform: null,
                allowUnsafe: true,
                warningsAsErrors: null,
                optimize: null,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: "embedded",
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            var emitOptions = compiler.EmitOptions;
            Assert.Equal(DebugInformationFormat.PortablePdb, emitOptions.DebugInformationFormat);
            Assert.True(compiler.EmitPdb);
        }

        [Fact]
        public void EmitOptions_DoesNotSetEmitPdb_IfDebugTypeIsNone()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
                new[] { "MyDefine" },
                languageVersion: "7.1",
                platform: null,
                allowUnsafe: true,
                warningsAsErrors: null,
                optimize: null,
                keyFile: null,
                delaySign: null,
                publicSign: null,
                debugType: "none",
                emitEntryPoint: null,
                generateXmlDocumentation: null);
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            Assert.False(compiler.EmitPdb);
        }

        [Fact]
        public void Constructor_ConfiguresAllowUnsafe()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
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
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            var compilationOptions = compiler.CSharpCompilationOptions;
            Assert.True(compilationOptions.AllowUnsafe);
        }

        [Fact]
        public void Constructor_SetsDiagnosticOption()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
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
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            var compilationOptions = compiler.CSharpCompilationOptions;
            Assert.Equal(ReportDiagnostic.Error, compilationOptions.GeneralDiagnosticOption);
        }

        [Fact]
        public void Constructor_SetsOptimizationLevel()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
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
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            var compilationOptions = compiler.CSharpCompilationOptions;
            Assert.Equal(OptimizationLevel.Release, compilationOptions.OptimizationLevel);
        }

        [Fact]
        public void Constructor_SetsDefines()
        {
            // Arrange
            var dependencyContextOptions = new DependencyContextCompilationOptions(
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
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();

            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act & Assert
            var parseOptions = compiler.ParseOptions;
            Assert.Equal(new[] { "MyDefine", "RELEASE" }, parseOptions.PreprocessorSymbolNames);
        }

        [Fact]
        public void Compile_UsesApplicationsCompilationSettings_ForParsingAndCompilation()
        {
            // Arrange
            var content = "public class Test {}";
            var define = "MY_CUSTOM_DEFINE";
            var dependencyContextOptions = new DependencyContextCompilationOptions(
                new[] { define },
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
            var referenceManager = Mock.Of<RazorReferenceManager>();
            var hostingEnvironment = Mock.Of<IHostingEnvironment>();
            var compiler = new TestCSharpCompiler(referenceManager, hostingEnvironment, dependencyContextOptions);

            // Act
            var syntaxTree = compiler.CreateSyntaxTree(SourceText.From(content));

            // Assert
            Assert.Contains(define, syntaxTree.Options.PreprocessorSymbolNames);
        }

        private class TestCSharpCompiler : CSharpCompiler
        {
            private readonly DependencyContextCompilationOptions _options;

            public TestCSharpCompiler(
                RazorReferenceManager referenceManager,
                IHostingEnvironment hostingEnvironment,
                DependencyContextCompilationOptions options)
                : base(referenceManager, hostingEnvironment)
            {
                _options = options;
            }

            protected internal override DependencyContextCompilationOptions GetDependencyContextCompilationOptions()
                => _options;
        }
    }
}
