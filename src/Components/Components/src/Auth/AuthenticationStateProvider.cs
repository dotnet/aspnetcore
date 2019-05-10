// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Obtains the <see cref="IAuthenticationState"/> from the registered <see cref="IAuthenticationStateProvider"/>,
    /// and supplies it to descendants as a cascading value whose value can change.
    /// </summary>
    public class AuthenticationStateProvider : ComponentBase
    {
        private IAuthenticationState _currentAuthenticationState;

        [Inject] private IAuthenticationStateProvider AuthStateProviderService { get; set; }

        /// <summary>
        /// The content to which the authentication state should be provided.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            AuthStateProviderService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        /// <inheritdoc />
        protected override async Task OnInitAsync()
        {
            AuthStateProviderService.AuthenticationStateChanged += OnAuthenticationStateChanged;

            // Initial synchronous render has an unauthorized authentication state
            // We create a new one because we can't stop ClaimsPrincipal from being mutable
            _currentAuthenticationState = new PendingAuthenticationState();

            // Then asynchronously we query for the actual authentication state and rerender
            // If this happens to return synchronously, the 'empty' render will be skipped
            _currentAuthenticationState = await AuthStateProviderService
                .GetAuthenticationStateAsync(forceRefresh: false);
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<IAuthenticationState>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<IAuthenticationState>.Value), _currentAuthenticationState);
            builder.AddAttribute(2, RenderTreeBuilder.ChildContent, ChildContent);
            builder.CloseComponent();
        }

        private void OnAuthenticationStateChanged(IAuthenticationState newAuthenticationState)
        {
            Invoke(() =>
            {
                _currentAuthenticationState = newAuthenticationState;
                StateHasChanged();
            });
        }
    }
}
