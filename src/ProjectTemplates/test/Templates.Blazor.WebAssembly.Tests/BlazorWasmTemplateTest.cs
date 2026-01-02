// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Blazor.Test;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class BlazorWasmTemplateTest : LoggedTest
{
    public BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

    private ITestOutputHelper _output;
    public ITestOutputHelper Output
    {
        get
        {
            if (_output == null)
            {
                _output = new TestOutputLogger(Logger);
            }
            return _output;
        }
    }

    public string ProjectType { get; } = "blazorwasm";

    protected async Task<Project> CreateBuildPublishAsync(string auth = null, string[] args = null, string targetFramework = null)
    {
        // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
        Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

        var project = await ProjectFactory.CreateProject(Output);
        if (targetFramework != null)
        {
            project.TargetFramework = targetFramework;
        }

        await project.RunDotNetNewAsync(ProjectType, auth: auth, args: args, errorOnRestoreError: false);

        await project.RunDotNetPublishAsync(noRestore: false);

        return project;
    }

    [Fact]
    public async Task BlazorWasmStandaloneTemplateCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync();

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");
    }

    [Fact]
    public async Task BlazorWasmStandaloneTemplateEmptyCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.Empty }); ;

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");
    }

    [Fact]
    public async Task BlazorWasmStandaloneTemplateNoHttpsCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.NoHttps });

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");
    }

    [Fact]
    public async Task BlazorWasmStandaloneTemplateNoHttpsEmptyCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.NoHttps, ArgConstants.Empty });

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");
    }

    [Fact]
    public Task BlazorWasmStandalonePwaTemplateCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Pwa });

    [Fact]
    public Task BlazorWasmStandalonePwaEmptyTemplateCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Pwa, ArgConstants.Empty });

    [Fact]
    public Task BlazorWasmStandalonePwaTemplateNoHttpsCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Pwa, ArgConstants.NoHttps });

    [Fact]
    public Task BlazorWasmStandalonePwaEmptyTemplateNoHttpsCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Pwa, ArgConstants.NoHttps, ArgConstants.Empty });
}

internal sealed class TestOutputLogger : ITestOutputHelper
{
    private readonly ILogger _logger;

    public TestOutputLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void WriteLine(string message)
    {
        _logger.LogInformation(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args));
        }
    }
}
