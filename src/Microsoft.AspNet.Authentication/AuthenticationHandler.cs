// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.DataHandler.Encoder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Base class for the per-request work performed by most authentication middleware.
    /// </summary>
    public abstract class AuthenticationHandler : IAuthenticationHandler
    {
        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();

        private Task<AuthenticationTicket> _authenticate;
        private bool _authenticateInitialized;
        private object _authenticateSyncLock;

        private Task _applyResponse;
        private bool _applyResponseInitialized;
        private object _applyResponseSyncLock;

        private AuthenticationOptions _baseOptions;

        protected ChallengeContext ChallengeContext { get; set; }
        protected SignInContext SignInContext { get; set; }
        protected SignOutContext SignOutContext { get; set; }

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

        protected ILogger Logger { get; private set; }

        protected IUrlEncoder UrlEncoder { get; private set; }

        internal AuthenticationOptions BaseOptions
        {
            get { return _baseOptions; }
        }

        internal bool AuthenticateCalled { get; set; }

        public IAuthenticationHandler PriorHandler { get; set; }

        public bool Faulted { get; set; }

        protected async Task BaseInitializeAsync([NotNull] AuthenticationOptions options, [NotNull] HttpContext context, [NotNull] ILogger logger, [NotNull] IUrlEncoder encoder)
        {
            _baseOptions = options;
            Context = context;
            RequestPathBase = Request.PathBase;
            Logger = logger;
            UrlEncoder = encoder;

            RegisterAuthenticationHandler();

            Response.OnSendingHeaders(OnSendingHeaderCallback, this);

            await InitializeCoreAsync();

            if (BaseOptions.AutomaticAuthentication)
            {
                var ticket = await AuthenticateAsync();
                if (ticket?.Principal != null)
                {
                    SecurityHelper.AddUserPrincipal(Context, ticket.Principal);
                }
            }
        }

        private static void OnSendingHeaderCallback(object state)
        {
            AuthenticationHandler handler = (AuthenticationHandler)state;
            handler.ApplyResponse();
        }

        protected virtual Task InitializeCoreAsync()
        {
            return Task.FromResult(0);
        }

        /// <summary>
        /// Called once per request after Initialize and Invoke.
        /// </summary>
        /// <returns>async completion</returns>
        internal async Task TeardownAsync()
        {
            try
            {
                await ApplyResponseAsync();
            }
            catch (Exception)
            {
                try
                {
                    await TeardownCoreAsync();
                }
                catch (Exception)
                {
                    // Don't mask the original exception
                }
                UnregisterAuthenticationHandler();
                throw;
            }

            await TeardownCoreAsync();
            UnregisterAuthenticationHandler();
        }

        protected virtual Task TeardownCoreAsync()
        {
            return Task.FromResult(0);
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
            return Task.FromResult(false);
        }

        public virtual void GetDescriptions(DescribeSchemesContext describeContext)
        {
            describeContext.Accept(BaseOptions.Description.Items);

            if (PriorHandler != null)
            {
                PriorHandler.GetDescriptions(describeContext);
            }
        }

        public virtual void Authenticate(AuthenticateContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                var ticket = Authenticate();
                if (ticket?.Principal != null)
                {
                    AuthenticateCalled = true;
                    context.Authenticated(ticket.Principal, ticket.Properties.Items, BaseOptions.Description.Items);
                }
                else
                {
                    context.NotAuthenticated();
                }
            }

            if (PriorHandler != null)
            {
                PriorHandler.Authenticate(context);
            }
        }

        public virtual async Task AuthenticateAsync(AuthenticateContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                var ticket = await AuthenticateAsync();
                if (ticket?.Principal != null)
                {
                    AuthenticateCalled = true;
                    context.Authenticated(ticket.Principal, ticket.Properties.Items, BaseOptions.Description.Items);
                }
                else
                {
                    context.NotAuthenticated();
                }
            }

            if (PriorHandler != null)
            {
                await PriorHandler.AuthenticateAsync(context);
            }
        }

        public AuthenticationTicket Authenticate()
        {
            return LazyInitializer.EnsureInitialized(
                ref _authenticate,
                ref _authenticateInitialized,
                ref _authenticateSyncLock,
                () =>
                {
                    return Task.FromResult(AuthenticateCore());
                }).GetAwaiter().GetResult();
        }

        protected abstract AuthenticationTicket AuthenticateCore();

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
        protected virtual Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            return Task.FromResult(AuthenticateCore());
        }

        private void ApplyResponse()
        {
            // If ApplyResponse already failed in the OnSendingHeaderCallback or TeardownAsync code path then a
            // failed task is cached. If called again the same error will be re-thrown. This breaks error handling
            // scenarios like the ability to display the error page or re-execute the request.
            try
            {
                if (!Faulted)
                {
                    LazyInitializer.EnsureInitialized(
                        ref _applyResponse,
                        ref _applyResponseInitialized,
                        ref _applyResponseSyncLock,
                        () =>
                        {
                            ApplyResponseCore();
                            return Task.FromResult(0);
                        }).GetAwaiter().GetResult(); // Block if the async version is in progress.
                }
            }
            catch (Exception)
            {
                Faulted = true;
                throw;
            }
        }

        protected virtual void ApplyResponseCore()
        {
            ApplyResponseGrant();
            ApplyResponseChallenge();
        }

        /// <summary>
        /// Causes the ApplyResponseCore to be invoked at most once per request. This method will be
        /// invoked either earlier, when the response headers are sent as a result of a response write or flush,
        /// or later, as the last step when the original async call to the middleware is returning.
        /// </summary>
        /// <returns></returns>
        private async Task ApplyResponseAsync()
        {
            // If ApplyResponse already failed in the OnSendingHeaderCallback or TeardownAsync code path then a
            // failed task is cached. If called again the same error will be re-thrown. This breaks error handling
            // scenarios like the ability to display the error page or re-execute the request.
            try
            {
                if (!Faulted)
                {
                    await LazyInitializer.EnsureInitialized(
                        ref _applyResponse,
                        ref _applyResponseInitialized,
                        ref _applyResponseSyncLock,
                        ApplyResponseCoreAsync);
                }
            }
            catch (Exception)
            {
                Faulted = true;
                throw;
            }
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

        protected abstract void ApplyResponseGrant();

        /// <summary>
        /// Override this method to dela with sign-in/sign-out concerns, if an authentication scheme in question
        /// deals with grant/revoke as part of it's request flow. (like setting/deleting cookies)
        /// </summary>
        /// <returns></returns>
        protected virtual Task ApplyResponseGrantAsync()
        {
            ApplyResponseGrant();
            return Task.FromResult(0);
        }

        public virtual void SignIn(SignInContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                SignInContext = context;
                SignOutContext = null;
                context.Accept();
            }

            if (PriorHandler != null)
            {
                PriorHandler.SignIn(context);
            }
        }

        public virtual void SignOut(SignOutContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                SignInContext = null;
                SignOutContext = context;
                context.Accept();
            }

            if (PriorHandler != null)
            {
                PriorHandler.SignOut(context);
            }
        }

        public virtual void Challenge(ChallengeContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                ChallengeContext = context;
                context.Accept();
            }

            if (PriorHandler != null)
            {
                PriorHandler.Challenge(context);
            }
        }

        protected abstract void ApplyResponseChallenge();

        public virtual bool ShouldHandleScheme(string authenticationScheme)
        {
            return string.Equals(BaseOptions.AuthenticationScheme, authenticationScheme, StringComparison.Ordinal) ||
                (BaseOptions.AutomaticAuthentication && string.IsNullOrWhiteSpace(authenticationScheme));
        }

        public virtual bool ShouldConvertChallengeToForbidden()
        {
            // Return 403 iff 401 and this handler's authenticate was called
            // and the challenge is for the authentication type
            return Response.StatusCode == 401 &&
                AuthenticateCalled &&
                ChallengeContext != null &&
                ShouldHandleScheme(ChallengeContext.AuthenticationScheme);
        }

        /// <summary>
        /// Override this method to deal with 401 challenge concerns, if an authentication scheme in question
        /// deals an authentication interaction as part of it's request flow. (like adding a response header, or
        /// changing the 401 result to 302 of a login page or external sign-in location.)
        /// </summary>
        /// <returns></returns>
        protected virtual Task ApplyResponseChallengeAsync()
        {
            ApplyResponseChallenge();
            return Task.FromResult(0);
        }

        protected void GenerateCorrelationId([NotNull] AuthenticationProperties properties)
        {
            var correlationKey = Constants.CorrelationPrefix + BaseOptions.AuthenticationScheme;

            var nonceBytes = new byte[32];
            CryptoRandom.GetBytes(nonceBytes);
            var correlationId = TextEncodings.Base64Url.Encode(nonceBytes);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };

            properties.Items[correlationKey] = correlationId;

            Response.Cookies.Append(correlationKey, correlationId, cookieOptions);
        }

        protected bool ValidateCorrelationId([NotNull] AuthenticationProperties properties)
        {
            var correlationKey = Constants.CorrelationPrefix + BaseOptions.AuthenticationScheme;
            var correlationCookie = Request.Cookies[correlationKey];
            if (string.IsNullOrWhiteSpace(correlationCookie))
            {
                Logger.LogWarning("{0} cookie not found.", correlationKey);
                return false;
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };
            Response.Cookies.Delete(correlationKey, cookieOptions);

            string correlationExtra;
            if (!properties.Items.TryGetValue(
                correlationKey,
                out correlationExtra))
            {
                Logger.LogWarning("{0} state property not found.", correlationKey);
                return false;
            }

            properties.Items.Remove(correlationKey);

            if (!string.Equals(correlationCookie, correlationExtra, StringComparison.Ordinal))
            {
                Logger.LogWarning("{0} correlation cookie and state property mismatch.", correlationKey);
                return false;
            }

            return true;
        }

        private void RegisterAuthenticationHandler()
        {
            var auth = Context.GetAuthentication();
            PriorHandler = auth.Handler;
            auth.Handler = this;
        }

        private void UnregisterAuthenticationHandler()
        {
            var auth = Context.GetAuthentication();
            auth.Handler = PriorHandler;
        }
    }
}