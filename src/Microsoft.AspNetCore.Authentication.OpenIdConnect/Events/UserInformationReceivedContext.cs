// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class UserInformationReceivedContext : BaseOpenIdConnectContext
    {
        public UserInformationReceivedContext(HttpContext context, OpenIdConnectOptions options)
            : base(context, options)
        {
        }

        public JObject User { get; set; }
    }
}
