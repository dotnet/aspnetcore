// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Renders content provided by <see cref="HeadContent"/> components.
/// </summary>
public sealed class HeadOutlet : ComponentBase
{
    private const string GetAndRemoveExistingTitle = "Blazor._internal.PageTitle.getAndRemoveExistingTitle";

    internal static readonly object HeadSectionId = new();
    internal static readonly object TitleSectionId = new();

    private string? _defaultTitle;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _defaultTitle = await JSRuntime.InvokeAsync<string>(GetAndRemoveExistingTitle);
            StateHasChanged();
        }
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Render the title content
        builder.OpenComponent<SectionOutlet>(0);
        builder.AddComponentParameter(1, nameof(SectionOutlet.SectionId), TitleSectionId);
        builder.CloseComponent();

        // Render the default title if it exists
        if (!string.IsNullOrEmpty(_defaultTitle))
        {
            builder.OpenComponent<SectionContent>(2);
            builder.AddComponentParameter(3, nameof(SectionContent.SectionId), TitleSectionId);
            builder.AddComponentParameter(4, nameof(SectionContent.IsDefaultContent), true);
            builder.AddComponentParameter(5, nameof(SectionContent.ChildContent), (RenderFragment)BuildDefaultTitleRenderTree);
            builder.CloseComponent();
        }

        // Render the rest of the head metadata
        builder.OpenComponent<SectionOutlet>(6);
        builder.AddComponentParameter(7, nameof(SectionOutlet.SectionId), HeadSectionId);
        builder.CloseComponent();
    }

    private void BuildDefaultTitleRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "title");
        builder.AddContent(1, _defaultTitle);
        builder.CloseElement();
    }
}
