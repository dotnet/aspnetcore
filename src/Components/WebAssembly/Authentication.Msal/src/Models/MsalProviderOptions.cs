// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Authentication.WebAssembly.Msal.Models
{
    public class MsalProviderOptions
    {
        [JsonPropertyName("auth")]
        public MsalAuthenticationOptions Authentication { get; set; } = new MsalAuthenticationOptions
        {
            RedirectUri = "authentication/login-callback",
            PostLogoutRedirectUri = "authentication/logout-callback"
        };

        [JsonPropertyName("cache")]
        public MsalCacheOptions Cache { get; set; }

        public IList<string> DefaultAccessTokenScopes { get; set; } = new List<string>();
    }
}
