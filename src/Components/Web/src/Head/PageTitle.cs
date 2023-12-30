// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Enables rendering an HTML <c>&lt;title&gt;</c> to a <see cref="HeadOutlet"/> component.
/// </summary>
public sealed class PageTitle : ComponentBase
{
    /// <summary>
    /// Gets or sets the content to be rendered as the document title.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<SectionContent>(0);
        builder.AddComponentParameter(1, nameof(SectionContent.SectionId), HeadOutlet.TitleSectionId);
        builder.AddComponentParameter(2, nameof(SectionContent.ChildContent), (RenderFragment)BuildTitleRenderTree);
        builder.CloseComponent();
    }

    private void BuildTitleRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "title");
        builder.AddContent(1, ChildContent);
        builder.CloseElement();
    }
}
