// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite.UrlActions
{
    internal class AbortAction : UrlAction
    {
        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            context.HttpContext.Abort();
            context.Result = RuleResult.EndResponse;
            context.Logger.AbortedRequest(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString);
        }
    }
}
