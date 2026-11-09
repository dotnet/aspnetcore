// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Components.McpTestApp.Hosting;

public sealed class InitialDataTools(InitialFormState state)
{
    [McpServerTool(Name = "open_initial_form")]
    public CallToolResult Open(InitialToolArguments input) => state.Open(input);

    [McpServerTool(Name = "unassociated_initial_form")]
    public CallToolResult Unassociated(InitialToolArguments input) => state.Open(input);
}

public sealed class InitialToolArguments
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool Error { get; set; }
    public bool Throw { get; set; }
}

public sealed class InitialFormData
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
}

public sealed class InitialFormState
{
    private readonly object _lock = new();
    private CallToolResult _sharedResult = CreateResult();
    private int _openingCalls;
    private int _projectionCalls;
    private int _renders;
    private int _submissions;

    public CallToolResult Open(InitialToolArguments input)
    {
        Interlocked.Increment(ref _openingCalls);
        if (input.Throw)
        {
            throw new McpException("The business opening tool failed.");
        }

        lock (_lock)
        {
            _sharedResult.IsError = input.Error;
            _sharedResult.Content = [new TextContentBlock { Text = input.Error ? "Business error." : $"Business result: {input.Label}." }];
            _sharedResult.StructuredContent = JsonSerializer.SerializeToElement(
                new InitialFormData { Label = input.Label, Count = input.Count }, McpJsonUtilities.DefaultOptions);
            return _sharedResult;
        }
    }

    public InitialFormData Project(CallToolResult result)
    {
        Interlocked.Increment(ref _projectionCalls);
        return result.StructuredContent is { } content
            ? content.Deserialize<InitialFormData>(McpJsonUtilities.DefaultOptions)!
            : throw new InvalidOperationException("The opening tool did not return initial data.");
    }

    public void Rendered() => Interlocked.Increment(ref _renders);
    public void Submitted() => Interlocked.Increment(ref _submissions);

    public object Read()
    {
        lock (_lock)
        {
            return new
            {
                OpeningCalls = Volatile.Read(ref _openingCalls),
                ProjectionCalls = Volatile.Read(ref _projectionCalls),
                Renders = Volatile.Read(ref _renders),
                Submissions = Volatile.Read(ref _submissions),
                SharedResultHasViewMetadata = _sharedResult.Meta?.ContainsKey("blazor") == true,
            };
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _sharedResult = CreateResult();
            _openingCalls = 0;
            _projectionCalls = 0;
            _renders = 0;
            _submissions = 0;
        }
    }

    private static CallToolResult CreateResult()
        => new()
        {
            Content = [],
            Meta = new JsonObject { ["business"] = "preserved" },
        };
}
