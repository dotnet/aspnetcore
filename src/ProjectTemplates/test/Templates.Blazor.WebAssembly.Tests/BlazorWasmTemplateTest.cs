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
}
