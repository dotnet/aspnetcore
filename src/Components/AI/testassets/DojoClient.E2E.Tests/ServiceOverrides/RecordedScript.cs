// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace DojoClient.E2E.Tests.ServiceOverrides;

// A recorded model response, replayed by AGUIDojoApi so browser tests are deterministic.
//
// A call is selected by the text of the last user message, so a test can pick its own script
// and give every run a unique lock namespace by appending a run id to the message it types.
internal sealed class RecordedScript
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object _threadLock = new();
    private string? _threadId;

    public required List<RecordedCall> Calls { get; init; }

    public static RecordedScript Load(string recordingFileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(recordingFileName);

        var assemblyDirectory = Path.GetDirectoryName(typeof(RecordedScript).Assembly.Location)
            ?? throw new InvalidOperationException("Could not locate the E2E test assembly.");
        var path = Path.Combine(assemblyDirectory, "Baselines", recordingFileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Recording not found: {path}", path);
        }

        return JsonSerializer.Deserialize<RecordedScript>(File.ReadAllText(path), SerializerOptions)
            ?? throw new InvalidOperationException($"Recording decoded to null: {recordingFileName}");
    }

    public RecordedCall GetCall(string lastUserMessage, int messageCount)
    {
        foreach (var call in Calls)
        {
            if (lastUserMessage.StartsWith(call.Prompt, StringComparison.Ordinal) &&
                (call.MessageCount is null || call.MessageCount == messageCount))
            {
                return call;
            }
        }

        throw new InvalidOperationException(
            $"No recorded call matches the last user message '{lastUserMessage}' " +
            $"with {messageCount} messages. " +
            $"Recorded prompts: {string.Join(", ", Calls.Select(call => call.Prompt))}.");
    }

    public void AssertStableThread(string threadId)
    {
        ArgumentException.ThrowIfNullOrEmpty(threadId);

        lock (_threadLock)
        {
            _threadId ??= threadId;
            if (_threadId != threadId)
            {
                throw new InvalidOperationException(
                    $"Expected AG-UI thread '{_threadId}', received '{threadId}'.");
            }
        }
    }
}

internal sealed class RecordedCall
{
    /// <summary>The prefix of the last user message this call answers.</summary>
    public required string Prompt { get; init; }

    /// <summary>The message count that distinguishes continuations of the same prompt.</summary>
    public int? MessageCount { get; init; }

    /// <summary>The tool declarations expected on this model request.</summary>
    public List<string>? ToolNames { get; init; }

    /// <summary>The function result call IDs expected on this model request.</summary>
    public List<string>? ToolResultCallIds { get; init; }

    /// <summary>The function results expected on this model request.</summary>
    public List<RecordedToolResult>? ToolResults { get; init; }

    /// <summary>The AG-UI state expected on this model request.</summary>
    public JsonElement? State { get; init; }

    /// <summary>Whether this call must use the same non-empty AG-UI thread as prior calls.</summary>
    public bool RequireStableThread { get; init; }

    /// <summary>The response, split into the checkpoints a test can stop at.</summary>
    public required List<RecordedFrame> Frames { get; init; }
}

internal sealed class RecordedFrame
{
    /// <summary>Name of the checkpoint, used to build the lock key that gates it.</summary>
    public required string Name { get; init; }

    /// <summary>The text chunks streamed for this checkpoint.</summary>
    public List<string> Chunks { get; init; } = [];

    /// <summary>A function call emitted at this checkpoint.</summary>
    public RecordedFunctionCall? FunctionCall { get; init; }
}

internal sealed class RecordedFunctionCall
{
    public required string CallId { get; init; }

    public required string Name { get; init; }

    public Dictionary<string, object?> Arguments { get; init; } = [];
}

internal sealed class RecordedToolResult
{
    public required string CallId { get; init; }

    public required string Result { get; init; }
}
