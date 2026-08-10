// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace DojoClient.Formatting;

internal sealed class FormattedChatClient : DelegatingChatClient
{
    internal FormattedChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder();

        await foreach (var update in base.GetStreamingResponseAsync(
            messages,
            options,
            cancellationToken).ConfigureAwait(false))
        {
            var firstTextIndex = -1;
            var chunks = new List<string>();
            for (var i = 0; i < update.Contents.Count; i++)
            {
                if (update.Contents[i] is not TextContent textContent)
                {
                    continue;
                }

                if (firstTextIndex < 0)
                {
                    firstTextIndex = i;
                }
                chunks.Add(textContent.Text ?? string.Empty);
            }

            for (var i = update.Contents.Count - 1; i >= 0; i--)
            {
                if (update.Contents[i] is TextContent)
                {
                    update.Contents.RemoveAt(i);
                }
            }

            if (firstTextIndex >= 0)
            {
                foreach (var chunk in chunks)
                {
                    text.Append(chunk);
                }
                var snapshot = text.ToString();
                update.Contents.Insert(
                    firstTextIndex,
                    new RichTextContent(snapshot, MarkdownRichTextParser.Parse(snapshot)));
            }

            yield return update;
        }
    }
}
