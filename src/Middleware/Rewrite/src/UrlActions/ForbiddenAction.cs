// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.UrlActions;

internal sealed class ForbiddenAction : UrlAction
{
    public override void ApplyAction(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Result = RuleResult.EndResponse;
    }
}
