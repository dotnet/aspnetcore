#if TESTING
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.MicrosoftAccount;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.MicrosoftAccount
{
    internal class TestMicrosoftAccountEvents
    {
        internal static Task OnCreatingTicket(OAuthCreatingTicketContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "Access token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.RefreshToken == "ValidRefreshToken", "Refresh token is not valid");
                Helpers.ThrowIfConditionFailed(() => MicrosoftAccountHelper.GetFirstName(context.User) == "AspnetvnextTest", "Email is not valid");
                Helpers.ThrowIfConditionFailed(() => MicrosoftAccountHelper.GetLastName(context.User) == "AspnetvnextTest", "Email is not valid");
                Helpers.ThrowIfConditionFailed(() => MicrosoftAccountHelper.GetId(context.User) == "fccf9a24999f4f4f", "Id is not valid");
                Helpers.ThrowIfConditionFailed(() => MicrosoftAccountHelper.GetName(context.User) == "AspnetvnextTest AspnetvnextTest", "Name is not valid");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(3600), "ExpiresIn is not valid");
                Helpers.ThrowIfConditionFailed(() => context.User != null, "User object is not valid");
                Helpers.ThrowIfConditionFailed(() => MicrosoftAccountHelper.GetId(context.User) == context.User.SelectToken("id").ToString(), "User id is not valid");
                context.Principal.Identities.First().AddClaim(new Claim("ManageStore", "false"));
            }

            return Task.FromResult(0);
        }

        internal static Task OnTicketReceived(TicketReceivedContext context)
        {
            if (context.Principal != null && context.Options.SignInScheme == new IdentityCookieOptions().ExternalCookieAuthenticationScheme)
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

        internal static Task RedirectToAuthorizationEndpoint(OAuthRedirectToAuthorizationContext context)
        {
            context.Response.Redirect(context.RedirectUri + "&custom_redirect_uri=custom");
            return Task.FromResult(0);
        }
    }
} 
#endif