using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Facebook
{
    internal class TestFacebookEvents
    {
        internal static Task OnCreatingTicket(OAuthCreatingTicketContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst(ClaimTypes.Email)?.Value == "AspnetvnextTest@test.com", "");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst(ClaimTypes.NameIdentifier)?.Value == "Id", "");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst("urn:facebook:link")?.Value == "https://www.facebook.com/myLink", "");
                Helpers.ThrowIfConditionFailed(() => context.Identity.FindFirst(ClaimTypes.Name)?.Value == "AspnetvnextTest AspnetvnextTest", "");
                Helpers.ThrowIfConditionFailed(() => context.User.SelectToken("id").ToString() == context.Identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, "");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(100), "");
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "");
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
