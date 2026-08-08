// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class BlazorJsInvokableTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private IPage _page = null!;
    private ServerInstance _server = null!;

    public BlazorJsInvokableTests(ServerFixture<E2ETestAssembly> fixture)
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
    public async Task JSInvokable_GeneratedResolverDispatchesStaticInstanceAndInheritedMethods()
    {
        await GotoAsync();
        await _page.ClickAsync("#invoke-js-addressing");
        await Expect(_page.Locator("#js-static")).ToHaveTextAsync("static:7");
        await Expect(_page.Locator("#js-instance")).ToHaveTextAsync("instance:8");
        await Expect(_page.Locator("#js-base")).ToHaveTextAsync("base:9");
    }

    [Fact]
    public async Task JSInvokable_AllReturnKindsAndObjectReferenceDisposeComplete()
    {
        await GotoAsync();
        await _page.ClickAsync("#invoke-js-return-kinds");
        await Expect(_page.Locator("#js-return-kinds")).ToHaveTextAsync(
            "void:null|sync:sync|task:null|taskT:task|valueTask:null|valueTaskT:value-task");
        await Expect(_page.Locator("#js-dispose-first")).ToHaveTextAsync("before-dispose");
        await Expect(_page.Locator("#js-dispose-second")).ToHaveTextAsync("disposed");
    }

    [Fact]
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
