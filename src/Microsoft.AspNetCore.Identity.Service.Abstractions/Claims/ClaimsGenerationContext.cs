// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service.Claims
{
    public class ClaimsGenerationContext
    {
        public string TokenType { get; set; }
        public TokenGeneratingContext GenerationContext { get; set; }
        public IEnumerable<TokenResult> GeneratedTokens { get; set; }
        public IList<Claim> Claims { get; set; }
    }
}
