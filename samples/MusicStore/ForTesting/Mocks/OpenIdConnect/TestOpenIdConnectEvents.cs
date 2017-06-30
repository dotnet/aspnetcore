using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.OpenIdConnect
{
    internal class TestOpenIdConnectEvents
    {
        private static List<string> eventsFired = new List<string>();

        internal static Task MessageReceived(MessageReceivedContext context)
        {
            Helpers.ThrowIfConditionFailed(() => context.ProtocolMessage != null, "ProtocolMessage is null.");
            eventsFired.Add(nameof(MessageReceived));
            return Task.FromResult(0);
        }

        internal static Task TokenValidated(TokenValidatedContext context)
        {
            Helpers.ThrowIfConditionFailed(() => context.Principal != null, "context.Principal is null.");
            Helpers.ThrowIfConditionFailed(() => context.Principal.Identity != null, "context.Principal.Identity is null.");
            Helpers.ThrowIfConditionFailed(() => !string.IsNullOrWhiteSpace(context.Principal.Identity.Name), "context.Principal.Identity.Name is null.");
            eventsFired.Add(nameof(TokenValidated));
            return Task.FromResult(0);
        }

        internal static Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            Helpers.ThrowIfConditionFailed(() => context.TokenEndpointRequest.Code == "AAABAAAAvPM1KaPlrEqdFSBzjqfTGGBtrTYVn589oKw4lLgJ6Svz0AhPVOJr0J2-Uu_KffGlqIbYlRAyxmt-vZ7VlSVdrWvOkNhK9OaAMaSD7LDoPbBTVMEkB0MdAgBTV34l2el-s8ZI02_9PvgQaORZs7n8eGaGbcoKAoxiDn2OcKuJVplXYgrGUwU4VpRaqe6RaNzuseM7qBFbLIv4Wps8CndE6W8ccmuu6EvGC6-H4uF9EZL7gU4nEcTcvkE4Qyt8do6VhTVfM1ygRNQgmV1BCig5t_5xfhL6-xWQdy15Uzn_Df8VSsyDXe8s9cxyKlqc_AIyLFy_NEiMQFUqjZWKd_rR3A8ugug15SEEGuo1kF3jMc7dVMdE6OF9UBd-Ax5ILWT7V4clnRQb6-CXB538DlolREfE-PowXYruFBA-ARD6rwAVtuVfCSbS0Zr4ZqfNjt6x8yQdK-OkdQRZ1thiZcZlm1lyb2EquGZ8Deh2iWBoY1uNcyjzhG-L43EivxtHAp6Y8cErhbo41iacgqOycgyJWxiB5J0HHkxD0nQ2RVVuY8Ybc9sdgyfKkkK2wZ3idGaRCdZN8Q9VBhWRXPDMqHWG8t3aZRtvJ_Xd3WhjNPJC0GpepUGNNQtXiEoIECC363o1z6PZC5-E7U3l9xK06BZkcfTOnggUiSWNCrxUKS44dNqaozdYlO5E028UgAEhJ4eDtcP3PZty-0j4j5Mw0F2FmyAA",
                "context.TokenEndpointRequest.Code is invalid.");
            eventsFired.Add(nameof(AuthorizationCodeReceived));

            // Verify all events are fired.
            if (eventsFired.Contains(nameof(RedirectToIdentityProvider)) &&
                eventsFired.Contains(nameof(MessageReceived)) &&
                eventsFired.Contains(nameof(TokenValidated)) &&
                eventsFired.Contains(nameof(AuthorizationCodeReceived)))
            {
                ((ClaimsIdentity)context.Principal.Identity).AddClaim(new Claim("ManageStore", "Allowed"));
            }

            return Task.FromResult(0);
        }

        internal static Task RedirectToIdentityProvider(RedirectContext context)
        {
            eventsFired.Add(nameof(RedirectToIdentityProvider));

            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
            {
                context.ProtocolMessage.PostLogoutRedirectUri =
                    context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase + new PathString("/Account/Login");
            }

            return Task.FromResult(0);
        }
    }
}
