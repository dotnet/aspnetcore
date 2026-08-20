// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class UrlTest
{
    [Fact]
    public async Task AddBrowserPlatform_RegistersStableScopedFacadeWithoutInterop()
    {
        var jsRuntime = new RecordingJSRuntime();
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(jsRuntime);
        services.AddBrowserPlatform();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var platform = scope.ServiceProvider.GetRequiredService<IBrowserPlatform>();

        Assert.Same(platform, scope.ServiceProvider.GetRequiredService<IBrowserPlatform>());
        Assert.Same(platform.Url, platform.Url);
        Assert.Empty(jsRuntime.ConstructorCalls);
    }

    [Fact]
    public async Task CreateAsync_InvokesUrlConstructorWithAbsoluteOrRelativeArguments()
    {
        var jsRuntime = new RecordingJSRuntime();
        var platform = CreatePlatform(jsRuntime);
        await using var absolute = await platform.Url.CreateAsync("https://example.test/a");
        await using var relative = await platform.Url.CreateAsync(
            "../b",
            "https://example.test/a/");

        Assert.Collection(
            jsRuntime.ConstructorCalls,
            call =>
            {
                Assert.Equal("URL", call.Identifier);
                Assert.Equal(["https://example.test/a"], call.Arguments);
            },
            call =>
            {
                Assert.Equal("URL", call.Identifier);
                Assert.Equal(["../b", "https://example.test/a/"], call.Arguments);
            });
    }

    [Fact]
    public async Task Url_ForwardsPropertyOperationsAndKeepsSearchParamsIdentity()
    {
        var jsRuntime = new RecordingJSRuntime();
        var platform = CreatePlatform(jsRuntime);
        await using var url = await platform.Url.CreateAsync("https://example.test/a");
        var urlReference = jsRuntime.UrlReferences.Single();

        var href = await url.GetHrefAsync();
        await url.SetPathnameAsync("/products");
        var first = await url.GetSearchParamsAsync();
        var second = await url.GetSearchParamsAsync();

        Assert.Equal("https://example.test/a", href);
        Assert.Equal("/products", urlReference.SetProperties["pathname"]);
        Assert.Same(first, second);
        Assert.Equal(1, urlReference.SearchParamsGetCount);
    }

    [Fact]
    public async Task SearchParams_ForwardsOperations()
    {
        var jsRuntime = new RecordingJSRuntime();
        var platform = CreatePlatform(jsRuntime);
        await using var url = await platform.Url.CreateAsync("https://example.test/");
        var searchParams = await url.GetSearchParamsAsync();

        await searchParams.AppendAsync("tag", "one");
        await searchParams.SetAsync("page", "2");
        var value = await searchParams.GetAsync("page");
        var values = await searchParams.GetAllAsync("tag");
        var hasPage = await searchParams.HasAsync("page");
        await searchParams.DeleteAsync("tag");
        await searchParams.SortAsync();

        Assert.Equal("2", value);
        Assert.Equal(["one"], values);
        Assert.True(hasPage);
        Assert.Collection(
            jsRuntime.SearchParamsReference.Invocations,
            call => AssertCall(call, "append", "tag", "one"),
            call => AssertCall(call, "set", "page", "2"),
            call => AssertCall(call, "get", "page"),
            call => AssertCall(call, "getAll", "tag"),
            call => AssertCall(call, "has", "page"),
            call => AssertCall(call, "delete", "tag"),
            call => AssertCall(call, "sort"));
    }

    [Fact]
    public async Task DisposeAsync_DisposesOwnedReferencesAndRejectsFurtherOperations()
    {
        var jsRuntime = new RecordingJSRuntime();
        var platform = CreatePlatform(jsRuntime);
        var url = await platform.Url.CreateAsync("https://example.test/");
        var searchParams = await url.GetSearchParamsAsync();
        var urlReference = jsRuntime.UrlReferences.Single();

        await url.DisposeAsync();
        await url.DisposeAsync();

        Assert.True(urlReference.Disposed);
        Assert.True(jsRuntime.SearchParamsReference.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await url.GetHrefAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await searchParams.GetAsync("page"));
    }

    private static IBrowserPlatform CreatePlatform(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        services.AddBrowserPlatform();

        return services.BuildServiceProvider().GetRequiredService<IBrowserPlatform>();
    }

    private static void AssertCall(Invocation call, string identifier, params object?[] arguments)
    {
        Assert.Equal(identifier, call.Identifier);
        Assert.Equal(arguments, call.Arguments);
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public List<Invocation> ConstructorCalls { get; } = [];

        public List<RecordingUrlReference> UrlReferences { get; } = [];

        public RecordingSearchParamsReference SearchParamsReference { get; } = new();

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

        public ValueTask<IJSObjectReference> InvokeConstructorAsync(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            ConstructorCalls.Add(new Invocation(identifier, args ?? []));
            var reference = new RecordingUrlReference(SearchParamsReference);
            UrlReferences.Add(reference);

            return ValueTask.FromResult<IJSObjectReference>(reference);
        }
    }

    private sealed class RecordingUrlReference(
        RecordingSearchParamsReference searchParamsReference) : IJSObjectReference
    {
        public Dictionary<string, object?> SetProperties { get; } = [];

        public int SearchParamsGetCount { get; private set; }

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
            object? result = identifier switch
            {
                "toString" => "https://example.test/a",
                _ => throw new NotSupportedException(identifier),
            };

            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask<TValue> GetValueAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken)
        {
            object result = identifier switch
            {
                "href" => "https://example.test/a",
                "origin" => "https://example.test",
                "searchParams" => GetSearchParamsReference(),
                _ => string.Empty,
            };

            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask SetValueAsync<TValue>(
            string identifier,
            TValue value,
            CancellationToken cancellationToken)
        {
            SetProperties[identifier] = value;

            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }

        private IJSObjectReference GetSearchParamsReference()
        {
            SearchParamsGetCount++;

            return searchParamsReference;
        }
    }

    private sealed class RecordingSearchParamsReference : IJSObjectReference
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
                "get" => "2",
                "getAll" => new[] { "one" },
                "has" => true,
                "toString" => "page=2",
                _ => default(TValue),
            };

            return ValueTask.FromResult((TValue?)result!);
        }

        public ValueTask<TValue> GetValueAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken)
        {
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
