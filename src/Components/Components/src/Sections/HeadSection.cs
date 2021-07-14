// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Sections
{
    /// <summary>
    /// Renders content provided by <see cref="HeadContent"/> components.
    /// </summary>
    public class HeadSection : ComponentBase
    {
        internal const string SectionOutletName = "head";

        /// <summary>
        /// The content to be rendered when no <see cref="HeadContent"/> instances are providing content.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SectionOutlet>(0);
            builder.AddAttribute(1, nameof(SectionOutlet.Name), SectionOutletName);
            builder.AddAttribute(2, nameof(SectionOutlet.ChildContent), ChildContent);
            builder.CloseComponent();
        }
    }
}
