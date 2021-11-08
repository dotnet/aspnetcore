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

    internal const string HeadSectionOutletName = "head";
    internal const string TitleSectionOutletName = "title";

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
        builder.AddAttribute(1, nameof(SectionOutlet.Name), TitleSectionOutletName);
        builder.CloseComponent();

        // Render the default title if it exists
        if (!string.IsNullOrEmpty(_defaultTitle))
        {
            builder.OpenComponent<SectionContent>(2);
            builder.AddAttribute(3, nameof(SectionContent.Name), TitleSectionOutletName);
            builder.AddAttribute(4, nameof(SectionContent.IsDefaultContent), true);
            builder.AddAttribute(5, nameof(SectionContent.ChildContent), (RenderFragment)BuildDefaultTitleRenderTree);
            builder.CloseComponent();
        }

        // Render the rest of the head metadata
        builder.OpenComponent<SectionOutlet>(6);
        builder.AddAttribute(7, nameof(SectionOutlet.Name), HeadSectionOutletName);
        builder.CloseComponent();
    }

    private void BuildDefaultTitleRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "title");
        builder.AddContent(1, _defaultTitle);
        builder.CloseElement();
    }
}
