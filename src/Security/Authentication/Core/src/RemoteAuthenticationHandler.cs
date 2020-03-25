// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    public abstract class RemoteAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationRequestHandler
        where TOptions : RemoteAuthenticationOptions, new()
    {
        private const string CorrelationProperty = ".xsrf";
        private const string CorrelationMarker = "N";
        private const string AuthSchemeKey = ".AuthScheme";

        protected string SignInScheme => Options.SignInScheme;

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new RemoteAuthenticationEvents Events
        {
            get { return (RemoteAuthenticationEvents)base.Events; }
            set { base.Events = value; }
        }

        protected RemoteAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<object> CreateEventsAsync()
            => Task.FromResult<object>(new RemoteAuthenticationEvents());

        public virtual Task<bool> ShouldHandleRequestAsync()
            => Task.FromResult(Options.CallbackPath == Request.Path);

        public virtual async Task<bool> HandleRequestAsync()
        {
            if (!await ShouldHandleRequestAsync())
            {
                return false;
            }

            AuthenticationTicket ticket = null;
            Exception exception = null;
            AuthenticationProperties properties = null;
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
                        throw new Exception("An error was returned from the RemoteFailure event.", errorContext.Result.Failure);
                    }
                }

                if (errorContext.Failure != null)
                {
                    throw new Exception("An error was encountered while handling the remote login.", errorContext.Failure);
                }
            }

            // We have a ticket if we get here
            var ticketContext = new TicketReceivedContext(Context, Scheme, Options, ticket)
            {
                ReturnUri = ticket.Properties.RedirectUri
            };

            ticket.Properties.RedirectUri = null;

            // Mark which provider produced this identity so we can cross-check later in HandleAuthenticateAsync
            ticketContext.Properties.Items[AuthSchemeKey] = Scheme.Name;

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

            await Context.SignInAsync(SignInScheme, ticketContext.Principal, ticketContext.Properties);

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
                string authenticatedScheme;
                var ticket = result.Ticket;
                if (ticket != null && ticket.Principal != null && ticket.Properties != null
                    && ticket.Properties.Items.TryGetValue(AuthSchemeKey, out authenticatedScheme)
                    && string.Equals(Scheme.Name, authenticatedScheme, StringComparison.Ordinal))
                {
                    return AuthenticateResult.Success(new AuthenticationTicket(ticket.Principal,
                        ticket.Properties, Scheme.Name));
                }

                return AuthenticateResult.Fail("Not authenticated");
            }

            return AuthenticateResult.Fail("Remote authentication does not directly support AuthenticateAsync");
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
            => Context.ForbidAsync(SignInScheme);

        protected virtual void GenerateCorrelationId(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var correlationId = Base64UrlTextEncoder.Encode(bytes);

            var cookieOptions = Options.CorrelationCookie.Build(Context, Clock.UtcNow);

            properties.Items[CorrelationProperty] = correlationId;

            var cookieName = Options.CorrelationCookie.Name + Scheme.Name + "." + correlationId;

            Response.Cookies.Append(cookieName, CorrelationMarker, cookieOptions);
        }

        protected virtual bool ValidateCorrelationId(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (!properties.Items.TryGetValue(CorrelationProperty, out string correlationId))
            {
                Logger.CorrelationPropertyNotFound(Options.CorrelationCookie.Name);
                return false;
            }

            properties.Items.Remove(CorrelationProperty);

            var cookieName = Options.CorrelationCookie.Name + Scheme.Name + "." + correlationId;

            var correlationCookie = Request.Cookies[cookieName];
            if (string.IsNullOrEmpty(correlationCookie))
            {
                Logger.CorrelationCookieNotFound(cookieName);
                return false;
            }

            var cookieOptions = Options.CorrelationCookie.Build(Context, Clock.UtcNow);

            Response.Cookies.Delete(cookieName, cookieOptions);

            if (!string.Equals(correlationCookie, CorrelationMarker, StringComparison.Ordinal))
            {
                Logger.UnexpectedCorrelationCookieValue(cookieName, correlationCookie);
                return false;
            }

            return true;
        }

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
                Response.Redirect(uri);

                return HandleRequestResult.Handle();
            }

            return HandleRequestResult.NoResult();
        }
    }
}
