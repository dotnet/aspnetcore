#if TESTING
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.Facebook;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Facebook
{
    /// <summary>
    /// Summary description for FacebookNotifications
    /// </summary>
    internal class FacebookNotifications
    {
        internal static async Task OnAuthenticated(OAuthAuthenticatedContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "");
                Helpers.ThrowIfConditionFailed(() => FacebookAuthenticationHelper.GetEmail(context.User) == "AspnetvnextTest@test.com", "");
                Helpers.ThrowIfConditionFailed(() => FacebookAuthenticationHelper.GetId(context.User) == "Id", "");
                Helpers.ThrowIfConditionFailed(() => FacebookAuthenticationHelper.GetLink(context.User) == "https://www.facebook.com/myLink", "");
                Helpers.ThrowIfConditionFailed(() => FacebookAuthenticationHelper.GetName(context.User) == "AspnetvnextTest AspnetvnextTest", "");
                Helpers.ThrowIfConditionFailed(() => FacebookAuthenticationHelper.GetUserName(context.User) == "AspnetvnextTest.AspnetvnextTest.7", "");
                Helpers.ThrowIfConditionFailed(() => context.User.SelectToken("id").ToString() == FacebookAuthenticationHelper.GetId(context.User), "");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(100), "");
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "");
                context.Principal.Identities.First().AddClaim(new Claim("ManageStore", "false"));
            }

            await Task.FromResult(0);
        }

        internal static async Task OnReturnEndpoint(OAuthReturnEndpointContext context)
        {
            if (context.Principal != null && context.SignInScheme == IdentityOptions.ExternalCookieAuthenticationScheme)
            {
                //This way we will know all notifications were fired.
                var identity = context.Principal.Identities.First();
                var manageStoreClaim = identity?.Claims.Where(c => c.Type == "ManageStore" && c.Value == "false").FirstOrDefault();
                if (manageStoreClaim != null)
                {
                    identity.RemoveClaim(manageStoreClaim);
                    identity.AddClaim(new Claim("ManageStore", "Allowed"));
                }
            }

            await Task.FromResult(0);
        }

        internal static void OnApplyRedirect(OAuthApplyRedirectContext context)
        {
            context.Response.Redirect(context.RedirectUri + "&custom_redirect_uri=custom");
        }
    }
} 
#endif