// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// The default implementation for <see cref="IRemoteAuthenticationService{TRemoteAuthenticationState}"/> that uses JS interop to authenticate the user.
/// </summary>
/// <typeparam name="TRemoteAuthenticationState">The state to preserve across authentication operations.</typeparam>
/// <typeparam name="TAccount">The type of the <see cref="RemoteUserAccount" />.</typeparam>
/// <typeparam name="TProviderOptions">The options to be passed down to the underlying JavaScript library handling the authentication operations.</typeparam>
public class RemoteAuthenticationService<
[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState,
[DynamicallyAccessedMembers(JsonSerialized)] TAccount,
[DynamicallyAccessedMembers(JsonSerialized)] TProviderOptions> :
    AuthenticationStateProvider,
    IRemoteAuthenticationService<TRemoteAuthenticationState>,
    IAccessTokenProvider
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TProviderOptions : new()
    where TAccount : RemoteUserAccount
{
    private static readonly TimeSpan _userCacheRefreshInterval = TimeSpan.FromSeconds(60);
    private bool _initialized;
    private readonly RemoteAuthenticationServiceJavaScriptLoggingOptions _loggingOptions;

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
    [Obsolete("Use the constructor RemoteAuthenticationService(IJSRuntime,IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>>,NavigationManager,AccountClaimsPrincipalFactory<TAccount>,ILogger<RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>>) instead.")]
    public RemoteAuthenticationService(
        IJSRuntime jsRuntime,
        IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>> options,
        NavigationManager navigation,
        AccountClaimsPrincipalFactory<TAccount> accountClaimsPrincipalFactory)
        : this(jsRuntime, options, navigation, accountClaimsPrincipalFactory, null)
    {
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/> to use for performing JavaScript interop operations.</param>
    /// <param name="options">The options to be passed down to the underlying JavaScript library handling the authentication operations.</param>
    /// <param name="navigation">The <see cref="NavigationManager"/> used to generate URLs.</param>
    /// <param name="accountClaimsPrincipalFactory">The <see cref="AccountClaimsPrincipalFactory{TAccount}"/> used to generate the <see cref="ClaimsPrincipal"/> for the user.</param>
    /// <param name="logger">The logger to use for login authentication operations.</param>
    public RemoteAuthenticationService(
        IJSRuntime jsRuntime,
        IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>> options,
        NavigationManager navigation,
        AccountClaimsPrincipalFactory<TAccount> accountClaimsPrincipalFactory,
        ILogger<RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>>? logger)
    {
        JsRuntime = jsRuntime;
        Navigation = navigation;
        AccountClaimsPrincipalFactory = accountClaimsPrincipalFactory;
        Options = options.Value;
        _loggingOptions = new RemoteAuthenticationServiceJavaScriptLoggingOptions
        {
            DebugEnabled = logger?.IsEnabled(LogLevel.Debug) ?? false,
            TraceEnabled = logger?.IsEnabled(LogLevel.Trace) ?? false
        };
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await GetUser(useCache: true));

    /// <inheritdoc />
    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await EnsureAuthService();
        var result = await JSInvokeWithContextAsync<RemoteAuthenticationContext<TRemoteAuthenticationState>, RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signIn", context);
        await UpdateUserOnSuccess(result);

        return result;
    }

    /// <inheritdoc />
    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await EnsureAuthService();
        var result = await JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignIn", context.Url);
        await UpdateUserOnSuccess(result);

        return result;
    }

    /// <inheritdoc />
    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await EnsureAuthService();
        var result = await JSInvokeWithContextAsync<RemoteAuthenticationContext<TRemoteAuthenticationState>, RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signOut", context);
        await UpdateUserOnSuccess(result);

        return result;
    }

    /// <inheritdoc />
    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await EnsureAuthService();
        var result = await JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignOut", context.Url);
        await UpdateUserOnSuccess(result);

        return result;
    }

    /// <inheritdoc />
    public virtual async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        await EnsureAuthService();
        var result = await JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken");

        return new AccessTokenResult(
            result.Status,
            result.Token,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? Options.AuthenticationPaths.LogInPath : null,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? new InteractiveRequestOptions
            {
                Interaction = InteractionType.GetToken,
                ReturnUrl = GetReturnUrl(null)
            } : null);
    }

    /// <inheritdoc />
    [DynamicDependency(JsonSerialized, typeof(AccessToken))]
    [DynamicDependency(JsonSerialized, typeof(AccessTokenRequestOptions))]

    public virtual async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        await EnsureAuthService();
        var result = await JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken", options);

        return new AccessTokenResult(
            result.Status,
            result.Token,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? Options.AuthenticationPaths.LogInPath : null,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? new InteractiveRequestOptions
            {
                Interaction = InteractionType.GetToken,
                ReturnUrl = GetReturnUrl(options.ReturnUrl),
                Scopes = options.Scopes ?? Array.Empty<string>(),
            } : null);
    }

    // JSRuntime.InvokeAsync does not properly annotate all arguments with DynamicallyAccessedMembersAttribute. https://github.com/dotnet/aspnetcore/issues/39839
    // Calling JsRuntime.InvokeAsync directly results allows the RemoteAuthenticationContext.State getter to be trimmed. https://github.com/dotnet/aspnetcore/issues/49956
    private ValueTask<TResult> JSInvokeWithContextAsync<[DynamicallyAccessedMembers(JsonSerialized)] TContext, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(
        string identifier, TContext context) => JsRuntime.InvokeAsync<TResult>(identifier, context);

    private string GetReturnUrl(string? customReturnUrl) =>
        customReturnUrl != null ? Navigation.ToAbsoluteUri(customReturnUrl).AbsoluteUri : Navigation.Uri;

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

    [DynamicDependency(JsonSerialized, typeof(RemoteAuthenticationServiceJavaScriptLoggingOptions))]
    private async ValueTask EnsureAuthService()
    {
        if (!_initialized)
        {
            await JsRuntime.InvokeVoidAsync("AuthenticationService.init", Options.ProviderOptions, _loggingOptions);
            _initialized = true;
        }
    }
    private async Task UpdateUserOnSuccess(RemoteAuthenticationResult<TRemoteAuthenticationState> result)
    {
        if (result.Status == RemoteAuthenticationStatus.Success)
        {
            var getUserTask = GetUser();
            await getUserTask;
            UpdateUser(getUserTask);
        }
    }

    private void UpdateUser(Task<ClaimsPrincipal> task)
    {
        NotifyAuthenticationStateChanged(UpdateAuthenticationState(task));

        static async Task<AuthenticationState> UpdateAuthenticationState(Task<ClaimsPrincipal> futureUser) => new AuthenticationState(await futureUser);
    }

}

// We need to do this as it can't be nested inside RemoteAuthenticationService because
// it needs to be put in an attribute for linking purposes and that can't be an open generic type.
internal class RemoteAuthenticationServiceJavaScriptLoggingOptions
{
    public bool DebugEnabled { get; set; }

    public bool TraceEnabled { get; set; }
}

// Internal for testing purposes
internal readonly struct InternalAccessTokenResult
{
    [JsonConverter(typeof(JsonStringEnumConverter<AccessTokenResultStatus>))]
    public AccessTokenResultStatus Status { get; init; }

    public AccessToken Token { get; init; }

    public InternalAccessTokenResult(AccessTokenResultStatus status, AccessToken token)
    {
        Status = status;
        Token = token;
    }
}
