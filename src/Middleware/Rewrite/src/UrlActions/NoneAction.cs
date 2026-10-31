// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.UrlActions;

internal sealed class NoneAction : UrlAction
{
    public RuleResult Result { get; }

    public NoneAction(RuleResult result)
    {
        Result = result;
    }
    // Explicitly say that nothing happens
    public override void ApplyAction(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        context.Result = Result;
    }
}
