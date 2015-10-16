#if TESTING
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Facebook;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Identity;
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
                Helpers.ThrowIfConditionFailed(() => FacebookHelper.GetEmail(context.User) == "AspnetvnextTest@test.com", "");
                Helpers.ThrowIfConditionFailed(() => FacebookHelper.GetId(context.User) == "Id", "");
                Helpers.ThrowIfConditionFailed(() => FacebookHelper.GetLink(context.User) == "https://www.facebook.com/myLink", "");
                Helpers.ThrowIfConditionFailed(() => FacebookHelper.GetName(context.User) == "AspnetvnextTest AspnetvnextTest", "");
                Helpers.ThrowIfConditionFailed(() => FacebookHelper.GetUserName(context.User) == "AspnetvnextTest.AspnetvnextTest.7", "");
                Helpers.ThrowIfConditionFailed(() => context.User.SelectToken("id").ToString() == FacebookHelper.GetId(context.User), "");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(100), "");
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "");
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