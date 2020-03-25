// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal static class ConditionEvaluator
    {
        public static MatchResults Evaluate(IEnumerable<Condition> conditions, RewriteContext context, BackReferenceCollection backReferences)
        {
            return Evaluate(conditions, context, backReferences, trackAllCaptures: false);
        }

        public static MatchResults Evaluate(IEnumerable<Condition> conditions, RewriteContext context, BackReferenceCollection backReferences, bool trackAllCaptures)
        {
            BackReferenceCollection prevBackReferences = null;
            MatchResults condResult = null;
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

                condResult = condition.Evaluate(context, backReferences, prevBackReferences);
                var currentBackReferences = condResult.BackReferences;
                if (condition.OrNext)
                {
                    orSucceeded = condResult.Success;
                }
                else if (!condResult.Success)
                {
                    return condResult;
                }

                if (condResult.Success && trackAllCaptures && prevBackReferences != null)
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