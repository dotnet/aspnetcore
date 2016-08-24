// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class ModRewriteRule : Rule
    {
        public UrlMatch InitialMatch { get; }
        public IList<Condition> Conditions { get; }
        public UrlAction Action { get; }
        public IList<PreAction> PreActions { get; }

        public ModRewriteRule(UrlMatch initialMatch, IList<Condition> conditions, UrlAction urlAction, IList<PreAction> preActions)
        {
            Conditions = conditions;
            InitialMatch = initialMatch;
            Action = urlAction;
            PreActions = preActions;
        }

        public override RuleResult ApplyRule(RewriteContext context)
        {
            // 1. Figure out which section of the string to match for the initial rule.
            var initMatchRes = InitialMatch.Evaluate(context.HttpContext.Request.Path, context);

            if (!initMatchRes.Success)
            {
                context.Logger?.ModRewriteDidNotMatchRule();
                return RuleResult.Continue;
            }

            MatchResults condMatchRes = null;
            if (Conditions != null)
            {
                condMatchRes = ConditionHelper.Evaluate(Conditions, context, initMatchRes);
                if (!condMatchRes.Success)
                {
                    context.Logger?.ModRewriteDidNotMatchRule();
                    return RuleResult.Continue;
                }
            }

            // At this point, we know our rule passed, first apply pre conditions,
            // which can modify things like the cookie or env, and then apply the action
            context.Logger?.ModRewriteMatchedRule();
            foreach (var preAction in PreActions)
            {
                preAction.ApplyAction(context.HttpContext, initMatchRes, condMatchRes);
            }

            return Action.ApplyAction(context, initMatchRes, condMatchRes);
        }
    }
}
