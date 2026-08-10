// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class RichContentBlock : ContentBlock
{
    private readonly List<string> _segments = new();
    private string? _cachedText;
    public string RawText => _cachedText ??= string.Concat(_segments);

    public IReadOnlyList<RichTextNode> Content { get; internal set; } =
        Array.Empty<RichTextNode>();

    public void AppendText(string text)
    {
        _segments.Add(text);
        _cachedText = null;
    }
}
