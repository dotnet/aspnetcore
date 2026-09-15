// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class StorageTest
{
    [Fact]
    public async Task AddBrowserPlatform_RegistersScopedRootWithStableFacades()
    {
        var jsRuntime = new RecordingJSRuntime();
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(jsRuntime);
        services.AddBrowserPlatform();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var browser = scope.ServiceProvider.GetRequiredService<IBrowserPlatform>();

        Assert.Same(browser, scope.ServiceProvider.GetRequiredService<IBrowserPlatform>());
        Assert.Same(browser.Window, browser.Window);
        Assert.Same(browser.Window.LocalStorage, browser.Window.LocalStorage);
        Assert.Same(browser.Window.SessionStorage, browser.Window.SessionStorage);
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task LocalStorage_ForwardsOperationsAndAcquiresReferenceOnce()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var storage = provider.GetRequiredService<IBrowserPlatform>().Window.LocalStorage;

        var length = await storage.GetLengthAsync();
        var key = await storage.KeyAsync(1);
        var value = await storage.GetItemAsync("name");
        await storage.SetItemAsync("name", "value");
        await storage.RemoveItemAsync("name");
        await storage.ClearAsync();

        Assert.Equal(2u, length);
        Assert.Equal("key-1", key);
        Assert.Equal("stored-value", value);
        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal("localStorage", jsRuntime.RequestedProperty);
        Assert.Collection(
            jsRuntime.ObjectReference.Invocations,
            invocation => AssertInvocation(invocation, "key", 1u),
            invocation => AssertInvocation(invocation, "getItem", "name"),
            invocation => AssertInvocation(invocation, "setItem", "name", "value"),
            invocation => AssertInvocation(invocation, "removeItem", "name"),
            invocation => AssertInvocation(invocation, "clear"));
    }

    [Fact]
    public async Task SessionStorage_UsesSessionStorageProperty()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);

        await provider.GetRequiredService<IBrowserPlatform>().Window.SessionStorage.GetLengthAsync();

        Assert.Equal("sessionStorage", jsRuntime.RequestedProperty);
    }

    [Fact]
    public async Task DisposeAsync_DisposesReferenceAndRejectsFurtherCalls()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var storage = provider.GetRequiredService<IBrowserPlatform>().Window.LocalStorage;
        await storage.GetLengthAsync();

        await storage.DisposeAsync();

        Assert.True(jsRuntime.ObjectReference.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await storage.GetLengthAsync());
    }

    [Fact]
    public async Task DisposeAsync_BeforeUseDoesNotAcquireReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var storage = provider.GetRequiredService<IBrowserPlatform>().Window.LocalStorage;

        await storage.DisposeAsync();

        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    private static ServiceProvider CreateServiceProvider(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        services.AddBrowserPlatform();

        return services.BuildServiceProvider();
    }

    private static void AssertInvocation(Invocation invocation, string identifier, params object?[] arguments)
    {
        Assert.Equal(identifier, invocation.Identifier);
        Assert.Equal(arguments, invocation.Arguments);
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public RecordingJSObjectReference ObjectReference { get; } = new();

        public int GetValueCallCount { get; private set; }

        public string? RequestedProperty { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            throw new NotSupportedException();
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            throw new NotSupportedException();
        }

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier)
        {
            GetValueCallCount++;
            RequestedProperty = identifier;

            return ValueTask.FromResult((TValue)(object)ObjectReference);
        }
    }

    private sealed class RecordingJSObjectReference : IJSObjectReference
    {
        public List<Invocation> Invocations { get; } = [];

        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Invocations.Add(new Invocation(identifier, args ?? []));

            object? result = identifier switch
            {
                "key" => $"key-{args![0]}",
                "getItem" => "stored-value",
                _ => default(TValue),
            };

            return ValueTask.FromResult((TValue?)result!);
        }

        public ValueTask<TValue> GetValueAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken)
        {
            Assert.Equal("length", identifier);

            return ValueTask.FromResult((TValue)(object)2u);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    private sealed record Invocation(string Identifier, object?[] Arguments);
}
