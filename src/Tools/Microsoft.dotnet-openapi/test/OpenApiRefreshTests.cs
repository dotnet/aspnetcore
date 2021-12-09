// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.OpenApi.Tests;
using Xunit.Abstractions;

namespace Microsoft.DotNet.OpenApi.Refresh.Tests;

public class OpenApiRefreshTests : OpenApiTestBase
{
    public OpenApiRefreshTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task OpenApi_Refresh_Basic()
    {
        CreateBasicProject(withOpenApi: false);

        // Add <OpenApiReference/> to the project. Ignore initial filename.json content.
        var app = GetApplication();
        var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

        AssertNoErrors(run);

        // File will grow after the refresh.
        var expectedJsonPath = Path.Combine(_tempDir.Root, "filename.json");
        await File.WriteAllTextAsync(expectedJsonPath, "trash");

        var firstWriteTime = File.GetLastWriteTime(expectedJsonPath);

        await Task.Delay(TimeSpan.FromSeconds(1));

        app = GetApplication();
        run = app.Execute(new[] { "refresh", FakeOpenApiUrl });

        AssertNoErrors(run);

        var secondWriteTime = File.GetLastWriteTime(expectedJsonPath);
        Assert.True(firstWriteTime < secondWriteTime, $"File wasn't updated! {firstWriteTime} {secondWriteTime}");
        Assert.Equal(Content, await File.ReadAllTextAsync(expectedJsonPath), ignoreLineEndingDifferences: true);
    }

    // Regression test for #35767 scenario.
    [Fact]
    public async Task OpenApi_Refresh_MuchShorterFile()
    {
        CreateBasicProject(withOpenApi: false);

        // Add <OpenApiReference/> to the project. Ignore initial filename.json content.
        var app = GetApplication();
        var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

        AssertNoErrors(run);

        // File will shrink after the refresh.
        var expectedJsonPath = Path.Combine(_tempDir.Root, "filename.json");
        await File.WriteAllTextAsync(expectedJsonPath, PackageUrlContent);

        var firstWriteTime = File.GetLastWriteTime(expectedJsonPath);

        await Task.Delay(TimeSpan.FromSeconds(1));

        app = GetApplication();
        run = app.Execute(new[] { "refresh", FakeOpenApiUrl });

        AssertNoErrors(run);

        var secondWriteTime = File.GetLastWriteTime(expectedJsonPath);
        Assert.True(firstWriteTime < secondWriteTime, $"File wasn't updated! {firstWriteTime} {secondWriteTime}");
        Assert.Equal(Content, await File.ReadAllTextAsync(expectedJsonPath), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task OpenApi_Refresh_UnchangedFile()
    {
        CreateBasicProject(withOpenApi: false);

        // Add <OpenApiReference/> to the project and write the filename.json file.
        var app = GetApplication();
        var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

        AssertNoErrors(run);

        var expectedJsonPath = Path.Combine(_tempDir.Root, "filename.json");
        var firstWriteTime = File.GetLastWriteTime(expectedJsonPath);

        await Task.Delay(TimeSpan.FromSeconds(1));

        app = GetApplication();
        run = app.Execute(new[] { "refresh", FakeOpenApiUrl });

        AssertNoErrors(run);

        var secondWriteTime = File.GetLastWriteTime(expectedJsonPath);
        Assert.Equal(firstWriteTime, secondWriteTime);
        Assert.Equal(Content, await File.ReadAllTextAsync(expectedJsonPath));
    }
}
