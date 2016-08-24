// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public static class ConditionHelper
    {

        public static MatchResults Evaluate(IEnumerable<Condition> conditions, RewriteContext context, MatchResults ruleMatch)
        {
            MatchResults prevCond = null;
            var orSucceeded = false;
            foreach (var condition in conditions)
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
