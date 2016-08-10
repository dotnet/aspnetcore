// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class UrlRewriteRule : Rule
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public UrlMatch InitialMatch { get; set; }
        public Conditions Conditions { get; set; }
        public UrlAction Action { get; set; }

        public override RuleResult ApplyRule(RewriteContext context)
        {
            if (!Enabled)
            {
                return RuleResult.Continue;
            }

            // Due to the path string always having a leading slash,
            // remove it from the path before regex comparison
            // TODO may need to check if there is a leading slash and remove conditionally
            var initMatchRes = InitialMatch.Evaluate(context.HttpContext.Request.Path.ToString().Substring(1), context);

            if (!initMatchRes.Success)
            {
                return RuleResult.Continue;
            }

            MatchResults condMatchRes = null;
            if (Conditions != null)
            {
                condMatchRes = Conditions.Evaluate(context, initMatchRes);
                if (!condMatchRes.Success)
                {
                    return RuleResult.Continue;
                }
            }

            // at this point we know the rule passed, evaluate the replacement.
            return Action.ApplyAction(context, initMatchRes, condMatchRes);
        }
    }
}