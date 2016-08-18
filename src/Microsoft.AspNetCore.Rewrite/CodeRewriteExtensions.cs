// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.AspNetCore.Rewrite.Internal.CodeRules;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// The builder to a list of rules for <see cref="RewriteOptions"/> and <see cref="RewriteMiddleware"/> 
    /// </summary>
    public static class CodeRewriteExtensions
    {
        /// <summary>
        /// Adds a rule to the current rules.
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="rule">A rule to be added to the current rules.</param>
        public static RewriteOptions AddRule(this RewriteOptions options, Rule rule)
        {
            options.Rules.Add(rule);
            return options;
        }

        /// <summary>
        /// Adds a list of rules to the current rules.
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="rules">A list of rules.</param>
        public static RewriteOptions AddRules(this RewriteOptions options, List<Rule> rules)
        {
            options.Rules.AddRange(rules);
            return options;
        }

        public static RewriteOptions RewriteRule(this RewriteOptions options, string regex, string onMatch)
        {
            return RewriteRule(options, regex, onMatch, stopProcessing: false);
        }

        public static RewriteOptions RewriteRule(this RewriteOptions options, string regex, string onMatch, bool stopProcessing)
        {
            var builder = new UrlRewriteRuleBuilder();
            var pattern = new InputParser().ParseInputString(onMatch);

            builder.AddUrlMatch(regex);
            builder.AddUrlAction(pattern, actionType: ActionType.Rewrite, stopProcessing: stopProcessing);
            options.Rules.Add(builder.Build());
            return options;
        }

        public static RewriteOptions RedirectRule(this RewriteOptions options, string regex, string onMatch, int statusCode)
        {
            return RedirectRule(options, regex, onMatch, statusCode, stopProcessing: false);
        }

        public static RewriteOptions RedirectRule(this RewriteOptions options, string regex, string onMatch, int statusCode, bool stopProcessing)
        {
            var builder = new UrlRewriteRuleBuilder();
            var pattern = new InputParser().ParseInputString(onMatch);

            builder.AddUrlMatch(regex);
            builder.AddUrlAction(pattern, actionType: ActionType.Redirect, stopProcessing: stopProcessing);
            options.Rules.Add(builder.Build());
            return options;
        }

        public static RewriteOptions RedirectToHttps(this RewriteOptions options, int statusCode)
        {
            return RedirectToHttps(options, statusCode, null);
        }

        // TODO Don't do this, it doesn't work in all cases. Will refactor tonight/ tomorrow.
        public static RewriteOptions RedirectToHttps(this RewriteOptions options, int statusCode, int? sslPort)
        {
            options.Rules.Add(new RedirectToHttpsRule { StatusCode = statusCode, SSLPort = sslPort });
            return options;
        }

        public static RewriteOptions RewriteToHttps(this RewriteOptions options)
        { 
            return RewriteToHttps(options, sslPort: null,  stopProcessing: false);
        }

        public static RewriteOptions RewriteToHttps(this RewriteOptions options, int? sslPort)
        {
            return RewriteToHttps(options, sslPort, stopProcessing: false);
        }

        public static RewriteOptions RewriteToHttps(this RewriteOptions options, int? sslPort, bool stopProcessing)
        {
            options.Rules.Add(new RewriteToHttpsRule {SSLPort = sslPort, stopProcessing = stopProcessing });
            return options;
        }

        public static RewriteOptions AddRule(this RewriteOptions options, Func<RewriteContext, RuleResult> rule)
        {
            options.Rules.Add(new FunctionalRule { OnApplyRule = rule});
            return options;
        }
    }
}
