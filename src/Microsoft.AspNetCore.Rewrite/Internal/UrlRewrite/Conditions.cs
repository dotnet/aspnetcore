// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public class Conditions
    {
        public List<Condition> ConditionList { get; set; } = new List<Condition>();
        public LogicalGrouping MatchType { get; set; } // default is MatchAll
        public bool TrackingAllCaptures { get; set; } 

        public MatchResults Evaluate(RewriteContext context, MatchResults ruleMatch)
        {
            MatchResults prevCond = null;
            var success = true;
            foreach (var condition in ConditionList)
            {
                var res = condition.Evaluate(context, ruleMatch, prevCond);
                success = (MatchType == LogicalGrouping.MatchAll ? (success && res.Success) : (success || res.Success));
                prevCond = res;
            }
            return new MatchResults { Success = success, BackReference = prevCond?.BackReference };
        }
    }
}
