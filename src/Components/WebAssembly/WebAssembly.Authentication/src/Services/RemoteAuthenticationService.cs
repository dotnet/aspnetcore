// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// The default implementation for <see cref="IRemoteAuthenticationService{TRemoteAuthenticationState}"/> that uses JS interop to authenticate the user.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The state to preserve across authentication operations.</typeparam>
    /// <typeparam name="TProviderOptions">The options to be passed down to the underlying JavaScript library handling the authentication operations.</typeparam>
    public class RemoteAuthenticationService<TRemoteAuthenticationState, TProviderOptions> :
        AuthenticationStateProvider,
        IRemoteAuthenticationService<TRemoteAuthenticationState>,
        IAccessTokenProvider
         where TRemoteAuthenticationState : RemoteAuthenticationState
         where TProviderOptions : new()
    {
        private static readonly TimeSpan _userCacheRefreshInterval = TimeSpan.FromSeconds(60);

        private bool _initialized = false;

        // This defaults to 1/1/1970
        private DateTimeOffset _userLastCheck = DateTimeOffset.FromUnixTimeSeconds(0);
        private ClaimsPrincipal _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());

        /// <summary>
        /// The <see cref="IJSRuntime"/> to use for performing JavaScript interop operations.
        /// </summary>
        protected readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// The <see cref="NavigationManager"/> used to compute absolute urls.
        /// </summary>
        protected readonly NavigationManager _navigation;

        /// <summary>
        /// The options for the underlying JavaScript library handling the authentication operations.
        /// </summary>
        protected readonly RemoteAuthenticationOptions<TProviderOptions> _options;

        /// <summary>
        /// Initializes a new instance of <see cref="RemoteAuthenticationService{TRemoteAuthenticationState, TProviderOptions}"/>.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/> to use for performing JavaScript interop operations.</param>
        /// <param name="options">The options to be passed down to the underlying JavaScript library handling the authentication operations.</param>
        public RemoteAuthenticationService(
            IJSRuntime jsRuntime,
            IOptions<RemoteAuthenticationOptions<TProviderOptions>> options,
            NavigationManager navigation)
        {
            _jsRuntime = jsRuntime;
            _navigation = navigation;
            _options = options.Value;
        }

        /// <inheritdoc />
        public override async Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await GetUser(useCache: true));

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var result = await _jsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signIn", context.State);
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                UpdateUser(GetUser());
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var result = await _jsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignIn", context.Url);
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                UpdateUser(GetUser());
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var result = await _jsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signOut", context.State);
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                UpdateUser(GetUser());
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var result = await _jsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignOut", context.Url);
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                UpdateUser(GetUser());
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async ValueTask<AccessTokenResult> RequestAccessToken()
        {
            await EnsureAuthService();
            var result = await _jsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken");

            if (string.Equals(result.Status, AccessTokenResultStatus.RequiresRedirect, StringComparison.OrdinalIgnoreCase))
            {
                var redirectUrl = GetRedirectUrl(null);
                result.RedirectUrl = redirectUrl.ToString();
            }

            return new AccessTokenResult(result.Status, result.Token, result.RedirectUrl);
        }

        /// <inheritdoc />
        public virtual async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            await EnsureAuthService();
            var result = await _jsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken", options);

            if (string.Equals(result.Status, AccessTokenResultStatus.RequiresRedirect, StringComparison.OrdinalIgnoreCase))
            {
                var redirectUrl = GetRedirectUrl(options.ReturnUrl);
                result.RedirectUrl = redirectUrl.ToString();
            }

            return new AccessTokenResult(result.Status, result.Token, result.RedirectUrl);
        }

        private Uri GetRedirectUrl(string customReturnUrl)
        {
            var returnUrl = customReturnUrl != null ? _navigation.ToAbsoluteUri(customReturnUrl).ToString() : null;
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl ?? _navigation.Uri);
            var redirectUrl = _navigation.ToAbsoluteUri($"{_options.AuthenticationPaths.LogInPath}?returnUrl={encodedReturnUrl}");
            return redirectUrl;
        }

        private async ValueTask<ClaimsPrincipal> GetUser(bool useCache = false)
        {
            var now = DateTimeOffset.Now;
            if (useCache && now < _userLastCheck + _userCacheRefreshInterval)
            {
                return _cachedUser;
            }

            _cachedUser = await GetAuthenticatedUser();
            _userLastCheck = now;

            return _cachedUser;
        }

        /// <summary>
        /// Gets the current authenticated used using JavaScript interop.
        /// </summary>
        /// <returns>A <see cref="Task{ClaimsPrincipal}"/>that will return the current authenticated user when completes.</returns>
        protected internal virtual async Task<ClaimsPrincipal> GetAuthenticatedUser()
        {
            await EnsureAuthService();
            var user = await _jsRuntime.InvokeAsync<IDictionary<string, object>>("AuthenticationService.getUser");

            var identity = user != null ? new ClaimsIdentity(
                _options.UserOptions.AuthenticationType,
                _options.UserOptions.NameClaim,
                _options.UserOptions.RoleClaim) : new ClaimsIdentity();

            if (user != null)
            {
                foreach (var kvp in user)
                {
                    var name = kvp.Key;
                    var value = kvp.Value;
                    if (value != null)
                    {
                        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in element.EnumerateArray())
                            {
                                identity.AddClaim(new Claim(name, JsonSerializer.Deserialize<object>(item.GetRawText()).ToString()));
                            }
                        }
                        else
                        {
                            identity.AddClaim(new Claim(name, value.ToString()));
                        }
                    }
                }
            }

            return new ClaimsPrincipal(identity);
        }

        private async ValueTask EnsureAuthService()
        {
            if (!_initialized)
            {
                await _jsRuntime.InvokeVoidAsync("AuthenticationService.init", _options.ProviderOptions);
                _initialized = true;
            }
        }

        private void UpdateUser(ValueTask<ClaimsPrincipal> task)
        {
            NotifyAuthenticationStateChanged(UpdateAuthenticationState(task));

            static async Task<AuthenticationState> UpdateAuthenticationState(ValueTask<ClaimsPrincipal> futureUser) => new AuthenticationState(await futureUser);
        }
    }

    // Internal for testing purposes
    internal struct InternalAccessTokenResult
    {
        public string Status { get; set; }
        public AccessToken Token { get; set; }
        public string RedirectUrl { get; set; }
    }
}
