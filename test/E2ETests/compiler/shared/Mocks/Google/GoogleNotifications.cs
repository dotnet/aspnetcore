using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Security.Google;
using Microsoft.AspNet.Security.OAuth;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Google
{
    /// <summary>
    /// Summary description for GoogleNotifications
    /// </summary>
    internal class GoogleNotifications
    {
        internal static async Task OnAuthenticated(GoogleAuthenticatedContext context)
        {
            if (context.Identity != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "Access token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.RefreshToken == "ValidRefreshToken", "Refresh token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Email == "AspnetvnextTest@gmail.com", "Email is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Id == "106790274378320830963", "Id is not valid");
                Helpers.ThrowIfConditionFailed(() => context.FamilyName == "AspnetvnextTest", "FamilyName is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Name == "AspnetvnextTest AspnetvnextTest", "Name is not valid");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(1200), "ExpiresIn is not valid");
                Helpers.ThrowIfConditionFailed(() => context.User != null, "User object is not valid");
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