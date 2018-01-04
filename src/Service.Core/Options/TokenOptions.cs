// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenOptions
    {
        public TokenMapping UserClaims { get; set; } = new TokenMapping("user");
        public TokenMapping ApplicationClaims { get; set; } = new TokenMapping("application");
        public TokenMapping ContextClaims { get; set; } = new TokenMapping("context");
        public TimeSpan NotValidBefore { get; set; }
        public TimeSpan NotValidAfter { get; set; }
    }
}
