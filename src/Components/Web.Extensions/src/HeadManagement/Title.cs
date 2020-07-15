// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that changes the title of the document.
    /// </summary>
    public sealed class Title : ComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Gets or sets the value to use as the document's title.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JSRuntime.SetTitleAsync(Value);
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.BuildHeadElementComment(0, new TitleElement(Value));
        }
    }
}
