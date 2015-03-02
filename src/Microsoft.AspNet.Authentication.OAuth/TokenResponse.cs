// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.OAuth
{
    public class TokenResponse
    {
        public TokenResponse(JObject response)
        {
            Response = response;
            AccessToken = response.Value<string>("access_token");
            TokenType = response.Value<string>("token_type");
            RefreshToken = response.Value<string>("refresh_token");
            ExpiresIn = response.Value<string>("expires_in");
        }

        public JObject Response { get; set; }
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
    }
}