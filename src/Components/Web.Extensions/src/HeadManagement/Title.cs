// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that changes the title of the document.
    /// </summary>
    public class Title : ComponentBase
    {
        private HeadManager _headManager = default!;

        [Inject]
        private IServiceProvider ServiceProvider { get; set; } = default!;

        /// <summary>
        /// Gets or sets the value to use as the document's title.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            _headManager = ServiceProvider.GetHeadManager() ??
                throw new InvalidOperationException($"{GetType()} requires a {typeof(HeadManager)} service.");
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await _headManager.SetTitleAsync(Value);
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_headManager.IsPrerendering)
            {
                _headManager.BuildHeadElementComment(builder, new TitleElement(Value));
            }
        }
    }
}
