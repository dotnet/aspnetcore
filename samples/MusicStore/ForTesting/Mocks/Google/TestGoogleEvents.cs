using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Google
{
    internal class TestGoogleEvents
    {
        internal static Task OnCreatingTicket(OAuthCreatingTicketContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "Access token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.RefreshToken == "ValidRefreshToken", "Refresh token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst(ClaimTypes.Email)?.Value == "AspnetvnextTest@gmail.com", "Email is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst(ClaimTypes.NameIdentifier)?.Value == "106790274378320830963", "Id is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst(ClaimTypes.Surname)?.Value == "AspnetvnextTest", "FamilyName is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst(ClaimTypes.Name)?.Value == "AspnetvnextTest AspnetvnextTest", "Name is not valid");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(1200), "ExpiresIn is not valid");
                Helpers.ThrowIfConditionFailed(() => context.User != null, "User object is not valid");
                context.Principal.Identities.First().AddClaim(new Claim("ManageStore", "false"));
            }

            return Task.FromResult(0);
        }

        internal static Task OnTicketReceived(TicketReceivedContext context)
        {
            if (context.Principal != null && context.Options.SignInScheme == IdentityConstants.ExternalScheme)
            {
                //This way we will know all events were fired.
                var identity = context.Principal.Identities.First();
                var manageStoreClaim = identity?.Claims.Where(c => c.Type == "ManageStore" && c.Value == "false").FirstOrDefault();
                if (manageStoreClaim != null)
                {
                    identity.RemoveClaim(manageStoreClaim);
                    identity.AddClaim(new Claim("ManageStore", "Allowed"));
                }
            }

            return Task.FromResult(0);
        }

        internal static Task RedirectToAuthorizationEndpoint(RedirectContext<OAuthOptions> context)
        {
            context.Response.Redirect(context.RedirectUri + "&custom_redirect_uri=custom");
            return Task.FromResult(0);
        }
    }
} 
