// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpAppsServer;

[McpServerToolType]
public sealed class NoteTools(NoteStore store)
{
    [McpServerTool(Name = "show_note"), Description("Open the standard note app.")]
    [McpMeta("ui", null, JsonValue = """{"resourceUri":"ui://note/app.html"}""")]
    public CallToolResult Show(string title)
    {
        Interlocked.Increment(ref store.InitialCalls);

        return Result(store.Message, title);
    }

    [McpServerTool(Name = "save_note"), Description("Save a note from the app.")]
    [McpMeta("ui", null, JsonValue = """{"visibility":["app"]}""")]
    public CallToolResult Save(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Length > 1000)
        {
            throw new ArgumentException("The note exceeds 1000 characters.", nameof(text));
        }

        Interlocked.Increment(ref store.SavedCalls);
        store.Message = $"Saved: {text}";

        return Result(store.Message, "Saved note");
    }

    [McpServerTool(Name = "model_only"), Description("A tool not available to the app.")]
    [McpMeta("ui", null, JsonValue = """{"visibility":["model"]}""")]
    public string ModelOnly()
    {
        Interlocked.Increment(ref store.ModelOnlyCalls);

        return "Model-only tool executed.";
    }

    [McpServerTool(Name = "wait_for_cancellation"), Description("Wait until the app cancels.")]
    [McpMeta("ui", null, JsonValue = """{"visibility":["app"]}""")]
    public async Task<string> WaitAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref store.ActiveCalls);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

            return "Completed.";
        }
        finally
        {
            Interlocked.Decrement(ref store.ActiveCalls);
            if (cancellationToken.IsCancellationRequested)
            {
                Interlocked.Increment(ref store.CancelledCalls);
            }
        }
    }

    private CallToolResult Result(string message, string title)
    {
        return new()
        {
            Content = [new TextContentBlock { Text = message }],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                message,
                title,
                initialCalls = store.InitialCalls,
                assetUrl = new Uri(store.Origin, "/asset.svg").AbsoluteUri,
                socketUrl = store.SocketOrigin + "/socket",
            }),
            Meta = new JsonObject { ["viewOnly"] = "Not for the model." },
        };
    }
}
