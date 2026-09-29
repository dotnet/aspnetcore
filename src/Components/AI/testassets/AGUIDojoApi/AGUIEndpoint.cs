// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using AGUI.Abstractions;
using AGUI.Formatting;
using AGUI.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace AGUIDojoApi;

internal static class AGUIEndpoint
{
    /// <summary>
    /// Maps one AG-UI dojo endpoint: it accepts a <see cref="RunAgentInput"/>, turns it into a
    /// chat request, streams the model response, and writes it back as AG-UI events.
    /// </summary>
    internal static IEndpointConventionBuilder MapDojoEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        IList<AITool>? serverTools = null,
        string? systemPrompt = null,
        Func<JsonSerializerOptions, AGUIStreamOptions>? configureStreamOptions = null,
        object? chatClientKey = null,
        bool treatClientToolsAsDeclarations = false)
    {
        return endpoints.MapPost(pattern, (
            [FromBody] RunAgentInput input,
            [FromServices] IOptions<JsonOptions> jsonOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var jsonSerializerOptions = jsonOptions.Value.SerializerOptions;
            var chatClient = chatClientKey is null
                ? httpContext.RequestServices.GetRequiredService<IChatClient>()
                : httpContext.RequestServices.GetRequiredKeyedService<IChatClient>(chatClientKey);

            var streamOptions = configureStreamOptions?.Invoke(jsonSerializerOptions) ??
                new AGUIStreamOptions();
            var clientTools = input.Tools;
            if (treatClientToolsAsDeclarations)
            {
                input.Tools = null;
            }

            var ctx = input.ToChatRequestContext(jsonSerializerOptions, streamOptions);
            input.Tools = clientTools;

            // A raw model returns these calls to the browser instead of executing them in the
            // mixed-invocation pipeline. Server-owned tools supersede duplicate declarations.
            if (treatClientToolsAsDeclarations && clientTools is { Count: > 0 })
            {
                var serverToolNames = serverTools?
                    .Select(tool => tool.Name)
                    .ToHashSet(StringComparer.Ordinal) ?? [];
                ctx.ChatOptions.Tools ??= [];
                foreach (var tool in clientTools.AsAITools())
                {
                    if (!serverToolNames.Contains(tool.Name))
                    {
                        ctx.ChatOptions.Tools.Add(tool);
                    }
                }
            }

            // Inject system prompt if provided
            if (systemPrompt is not null)
            {
                ctx.Messages.Insert(0, new ChatMessage(ChatRole.System, systemPrompt));
            }

            // Add server tools alongside any approval-wrapped client tools already
            // installed by ToChatRequestContext.
            if (serverTools is { Count: > 0 })
            {
                ctx.ChatOptions.Tools ??= [];
                foreach (var tool in serverTools)
                {
                    ctx.ChatOptions.Tools.Add(tool);
                }
            }

            var updates = chatClient.GetStreamingResponseAsync(ctx.Messages, ctx.ChatOptions, cancellationToken);

            var events = updates.AsAGUIEventStreamAsync(ctx, cancellationToken);

            return new AGUIEventStreamResult(events, new SseEventStreamFormatter(), cancellationToken);
        });
    }
}
