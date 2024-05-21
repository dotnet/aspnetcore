// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;
using Microsoft.Extensions.ApiDescription.Tool.Commands;
using Microsoft.OpenApi.Readers;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.OpenApi;

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
        var document = new OpenApiStreamReader().Read(File.OpenRead(Path.Combine(outputPath.FullName, "Sample.json")), out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi3_0, diagnostic.SpecificationVersion);
        Assert.Equal("GetDocumentSample | v1", document.Info.Title);
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
        var document = new OpenApiStreamReader().Read(File.OpenRead(Path.Combine(outputPath.FullName, "Sample.json")), out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi2_0, diagnostic.SpecificationVersion);
        Assert.Equal("GetDocumentSample | v1", document.Info.Title);
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
        var document = new OpenApiStreamReader().Read(File.OpenRead(Path.Combine(outputPath.FullName, "Sample_internal.json")), out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi3_0, diagnostic.SpecificationVersion);
        // Document name in the title gives us a clue that the correct document was actually resolved
        Assert.Equal("GetDocumentSample | internal", document.Info.Title);
    }
}
