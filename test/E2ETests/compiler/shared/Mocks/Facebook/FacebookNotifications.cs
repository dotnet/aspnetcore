using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Security.Facebook;
using Microsoft.AspNet.Security.OAuth;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Facebook
{
    /// <summary>
    /// Summary description for FacebookNotifications
    /// </summary>
    internal class FacebookNotifications
    {
        internal static async Task OnAuthenticated(FacebookAuthenticatedContext context)
        {
            if (context.Identity != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "");
                Helpers.ThrowIfConditionFailed(() => context.Email == "AspnetvnextTest@test.com", "");
                Helpers.ThrowIfConditionFailed(() => context.Id == "Id", "");
                Helpers.ThrowIfConditionFailed(() => context.Link == "https://www.facebook.com/myLink", "");
                Helpers.ThrowIfConditionFailed(() => context.Name == "AspnetvnextTest AspnetvnextTest", "");
                Helpers.ThrowIfConditionFailed(() => context.UserName == "AspnetvnextTest.AspnetvnextTest.7", "");
                Helpers.ThrowIfConditionFailed(() => context.User.SelectToken("id").ToString() == context.Id, "");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(100), "");
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "");
                context.Identity.AddClaim(new Claim("ManageStore", "false"));
            }

            await Task.FromResult(0);
        }

        internal static async Task OnReturnEndpoint(OAuthReturnEndpointContext context)
        {
            if (context.Identity != null && context.SignInAsAuthenticationType == IdentityOptions.ExternalCookieAuthenticationType)
            {
                //This way we will know all notifications were fired.
                var manageStoreClaim = context.Identity.Claims.Where(c => c.Type == "ManageStore" && c.Value == "false").FirstOrDefault();
                if (manageStoreClaim != null)
                {
                    context.Identity.RemoveClaim(manageStoreClaim);
                    context.Identity.AddClaim(new Claim("ManageStore", "Allowed"));
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