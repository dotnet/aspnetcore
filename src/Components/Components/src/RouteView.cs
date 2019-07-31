// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Displays the specified page component, rendering it inside its layout
    /// and any further nested layouts.
    /// </summary>
    public class RouteView : ComponentBase
    {
        private readonly RenderFragment _renderPageWithParametersDelegate;

        /// <summary>
        /// Gets or sets the route data. This determines the page that will be
        /// displayed and the parameter values that will be supplied to the page.
        /// </summary>
        [Parameter]
        public ComponentRouteData RouteData { get; set; }

        /// <summary>
        /// Gets or sets the type of a layout to be used if the page does not
        /// declare any layout. If specified, the type must implement <see cref="IComponent"/>
        /// and accept a parameter named <see cref="LayoutComponentBase.Body"/>.
        /// </summary>
        [Parameter]
        public Type DefaultLayout { get; set; }

        public RouteView()
        {
            _renderPageWithParametersDelegate = RenderPageWithParameters;
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (RouteData == null)
            {
                throw new InvalidOperationException($"The {nameof(RouteView)} component requires a non-null value for the parameter {nameof(RouteData)}.");
            }

            var pageLayoutType = RouteData.PageComponentType.GetCustomAttribute<LayoutAttribute>()?.LayoutType
                ?? DefaultLayout;

            builder.OpenComponent<LayoutView>(0);
            builder.AddAttribute(1, nameof(LayoutView.Layout), pageLayoutType);
            builder.AddAttribute(2, nameof(LayoutView.ChildContent), _renderPageWithParametersDelegate);
            builder.CloseComponent();
        }

        private void RenderPageWithParameters(RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, RouteData.PageComponentType);

            foreach (var kvp in RouteData.PageParameters)
            {
                builder.AddAttribute(1, kvp.Key, kvp.Value);
            }

            builder.CloseComponent();
        }
    }
}
