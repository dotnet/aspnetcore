// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class PathRule : Rule
    {
        public Regex MatchPattern { get; set; }
        public string OnMatch { get; set; }
        public Transformation OnCompletion { get; set; } = Transformation.Rewrite;
        public override RuleResult ApplyRule(RewriteContext context)
        {
            var matches = MatchPattern.Match(context.HttpContext.Request.Path);
            if (matches.Success)
            {
                // New method here to translate the outgoing format string to the correct value.
                var path = matches.Result(OnMatch);
                if (OnCompletion == Transformation.Redirect)
                {
                    var req = context.HttpContext.Request;
                    var newUrl = string.Concat(
                        req.Scheme,
                        "://",
                        req.PathBase,
                        path,
                        req.QueryString);
                    context.HttpContext.Response.Redirect(newUrl);
                    return RuleResult.ResponseComplete;
                }
                else
                {
                    context.HttpContext.Request.Path = path;
                }
                if (OnCompletion == Transformation.TerminatingRewrite)
                {
                    return RuleResult.StopRules;
                }
                else
                {
                    return RuleResult.Continue;
                }
            }
            return RuleResult.Continue;
        }
    }
}
