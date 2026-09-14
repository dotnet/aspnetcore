// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace DojoClient.E2E.Tests.ServiceOverrides;

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
                    $"Expected dojo thread '{_threadId}', received '{threadId}'.");
            }
        }
    }
}

internal sealed class RecordedCall
{
    /// <summary>The latest user message must start with this prompt.</summary>
    public required string Prompt { get; init; }

    /// <summary>Optional message count used to distinguish continuations of the same prompt.</summary>
    public int? MessageCount { get; init; }

    /// <summary>Expected tool declarations, in request order; null skips this check.</summary>
    public List<string>? ToolNames { get; init; }

    /// <summary>Expected result call IDs from tool-role messages, in order.</summary>
    public List<string>? ToolResultCallIds { get; init; }

    /// <summary>Expected JSON-encoded results, checked when ToolResultCallIds is supplied.</summary>
    public List<RecordedToolResult>? ToolResults { get; init; }

    /// <summary>Optional UI state that must be present in the model request.</summary>
    public JsonElement? State { get; init; }

    /// <summary>Requires one nonempty thread identity across this recording session's calls.</summary>
    public bool RequireStableThread { get; init; }

    /// <summary>Ordered response frames, separated by test-controlled checkpoint gates.</summary>
    public required List<RecordedFrame> Frames { get; init; }
}

internal sealed class RecordedFrame
{
    /// <summary>Checkpoint name released after this frame to allow the next frame to stream.</summary>
    public required string Name { get; init; }

    /// <summary>Text chunks emitted after any state and function call in this frame.</summary>
    public List<string> Chunks { get; init; } = [];

    /// <summary>Optional predictive-state snapshot emitted before other frame content.</summary>
    public JsonElement? State { get; init; }

    /// <summary>Optional native tool call emitted before this frame's text chunks.</summary>
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
