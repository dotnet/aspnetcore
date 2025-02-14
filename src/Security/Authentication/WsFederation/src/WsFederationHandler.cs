// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// A per-request authentication handler for the WsFederation.
/// </summary>
public class WsFederationHandler : RemoteAuthenticationHandler<WsFederationOptions>, IAuthenticationSignOutHandler
{
    private const string CorrelationProperty = ".xsrf";
    private WsFederationConfiguration? _configuration;

    /// <summary>
    /// Creates a new WsFederationAuthenticationHandler
    /// </summary>
    /// <param name="options"></param>
    /// <param name="encoder"></param>
    /// <param name="clock"></param>
    /// <param name="logger"></param>
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public WsFederationHandler(IOptionsMonitor<WsFederationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    /// <summary>
    /// Creates a new WsFederationAuthenticationHandler
    /// </summary>
    /// <param name="options"></param>
    /// <param name="encoder"></param>
    /// <param name="logger"></param>
    public WsFederationHandler(IOptionsMonitor<WsFederationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
    /// If it is not provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    protected new WsFederationEvents Events
    {
        get { return (WsFederationEvents)base.Events; }
        set { base.Events = value; }
    }

    /// <summary>
    /// Creates a new instance of the events instance.
    /// </summary>
    /// <returns>A new instance of the events instance.</returns>
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new WsFederationEvents());

    /// <summary>
    /// Overridden to handle remote signout requests
    /// </summary>
    /// <returns><see langword="true" /> if request processing should stop.</returns>
    public override Task<bool> HandleRequestAsync()
    {
        // RemoteSignOutPath and CallbackPath may be the same, fall through if the message doesn't match.
        if (Options.RemoteSignOutPath.HasValue && Options.RemoteSignOutPath == Request.Path && HttpMethods.IsGet(Request.Method)
            && string.Equals(Request.Query[WsFederationConstants.WsFederationParameterNames.Wa],
                WsFederationConstants.WsFederationActions.SignOutCleanup, StringComparison.OrdinalIgnoreCase))
        {
            // We've received a remote sign-out request
            return HandleRemoteSignOutAsync();
        }

        return base.HandleRequestAsync();
    }

    /// <summary>
    /// Handles Challenge
    /// </summary>
    /// <returns></returns>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (_configuration == null)
        {
            _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
        }

        // Save the original challenge URI so we can redirect back to it when we're done.
        if (string.IsNullOrEmpty(properties.RedirectUri))
        {
            properties.RedirectUri = OriginalPathBase + OriginalPath + Request.QueryString;
        }

        var wsFederationMessage = new WsFederationMessage()
        {
            IssuerAddress = _configuration.TokenEndpoint ?? string.Empty,
            Wtrealm = Options.Wtrealm,
            Wa = WsFederationConstants.WsFederationActions.SignIn,
        };

        if (!string.IsNullOrEmpty(Options.Wreply))
        {
            wsFederationMessage.Wreply = Options.Wreply;
        }
        else
        {
            wsFederationMessage.Wreply = BuildRedirectUri(Options.CallbackPath);
        }

        GenerateCorrelationId(properties);

        var redirectContext = new RedirectContext(Context, Scheme, Options, properties)
        {
            ProtocolMessage = wsFederationMessage
        };
        await Events.RedirectToIdentityProvider(redirectContext);

        if (redirectContext.Handled)
        {
            return;
        }

        wsFederationMessage = redirectContext.ProtocolMessage;

        if (!string.IsNullOrEmpty(wsFederationMessage.Wctx))
        {
            properties.Items[WsFederationDefaults.UserstatePropertiesKey] = wsFederationMessage.Wctx;
        }

        wsFederationMessage.Wctx = Uri.EscapeDataString(Options.StateDataFormat.Protect(properties));

        var redirectUri = wsFederationMessage.CreateSignInUrl();
        if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
        {
            Logger.MalformedRedirectUri(redirectUri);
        }
        Response.Redirect(redirectUri);
    }

