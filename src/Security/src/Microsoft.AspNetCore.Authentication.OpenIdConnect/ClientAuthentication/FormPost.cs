using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// Send client id and client secret in the request body 
    /// </summary>
    public class FormPost : IClientAuthentication
    {
        public HttpRequestMessage SetClientAuthentication(HttpRequestMessage message, OpenIdConnectMessage tokenEndpointRequest)
        {
            return message;
        }
    }
}
