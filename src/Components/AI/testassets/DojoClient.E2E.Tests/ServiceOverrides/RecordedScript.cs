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
    public required string Prompt { get; init; }

    public int? MessageCount { get; init; }

    public List<string>? ToolNames { get; init; }

    public List<string>? ToolResultCallIds { get; init; }

    public List<RecordedToolResult>? ToolResults { get; init; }

    public JsonElement? State { get; init; }

    public bool RequireStableThread { get; init; }

    public required List<RecordedFrame> Frames { get; init; }
}

internal sealed class RecordedFrame
{
    public required string Name { get; init; }

    public List<string> Chunks { get; init; } = [];

    public JsonElement? State { get; init; }

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
