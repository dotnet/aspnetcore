// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.RuleAbstraction;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// The builder to a list of rules for <see cref="UrlRewriteOptions"/> and <see cref="UrlRewriteMiddleware"/> 
    /// </summary>
    public static class UrlRewriteOptionsAddRulesExtensions
    {
        /// <summary>
        /// Adds a rule to the current rules.
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="rule">A rule to be added to the current rules.</param>
        public static UrlRewriteOptions AddRule(this UrlRewriteOptions options, Rule rule)
        {
            options.Rules.Add(rule);
            return options;
        }

        /// <summary>
        /// Adds a list of rules to the current rules.
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="rules">A list of rules.</param>
        public static UrlRewriteOptions AddRules(this UrlRewriteOptions options, List<Rule> rules)
        {
            options.Rules.AddRange(rules);
            return options;
        }

        /// <summary>
        /// Creates a rewrite path rule.
        /// </summary>
        /// <param name="options">The Url rewrite options.</param>
        /// <param name="regex">The string regex pattern to compare against the http context.</param>
        /// <param name="newPath">The string to replace the path with (with capture parameters).</param>
        /// <param name="stopRewriteOnSuccess">Whether or not to stop rewriting on success of rule.</param>
        /// <returns></returns>
        public static UrlRewriteOptions RewritePath(this UrlRewriteOptions options, string regex, string newPath, bool stopRewriteOnSuccess = false)
        {
            options.Rules.Add(new PathRule { MatchPattern = new Regex(regex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1)), OnMatch = newPath, OnCompletion = stopRewriteOnSuccess ? Transformation.TerminatingRewrite : Transformation.Rewrite });
            return options;
        }

        /// <summary>
        /// Rewrite http to https.
        /// </summary>
        /// <param name="options">The Url rewrite options.</param>
        /// <param name="stopRewriteOnSuccess">Whether or not to stop rewriting on success of rule.</param>
        /// <returns></returns>
        public static UrlRewriteOptions RewriteScheme(this UrlRewriteOptions options, bool stopRewriteOnSuccess = false)
        {
            options.Rules.Add(new SchemeRule {OnCompletion = stopRewriteOnSuccess ? Transformation.TerminatingRewrite : Transformation.Rewrite });
            return options;
        }

        /// <summary>
        /// Redirect a path to another path.
        /// </summary>
        /// <param name="options">The Url rewrite options.</param>
        /// <param name="regex">The string regex pattern to compare against the http context.</param>
        /// <param name="newPath">The string to replace the path with (with capture parameters).</param>
        /// <param name="stopRewriteOnSuccess">Whether or not to stop rewriting on success of rule.</param>
        /// <returns></returns>
        public static UrlRewriteOptions RedirectPath(this UrlRewriteOptions options, string regex, string newPath, bool stopRewriteOnSuccess = false)
        {
            options.Rules.Add(new PathRule { MatchPattern = new Regex(regex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1)), OnMatch = newPath, OnCompletion = Transformation.Redirect });
            return options;
        }

        /// <summary>
        /// Redirect http to https.
        /// </summary>
        /// <param name="options">The Url rewrite options.</param>
        /// <param name="sslPort">The port to redirect the scheme to.</param>
        /// <returns></returns>
        public static UrlRewriteOptions RedirectScheme(this UrlRewriteOptions options, int? sslPort)
        {
            options.Rules.Add(new SchemeRule { SSLPort = sslPort, OnCompletion = Transformation.Redirect });
            return options;
        }

        /// <summary>
        /// User generated rule to do a specific match on a path and what to do on success of the match.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="onApplyRule"></param>
        /// <param name="transform"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static UrlRewriteOptions CustomRule(this UrlRewriteOptions options, Func<UrlRewriteContext, RuleResult> onApplyRule, Transformation transform, string description = null)
        {
            options.Rules.Add(new FunctionalRule { OnApplyRule = onApplyRule, OnCompletion = transform});
            return options;
        }
    }
}
