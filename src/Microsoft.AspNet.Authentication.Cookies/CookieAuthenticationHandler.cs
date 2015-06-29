// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Authentication.Cookies
{
    internal class CookieAuthenticationHandler : AuthenticationHandler<CookieAuthenticationOptions>
    {
        private const string HeaderNameCacheControl = "Cache-Control";
        private const string HeaderNamePragma = "Pragma";
        private const string HeaderNameExpires = "Expires";
        private const string HeaderValueNoCache = "no-cache";
        private const string HeaderValueMinusOne = "-1";
        private const string SessionIdClaim = "Microsoft.AspNet.Authentication.Cookies-SessionId";

        private bool _shouldRenew;
        private DateTimeOffset _renewIssuedUtc;
        private DateTimeOffset _renewExpiresUtc;
        private string _sessionKey;

        public override async Task<AuthenticationTicket> AuthenticateAsync()
        {
            AuthenticationTicket ticket = null;
            try
            {
                var cookie = Options.CookieManager.GetRequestCookie(Context, Options.CookieName);
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    return null;
                }

                ticket = Options.TicketDataFormat.Unprotect(cookie);

                if (ticket == null)
                {
                    Logger.LogWarning(@"Unprotect ticket failed");
                    return null;
                }

                if (Options.SessionStore != null)
                {
                    var claim = ticket.Principal.Claims.FirstOrDefault(c => c.Type.Equals(SessionIdClaim));
                    if (claim == null)
                    {
                        Logger.LogWarning(@"SessionId missing");
                        return null;
                    }
                    _sessionKey = claim.Value;
                    ticket = await Options.SessionStore.RetrieveAsync(_sessionKey);
                    if (ticket == null)
                    {
                        Logger.LogWarning(@"Identity missing in session store");
                        return null;
                    }
                }

                var currentUtc = Options.SystemClock.UtcNow;
                var issuedUtc = ticket.Properties.IssuedUtc;
                var expiresUtc = ticket.Properties.ExpiresUtc;

                if (expiresUtc != null && expiresUtc.Value < currentUtc)
                {
                    if (Options.SessionStore != null)
                    {
                        await Options.SessionStore.RemoveAsync(_sessionKey);
                    }
                    return null;
                }

                var allowRefresh = ticket.Properties.AllowRefresh ?? true;
                if (issuedUtc != null && expiresUtc != null && Options.SlidingExpiration && allowRefresh)
                {
                    var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                    var timeRemaining = expiresUtc.Value.Subtract(currentUtc);

                    if (timeRemaining < timeElapsed)
                    {
                        _shouldRenew = true;
                        _renewIssuedUtc = currentUtc;
                        var timeSpan = expiresUtc.Value.Subtract(issuedUtc.Value);
                        _renewExpiresUtc = currentUtc.Add(timeSpan);
                    }
                }

                var context = new CookieValidatePrincipalContext(Context, ticket, Options);

                await Options.Notifications.ValidatePrincipal(context);

                return new AuthenticationTicket(context.Principal, context.Properties, Options.AuthenticationScheme);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.Authenticate, exception, ticket);
                Options.Notifications.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
                return exceptionContext.Ticket;
            }
        }

        private CookieOptions BuildCookieOptions()
        {
            var cookieOptions = new CookieOptions
            {
                Domain = Options.CookieDomain,
                HttpOnly = Options.CookieHttpOnly,
                Path = Options.CookiePath ?? (OriginalPathBase.HasValue ? OriginalPathBase.ToString() : "/"),
            };
            if (Options.CookieSecure == CookieSecureOption.SameAsRequest)
            {
                cookieOptions.Secure = Request.IsHttps;
            }
            else
            {
                cookieOptions.Secure = Options.CookieSecure == CookieSecureOption.Always;
            }
            return cookieOptions;
        }

        private async Task ApplyCookie(AuthenticationTicket model)
        {
            var cookieOptions = BuildCookieOptions();

            model.Properties.IssuedUtc = _renewIssuedUtc;
            model.Properties.ExpiresUtc = _renewExpiresUtc;

            if (Options.SessionStore != null && _sessionKey != null)
            {
                await Options.SessionStore.RenewAsync(_sessionKey, model);
                var principal = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] { new Claim(SessionIdClaim, _sessionKey, ClaimValueTypes.String, Options.ClaimsIssuer) },
                        Options.AuthenticationScheme));
                model = new AuthenticationTicket(principal, null, Options.AuthenticationScheme);
            }

            var cookieValue = Options.TicketDataFormat.Protect(model);

            if (model.Properties.IsPersistent)
            {
                cookieOptions.Expires = _renewExpiresUtc.ToUniversalTime().DateTime;
            }

            Options.CookieManager.AppendResponseCookie(
                Context,
                Options.CookieName,
                cookieValue,
                cookieOptions);

            Response.Headers.Set(
                HeaderNameCacheControl,
                HeaderValueNoCache);

            Response.Headers.Set(
                HeaderNamePragma,
                HeaderValueNoCache);

            Response.Headers.Set(
                HeaderNameExpires,
                HeaderValueMinusOne);
        }

        protected override async Task FinishResponseAsync()
        {
            // Only renew if requested, and neither sign in or sign out was called
            if (!_shouldRenew || SignInAccepted || SignOutAccepted)
            {
                return;
            }

            var model = await AuthenticateAsync();
            try
            {
                await ApplyCookie(model);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.ApplyResponseGrant, exception, model);
                Options.Notifications.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
        }

        protected override async Task HandleSignInAsync(SignInContext signin)
        {
            var model = await AuthenticateAsync();
            try
            {
                var cookieOptions = BuildCookieOptions();

                var signInContext = new CookieResponseSignInContext(
                    Context,
                    Options,
                    Options.AuthenticationScheme,
                    signin.Principal,
                    new AuthenticationProperties(signin.Properties),
                    cookieOptions);

                DateTimeOffset issuedUtc;
                if (signInContext.Properties.IssuedUtc.HasValue)
                {
                    issuedUtc = signInContext.Properties.IssuedUtc.Value;
                }
                else
                {
                    issuedUtc = Options.SystemClock.UtcNow;
                    signInContext.Properties.IssuedUtc = issuedUtc;
                }

                if (!signInContext.Properties.ExpiresUtc.HasValue)
                {
                    signInContext.Properties.ExpiresUtc = issuedUtc.Add(Options.ExpireTimeSpan);
                }

                Options.Notifications.ResponseSignIn(signInContext);

                if (signInContext.Properties.IsPersistent)
                {
                    var expiresUtc = signInContext.Properties.ExpiresUtc ?? issuedUtc.Add(Options.ExpireTimeSpan);
                    signInContext.CookieOptions.Expires = expiresUtc.ToUniversalTime().DateTime;
                }

                model = new AuthenticationTicket(signInContext.Principal, signInContext.Properties, signInContext.AuthenticationScheme);
                if (Options.SessionStore != null)
                {
                    if (_sessionKey != null)
                    {
                        await Options.SessionStore.RemoveAsync(_sessionKey);
                    }
                    _sessionKey = await Options.SessionStore.StoreAsync(model);
                    var principal = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(SessionIdClaim, _sessionKey, ClaimValueTypes.String, Options.ClaimsIssuer) },
                            Options.ClaimsIssuer));
                    model = new AuthenticationTicket(principal, null, Options.AuthenticationScheme);
                }
                var cookieValue = Options.TicketDataFormat.Protect(model);

                Options.CookieManager.AppendResponseCookie(
                    Context,
                    Options.CookieName,
                    cookieValue,
                    signInContext.CookieOptions);

                var signedInContext = new CookieResponseSignedInContext(
                    Context,
                    Options,
                    Options.AuthenticationScheme,
                    signInContext.Principal,
                    signInContext.Properties);

                Options.Notifications.ResponseSignedIn(signedInContext);

                Response.Headers.Set(
                    HeaderNameCacheControl,
                    HeaderValueNoCache);

                Response.Headers.Set(
                    HeaderNamePragma,
                    HeaderValueNoCache);

                Response.Headers.Set(
                    HeaderNameExpires,
                    HeaderValueMinusOne);

                var shouldLoginRedirect = Options.LoginPath.HasValue && Request.Path == Options.LoginPath;

                if ((shouldLoginRedirect) && Response.StatusCode == 200)
                {
                    var query = Request.Query;
                    var redirectUri = query.Get(Options.ReturnUrlParameter);
                    if (!string.IsNullOrWhiteSpace(redirectUri)
                        && IsHostRelative(redirectUri))
                    {
                        var redirectContext = new CookieApplyRedirectContext(Context, Options, redirectUri);
                        Options.Notifications.ApplyRedirect(redirectContext);
                    }
                }
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.ApplyResponseGrant, exception, model);
                Options.Notifications.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
        }

        protected override async Task HandleSignOutAsync(SignOutContext signOutContext)
        {
            var model = await AuthenticateAsync();
            try
            {
                var cookieOptions = BuildCookieOptions();

                if (Options.SessionStore != null && _sessionKey != null)
                {
                    await Options.SessionStore.RemoveAsync(_sessionKey);
                }

                var context = new CookieResponseSignOutContext(
                    Context,
                    Options,
                    cookieOptions);

                Options.Notifications.ResponseSignOut(context);

                Options.CookieManager.DeleteCookie(
                    Context,
                    Options.CookieName,
                    context.CookieOptions);

                Response.Headers.Set(
                    HeaderNameCacheControl,
                    HeaderValueNoCache);

                Response.Headers.Set(
                    HeaderNamePragma,
                    HeaderValueNoCache);

                Response.Headers.Set(
                    HeaderNameExpires,
                    HeaderValueMinusOne);

                var shouldLogoutRedirect = Options.LogoutPath.HasValue && Request.Path == Options.LogoutPath;

                if (shouldLogoutRedirect && Response.StatusCode == 200)
                {
                    var query = Request.Query;
                    var redirectUri = query.Get(Options.ReturnUrlParameter);
                    if (!string.IsNullOrWhiteSpace(redirectUri)
                        && IsHostRelative(redirectUri))
                    {
                        var redirectContext = new CookieApplyRedirectContext(Context, Options, redirectUri);
                        Options.Notifications.ApplyRedirect(redirectContext);
                    }
                }
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.ApplyResponseGrant, exception, model);
                Options.Notifications.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }

        }

        private static bool IsHostRelative(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            if (path.Length == 1)
            {
                return path[0] == '/';
            }
            return path[0] == '/' && path[1] != '/' && path[1] != '\\';
        }

        protected override Task<bool> HandleForbiddenAsync(ChallengeContext context)
        {
            // HandleForbidden by redirecting to AccessDeniedPath if set
            if (Options.AccessDeniedPath.HasValue)
            {
                try
                {
                    var accessDeniedUri =
                        Request.Scheme +
                        "://" +
                        Request.Host +
                        Request.PathBase +
                        Options.AccessDeniedPath;

                    var redirectContext = new CookieApplyRedirectContext(Context, Options, accessDeniedUri);
                    Options.Notifications.ApplyRedirect(redirectContext);
                }
                catch (Exception exception)
                {
                    var exceptionContext = new CookieExceptionContext(Context, Options,
                        CookieExceptionContext.ExceptionLocation.ApplyResponseChallenge, exception, ticket: null);
                    Options.Notifications.Exception(exceptionContext);
                    if (exceptionContext.Rethrow)
                    {
                        throw;
                    }
                }
                return Task.FromResult(true);
            }
            else
            {
                return base.HandleForbiddenAsync(context);
            }
        }

        protected override Task<bool> HandleUnauthorizedAsync([NotNull] ChallengeContext context)
        {
            if (!Options.LoginPath.HasValue)
            {
                return base.HandleUnauthorizedAsync(context);
            }

            var redirectUri = new AuthenticationProperties(context.Properties).RedirectUri;
            try
            {
                if (string.IsNullOrWhiteSpace(redirectUri))
                {
                    redirectUri =
                        Request.PathBase +
                        Request.Path +
                        Request.QueryString;
                }

                var loginUri =
                    Request.Scheme +
                    "://" +
                    Request.Host +
                    Request.PathBase +
                    Options.LoginPath +
                    QueryString.Create(Options.ReturnUrlParameter, redirectUri);

                var redirectContext = new CookieApplyRedirectContext(Context, Options, loginUri);
                Options.Notifications.ApplyRedirect(redirectContext);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.ApplyResponseChallenge, exception, ticket: null);
                Options.Notifications.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
            return Task.FromResult(true);
        }
    }
}
