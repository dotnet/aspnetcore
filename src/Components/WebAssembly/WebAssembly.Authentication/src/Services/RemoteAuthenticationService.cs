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
    IAccessTokenProvider,
    IDisposable
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TProviderOptions : new()
    where TAccount : RemoteUserAccount
{
    private static readonly TimeSpan _userCacheRefreshInterval = TimeSpan.FromSeconds(60);
    private readonly ILogger<RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>> _logger;
    private readonly DotNetObjectReference<LoggerWrapper> _loggerObjectRef;
    private bool _initialized;

    // This defaults to 1/1/1970
    private DateTimeOffset _userLastCheck = DateTimeOffset.FromUnixTimeSeconds(0);
    private ClaimsPrincipal _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
    private bool disposedValue;

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
        ILogger<RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>> logger)
    {
        JsRuntime = jsRuntime;
        Navigation = navigation;
        AccountClaimsPrincipalFactory = accountClaimsPrincipalFactory;
        Options = options.Value;
        _logger = logger;        
        _loggerObjectRef = DotNetObjectReference.Create(new LoggerWrapper(logger));
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await GetUser(useCache: true));

    /// <inheritdoc />
    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await EnsureAuthService();
        var result = await JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signIn", context);
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
        var result = await JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signOut", context);
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
            result.Status == AccessTokenResultStatus.RequiresRedirect ? InteractiveRequestOptions.GetToken(GetReturnUrl(null)) : null);
    }

    /// <inheritdoc />
    [DynamicDependency(JsonSerialized, typeof(AccessToken))]
    [DynamicDependency(JsonSerialized, typeof(AccessTokenRequestOptions))]
    public virtual async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        await EnsureAuthService();
        var result = await JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken", options);

        return new AccessTokenResult(
            result.Status,
            result.Token,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? Options.AuthenticationPaths.LogInPath : null,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? InteractiveRequestOptions.GetToken(GetReturnUrl(options.ReturnUrl), options.Scopes) : null);
    }

    private string GetReturnUrl(string customReturnUrl) =>
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

    private async ValueTask EnsureAuthService()
    {
        if (!_initialized)
        {
            await JsRuntime.InvokeVoidAsync("AuthenticationService.init", Options.ProviderOptions, _loggerObjectRef);
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

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _loggerObjectRef?.Dispose();
            }

            disposedValue = true;
        }
    }

    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private class LoggerWrapper
    {
        private readonly ILogger _logger;

        public LoggerWrapper(ILogger logger)
        {
            _logger = logger;
        }

        [JSInvokable]
        public void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        [JSInvokable]
        public void IsEnabled(LogLevel level)
        {
            _logger.IsEnabled(level);
        }
    }
}

// Internal for testing purposes
internal record struct InternalAccessTokenResult([property: JsonConverter(typeof(JsonStringEnumConverter))] AccessTokenResultStatus Status, AccessToken Token);
