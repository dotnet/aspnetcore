#if TESTING
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.Twitter;
using Microsoft.AspNet.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Twitter
{
    /// <summary>
    /// Summary description for TwitterNotifications
    /// </summary>
    internal class TwitterNotifications
    {
        internal static async Task OnAuthenticated(TwitterAuthenticatedContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.UserId == "valid_user_id", "UserId is not valid");
                Helpers.ThrowIfConditionFailed(() => context.ScreenName == "valid_screen_name", "ScreenName is not valid");
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "valid_oauth_token", "AccessToken is not valid");
                Helpers.ThrowIfConditionFailed(() => context.AccessTokenSecret == "valid_oauth_token_secret", "AccessTokenSecret is not valid");
                context.Principal.Identities.First().AddClaim(new Claim("ManageStore", "false"));
            }

            await Task.FromResult(0);
        }

        internal static async Task OnReturnEndpoint(TwitterReturnEndpointContext context)
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

        internal static void OnApplyRedirect(TwitterApplyRedirectContext context)
        {
            context.Response.Redirect(context.RedirectUri + "&custom_redirect_uri=custom");
        }
    }
} 
#endif