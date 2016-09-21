// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public class GoneAction : UrlAction
    {
        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status410Gone;
            context.Result = RuleResult.EndResponse;
        }
    }
}
