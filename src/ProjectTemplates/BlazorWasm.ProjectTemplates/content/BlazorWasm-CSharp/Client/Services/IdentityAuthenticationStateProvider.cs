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
        private readonly HttpClient _client;
        private ClaimsPrincipal _currentUser;

        public IdentityAuthenticationStateProvider(HttpClient client)
        {
            _client = client;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_currentUser == null)
            {
                await FetchUser();
            }

            return new AuthenticationState(_currentUser);
        }

        private async Task FetchUser()
        {
            var user = await _client.GetJsonAsync<UserInfo>("/User");

            if (!user.IsAuthenticated)
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                return;
            }

            var identity = new ClaimsIdentity(
                "BlazorWasm_CSharp",
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
