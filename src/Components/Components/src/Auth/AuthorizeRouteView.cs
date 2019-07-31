// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Auth;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Combines the behaviors of <see cref="AuthorizeView"/> and <see cref="RouteView"/>,
    /// so that it displays the page matching the specified route but only if the user
    /// is authorized to see it.
    ///
    /// Additionally, this component supplies a cascading parameter of type <see cref="Task{AuthenticationState}"/>,
    /// which makes the user's current authentication state available to descendants.
    /// </summary>
    public sealed class AuthorizeRouteView : RouteView
    {
        /// <summary>
        /// The content that will be displayed if the user is not authorized.
        /// </summary>
        [Parameter] public RenderFragment<AuthenticationState> NotAuthorized { get; set; }

        /// <summary>
        /// The content that will be displayed while asynchronous authorization is in progress.
        /// </summary>
        [Parameter] public RenderFragment Authorizing { get; set; }

        /// <inheritdoc />
        protected override void Render(RenderTreeBuilder builder)
        {
            // TODO: Consider merging the behavior of CascadingAuthenticationState into this
            // component (i.e., rendering the CascadingValue directly) to avoid the extra
            // layer of component nesting and eliminate CascadingAuthenticationState as public API.
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, nameof(CascadingAuthenticationState.ChildContent), (RenderFragment)(builder =>
            {
                // TODO: Make AuthorizeRouteViewCore into a nested class here
                builder.OpenComponent<AuthorizeRouteViewCore>(0);
                builder.AddAttribute(1, nameof(AuthorizeRouteViewCore.RouteData), RouteData);

                // TODO: Cache the delegate
                builder.AddAttribute(2, nameof(AuthorizeRouteViewCore.Authorized), (RenderFragment<AuthenticationState>)(state => base.Render));

                // TODO: Cache the delegate
                builder.AddAttribute(3, nameof(AuthorizeRouteViewCore.Authorizing), RenderContentInDefaultLayout(Authorizing));

                // TODO: Cache the delegate
                builder.AddAttribute(4, nameof(AuthorizeRouteViewCore.NotAuthorized), (RenderFragment<AuthenticationState>)(state => RenderContentInDefaultLayout(NotAuthorized(state))));

                builder.CloseComponent();
            }));
            builder.CloseComponent();
        }

        private RenderFragment RenderContentInDefaultLayout(RenderFragment content)
        {
            if (DefaultLayout != null)
            {
                return builder =>
                {
                    builder.OpenComponent<LayoutView>(0);
                    builder.AddAttribute(1, nameof(LayoutView.Layout), DefaultLayout);
                    builder.AddAttribute(2, nameof(LayoutView.ChildContent), content);
                    builder.CloseComponent();
                };
            }
            else
            {
                return content;
            }
        }

        private class AuthorizeRouteViewCore : AuthorizeViewCore
        {
            [Parameter]
            public ComponentRouteData RouteData { get; set; }

            protected override IAuthorizeData[] GetAuthorizeData()
                => AttributeAuthorizeDataCache.GetAuthorizeDataForType(RouteData.PageComponentType);
        }
    }
}
