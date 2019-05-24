// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A base class for components that display differing content depending on the user's authorization status.
    /// </summary>
    public abstract class AuthorizeViewCore : ComponentBase
    {
        private AuthenticationState currentAuthenticationState;
        private bool isAuthorized;

        /// <summary>
        /// The content that will be displayed if the user is authorized.
        /// </summary>
        [Parameter] public RenderFragment<AuthenticationState> ChildContent { get; private set; }

        /// <summary>
        /// The content that will be displayed if the user is not authorized.
        /// </summary>
        [Parameter] public RenderFragment<AuthenticationState> NotAuthorized { get; private set; }

        /// <summary>
        /// The content that will be displayed if the user is authorized.
        /// If you specify a value for this parameter, do not also specify a value for <see cref="ChildContent"/>.
        /// </summary>
        [Parameter] public RenderFragment<AuthenticationState> Authorized { get; private set; }

        /// <summary>
        /// The content that will be displayed while asynchronous authorization is in progress.
        /// </summary>
        [Parameter] public RenderFragment Authorizing { get; private set; }

        /// <summary>
        /// The resource to which access is being controlled.
        /// </summary>
        [Parameter] public object Resource { get; private set; }

        [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; }

        [Inject] private IAuthorizationPolicyProvider AuthorizationPolicyProvider { get; set; }

        [Inject] private IAuthorizationService AuthorizationService { get; set; }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (currentAuthenticationState == null)
            {
                builder.AddContent(0, Authorizing);
            }
            else if (isAuthorized)
            {
                var authorizedContent = Authorized ?? ChildContent;
                builder.AddContent(1, authorizedContent?.Invoke(currentAuthenticationState));
            }
            else
            {
                builder.AddContent(2, NotAuthorized?.Invoke(currentAuthenticationState));
            }
        }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            // We allow 'ChildContent' for convenience in basic cases, and 'Authorized' for symmetry
            // with 'NotAuthorized' in other cases. Besides naming, they are equivalent. To avoid
            // confusion, explicitly prevent the case where both are supplied.
            if (ChildContent != null && Authorized != null)
            {
                throw new InvalidOperationException($"Do not specify both '{nameof(Authorized)}' and '{nameof(ChildContent)}'.");
            }

            if (AuthenticationState == null)
            {
                throw new InvalidOperationException($"Authorization requires a cascading parameter of type Task<{nameof(AuthenticationState)}>. Consider using {typeof(CascadingAuthenticationState).Name} to supply this.");
            }

            // First render in pending state
            // If the task has already completed, this render will be skipped
            currentAuthenticationState = null;

            // Then render in completed state
            // Importantly, we *don't* call StateHasChanged between the following async steps,
            // otherwise we'd display an incorrect UI state while waiting for IsAuthorizedAsync
            currentAuthenticationState = await AuthenticationState;
            isAuthorized = await IsAuthorizedAsync(currentAuthenticationState.User);
        }

        /// <summary>
        /// Gets the data required to apply authorization rules.
        /// </summary>
        protected abstract IAuthorizeData[] GetAuthorizeData();

        private async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user)
        {
            var authorizeData = GetAuthorizeData();
            var policy = await AuthorizationPolicy.CombineAsync(
                AuthorizationPolicyProvider, authorizeData);
            var result = await AuthorizationService.AuthorizeAsync(user, Resource, policy);
            return result.Succeeded;
        }
    }
}
