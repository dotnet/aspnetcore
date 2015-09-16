using Microsoft.AspNet.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// This Context can be used to be informed when an 'AuthorizationCode' is redeemed for tokens at the token endpoint.
    /// </summary>
    public class TokenResponseReceivedContext : BaseControlContext<OpenIdConnectOptions>
    {
        /// <summary>
        /// Creates a <see cref="TokenResponseReceivedContext"/>
        /// </summary>
        public TokenResponseReceivedContext(HttpContext context, OpenIdConnectOptions options)
            : base(context, options)
        {
        }

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
