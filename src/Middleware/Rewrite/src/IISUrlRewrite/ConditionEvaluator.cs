// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal static class ConditionEvaluator
    {
        public static MatchResults Evaluate(ConditionCollection conditions, RewriteContext context, BackReferenceCollection backReferences)
        {
            BackReferenceCollection prevBackReferences = null;
            MatchResults condResult = null;
            var orSucceeded = false;
            foreach (var condition in conditions)
            {
                if (orSucceeded && conditions.Grouping == LogicalGrouping.MatchAny)
                {
                    continue;
                }

                if (orSucceeded)
                {
                    orSucceeded = false;
                    continue;
                }

                condResult = condition.Evaluate(context, backReferences, prevBackReferences);
                var currentBackReferences = condResult.BackReferences;
                if (conditions.Grouping == LogicalGrouping.MatchAny)
                {
                    orSucceeded = condResult.Success;
                }
                else if (!condResult.Success)
                {
                    return condResult;
                }

                if (condResult.Success && conditions.TrackAllCaptures && prevBackReferences!= null)
                {
                    prevBackReferences.Add(currentBackReferences);
                    currentBackReferences = prevBackReferences;
                }

                prevBackReferences = currentBackReferences;
            }

            return new MatchResults { BackReferences = prevBackReferences, Success = condResult.Success };
        }
    }
}