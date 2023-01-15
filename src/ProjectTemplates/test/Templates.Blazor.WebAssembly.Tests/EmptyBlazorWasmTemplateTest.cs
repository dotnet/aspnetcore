// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Newtonsoft.Json.Linq;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Blazor.Test;

public class EmptyBlazorWasmTemplateTest : BlazorTemplateTest
{
    public EmptyBlazorWasmTemplateTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory) { }

    public override string ProjectType { get; } = "blazorwasm-empty";

    [Fact]
    public async Task EmptyBlazorWasmStandaloneTemplateCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync();

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");
    }

    [Fact]
    public async Task EmptyBlazorWasmStandaloneTemplateNoHttpsCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.NoHttps });

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");
    }

    [Fact]
    public Task EmptyBlazorWasmHostedTemplateCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Hosted }, serverProject: true);

    [Fact]
    public Task EmptyBlazorWasmHostedTemplateNoHttpsCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Hosted, ArgConstants.NoHttps }, serverProject: true);

    [Fact]
    public Task EmptyBlazorWasmStandalonePwaTemplateCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Pwa });

    [Fact]
    public Task EmptyBlazorWasmStandalonePwaTemplateNoHttpsCanCreateBuildPublish()
        => CreateBuildPublishAsync(args: new[] { ArgConstants.Pwa, ArgConstants.NoHttps });

    [Fact]
    public async Task EmptyBlazorWasmHostedPwaTemplateCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.Hosted, ArgConstants.Pwa }, serverProject: true);

        var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

        ValidatePublishedServiceWorker(serverProject);
    }

    [Fact]
    public async Task EmptyBlazorWasmHostedPwaTemplateNoHttpsCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.Hosted, ArgConstants.Pwa, ArgConstants.NoHttps }, serverProject: true);

        var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

        ValidatePublishedServiceWorker(serverProject);
    }

    private void ValidatePublishedServiceWorker(Project project)
    {
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");

        // When publishing the PWA template, we generate an assets manifest
        // and move service-worker.published.js to overwrite service-worker.js
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker.published.js")), "service-worker.published.js should not be published");
        Assert.True(File.Exists(Path.Combine(publishDir, "service-worker.js")), "service-worker.js should be published");
        Assert.True(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "service-worker-assets.js should be published");

        // We automatically append the SWAM version as a comment in the published service worker file
        var serviceWorkerAssetsManifestContents = ReadFile(publishDir, "service-worker-assets.js");
        var serviceWorkerContents = ReadFile(publishDir, "service-worker.js");

        // Parse the "version": "..." value from the SWAM, and check it's in the service worker
        var serviceWorkerAssetsManifestVersionMatch = new Regex(@"^\s*\""version\"":\s*(\""[^\""]+\"")", RegexOptions.Multiline)
            .Match(serviceWorkerAssetsManifestContents);
        Assert.True(serviceWorkerAssetsManifestVersionMatch.Success);
        var serviceWorkerAssetsManifestVersionJson = serviceWorkerAssetsManifestVersionMatch.Groups[1].Captures[0].Value;
        var serviceWorkerAssetsManifestVersion = JsonSerializer.Deserialize<string>(serviceWorkerAssetsManifestVersionJson);
        Assert.True(serviceWorkerContents.Contains($"/* Manifest version: {serviceWorkerAssetsManifestVersion} */", StringComparison.Ordinal));
    }
}
