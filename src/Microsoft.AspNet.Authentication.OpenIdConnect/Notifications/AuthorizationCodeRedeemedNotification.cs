using Microsoft.AspNet.Http;
using Microsoft.IdentityModel.Protocols;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// This Notification can be used to be informed when an 'AuthorizationCode' is redeemed for tokens at the token endpoint.
    /// </summary>
    public class AuthorizationCodeRedeemedNotification : BaseNotification<OpenIdConnectAuthenticationOptions>
    {
        /// <summary>
        /// Creates a <see cref="AuthorizationCodeRedeemedNotification"/>
        /// </summary>
        public AuthorizationCodeRedeemedNotification(HttpContext context, OpenIdConnectAuthenticationOptions options) : base(context, options)
        {
        }

        /// <summary>
        /// Gets or sets the 'code'.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectTokenEndpointResponse"/> that contains the tokens and json response received after redeeming the code at the token endpoint.
        /// </summary>
        public OpenIdConnectTokenEndpointResponse TokenEndpointResponse { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
        /// </summary>
        public OpenIdConnectMessage ProtocolMessage { get; set; }

    }
}
