// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.JSInterop;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class JSInteropValueTypesTest: ServerTestBase<BlazorWasmTestAppFixture<Program>>
{
    private IJSRuntime _jsRuntime;
    public JSInteropValueTypesTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<Program> serverFixture,
        ITestOutputHelper output,
        IJSRuntime jsRuntime)
        : base(browserFixture, serverFixture, output)
    {
        _serverFixture.PathBase = ServerPathBase;
        _jsRuntime = jsRuntime;
    }

    [Fact]
    public async Task CanStoreGuid()
    {
        var guid = Guid.NewGuid();

        // Store guid in localStorage
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "guid", guid);

        // Retrieve guid from localStorage as string because that "conversion" always works
        var storedGuid = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "guid");

        Assert.Equal(guid.ToString(), storedGuid);
    }

    [Fact]
    public async Task CanRetrieveStoredGuid()
    {
        var guid = Guid.NewGuid();

        // Store guid in localStorage
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "guid", guid);

        // Retrieve guid from localStorage
        var storedGuid = await _jsRuntime.InvokeAsync<Guid>("localStorage.getItem", "guid");

        Assert.Equal(guid, storedGuid);
    }

    [Fact]
    public async Task CanRetrieveNullAsNullable()
    {
        // Store null in localStorage
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "nullHere", null);

        // Retrieve null from localStorage
        var storedNull = await _jsRuntime.InvokeAsync<Guid?>("localStorage.getItem", "nullHere");

        Assert.Null(storedNull);
    }
}
