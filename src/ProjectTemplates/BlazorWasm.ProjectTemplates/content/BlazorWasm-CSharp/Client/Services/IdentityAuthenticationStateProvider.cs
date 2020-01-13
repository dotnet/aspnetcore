using BlazorWasm_CSharp.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlazorWasm_CSharp.Client
{
    public class IdentityAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser;

        public IdentityAuthenticationStateProvider(HttpClient client)
        {
            Client = client;
        }

        public HttpClient Client { get; }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_currentUser != null)
            {
                return new AuthenticationState(_currentUser);
            }

            await UpdateUser();

            return new AuthenticationState(_currentUser);
        }

        private async Task UpdateUser()
        {
            var user = await Client.GetJsonAsync<UserInfo>("/User");

            if (user.AuthenticationType == null)
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                return;
            }

            var identity = new ClaimsIdentity(
                user.AuthenticationType,
                user.NameClaimType,
                user.RoleClaimType);

            if (user.Claims != null)
            {
                foreach (var claim in user.Claims)
                {
                    identity.AddClaim(new Claim(claim.Type, claim.Value));
                }
            }

            _currentUser = new ClaimsPrincipal(identity);
        }
    }
}
