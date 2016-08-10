// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class Conditions
    {
        public List<Condition> ConditionList { get; set; } = new List<Condition>();

        public MatchResults Evaluate(RewriteContext context, MatchResults ruleMatch)
        {
            MatchResults prevCond = null;
            var orSucceeded = false;
            foreach (var condition in ConditionList)
            {
                if (orSucceeded && condition.OrNext)
                {
                    continue;
                }
                else if (orSucceeded)
                {
                    orSucceeded = false;
                    continue;
                }

                prevCond = condition.Evaluate(context, ruleMatch, prevCond);

                if (condition.OrNext)
                {
                    orSucceeded = prevCond.Success;
                    continue;
                }
                else if (!prevCond.Success)
                {
                    return prevCond;
                }
            }
            return prevCond;
        }
    }
}
