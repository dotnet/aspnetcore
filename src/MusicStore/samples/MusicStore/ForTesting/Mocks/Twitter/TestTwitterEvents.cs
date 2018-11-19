using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Identity;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Twitter
{
    internal class TestTwitterEvents
    {
        internal static Task OnCreatingTicket(TwitterCreatingTicketContext context)
        {
            if (context.Principal != null)
            {
                Helpers.ThrowIfConditionFailed(() => context.UserId == "valid_user_id", "UserId is not valid");
                Helpers.ThrowIfConditionFailed(() => context.ScreenName == "valid_screen_name", "ScreenName is not valid");
                Helpers.ThrowIfConditionFailed(() => context.AccessToken == "valid_oauth_token", "AccessToken is not valid");
                Helpers.ThrowIfConditionFailed(() => context.AccessTokenSecret == "valid_oauth_token_secret", "AccessTokenSecret is not valid");
                context.Principal.Identities.First().AddClaim(new Claim("ManageStore", "false"));
            }

            return Task.FromResult(0);
        }

        internal static Task OnTicketReceived(TicketReceivedContext context)
        {
            if (context.Principal != null && context.Options.SignInScheme == IdentityConstants.ExternalScheme)
            {
                //This way we will know all Events were fired.
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

        internal static Task RedirectToAuthorizationEndpoint(RedirectContext<TwitterOptions> context)
        {
            context.Response.Redirect(context.RedirectUri + "&custom_redirect_uri=custom");
            return Task.FromResult(0);
        }
    }
} 
