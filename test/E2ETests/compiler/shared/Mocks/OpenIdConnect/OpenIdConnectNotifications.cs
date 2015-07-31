#if TESTING
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.OpenIdConnect
{
    internal class OpenIdConnectNotifications
    {
        private static List<string> notificationsFired = new List<string>();

        internal static Task MessageReceived(MessageReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            Helpers.ThrowIfConditionFailed(() => context.ProtocolMessage != null, "ProtocolMessage is null.");
            notificationsFired.Add(nameof(MessageReceived));
            return Task.FromResult(0);
        }

        internal static Task SecurityTokenReceived(SecurityTokenReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            notificationsFired.Add(nameof(SecurityTokenReceived));
            return Task.FromResult(0);
        }

        internal static Task SecurityTokenValidated(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            Helpers.ThrowIfConditionFailed(() => context.AuthenticationTicket != null, "context.AuthenticationTicket is null.");
            Helpers.ThrowIfConditionFailed(() => context.AuthenticationTicket.Principal != null, "context.AuthenticationTicket.Principal is null.");
            Helpers.ThrowIfConditionFailed(() => context.AuthenticationTicket.Principal.Identity != null, "context.AuthenticationTicket.Principal.Identity is null.");
            Helpers.ThrowIfConditionFailed(() => !string.IsNullOrWhiteSpace(context.AuthenticationTicket.Principal.Identity.Name), "context.AuthenticationTicket.Principal.Identity.Name is null.");
            notificationsFired.Add(nameof(SecurityTokenValidated));
            return Task.FromResult(0);
        }

        internal static Task AuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            Helpers.ThrowIfConditionFailed(() => context.Code == "AAABAAAAvPM1KaPlrEqdFSBzjqfTGGBtrTYVn589oKw4lLgJ6Svz0AhPVOJr0J2-Uu_KffGlqIbYlRAyxmt-vZ7VlSVdrWvOkNhK9OaAMaSD7LDoPbBTVMEkB0MdAgBTV34l2el-s8ZI02_9PvgQaORZs7n8eGaGbcoKAoxiDn2OcKuJVplXYgrGUwU4VpRaqe6RaNzuseM7qBFbLIv4Wps8CndE6W8ccmuu6EvGC6-H4uF9EZL7gU4nEcTcvkE4Qyt8do6VhTVfM1ygRNQgmV1BCig5t_5xfhL6-xWQdy15Uzn_Df8VSsyDXe8s9cxyKlqc_AIyLFy_NEiMQFUqjZWKd_rR3A8ugug15SEEGuo1kF3jMc7dVMdE6OF9UBd-Ax5ILWT7V4clnRQb6-CXB538DlolREfE-PowXYruFBA-ARD6rwAVtuVfCSbS0Zr4ZqfNjt6x8yQdK-OkdQRZ1thiZcZlm1lyb2EquGZ8Deh2iWBoY1uNcyjzhG-L43EivxtHAp6Y8cErhbo41iacgqOycgyJWxiB5J0HHkxD0nQ2RVVuY8Ybc9sdgyfKkkK2wZ3idGaRCdZN8Q9VBhWRXPDMqHWG8t3aZRtvJ_Xd3WhjNPJC0GpepUGNNQtXiEoIECC363o1z6PZC5-E7U3l9xK06BZkcfTOnggUiSWNCrxUKS44dNqaozdYlO5E028UgAEhJ4eDtcP3PZty-0j4j5Mw0F2FmyAA",
                "context.Code is invalid.");
            notificationsFired.Add(nameof(AuthorizationCodeReceived));

            // Verify all notifications are fired.
            if (notificationsFired.Contains(nameof(RedirectToIdentityProvider)) &&
                notificationsFired.Contains(nameof(MessageReceived)) &&
                notificationsFired.Contains(nameof(SecurityTokenReceived)) &&
                notificationsFired.Contains(nameof(SecurityTokenValidated)) &&
                notificationsFired.Contains(nameof(AuthorizationCodeReceived)))
            {
                ((ClaimsIdentity)context.AuthenticationTicket.Principal.Identity).AddClaim(new Claim("ManageStore", "Allowed"));
            }

            return Task.FromResult(0);
        }

        internal static Task RedirectToIdentityProvider
            (RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            notificationsFired.Add(nameof(RedirectToIdentityProvider));

            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.LogoutRequest)
            {
                context.ProtocolMessage.PostLogoutRedirectUri =
                    context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase + new PathString("/Account/Login");
            }

            return Task.FromResult(0);
        }
    }
}
#endif