// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Base class for the per-request work performed by most authentication middleware.
    /// </summary>
    /// <typeparam name="TOptions">Specifies which type for of AuthenticationOptions property</typeparam>
    public abstract class AuthenticationHandler<TOptions> : IAuthenticationHandler where TOptions : AuthenticationOptions
    {
        private Task<AuthenticateResult> _authenticateTask;
        private bool _finishCalled;

        protected bool SignInAccepted { get; set; }
        protected bool SignOutAccepted { get; set; }
        protected bool ChallengeCalled { get; set; }

        protected HttpContext Context { get; private set; }

        protected HttpRequest Request
        {
            get { return Context.Request; }
        }

        protected HttpResponse Response
        {
            get { return Context.Response; }
        }

        protected PathString OriginalPathBase { get; private set; }

        protected PathString OriginalPath { get; private set; }

        protected ILogger Logger { get; private set; }

        protected UrlEncoder UrlEncoder { get; private set; }

        public IAuthenticationHandler PriorHandler { get; set; }

        protected string CurrentUri
        {
            get
            {
                return Request.Scheme + "://" + Request.Host + Request.PathBase + Request.Path + Request.QueryString;
            }
        }

        protected TOptions Options { get; private set; }

        /// <summary>
        /// Initialize is called once per request to contextualize this instance with appropriate state.
        /// </summary>
        /// <param name="options">The original options passed by the application control behavior</param>
        /// <param name="context">The utility object to observe the current request and response</param>
        /// <param name="logger">The logging factory used to create loggers</param>
        /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
        /// <returns>async completion</returns>
        public async Task InitializeAsync(TOptions options, HttpContext context, ILogger logger, UrlEncoder encoder)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            Options = options;
            Context = context;
            OriginalPathBase = Request.PathBase;
            OriginalPath = Request.Path;
            Logger = logger;
            UrlEncoder = encoder;

            RegisterAuthenticationHandler();

            Response.OnStarting(OnStartingCallback, this);

            if (ShouldHandleScheme(AuthenticationManager.AutomaticScheme, Options.AutomaticAuthenticate))
            {
                var result = await HandleAuthenticateOnceAsync();
                if (result?.Failure != null)
                {
                    Logger.AuthenticationSchemeNotAuthenticatedWithFailure(Options.AuthenticationScheme, result.Failure.Message);
                }
                var ticket = result?.Ticket;
                if (ticket?.Principal != null)
                {
                    Context.User = SecurityHelper.MergeUserPrincipal(Context.User, ticket.Principal);
                    Logger.UserPrinicpalMerged(Options.AuthenticationScheme);
                }
            }
        }

        protected string BuildRedirectUri(string targetPath)
        {
            return Request.Scheme + "://" + Request.Host + OriginalPathBase + targetPath;
        }

        private static async Task OnStartingCallback(object state)
        {
            var handler = (AuthenticationHandler<TOptions>)state;
            await handler.FinishResponseOnce();
        }

        private async Task FinishResponseOnce()
        {
            if (!_finishCalled)
            {
                _finishCalled = true;
                await FinishResponseAsync();
                await HandleAutomaticChallengeIfNeeded();
            }
        }

        /// <summary>
        /// Hook that is called when the response about to be sent
        /// </summary>
        /// <returns></returns>
        protected virtual Task FinishResponseAsync()
        {
            return Task.FromResult(0);
        }

        private async Task HandleAutomaticChallengeIfNeeded()
        {
            if (!ChallengeCalled && Options.AutomaticChallenge && Response.StatusCode == 401)
            {
                await HandleUnauthorizedAsync(new ChallengeContext(Options.AuthenticationScheme));
            }
        }

        /// <summary>
        /// Called once after Invoke by AuthenticationMiddleware.
        /// </summary>
        /// <returns>async completion</returns>
        internal async Task TeardownAsync()
        {
            try
            {
                await FinishResponseOnce();
            }
            finally
            {
                UnregisterAuthenticationHandler();
            }
        }

        /// <summary>
        /// Called once by common code after initialization. If an authentication middleware responds directly to
        /// specifically known paths it must override this virtual, compare the request path to it's known paths,
        /// provide any response information as appropriate, and true to stop further processing.
        /// </summary>
        /// <returns>Returning false will cause the common code to call the next middleware in line. Returning true will
        /// cause the common code to begin the async completion journey without calling the rest of the middleware
        /// pipeline.</returns>
        public virtual Task<bool> HandleRequestAsync()
        {
            return Task.FromResult(false);
        }

        public void GetDescriptions(DescribeSchemesContext describeContext)
        {
            describeContext.Accept(Options.Description.Items);

            if (PriorHandler != null)
            {
                PriorHandler.GetDescriptions(describeContext);
            }
        }

        public bool ShouldHandleScheme(string authenticationScheme, bool handleAutomatic)
        {
            return string.Equals(Options.AuthenticationScheme, authenticationScheme, StringComparison.Ordinal) ||
                (handleAutomatic && string.Equals(authenticationScheme, AuthenticationManager.AutomaticScheme, StringComparison.Ordinal));
        }

        public async Task AuthenticateAsync(AuthenticateContext context)
        {
            var handled = false;
            if (ShouldHandleScheme(context.AuthenticationScheme, Options.AutomaticAuthenticate))
            {
                // Calling Authenticate more than once should always return the original value.
                var result = await HandleAuthenticateOnceAsync();

                if (result?.Failure != null)
                {
                    context.Failed(result.Failure);
                }
                else
                {
                    var ticket = result?.Ticket;
                    if (ticket?.Principal != null)
                    {
                        context.Authenticated(ticket.Principal, ticket.Properties.Items, Options.Description.Items);
                        Logger.AuthenticationSchemeAuthenticated(Options.AuthenticationScheme);
                        handled = true;
                    }
                    else
                    {
                        context.NotAuthenticated();
                        Logger.AuthenticationSchemeNotAuthenticated(Options.AuthenticationScheme);
                    }
                }
            }

            if (PriorHandler != null && !handled)
            {
                await PriorHandler.AuthenticateAsync(context);
            }
        }

        /// <summary>
        /// Used to ensure HandleAuthenticateAsync is only invoked once. The subsequent calls
        /// will return the same authenticate result.
        /// </summary>
        protected Task<AuthenticateResult> HandleAuthenticateOnceAsync()
        {
            if (_authenticateTask == null)
            {
                _authenticateTask = HandleAuthenticateAsync();
            }

            return _authenticateTask;
        }

        /// <summary>
        /// Used to ensure HandleAuthenticateAsync is only invoked once safely. The subsequent
        /// calls will return the same authentication result. Any exceptions will be converted
        /// into a failed authenticatoin result containing the exception.
        /// </summary>
        protected async Task<AuthenticateResult> HandleAuthenticateOnceSafeAsync()
        {
            try
            {
                return await HandleAuthenticateOnceAsync();
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex);
            }
        }

        protected abstract Task<AuthenticateResult> HandleAuthenticateAsync();

        public async Task SignInAsync(SignInContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme, handleAutomatic: false))
            {
                SignInAccepted = true;
                await HandleSignInAsync(context);
                Logger.AuthenticationSchemeSignedIn(Options.AuthenticationScheme);
                context.Accept();
            }
            else if (PriorHandler != null)
            {
                await PriorHandler.SignInAsync(context);
            }
        }

        protected virtual Task HandleSignInAsync(SignInContext context)
        {
            return Task.FromResult(0);
        }

        public async Task SignOutAsync(SignOutContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (ShouldHandleScheme(context.AuthenticationScheme, handleAutomatic: false))
            {
                SignOutAccepted = true;
                await HandleSignOutAsync(context);
                Logger.AuthenticationSchemeSignedOut(Options.AuthenticationScheme);
                context.Accept();
            }
            else if (PriorHandler != null)
            {
                await PriorHandler.SignOutAsync(context);
            }
        }

        protected virtual Task HandleSignOutAsync(SignOutContext context)
        {
            return Task.FromResult(0);
        }

        protected virtual Task<bool> HandleForbiddenAsync(ChallengeContext context)
        {
            Response.StatusCode = 403;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Override this method to deal with 401 challenge concerns, if an authentication scheme in question
        /// deals an authentication interaction as part of it's request flow. (like adding a response header, or
        /// changing the 401 result to 302 of a login page or external sign-in location.)
        /// </summary>
        /// <param name="context"></param>
        /// <returns>True if no other handlers should be called</returns>
        protected virtual Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            Response.StatusCode = 401;
            return Task.FromResult(false);
        }

        public async Task ChallengeAsync(ChallengeContext context)
        {
            ChallengeCalled = true;
            var handled = false;
            if (ShouldHandleScheme(context.AuthenticationScheme, Options.AutomaticChallenge))
            {
                switch (context.Behavior)
                {
                    case ChallengeBehavior.Automatic:
                        // If there is a principal already, invoke the forbidden code path
                        var result = await HandleAuthenticateOnceSafeAsync();
                        if (result?.Ticket?.Principal != null)
                        {
                            goto case ChallengeBehavior.Forbidden;
                        }
                        goto case ChallengeBehavior.Unauthorized;
                    case ChallengeBehavior.Unauthorized:
                        handled = await HandleUnauthorizedAsync(context);
                        Logger.AuthenticationSchemeChallenged(Options.AuthenticationScheme);
                        break;
                    case ChallengeBehavior.Forbidden:
                        handled = await HandleForbiddenAsync(context);
                        Logger.AuthenticationSchemeForbidden(Options.AuthenticationScheme);
                        break;
                }
                context.Accept();
            }

            if (!handled && PriorHandler != null)
            {
                await PriorHandler.ChallengeAsync(context);
            }
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
