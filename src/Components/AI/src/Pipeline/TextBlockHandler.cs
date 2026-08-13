// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

// Fallback handler: claims TextContent and accumulates it into a single block for the
// duration of a model message. It runs last so more specific handlers get first refusal.
internal sealed class TextBlockHandler : ContentBlockHandler<RichContentBlock>
{
    public override BlockMappingResult<RichContentBlock> Handle(
        BlockMappingContext context, RichContentBlock state)
    {
        TextContent? textContent = null;
        foreach (var content in context.UnhandledContents)
        {
            if (content is TextContent tc)
            {
                textContent = tc;
                break;
            }
        }

        if (textContent is null)
        {
            if (state.Id != string.Empty)
            {
                return BlockMappingResult<RichContentBlock>.Complete();
            }

            return BlockMappingResult<RichContentBlock>.Pass();
        }

        context.MarkHandled(textContent);
        state.AppendText(textContent.Text ?? string.Empty);
        RebuildParagraphs(state);

        if (state.Id == string.Empty)
        {
            state.Id = context.Update.MessageId ?? Guid.NewGuid().ToString("N");
            return BlockMappingResult<RichContentBlock>.Emit(state, state);
        }

        return BlockMappingResult<RichContentBlock>.Update(state);
    }

    internal static void RebuildParagraphs(RichContentBlock state)
    {
        var rawText = state.RawText;
        if (rawText.Length == 0)
        {
            state.Paragraphs = Array.Empty<string>();
            return;
        }

        var paragraphs = new List<string>();
        var start = 0;
        while (start < rawText.Length)
        {
            var breakIndex = rawText.IndexOf("\n\n", start, StringComparison.Ordinal);
            if (breakIndex < 0)
            {
                AddParagraph(paragraphs, rawText.AsSpan(start));
                break;
            }

            if (breakIndex > start)
            {
                AddParagraph(paragraphs, rawText.AsSpan(start, breakIndex - start));
            }

            start = breakIndex + 2;
        }

        state.Paragraphs = paragraphs;
    }

    private static void AddParagraph(List<string> paragraphs, ReadOnlySpan<char> text)
    {
        var trimmed = text.TrimEnd("\r\n".AsSpan());
        if (trimmed.Length == 0)
        {
            return;
        }

        paragraphs.Add(trimmed.ToString());
    }
}
