// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public abstract class SessionManager
    {
        private readonly IOptions<IdentityServiceOptions> _options;
        private readonly IOptions<IdentityOptions> _identityOptions;
        private readonly CookieAuthenticationOptions _sessionCookieOptions;
        private readonly ITimeStampManager _timeStampManager;
        private readonly IHttpContextAccessor _contextAccessor;
        protected readonly ProtocolErrorProvider _errorProvider;

        private HttpContext _context;

        public SessionManager(
            IOptions<IdentityServiceOptions> options,
            IOptions<IdentityOptions> identityOptions,
            IOptionsSnapshot<CookieAuthenticationOptions> cookieOptions,
            ITimeStampManager timeStampManager,
            IHttpContextAccessor contextAccessor,
            ProtocolErrorProvider errorProvider)
        {
            _options = options;
            _identityOptions = identityOptions;
            _timeStampManager = timeStampManager;
            _contextAccessor = contextAccessor;
            _errorProvider = errorProvider;
            _sessionCookieOptions = cookieOptions.Get(IdentityServiceOptions.CookieAuthenticationScheme);
        }

        public HttpContext Context
        {
            get
            {
                if (_context == null)
                {
                    _context = _contextAccessor.HttpContext;
                }
                if (_context == null)
                {
                    throw new InvalidOperationException($"{nameof(HttpContext)} can't be null.");
                }

                return _context;
            }
            set
            {
                _context = value;
            }
        }

        public async Task<ClaimsPrincipal> GetCurrentSessions() =>
            await GetPrincipal(_options.Value.SessionPolicy) ?? new ClaimsPrincipal(new ClaimsIdentity());

        public async Task<ClaimsPrincipal> GetCurrentLoggedInUser() =>
            await GetPrincipal(_options.Value.LoginPolicy) ?? new ClaimsPrincipal(new ClaimsIdentity());

        private async Task<ClaimsPrincipal> GetPrincipal(AuthorizationPolicy policy)
        {
            ClaimsPrincipal newPrincipal = null;
            for (var i = 0; i < policy.AuthenticationSchemes.Count; i++)
            {
                var scheme = policy.AuthenticationSchemes[i];
                var result = await Context.AuthenticateAsync(scheme);
                if (result != null)
                {
                    newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, result.Principal);
                }
            }

            return newPrincipal;
        }

        public async Task StartSessionAsync(ClaimsPrincipal user, ClaimsPrincipal application)
        {
            var policy = _options.Value.SessionPolicy;
            ClaimsPrincipal newPrincipal = await GetCurrentSessions();

            newPrincipal = FilterExistingIdentities();
            newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, CreatePrincipal());

            for (var i = 0; i < policy.AuthenticationSchemes.Count; i++)
            {
                var scheme = policy.AuthenticationSchemes[i];
                await Context.SignInAsync(scheme, newPrincipal);
            }

            ClaimsPrincipal FilterExistingIdentities()
            {
                var scheme = IdentityServiceOptions.CookieAuthenticationScheme;
                string userIdClaimType = _identityOptions.Value.ClaimsIdentity.UserIdClaimType;
                var userId = user.FindFirstValue(userIdClaimType);
                var clientId = application.FindFirstValue(IdentityServiceClaimTypes.ClientId);

                var filteredIdentities = newPrincipal.Identities
                    .Where(i => scheme.Equals(i.AuthenticationType, StringComparison.Ordinal) &&
                                !IsUserSesionForApplication(i, userId, clientId));

                return new ClaimsPrincipal(filteredIdentities);
            }

            ClaimsPrincipal CreatePrincipal()
            {
                var principal = new ClaimsPrincipal();
                var userId = user.FindFirstValue(_identityOptions.Value.ClaimsIdentity.UserIdClaimType);
                var clientId = application.FindFirstValue(IdentityServiceClaimTypes.ClientId);
                var logoutUris = application.FindAll(IdentityServiceClaimTypes.LogoutRedirectUri);

                var duration = _sessionCookieOptions.ExpireTimeSpan;
                var expiration = _timeStampManager.GetTimeStampInEpochTime(duration);

                var identity = new ClaimsIdentity(
                    new List<Claim>(logoutUris)
                    {
                        new Claim(IdentityServiceClaimTypes.UserId,userId),
                        new Claim(IdentityServiceClaimTypes.ClientId,clientId),
                        new Claim(IdentityServiceClaimTypes.Expires,expiration)
                    },
                    IdentityServiceOptions.CookieAuthenticationScheme);

                principal.AddIdentity(identity);

                return principal;
            }

        }

        public async Task<LogoutResult> EndSessionAsync(LogoutRequest request)
        {
            var loginPolicy = _options.Value.LoginPolicy;
            for (int i = 0; i < loginPolicy.AuthenticationSchemes.Count; i++)
            {
                var scheme = loginPolicy.AuthenticationSchemes[i];
                await Context.SignOutAsync(scheme);
            }

            var policy = _options.Value.SessionPolicy;

            for (var i = 0; i < policy.AuthenticationSchemes.Count; i++)
            {
                var scheme = policy.AuthenticationSchemes[i];
                await Context.SignOutAsync(scheme);
            }

            var postLogoutUri = request.LogoutRedirectUri;
            var state = request.Message.State;
            var redirectUri = request.Message.State == null ?
                postLogoutUri :
                QueryHelpers.AddQueryString(postLogoutUri, OpenIdConnectParameterNames.State, state);

            return LogoutResult.Redirect(redirectUri);
        }

        private bool IsUserSesionForApplication(ClaimsIdentity identity, string userId, string clientId)
        {
            var userIdClaimType = _identityOptions.Value.ClaimsIdentity.UserIdClaimType;
            return identity.Claims.SingleOrDefault(c => ClaimMatches(c, userIdClaimType, userId)) != null &&
                identity.Claims.SingleOrDefault(c => ClaimMatches(c, IdentityServiceClaimTypes.ClientId, clientId)) != null;

            bool ClaimMatches(Claim claim, string type, string value) =>
                claim.Type.Equals(type, StringComparison.Ordinal) && claim.Value.Equals(value, StringComparison.Ordinal);
        }

        protected bool IsAuthenticatedWithApplication(ClaimsPrincipal loggedUser, ClaimsPrincipal sessions, OpenIdConnectMessage message)
        {
            string userIdClaimType = _identityOptions.Value.ClaimsIdentity.UserIdClaimType;
            var userId = loggedUser.FindFirstValue(userIdClaimType);
            var clientId = message.ClientId;

            return sessions.Identities.Any(i => IsUserSesionForApplication(i, userId, clientId)) ||
                loggedUser.Identities.Any(i => i.IsAuthenticated);
        }

        public abstract Task<Session> CreateSessionAsync(string userId, string clientId);

        public abstract Task<AuthorizeResult> IsAuthorizedAsync(AuthorizationRequest request);
    }

    public class SessionManager<TUser, TApplication> : SessionManager where TUser : class where TApplication : class
    {
        private readonly UserManager<TUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<TUser> _userPrincipalFactory;
        private readonly ApplicationManager<TApplication> _applicationManager;
        private readonly IApplicationClaimsPrincipalFactory<TApplication> _applicationPrincipalFactory;

        public SessionManager(
            IOptions<IdentityServiceOptions> options,
            IOptions<IdentityOptions> identityOptions,
            IOptionsSnapshot<CookieAuthenticationOptions> cookieOptions,
            ITimeStampManager timeStampManager,
            UserManager<TUser> userManager,
            IUserClaimsPrincipalFactory<TUser> userPrincipalFactory,
            IApplicationClaimsPrincipalFactory<TApplication> applicationPrincipalFactory,
            ApplicationManager<TApplication> applicationManager,
            IHttpContextAccessor contextAccessor,
            ProtocolErrorProvider errorProvider)
            : base(options, identityOptions, cookieOptions, timeStampManager, contextAccessor, errorProvider)
        {
            _userManager = userManager;
            _userPrincipalFactory = userPrincipalFactory;
            _applicationManager = applicationManager;
            _applicationPrincipalFactory = applicationPrincipalFactory;
        }

        public override async Task<AuthorizeResult> IsAuthorizedAsync(AuthorizationRequest request)
        {
            var message = request.Message;
            var sessions = await GetCurrentSessions();
            var loggedUser = await GetCurrentLoggedInUser();

            var hasASession = IsAuthenticatedWithApplication(loggedUser, sessions, message);
            var isLoggedIn = loggedUser.Identities.Any(i => i.IsAuthenticated);
            if (!(hasASession || isLoggedIn) && PromptIsForbidden(message))
            {
                return AuthorizeResult.Forbidden(RequiresLogin(request));
            }

            if (!(hasASession || isLoggedIn) || PromptIsMandatory(message))
            {
                return AuthorizeResult.LoginRequired();
            }

            var user = await _userManager.GetUserAsync(loggedUser);
            var userPrincipal = await _userPrincipalFactory.CreateAsync(user);

            var application = await _applicationManager.FindByClientIdAsync(message.ClientId);
            var applicationPrincipal = await _applicationPrincipalFactory.CreateAsync(application);

            return AuthorizeResult.Authorized(userPrincipal, applicationPrincipal);
        }

        public override async Task<Session> CreateSessionAsync(string userId, string clientId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var userPrincipal = await _userPrincipalFactory.CreateAsync(user);

            var application = await _applicationManager.FindByClientIdAsync(clientId);
            var applicationPrincipal = await _applicationPrincipalFactory.CreateAsync(application);

            return new Session(userPrincipal, applicationPrincipal);
        }

        private bool PromptIsMandatory(OpenIdConnectMessage message)
        {
            return message.Prompt != null && message.Prompt.Contains(PromptValues.Login);
        }

        private bool PromptIsForbidden(OpenIdConnectMessage message)
        {
            return message.Prompt != null && message.Prompt.Contains(PromptValues.None);
        }

        private AuthorizationRequestError RequiresLogin(AuthorizationRequest request)
        {
            var error = _errorProvider.RequiresLogin();
            error.State = request.Message.State;

            return new AuthorizationRequestError(
                error,
                request.RequestGrants.RedirectUri,
                request.RequestGrants.ResponseMode);
        }
    }
}
