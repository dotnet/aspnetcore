// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class FetchTest
{
    [Fact]
    public async Task FetchAsync_MapsRequestOptions()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var platform = provider.GetRequiredService<IBrowserPlatform>();
        await using var response = await platform.FetchAsync(
            "/echo",
            new RequestInit
            {
                Method = "POST",
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "text/plain",
                },
                Body = "request-body",
            });

        Assert.Equal("fetch", jsRuntime.Identifier);
        Assert.Equal("/echo", jsRuntime.Arguments[0]);

        var options = Assert.IsType<Dictionary<string, object?>>(jsRuntime.Arguments[1]);
        Assert.Equal("POST", options["method"]);
        Assert.Equal("request-body", options["body"]);

        var headers = Assert.IsType<Dictionary<string, string>>(options["headers"]);
        Assert.Equal("text/plain", headers["content-type"]);
    }

    [Fact]
    public async Task Response_ForwardsPropertiesBodyAndHeaders()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var response = await provider
            .GetRequiredService<IBrowserPlatform>()
            .FetchAsync("/echo");

        var ok = await response.GetOkAsync();
        var status = await response.GetStatusAsync();
        var statusText = await response.GetStatusTextAsync();
        var url = await response.GetUrlAsync();
        var body = await response.TextAsync();
        var firstHeaders = await response.GetHeadersAsync();
        var secondHeaders = await response.GetHeadersAsync();
        var contentType = await firstHeaders.GetAsync("content-type");

        Assert.True(ok);
        Assert.Equal((ushort)200, status);
        Assert.Equal("OK", statusText);
        Assert.Equal("https://example.test/echo", url);
        Assert.Equal("response-body", body);
        Assert.Equal("text/plain", contentType);
        Assert.Same(firstHeaders, secondHeaders);
        Assert.Equal(1, jsRuntime.ResponseReference.HeadersGetCount);
    }

    [Fact]
    public async Task DisposeAsync_DisposesResponseAndHeadersAndRejectsFurtherCalls()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var response = await provider
            .GetRequiredService<IBrowserPlatform>()
            .FetchAsync("/echo");
        var headers = await response.GetHeadersAsync();

        await response.DisposeAsync();
        await response.DisposeAsync();

        Assert.True(jsRuntime.ResponseReference.Disposed);
        Assert.True(jsRuntime.HeadersReference.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await response.GetStatusAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await headers.GetAsync("content-type"));
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
        public RecordingHeadersReference HeadersReference { get; } = new();

        public RecordingResponseReference ResponseReference { get; }

        public RecordingJSRuntime()
        {
            ResponseReference = new RecordingResponseReference(HeadersReference);
        }

        public string? Identifier { get; private set; }

        public object?[] Arguments { get; private set; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Identifier = identifier;
            Arguments = args ?? [];
            return ValueTask.FromResult((TValue)(object)ResponseReference);
        }
    }

    private sealed class RecordingResponseReference(
        RecordingHeadersReference headersReference) : IJSObjectReference
    {
        public int HeadersGetCount { get; private set; }

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
            Assert.Equal("text", identifier);

            return ValueTask.FromResult((TValue)(object)"response-body");
        }

        public ValueTask<TValue> GetValueAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken)
        {
            object value = identifier switch
            {
                "ok" => true,
                "status" => (ushort)200,
                "statusText" => "OK",
                "url" => "https://example.test/echo",
                "headers" => GetHeaders(),
                _ => throw new NotSupportedException(identifier),
            };

            return ValueTask.FromResult((TValue)value);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }

        private IJSObjectReference GetHeaders()
        {
            HeadersGetCount++;

            return headersReference;
        }
    }

    private sealed class RecordingHeadersReference : IJSObjectReference
    {
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
            object? value = identifier switch
            {
                "get" => "text/plain",
                "has" => true,
                _ => default(TValue),
            };

            return ValueTask.FromResult((TValue?)value!);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }
}
