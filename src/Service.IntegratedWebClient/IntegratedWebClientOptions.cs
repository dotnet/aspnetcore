// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;

namespace Microsoft.AspNetCore.Identity.Service.IntegratedWebClient
{
    public class IntegratedWebClientOptions
    {
        public string ClientId { get; set; }
        public string TokenRedirectUrn { get; set; } = "urn:self:aspnet:identity:integrated";
        public string MetadataUri { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public string EndsSessionEndpoint { get; set; }
        public string CookieSignInScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;
    }
}
