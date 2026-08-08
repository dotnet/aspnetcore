// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class BlazorEndpointMetadataTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private IPage _page = null!;
    private ServerInstance _server = null!;

    public BlazorEndpointMetadataTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await FeatureAppServer.StartAsync(_fixture);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [Fact]
    public async Task EndpointMetadata_InheritedAndCustomMetadataDriveRoutesLayoutAndRenderMode()
    {
        await _page.GotoAsync(_server.TestUrl + "/endpoint-metadata/7");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#metadata-increment");

        await Expect(_page.Locator("#metadata-layout")).ToBeVisibleAsync();
        await Expect(_page.Locator("#route-id")).ToHaveTextAsync("7");
        var markers = await _page.Locator("#endpoint-markers").TextContentAsync();
        Assert.Contains("base", markers);
        Assert.Contains("derived", markers);

        await _page.ClickAsync("#metadata-increment");
        await Expect(_page.Locator("#metadata-count")).ToHaveTextAsync("1");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
