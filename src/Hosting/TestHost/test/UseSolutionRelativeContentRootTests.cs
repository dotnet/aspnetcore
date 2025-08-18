// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.TestHost;

#pragma warning disable ASPDEPR004 // WebHostBuilder is obsolete
#pragma warning disable ASPDEPR008 // WebHost is obsolete
public class UseSolutionRelativeContentRootTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _contentDirectory;

    public UseSolutionRelativeContentRootTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")[..8]);
        _contentDirectory = Path.Combine(_tempDirectory, "src");
        Directory.CreateDirectory(_contentDirectory);
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_FindsSlnFile()
    {
        var solutionFile = Path.Combine(_tempDirectory, "TestApp.sln");
        File.WriteAllText(solutionFile, "Microsoft Visual Studio Solution File, Format Version 12.00");

        var builder = new WebHostBuilder()
            .UseTestServer()
            .Configure(app => { });

        builder.UseSolutionRelativeContentRoot("src", applicationBasePath: _tempDirectory);

        using var host = builder.Build();
        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(_contentDirectory, environment.ContentRootPath);
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_FindsSlnxFile()
    {
        var solutionFile = Path.Combine(_tempDirectory, "TestApp.slnx");
        File.WriteAllText(solutionFile, """
            <Solution>
              <Configurations>
                <Configuration Name="Debug|Any CPU" />
                <Configuration Name="Release|Any CPU" />
              </Configurations>
            </Solution>
            """);

        var builder = new WebHostBuilder()
            .UseTestServer()
            .Configure(app => { });

        builder.UseSolutionRelativeContentRoot("src", applicationBasePath: _tempDirectory);

        using var host = builder.Build();
        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(_contentDirectory, environment.ContentRootPath);
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_WithSolutionName_FindsSpecifiedFile()
    {
        var subDirectory = Path.Combine(_tempDirectory, "sub");
        Directory.CreateDirectory(subDirectory);

        var slnFile = Path.Combine(subDirectory, "TestApp.sln");
        var slnxFile = Path.Combine(_tempDirectory, "TestApp.slnx");
        File.WriteAllText(slnFile, "Microsoft Visual Studio Solution File, Format Version 12.00");
        File.WriteAllText(slnxFile, """
            <Solution>
              <Configurations>
                <Configuration Name="Debug|Any CPU" />
              </Configurations>
            </Solution>
            """);

        var builder = new WebHostBuilder()
            .UseTestServer()
            .Configure(app => { });

        builder.UseSolutionRelativeContentRoot("src", _tempDirectory, "*.slnx");

        using var host = builder.Build();
        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(_contentDirectory, environment.ContentRootPath);
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_WithMultipleSolutionNames_FindsInCurrentDirectoryFirst()
    {
        var expectedPath = Path.Combine(_contentDirectory, "sub");
        Directory.CreateDirectory(expectedPath);

        var slnFile = Path.Combine(_tempDirectory, "TestApp.sln");
        var slnxFile = Path.Combine(_contentDirectory, "TestApp.slnx");
        File.WriteAllText(slnFile, "Microsoft Visual Studio Solution File, Format Version 12.00");
        File.WriteAllText(slnxFile, """
            <Solution>
              <Configurations>
                <Configuration Name="Debug|Any CPU" />
              </Configurations>
            </Solution>
            """);

        var builder = new WebHostBuilder()
            .UseTestServer()
            .Configure(app => { });

        builder.UseSolutionRelativeContentRoot("sub", _contentDirectory, ["*.sln", "*.slnx"]);

        using var host = builder.Build();
        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(expectedPath, environment.ContentRootPath);
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_WithMultipleSolutionNames_WorksWithMultipleFiles()
    {
        var slnFile = Path.Combine(_tempDirectory, "TestApp.sln");
        var slnxFile = Path.Combine(_tempDirectory, "TestApp.slnx");
        File.WriteAllText(slnFile, "Microsoft Visual Studio Solution File, Format Version 12.00");
        File.WriteAllText(slnxFile, """
            <Solution>
              <Configurations>
                <Configuration Name="Debug|Any CPU" />
              </Configurations>
            </Solution>
            """);

        var builder = new WebHostBuilder()
            .UseTestServer()
            .Configure(app => { });

        builder.UseSolutionRelativeContentRoot("src", applicationBasePath: _tempDirectory, solutionNames: ["*.sln", "*.slnx"]);

        using var host = builder.Build();
        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(_contentDirectory, environment.ContentRootPath);
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_ThrowsWhenSolutionNotFound()
    {
        var builder = new WebHostBuilder()
            .UseTestServer()
            .Configure(app => { });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.UseSolutionRelativeContentRoot("src", applicationBasePath: _tempDirectory));

        Assert.Contains("Solution root could not be located", exception.Message);
        Assert.Contains(_tempDirectory, exception.Message);
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_WithSolutionName_SearchesParentDirectories()
    {
        var subDirectory = Path.Combine(_tempDirectory, "sub", "folder");
        Directory.CreateDirectory(subDirectory);

        var solutionFile = Path.Combine(_tempDirectory, "TestApp.slnx");
        File.WriteAllText(solutionFile, """
            <Solution>
              <Configurations>
                <Configuration Name="Debug|Any CPU" />
              </Configurations>
            </Solution>
            """);

        var builder = new WebHostBuilder()
            .UseTestServer()
            .Configure(app => { });

        builder.UseSolutionRelativeContentRoot("src", subDirectory, "*.slnx");

        using var host = builder.Build();
        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(_contentDirectory, environment.ContentRootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
#pragma warning restore ASPDEPR008 // WebHost is obsolete
#pragma warning disable ASPDEPR004 // WebHostBuilder is obsolete
