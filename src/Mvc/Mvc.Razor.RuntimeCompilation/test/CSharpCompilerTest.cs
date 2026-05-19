// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Moq;
using DependencyContextCompilationOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

public class CSharpCompilerTest
{
    private readonly RazorReferenceManager ReferenceManager = new TestRazorReferenceManager();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetCompilationOptions_ReturnsDefaultOptionsIfApplicationNameIsNullOrEmpty(string name)
    {
        // Arrange
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>(e => e.ApplicationName == name);
        var compiler = new CSharpCompiler(ReferenceManager, hostingEnvironment);

        // Act
        var options = compiler.GetDependencyContextCompilationOptions();

        // Assert
        Assert.Same(DependencyContextCompilationOptions.Default, options);
    }

    [Fact]
    public void GetCompilationOptions_ReturnsDefaultOptionsIfApplicationDoesNotHaveDependencyContext()
    {
        // Arrange
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();
        var compiler = new CSharpCompiler(ReferenceManager, hostingEnvironment);

        // Act
        var options = compiler.GetDependencyContextCompilationOptions();

        // Assert
        Assert.Same(DependencyContextCompilationOptions.Default, options);
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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.SetupGet(e => e.EnvironmentName)
              .Returns(environment);
        var compiler = new CSharpCompiler(ReferenceManager, hostingEnvironment.Object);

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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.SetupGet(e => e.EnvironmentName)
              .Returns(environment);
        var compiler = new CSharpCompiler(ReferenceManager, hostingEnvironment.Object);

        // Act & Assert
        var parseOptions = compiler.ParseOptions;
        Assert.Equal(new[] { expectedConfiguration }, parseOptions.PreprocessorSymbolNames);
    }

    [Fact]
    public void EnsureOptions_ConfiguresDefaultCompilationOptions()
    {
        // Arrange
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>(h => h.EnvironmentName == "Development");
        var compiler = new CSharpCompiler(ReferenceManager, hostingEnvironment);

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
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>(h => h.EnvironmentName == "Development");
        var compiler = new CSharpCompiler(ReferenceManager, hostingEnvironment);

        // Act & Assert
        var parseOptions = compiler.ParseOptions;
        Assert.Equal(LanguageVersion.CSharp8, parseOptions.LanguageVersion);
        Assert.Equal(new[] { "DEBUG" }, parseOptions.PreprocessorSymbolNames);
    }

    [Fact]
    public void Constructor_ConfiguresPreprocessorSymbolNames()
    {
        // Arrange
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();
        var dependencyContextOptions = GetDependencyContextCompilationOptions("SOME_TEST_DEFINE");

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        var parseOptions = compiler.ParseOptions;
        Assert.Contains("SOME_TEST_DEFINE", parseOptions.PreprocessorSymbolNames);
    }

    [Fact]
    public void Constructor_ConfiguresLanguageVersion()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions(languageVersion: "7.1");
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        var compilationOptions = compiler.ParseOptions;
        Assert.Equal(LanguageVersion.CSharp7_1, compilationOptions.LanguageVersion);
    }

    [Fact]
    public void EmitOptions_ReadsDebugTypeFromDependencyContext()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions(debugType: "portable");
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        var emitOptions = compiler.EmitOptions;
        Assert.Equal(DebugInformationFormat.PortablePdb, emitOptions.DebugInformationFormat);
        Assert.True(compiler.EmitPdb);
    }

    [Fact]
    public void EmitOptions_SetsDebugInformationFormatToPortable_WhenDebugTypeIsEmbedded()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions(debugType: "embedded");
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        var emitOptions = compiler.EmitOptions;
        Assert.Equal(DebugInformationFormat.PortablePdb, emitOptions.DebugInformationFormat);
        Assert.True(compiler.EmitPdb);
    }

    [Fact]
    public void EmitOptions_DoesNotSetEmitPdb_IfDebugTypeIsNone()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions(debugType: "none");
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        Assert.False(compiler.EmitPdb);
    }

    [Fact]
    public void Constructor_ConfiguresAllowUnsafe()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions(allowUnsafe: true);
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        var compilationOptions = compiler.CSharpCompilationOptions;
        Assert.True(compilationOptions.AllowUnsafe);
    }

    [Fact]
    public void Constructor_SetsDiagnosticOption()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions(warningsAsErrors: true);
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        var compilationOptions = compiler.CSharpCompilationOptions;
        Assert.Equal(ReportDiagnostic.Error, compilationOptions.GeneralDiagnosticOption);
    }

    [Fact]
    public void Constructor_SetsOptimizationLevel()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions(optimize: true);
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();

        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act & Assert
        var compilationOptions = compiler.CSharpCompilationOptions;
        Assert.Equal(OptimizationLevel.Release, compilationOptions.OptimizationLevel);
    }

    [Fact]
    public void Constructor_SetsDefines()
    {
        // Arrange
        var dependencyContextOptions = GetDependencyContextCompilationOptions("MyDefine");
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();
        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

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
        var dependencyContextOptions = GetDependencyContextCompilationOptions(define);
        var hostingEnvironment = Mock.Of<IWebHostEnvironment>();
        var compiler = new TestCSharpCompiler(ReferenceManager, hostingEnvironment, dependencyContextOptions);

        // Act
        var syntaxTree = compiler.CreateSyntaxTree(SourceText.From(content));

        // Assert
        Assert.Contains(define, syntaxTree.Options.PreprocessorSymbolNames);
    }

    private static DependencyContextCompilationOptions GetDependencyContextCompilationOptions(
        string define = null,
        string languageVersion = null,
        string platform = null,
        bool? allowUnsafe = null,
        bool? warningsAsErrors = null,
        bool? optimize = null,
        string keyFile = null,
        bool? delaySign = null,
        bool? publicSign = null,
        string debugType = null)
    {
        return new DependencyContextCompilationOptions(
            new[] { define },
            languageVersion,
            platform,
            allowUnsafe,
            warningsAsErrors,
            optimize,
            keyFile,
            delaySign,
            publicSign,
            debugType,
            emitEntryPoint: null,
            generateXmlDocumentation: null);
    }

    private class TestCSharpCompiler : CSharpCompiler
    {
        private readonly DependencyContextCompilationOptions _options;

        public TestCSharpCompiler(
            RazorReferenceManager referenceManager,
            IWebHostEnvironment hostingEnvironment,
            DependencyContextCompilationOptions options)
            : base(referenceManager, hostingEnvironment)
        {
            _options = options;
        }

        protected internal override DependencyContextCompilationOptions GetDependencyContextCompilationOptions()
            => _options;
    }
}
