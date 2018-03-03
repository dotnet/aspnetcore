// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

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
