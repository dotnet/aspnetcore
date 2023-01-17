// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite.UrlActions;

internal sealed class AbortAction : UrlAction
{
    public override void ApplyAction(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        context.HttpContext.Abort();
        context.Result = RuleResult.EndResponse;
        context.Logger.AbortedRequest(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString);
    }
}
