using System;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace OpenIdConnect.AzureAdSample
{
    public class AuthPropertiesTokenCache : TokenCache
    {
        private const string TokenCacheKey = ".TokenCache";

        private AuthenticationProperties _authProperties;

        public bool HasCacheChanged { get; internal set; }

        public AuthPropertiesTokenCache(AuthenticationProperties authProperties) : base()
        {
            _authProperties = authProperties;
            BeforeAccess = BeforeAccessNotification;
            AfterAccess = AfterAccessNotification;
            BeforeWrite = BeforeWriteNotification;

            string cachedTokensText;
            if (authProperties.Items.TryGetValue(TokenCacheKey, out cachedTokensText))
            {
                var cachedTokens = Convert.FromBase64String(cachedTokensText);
                Deserialize(cachedTokens);
            }
        }

        // Notification raised before ADAL accesses the cache.
        // This is your chance to update the in-memory copy from the DB, if the in-memory version is stale
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {

        }

        // Notification raised after ADAL accessed the cache.
        // If the HasStateChanged flag is set, ADAL changed the content of the cache
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if state changed
            if (HasStateChanged)
            {
                HasCacheChanged = true;
                var cachedTokens = Serialize();
                var cachedTokensText = Convert.ToBase64String(cachedTokens);
                _authProperties.Items[TokenCacheKey] = cachedTokensText;
            }
        }

        private void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        }
    }
}
