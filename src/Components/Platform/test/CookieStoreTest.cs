// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0040 // Partitioned is experimental.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class CookieStoreTest
{
    [Fact]
    public async Task CookieStore_IsStableAndNotAcquiredBeforeUse()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;

        Assert.Same(window.CookieStore, window.CookieStore);
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task CookieStore_ForwardsOperationsAndAcquiresReferenceOnce()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        var getOptions = new CookieStoreGetOptions { Name = "name" };
        var init = new CookieInit { Name = "name", Value = "value" };
        var deleteOptions = new CookieStoreDeleteOptions { Name = "name" };

        var byName = await cookieStore.GetAsync("name");
        var byOptions = await cookieStore.GetAsync(getOptions);
        var all = await cookieStore.GetAllAsync();
        var allByName = await cookieStore.GetAllAsync("name");
        var allByOptions = await cookieStore.GetAllAsync(getOptions);
        await cookieStore.SetAsync("name", "value");
        await cookieStore.SetAsync(init);
        await cookieStore.DeleteAsync("name");
        await cookieStore.DeleteAsync(deleteOptions);

        Assert.Equal("stored-value", byName?.Value);
        Assert.Equal("stored-value", byOptions?.Value);
        Assert.Equal("stored-value", Assert.Single(all).Value);
        Assert.Single(allByName);
        Assert.Single(allByOptions);
        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal("cookieStore", jsRuntime.RequestedProperty);
        Assert.Collection(
            jsRuntime.ObjectReference.Invocations,
            invocation => AssertInvocation(invocation, "get", "name"),
            invocation => AssertInvocation(invocation, "get", getOptions),
            invocation => AssertInvocation(invocation, "getAll"),
            invocation => AssertInvocation(invocation, "getAll", "name"),
            invocation => AssertInvocation(invocation, "getAll", getOptions),
            invocation => AssertInvocation(invocation, "set", "name", "value"),
            invocation => AssertInvocation(invocation, "set", init),
            invocation => AssertInvocation(invocation, "delete", "name"),
            invocation => AssertInvocation(invocation, "delete", deleteOptions));
    }

    [Fact]
    public async Task CookieStore_SharesPendingAcquisitionBetweenCallers()
    {
        var pending = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = pending };
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        var first = cookieStore.GetAllAsync().AsTask();
        var second = cookieStore.GetAsync("name").AsTask();
        pending.SetResult(jsRuntime.ObjectReference);
        await Task.WhenAll(first, second);

        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal(2, jsRuntime.ObjectReference.Invocations.Count);
    }

    [Fact]
    public async Task CookieStore_RejectsNullArguments()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        await Assert.ThrowsAsync<ArgumentNullException>("name", () => cookieStore.GetAsync(default(string)!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("options", () => cookieStore.GetAsync(default(CookieStoreGetOptions)!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("name", () => cookieStore.GetAllAsync(default(string)!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("options", () => cookieStore.GetAllAsync(default(CookieStoreGetOptions)!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("name", () => cookieStore.SetAsync(null!, "value").AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("value", () => cookieStore.SetAsync("name", null!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("options", () => cookieStore.SetAsync(null!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("name", () => cookieStore.DeleteAsync(default(string)!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("options", () => cookieStore.DeleteAsync(default(CookieStoreDeleteOptions)!).AsTask());
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task CookieStore_ReacquiresReferenceAfterAcquisitionIsCanceled()
    {
        var jsRuntime = new RecordingJSRuntime { CancelNextGetValue = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cookieStore.GetAllAsync().AsTask());
        var all = await cookieStore.GetAllAsync();

        Assert.Single(all);
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task CookieStore_ThrowsNotSupportedExceptionWhenTheBrowserHasNoCookieStore()
    {
        // JS interop throws this for a missing window.cookieStore, such as on a page that is not a secure context.
        var missingProperty = new JSException("The property 'cookieStore' is not defined or is not readable.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = missingProperty };
        var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        Func<Task>[] members =
        [
            () => cookieStore.GetAsync("name").AsTask(),
            () => cookieStore.GetAsync(new CookieStoreGetOptions()).AsTask(),
            () => cookieStore.GetAllAsync().AsTask(),
            () => cookieStore.GetAllAsync("name").AsTask(),
            () => cookieStore.GetAllAsync(new CookieStoreGetOptions()).AsTask(),
            () => cookieStore.SetAsync("name", "value").AsTask(),
            () => cookieStore.SetAsync(new CookieInit { Name = "name", Value = "value" }).AsTask(),
            () => cookieStore.DeleteAsync("name").AsTask(),
            () => cookieStore.DeleteAsync(new CookieStoreDeleteOptions { Name = "name" }).AsTask(),
        ];

        foreach (var member in members)
        {
            var exception = await Assert.ThrowsAsync<NotSupportedException>(member);
            Assert.Same(missingProperty, exception.InnerException);
        }
        // Disposal must not throw for the failed acquisitions.
        await provider.DisposeAsync();

        Assert.Equal(members.Length, jsRuntime.GetValueCallCount);
        Assert.Empty(jsRuntime.ObjectReference.Invocations);
    }

    [Fact]
    public async Task CookieStore_ThrowsNotSupportedExceptionWhenCookieStoreIsNull()
    {
        var jsRuntime = new RecordingJSRuntime { CookieStoreNull = true };
        var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        await Assert.ThrowsAsync<NotSupportedException>(() => cookieStore.GetAllAsync().AsTask());
        await Assert.ThrowsAsync<NotSupportedException>(() => cookieStore.SetAsync("name", "value").AsTask());
        // Disposal must not throw for the failed acquisitions.
        await provider.DisposeAsync();

        Assert.Equal(2, jsRuntime.GetValueCallCount);
        Assert.Empty(jsRuntime.ObjectReference.Invocations);
    }

    [Fact]
    public async Task CookieStore_PassesThroughOtherAcquisitionFailuresAndReacquires()
    {
        var disconnected = new JSDisconnectedException("The circuit is disconnected.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = disconnected };
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        var exception = await Assert.ThrowsAsync<JSDisconnectedException>(() => cookieStore.GetAllAsync().AsTask());
        jsRuntime.GetValueException = null;
        var all = await cookieStore.GetAllAsync();

        Assert.Same(disconnected, exception);
        Assert.Single(all);
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DisposesTheAcquiredReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);

        await using (provider)
        {
            await provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore.GetAllAsync();
        }

        Assert.True(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_MakesCookieStoreUnusable()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;
        await cookieStore.GetAllAsync();

        await provider.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => cookieStore.GetAllAsync().AsTask());
        Assert.Equal(1, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DoesNotWaitForPendingAcquisition()
    {
        var neverCompletes = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = neverCompletes };
        var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        _ = cookieStore.GetAllAsync();

        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(jsRuntime.ObjectReference.Disposed);
    }

    // A mock IJSRuntime records arguments before serialization, so the tests below check the JSON the browser receives.
    [Fact]
    public async Task SetAsync_SendsCookieInitWithoutNullMembers()
    {
        var jsRuntime = new SerializingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        await cookieStore.SetAsync(new CookieInit { Name = "a", Value = "1" });
        await cookieStore.SetAsync(new CookieInit
        {
            Name = "b",
            Value = "2",
            Expires = 1767225600000,
            Domain = "example.com",
            Path = "/app",
            SameSite = CookieSameSite.Lax,
            Partitioned = true,
            MaxAge = 3600,
        });
        await cookieStore.SetAsync(new CookieInit { Name = "c", Value = "3", Path = null! });
        await cookieStore.SetAsync("d", "4");

        Assert.Equal(
            [
                """[{"name":"a","value":"1","path":"/","sameSite":"strict","partitioned":false}]""",
                """[{"name":"b","value":"2","expires":1767225600000,"domain":"example.com","path":"/app","sameSite":"lax","partitioned":true,"maxAge":3600}]""",
                """[{"name":"c","value":"3","sameSite":"strict","partitioned":false}]""",
                """["d","4"]""",
            ],
            jsRuntime.ArgumentsSentTo("set"));
    }

    [Theory]
    [InlineData(CookieSameSite.Strict, "strict")]
    [InlineData(CookieSameSite.Lax, "lax")]
    [InlineData(CookieSameSite.None, "none")]
    public async Task SetAsync_SendsSameSiteAsTheWebIdlString(CookieSameSite sameSite, string expected)
    {
        var jsRuntime = new SerializingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        await cookieStore.SetAsync(new CookieInit { Name = "a", Value = "1", SameSite = sameSite });

        Assert.Contains($"\"sameSite\":\"{expected}\"", Assert.Single(jsRuntime.ArgumentsSentTo("set")));
    }

    [Fact]
    public async Task GetAsync_SendsOnlyTheOptionsThatAreSet()
    {
        var jsRuntime = new SerializingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        await cookieStore.GetAsync(new CookieStoreGetOptions());
        await cookieStore.GetAsync(new CookieStoreGetOptions { Name = "a" });
        await cookieStore.GetAllAsync(new CookieStoreGetOptions { Url = "https://localhost/" });

        Assert.Equal(["""[{}]""", """[{"name":"a"}]"""], jsRuntime.ArgumentsSentTo("get"));
        Assert.Equal(["""[{"url":"https://localhost/"}]"""], jsRuntime.ArgumentsSentTo("getAll"));
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteOptionsWithoutNullMembers()
    {
        var jsRuntime = new SerializingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        await cookieStore.DeleteAsync(new CookieStoreDeleteOptions { Name = "a" });
        await cookieStore.DeleteAsync(new CookieStoreDeleteOptions { Name = "b", Domain = "example.com", Path = "/app", Partitioned = true });
        await cookieStore.DeleteAsync(new CookieStoreDeleteOptions { Name = "c", Path = null! });

        Assert.Equal(
            [
                """[{"name":"a","path":"/","partitioned":false}]""",
                """[{"name":"b","domain":"example.com","path":"/app","partitioned":true}]""",
                """[{"name":"c","partitioned":false}]""",
            ],
            jsRuntime.ArgumentsSentTo("delete"));
    }

    [Fact]
    public async Task GetAllAsync_ReadsNameAndValueAndIgnoresOtherMembers()
    {
        // Chromium still returns members that were removed from the specification, such as domain and expires.
        var jsRuntime = new SerializingJSRuntime
        {
            GetAllResult = """[{"name":"a","value":"1","domain":null,"path":"/","expires":null,"secure":true,"sameSite":"strict","partitioned":false},{"name":"b","value":"2"}]""",
        };
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        var cookies = await cookieStore.GetAllAsync();

        Assert.Collection(
            cookies,
            cookie => Assert.Equal(("a", "1"), (cookie.Name, cookie.Value)),
            cookie => Assert.Equal(("b", "2"), (cookie.Name, cookie.Value)));
    }

    [Fact]
    public async Task GetAsync_ReadsTheCookieOrNull()
    {
        var jsRuntime = new SerializingJSRuntime
        {
            GetResult = """{"name":"a","value":"1","domain":null,"path":"/","expires":null,"secure":true,"sameSite":"strict","partitioned":false}""",
        };
        await using var provider = CreateServiceProvider(jsRuntime);
        var cookieStore = provider.GetRequiredService<IBrowserPlatform>().Window.CookieStore;

        var cookie = await cookieStore.GetAsync("a");
        jsRuntime.GetResult = "null";
        var missing = await cookieStore.GetAsync("missing");

        Assert.Equal(("a", "1"), (cookie?.Name, cookie?.Value));
        Assert.Null(missing);
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

        public bool CancelNextGetValue { get; set; }

        public bool CookieStoreNull { get; set; }

        public Exception? GetValueException { get; set; }

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

            if (GetValueException is { } exception)
            {
                return ValueTask.FromException<TValue>(exception);
            }

            // JS interop returns a null reference when window.cookieStore exists and holds null or undefined.
            return ValueTask.FromResult(CookieStoreNull ? default! : (TValue)(object)ObjectReference);
        }
    }

    private sealed class RecordingJSObjectReference : IJSObjectReference
    {
        private static readonly CookieListItem _storedCookie = new() { Name = "name", Value = "stored-value" };

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
                "get" => _storedCookie,
                "getAll" => new[] { _storedCookie },
                _ => default(TValue),
            };

            return ValueTask.FromResult((TValue?)result!);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    private sealed record Invocation(string Identifier, object?[] Arguments);

    // Runs the real JSRuntime serializer and answers each call as the browser would.
    private sealed class SerializingJSRuntime : JSRuntime
    {
        private readonly List<(string Identifier, string? ArgsJson)> _calls = [];

        public string GetAllResult { get; init; } = "[]";

        public string GetResult { get; set; } = "null";

        public string[] ArgumentsSentTo(string identifier)
            => [.. _calls.Where(call => call.Identifier == identifier).Select(call => call.ArgsJson ?? string.Empty)];

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            _calls.Add((invocationInfo.Identifier, invocationInfo.ArgsJson));

            var result = invocationInfo.Identifier switch
            {
                "cookieStore" => """{"__jsObjectId":1}""",
                "get" => GetResult,
                "getAll" => GetAllResult,
                _ => "null",
            };

            if (invocationInfo.AsyncHandle != 0)
            {
                DotNetDispatcher.EndInvokeJS(this, $"[{invocationInfo.AsyncHandle},true,{result}]");
            }
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotSupportedException();

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => throw new NotSupportedException();
    }
}
