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
        private readonly RenderFragment<AuthenticationState> _renderAuthorizedDelegate;
        private readonly RenderFragment<AuthenticationState> _renderNotAuthorizedDelegate;
        private readonly RenderFragment _renderAuthorizingDelegate;

        public AuthorizeRouteView()
        {
            // Cache the rendering delegates so that we only construct new closure instances
            // when they are actually used (e.g., we never prepare a RenderFragment bound to
            // the Authorizing content except when you are displaying that particular state)
            var renderBaseRouteViewDelegate = (RenderFragment)base.Render;
            _renderAuthorizedDelegate = authenticateState => renderBaseRouteViewDelegate;
            _renderNotAuthorizedDelegate = authenticationState => RenderContentInDefaultLayout(NotAuthorized(authenticationState));
            _renderAuthorizingDelegate = builder => RenderContentInDefaultLayout(Authorizing);
        }

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
                builder.OpenComponent<AuthorizeRouteViewCore>(0);
                builder.AddAttribute(1, nameof(AuthorizeRouteViewCore.RouteData), RouteData);
                builder.AddAttribute(2, nameof(AuthorizeRouteViewCore.Authorized), _renderAuthorizedDelegate);
                builder.AddAttribute(3, nameof(AuthorizeRouteViewCore.Authorizing), _renderAuthorizingDelegate);
                builder.AddAttribute(4, nameof(AuthorizeRouteViewCore.NotAuthorized), _renderNotAuthorizedDelegate);
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
