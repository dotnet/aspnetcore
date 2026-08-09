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
public partial class BlazorJsInvokableTests : BrowserTest
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
    public async Task JSInvokable_GeneratedResolverDispatchesStaticInstanceAndInheritedMethods()
    {
        await GotoAsync();
        await _page.ClickAsync("#invoke-js-addressing");
        await Expect(_page.Locator("#js-static")).ToHaveTextAsync("static:7");
        await Expect(_page.Locator("#js-instance")).ToHaveTextAsync("instance:8");
        await Expect(_page.Locator("#js-base")).ToHaveTextAsync("base:9");
    }

    [TestMethod]
    public async Task JSInvokable_AllReturnKindsAndObjectReferenceDisposeComplete()
    {
        await GotoAsync();
        await _page.ClickAsync("#invoke-js-return-kinds");
        await Expect(_page.Locator("#js-return-kinds")).ToHaveTextAsync(
            "void:null|sync:sync|task:null|taskT:task|valueTask:null|valueTaskT:value-task");
        await Expect(_page.Locator("#js-dispose-first")).ToHaveTextAsync("before-dispose");
        await Expect(_page.Locator("#js-dispose-second")).ToHaveTextAsync("disposed");
    }

    [TestMethod]
    public async Task JSInvokable_PocoAndPolymorphicArgumentsAndResultsUseUnifiedJsonResolvers()
    {
        await GotoAsync();
        await _page.ClickAsync("#invoke-js-payloads");
        await Expect(_page.Locator("#js-poco")).ToHaveTextAsync("request:ada:36|result:ADA:37");
        await Expect(_page.Locator("#js-polymorphic")).ToHaveTextAsync("cat:milo:9");
        await Expect(_page.Locator("#js-wire-discriminator")).ToHaveTextAsync("cat");
    }

    private async Task GotoAsync()
    {
        await _page.GotoAsync(_server.TestUrl + "/js-invokable-matrix");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#invoke-js-addressing");
    }
}
