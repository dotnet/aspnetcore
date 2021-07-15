// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Sections
{
    /// <summary>
    /// Renders content provided by <see cref="HeadContent"/> components.
    /// </summary>
    public sealed class HeadSection : ComponentBase
    {
        private const string GetAndRemoveExistingTitle = "Blazor._internal.PageTitle.getAndRemoveExistingTitle";

        internal const string HeadSectionOutletName = "head";
        internal const string TitleSectionOutletName = "title";

        private string? _defaultTitle;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// The content to be rendered when no <see cref="HeadContent"/> instances are providing content.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

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

            if (!string.IsNullOrEmpty(_defaultTitle))
            {
                builder.AddAttribute(2, nameof(SectionOutlet.ChildContent), (RenderFragment)BuildDefaultTitleRenderTree);
            }

            builder.CloseComponent();

            // Render the rest of the head metadata
            builder.OpenComponent<SectionOutlet>(3);
            builder.AddAttribute(4, nameof(SectionOutlet.Name), HeadSectionOutletName);
            builder.AddAttribute(5, nameof(SectionOutlet.ChildContent), ChildContent);
            builder.CloseComponent();
        }

        private void BuildDefaultTitleRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "title");
            builder.AddContent(1, _defaultTitle);
            builder.CloseElement();
        }
    }
}
