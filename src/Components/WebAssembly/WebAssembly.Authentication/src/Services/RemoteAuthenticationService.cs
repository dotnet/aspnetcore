// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// The default implementation for <see cref="IRemoteAuthenticationService{TRemoteAuthenticationState}"/> that uses JS interop to authenticate the user.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The state to preserve across authentication operations.</typeparam>
    /// <typeparam name="TAccount">The type of the <see cref="RemoteUserAccount" />.</typeparam>
    /// <typeparam name="TProviderOptions">The options to be passed down to the underlying JavaScript library handling the authentication operations.</typeparam>
    public class RemoteAuthenticationService<TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount, TProviderOptions> :
        AuthenticationStateProvider,
        IRemoteAuthenticationService<TRemoteAuthenticationState>,
        IAccessTokenProvider
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TProviderOptions : new()
        where TAccount : RemoteUserAccount
    {
        private static readonly TimeSpan _userCacheRefreshInterval = TimeSpan.FromSeconds(60);
        private bool _initialized = false;

        // This defaults to 1/1/1970
        private DateTimeOffset _userLastCheck = DateTimeOffset.FromUnixTimeSeconds(0);
        private ClaimsPrincipal _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());

        /// <summary>
        /// Gets the <see cref="IJSRuntime"/> to use for performing JavaScript interop operations.
        /// </summary>
        protected IJSRuntime JsRuntime { get; }

        /// <summary>
        /// Gets the <see cref="NavigationManager"/> used to compute absolute urls.
        /// </summary>
        protected NavigationManager Navigation { get; }

        /// <summary>
        /// Gets the <see cref="AccountClaimsPrincipalFactory{TAccount}"/> to map accounts to <see cref="ClaimsPrincipal"/>.
        /// </summary>
        protected AccountClaimsPrincipalFactory<TAccount> AccountClaimsPrincipalFactory { get; }

        /// <summary>
        /// Gets the options for the underlying JavaScript library handling the authentication operations.
        /// </summary>
        protected RemoteAuthenticationOptions<TProviderOptions> Options { get; }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/> to use for performing JavaScript interop operations.</param>
        /// <param name="options">The options to be passed down to the underlying JavaScript library handling the authentication operations.</param>
        /// <param name="navigation">The <see cref="NavigationManager"/> used to generate URLs.</param>
        /// <param name="accountClaimsPrincipalFactory">The <see cref="AccountClaimsPrincipalFactory{TAccount}"/> used to generate the <see cref="ClaimsPrincipal"/> for the user.</param>
        public RemoteAuthenticationService(
            IJSRuntime jsRuntime,
            IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>> options,
            NavigationManager navigation,
            AccountClaimsPrincipalFactory<TAccount> accountClaimsPrincipalFactory)
        {
            JsRuntime = jsRuntime;
            Navigation = navigation;
            AccountClaimsPrincipalFactory = accountClaimsPrincipalFactory;
            Options = options.Value;
        }

        /// <inheritdoc />
        public override async Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await GetUser(useCache: true));

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var internalResult = await JsRuntime.InvokeAsync<InternalRemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signIn", context.State);
            var result = internalResult.Convert();
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                var getUserTask = GetUser();
                await getUserTask;
                UpdateUser(getUserTask);
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var internalResult = await JsRuntime.InvokeAsync<InternalRemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignIn", context.Url);
            var result = internalResult.Convert();
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                var getUserTask = GetUser();
                await getUserTask;
                UpdateUser(getUserTask);
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var internalResult = await JsRuntime.InvokeAsync<InternalRemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signOut", context.State);
            var result = internalResult.Convert();
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                var getUserTask = GetUser();
                await getUserTask;
                UpdateUser(getUserTask);
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(
            RemoteAuthenticationContext<TRemoteAuthenticationState> context)
        {
            await EnsureAuthService();
            var internalResult = await JsRuntime.InvokeAsync<InternalRemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignOut", context.Url);
            var result = internalResult.Convert();
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                var getUserTask = GetUser();
                await getUserTask;
                UpdateUser(getUserTask);
            }

            return result;
        }

        /// <inheritdoc />
        public virtual async ValueTask<AccessTokenResult> RequestAccessToken()
        {
            await EnsureAuthService();
            var result = await JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken");

            if (!Enum.TryParse<AccessTokenResultStatus>(result.Status, ignoreCase: true, out var parsedStatus))
            {
                throw new InvalidOperationException($"Invalid access token result status '{result.Status ?? "(null)"}'");
            }

            if (parsedStatus == AccessTokenResultStatus.RequiresRedirect)
            {
                var redirectUrl = GetRedirectUrl(null);
                result.RedirectUrl = redirectUrl.ToString();
            }

            return new AccessTokenResult(parsedStatus, result.Token, result.RedirectUrl);
        }

        /// <inheritdoc />
        public virtual async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            await EnsureAuthService();
            var result = await JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken", options);

            if (!Enum.TryParse<AccessTokenResultStatus>(result.Status, ignoreCase: true, out var parsedStatus))
            {
                throw new InvalidOperationException($"Invalid access token result status '{result.Status ?? "(null)"}'");
            }

            if (parsedStatus == AccessTokenResultStatus.RequiresRedirect)
            {
                var redirectUrl = GetRedirectUrl(options.ReturnUrl);
                result.RedirectUrl = redirectUrl.ToString();
            }

            return new AccessTokenResult(parsedStatus, result.Token, result.RedirectUrl);
        }

        private Uri GetRedirectUrl(string customReturnUrl)
        {
            var returnUrl = customReturnUrl != null ? Navigation.ToAbsoluteUri(customReturnUrl).ToString() : null;
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl ?? Navigation.Uri);
            var redirectUrl = Navigation.ToAbsoluteUri($"{Options.AuthenticationPaths.LogInPath}?returnUrl={encodedReturnUrl}");
            return redirectUrl;
        }

        private async Task<ClaimsPrincipal> GetUser(bool useCache = false)
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
        protected internal virtual async ValueTask<ClaimsPrincipal> GetAuthenticatedUser()
        {
            await EnsureAuthService();
            var account = await JsRuntime.InvokeAsync<TAccount>("AuthenticationService.getUser");
            var user = await AccountClaimsPrincipalFactory.CreateUserAsync(account, Options.UserOptions);

            return user;
        }

        private async ValueTask EnsureAuthService()
        {
            if (!_initialized)
            {
                await JsRuntime.InvokeVoidAsync("AuthenticationService.init", Options.ProviderOptions);
                _initialized = true;
            }
        }

        private void UpdateUser(Task<ClaimsPrincipal> task)
        {
            NotifyAuthenticationStateChanged(UpdateAuthenticationState(task));

            static async Task<AuthenticationState> UpdateAuthenticationState(Task<ClaimsPrincipal> futureUser) => new AuthenticationState(await futureUser);
        }
    }

    // Internal for testing purposes
    internal struct InternalAccessTokenResult
    {
        public string Status { get; set; }
        public AccessToken Token { get; set; }
        public string RedirectUrl { get; set; }
    }

    // Internal for testing purposes
    internal struct InternalRemoteAuthenticationResult<TRemoteAuthenticationState> where TRemoteAuthenticationState : RemoteAuthenticationState
    {
        public string Status { get; set; }

        public string ErrorMessage { get; set; }

        public TRemoteAuthenticationState State { get; set; }

        public RemoteAuthenticationResult<TRemoteAuthenticationState> Convert()
        {
            var result = new RemoteAuthenticationResult<TRemoteAuthenticationState>();
            result.ErrorMessage = ErrorMessage;
            result.State = State;

            if (Status != null && Enum.TryParse<RemoteAuthenticationStatus>(Status, ignoreCase: true, out var status))
            {
                result.Status = status;
            }
            else
            {
                throw new InvalidOperationException($"Can't convert status '${Status ?? "(null)"}'.");
            }

            return result;
        }
    }
}
