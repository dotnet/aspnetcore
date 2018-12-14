// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Layouts
{
    /// <summary>
    /// Displays the specified page component, rendering it inside its layout
    /// and any further nested layouts.
    /// </summary>
    public class LayoutDisplay : IComponent
    {
        internal const string NameOfPage = nameof(Page);
        internal const string NameOfPageParameters = nameof(PageParameters);

        private RenderHandle _renderHandle;

        /// <summary>
        /// Gets or sets the type of the page component to display.
        /// The type must implement <see cref="IComponent"/>.
        /// </summary>
        [Parameter]
        Type Page { get; set; }

        /// <summary>
        /// Gets or sets the parameters to pass to the page.
        /// </summary>
        [Parameter]
        IDictionary<string, object> PageParameters { get; set; }

        /// <inheritdoc />
        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public void SetParameters(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            Render();
        }

        private void Render()
        {
            // In the middle, we render the requested page
            var fragment = RenderComponentWithBody(Page, bodyParam: null);

            // Repeatedly wrap it in each layer of nested layout until we get
            // to a layout that has no parent
            Type layoutType = Page;
            while ((layoutType = GetLayoutType(layoutType)) != null)
            {
                fragment = RenderComponentWithBody(layoutType, fragment);
            }

            _renderHandle.Render(fragment);
        }

        private RenderFragment RenderComponentWithBody(Type componentType, RenderFragment bodyParam) => builder =>
        {
            builder.OpenComponent(0, componentType);
            if (bodyParam != null)
            {
                builder.AddAttribute(1, LayoutComponentBase.BodyPropertyName, bodyParam);
            }
            else
            {
                if (PageParameters != null)
                {
                    foreach (var kvp in PageParameters)
                    {
                        builder.AddAttribute(1, kvp.Key, kvp.Value);
                    }
                }
            }
            builder.CloseComponent();
        };

        private Type GetLayoutType(Type type)
            => type.GetCustomAttribute<LayoutAttribute>()?.LayoutType;
    }
}
