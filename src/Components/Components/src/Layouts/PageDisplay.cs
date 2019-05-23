// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Auth;
using Microsoft.AspNetCore.Components.Internal;
using Microsoft.AspNetCore.Components.Layouts;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Displays the specified page component, rendering it inside its layout
    /// and any further nested layouts, plus applying any authorization rules.
    /// </summary>
    public class PageDisplay : IComponent
    {
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

        /// <summary>
        /// The content that will be displayed if the user is not authorized.
        /// </summary>
        [Parameter]
        public RenderFragment<AuthenticationState> NotAuthorizedContent { get; private set; }

        /// <summary>
        /// The content that will be displayed while asynchronous authorization is in progress.
        /// </summary>
        [Parameter]
        public RenderFragment AuthorizingContent { get; private set; }

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
            // In the middle goes the requested page
            var fragment = (RenderFragment)RenderPageWithParameters;

            // Around that goes an AuthorizeViewCore
            fragment = WrapInAuthorizeViewCore(fragment);

            // Then repeatedly wrap that in each layer of nested layout until we get
            // to a layout that has no parent
            Type layoutType = Page;
            while ((layoutType = GetLayoutType(layoutType)) != null)
            {
                fragment = WrapInLayout(layoutType, fragment);
            }

            _renderHandle.Render(fragment);
        }

        private RenderFragment WrapInLayout(Type layoutType, RenderFragment bodyParam) => builder =>
        {
            builder.OpenComponent(0, layoutType);
            builder.AddAttribute(1, LayoutComponentBase.BodyPropertyName, bodyParam);
            builder.CloseComponent();
        };

        private void RenderPageWithParameters(RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, Page);

            if (PageParameters != null)
            {
                foreach (var kvp in PageParameters)
                {
                    builder.AddAttribute(1, kvp.Key, kvp.Value);
                }
            }

            builder.CloseComponent();
        }

        private RenderFragment WrapInAuthorizeViewCore(RenderFragment pageFragment)
        {
            var authorizeData = AttributeAuthorizeDataCache.GetAuthorizeDataForType(Page);
            if (authorizeData == null)
            {
                // No authorization, so no need to wrap the fragment
                return pageFragment;
            }

            // Some authorization data exists, so we do need to wrap the fragment
            RenderFragment<AuthenticationState> authorizedContent = context => pageFragment;
            return builder =>
            {
                builder.OpenComponent<AuthorizeViewWithSuppliedData>(0);
                builder.AddAttribute(1, nameof(AuthorizeViewWithSuppliedData.AuthorizeDataParam), authorizeData);
                builder.AddAttribute(2, nameof(AuthorizeViewWithSuppliedData.Authorized), authorizedContent);
                builder.AddAttribute(3, nameof(AuthorizeViewWithSuppliedData.NotAuthorized), NotAuthorizedContent ?? DefaultNotAuthorizedContent);
                builder.AddAttribute(4, nameof(AuthorizeViewWithSuppliedData.Authorizing), AuthorizingContent);
                builder.CloseComponent();
            };
        }

        private static Type GetLayoutType(Type type)
            => type.GetCustomAttribute<LayoutAttribute>()?.LayoutType;

        private class AuthorizeViewWithSuppliedData : AuthorizeViewCore
        {
            [Parameter] public IAuthorizeData[] AuthorizeDataParam { get; private set; }

            protected override IAuthorizeData[] AuthorizeData => AuthorizeDataParam;
        }

        // There has to be some default content. If we render blank by default, developers
        // will find it hard to guess why their UI isn't appearing.
        private static RenderFragment DefaultNotAuthorizedContent(AuthenticationState authenticationState)
            => builder => builder.AddContent(0, "Not authorized");
    }
}
