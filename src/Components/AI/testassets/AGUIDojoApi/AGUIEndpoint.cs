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
        Func<JsonSerializerOptions, AGUIStreamOptions>? configureStreamOptions = null)
    {
        return endpoints.MapPost(pattern, (
            [FromBody] RunAgentInput input,
            // The model client is resolved from DI rather than captured at map time so that the
            // E2E tests can replace it with a recorded one through a service override.
            [FromServices] IChatClient chatClient,
            [FromServices] IOptions<JsonOptions> jsonOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var jsonSerializerOptions = jsonOptions.Value.SerializerOptions;

            var streamOptions = configureStreamOptions?.Invoke(jsonSerializerOptions)
                ?? new AGUIStreamOptions();

            var ctx = input.ToChatRequestContext(jsonSerializerOptions, streamOptions);

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
