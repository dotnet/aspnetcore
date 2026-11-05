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
        store.Saved.TrySetResult();

        return Result(store.Message, "Saved note");
    }

    [McpServerTool(Name = "edit_note"), Description("Open the note app and wait for its user to save.")]
    [McpMeta("ui", null, JsonValue = """{"resourceUri":"ui://note/app.html"}""")]
    public async Task<CallToolResult> EditAsync(string title, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref store.InitialCalls);
        Interlocked.Increment(ref store.ActiveInitialCalls);
        try
        {
            await store.Saved.Task.WaitAsync(cancellationToken);

            return Result(store.Message, title);
        }
        finally
        {
            Interlocked.Decrement(ref store.ActiveInitialCalls);
            if (cancellationToken.IsCancellationRequested)
            {
                Interlocked.Increment(ref store.CancelledInitialCalls);
            }
        }
    }

    [McpServerTool(Name = "fail_note"), Description("Open a note app with an MCP error result.")]
    [McpMeta("ui", null, JsonValue = """{"resourceUri":"ui://note/app.html"}""")]
    public CallToolResult Fail(string title)
    {
        Interlocked.Increment(ref store.InitialCalls);
        var result = Result("The note operation failed.", title);
        result.IsError = true;

        return result;
    }

    [McpServerTool(Name = "model_only"), Description("A tool not available to the app.")]
    [McpMeta("ui", null, JsonValue = """{"visibility":["model"]}""")]
    public string ModelOnly()
    {
        Interlocked.Increment(ref store.ModelOnlyCalls);

        return "Model-only tool executed.";
    }

    [McpServerTool(Name = "show_content"), Description("Open an app with text and optionally an image.")]
    [McpMeta("ui", null, JsonValue = """{"resourceUri":"ui://note/app.html"}""")]
    public CallToolResult ShowContent(string title, bool includeImage)
    {
        Interlocked.Increment(ref store.InitialCalls);
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = title, Meta = new() { ["contentOnly"] = "Preserved" } }]
        };
        if (includeImage)
        {
            result.Content.Add(ImageContentBlock.FromBytes(Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a0yAAAAAASUVORK5CYII="), "image/png"));
        }

        return result;
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

    [McpServerTool(Name = "echo_payload"), Description("Echo the app's payload without modifying it.")]
    [McpMeta("ui", null, JsonValue = """{"visibility":["app"]}""")]
    public CallToolResult Echo(string payload) => new()
    {
        Content = [new TextContentBlock { Text = payload }],
        StructuredContent = JsonSerializer.SerializeToElement(new { payload })
    };

    private CallToolResult Result(string message, string title)
    {
        var result = new CallToolResult
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
        if (store.Payload.Length != 0)
        {
            result.Meta["payload"] = store.Payload;
        }
        return result;
    }
}
