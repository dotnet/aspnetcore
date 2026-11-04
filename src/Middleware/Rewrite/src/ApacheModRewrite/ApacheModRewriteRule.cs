// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

internal sealed class ApacheModRewriteRule : IRule
{
    public UrlMatch InitialMatch { get; }
    public IList<Condition>? Conditions { get; }
    public IList<UrlAction> Actions { get; }

    public ApacheModRewriteRule(UrlMatch initialMatch, IList<Condition>? conditions, IList<UrlAction> urlActions)
    {
        Conditions = conditions;
        InitialMatch = initialMatch;
        Actions = urlActions;
    }

    public void ApplyRule(RewriteContext context)
    {
        // 1. Figure out which section of the string to match for the initial rule.
        var initMatchRes = InitialMatch.Evaluate(context.HttpContext.Request.Path, context);

        if (!initMatchRes.Success)
        {
            context.Logger.ModRewriteNotMatchedRule();
            return;
        }

        BackReferenceCollection? condBackReferences = null;
        if (Conditions != null)
        {
            var condResult = ConditionEvaluator.Evaluate(Conditions, context, initMatchRes.BackReferences);
            if (!condResult.Success)
            {
                context.Logger.ModRewriteNotMatchedRule();
                return;
            }

            condBackReferences = condResult.BackReferences;
        }

        // At this point, we know our rule passed, first apply pre conditions,
        // which can modify things like the cookie or env, and then apply the action
        context.Logger.ModRewriteMatchedRule();

        foreach (var action in Actions)
        {
            action.ApplyAction(context, initMatchRes?.BackReferences, condBackReferences);
        }
    }
}
