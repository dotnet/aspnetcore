// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class RequestGrants
    {
        public string RedirectUri { get; set; }
        public string ResponseMode { get; set; }
        public IList<string> Tokens { get; set; } = new List<string>();
        public IList<ApplicationScope> Scopes { get; set; } = new List<ApplicationScope>();
        public IList<Claim> Claims { get; set; } = new List<Claim>();
    }
}
