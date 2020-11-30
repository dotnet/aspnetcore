using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApplication1
{
    public class MemberTicketStore : ITicketStore
    {
        IDictionary<string, AuthenticationTicket> tickets = new Dictionary<string, AuthenticationTicket>();
        public Task RemoveAsync(string key)
        {
            tickets.Remove(key);
            return Task.CompletedTask;
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            tickets[key] = ticket;
            return Task.CompletedTask;
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            var ticket = tickets.TryGetValue(key, out var t) ? t : null;
            return Task.FromResult(ticket);
        }

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = Guid.NewGuid().ToString("N");
            tickets[key] = ticket;
            return Task.FromResult(key);
        }
    }
}
