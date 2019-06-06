// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    public class OAuthTokenResponse : IDisposable
    {
        private OAuthTokenResponse(JsonDocument response)
        {
            Response = response;
            var root = response.RootElement;
            AccessToken = root.GetString("access_token");
            TokenType = root.GetString("token_type");
            RefreshToken = root.GetString("refresh_token");
            ExpiresIn = root.GetString("expires_in");
        }

        private OAuthTokenResponse(Exception error)
        {
            Error = error;
        }

        public static OAuthTokenResponse Success(JsonDocument response)
        {
            return new OAuthTokenResponse(response);
        }

        public static OAuthTokenResponse Failed(Exception error)
        {
            return new OAuthTokenResponse(error);
        }

        public void Dispose()
        {
            Response?.Dispose();
        }

        public JsonDocument Response { get; set; }
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
        public Exception Error { get; set; }
    }
}
