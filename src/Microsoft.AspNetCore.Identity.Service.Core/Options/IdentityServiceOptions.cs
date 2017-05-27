// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityServiceOptions
    {
        public const string LoginPolicyName = "Microsoft.AspNetCore.Identity.Service.Login";
        public const string SessionPolicyName = "Microsoft.AspNetCore.Identity.Service.Session";
        public const string ManagementPolicyName = "Microsoft.AspNetCore.Identity.Service.Management";
        public const string CookieAuthenticationScheme = "Microsoft.AspNetCore.Identity.Service.Session.Cookies";
        public const string AuthenticationCookieName = "Microsoft.AspNetCore.Identity.Service";

        public string Issuer { get; set; }

        public AuthorizationPolicy LoginPolicy { get; set; }
        public AuthorizationPolicy SessionPolicy { get; set; }
        public AuthorizationPolicy ManagementPolicy { get; set; }

        public IList<SigningCredentials> SigningKeys { get; set; } = new List<SigningCredentials>();

        public TokenOptions AuthorizationCodeOptions { get; set; } = new TokenOptions();

        public TokenOptions AccessTokenOptions { get; set; } = new TokenOptions();

        public TokenOptions RefreshTokenOptions { get; set; } = new TokenOptions();

        public TokenOptions IdTokenOptions { get; set; } = new TokenOptions();

        public JsonSerializerSettings SerializationSettings { get; set; }
    }
}
