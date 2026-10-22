// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class NavigatorTest
{
    [Fact]
    public async Task Navigator_IsStableAndDoesNotCallTheBrowser()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;

        Assert.Same(window.Navigator, window.Navigator);
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task Navigator_ReadsAttributesAndAcquiresReferenceOnce()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var navigator = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator;

        Assert.Equal("en-US", await navigator.GetLanguageAsync());
        Assert.Equal(new[] { "en-US", "ja" }, await navigator.GetLanguagesAsync());
        Assert.True(await navigator.GetOnLineAsync());
        Assert.True(await navigator.GetCookieEnabledAsync());
        Assert.Equal("test-agent", await navigator.GetUserAgentAsync());
        Assert.Equal(8ul, await navigator.GetHardwareConcurrencyAsync());
        Assert.Equal(5, await navigator.GetMaxTouchPointsAsync());

        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal("navigator", jsRuntime.RequestedProperty);
        Assert.Equal(
            new[] { "language", "languages", "onLine", "cookieEnabled", "userAgent", "hardwareConcurrency", "maxTouchPoints" },
            jsRuntime.ObjectReference.ReadProperties);
    }

    [Fact]
    public async Task Navigator_ReacquiresReferenceAfterAcquisitionIsCanceled()
    {
        var jsRuntime = new RecordingJSRuntime { CancelNextGetValue = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var navigator = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => navigator.GetLanguageAsync().AsTask());

        Assert.Equal("en-US", await navigator.GetLanguageAsync());
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DisposesTheAcquiredReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);

        await using (provider)
        {
            await provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.GetLanguageAsync();
            Assert.False(jsRuntime.ObjectReference.Disposed);
        }

        Assert.True(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_MakesNavigatorUnusable()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var navigator = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator;
        await navigator.GetLanguageAsync();

        await provider.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => navigator.GetLanguageAsync().AsTask());
        Assert.Equal(1, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DoesNotWaitForPendingAcquisition()
    {
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = new TaskCompletionSource<IJSObjectReference>() };
        var provider = CreateServiceProvider(jsRuntime);

        _ = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.GetLanguageAsync();

        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ScopeDisposal_BeforeUseDoesNotAcquireReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        _ = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator;

        await provider.DisposeAsync();

        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task GetOrAddFeature_CreatesOnceWithContextAndDisposesWithScope()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var navigator = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator;
        var created = 0;

        var feature = navigator.GetOrAddFeature(context =>
        {
            created++;
            return new TestFeature(context);
        });

        Assert.Same(feature, navigator.GetOrAddFeature(context => new TestFeature(context)));
        Assert.Equal(1, created);
        Assert.Same(jsRuntime, feature.Context.JSRuntime);

        await provider.DisposeAsync();

        Assert.True(feature.Disposed);
        Assert.Throws<ObjectDisposedException>(() => navigator.GetOrAddFeature(context => new TestFeature(context)));
    }

    private static ServiceProvider CreateServiceProvider(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        services.AddBrowserPlatform();

        return services.BuildServiceProvider();
    }

    private sealed class TestFeature(FeatureContext context) : IInternalAsyncDisposal
    {
        public FeatureContext Context { get; } = context;

        public bool Disposed { get; private set; }

        ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public RecordingJSObjectReference ObjectReference { get; } = new();

        public int GetValueCallCount { get; private set; }

        public string? RequestedProperty { get; private set; }

        public bool CancelNextGetValue { get; set; }

        public TaskCompletionSource<IJSObjectReference>? PendingNextGetValue { get; set; }

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

            if (CancelNextGetValue)
            {
                CancelNextGetValue = false;

                return ValueTask.FromCanceled<TValue>(new CancellationToken(canceled: true));
            }

            if (PendingNextGetValue is { } pending)
            {
                PendingNextGetValue = null;

                return new ValueTask<TValue>((Task<TValue>)(object)pending.Task);
            }

            return ValueTask.FromResult((TValue)(object)ObjectReference);
        }
    }

    private sealed class RecordingJSObjectReference : IJSObjectReference
    {
        public List<string> ReadProperties { get; } = [];

        public bool Disposed { get; private set; }

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
            ReadProperties.Add(identifier);

            object value = identifier switch
            {
                "language" => "en-US",
                "languages" => new[] { "en-US", "ja" },
                "onLine" => true,
                "cookieEnabled" => true,
                "userAgent" => "test-agent",
                "hardwareConcurrency" => 8ul,
                "maxTouchPoints" => 5,
                _ => throw new InvalidOperationException($"Unexpected property '{identifier}'."),
            };

            return ValueTask.FromResult((TValue)value);
        }

        public ValueTask<TValue> GetValueAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }
}
