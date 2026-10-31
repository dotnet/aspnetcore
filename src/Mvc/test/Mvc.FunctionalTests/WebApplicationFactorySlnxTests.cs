// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using BasicWebSite;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class WebApplicationFactorySlnxTests : IClassFixture<WebApplicationFactory<BasicWebSite.Startup>>, IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _contentDirectory;

    public WebApplicationFactorySlnxTests(WebApplicationFactory<BasicWebSite.Startup> factory)
    {
        Factory = factory;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")[..8]);
        _contentDirectory = Path.Combine(_tempDirectory, "BasicWebSite");

        Directory.CreateDirectory(_tempDirectory);
        Directory.CreateDirectory(_contentDirectory);

        // Create a minimal wwwroot directory to satisfy content root expectations
        var wwwrootDir = Path.Combine(_contentDirectory, "wwwroot");
        Directory.CreateDirectory(wwwrootDir);
    }

    public WebApplicationFactory<BasicWebSite.Startup> Factory { get; }

    [Fact]
    public async Task WebApplicationFactory_UsesSlnxForSolutionRelativeContentRoot()
    {
        // Create .slnx file in temp directory
        var slnxFile = Path.Combine(_tempDirectory, "TestSolution.slnx");
        File.WriteAllText(slnxFile, """
            <Solution>
              <Configurations>
                <Configuration Name="Debug|Any CPU" />
                <Configuration Name="Release|Any CPU" />
              </Configurations>
              <Folder Name="/BasicWebSite/">
                <Project Path="BasicWebSite/BasicWebSite.csproj" />
              </Folder>
            </Solution>
            """);

        var factory = Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSolutionRelativeContentRoot("BasicWebSite", _tempDirectory, "TestSolution.slnx");
        });

        using var client = factory.CreateClient();

        // Verify that the content root was set correctly by accessing the environment
        var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();
        Assert.Equal(_contentDirectory, environment.ContentRootPath);
        Assert.True(Directory.Exists(environment.ContentRootPath));

        // Verify the factory is functional with the .slnx-resolved content root
        var response = await client.GetAsync("/");
        Assert.True(response.IsSuccessStatusCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
