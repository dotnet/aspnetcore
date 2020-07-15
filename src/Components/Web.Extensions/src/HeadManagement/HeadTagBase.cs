// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Serves as a base for components that represent tags in the HTML head.
    /// </summary>
    public abstract class HeadTagBase : ComponentBase, IDisposable
    {
        private readonly string _id = Guid.NewGuid().ToString("N");

        private TagElement _tagElement;

        private HeadManager _headManager = default!;

        [Inject]
        private IServiceProvider ServiceProvider { get; set; } = default!;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the meta element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? Attributes { get; set; }

        /// <summary>
        /// Gets the name of the tag being represented.
        /// </summary>
        protected abstract string TagName { get; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            _headManager = ServiceProvider.GetHeadManager() ??
                throw new InvalidOperationException($"{GetType()} requires a {typeof(HeadManager)} service.");
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            _tagElement = new TagElement(TagName, Attributes);
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await _headManager.ApplyTagAsync(_tagElement, _id);
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_headManager.IsPrerendering)
            {
                _headManager.BuildHeadElementComment(builder, _tagElement);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_headManager.IsPrerendering)
            {
                _ = _headManager.RemoveTagAsync(_id);
            }
        }
    }
}
