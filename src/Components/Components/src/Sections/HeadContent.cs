// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Sections
{
    /// <summary>
    /// Provides content to <see cref="HeadSection"/> components.
    /// </summary>
    public class HeadContent : ComponentBase
    {
        /// <summary>
        /// Gets or sets the content to be rendered in <see cref="HeadSection"/> instances.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SectionContent>(0);
            builder.AddAttribute(1, nameof(SectionContent.Name), HeadSection.SectionOutletName);
            builder.AddAttribute(2, nameof(SectionContent.ChildContent), ChildContent);
            builder.CloseComponent();
        }
    }
}
