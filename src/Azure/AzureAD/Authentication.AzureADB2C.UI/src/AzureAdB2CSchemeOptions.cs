// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    internal class AzureADB2CSchemeOptions
    {
        public IDictionary<string, AzureADB2COpenIDSchemeMapping> OpenIDMappings { get; set; } = new Dictionary<string, AzureADB2COpenIDSchemeMapping>();

        public IDictionary<string, JwtBearerSchemeMapping> JwtBearerMappings { get; set; } = new Dictionary<string, JwtBearerSchemeMapping>();

        public class AzureADB2COpenIDSchemeMapping
        {
            public string OpenIdConnectScheme { get; set; }
            public string CookieScheme { get; set; }
        }

        public class JwtBearerSchemeMapping
        {
            public string JwtBearerScheme { get; set; }
        }
    }
}
