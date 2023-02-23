// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Authorization;

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
    // We expect applications to supply their own authorizing/not-authorized content, but
    // it's better to have defaults than to make the parameters mandatory because in some
    // cases they will never be used (e.g., "authorizing" in out-of-box server-side Blazor)
    private static readonly RenderFragment<AuthenticationState> _defaultNotAuthorizedContent
        = state => builder => builder.AddContent(0, "Not authorized");
    private static readonly RenderFragment _defaultAuthorizingContent
        = builder => builder.AddContent(0, "Authorizing...");

    private readonly RenderFragment _renderAuthorizeRouteViewCoreDelegate;
    private readonly RenderFragment<AuthenticationState> _renderAuthorizedDelegate;
    private readonly RenderFragment<AuthenticationState> _renderNotAuthorizedDelegate;
    private readonly RenderFragment _renderAuthorizingDelegate;

    /// <summary>
    /// Initialize a new instance of a <see cref="AuthorizeRouteView"/>.
    /// </summary>
    public AuthorizeRouteView()
    {
        // Cache the rendering delegates so that we only construct new closure instances
        // when they are actually used (e.g., we never prepare a RenderFragment bound to
        // the NotAuthorized content except when you are displaying that particular state)
        RenderFragment renderBaseRouteViewDelegate = base.Render;
        _renderAuthorizedDelegate = authenticateState => renderBaseRouteViewDelegate;
        _renderNotAuthorizedDelegate = authenticationState => builder => RenderNotAuthorizedInDefaultLayout(builder, authenticationState);
        _renderAuthorizingDelegate = RenderAuthorizingInDefaultLayout;
        _renderAuthorizeRouteViewCoreDelegate = RenderAuthorizeRouteViewCore;
    }

    /// <summary>
    /// The content that will be displayed if the user is not authorized.
    /// </summary>
    [Parameter]
    public RenderFragment<AuthenticationState>? NotAuthorized { get; set; }

    /// <summary>
    /// The content that will be displayed while asynchronous authorization is in progress.
    /// </summary>
    [Parameter]
    public RenderFragment? Authorizing { get; set; }

    /// <summary>
    /// The resource to which access is being controlled.
    /// </summary>
    [Parameter]
    public object? Resource { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? ExistingCascadedAuthenticationState { get; set; }

    /// <inheritdoc />
    protected override void Render(RenderTreeBuilder builder)
    {
        if (ExistingCascadedAuthenticationState != null)
        {
            // If this component is already wrapped in a <CascadingAuthenticationState> (or another
            // compatible provider), then don't interfere with the cascaded authentication state.
            _renderAuthorizeRouteViewCoreDelegate(builder);
        }
        else
        {
            // Otherwise, implicitly wrap the output in a <CascadingAuthenticationState>
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddComponentParameter(1, nameof(CascadingAuthenticationState.ChildContent), _renderAuthorizeRouteViewCoreDelegate);
            builder.CloseComponent();
        }
    }

    private void RenderAuthorizeRouteViewCore(RenderTreeBuilder builder)
    {
        builder.OpenComponent<AuthorizeRouteViewCore>(0);
        builder.AddComponentParameter(1, nameof(AuthorizeRouteViewCore.RouteData), RouteData);
        builder.AddComponentParameter(2, nameof(AuthorizeRouteViewCore.Authorized), _renderAuthorizedDelegate);
        builder.AddComponentParameter(3, nameof(AuthorizeRouteViewCore.Authorizing), _renderAuthorizingDelegate);
        builder.AddComponentParameter(4, nameof(AuthorizeRouteViewCore.NotAuthorized), _renderNotAuthorizedDelegate);
        builder.AddComponentParameter(5, nameof(AuthorizeRouteViewCore.Resource), Resource);
        builder.CloseComponent();
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:RequiresUnreferencedCode",
        Justification = "OpenComponent already has the right set of attributes")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2110:RequiresUnreferencedCode",
        Justification = "OpenComponent already has the right set of attributes")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2118:RequiresUnreferencedCode",
        Justification = "OpenComponent already has the right set of attributes")]
    private void RenderContentInDefaultLayout(RenderTreeBuilder builder, RenderFragment content)
    {
        builder.OpenComponent<LayoutView>(0);
        builder.AddComponentParameter(1, nameof(LayoutView.Layout), DefaultLayout);
        builder.AddComponentParameter(2, nameof(LayoutView.ChildContent), content);
        builder.CloseComponent();
    }

    private void RenderNotAuthorizedInDefaultLayout(RenderTreeBuilder builder, AuthenticationState authenticationState)
    {
        var content = NotAuthorized ?? _defaultNotAuthorizedContent;
        RenderContentInDefaultLayout(builder, content(authenticationState));
    }

    private void RenderAuthorizingInDefaultLayout(RenderTreeBuilder builder)
    {
        var content = Authorizing ?? _defaultAuthorizingContent;
        RenderContentInDefaultLayout(builder, content);
    }

    private sealed class AuthorizeRouteViewCore : AuthorizeViewCore
    {
        [Parameter]
        public RouteData RouteData { get; set; } = default!;

        protected override IAuthorizeData[]? GetAuthorizeData()
            => AttributeAuthorizeDataCache.GetAuthorizeDataForType(RouteData.PageType);
    }
}
