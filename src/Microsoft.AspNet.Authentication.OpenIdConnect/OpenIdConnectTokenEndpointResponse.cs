// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
            ProtocolMessage = new OpenIdConnectMessage()
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
        public OpenIdConnectMessage ProtocolMessage { get; set; }

        /// <summary>
        /// Json response returned from the token endpoint
        /// </summary>
        public JObject JsonResponse { get; set; }
    }
}
