// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpContentDispatcherTests
{
    private const TaskCreationOptions CreationOptions = TaskCreationOptions.RunContinuationsAsynchronously;
    private const string Request = """{"method":"tools/call","parameters":{"value":1}}""";

    [Fact]
    public async Task DispatchPreservesMcpRequestAndResponse()
    {
        using var dispatcher = CreateDispatcher(static (request, _) =>
        {
            Assert.Equal("tools/call", request.Method);
            return ValueTask.FromResult(new McpAppResponse(request.Parameters));
        });

        var json = await dispatcher.DispatchAsync("request", Request);

        var response = JsonSerializer.Deserialize(json, McpAppJsonContext.Default.McpAppResponse)!;
        Assert.Equal(1, response.Result!.Value.GetProperty("value").GetInt32());
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task DuplicateIdentityDoesNotCancelOrRemoveOriginalOperation()
    {
        var entered = new TaskCompletionSource(CreationOptions);
        var release = new TaskCompletionSource<McpAppResponse>(CreationOptions);
        using var dispatcher = CreateDispatcher(async (_, token) =>
        {
            entered.TrySetResult();
            return await release.Task.WaitAsync(token);
        });
        var original = dispatcher.DispatchAsync("same", Request);
        await entered.Task;

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("same", Request));
        Assert.False(original.IsCompleted);
        release.SetResult(new(McpAppPolicy.Wire(17)));

        Assert.Equal(17, ReadResult(await original).GetInt32());
        Assert.Equal(17, ReadResult(await dispatcher.DispatchAsync("same", Request)).GetInt32());
    }

    [Fact]
    public async Task CancellingOneOperationDoesNotCancelAnother()
    {
        var entered = new TaskCompletionSource(CreationOptions);
        using var dispatcher = CreateDispatcher(async (request, token) =>
        {
            if (request.Method == "tools/call")
            {
                entered.SetResult();
                await Task.Delay(Timeout.Infinite, token);
            }
            return new(McpAppPolicy.Wire(request.Method));
        });
        var pending = dispatcher.DispatchAsync("first", Request);
        await entered.Task;

        dispatcher.Cancel("first");
        dispatcher.Cancel("unknown");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal("tools/list", ReadResult(await dispatcher.DispatchAsync("second",
            """{"method":"tools/list","parameters":{}}""")).GetString());
    }

    [Fact]
    public async Task ClosingCancelsPendingAndRejectsFutureWork()
    {
        var entered = new TaskCompletionSource(CreationOptions);
        var dispatcher = CreateDispatcher(async (_, token) =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return new();
        });
        var pending = dispatcher.DispatchAsync("pending", Request);
        await entered.Task;

        dispatcher.Dispose();
        dispatcher.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => dispatcher.DispatchAsync("late", Request));
    }

    [Fact]
    public async Task ConcurrencyLimitIsReleasedAfterCompletion()
    {
        var entered = new TaskCompletionSource(CreationOptions);
        var release = new TaskCompletionSource<McpAppResponse>(CreationOptions);
        using var dispatcher = CreateDispatcher(async (_, token) =>
        {
            entered.TrySetResult();
            return await release.Task.WaitAsync(token);
        }, options => options.MaximumPendingRequests = 1);
        var first = dispatcher.DispatchAsync("first", Request);
        await entered.Task;

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("second", Request));
        release.SetResult(new(McpAppPolicy.Wire(2)));
        await first;

        Assert.Equal(2, ReadResult(await dispatcher.DispatchAsync("third", Request)).GetInt32());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task RequestByteLimitIsInclusive(int allowance)
    {
        using var dispatcher = CreateDispatcher(static (_, _) => ValueTask.FromResult(new McpAppResponse()),
            options => options.MaximumMessageBytes = Encoding.UTF8.GetByteCount(Request) + allowance);

        if (allowance < 0)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("request", Request));
        }
        else
        {
            Assert.Equal("{}", await dispatcher.DispatchAsync("request", Request));
        }
    }

    [Fact]
    public async Task OversizedResponseFailsRatherThanReturningTruncatedJson()
    {
        using var dispatcher = CreateDispatcher(static (_, _) =>
            ValueTask.FromResult(new McpAppResponse(McpAppPolicy.Wire(new string('x', 100)))),
            options => options.MaximumMessageBytes = Encoding.UTF8.GetByteCount(Request));

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("request", Request));
    }

    [Fact]
    public async Task UncooperativeHandlerKeepsItsSlotAfterDeadline()
    {
        var release = new TaskCompletionSource<McpAppResponse>(CreationOptions);
        using var dispatcher = CreateDispatcher((_, _) => new(release.Task), options =>
        {
            options.MaximumPendingRequests = 1;
            options.OperationTimeout = TimeSpan.FromMilliseconds(20);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchAsync("first", Request));
        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("second", Request));

        release.SetResult(new());
    }

    [Fact]
    public async Task InvalidJsonDoesNotPoisonRequestIdentity()
    {
        using var dispatcher = CreateDispatcher(static (_, _) => ValueTask.FromResult(new McpAppResponse()));

        await Assert.ThrowsAsync<JsonException>(() => dispatcher.DispatchAsync("request", "{"));

        Assert.Equal("{}", await dispatcher.DispatchAsync("request", Request));
    }

    [Fact]
    public void ProxyDocumentHasNoGenericHostDependency()
    {
        var html = McpAppProxy.CreateDocument(new("https://host.example/path"),
            new("https://proxy.example"), null);

        Assert.Contains("\"https://host.example\"", html);
        Assert.Contains("Microsoft.AspNetCore.Components.AI.Mcp/mcp-proxy.js", html);
        Assert.DoesNotContain("host.example/path", html);
        Assert.DoesNotContain("isolated-content", html);
    }

    private static JsonElement ReadResult(string json) =>
        JsonSerializer.Deserialize(json, McpAppJsonContext.Default.McpAppResponse)!.Result!.Value;

    private static McpContentDispatcher CreateDispatcher(
        Func<McpAppRequest, CancellationToken, ValueTask<McpAppResponse>> handler,
        Action<McpContentOptions>? configure = null)
    {
        var options = new McpContentOptions { ProxyUrl = new("https://proxy.example/proxy") };
        configure?.Invoke(options);
        return new(options, handler);
    }
}
