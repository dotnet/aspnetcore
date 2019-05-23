// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Layouts;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Displays the specified page component, rendering it inside its layout
    /// and any further nested layouts, plus applying any authorization rules.
    /// </summary>
    public class PageDisplay : IComponent
    {
        internal const string NameOfPage = nameof(Page);
        internal const string NameOfPageParameters = nameof(PageParameters);

        private RenderHandle _renderHandle;

        /// <summary>
        /// Gets or sets the type of the page component to display.
        /// The type must implement <see cref="IComponent"/>.
        /// </summary>
        [Parameter]
        public Type Page { get; private set; }

        /// <summary>
        /// Gets or sets the parameters to pass to the page.
        /// </summary>
        [Parameter]
        public IDictionary<string, object> PageParameters { get; private set; }

        /// <inheritdoc />
        public void Configure(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            Render();
            return Task.CompletedTask;
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
