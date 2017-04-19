// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    public abstract class RemoteAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationRequestHandler 
        where TOptions : RemoteAuthenticationOptions, new()
    {
        private const string CorrelationPrefix = ".AspNetCore.Correlation.";
        private const string CorrelationProperty = ".xsrf";
        private const string CorrelationMarker = "N";
        private const string AuthSchemeKey = ".AuthScheme";

        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();

        protected string SignInScheme => Options.SignInScheme;

        protected IDataProtectionProvider DataProtection { get; set; }

        private readonly AuthenticationOptions _authOptions;

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring. 
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new RemoteAuthenticationEvents Events
        {
            get { return (RemoteAuthenticationEvents)base.Events; }
            set { base.Events = value; }
        }

        protected RemoteAuthenticationHandler(IOptions<AuthenticationOptions> sharedOptions, IOptionsSnapshot<TOptions> options, IDataProtectionProvider dataProtection, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _authOptions = sharedOptions.Value;
            DataProtection = dataProtection;
        }

        protected override Task InitializeHandlerAsync()
        {
            DataProtection = Options.DataProtectionProvider ?? DataProtection;
            return TaskCache.CompletedTask;
        }

        protected override Task<object> CreateEventsAsync()
        {
            return Task.FromResult<object>(new RemoteAuthenticationEvents());
        }

        protected override void InitializeOptions()
        {
            base.InitializeOptions();

            if (Options.SignInScheme == null)
            {
                Options.SignInScheme = _authOptions.DefaultSignInScheme;
            }
        }

        public virtual Task<bool> ShouldHandleRequestAsync()
        {
            return Task.FromResult(Options.CallbackPath == Request.Path);
        }

        public virtual async Task<bool> HandleRequestAsync()
        {
            if (!await ShouldHandleRequestAsync())
            {
                return false;
            }

            AuthenticationTicket ticket = null;
            Exception exception = null;
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
                else if (authResult.Nothing)
                {
                    return false;
                }
                else if (!authResult.Succeeded)
                {
                    exception = authResult.Failure ??
                                new InvalidOperationException("Invalid return state, unable to redirect.");
                }

                ticket = authResult.Ticket;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                Logger.RemoteAuthenticationError(exception.Message);
                var errorContext = new FailureContext(Context, exception);
                await Events.RemoteFailure(errorContext);

                if (errorContext.HandledResponse)
                {
                    return true;
                }
                else if (errorContext.Skipped)
                {
                    return false;
                }

                throw new AggregateException("Unhandled remote failure.", exception);
            }

            // We have a ticket if we get here
            var ticketContext = new TicketReceivedContext(Context, Options, ticket)
            {
                ReturnUri = ticket.Properties.RedirectUri,
            };
            // REVIEW: is this safe or good?
            ticket.Properties.RedirectUri = null;

            // Mark which provider produced this identity so we can cross-check later in HandleAuthenticateAsync
            ticketContext.Properties.Items[AuthSchemeKey] = Scheme.Name;

            await Events.TicketReceived(ticketContext);

            if (ticketContext.HandledResponse)
            {
                Logger.SigninHandled();
                return true;
            }
            else if (ticketContext.Skipped)
            {
                Logger.SigninSkipped();
                return false;
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
        protected abstract Task<AuthenticateResult> HandleRemoteAuthenticateAsync();

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

        protected override Task HandleSignOutAsync(SignOutContext context)
        {
            throw new NotSupportedException();
        }

        protected override Task HandleSignInAsync(SignInContext context)
        {
            throw new NotSupportedException();
        }

        // REVIEW: This behaviour needs a test (forwarding of forbidden to sign in scheme)
        protected override Task HandleForbiddenAsync(ChallengeContext context)
        {
            return Context.ForbidAsync(SignInScheme);
        }

        protected virtual void GenerateCorrelationId(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var bytes = new byte[32];
            CryptoRandom.GetBytes(bytes);
            var correlationId = Base64UrlTextEncoder.Encode(bytes);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                Expires = Clock.UtcNow.Add(Options.RemoteAuthenticationTimeout),
            };

            properties.Items[CorrelationProperty] = correlationId;

            var cookieName = CorrelationPrefix + Scheme.Name + "." + correlationId;

            Response.Cookies.Append(cookieName, CorrelationMarker, cookieOptions);
        }

        protected virtual bool ValidateCorrelationId(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            string correlationId;
            if (!properties.Items.TryGetValue(CorrelationProperty, out correlationId))
            {
                Logger.CorrelationPropertyNotFound(CorrelationPrefix);
                return false;
            }

            properties.Items.Remove(CorrelationProperty);

            var cookieName = CorrelationPrefix + Scheme.Name + "." + correlationId;

            var correlationCookie = Request.Cookies[cookieName];
            if (string.IsNullOrEmpty(correlationCookie))
            {
                Logger.CorrelationCookieNotFound(cookieName);
                return false;
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };
            Response.Cookies.Delete(cookieName, cookieOptions);

            if (!string.Equals(correlationCookie, CorrelationMarker, StringComparison.Ordinal))
            {
                Logger.UnexpectedCorrelationCookieValue(cookieName, correlationCookie);
                return false;
            }

            return true;
        }
    }
}