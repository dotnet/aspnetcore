// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    public class OAuthTokenResponse
    {
        private OAuthTokenResponse(JObject response)
        {
            Response = response;
            AccessToken = response.Value<string>("access_token");
            TokenType = response.Value<string>("token_type");
            RefreshToken = response.Value<string>("refresh_token");
            ExpiresIn = response.Value<string>("expires_in");
        }

        private OAuthTokenResponse(Exception error)
        {
            Error = error;
        }

        public static OAuthTokenResponse Success(JObject response)
        {
            return new OAuthTokenResponse(response);
        }

        public static OAuthTokenResponse Failed(Exception error)
        {
            return new OAuthTokenResponse(error);
        }

        public JObject Response { get; set; }
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
        public Exception Error { get; set; }
    }
}