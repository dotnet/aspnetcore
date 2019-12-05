// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// A per-request authentication handler for the WsFederation.
    /// </summary>
    public class WsFederationHandler : RemoteAuthenticationHandler<WsFederationOptions>, IAuthenticationSignOutHandler
    {
        private const string CorrelationProperty = ".xsrf";
        private WsFederationConfiguration _configuration;

        /// <summary>
        /// Creates a new WsFederationAuthenticationHandler
        /// </summary>
        /// <param name="options"></param>
        /// <param name="encoder"></param>
        /// <param name="clock"></param>
        /// <param name="logger"></param>
        public WsFederationHandler(IOptionsMonitor<WsFederationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
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
            WsFederationMessage wsFederationMessage = null;
            AuthenticationProperties properties = null;

            // assumption: if the ContentType is "application/x-www-form-urlencoded" it should be safe to read as it is small.
            if (HttpMethods.IsPost(Request.Method)
              && !string.IsNullOrEmpty(Request.ContentType)
              // May have media/type; charset=utf-8, allow partial match.
              && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
              && Request.Body.CanRead)
            {
                var form = await Request.ReadFormAsync();

                wsFederationMessage = new WsFederationMessage(form.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value)));
            }

            if (wsFederationMessage == null || !wsFederationMessage.IsSignInMessage)
            {
                if (Options.SkipUnrecognizedRequests)
                {
                    // Not for us?
                    return HandleRequestResult.SkipHandler();
                }

                return HandleRequestResult.Fail("No message.");
            }

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
                        return HandleRequestResult.Fail("Unsolicited logins are not allowed.");
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
                properties = messageReceivedContext.Properties; // Provides a new instance if not set.

                // If state did flow from the challenge then validate it. See AllowUnsolicitedLogins above.
                if (properties.Items.TryGetValue(CorrelationProperty, out string correlationId)
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
                properties = messageReceivedContext.Properties;

                if (_configuration == null)
                {
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                // Copy and augment to avoid cross request race conditions for updated configurations.
                var tvp = Options.TokenValidationParameters.Clone();
                var issuers = new[] { _configuration.Issuer };
                tvp.ValidIssuers = (tvp.ValidIssuers == null ? issuers : tvp.ValidIssuers.Concat(issuers));
                tvp.IssuerSigningKeys = (tvp.IssuerSigningKeys == null ? _configuration.SigningKeys : tvp.IssuerSigningKeys.Concat(_configuration.SigningKeys));

                ClaimsPrincipal principal = null;
                SecurityToken parsedToken = null;
                foreach (var validator in Options.SecurityTokenHandlers)
                {
                    if (validator.CanReadToken(token))
                    {
                        principal = validator.ValidateToken(token, tvp, out parsedToken);
                        break;
                    }
                }

                if (principal == null)
                {
                    throw new SecurityTokenException(Resources.Exception_NoTokenValidatorFound);
                }

                if (Options.UseTokenLifetime && parsedToken != null)
                {
                    // Override any session persistence to match the token lifetime.
                    var issued = parsedToken.ValidFrom;
                    if (issued != DateTime.MinValue)
                    {
                        properties.IssuedUtc = issued.ToUniversalTime();
                    }
                    var expires = parsedToken.ValidTo;
                    if (expires != DateTime.MinValue)
                    {
                        properties.ExpiresUtc = expires.ToUniversalTime();
                    }
                    properties.AllowRefresh = false;
                }

                var securityTokenValidatedContext = new SecurityTokenValidatedContext(Context, Scheme, Options, principal, properties)
                {
                    ProtocolMessage = wsFederationMessage,
                    SecurityToken = parsedToken,
                };

                await Events.SecurityTokenValidated(securityTokenValidatedContext);
                if (securityTokenValidatedContext.Result != null)
                {
                    return securityTokenValidatedContext.Result;
                }

                // Flow possible changes
                principal = securityTokenValidatedContext.Principal;
                properties = securityTokenValidatedContext.Properties;

                return HandleRequestResult.Success(new AuthenticationTicket(principal, properties, Scheme.Name));
            }
            catch (Exception exception)
            {
                Logger.ExceptionProcessingMessage(exception);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the notification.
                if (Options.RefreshOnIssuerKeyNotFound && exception.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    Options.ConfigurationManager.RequestRefresh();
                }

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

        /// <summary>
        /// Handles Signout
        /// </summary>
        /// <returns></returns>
        public async virtual Task SignOutAsync(AuthenticationProperties properties)
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
            var message = new WsFederationMessage(Request.Query.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value)));
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

            if (!uri.StartsWith("/", StringComparison.Ordinal))
            {
                return uri;
            }

            return BuildRedirectUri(uri);
        }
    }
}
