using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// Class to store the response returned from token endpoint
    /// </summary>
    public class OpenIdConnectTokenEndpointResponse
    {
        public OpenIdConnectTokenEndpointResponse(JObject jsonResponse)
        {
            JsonResponse = jsonResponse;
            Message = new OpenIdConnectMessage()
            {
                AccessToken = JsonResponse.Value<string>(OpenIdConnectParameterNames.AccessToken),
                IdToken = JsonResponse.Value<string>(OpenIdConnectParameterNames.IdToken),
                TokenType = JsonResponse.Value<string>(OpenIdConnectParameterNames.TokenType),
                ExpiresIn = JsonResponse.Value<string>(OpenIdConnectParameterNames.ExpiresIn)
            };
        }

        /// <summary>
        /// OpenIdConnect message that contains the id token and access tokens
        /// </summary>
        public OpenIdConnectMessage Message { get; set; }

        /// <summary>
        /// Json response returned from the token endpoint
        /// </summary>
        public JObject JsonResponse { get; set; }
    }
}
