// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class MessageAttachmentContent : ComponentBase
{
    private DataContent? _currentContent;
    private RenderFragment? _content;

    [Parameter, EditorRequired]
    public DataContent Content { get; set; } = default!;

    [Parameter]
    public string? AlternativeText { get; set; }

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Content);

        if (!ReferenceEquals(_currentContent, Content))
        {
            _currentContent = Content;
            _content = MediaContentFactory.Create(Content, AlternativeText);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(
            0,
            _content
                ?? throw new InvalidOperationException(
                    $"{nameof(MessageAttachmentContent)}.{nameof(Content)} is required."));
    }
}
