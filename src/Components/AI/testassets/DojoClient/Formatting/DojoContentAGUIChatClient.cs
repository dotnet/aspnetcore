// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using AGUI.Abstractions;
using DojoAgent;
using Microsoft.Extensions.AI;

namespace DojoClient.Formatting;

internal sealed class DojoContentAGUIChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(
            messages, options, cancellationToken).ConfigureAwait(false))
        {
            if (update.RawRepresentation is CustomEvent { Name: DojoContentTransport.EventName } customEvent)
            {
                yield return DojoContentTransport.Decode(customEvent);
            }
            else
            {
                yield return update;
            }
        }
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);
}
