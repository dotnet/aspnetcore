// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using AIApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.AI;

namespace AIApp.E2E.Tests.ServiceOverrides;

internal sealed class GatedReplayChatClient : IChatClient
{
    private readonly ReplayCheckpointScript _script;
    private readonly TestLockProvider _locks;
    private readonly TestSessionContext _session;
    private readonly NavigationManager? _navigation;
    private readonly ReplayCheckpointState? _checkpointState;
    private int _callIndex;

    public GatedReplayChatClient(
        ReplayCheckpointScript script,
        TestLockProvider locks,
        TestSessionContext session,
        NavigationManager? navigation = null,
        ReplayCheckpointState? checkpointState = null)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(locks);
        ArgumentNullException.ThrowIfNull(session);
        _script = script;
        _locks = locks;
        _session = session;
        _navigation = navigation;
        _checkpointState = checkpointState;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sessionId = GetSessionId();
        var callIndex = _callIndex++;
        if (callIndex >= _script.Calls.Count)
        {
            throw new InvalidOperationException(
                $"Replay script has {_script.Calls.Count} calls but call {callIndex + 1} was requested.");
        }

        var call = _script.Calls[callIndex];
        AssertRequest(call.Request, messages, options, callIndex);

        for (var checkpointIndex = 0; checkpointIndex < call.Frames.Count; checkpointIndex++)
        {
            var frame = call.Frames[checkpointIndex];
            foreach (var update in frame.Updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
            }

            var lockKey = $"{sessionId}:{_script.GetLockName(callIndex, checkpointIndex)}";
            _checkpointState?.SetCheckpoint(frame.Name);
            try
            {
                await _locks.WaitOn(lockKey).WaitAsync(cancellationToken);
            }
            finally
            {
                _checkpointState?.ClearCheckpoint();
            }
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : null;

    public void Dispose()
    {
    }

    private string GetSessionId()
    {
        if (_session.Id is not null)
        {
            return _session.Id;
        }

        if (_navigation is not null &&
            QueryHelpers.ParseQuery(new Uri(_navigation.Uri).Query)
                .TryGetValue(ReplayTestSession.QueryParameterName, out var sessionId) &&
            !string.IsNullOrEmpty(sessionId))
        {
            _session.Id = sessionId.ToString();
            return _session.Id;
        }

        throw new InvalidOperationException("A test session is required for gated replay.");
    }

    private static void AssertRequest(
        ReplayRequestExpectation? expectation,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        int callIndex)
    {
        if (expectation is null)
        {
            return;
        }

        var messageList = messages.ToList();
        if (expectation.MessageCount is int messageCount && messageList.Count != messageCount)
        {
            throw new InvalidOperationException(
                $"Replay call {callIndex + 1} expected {messageCount} messages but received {messageList.Count}.");
        }

        if (expectation.LastUserMessage is { } expectedText)
        {
            var actualText = messageList.LastOrDefault(message => message.Role == ChatRole.User)?.Text;
            if (!string.Equals(expectedText, actualText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Replay call {callIndex + 1} expected last user message '{expectedText}' but received '{actualText}'.");
            }
        }

        if (expectation.ToolNames is { } expectedToolNames)
        {
            var actualToolNames = options?.Tools?
                .Select(tool => tool.Name)
                .Order(StringComparer.Ordinal)
                .ToArray() ?? [];
            var orderedExpectedToolNames = expectedToolNames.Order(StringComparer.Ordinal).ToArray();
            if (!actualToolNames.SequenceEqual(orderedExpectedToolNames, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Replay call {callIndex + 1} expected tools [{string.Join(", ", orderedExpectedToolNames)}] " +
                    $"but received [{string.Join(", ", actualToolNames)}].");
            }
        }

        if (expectation.FunctionResult is { } expectedFunctionResult)
        {
            var actualFunctionResult = messageList
                .Where(message => message.Role == ChatRole.Tool)
                .SelectMany(message => message.Contents.OfType<FunctionResultContent>())
                .LastOrDefault();
            if (actualFunctionResult is null)
            {
                throw new InvalidOperationException(
                    $"Replay call {callIndex + 1} expected a function result but received none.");
            }

            var actualResult = actualFunctionResult.Result?.ToString();
            if (!string.Equals(
                    expectedFunctionResult.CallId,
                    actualFunctionResult.CallId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    expectedFunctionResult.Result,
                    actualResult,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Replay call {callIndex + 1} expected function result " +
                    $"'{expectedFunctionResult.CallId}: {expectedFunctionResult.Result}' but received " +
                    $"'{actualFunctionResult.CallId}: {actualResult}'.");
            }
        }
    }
}
