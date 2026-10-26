// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class EventListenerLifecycleTest
{
    private const string EmptyChange = """[{"changed":[],"deleted":[]}]""";

    [Fact]
    public async Task WindowAddEventListenerAsync_StaysOnTheRendererSynchronizationContext()
    {
        var jsRuntime = new BrowserJSRuntime { CompleteOnThreadPool = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;
        var dispatcher = Dispatcher.CreateDefault();
        var context = await dispatcher.InvokeAsync(() => SynchronizationContext.Current);

        await dispatcher.InvokeAsync(async () => await window.AddEventListenerAsync(Window.StorageEvent, _ => Task.CompletedTask));

        Assert.NotNull(context);
        Assert.Equal(["window", "import", "createCallback", "addEventListener"], jsRuntime.Calls.Select(call => call.Identifier));
        Assert.All(jsRuntime.Calls, call => Assert.Same(context, call.Context));
    }

    [Fact]
    public async Task CookieStoreAddEventListenerAsync_StaysOnTheRendererSynchronizationContext()
    {
        var jsRuntime = new BrowserJSRuntime { CompleteOnThreadPool = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        var dispatcher = Dispatcher.CreateDefault();
        var context = await dispatcher.InvokeAsync(() => SynchronizationContext.Current);

        await dispatcher.InvokeAsync(async () => await cookieStore.AddEventListenerAsync(CookieStore.ChangeEvent, _ => Task.CompletedTask));

        Assert.NotNull(context);
        Assert.Equal(["cookieStore", "import", "createCallback", "addEventListener"], jsRuntime.Calls.Select(call => call.Identifier));
        Assert.All(jsRuntime.Calls, call => Assert.Same(context, call.Context));
    }

    [Fact]
    public async Task ScopeDisposal_RemovesTheOtherListenersWhenOneRemovalFails()
    {
        var jsRuntime = new BrowserJSRuntime();
        var loggerFactory = new RecordingLoggerFactory();
        var provider = CreateServiceProvider(jsRuntime, loggerFactory);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        await cookieStore.AddEventListenerAsync(CookieStore.ChangeEvent, _ => Task.CompletedTask);
        await cookieStore.AddEventListenerAsync(CookieStore.ChangeEvent, _ => Task.CompletedTask);
        jsRuntime.FailNextCallTo("removeEventListener");

        await provider.DisposeAsync();

        // The cookie store is object 1, the module is object 2, and the functions are objects 3 and 4.
        Assert.Equal(2, jsRuntime.CallsTo("removeEventListener").Count());
        Assert.Equal(
            ["[3]", "[4]", "[1]", "[2]"],
            jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(call => call.ArgsJson));
        Assert.False((await jsRuntime.InvokeCallbackAsync(0, EmptyChange)).Success);
        Assert.False((await jsRuntime.InvokeCallbackAsync(1, EmptyChange)).Success);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal((LogLevel.Warning, "ListenerRemovalFailed"), (entry.Level, entry.EventId.Name));
        Assert.Contains("removeEventListener", Assert.IsType<JSException>(entry.Exception).Message);
    }

    [Fact]
    public async Task DisposeAsync_ReleasesTheFunctionAndTheCallbackWhenTheRemovalFails()
    {
        var jsRuntime = new BrowserJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        var listener = await cookieStore.AddEventListenerAsync(CookieStore.ChangeEvent, _ => Task.CompletedTask);
        jsRuntime.FailNextCallTo("removeEventListener");

        var exception = await Assert.ThrowsAsync<JSException>(() => listener.DisposeAsync().AsTask());
        await listener.DisposeAsync();

        // The function is object 3.
        Assert.Contains("removeEventListener", exception.Message);
        Assert.Single(jsRuntime.CallsTo("removeEventListener"));
        Assert.Equal(["[3]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(call => call.ArgsJson));
        Assert.False((await jsRuntime.InvokeLastCallbackAsync(EmptyChange)).Success);
    }

    [Fact]
    public async Task AddEventListenerAsync_ThrowsTheOriginalExceptionWhenTheCleanupAlsoFails()
    {
        var jsRuntime = new BrowserJSRuntime();
        var loggerFactory = new RecordingLoggerFactory();
        await using var provider = CreateServiceProvider(jsRuntime, loggerFactory);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        jsRuntime.FailNextCallTo("addEventListener");
        jsRuntime.FailNextCallTo("removeEventListener");

        var exception = await Assert.ThrowsAsync<JSException>(
            () => cookieStore.AddEventListenerAsync(CookieStore.ChangeEvent, _ => Task.CompletedTask).AsTask());

        // The function is object 3.
        Assert.Contains("addEventListener", exception.Message);
        Assert.Equal(["[3]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(call => call.ArgsJson));
        Assert.False((await jsRuntime.InvokeLastCallbackAsync(EmptyChange)).Success);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal("ListenerRemovalFailed", entry.EventId.Name);
    }

    [Fact]
    public async Task ScopeDisposal_DuringTheModuleImport_ReleasesTheModuleThatArrivesLater()
    {
        var jsRuntime = new BrowserJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        jsRuntime.HoldCallsTo("import");

        var adding = cookieStore.AddEventListenerAsync(CookieStore.ChangeEvent, _ => Task.CompletedTask).AsTask();
        await provider.DisposeAsync();
        jsRuntime.ReleaseHeldCalls("import");

        // The cookie store is object 1, and the module is object 2.
        await Assert.ThrowsAsync<ObjectDisposedException>(() => adding);
        Assert.Empty(jsRuntime.CallsTo("createCallback"));
        Assert.Empty(jsRuntime.CallsTo("addEventListener"));
        Assert.Equal(["[1]", "[2]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(call => call.ArgsJson));
    }

    private static ServiceProvider CreateServiceProvider(IJSRuntime jsRuntime, ILoggerFactory? loggerFactory = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        services.AddBrowserPlatform();

        if (loggerFactory is not null)
        {
            services.AddSingleton(loggerFactory);
        }

        return services.BuildServiceProvider();
    }
}
