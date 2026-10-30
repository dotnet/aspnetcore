// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class ScreenTest
{
    [Fact]
    public async Task Screen_IsStableAndDoesNotCallTheBrowser()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;

        Assert.Same(window.Screen, window.Screen);
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task Screen_ReadsAttributesAndAcquiresReferenceOnce()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var screen = provider.GetRequiredService<IBrowserPlatform>().Window.Screen;

        Assert.Equal(1880, await screen.GetAvailWidthAsync());
        Assert.Equal(1040, await screen.GetAvailHeightAsync());
        Assert.Equal(1920, await screen.GetWidthAsync());
        Assert.Equal(1080, await screen.GetHeightAsync());
        Assert.Equal(24u, await screen.GetColorDepthAsync());
        Assert.Equal(30u, await screen.GetPixelDepthAsync());

        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal("screen", jsRuntime.RequestedProperty);
        Assert.Equal(
            new[] { "availWidth", "availHeight", "width", "height", "colorDepth", "pixelDepth" },
            jsRuntime.ObjectReference.ReadProperties);
    }

    [Fact]
    public async Task Screen_ReacquiresReferenceAfterAcquisitionIsCanceled()
    {
        var jsRuntime = new RecordingJSRuntime { CancelNextGetValue = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var screen = provider.GetRequiredService<IBrowserPlatform>().Window.Screen;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => screen.GetWidthAsync().AsTask());

        Assert.Equal(1920, await screen.GetWidthAsync());
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DisposesTheAcquiredReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);

        await using (provider)
        {
            await provider.GetRequiredService<IBrowserPlatform>().Window.Screen.GetWidthAsync();
            Assert.False(jsRuntime.ObjectReference.Disposed);
        }

        Assert.True(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_MakesScreenUnusable()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var screen = provider.GetRequiredService<IBrowserPlatform>().Window.Screen;
        await screen.GetWidthAsync();

        await provider.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => screen.GetWidthAsync().AsTask());
        Assert.Equal(1, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DoesNotWaitForPendingAcquisition()
    {
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = new TaskCompletionSource<IJSObjectReference>() };
        var provider = CreateServiceProvider(jsRuntime);

        _ = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.GetWidthAsync();

        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ScopeDisposal_BeforeUseDoesNotAcquireReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        _ = provider.GetRequiredService<IBrowserPlatform>().Window.Screen;

        await provider.DisposeAsync();

        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    private static ServiceProvider CreateServiceProvider(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        services.AddBrowserPlatform();

        return services.BuildServiceProvider();
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

            // Distinct values, so a swapped identifier fails. Browsers return the same for the two depths.
            object value = identifier switch
            {
                "availWidth" => 1880,
                "availHeight" => 1040,
                "width" => 1920,
                "height" => 1080,
                "colorDepth" => 24u,
                "pixelDepth" => 30u,
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
