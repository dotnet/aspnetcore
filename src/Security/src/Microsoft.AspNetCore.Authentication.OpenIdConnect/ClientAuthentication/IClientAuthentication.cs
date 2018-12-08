using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// Configure the client authentication mode to call access_token endpoint
    /// </summary>
    public interface IClientAuthentication
    {
        /// <summary>
        /// Adapt the request for the client authentication
        /// </summary>
        /// <param name="message"></param>
        /// <param name="tokenEndpointRequest"></param>
        /// <returns></returns>
        HttpRequestMessage SetClientAuthentication(HttpRequestMessage message, OpenIdConnectMessage tokenEndpointRequest);
    }
}
