// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.OpenApi.Tests;
using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;

namespace Microsoft.DotNet.OpenApi.Remove.Tests;

public class OpenApiRemoveTests : OpenApiTestBase
{
    public OpenApiRemoveTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task OpenApi_Remove_File()
    {
        var nswagJsonFile = "openapi.json";
        _tempDir
            .WithCSharpProject("testproj")
            .WithTargetFrameworks(TestTFM)
            .Dir()
            .WithContentFile(nswagJsonFile)
            .WithContentFile("Startup.cs")
            .Create();

        var add = GetApplication();
        var run = add.Execute(new[] { "add", "file", nswagJsonFile });

        AssertNoErrors(run);

        // csproj contents
        var csproj = new FileInfo(Path.Join(_tempDir.Root, "testproj.csproj"));
        using (var csprojStream = csproj.OpenRead())
        using (var reader = new StreamReader(csprojStream))
        {
            var content = await reader.ReadToEndAsync();
            Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
            Assert.Contains($"<OpenApiReference Include=\"{nswagJsonFile}\"", content);
        }

        var remove = GetApplication();
        var removeRun = remove.Execute(new[] { "remove", nswagJsonFile });

        AssertNoErrors(removeRun);

        // csproj contents
        csproj = new FileInfo(Path.Join(_tempDir.Root, "testproj.csproj"));
        using (var csprojStream = csproj.OpenRead())
        using (var reader = new StreamReader(csprojStream))
        {
            var content = await reader.ReadToEndAsync();
            // Don't remove the package reference, they might have taken other dependencies on it
            Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
            Assert.DoesNotContain($"<OpenApiReference Include=\"{nswagJsonFile}\"", content);
        }
        Assert.False(File.Exists(Path.Combine(_tempDir.Root, nswagJsonFile)));
    }

    [Fact]
    public async Task OpenApi_Remove_ViaUrl()
    {
        _tempDir
            .WithCSharpProject("testproj")
            .WithTargetFrameworks(TestTFM)
            .Dir()
            .WithContentFile("Startup.cs")
            .Create();

        var add = GetApplication();
        var run = add.Execute(new[] { "add", "url", FakeOpenApiUrl });

        AssertNoErrors(run);

        // csproj contents
        var csproj = new FileInfo(Path.Join(_tempDir.Root, "testproj.csproj"));
        using (var csprojStream = csproj.OpenRead())
        using (var reader = new StreamReader(csprojStream))
        {
            var content = await reader.ReadToEndAsync();
            // Don't remove the package reference, they might have taken other dependencies on it
            Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
        }

        var remove = GetApplication();
        var removeRun = remove.Execute(new[] { "remove", FakeOpenApiUrl });

        AssertNoErrors(removeRun);

        // csproj contents
        csproj = new FileInfo(Path.Join(_tempDir.Root, "testproj.csproj"));
        using var removedCsprojStream = csproj.OpenRead();
        using var removedReader = new StreamReader(removedCsprojStream);
        var removedContent = await removedReader.ReadToEndAsync();
        // Don't remove the package reference, they might have taken other dependencies on it
        Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", removedContent);
        Assert.DoesNotContain($"<OpenApiReference", removedContent);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/12738")]
    public async Task OpenApi_Remove_Project()
    {
        _tempDir
           .WithCSharpProject("testproj")
           .WithTargetFrameworks(TestTFM)
           .Dir()
           .WithContentFile("Startup.cs")
           .Create();

        using var refProj = new TemporaryDirectory();
        var refProjName = "refProj";
        refProj
            .WithCSharpProject(refProjName)
            .WithTargetFrameworks(TestTFM)
            .Dir()
            .Create();

        var app = GetApplication();
        var refProjFile = Path.Join(refProj.Root, $"{refProjName}.csproj");
        var run = app.Execute(new[] { "add", "project", refProjFile });

        AssertNoErrors(run);

        // csproj contents
        using (var csprojStream = new FileInfo(Path.Join(_tempDir.Root, "testproj.csproj")).OpenRead())
        using (var reader = new StreamReader(csprojStream))
        {
            var content = await reader.ReadToEndAsync();
            Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
            Assert.Contains($"<OpenApiProjectReference Include=\"{refProjFile}\"", content);
        }

        var remove = GetApplication();
        run = app.Execute(new[] { "remove", refProjFile });

        AssertNoErrors(run);

        // csproj contents
        using (var csprojStream = new FileInfo(Path.Join(_tempDir.Root, "testproj.csproj")).OpenRead())
        using (var reader = new StreamReader(csprojStream))
        {
            var content = await reader.ReadToEndAsync();
            Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
            Assert.DoesNotContain($"<OpenApiProjectReference Include=\"{refProjFile}\"", content);
        }
    }

    [Fact]
    public async Task OpenApi_Remove_Multiple()
    {
        var nswagJsonFile = "openapi.json";
        var swagFile2 = "swag2.json";
        _tempDir
            .WithCSharpProject("testproj")
            .WithTargetFrameworks(TestTFM)
            .Dir()
            .WithContentFile(nswagJsonFile)
            .WithFile(swagFile2)
            .WithContentFile("Startup.cs")
            .Create();

        var add = GetApplication();
        var run = add.Execute(new[] { "add", "file", nswagJsonFile });

        AssertNoErrors(run);

        add = GetApplication();
        run = add.Execute(new[] { "add", "file", swagFile2 });

        AssertNoErrors(run);

        var remove = GetApplication();
        var removeRun = remove.Execute(new[] { "remove", nswagJsonFile, swagFile2 });

        AssertNoErrors(removeRun);

        // csproj contents
        var csproj = new FileInfo(Path.Join(_tempDir.Root, "testproj.csproj"));
        using (var csprojStream = csproj.OpenRead())
        using (var reader = new StreamReader(csprojStream))
        {
            var content = await reader.ReadToEndAsync();
            // Don't remove the package reference, they might have taken other dependencies on it
            Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
            Assert.DoesNotContain($"<OpenApiReference Include=\"{nswagJsonFile}\"", content);
        }
        Assert.False(File.Exists(Path.Combine(_tempDir.Root, nswagJsonFile)));
    }
}
