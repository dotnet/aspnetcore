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
using Newtonsoft.Json.Linq;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Blazor.Test;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class BlazorWasmTemplateTest : BlazorTemplateTest
{
    public BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory) { }

    public override string ProjectType { get; } = "blazorwasm";

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

    [Fact]
    public async Task BlazorWasmWebWorkerTemplateCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.WebWorker });

        // Verify the WorkerClient project was created
        var workerClientDir = Path.Combine(project.TemplateOutputDir, $"{project.ProjectName}.WorkerClient");
        Assert.True(Directory.Exists(workerClientDir), "WebWorker templates should produce a WorkerClient project");

        // Verify worker files exist
        Assert.True(File.Exists(Path.Combine(workerClientDir, "WorkerClient.cs")), "WorkerClient.cs should exist in WorkerClient project");
        Assert.True(File.Exists(Path.Combine(workerClientDir, "wwwroot", "worker.js")), "worker.js should exist in WorkerClient project");
        Assert.True(File.Exists(Path.Combine(workerClientDir, "wwwroot", "worker-client.js")), "worker-client.js should exist in WorkerClient project");

        // Verify the ImageProcessor page was created
        var imageProcessorPath = Path.Combine(project.TemplateOutputDir, "Pages", "ImageProcessor.razor");
        Assert.True(File.Exists(imageProcessorPath), "ImageProcessor.razor should exist in Pages folder");
    }

    [Fact]
    public async Task BlazorWasmWebWorkerTemplateEmptyCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.WebWorker, ArgConstants.Empty });

        // Verify the WorkerClient project was created
        var workerClientDir = Path.Combine(project.TemplateOutputDir, $"{project.ProjectName}.WorkerClient");
        Assert.True(Directory.Exists(workerClientDir), "WebWorker templates should produce a WorkerClient project");
    }

    [Fact]
    public async Task BlazorWasmWebWorkerTemplateNoHttpsCanCreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync(args: new[] { ArgConstants.WebWorker, ArgConstants.NoHttps });

        // Verify the WorkerClient project was created
        var workerClientDir = Path.Combine(project.TemplateOutputDir, $"{project.ProjectName}.WorkerClient");
        Assert.True(Directory.Exists(workerClientDir), "WebWorker templates should produce a WorkerClient project");
    }
}
