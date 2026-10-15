// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

// Runs the real JSRuntime serializer and dispatcher, and plays the browser side of each call.
internal sealed class BrowserJSRuntime : JSRuntime
{
    private readonly Dictionary<string, TaskCompletionSource<DotNetInvocationResult>> _pendingDotNetCalls = [];
    private long _nextObjectId = 1;
    private long _nextCallId = 1;

    public List<BrowserCall> Calls { get; } = [];

    public IEnumerable<BrowserCall> CallsTo(string identifier) => Calls.Where(call => call.Identifier == identifier);

    // Calls the Invoke method of the DotNetObjectReference that the last createCallback call received.
    public async Task<DotNetInvocationResult> InvokeLastCallbackAsync(string argsJson)
    {
        var createCallback = CallsTo("createCallback").Last();
        using var args = JsonDocument.Parse(createCallback.ArgsJson);
        var dotNetObjectId = args.RootElement[0].GetProperty("__dotNetObject").GetInt64();
        var callId = (_nextCallId++).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var completion = new TaskCompletionSource<DotNetInvocationResult>();
        _pendingDotNetCalls[callId] = completion;

        DotNetDispatcher.BeginInvokeDotNet(this, new DotNetInvocationInfo(null, "Invoke", dotNetObjectId, callId), argsJson);

        return await completion.Task;
    }

    protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
    {
        Calls.Add(new BrowserCall(invocationInfo.Identifier, invocationInfo.CallType, invocationInfo.TargetInstanceId, invocationInfo.ArgsJson));

        var result = invocationInfo.ResultType == JSCallResultType.JSObjectReference
            ? $$"""{"__jsObjectId":{{_nextObjectId++}}}"""
            : "null";

        if (invocationInfo.AsyncHandle != 0)
        {
            DotNetDispatcher.EndInvokeJS(this, $"[{invocationInfo.AsyncHandle},true,{result}]");
        }
    }

    protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        => throw new NotSupportedException();

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
        _pendingDotNetCalls.Remove(invocationInfo.CallId!, out var completion);
        completion!.SetResult(invocationResult);
    }
}

internal sealed record BrowserCall(string Identifier, JSCallType CallType, long TargetInstanceId, string ArgsJson);
