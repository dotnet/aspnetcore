// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// A component that handles remote authentication operations in an application.
    /// </summary>
    /// <typeparam name="TAuthenticationState">The user state type persisted while the operation is in progress. It must be serializable.</typeparam>
    public class RemoteAuthenticatorViewCore<TAuthenticationState> : ComponentBase where TAuthenticationState : RemoteAuthenticationState
    {
        private string _message;
        private RemoteAuthenticationApplicationPathsOptions _applicationPaths;
        private string _action;

        /// <summary>
        /// Gets or sets the <see cref="RemoteAuthenticationActions"/> action the component needs to handle.
        /// </summary>
        [Parameter] public string Action { get => _action; set => _action = value?.ToLowerInvariant(); }

        /// <summary>
        /// Gets or sets the <typeparamref name="TAuthenticationState"/> instance to be preserved during the authentication operation.
        /// </summary>
        [Parameter] public TAuthenticationState AuthenticationState { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.LogIn"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment LoggingIn { get; set; } = DefaultLogInFragment;

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.Register"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment Registering { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.Profile"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment UserProfile { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.LogInCallback"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment CompletingLoggingIn { get; set; } = DefaultLogInCallbackFragment;

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.LogInFailed"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment<string> LogInFailed { get; set; } = DefaultLogInFailedFragment;

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.LogOut"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment LogOut { get; set; } = DefaultLogOutFragment;

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.LogOutCallback"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment CompletingLogOut { get; set; } = DefaultLogOutCallbackFragment;

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.LogOutFailed"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment<string> LogOutFailed { get; set; } = DefaultLogOutFailedFragment;

        /// <summary>
        /// Gets or sets a <see cref="RenderFragment"/> with the UI to display while <see cref="RemoteAuthenticationActions.LogOutSucceeded"/> is being handled.
        /// </summary>
        [Parameter] public RenderFragment LogOutSucceeded { get; set; } = DefaultLoggedOutFragment;

        /// <summary>
        /// Gets or sets an event callback that will be invoked with the stored authentication state when a log in operation succeeds.
        /// </summary>
        [Parameter] public EventCallback<TAuthenticationState> OnLogInSucceeded { get; set; }

        /// <summary>
        /// Gets or sets an event callback that will be invoked with the stored authentication state when a log out operation succeeds.
        /// </summary>
        [Parameter] public EventCallback<TAuthenticationState> OnLogOutSucceeded { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IJSRuntime"/> to use for performin JavaScript interop.
        /// </summary>
        [Inject] internal IJSRuntime JS { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="NavigationManager"/> to use for redirecting the browser.
        /// </summary>
        [Inject] internal NavigationManager Navigation { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IRemoteAuthenticationService{TRemoteAuthenticationState}"/> to use for handling the underlying authentication protocol.
        /// </summary>
        [Inject] internal IRemoteAuthenticationService<TAuthenticationState> AuthenticationService { get; set; }

        /// <summary>
        /// Gets or sets a default <see cref="IRemoteAuthenticationPathsProvider"/> to use as fallback if an <see cref="ApplicationPaths"/> has not been explicitly specified.
        /// </summary>
#pragma warning disable PUB0001 // Pubternal type in public API
        [Inject] internal IRemoteAuthenticationPathsProvider RemoteApplicationPathsProvider { get; set; }
#pragma warning restore PUB0001 // Pubternal type in public API

        /// <summary>
        /// Gets or sets a default <see cref="AuthenticationStateProvider"/> with the current user.
        /// </summary>
        [Inject] internal AuthenticationStateProvider AuthenticationProvider { get; set; }

        /// <summary>
        /// Gets or sets a default <see cref="AuthenticationStateProvider"/> with the current user.
        /// </summary>
        [Inject] internal SignOutSessionStateManager SignOutManager { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RemoteAuthenticationApplicationPathsOptions"/> with the paths to different authentication pages.
        /// </summary>
        [Parameter]
        public RemoteAuthenticationApplicationPathsOptions ApplicationPaths
        {
            get => _applicationPaths ?? RemoteApplicationPathsProvider.ApplicationPaths;
            set => _applicationPaths = value;
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            switch (Action)
            {
                case RemoteAuthenticationActions.Profile:
                    builder.AddContent(0, UserProfile);
                    break;
                case RemoteAuthenticationActions.Register:
                    builder.AddContent(0, Registering);
                    break;
                case RemoteAuthenticationActions.LogIn:
                    builder.AddContent(0, LoggingIn);
                    break;
                case RemoteAuthenticationActions.LogInCallback:
                    builder.AddContent(0, CompletingLoggingIn);
                    break;
                case RemoteAuthenticationActions.LogInFailed:
                    builder.AddContent(0, LogInFailed(_message));
                    break;
                case RemoteAuthenticationActions.LogOut:
                    builder.AddContent(0, LogOut);
                    break;
                case RemoteAuthenticationActions.LogOutCallback:
                    builder.AddContent(0, CompletingLogOut);
                    break;
                case RemoteAuthenticationActions.LogOutFailed:
                    builder.AddContent(0, LogOutFailed(_message));
                    break;
                case RemoteAuthenticationActions.LogOutSucceeded:
                    builder.AddContent(0, LogOutSucceeded);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid action '{Action}'.");
            }
        }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            switch (Action)
            {
                case RemoteAuthenticationActions.LogIn:
                    await ProcessLogIn(GetReturnUrl(state: null));
                    break;
                case RemoteAuthenticationActions.LogInCallback:
                    await ProcessLogInCallback();
                    break;
                case RemoteAuthenticationActions.LogInFailed:
                    break;
                case RemoteAuthenticationActions.Profile:
                    if (ApplicationPaths.RemoteProfilePath == null)
                    {
                        UserProfile ??= ProfileNotSupportedFragment;
                    }
                    else
                    {
                        UserProfile ??= LoggingIn;
                        await RedirectToProfile();
                    }
                    break;
                case RemoteAuthenticationActions.Register:
                    if (ApplicationPaths.RemoteRegisterPath == null)
                    {
                        Registering ??= RegisterNotSupportedFragment;
                    }
                    else
                    {
                        Registering ??= LoggingIn;
                        await RedirectToRegister();
                    }
                    break;
                case RemoteAuthenticationActions.LogOut:
                    await ProcessLogOut(GetReturnUrl(state: null, Navigation.ToAbsoluteUri(ApplicationPaths.LogOutSucceededPath).AbsoluteUri));
                    break;
                case RemoteAuthenticationActions.LogOutCallback:
                    await ProcessLogOutCallback();
                    break;
                case RemoteAuthenticationActions.LogOutFailed:
                    break;
                case RemoteAuthenticationActions.LogOutSucceeded:
                    break;
                default:
                    throw new InvalidOperationException($"Invalid action '{Action}'.");
            }
        }

        private async Task ProcessLogIn(string returnUrl)
        {
            AuthenticationState.ReturnUrl = returnUrl;
            var result = await AuthenticationService.SignInAsync(new RemoteAuthenticationContext<TAuthenticationState>
            {
                State = AuthenticationState
            });

            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    break;
                case RemoteAuthenticationStatus.Success:
                    await OnLogInSucceeded.InvokeAsync(result.State);
                    await NavigateToReturnUrl(GetReturnUrl(result.State, returnUrl));
                    break;
                case RemoteAuthenticationStatus.Failure:
                    _message = result.ErrorMessage;
                    Navigation.NavigateTo(ApplicationPaths.LogInFailedPath);
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                default:
                    throw new InvalidOperationException($"Invalid authentication result status '{result.Status}'.");
            }
        }

        private async Task ProcessLogInCallback()
        {
            var url = Navigation.Uri;
            var result = await AuthenticationService.CompleteSignInAsync(new RemoteAuthenticationContext<TAuthenticationState> { Url = url });
            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    // There should not be any redirects as the only time CompleteSignInAsync finishes
                    // is when we are doing a redirect sign in flow.
                    throw new InvalidOperationException("Should not redirect.");
                case RemoteAuthenticationStatus.Success:
                    await OnLogInSucceeded.InvokeAsync(result.State);
                    await NavigateToReturnUrl(GetReturnUrl(result.State));
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                    break;
                case RemoteAuthenticationStatus.Failure:
                    var uri = Navigation.ToAbsoluteUri($"{ApplicationPaths.LogInFailedPath}?message={Uri.EscapeDataString(result.ErrorMessage)}").ToString();
                    await NavigateToReturnUrl(uri);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid authentication result status '{result.Status}'.");
            }
        }

        private async Task ProcessLogOut(string returnUrl)
        {
            if (!await SignOutManager.ValidateSignOutState())
            {
                var uri = $"{Navigation.ToAbsoluteUri(ApplicationPaths.LogOutFailedPath)}?message={Uri.EscapeDataString("The logout was not initiated from within the page.")}";
                Navigation.NavigateTo(uri);

                return;
            }

            AuthenticationState.ReturnUrl = returnUrl;

            var state = await AuthenticationProvider.GetAuthenticationStateAsync();
            var isauthenticated = state.User.Identity.IsAuthenticated;
            if (isauthenticated)
            {
                var result = await AuthenticationService.SignOutAsync(new RemoteAuthenticationContext<TAuthenticationState> { State = AuthenticationState });
                switch (result.Status)
                {
                    case RemoteAuthenticationStatus.Redirect:
                        break;
                    case RemoteAuthenticationStatus.Success:
                        await OnLogOutSucceeded.InvokeAsync(result.State);
                        await NavigateToReturnUrl(returnUrl);
                        break;
                    case RemoteAuthenticationStatus.OperationCompleted:
                        break;
                    case RemoteAuthenticationStatus.Failure:
                        _message = result.ErrorMessage;
                        Navigation.NavigateTo(ApplicationPaths.LogOutFailedPath);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid authentication result status.");
                }
            }
            else
            {
                await NavigateToReturnUrl(returnUrl);
            }
        }

        private async Task ProcessLogOutCallback()
        {
            var result = await AuthenticationService.CompleteSignOutAsync(new RemoteAuthenticationContext<TAuthenticationState> { Url = Navigation.Uri });
            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    // There should not be any redirects as the only time completeAuthentication finishes
                    // is when we are doing a redirect sign in flow.
                    throw new InvalidOperationException("Should not redirect.");
                case RemoteAuthenticationStatus.Success:
                    await OnLogOutSucceeded.InvokeAsync(result.State);
                    await NavigateToReturnUrl(GetReturnUrl(result.State, Navigation.ToAbsoluteUri(ApplicationPaths.LogOutSucceededPath).ToString()));
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                    break;
                case RemoteAuthenticationStatus.Failure:
                    var uri = Navigation.ToAbsoluteUri($"{ApplicationPaths.LogOutFailedPath}?message={Uri.EscapeDataString(result.ErrorMessage)}").ToString();
                    await NavigateToReturnUrl(uri);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid authentication result status.");
            }
        }

        private string GetReturnUrl(TAuthenticationState state, string defaultReturnUrl = null)
        {
            if (state?.ReturnUrl != null)
            {
                return state.ReturnUrl;
            }

            var fromQuery = QueryStringHelper.GetParameter(new Uri(Navigation.Uri).Query, "returnUrl");
            if (!string.IsNullOrWhiteSpace(fromQuery) && !fromQuery.StartsWith(Navigation.BaseUri, StringComparison.Ordinal))
            {
                // This is an extra check to prevent open redirects.
                throw new InvalidOperationException("Invalid return url. The return url needs to have the same origin as the current page.");
            }

            return fromQuery ?? defaultReturnUrl ?? Navigation.BaseUri;
        }

        private async Task NavigateToReturnUrl(string returnUrl) => await JS.InvokeVoidAsync("Blazor.navigateTo", returnUrl, false, true);

        private ValueTask RedirectToRegister()
        {
            var loginUrl = Navigation.ToAbsoluteUri(ApplicationPaths.LogInPath).PathAndQuery;
            var registerUrl = Navigation.ToAbsoluteUri($"{ApplicationPaths.RemoteRegisterPath}?returnUrl={Uri.EscapeDataString(loginUrl)}").PathAndQuery;

            return JS.InvokeVoidAsync("location.replace", registerUrl);
        }

        private ValueTask RedirectToProfile() => JS.InvokeVoidAsync("location.replace", Navigation.ToAbsoluteUri(ApplicationPaths.RemoteProfilePath).PathAndQuery);

        private static void DefaultLogInFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Checking login state...");
            builder.CloseElement();
        }

        private static void RegisterNotSupportedFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Registration is not supported.");
            builder.CloseElement();
        }

        private static void ProfileNotSupportedFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Editing the profile is not supported.");
            builder.CloseElement();
        }

        private static void DefaultLogInCallbackFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Completing login...");
            builder.CloseElement();
        }

        private static RenderFragment DefaultLogInFailedFragment(string message)
        {
            return builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "There was an error trying to log you in: '");
                builder.AddContent(2, message);
                builder.AddContent(3, "'");
                builder.CloseElement();
            };
        }

        private static void DefaultLogOutFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Processing logout...");
            builder.CloseElement();
        }

        private static void DefaultLogOutCallbackFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Processing logout callback...");
            builder.CloseElement();
        }

        private static RenderFragment DefaultLogOutFailedFragment(string message)
        {
            return builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "There was an error trying to log you out: '");
                builder.AddContent(2, message);
                builder.AddContent(3, "'");
                builder.CloseElement();
            };
        }

        private static void DefaultLoggedOutFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "You are logged out.");
            builder.CloseElement();
        }
    }
}
