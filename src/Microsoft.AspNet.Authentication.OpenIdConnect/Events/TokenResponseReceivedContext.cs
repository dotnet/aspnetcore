using Microsoft.AspNet.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// This Context can be used to be informed when an 'AuthorizationCode' is redeemed for tokens at the token endpoint.
    /// </summary>
    public class TokenResponseReceivedContext : BaseOpenIdConnectContext
    {
        /// <summary>
        /// Creates a <see cref="TokenResponseReceivedContext"/>
        /// </summary>
        public TokenResponseReceivedContext(HttpContext context, OpenIdConnectOptions options)
            : base(context, options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/> that contains the tokens received after redeeming the code at the token endpoint.
        /// </summary>
        public OpenIdConnectMessage TokenEndpointResponse { get; set; }
    }
}
