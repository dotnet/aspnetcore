// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI
{
    internal class AzureADSchemeOptions
    {
        public IDictionary<string, AzureADOpenIDSchemeMapping> OpenIDMappings { get; set; } = new Dictionary<string, AzureADOpenIDSchemeMapping>();

        public IDictionary<string, JwtBearerSchemeMapping> JwtBearerMappings { get; set; } = new Dictionary<string, JwtBearerSchemeMapping>();

        public class AzureADOpenIDSchemeMapping
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
