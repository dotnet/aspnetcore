// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Authentication.Cookies
{
    internal class CookieAuthenticationHandler : AuthenticationHandler<CookieAuthenticationOptions>
    {
        private const string HeaderValueNoCache = "no-cache";
        private const string HeaderValueMinusOne = "-1";
        private const string SessionIdClaim = "Microsoft.AspNet.Authentication.Cookies-SessionId";

        private bool _shouldRenew;
        private DateTimeOffset? _renewIssuedUtc;
        private DateTimeOffset? _renewExpiresUtc;
        private string _sessionKey;
        private Task<AuthenticationTicket> _cookieTicketTask;

        private Task<AuthenticationTicket> EnsureCookieTicket()
        {
            // We only need to read the ticket once
            if (_cookieTicketTask == null)
            {
                _cookieTicketTask = ReadCookieTicket();
            }
            return _cookieTicketTask;
        }

        private async Task<AuthenticationTicket> ReadCookieTicket()
        {
            var cookie = Options.CookieManager.GetRequestCookie(Context, Options.CookieName);
            if (string.IsNullOrEmpty(cookie))
            {
                return null;
            }

            var ticket = Options.TicketDataFormat.Unprotect(cookie);
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

            // Finally we have a valid ticket
            return ticket;
        }

        protected override async Task<AuthenticationTicket> HandleAuthenticateAsync()
        {
            AuthenticationTicket ticket = null;
            try
            {
                ticket = await EnsureCookieTicket();
                if (ticket == null)
                {
                    return null;
                }

                var context = new CookieValidatePrincipalContext(Context, ticket, Options);
                await Options.Events.ValidatePrincipal(context);

                if (context.Principal == null)
                {
                    return null;
                }

                if (context.ShouldRenew)
                {
                    _shouldRenew = true;
                }

                return new AuthenticationTicket(context.Principal, context.Properties, Options.AuthenticationScheme);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.Authenticate, exception, ticket);
                await Options.Events.Exception(exceptionContext);
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

        protected override async Task FinishResponseAsync()
        {
            // Only renew if requested, and neither sign in or sign out was called
            if (!_shouldRenew || SignInAccepted || SignOutAccepted)
            {
                return;
            }

            var ticket = await HandleAuthenticateOnceAsync();
            try
            {
                if (_renewIssuedUtc.HasValue)
                {
                    ticket.Properties.IssuedUtc = _renewIssuedUtc;
                }
                if (_renewExpiresUtc.HasValue)
                {
                    ticket.Properties.ExpiresUtc = _renewExpiresUtc;
                }

                if (Options.SessionStore != null && _sessionKey != null)
                {
                    await Options.SessionStore.RenewAsync(_sessionKey, ticket);
                    var principal = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(SessionIdClaim, _sessionKey, ClaimValueTypes.String, Options.ClaimsIssuer) },
                            Options.AuthenticationScheme));
                    ticket = new AuthenticationTicket(principal, null, Options.AuthenticationScheme);
                }

                var cookieValue = Options.TicketDataFormat.Protect(ticket);

                var cookieOptions = BuildCookieOptions();
                if (ticket.Properties.IsPersistent && _renewExpiresUtc.HasValue)
                {
                    cookieOptions.Expires = _renewExpiresUtc.Value.ToUniversalTime().DateTime;
                }

                Options.CookieManager.AppendResponseCookie(
                    Context,
                    Options.CookieName,
                    cookieValue,
                    cookieOptions);

                await ApplyHeaders(shouldRedirectToReturnUrl: false);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.FinishResponse, exception, ticket);
                await Options.Events.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
        }

        protected override async Task HandleSignInAsync(SignInContext signin)
        {
            var ticket = await EnsureCookieTicket();
            try
            {
                var cookieOptions = BuildCookieOptions();

                var signInContext = new CookieSigningInContext(
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

                await Options.Events.SigningIn(signInContext);

                if (signInContext.Properties.IsPersistent)
                {
                    var expiresUtc = signInContext.Properties.ExpiresUtc ?? issuedUtc.Add(Options.ExpireTimeSpan);
                    signInContext.CookieOptions.Expires = expiresUtc.ToUniversalTime().DateTime;
                }

                ticket = new AuthenticationTicket(signInContext.Principal, signInContext.Properties, signInContext.AuthenticationScheme);
                if (Options.SessionStore != null)
                {
                    if (_sessionKey != null)
                    {
                        await Options.SessionStore.RemoveAsync(_sessionKey);
                    }
                    _sessionKey = await Options.SessionStore.StoreAsync(ticket);
                    var principal = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(SessionIdClaim, _sessionKey, ClaimValueTypes.String, Options.ClaimsIssuer) },
                            Options.ClaimsIssuer));
                    ticket = new AuthenticationTicket(principal, null, Options.AuthenticationScheme);
                }
                var cookieValue = Options.TicketDataFormat.Protect(ticket);

                Options.CookieManager.AppendResponseCookie(
                    Context,
                    Options.CookieName,
                    cookieValue,
                    signInContext.CookieOptions);

                var signedInContext = new CookieSignedInContext(
                    Context,
                    Options,
                    Options.AuthenticationScheme,
                    signInContext.Principal,
                    signInContext.Properties);

                await Options.Events.SignedIn(signedInContext);

                // Only redirect on the login path
                var shouldRedirect = Options.LoginPath.HasValue && OriginalPath == Options.LoginPath;
                await ApplyHeaders(shouldRedirect);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.SignIn, exception, ticket);
                await Options.Events.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
        }

        protected override async Task HandleSignOutAsync(SignOutContext signOutContext)
        {
            var ticket = await EnsureCookieTicket();
            try
            {
                var cookieOptions = BuildCookieOptions();
                if (Options.SessionStore != null && _sessionKey != null)
                {
                    await Options.SessionStore.RemoveAsync(_sessionKey);
                }

                var context = new CookieSigningOutContext(
                    Context,
                    Options,
                    cookieOptions);

                await Options.Events.SigningOut(context);

                Options.CookieManager.DeleteCookie(
                    Context,
                    Options.CookieName,
                    context.CookieOptions);

                // Only redirect on the logout path
                var shouldRedirect = Options.LogoutPath.HasValue && OriginalPath == Options.LogoutPath;
                await ApplyHeaders(shouldRedirect);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.SignOut, exception, ticket);
                await Options.Events.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
        }

        private async Task ApplyHeaders(bool shouldRedirectToReturnUrl)
        {
            Response.Headers[HeaderNames.CacheControl] = HeaderValueNoCache;
            Response.Headers[HeaderNames.Pragma] = HeaderValueNoCache;
            Response.Headers[HeaderNames.Expires] = HeaderValueMinusOne;
            if (shouldRedirectToReturnUrl && Response.StatusCode == 200)
            {
                var query = Request.Query;
                var redirectUri = query[Options.ReturnUrlParameter];
                if (!StringValues.IsNullOrEmpty(redirectUri)
                    && IsHostRelative(redirectUri))
                {
                    var redirectContext = new CookieRedirectContext(Context, Options, redirectUri);
                    await Options.Events.RedirectToReturnUrl(redirectContext);
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

        protected async override Task<bool> HandleForbiddenAsync(ChallengeContext context)
        {
            try
            {
                var accessDeniedUri =
                    Request.Scheme +
                    "://" +
                    Request.Host +
                    OriginalPathBase +
                    Options.AccessDeniedPath;

                var redirectContext = new CookieRedirectContext(Context, Options, accessDeniedUri);
                await Options.Events.RedirectToAccessDenied(redirectContext);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.Forbidden, exception, ticket: null);
                await Options.Events.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
            return true;
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var redirectUri = new AuthenticationProperties(context.Properties).RedirectUri;
            try
            {
                if (string.IsNullOrEmpty(redirectUri))
                {
                    redirectUri = OriginalPathBase + Request.Path + Request.QueryString;
                }

                var loginUri = Options.LoginPath + QueryString.Create(Options.ReturnUrlParameter, redirectUri);
                var redirectContext = new CookieRedirectContext(Context, Options, BuildRedirectUri(loginUri));
                await Options.Events.RedirectToLogin(redirectContext);
            }
            catch (Exception exception)
            {
                var exceptionContext = new CookieExceptionContext(Context, Options,
                    CookieExceptionContext.ExceptionLocation.Unauthorized, exception, ticket: null);
                await Options.Events.Exception(exceptionContext);
                if (exceptionContext.Rethrow)
                {
                    throw;
                }
            }
            return true;
        }
    }
}
