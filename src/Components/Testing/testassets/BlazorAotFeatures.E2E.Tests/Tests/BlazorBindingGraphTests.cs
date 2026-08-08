// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class BlazorBindingGraphTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private IPage _page = null!;
    private ServerInstance _server = null!;

    public BlazorBindingGraphTests(ServerFixture<E2ETestAssembly> fixture)
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
    public async Task BindingGraph_FieldsPropertiesIndexersAndTypedArraysRoundTrip()
    {
        await _page.GotoAsync(_server.TestUrl + "/binding-graph");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#bind-field");

        await FillAndBlurAsync("#bind-field", "field-value");
        await Expect(_page.Locator("#bound-field")).ToHaveTextAsync("field-value");

        await FillAndBlurAsync("#bind-property", "property-value");
        await Expect(_page.Locator("#bound-property")).ToHaveTextAsync("property-value");

        await FillAndBlurAsync("#bind-array-item", "row-1");
        await Expect(_page.Locator("#bound-array-item")).ToHaveTextAsync("row-1");

        await FillAndBlurAsync("#bind-custom-indexer", "book-2");
        await Expect(_page.Locator("#bound-custom-indexer")).ToHaveTextAsync("book-2");

        await FillAndBlurAsync("#bind-string-array-item", "tag-1");
        await Expect(_page.Locator("#bound-string-array-item")).ToHaveTextAsync("tag-1");

        await _page.SelectOptionAsync("#typed-array-select", ["2", "3"]);
        await Expect(_page.Locator("#bound-typed-array")).ToHaveTextAsync("2,3");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    private async Task FillAndBlurAsync(string selector, string value)
    {
        await _page.FillAsync(selector, value);
        await _page.Locator(selector).BlurAsync();
    }
}
