#if TESTING
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.Google;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Google
{
    /// <summary>
    /// Summary description for GoogleNotifications
    /// </summary>
    internal class GoogleNotifications
    {
        internal static async Task OnAuthenticated(OAuthAuthenticatedContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "Access token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.RefreshToken == "ValidRefreshToken", "Refresh token is not valid");
                Helpers.ThrowIfConditionFailed(() => GoogleAuthenticationHelper.GetEmail(context.User) == "AspnetvnextTest@gmail.com", "Email is not valid");
                Helpers.ThrowIfConditionFailed(() => GoogleAuthenticationHelper.GetId(context.User) == "106790274378320830963", "Id is not valid");
                Helpers.ThrowIfConditionFailed(() => GoogleAuthenticationHelper.GetFamilyName(context.User) == "AspnetvnextTest", "FamilyName is not valid");
                Helpers.ThrowIfConditionFailed(() => GoogleAuthenticationHelper.GetName(context.User) == "AspnetvnextTest AspnetvnextTest", "Name is not valid");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(1200), "ExpiresIn is not valid");
                Helpers.ThrowIfConditionFailed(() => context.User != null, "User object is not valid");
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