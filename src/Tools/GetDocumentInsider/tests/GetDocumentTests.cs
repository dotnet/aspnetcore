// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;
using Microsoft.Extensions.ApiDescription.Tool.Commands;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.ApiDescription.Tool.Tests;

public class GetDocumentTests(ITestOutputHelper output)
{
    private readonly TestConsole _console = new(output);
    private readonly string _testAppAssembly = typeof(GetDocumentSample.Program).Assembly.Location;
    private readonly string _testAppProject = "Sample";
    private readonly string _testAppFrameworkMoniker = typeof(Program).Assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
    private readonly string _toolsDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);

    [Fact]
    public void GetDocument_Works()
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache")
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert
        using var stream = new MemoryStream(File.ReadAllBytes(Path.Combine(outputPath.FullName, "Sample.json")));
        var result = OpenApiDocument.Load(stream, "json");
        // TODO: Needs https://github.com/microsoft/OpenAPI.NET/issues/2055 to be fixed
        // Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi3_1, result.Diagnostic.SpecificationVersion);
        Assert.Equal("GetDocumentSample | v1", result.Document.Info.Title);
    }

    [Fact]
    public void GetDocument_WithOpenApiVersion_Works()
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache"),
            "--openapi-version", "OpenApi2_0"
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert
        using var stream = new MemoryStream(File.ReadAllBytes(Path.Combine(outputPath.FullName, "Sample.json")));
        var result = OpenApiDocument.Load(stream, "json");
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi2_0, result.Diagnostic.SpecificationVersion);
        Assert.Equal("GetDocumentSample | v1", result.Document.Info.Title);
    }

    [Fact]
    public void GetDocument_WithInvalidOpenApiVersion_Errors()
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache"),
            "--openapi-version", "OpenApi4_0"
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert that error was produced and files were generated with v3.
        Assert.Contains("Invalid OpenAPI spec version 'OpenApi4_0' provided. Falling back to default: v3.0.", _console.GetOutput());
        using var stream = new MemoryStream(File.ReadAllBytes(Path.Combine(outputPath.FullName, "Sample.json")));
        var result = OpenApiDocument.Load(stream, "json");
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi3_1, result.Diagnostic.SpecificationVersion);
        Assert.Equal("GetDocumentSample | v1", result.Document.Info.Title);
    }

    [Fact]
    public void GetDocument_WithDocumentName_Works()
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache"),
            "--document-name", "internal"
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert
        var expectedDocumentPath = Path.Combine(outputPath.FullName, "Sample_internal.json");

        // There should only be one document when document name is specified
        var documentNames = Directory.GetFiles(outputPath.FullName).Where(documentName => documentName.EndsWith(".json", StringComparison.Ordinal)).ToList();
        Assert.Single(documentNames);
        Assert.Contains(expectedDocumentPath, documentNames);

        using var stream = new MemoryStream(File.ReadAllBytes(Path.Combine(outputPath.FullName, "Sample_internal.json")));
        var result = OpenApiDocument.Load(stream, "json");
        Assert.Empty(result.Diagnostic.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi3_1, result.Diagnostic.SpecificationVersion);
        // Document name in the title gives us a clue that the correct document was actually resolved
        Assert.Equal("GetDocumentSample | internal", result.Document.Info.Title);
    }

    [Fact]
    public void GetDocument_WithInvalidDocumentName_Errors()
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache"),
            "--document-name", "invalid"
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert that error was produced and no files were generated
        Assert.Contains("Document with name 'invalid' not found.", _console.GetOutput());
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "Sample.json")));
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "Sample_internal.json")));
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "Sample_invalid.json")));
    }

    [Theory]
    [InlineData("customFileName")]
    [InlineData("custom-File_Name")]
    [InlineData("custom_File-Name")]
    [InlineData("custom1File2Name")]
    public void GetDocument_WithValidFileName_Works(string fileName)
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache"),
            "--file-name", fileName
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert
        Assert.True(File.Exists(Path.Combine(outputPath.FullName, $"{fileName}.json")));
        Assert.True(File.Exists(Path.Combine(outputPath.FullName, $"{fileName}_internal.json")));
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "Sample.json")));
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "Sample_internal.json")));
    }

    [Theory]
    [InlineData("customFile=ù^*Name")]
    [InlineData("&$*")]
    public void GetDocument_WithInvalideFileName_Errors(string fileName)
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache"),
            "--file-name", fileName
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert

        Assert.Contains("FileName format invalid, only Alphanumeric and \"_ -\" authorized", _console.GetOutput());
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, $"{fileName}.json")));
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "Sample.json")));
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "Sample_internal.json")));
    }

    [Fact]
    public void GetDocument_WithEmptyFileName_Works()
    {
        // Arrange
        var outputPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var app = new Program(_console);

        // Act
        app.Run([
            "--assembly", _testAppAssembly,
            "--project", _testAppProject,
            "--framework", _testAppFrameworkMoniker,
            "--tools-directory", _toolsDirectory,
            "--output", outputPath.FullName,
            "--file-list", Path.Combine(outputPath.FullName, "file-list.cache"),
            "--file-name", ""
        ], new GetDocumentCommand(_console), throwOnUnexpectedArg: false);

        // Assert
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, ".json")));
        Assert.False(File.Exists(Path.Combine(outputPath.FullName, "_internal.json")));
        Assert.True(File.Exists(Path.Combine(outputPath.FullName, "Sample.json")));
        Assert.True(File.Exists(Path.Combine(outputPath.FullName, "Sample_internal.json")));
    }
}
