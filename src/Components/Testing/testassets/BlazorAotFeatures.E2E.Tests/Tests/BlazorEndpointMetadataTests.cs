// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using BlazorServerAotSample;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[UITest]
public partial class BlazorEndpointMetadataTests : BrowserTest
{
    private IPage _page = null!;
    private ServerInstance _server = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<FeatureApp>(TestRoot.Servers, FeatureAppServer.Configure);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task EndpointMetadata_InheritedAndCustomMetadataDriveRoutesLayoutAndRenderMode()
    {
        await _page.GotoAsync(_server.TestUrl + "/endpoint-metadata/7");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#metadata-increment");

        await Expect(_page.Locator("#metadata-layout")).ToBeVisibleAsync();
        await Expect(_page.Locator("#route-id")).ToHaveTextAsync("7");
        var markers = await _page.Locator("#endpoint-markers").TextContentAsync();
        StringAssert.Contains(markers, "base");
        StringAssert.Contains(markers, "derived");

        await _page.ClickAsync("#metadata-increment");
        await Expect(_page.Locator("#metadata-count")).ToHaveTextAsync("1");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
