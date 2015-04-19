#if TESTING
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.MicrosoftAccount;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.MicrosoftAccount
{
    /// <summary>
    /// Summary description for MicrosoftAccountNotifications
    /// </summary>
    internal class MicrosoftAccountNotifications
    {
        internal static async Task OnAuthenticated(MicrosoftAccountAuthenticatedContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "ValidAccessToken", "Access token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.RefreshToken == "ValidRefreshToken", "Refresh token is not valid");
                Helpers.ThrowIfConditionFailed(() => context.FirstName == "AspnetvnextTest", "Email is not valid");
                Helpers.ThrowIfConditionFailed(() => context.LastName == "AspnetvnextTest", "Email is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Id == "fccf9a24999f4f4f", "Id is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Name == "AspnetvnextTest AspnetvnextTest", "Name is not valid");
                Helpers.ThrowIfConditionFailed(() => context.ExpiresIn.Value == TimeSpan.FromSeconds(3600), "ExpiresIn is not valid");
                Helpers.ThrowIfConditionFailed(() => context.User != null, "User object is not valid");
                Helpers.ThrowIfConditionFailed(() => context.Id == context.User.SelectToken("id").ToString(), "User id is not valid");
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