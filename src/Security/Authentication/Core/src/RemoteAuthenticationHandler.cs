// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// An opinionated abstraction for an <see cref="AuthenticationHandler{TOptions}"/> that performs authentication using a separately hosted
/// provider.
/// </summary>
/// <typeparam name="TOptions">The type for the options used to configure the authentication handler.</typeparam>
public abstract class RemoteAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationRequestHandler
    where TOptions : RemoteAuthenticationOptions, new()
{
    private const string CorrelationProperty = ".xsrf";
    private const string CorrelationMarker = "N";
    private const string AuthSchemeKey = ".AuthScheme";

    /// <summary>
    /// The authentication scheme used by default for signin.
    /// </summary>
    protected string? SignInScheme => Options.SignInScheme;

    /// <summary>
    /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
    /// If it is not provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    protected new RemoteAuthenticationEvents Events
    {
        get { return (RemoteAuthenticationEvents)base.Events!; }
        set { base.Events = value; }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RemoteAuthenticationHandler{TOptions}" />.
    /// </summary>
    /// <param name="options">The monitor for the options instance.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    /// <param name="clock">The <see cref="ISystemClock"/>.</param>
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    protected RemoteAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    /// <summary>
    /// Initializes a new instance of <see cref="RemoteAuthenticationHandler{TOptions}" />.
    /// </summary>
    /// <param name="options">The monitor for the options instance.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    protected RemoteAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    /// <inheritdoc />
    protected override Task<object> CreateEventsAsync()
        => Task.FromResult<object>(new RemoteAuthenticationEvents());

    /// <summary>
    /// Gets a value that determines if the current authentication request should be handled by <see cref="HandleRequestAsync" />.
    /// </summary>
    /// <returns><see langword="true"/> to handle the operation, otherwise <see langword="false"/>.</returns>
    public virtual Task<bool> ShouldHandleRequestAsync()
        => Task.FromResult(Options.CallbackPath == Request.Path);

    /// <summary>
    /// Handles the current authentication request.
    /// </summary>
    /// <returns><see langword="true"/> if authentication was handled, otherwise <see langword="false"/>.</returns>
    public virtual async Task<bool> HandleRequestAsync()
    {
        if (!await ShouldHandleRequestAsync())
        {
            return false;
        }

        AuthenticationTicket? ticket = null;
        Exception? exception = null;
        AuthenticationProperties? properties = null;
        try
        {
            var authResult = await HandleRemoteAuthenticateAsync();
            if (authResult == null)
            {
                exception = new InvalidOperationException("Invalid return state, unable to redirect.");
            }
            else if (authResult.Handled)
            {
                return true;
            }
            else if (authResult.Skipped || authResult.None)
            {
                return false;
            }
            else if (!authResult.Succeeded)
            {
                exception = authResult.Failure ?? new InvalidOperationException("Invalid return state, unable to redirect.");
                properties = authResult.Properties;
            }

            ticket = authResult?.Ticket;
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            Logger.RemoteAuthenticationError(exception.Message);
            var errorContext = new RemoteFailureContext(Context, Scheme, Options, exception)
            {
                Properties = properties
            };
            await Events.RemoteFailure(errorContext);

            if (errorContext.Result != null)
            {
                if (errorContext.Result.Handled)
                {
                    return true;
                }
                else if (errorContext.Result.Skipped)
                {
                    return false;
                }
                else if (errorContext.Result.Failure != null)
                {
                    throw new AuthenticationFailureException("An error was returned from the RemoteFailure event.", errorContext.Result.Failure);
                }
            }

            if (errorContext.Failure != null)
            {
                throw new AuthenticationFailureException("An error was encountered while handling the remote login.", errorContext.Failure);
            }
        }

        // We have a ticket if we get here
        Debug.Assert(ticket != null);
        var ticketContext = new TicketReceivedContext(Context, Scheme, Options, ticket)
        {
            ReturnUri = ticket.Properties.RedirectUri
        };

        ticket.Properties.RedirectUri = null;

        // Mark which provider produced this identity so we can cross-check later in HandleAuthenticateAsync
        ticketContext.Properties!.Items[AuthSchemeKey] = Scheme.Name;

        await Events.TicketReceived(ticketContext);

        if (ticketContext.Result != null)
        {
            if (ticketContext.Result.Handled)
            {
                Logger.SignInHandled();
                return true;
            }
            else if (ticketContext.Result.Skipped)
            {
                Logger.SignInSkipped();
                return false;
            }
        }

        await Context.SignInAsync(SignInScheme, ticketContext.Principal!, ticketContext.Properties);

        // Default redirect path is the base path
        if (string.IsNullOrEmpty(ticketContext.ReturnUri))
        {
            ticketContext.ReturnUri = "/";
        }

        Response.Redirect(ticketContext.ReturnUri);
        return true;
    }

    /// <summary>
    /// Authenticate the user identity with the identity provider.
    ///
    /// The method process the request on the endpoint defined by CallbackPath.
    /// </summary>
    protected abstract Task<HandleRequestResult> HandleRemoteAuthenticateAsync();

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = await Context.AuthenticateAsync(SignInScheme);
        if (result != null)
        {
            if (result.Failure != null)
            {
                return result;
            }

            // The SignInScheme may be shared with multiple providers, make sure this provider issued the identity.
            var ticket = result.Ticket;
            if (ticket != null && ticket.Principal != null && ticket.Properties != null
                && ticket.Properties.Items.TryGetValue(AuthSchemeKey, out var authenticatedScheme)
                && string.Equals(Scheme.Name, authenticatedScheme, StringComparison.Ordinal))
            {
                return AuthenticateResult.Success(new AuthenticationTicket(ticket.Principal,
                    ticket.Properties, Scheme.Name));
            }

            return AuthenticateResult.NoResult();
        }

        return AuthenticateResult.Fail("Remote authentication does not directly support AuthenticateAsync");
    }

    /// <inheritdoc />
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => Context.ForbidAsync(SignInScheme);

    /// <summary>
    /// Produces a cookie containing a nonce used to correlate the current remote authentication request.
    /// </summary>
    /// <param name="properties"></param>
    protected virtual void GenerateCorrelationId(AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var correlationId = Base64UrlTextEncoder.Encode(bytes);

        var cookieOptions = Options.CorrelationCookie.Build(Context, TimeProvider.GetUtcNow());

        properties.Items[CorrelationProperty] = correlationId;

        var cookieName = Options.CorrelationCookie.Name + correlationId;

        Response.Cookies.Append(cookieName, CorrelationMarker, cookieOptions);
    }

    /// <summary>
    /// Validates that the current request correlates with the current remote authentication request.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    protected virtual bool ValidateCorrelationId(AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (!properties.Items.TryGetValue(CorrelationProperty, out var correlationId))
        {
            Logger.CorrelationPropertyNotFound(Options.CorrelationCookie.Name!);
            return false;
        }

        properties.Items.Remove(CorrelationProperty);

        var cookieName = Options.CorrelationCookie.Name + correlationId;

        var correlationCookie = Request.Cookies[cookieName];
        if (string.IsNullOrEmpty(correlationCookie))
        {
            Logger.CorrelationCookieNotFound(cookieName);
            return false;
        }

        var cookieOptions = Options.CorrelationCookie.Build(Context, TimeProvider.GetUtcNow());

        Response.Cookies.Delete(cookieName, cookieOptions);

        if (!string.Equals(correlationCookie, CorrelationMarker, StringComparison.Ordinal))
        {
            Logger.UnexpectedCorrelationCookieValue(cookieName, correlationCookie);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Derived types may override this method to handle access denied errors.
    /// </summary>
    /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
    /// <returns>The <see cref="HandleRequestResult"/>.</returns>
    protected virtual async Task<HandleRequestResult> HandleAccessDeniedErrorAsync(AuthenticationProperties properties)
    {
        Logger.AccessDeniedError();
        var context = new AccessDeniedContext(Context, Scheme, Options)
        {
            AccessDeniedPath = Options.AccessDeniedPath,
            Properties = properties,
            ReturnUrl = properties?.RedirectUri,
            ReturnUrlParameter = Options.ReturnUrlParameter
        };
        await Events.AccessDenied(context);

        if (context.Result != null)
        {
            if (context.Result.Handled)
            {
                Logger.AccessDeniedContextHandled();
            }
            else if (context.Result.Skipped)
            {
                Logger.AccessDeniedContextSkipped();
            }

            return context.Result;
        }

        // If an access denied endpoint was specified, redirect the user agent.
        // Otherwise, invoke the RemoteFailure event for further processing.
        if (context.AccessDeniedPath.HasValue)
        {
            string uri = context.AccessDeniedPath;
            if (!string.IsNullOrEmpty(context.ReturnUrlParameter) && !string.IsNullOrEmpty(context.ReturnUrl))
            {
                uri = QueryHelpers.AddQueryString(uri, context.ReturnUrlParameter, context.ReturnUrl);
            }
            Response.Redirect(BuildRedirectUri(uri));

            return HandleRequestResult.Handle();
        }

        return HandleRequestResult.NoResult();
    }
}
