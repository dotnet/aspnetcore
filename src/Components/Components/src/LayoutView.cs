// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Displays the specified content inside the specified layout and any further
    /// nested layouts.
    /// </summary>
    public class LayoutView : IComponent
    {
        private static readonly RenderFragment EmptyRenderFragment = builder => { };

        private RenderHandle _renderHandle;

        /// <summary>
        /// Gets or sets the content to display.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the type of the layout in which to display the content.
        /// The type must implement <see cref="IComponent"/> and accept a parameter named <see cref="LayoutComponentBase.Body"/>.
        /// </summary>
        [Parameter]
        public Type Layout { get; set; }

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            Render();
            return Task.CompletedTask;
        }

        private void Render()
        {
            // In the middle goes the supplied content
            var fragment = ChildContent ?? EmptyRenderFragment;

            // Then repeatedly wrap that in each layer of nested layout until we get
            // to a layout that has no parent
            var layoutType = Layout;
            while (layoutType != null)
            {
                fragment = WrapInLayout(layoutType, fragment);
                layoutType = GetParentLayoutType(layoutType);
            }

            _renderHandle.Render(fragment);
        }

        private static RenderFragment WrapInLayout(Type layoutType, RenderFragment bodyParam)
        {
            return builder =>
            {
                builder.OpenComponent(0, layoutType);
                builder.AddAttribute(1, LayoutComponentBase.BodyPropertyName, bodyParam);
                builder.CloseComponent();
            };
        }

        private static Type GetParentLayoutType(Type type)
            => type.GetCustomAttribute<LayoutAttribute>()?.LayoutType;
    }
}
