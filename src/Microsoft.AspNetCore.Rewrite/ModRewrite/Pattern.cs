// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.ModRewrite;

namespace Microsoft.AspNetCore.Rewrite.ModRewrite
{
    /// <summary>
    /// Contains a sequence of pattern segments, which on obtaining the context, will create the appropriate
    /// test string and condition for rules and conditions.
    /// </summary>
    public class Pattern
    {
        private IReadOnlyList<PatternSegment> Segments { get; }
        /// <summary>
        /// Creates a new Pattern
        /// </summary>
        /// <param name="segments">List of pattern segments which will be applied.</param>
        public Pattern(IReadOnlyList<PatternSegment> segments)
        {
            Segments = segments;
        }

        /// <summary>
        /// Creates the appropriate test string from the Httpcontext and Segments.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ruleMatch"></param>
        /// <param name="prevCondition"></param>
        /// <returns></returns>
        public string GetPattern(HttpContext context, Match ruleMatch, Match prevCondition)
        {
            var res = new StringBuilder();
            foreach (var segment in Segments)
            {
                // TODO handle case when segment.Variable is 0 in rule and condition
                switch (segment.Type)
                {
                    case SegmentType.Literal:
                        res.Append(segment.Variable);
                        break;
                    case SegmentType.ServerParameter:
                        res.Append(ServerVariables.Resolve(segment.Variable, context));
                        break;
                    case SegmentType.RuleParameter:
                        var ruleParam = ruleMatch.Groups[segment.Variable];
                        if (ruleParam != null)
                        {
                            res.Append(ruleParam);
                        }
                        break;
                    case SegmentType.ConditionParameter:
                        var condParam = prevCondition.Groups[segment.Variable];
                        if (condParam != null)
                        {
                            res.Append(condParam);
                        }
                        break;
                }
            }
            return res.ToString();
        }
    }
}
