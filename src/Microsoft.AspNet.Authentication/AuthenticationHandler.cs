// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features.Authentication;
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
        private Task<AuthenticationTicket> _authenticateTask;
        private bool _finishCalled;
        private AuthenticationOptions _baseOptions;

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

        protected ILogger Logger { get; private set; }

        protected IUrlEncoder UrlEncoder { get; private set; }

        internal AuthenticationOptions BaseOptions
        {
            get { return _baseOptions; }
        }

        public IAuthenticationHandler PriorHandler { get; set; }

        protected string CurrentUri
        {
            get
            {
                return Request.Scheme + "://" + Request.Host + Request.PathBase + Request.Path + Request.QueryString;
            }
        }

        protected async Task BaseInitializeAsync([NotNull] AuthenticationOptions options, [NotNull] HttpContext context, [NotNull] ILogger logger, [NotNull] IUrlEncoder encoder)
        {
            _baseOptions = options;
            Context = context;
            OriginalPathBase = Request.PathBase;
            Logger = logger;
            UrlEncoder = encoder;

            RegisterAuthenticationHandler();

            Response.OnStarting(OnStartingCallback, this);

            // Automatic authentication is the empty scheme
            if (ShouldHandleScheme(string.Empty))
            {
                var ticket = await AuthenticateOnceAsync();
                if (ticket?.Principal != null)
                {
                    Context.User = SecurityHelper.MergeUserPrincipal(Context.User, ticket.Principal);
                }
            }
        }

        protected string BuildRedirectUri(string targetPath)
        {
            return Request.Scheme + "://" + Request.Host + OriginalPathBase + targetPath;
        }

        private static async Task OnStartingCallback(object state)
        {
            var handler = (AuthenticationHandler)state;
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
            if (!ChallengeCalled && BaseOptions.AutomaticAuthentication && Response.StatusCode == 401)
            {
                await HandleUnauthorizedAsync(new ChallengeContext(BaseOptions.AuthenticationScheme));
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
        public virtual Task<bool> InvokeAsync()
        {
            return Task.FromResult(false);
        }

        public void GetDescriptions(DescribeSchemesContext describeContext)
        {
            describeContext.Accept(BaseOptions.Description.Items);

            if (PriorHandler != null)
            {
                PriorHandler.GetDescriptions(describeContext);
            }
        }

        protected Task<AuthenticationTicket> AuthenticateOnceAsync()
        {
            if (_authenticateTask == null)
            {
                _authenticateTask = AuthenticateAsync();
            }
            return _authenticateTask;
        }

        public async Task AuthenticateAsync(AuthenticateContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                // Calling Authenticate more than once should always return the original value. 
                var ticket = await AuthenticateOnceAsync();
                if (ticket?.Principal != null)
                {
                    context.Authenticated(ticket.Principal, ticket.Properties.Items, BaseOptions.Description.Items);
                    _authenticateTask = Task.FromResult(ticket);
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

        protected abstract Task<AuthenticationTicket> AuthenticateAsync();

        public bool ShouldHandleScheme(string authenticationScheme)
        {
            return string.Equals(BaseOptions.AuthenticationScheme, authenticationScheme, StringComparison.Ordinal) ||
                (BaseOptions.AutomaticAuthentication && string.IsNullOrWhiteSpace(authenticationScheme));
        }

        public async Task SignInAsync(SignInContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                SignInAccepted = true;
                await HandleSignInAsync(context);
                context.Accept();
            }

            if (PriorHandler != null)
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
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                SignOutAccepted = true;
                await HandleSignOutAsync(context);
                context.Accept();
            }

            if (PriorHandler != null)
            {
                await PriorHandler.SignOutAsync(context);
            }
        }

        protected virtual Task HandleSignOutAsync(SignOutContext context)
        {
            return Task.FromResult(0);
        }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <returns>True if no other handlers should be called</returns>
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
            bool handled = false;
            ChallengeCalled = true;
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                switch (context.Behavior)
                {
                    case ChallengeBehavior.Automatic:
                        // If there is a principal already, invoke the forbidden code path
                        var ticket = await AuthenticateAsync();
                        if (ticket?.Principal != null)
                        {
                            handled = await HandleForbiddenAsync(context);
                        }
                        else
                        {
                            handled = await HandleUnauthorizedAsync(context);
                        }
                        break;
                    case ChallengeBehavior.Unauthorized:
                        handled = await HandleUnauthorizedAsync(context);
                        break;
                    case ChallengeBehavior.Forbidden:
                        handled = await HandleForbiddenAsync(context);
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