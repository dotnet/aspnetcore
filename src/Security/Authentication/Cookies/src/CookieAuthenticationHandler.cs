// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Implementation for the cookie-based authentication handler.
/// </summary>
public class CookieAuthenticationHandler : SignInAuthenticationHandler<CookieAuthenticationOptions>
{
    // This should be kept in sync with HttpConnectionDispatcher
    private const string HeaderValueNoCache = "no-cache";
    private const string HeaderValueNoCacheNoStore = "no-cache,no-store";
    private const string HeaderValueEpocDate = "Thu, 01 Jan 1970 00:00:00 GMT";
    private const string SessionIdClaim = "Microsoft.AspNetCore.Authentication.Cookies-SessionId";

    private bool _shouldRefresh;
    private bool _signInCalled;
    private bool _signOutCalled;

    private DateTimeOffset? _refreshIssuedUtc;
    private DateTimeOffset? _refreshExpiresUtc;
    private string? _sessionKey;
    private Task<AuthenticateResult>? _readCookieTask;
    private AuthenticationTicket? _refreshTicket;

    /// <summary>
    /// Initializes a new instance of <see cref="CookieAuthenticationHandler"/>.
    /// </summary>
    /// <param name="options">Accessor to <see cref="CookieAuthenticationOptions"/>.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    /// <param name="clock">The <see cref="ISystemClock"/>.</param>
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public CookieAuthenticationHandler(IOptionsMonitor<CookieAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="CookieAuthenticationHandler"/>.
    /// </summary>
    /// <param name="options">Accessor to <see cref="CookieAuthenticationOptions"/>.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    public CookieAuthenticationHandler(IOptionsMonitor<CookieAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    { }

    /// <summary>
    /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
    /// If it is not provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    protected new CookieAuthenticationEvents Events
    {
        get { return (CookieAuthenticationEvents)base.Events!; }
        set { base.Events = value; }
    }

    /// <inheritdoc />
    protected override Task InitializeHandlerAsync()
    {
        // Cookies needs to finish the response
        Context.Response.OnStarting(FinishResponseAsync);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a new instance of the events instance.
    /// </summary>
    /// <returns>A new instance of the events instance.</returns>
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new CookieAuthenticationEvents());

    private Task<AuthenticateResult> EnsureCookieTicket()
    {
        // We only need to read the ticket once
        if (_readCookieTask == null)
        {
            _readCookieTask = ReadCookieTicket();
        }
        return _readCookieTask;
    }

    private async Task CheckForRefreshAsync(AuthenticationTicket ticket)
    {
        var currentUtc = TimeProvider.GetUtcNow();
        var issuedUtc = ticket.Properties.IssuedUtc;
        var expiresUtc = ticket.Properties.ExpiresUtc;
        var allowRefresh = ticket.Properties.AllowRefresh ?? true;
        if (issuedUtc != null && expiresUtc != null && Options.SlidingExpiration && allowRefresh)
        {
            var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
            var timeRemaining = expiresUtc.Value.Subtract(currentUtc);

            var eventContext = new CookieSlidingExpirationContext(Context, Scheme, Options, ticket, timeElapsed, timeRemaining)
            {
                ShouldRenew = timeRemaining < timeElapsed,
            };
            await Events.CheckSlidingExpiration(eventContext);

            if (eventContext.ShouldRenew)
            {
                RequestRefresh(ticket);
            }
        }
    }

    private void RequestRefresh(AuthenticationTicket ticket, ClaimsPrincipal? replacedPrincipal = null)
    {
        var issuedUtc = ticket.Properties.IssuedUtc;
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (issuedUtc != null && expiresUtc != null)
        {
            _shouldRefresh = true;
            var currentUtc = TimeProvider.GetUtcNow();
            _refreshIssuedUtc = currentUtc;
            var timeSpan = expiresUtc.Value.Subtract(issuedUtc.Value);
            _refreshExpiresUtc = currentUtc.Add(timeSpan);
            _refreshTicket = CloneTicket(ticket, replacedPrincipal);
        }
    }

    private static AuthenticationTicket CloneTicket(AuthenticationTicket ticket, ClaimsPrincipal? replacedPrincipal)
    {
        var principal = replacedPrincipal ?? ticket.Principal;
        var newPrincipal = new ClaimsPrincipal();
        foreach (var identity in principal.Identities)
        {
            newPrincipal.AddIdentity(identity.Clone());
        }

        var newProperties = new AuthenticationProperties();
        foreach (var item in ticket.Properties.Items)
        {
            newProperties.Items[item.Key] = item.Value;
        }

        return new AuthenticationTicket(newPrincipal, newProperties, ticket.AuthenticationScheme);
    }

    private async Task<AuthenticateResult> ReadCookieTicket()
    {
        var cookie = Options.CookieManager.GetRequestCookie(Context, Options.Cookie.Name!);
        if (string.IsNullOrEmpty(cookie))
        {
            return AuthenticateResult.NoResult();
        }

        var ticket = Options.TicketDataFormat.Unprotect(cookie, GetTlsTokenBinding());
        if (ticket == null)
        {
            return AuthenticateResults.FailedUnprotectingTicket;
        }

        if (Options.SessionStore != null)
        {
            var claim = ticket.Principal.Claims.FirstOrDefault(c => c.Type.Equals(SessionIdClaim));
            if (claim == null)
            {
                return AuthenticateResults.MissingSessionId;
            }
            // Only store _sessionKey if it matches an existing session. Otherwise we'll create a new one.
            ticket = await Options.SessionStore.RetrieveAsync(claim.Value, Context, Context.RequestAborted);
            if (ticket == null)
            {
                return AuthenticateResults.MissingIdentityInSession;
            }
            _sessionKey = claim.Value;
        }

        var currentUtc = TimeProvider.GetUtcNow();
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (expiresUtc != null && expiresUtc.Value < currentUtc)
        {
            if (Options.SessionStore != null)
            {
                await Options.SessionStore.RemoveAsync(_sessionKey!, Context, Context.RequestAborted);

                // Clear out the session key if its expired, so renew doesn't try to use it
                _sessionKey = null;
            }
            return AuthenticateResults.ExpiredTicket;
        }

        // Finally we have a valid ticket
        return AuthenticateResult.Success(ticket);
    }

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = await EnsureCookieTicket();
        if (!result.Succeeded)
        {
            return result;
        }

        // We check this before the ValidatePrincipal event because we want to make sure we capture a clean clone
        // without picking up any per-request modifications to the principal.
        await CheckForRefreshAsync(result.Ticket);

        Debug.Assert(result.Ticket != null);
        var context = new CookieValidatePrincipalContext(Context, Scheme, Options, result.Ticket);
        await Events.ValidatePrincipal(context);

        if (context.Principal == null)
        {
            return AuthenticateResults.NoPrincipal;
        }

        if (context.ShouldRenew)
        {
            RequestRefresh(result.Ticket, context.Principal);
        }

        return AuthenticateResult.Success(new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name));
    }

    private CookieOptions BuildCookieOptions()
    {
        var cookieOptions = Options.Cookie.Build(Context);
        // ignore the 'Expires' value as this will be computed elsewhere
        cookieOptions.Expires = null;

        return cookieOptions;
    }

    /// <inheritdoc />
    protected virtual async Task FinishResponseAsync()
    {
        // Only renew if requested, and neither sign in or sign out was called
        if (!_shouldRefresh || _signInCalled || _signOutCalled)
        {
            return;
        }

        var ticket = _refreshTicket;
        if (ticket != null)
        {
            var properties = ticket.Properties;

            if (_refreshIssuedUtc.HasValue)
            {
                properties.IssuedUtc = _refreshIssuedUtc;
            }

            if (_refreshExpiresUtc.HasValue)
            {
                properties.ExpiresUtc = _refreshExpiresUtc;
            }

            if (Options.SessionStore != null && _sessionKey != null)
            {
                await Options.SessionStore.RenewAsync(_sessionKey, ticket, Context, Context.RequestAborted);
                var principal = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] { new Claim(SessionIdClaim, _sessionKey, ClaimValueTypes.String, Options.ClaimsIssuer) },
                        Scheme.Name));
                ticket = new AuthenticationTicket(principal, null, Scheme.Name);
            }

            var cookieValue = Options.TicketDataFormat.Protect(ticket, GetTlsTokenBinding());

            var cookieOptions = BuildCookieOptions();
            if (properties.IsPersistent && _refreshExpiresUtc.HasValue)
            {
                cookieOptions.Expires = _refreshExpiresUtc.Value.ToUniversalTime();
            }

            Options.CookieManager.AppendResponseCookie(
                Context,
                Options.Cookie.Name!,
                cookieValue,
                cookieOptions);

            await ApplyHeaders(shouldRedirect: false, shouldHonorReturnUrlParameter: false, properties: properties);
        }
    }

    /// <inheritdoc />
    protected override async Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        ArgumentNullException.ThrowIfNull(user);

        properties = properties ?? new AuthenticationProperties();

        _signInCalled = true;

        // Process the request cookie to initialize members like _sessionKey.
        await EnsureCookieTicket();
        var cookieOptions = BuildCookieOptions();

        var signInContext = new CookieSigningInContext(
            Context,
            Scheme,
            Options,
            user,
            properties,
            cookieOptions);

        DateTimeOffset issuedUtc;
        if (signInContext.Properties.IssuedUtc.HasValue)
        {
            issuedUtc = signInContext.Properties.IssuedUtc.Value;
        }
        else
        {
            issuedUtc = TimeProvider.GetUtcNow();
            signInContext.Properties.IssuedUtc = issuedUtc;
        }

        if (!signInContext.Properties.ExpiresUtc.HasValue)
        {
            signInContext.Properties.ExpiresUtc = issuedUtc.Add(Options.ExpireTimeSpan);
        }

        await Events.SigningIn(signInContext);

        if (signInContext.Properties.IsPersistent)
        {
            var expiresUtc = signInContext.Properties.ExpiresUtc ?? issuedUtc.Add(Options.ExpireTimeSpan);
            signInContext.CookieOptions.Expires = expiresUtc.ToUniversalTime();
        }

        var ticket = new AuthenticationTicket(signInContext.Principal!, signInContext.Properties, signInContext.Scheme.Name);

        if (Options.SessionStore != null)
        {
            if (_sessionKey != null)
            {
                // Renew the ticket in cases of multiple requests see: https://github.com/dotnet/aspnetcore/issues/22135
                await Options.SessionStore.RenewAsync(_sessionKey, ticket, Context, Context.RequestAborted);
            }
            else
            {
                _sessionKey = await Options.SessionStore.StoreAsync(ticket, Context, Context.RequestAborted);
            }

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(SessionIdClaim, _sessionKey, ClaimValueTypes.String, Options.ClaimsIssuer) },
                    Options.ClaimsIssuer));
            ticket = new AuthenticationTicket(principal, null, Scheme.Name);
        }

        var cookieValue = Options.TicketDataFormat.Protect(ticket, GetTlsTokenBinding());

        Options.CookieManager.AppendResponseCookie(
            Context,
            Options.Cookie.Name!,
            cookieValue,
            signInContext.CookieOptions);

        var signedInContext = new CookieSignedInContext(
            Context,
            Scheme,
            signInContext.Principal!,
            signInContext.Properties,
            Options);

        await Events.SignedIn(signedInContext);

        // Only honor the ReturnUrl query string parameter on the login path
        var shouldHonorReturnUrlParameter = Options.LoginPath.HasValue && OriginalPath == Options.LoginPath;
        await ApplyHeaders(shouldRedirect: true, shouldHonorReturnUrlParameter, signedInContext.Properties);

        Logger.AuthenticationSchemeSignedIn(Scheme.Name);
    }

    /// <inheritdoc />
    protected override async Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        properties = properties ?? new AuthenticationProperties();

        _signOutCalled = true;

        // Process the request cookie to initialize members like _sessionKey.
        await EnsureCookieTicket();
        var cookieOptions = BuildCookieOptions();
        if (Options.SessionStore != null && _sessionKey != null)
        {
            await Options.SessionStore.RemoveAsync(_sessionKey, Context, Context.RequestAborted);
        }

        var context = new CookieSigningOutContext(
            Context,
            Scheme,
            Options,
            properties,
            cookieOptions);

        await Events.SigningOut(context);

        Options.CookieManager.DeleteCookie(
            Context,
            Options.Cookie.Name!,
            context.CookieOptions);

        // Only honor the ReturnUrl query string parameter on the logout path
        var shouldHonorReturnUrlParameter = Options.LogoutPath.HasValue && OriginalPath == Options.LogoutPath;
        await ApplyHeaders(shouldRedirect: true, shouldHonorReturnUrlParameter, context.Properties);

        Logger.AuthenticationSchemeSignedOut(Scheme.Name);
    }

    private async Task ApplyHeaders(bool shouldRedirect, bool shouldHonorReturnUrlParameter, AuthenticationProperties properties)
    {
        Response.Headers.CacheControl = HeaderValueNoCacheNoStore;
        Response.Headers.Pragma = HeaderValueNoCache;
        Response.Headers.Expires = HeaderValueEpocDate;

        if (shouldRedirect && Response.StatusCode == 200)
        {
            // set redirect uri in order:
            // 1. properties.RedirectUri
            // 2. query parameter ReturnUrlParameter (if the request path matches the path set in the options)
            //
            // Absolute uri is not allowed if it is from query string as query string is not
            // a trusted source.
            var redirectUri = properties.RedirectUri;
            if (shouldHonorReturnUrlParameter && string.IsNullOrEmpty(redirectUri))
            {
                redirectUri = Request.Query[Options.ReturnUrlParameter];
                if (string.IsNullOrEmpty(redirectUri) || !IsHostRelative(redirectUri))
                {
                    redirectUri = null;
                }
            }

            if (redirectUri != null)
            {
                await Events.RedirectToReturnUrl(
                    new RedirectContext<CookieAuthenticationOptions>(Context, Scheme, Options, properties, redirectUri));
            }
        }
    }

    private static bool IsHostRelative(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        if (path.Length == 1)
        {
            return path[0] == '/';
        }
        return path[0] == '/' && path[1] != '/' && path[1] != '\\';
    }

    /// <inheritdoc />
    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        var returnUrl = properties.RedirectUri;
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = OriginalPathBase + OriginalPath + Request.QueryString;
        }
        var accessDeniedUri = Options.AccessDeniedPath + QueryString.Create(Options.ReturnUrlParameter, returnUrl);
        var redirectContext = new RedirectContext<CookieAuthenticationOptions>(Context, Scheme, Options, properties, BuildRedirectUri(accessDeniedUri));
        await Events.RedirectToAccessDenied(redirectContext);
    }

    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var redirectUri = properties.RedirectUri;
        if (string.IsNullOrEmpty(redirectUri))
        {
            redirectUri = OriginalPathBase + OriginalPath + Request.QueryString;
        }

        var loginUri = Options.LoginPath + QueryString.Create(Options.ReturnUrlParameter, redirectUri);
        var redirectContext = new RedirectContext<CookieAuthenticationOptions>(Context, Scheme, Options, properties, BuildRedirectUri(loginUri));
        await Events.RedirectToLogin(redirectContext);
    }

    private string? GetTlsTokenBinding()
    {
        var binding = Context.Features.Get<ITlsTokenBindingFeature>()?.GetProvidedTokenBindingId();
        return binding == null ? null : Convert.ToBase64String(binding);
    }
}
