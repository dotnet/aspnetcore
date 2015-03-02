using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Cookies.Infrastructure;
using Microsoft.Framework.Cache.Memory;

namespace CookieSessionSample
{
    public class MemoryCacheSessionStore : IAuthenticationSessionStore
    {
        private const string KeyPrefix = "AuthSessionStore-";
        private IMemoryCache _cache;

        public MemoryCacheSessionStore()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var guid = Guid.NewGuid();
            var key = KeyPrefix + guid.ToString();
            await RenewAsync(key, ticket);
            return key;
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            _cache.Set(key, ticket, context =>
            {
                var expiresUtc = ticket.Properties.ExpiresUtc;
                if (expiresUtc.HasValue)
                {
                    context.SetAbsoluteExpiration(expiresUtc.Value);
                }
                context.SetSlidingExpiration(TimeSpan.FromHours(1)); // TODO: configurable.

                return (AuthenticationTicket)context.State;
            });
            return Task.FromResult(0);
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            AuthenticationTicket ticket;
            _cache.TryGetValue(key, out ticket);
            return Task.FromResult(ticket);
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.FromResult(0);
        }
    }
}
