// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.UrlActions
{
    public class VoidAction : UrlAction
    {
        // Explicitly say that nothing happens
        public override RuleResult ApplyAction(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            return RuleResult.Continue;
        }
    }
}
