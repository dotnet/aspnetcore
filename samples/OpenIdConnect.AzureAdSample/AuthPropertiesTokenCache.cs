using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace OpenIdConnect.AzureAdSample
{
    public class AuthPropertiesTokenCache : TokenCache
    {
        private const string TokenCacheKey = ".TokenCache";

        private HttpContext _httpContext;
        private ClaimsPrincipal _principal;
        private AuthenticationProperties _authProperties;
        private string _signInScheme;

        private AuthPropertiesTokenCache(AuthenticationProperties authProperties) : base()
        {
            _authProperties = authProperties;
            BeforeAccess = BeforeAccessNotificationWithProperties;
            AfterAccess = AfterAccessNotificationWithProperties;
            BeforeWrite = BeforeWriteNotification;
        }

        private AuthPropertiesTokenCache(HttpContext httpContext, string signInScheme) : base()
        {
            _httpContext = httpContext;
            _signInScheme = signInScheme;
            BeforeAccess = BeforeAccessNotificationWithContext;
            AfterAccess = AfterAccessNotificationWithContext;
            BeforeWrite = BeforeWriteNotification;
        }

        public static TokenCache ForCodeRedemption(AuthenticationProperties authProperties)
        {
            return new AuthPropertiesTokenCache(authProperties);
        }

        public static TokenCache ForApiCalls(HttpContext httpContext,
            string signInScheme = CookieAuthenticationDefaults.AuthenticationScheme)
        {
            return new AuthPropertiesTokenCache(httpContext, signInScheme);
        }

        private void BeforeAccessNotificationWithProperties(TokenCacheNotificationArgs args)
        {
            string cachedTokensText;
            if (_authProperties.Items.TryGetValue(TokenCacheKey, out cachedTokensText))
            {
                var cachedTokens = Convert.FromBase64String(cachedTokensText);
                Deserialize(cachedTokens);
            }
        }

        private void BeforeAccessNotificationWithContext(TokenCacheNotificationArgs args)
        {
            // Retrieve the auth session with the cached tokens
            var result = _httpContext.AuthenticateAsync(_signInScheme).Result;
            _authProperties = result.Ticket.Properties;
            _principal = result.Ticket.Principal;

            BeforeAccessNotificationWithProperties(args);
        }

        private void AfterAccessNotificationWithProperties(TokenCacheNotificationArgs args)
        {
            // if state changed
            if (HasStateChanged)
            {
                var cachedTokens = Serialize();
                var cachedTokensText = Convert.ToBase64String(cachedTokens);
                _authProperties.Items[TokenCacheKey] = cachedTokensText;
            }
        }

        private void AfterAccessNotificationWithContext(TokenCacheNotificationArgs args)
        {
            // if state changed
            if (HasStateChanged)
            {
                AfterAccessNotificationWithProperties(args);

                var cachedTokens = Serialize();
                var cachedTokensText = Convert.ToBase64String(cachedTokens);
                _authProperties.Items[TokenCacheKey] = cachedTokensText;
                _httpContext.SignInAsync(_signInScheme, _principal, _authProperties).Wait();
            }
        }

        private void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        }
    }
}
