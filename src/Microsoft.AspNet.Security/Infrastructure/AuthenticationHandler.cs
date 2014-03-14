// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Logging;
using Microsoft.AspNet.Security.DataHandler.Encoder;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Security.Infrastructure
{
    /// <summary>
    /// Base class for the per-request work performed by most authentication middleware.
    /// </summary>
    public abstract class AuthenticationHandler
    {
        private static readonly RNGCryptoServiceProvider Random = new RNGCryptoServiceProvider();

        private object _registration;

        private Task<AuthenticationTicket> _authenticate;
        private bool _authenticateInitialized;
        private object _authenticateSyncLock;

        private Task _applyResponse;
        private bool _applyResponseInitialized;
        private object _applyResponseSyncLock;

        private AuthenticationOptions _baseOptions;

        protected HttpContext Context { get; private set; }

        protected HttpRequest Request
        {
            get { return Context.Request; }
        }

        protected HttpResponse Response
        {
            get { return Context.Response; }
        }

        protected PathString RequestPathBase { get; private set; }
        protected SecurityHelper Helper { get; private set; }

        internal AuthenticationOptions BaseOptions
        {
            get { return _baseOptions; }
        }

        protected async Task BaseInitializeAsync(AuthenticationOptions options, HttpContext context)
        {
            _baseOptions = options;
            Context = context;
            Helper = new SecurityHelper(context);
            RequestPathBase = Request.PathBase;

            _registration = Request.RegisterAuthenticationHandler(this);

            Response.OnSendingHeaders(OnSendingHeaderCallback, this);

            await InitializeCoreAsync();

            if (BaseOptions.AuthenticationMode == AuthenticationMode.Active)
            {
                AuthenticationTicket ticket = await AuthenticateAsync();
                if (ticket != null && ticket.Identity != null)
                {
                    Helper.AddUserIdentity(ticket.Identity);
                }
            }
        }

        private static void OnSendingHeaderCallback(object state)
        {
            AuthenticationHandler handler = (AuthenticationHandler)state;
            handler.ApplyResponseAsync().Wait();
        }

        protected virtual Task InitializeCoreAsync()
        {
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Called once per request after Initialize and Invoke. 
        /// </summary>
        /// <returns>async completion</returns>
        internal async Task TeardownAsync()
        {
            await ApplyResponseAsync();
            await TeardownCoreAsync();
            Request.UnregisterAuthenticationHandler(_registration);
        }

        protected virtual Task TeardownCoreAsync()
        {
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Called once by common code after initialization. If an authentication middleware responds directly to
        /// specifically known paths it must override this virtual, compare the request path to it's known paths, 
        /// provide any response information as appropriate, and true to stop further processing.
        /// </summary>
        /// <returns>Returning false will cause the common code to call the next middleware in line. Returning true will
        /// cause the common code to begin the async completion journey without calling the rest of the middleware
        /// pipeline.</returns>
        public virtual Task<bool> InvokeAsync()
        {
            return Task.FromResult<bool>(false);
        }

        /// <summary>
        /// Causes the authentication logic in AuthenticateCore to be performed for the current request 
        /// at most once and returns the results. Calling Authenticate more than once will always return 
        /// the original value. 
        /// 
        /// This method should always be called instead of calling AuthenticateCore directly.
        /// </summary>
        /// <returns>The ticket data provided by the authentication logic</returns>
        public Task<AuthenticationTicket> AuthenticateAsync()
        {
            return LazyInitializer.EnsureInitialized(
                ref _authenticate,
                ref _authenticateInitialized,
                ref _authenticateSyncLock,
                AuthenticateCoreAsync);
        }

        /// <summary>
        /// The core authentication logic which must be provided by the handler. Will be invoked at most
        /// once per request. Do not call directly, call the wrapping Authenticate method instead.
        /// </summary>
        /// <returns>The ticket data provided by the authentication logic</returns>
        protected abstract Task<AuthenticationTicket> AuthenticateCoreAsync();

        /// <summary>
        /// Causes the ApplyResponseCore to be invoked at most once per request. This method will be
        /// invoked either earlier, when the response headers are sent as a result of a response write or flush,
        /// or later, as the last step when the original async call to the middleware is returning.
        /// </summary>
        /// <returns></returns>
        private Task ApplyResponseAsync()
        {
            return LazyInitializer.EnsureInitialized(
                ref _applyResponse,
                ref _applyResponseInitialized,
                ref _applyResponseSyncLock,
                ApplyResponseCoreAsync);
        }

        /// <summary>
        /// Core method that may be overridden by handler. The default behavior is to call two common response 
        /// activities, one that deals with sign-in/sign-out concerns, and a second to deal with 401 challenges.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task ApplyResponseCoreAsync()
        {
            await ApplyResponseGrantAsync();
            await ApplyResponseChallengeAsync();
        }

        /// <summary>
        /// Override this method to dela with sign-in/sign-out concerns, if an authentication scheme in question
        /// deals with grant/revoke as part of it's request flow. (like setting/deleting cookies)
        /// </summary>
        /// <returns></returns>
        protected virtual Task ApplyResponseGrantAsync()
        {
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Override this method to dela with 401 challenge concerns, if an authentication scheme in question
        /// deals an authentication interaction as part of it's request flow. (like adding a response header, or
        /// changing the 401 result to 302 of a login page or external sign-in location.)
        /// </summary>
        /// <returns></returns>
        protected virtual Task ApplyResponseChallengeAsync()
        {
            return Task.FromResult<object>(null);
        }

        protected void GenerateCorrelationId(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }

            string correlationKey = Constants.CorrelationPrefix + BaseOptions.AuthenticationType;

            var nonceBytes = new byte[32];
            Random.GetBytes(nonceBytes);
            string correlationId = TextEncodings.Base64Url.Encode(nonceBytes);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsSecure
            };

            properties.Dictionary[correlationKey] = correlationId;

            Response.Cookies.Append(correlationKey, correlationId, cookieOptions);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            MessageId = "Microsoft.Owin.Logging.LoggerExtensions.WriteWarning(Microsoft.Owin.Logging.ILogger,System.String,System.String[])",
            Justification = "Logging is not Localized")]
        protected bool ValidateCorrelationId(AuthenticationProperties properties, ILogger logger)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }

            string correlationKey = Constants.CorrelationPrefix + BaseOptions.AuthenticationType;

            string correlationCookie = Request.Cookies[correlationKey];
            if (string.IsNullOrWhiteSpace(correlationCookie))
            {
                logger.WriteWarning("{0} cookie not found.", correlationKey);
                return false;
            }

            Response.Cookies.Delete(correlationKey);

            string correlationExtra;
            if (!properties.Dictionary.TryGetValue(
                correlationKey,
                out correlationExtra))
            {
                logger.WriteWarning("{0} state property not found.", correlationKey);
                return false;
            }

            properties.Dictionary.Remove(correlationKey);

            if (!string.Equals(correlationCookie, correlationExtra, StringComparison.Ordinal))
            {
                logger.WriteWarning("{0} correlation cookie and state property mismatch.", correlationKey);
                return false;
            }

            return true;
        }
    }
}
