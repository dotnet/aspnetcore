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
    private readonly HashSet<string> _identifiersToFail = [];
    private readonly HashSet<string> _identifiersToHold = [];
    private readonly List<(string Identifier, long AsyncHandle, string Result)> _heldCalls = [];
    private long _nextObjectId = 1;
    private long _nextCallId = 1;

    public List<BrowserCall> Calls { get; } = [];

    // Completes calls from a thread pool thread, as a transport that does not use the renderer's dispatcher does.
    public bool CompleteOnThreadPool { get; init; }

    public IEnumerable<BrowserCall> CallsTo(string identifier) => Calls.Where(call => call.Identifier == identifier);

    // Makes the next call to the identifier fail, as a browser exception or a timeout would.
    public void FailNextCallTo(string identifier) => _identifiersToFail.Add(identifier);

    // Keeps the calls to the identifier pending until ReleaseHeldCalls.
    public void HoldCallsTo(string identifier) => _identifiersToHold.Add(identifier);

    public void ReleaseHeldCalls(string identifier, bool succeeded = true)
    {
        _identifiersToHold.Remove(identifier);

        foreach (var held in _heldCalls.Where(held => held.Identifier == identifier).ToArray())
        {
            _heldCalls.Remove(held);
            Complete(held.AsyncHandle, succeeded, succeeded ? held.Result : $"\"{identifier} failed.\"");
        }
    }

    // Calls the Invoke method of the DotNetObjectReference that the last createCallback call received.
    public Task<DotNetInvocationResult> InvokeLastCallbackAsync(string argsJson)
        => InvokeCallbackAsync(CallsTo("createCallback").Count() - 1, argsJson);

    // Calls the Invoke method of the DotNetObjectReference that the createCallback call at the index received.
    public async Task<DotNetInvocationResult> InvokeCallbackAsync(int index, string argsJson)
    {
        var createCallback = CallsTo("createCallback").ElementAt(index);
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
        Calls.Add(new BrowserCall(
            invocationInfo.Identifier,
            invocationInfo.CallType,
            invocationInfo.TargetInstanceId,
            invocationInfo.ArgsJson,
            SynchronizationContext.Current));

        if (invocationInfo.AsyncHandle == 0)
        {
            return;
        }

        if (_identifiersToFail.Remove(invocationInfo.Identifier))
        {
            Complete(invocationInfo.AsyncHandle, succeeded: false, $"\"{invocationInfo.Identifier} failed.\"");
            return;
        }

        var result = invocationInfo.ResultType == JSCallResultType.JSObjectReference
            ? $$"""{"__jsObjectId":{{_nextObjectId++}}}"""
            : "null";

        if (_identifiersToHold.Contains(invocationInfo.Identifier))
        {
            _heldCalls.Add((invocationInfo.Identifier, invocationInfo.AsyncHandle, result));
            return;
        }

        Complete(invocationInfo.AsyncHandle, succeeded: true, result);
    }

    protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        => throw new NotSupportedException();

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
        _pendingDotNetCalls.Remove(invocationInfo.CallId!, out var completion);
        completion!.SetResult(invocationResult);
    }

    private void Complete(long asyncHandle, bool succeeded, string result)
    {
        var json = $"[{asyncHandle},{(succeeded ? "true" : "false")},{result}]";

        if (CompleteOnThreadPool)
        {
            ThreadPool.QueueUserWorkItem(_ => DotNetDispatcher.EndInvokeJS(this, json));
        }
        else
        {
            DotNetDispatcher.EndInvokeJS(this, json);
        }
    }
}

internal sealed record BrowserCall(
    string Identifier,
    JSCallType CallType,
    long TargetInstanceId,
    string ArgsJson,
    SynchronizationContext? Context);
