// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpContentDispatcherTests
{
    private const TaskCreationOptions CreationOptions = TaskCreationOptions.RunContinuationsAsynchronously;

    [Fact]
    public async Task DispatchPreservesSdkRequestResponseAndIdentity()
    {
        using var dispatcher = new McpContentDispatcher(static (request, _) =>
        {
            Assert.Equal(RequestMethods.ToolsCall, request.Method);
            return ValueTask.FromResult<JsonRpcMessage>(new JsonRpcResponse
            {
                Id = request.Id, Result = request.Params
            });
        });

        var json = await dispatcher.DispatchAsync(Request("request", new JsonObject { ["value"] = 1 }));

        var response = Assert.IsType<JsonRpcResponse>(JsonSerializer.Deserialize<JsonRpcMessage>(json, McpJsonUtilities.DefaultOptions));
        Assert.Equal("2.0", response.JsonRpc);
        Assert.Equal(new RequestId("request"), response.Id);
        Assert.Equal(1, response.Result!["value"]!.GetValue<int>());
    }

    [Fact]
    public async Task SdkErrorResponseRetainsCodeMessageDataAndId()
    {
        using var dispatcher = new McpContentDispatcher(static (request, _) =>
            ValueTask.FromResult<JsonRpcMessage>(new JsonRpcError
            {
                Id = request.Id,
                Error = new() { Code = -32601, Message = "Unsupported", Data = new JsonObject { ["method"] = request.Method } }
            }));

        var json = await dispatcher.DispatchAsync(Request("error"));

        var response = Assert.IsType<JsonRpcError>(JsonSerializer.Deserialize<JsonRpcMessage>(json, McpJsonUtilities.DefaultOptions));
        Assert.Equal(new RequestId("error"), response.Id);
        Assert.Equal(-32601, response.Error.Code);
        Assert.Equal("Unsupported", response.Error.Message);
        Assert.Equal(RequestMethods.ToolsCall, Assert.IsType<JsonElement>(response.Error.Data).GetProperty("method").GetString());
    }

    [Fact]
    public async Task DuplicateIdentityDoesNotCancelOrRemoveOriginalOperation()
    {
        var entered = new TaskCompletionSource(CreationOptions);
        var release = new TaskCompletionSource(CreationOptions);
        using var dispatcher = new McpContentDispatcher(async (request, token) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return Response(request, 17);
        });
        var original = dispatcher.DispatchAsync(Request("same"));
        await entered.Task;

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(Request("same")));
        Assert.False(original.IsCompleted);
        release.SetResult();

        Assert.Equal(17, ReadResult(await original)!.GetValue<int>());
        Assert.Equal(17, ReadResult(await dispatcher.DispatchAsync(Request("same")))!.GetValue<int>());
    }

    [Fact]
    public async Task CancellingOneOperationDoesNotCancelAnother()
    {
        var entered = new TaskCompletionSource(CreationOptions);
        using var dispatcher = new McpContentDispatcher(async (request, token) =>
        {
            if (request.Id == new RequestId("first"))
            {
                entered.SetResult();
                await Task.Delay(Timeout.Infinite, token);
            }
            return Response(request, 2);
        });
        var pending = dispatcher.DispatchAsync(Request("first"));
        await entered.Task;

        dispatcher.Cancel(new("first"));
        dispatcher.Cancel(new("unknown"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(2, ReadResult(await dispatcher.DispatchAsync(Request("second")))!.GetValue<int>());
    }

    [Fact]
    public async Task ClosingCancelsPendingAndRejectsFutureWork()
    {
        var entered = new TaskCompletionSource(CreationOptions);
        var dispatcher = new McpContentDispatcher(async (request, token) =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return Response(request, 0);
        });
        var pending = dispatcher.DispatchAsync(Request("pending"));
        await entered.Task;

        dispatcher.Dispose();
        dispatcher.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => dispatcher.DispatchAsync(Request("late")));
    }

    [Fact]
    public async Task ConcurrentOperationsAreNotLimitedToThirtyTwo()
    {
        var release = new TaskCompletionSource(CreationOptions);
        using var dispatcher = new McpContentDispatcher(async (request, token) =>
        {
            await release.Task.WaitAsync(token);
            return Response(request, 2);
        });
        var pending = Enumerable.Range(0, 33)
            .Select(id => dispatcher.DispatchAsync(Request(id.ToString(System.Globalization.CultureInfo.InvariantCulture)))).ToArray();

        Assert.All(pending, operation => Assert.False(operation.IsCompleted));
        release.SetResult();
        await Task.WhenAll(pending);

        Assert.Equal(2, ReadResult(await dispatcher.DispatchAsync(Request("next")))!.GetValue<int>());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task RequestSizeDoesNotImposeTwoMiBLimit(int allowance)
    {
        using var dispatcher = new McpContentDispatcher(static (request, _) =>
            ValueTask.FromResult<JsonRpcMessage>(Response(request, 0)));
        var empty = Request("request", new JsonObject { ["value"] = "" });
        var json = Request("request", new JsonObject
        {
            ["value"] = new string('x', 2 * 1024 * 1024 - Encoding.UTF8.GetByteCount(empty) + allowance)
        });
        Assert.Equal(2 * 1024 * 1024 + allowance, Encoding.UTF8.GetByteCount(json));

        Assert.Equal(0, ReadResult(await dispatcher.DispatchAsync(json))!.GetValue<int>());
    }

    [Fact]
    public async Task LargeResponseIsReturnedIntact()
    {
        using var dispatcher = new McpContentDispatcher(static (request, _) =>
            ValueTask.FromResult<JsonRpcMessage>(new JsonRpcResponse
            {
                Id = request.Id, Result = JsonValue.Create(new string('x', 2 * 1024 * 1024 + 1))
            }));

        Assert.Equal(new string('x', 2 * 1024 * 1024 + 1),
            ReadResult(await dispatcher.DispatchAsync(Request("large")))!.GetValue<string>());
    }

    [Fact]
    public async Task CancelledUncooperativeHandlerRetainsItsIdentityUntilCompletion()
    {
        var release = new TaskCompletionSource(CreationOptions);
        using var dispatcher = new McpContentDispatcher(async (request, _) =>
        {
            await release.Task;
            return Response(request, 0);
        });
        var pending = dispatcher.DispatchAsync(Request("cancelled"));
        dispatcher.Cancel(new("cancelled"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(Request("cancelled")));
        release.SetResult();
    }

    [Fact]
    public async Task OperationDoesNotExpireAfterThirtySeconds()
    {
        var release = new TaskCompletionSource(CreationOptions);
        using var dispatcher = new McpContentDispatcher(async (request, token) =>
        {
            await release.Task.WaitAsync(token);
            return Response(request, 17);
        });
        var pending = dispatcher.DispatchAsync(Request("delayed"));

        await Task.Delay(TimeSpan.FromSeconds(31));
        release.SetResult();

        Assert.Equal(17, ReadResult(await pending)!.GetValue<int>());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("""{"jsonrpc":"1.0","id":"request","method":"tools/list","params":{}}""")]
    public async Task InvalidJsonDoesNotPoisonRequestIdentity(string json)
    {
        using var dispatcher = new McpContentDispatcher(static (request, _) =>
            ValueTask.FromResult<JsonRpcMessage>(Response(request, 0)));

        await Assert.ThrowsAsync<JsonException>(() => dispatcher.DispatchAsync(json));

        Assert.Equal(0, ReadResult(await dispatcher.DispatchAsync(Request("request")))!.GetValue<int>());
    }

    private static string Request(string id, JsonObject? parameters = null) =>
        JsonSerializer.Serialize(new JsonRpcRequest
        {
            Id = new(id), Method = RequestMethods.ToolsCall, Params = parameters ?? new JsonObject()
        }, McpJsonUtilities.DefaultOptions);

    private static JsonRpcResponse Response(JsonRpcRequest request, int value) =>
        new() { Id = request.Id, Result = JsonValue.Create(value) };

    private static JsonNode? ReadResult(string json) =>
        Assert.IsType<JsonRpcResponse>(JsonSerializer.Deserialize<JsonRpcMessage>(json, McpJsonUtilities.DefaultOptions)).Result;
}