    /// <summary>
    /// Invoked to process incoming authentication messages.
    /// </summary>
    /// <returns></returns>
    protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        WsFederationMessage? wsFederationMessage = null;
        AuthenticationProperties? properties = null;

        // assumption: if the ContentType is "application/x-www-form-urlencoded" it should be safe to read as it is small.
        if (HttpMethods.IsPost(Request.Method)
          && !string.IsNullOrEmpty(Request.ContentType)
          // May have media/type; charset=utf-8, allow partial match.
          && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
          && Request.Body.CanRead)
        {
            var form = await Request.ReadFormAsync(Context.RequestAborted);

            // ToArray handles the StringValues.IsNullOrEmpty case. We assume non-empty Value does not contain null elements.
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            wsFederationMessage = new WsFederationMessage(form.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value.ToArray())));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        }

        if (wsFederationMessage == null || !wsFederationMessage.IsSignInMessage)
        {
            if (Options.SkipUnrecognizedRequests)
            {
                // Not for us?
                return HandleRequestResult.SkipHandler();
            }

            return HandleRequestResults.NoMessage;
        }

        List<Exception>? validationFailures = null;
        try
        {
            // Retrieve our cached redirect uri
            var state = wsFederationMessage.Wctx;
            // WsFed allows for uninitiated logins, state may be missing. See AllowUnsolicitedLogins.
            properties = Options.StateDataFormat.Unprotect(state);

            if (properties == null)
            {
                if (!Options.AllowUnsolicitedLogins)
                {
                    return HandleRequestResults.UnsolicitedLoginsNotAllowed;
                }
            }
            else
            {
                // Extract the user state from properties and reset.
                properties.Items.TryGetValue(WsFederationDefaults.UserstatePropertiesKey, out var userState);
                wsFederationMessage.Wctx = userState;
            }

            var messageReceivedContext = new MessageReceivedContext(Context, Scheme, Options, properties)
            {
                ProtocolMessage = wsFederationMessage
            };
            await Events.MessageReceived(messageReceivedContext);
            if (messageReceivedContext.Result != null)
            {
                return messageReceivedContext.Result;
            }
            wsFederationMessage = messageReceivedContext.ProtocolMessage;
            properties = messageReceivedContext.Properties!; // Provides a new instance if not set.

            // If state did flow from the challenge then validate it. See AllowUnsolicitedLogins above.
            if (properties.Items.TryGetValue(CorrelationProperty, out string? correlationId)
                && !ValidateCorrelationId(properties))
            {
                return HandleRequestResult.Fail("Correlation failed.", properties);
            }

            if (wsFederationMessage.Wresult == null)
            {
                Logger.SignInWithoutWResult();
                return HandleRequestResult.Fail(Resources.SignInMessageWresultIsMissing, properties);
            }

            var token = wsFederationMessage.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                Logger.SignInWithoutToken();
                return HandleRequestResult.Fail(Resources.SignInMessageTokenIsMissing, properties);
            }

            var securityTokenReceivedContext = new SecurityTokenReceivedContext(Context, Scheme, Options, properties)
            {
                ProtocolMessage = wsFederationMessage
            };
            await Events.SecurityTokenReceived(securityTokenReceivedContext);
            if (securityTokenReceivedContext.Result != null)
            {
                return securityTokenReceivedContext.Result;
            }
            wsFederationMessage = securityTokenReceivedContext.ProtocolMessage;
            properties = messageReceivedContext.Properties!;

            var tvp = await SetupTokenValidationParametersAsync();
            ClaimsPrincipal? principal = null;
            SecurityToken? validatedToken = null;
            if (!Options.UseSecurityTokenHandlers)
            {
                foreach (var tokenHandler in Options.TokenHandlers)
                {
                    try
                    {
                        var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, tvp);
                        if (tokenValidationResult.IsValid)
                        {
                            principal = new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
                            validatedToken = tokenValidationResult.SecurityToken;
                            break;
                        }
                        else
                        {
                            validationFailures ??= new List<Exception>(1);
                            Exception exception = tokenValidationResult.Exception ?? new SecurityTokenValidationException($"The TokenHandler: '{tokenHandler}', was unable to validate the Token.");
                            validationFailures.Add(exception);
                            RequestRefresh(exception);
                        }
                    }
                    catch (Exception ex)
                    {
                        validationFailures ??= new List<Exception>(1);
                        validationFailures.Add(new SecurityTokenValidationException($"TokenHandler: '{tokenHandler}', threw an exception (see inner exception).", ex));
                        RequestRefresh(ex);
                    }
                }
            }
            else
            {

#pragma warning disable CS0618 // Type or member is obsolete
                foreach (var validator in Options.SecurityTokenHandlers)
                {
                    if (validator.CanReadToken(token))
                    {
                        try
                        {
                            principal = validator.ValidateToken(token, tvp, out validatedToken);
                        }
                        catch (Exception ex)
                        {
                            validationFailures ??= new List<Exception>(1);
                            validationFailures.Add(ex);
                            continue;
                        }
                        break;
                    }
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (principal == null)
            {
                if (validationFailures == null || validationFailures.Count == 0)
                {
                    throw new SecurityTokenException(Resources.Exception_NoTokenValidatorFound);
                }
                else if (validationFailures.Count == 1)
                {
                    throw new SecurityTokenException(Resources.Exception_NoTokenValidatorFound, validationFailures[0]);
                }
                else
                {
                    throw new SecurityTokenException(Resources.Exception_NoTokenValidatorFound, new AggregateException(validationFailures));
                }
            }

            if (Options.UseTokenLifetime && validatedToken != null)
            {
                // Override any session persistence to match the token lifetime.
                var issued = validatedToken.ValidFrom;
                if (issued != DateTime.MinValue)
                {
                    properties.IssuedUtc = issued.ToUniversalTime();
                }
                var expires = validatedToken.ValidTo;
                if (expires != DateTime.MinValue)
                {
                    properties.ExpiresUtc = expires.ToUniversalTime();
                }
                properties.AllowRefresh = false;
            }

            var securityTokenValidatedContext = new SecurityTokenValidatedContext(Context, Scheme, Options, principal, properties)
            {
                ProtocolMessage = wsFederationMessage,
                SecurityToken = validatedToken,
            };

            await Events.SecurityTokenValidated(securityTokenValidatedContext);
            if (securityTokenValidatedContext.Result != null)
            {
                return securityTokenValidatedContext.Result;
            }

            // Flow possible changes
            principal = securityTokenValidatedContext.Principal!;
            properties = securityTokenValidatedContext.Properties;

            return HandleRequestResult.Success(new AuthenticationTicket(principal, properties, Scheme.Name));
        }
        catch (Exception exception)
        {
            Logger.ExceptionProcessingMessage(exception);

            RequestRefresh(exception);
            var authenticationFailedContext = new AuthenticationFailedContext(Context, Scheme, Options)
            {
                ProtocolMessage = wsFederationMessage,
                Exception = exception
            };

            await Events.AuthenticationFailed(authenticationFailedContext);
            if (authenticationFailedContext.Result != null)
            {
                return authenticationFailedContext.Result;
            }

            return HandleRequestResult.Fail(exception, properties);
        }
    }

    private async Task<TokenValidationParameters> SetupTokenValidationParametersAsync()
    {
        // Clone to avoid cross request race conditions for updated configurations.
        var tokenValidationParameters = Options.TokenValidationParameters.Clone();

        if (Options.ConfigurationManager is BaseConfigurationManager baseConfigurationManager)
        {
            tokenValidationParameters.ConfigurationManager = baseConfigurationManager;
        }
        else
        {
            if (Options.ConfigurationManager != null)
            {
                // GetConfigurationAsync has a time interval that must pass before new http request will be issued.
                _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);

                var issuers = new[] { _configuration.Issuer };
                tokenValidationParameters.ValidIssuers = (tokenValidationParameters.ValidIssuers == null ? issuers : tokenValidationParameters.ValidIssuers.Concat(issuers));
                tokenValidationParameters.IssuerSigningKeys = (tokenValidationParameters.IssuerSigningKeys == null ? _configuration.SigningKeys : tokenValidationParameters.IssuerSigningKeys.Concat(_configuration.SigningKeys));
            }
        }

        return tokenValidationParameters;
    }

    private void RequestRefresh(Exception exception)
    {
        // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the notification.
        // Refreshing on SecurityTokenSignatureKeyNotFound may be redundant if Last-Known-Good is enabled, it won't do much harm, most likely will be a nop.
        if (Options.RefreshOnIssuerKeyNotFound && exception is SecurityTokenSignatureKeyNotFoundException)
        {
            Options.ConfigurationManager.RequestRefresh();
        }
    }

    /// <summary>
    /// Handles Signout
    /// </summary>
    /// <returns></returns>
    public virtual async Task SignOutAsync(AuthenticationProperties? properties)
    {
        var target = ResolveTarget(Options.ForwardSignOut);
        if (target != null)
        {
            await Context.SignOutAsync(target, properties);
            return;
        }

        if (_configuration == null)
        {
            _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
        }

        var wsFederationMessage = new WsFederationMessage()
        {
            IssuerAddress = _configuration.TokenEndpoint ?? string.Empty,
            Wtrealm = Options.Wtrealm,
            Wa = WsFederationConstants.WsFederationActions.SignOut,
        };

        // Set Wreply in order:
        // 1. properties.Redirect
        // 2. Options.SignOutWreply
        // 3. Options.Wreply
        if (properties != null && !string.IsNullOrEmpty(properties.RedirectUri))
        {
            wsFederationMessage.Wreply = BuildRedirectUriIfRelative(properties.RedirectUri);
        }
        else if (!string.IsNullOrEmpty(Options.SignOutWreply))
        {
            wsFederationMessage.Wreply = BuildRedirectUriIfRelative(Options.SignOutWreply);
        }
        else if (!string.IsNullOrEmpty(Options.Wreply))
        {
            wsFederationMessage.Wreply = BuildRedirectUriIfRelative(Options.Wreply);
        }

        var redirectContext = new RedirectContext(Context, Scheme, Options, properties)
        {
            ProtocolMessage = wsFederationMessage
        };
        await Events.RedirectToIdentityProvider(redirectContext);

        if (!redirectContext.Handled)
        {
            var redirectUri = redirectContext.ProtocolMessage.CreateSignOutUrl();
            if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
            {
                Logger.MalformedRedirectUri(redirectUri);
            }
            Response.Redirect(redirectUri);
        }
    }

    /// <summary>
    /// Handles wsignoutcleanup1.0 messages sent to the RemoteSignOutPath
    /// </summary>
    /// <returns></returns>
    protected virtual async Task<bool> HandleRemoteSignOutAsync()
    {
        // ToArray handles the StringValues.IsNullOrEmpty case. We assume non-empty Value does not contain null elements.
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        var message = new WsFederationMessage(Request.Query.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value.ToArray())));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        var remoteSignOutContext = new RemoteSignOutContext(Context, Scheme, Options, message);
        await Events.RemoteSignOut(remoteSignOutContext);

        if (remoteSignOutContext.Result != null)
        {
            if (remoteSignOutContext.Result.Handled)
            {
                Logger.RemoteSignOutHandledResponse();
                return true;
            }
            if (remoteSignOutContext.Result.Skipped)
            {
                Logger.RemoteSignOutSkipped();
                return false;
            }
        }

        Logger.RemoteSignOut();

        await Context.SignOutAsync(Options.SignOutScheme);
        return true;
    }

    /// <summary>
    /// Build a redirect path if the given path is a relative path.
    /// </summary>
    private string BuildRedirectUriIfRelative(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return uri;
        }

        if (!uri.StartsWith('/'))
        {
            return uri;
        }

        return BuildRedirectUri(uri);
    }
}
