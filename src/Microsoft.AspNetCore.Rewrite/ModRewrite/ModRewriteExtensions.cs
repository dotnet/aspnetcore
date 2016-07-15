// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Rewrite.ModRewrite;

namespace Microsoft.AspNetCore.Rewrite
{
    public static class ModRewriteExtensions
    {
        /// <summary>
        /// Imports rules from a mod_rewrite file and adds the rules to current rules. 
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="filePath">The path to the file containing mod_rewrite rules.</param>
        public static UrlRewriteOptions ImportFromModRewrite(this UrlRewriteOptions options, string filePath)
        {
            // TODO use IHostingEnvironment as param.
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }
            // TODO IHosting to fix!

            using (var stream = File.OpenRead(filePath))
            {
                options.Rules.AddRange(FileParser.Parse(new StreamReader(stream)));
            };
            return options;
        }

        /// <summary>
        /// Imports rules from a mod_rewrite file and adds the rules to current rules. 
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="reader">Text reader containing a stream of mod_rewrite rules.</param>
        public static UrlRewriteOptions ImportFromModRewrite(this UrlRewriteOptions options, TextReader reader)
        {
            options.Rules.AddRange(FileParser.Parse(reader));
            return options;
        }

        /// <summary>
        /// Adds a mod_rewrite rule to the current rules.
        /// Additional properties (conditions, flags) for the rule can be added through the action.
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="rule">The literal string of a mod_rewrite rule: 
        /// "RewriteRule Pattern Substitution [Flags]"</param>
        /// <param name="action">Action to perform on the <see cref="RuleBuilder"/> </param>
        public static UrlRewriteOptions AddModRewriteRule(this UrlRewriteOptions options, string rule, Action<RuleBuilder> action)
        {
            var builder = new RuleBuilder(rule);
            action(builder);
            options.Rules.Add(builder.Build());
            return options;
        }

        /// <summary>
        /// Adds a mod_rewrite rule to the current rules.
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="rule">The literal string of a mod_rewrite rule: 
        /// "RewriteRule Pattern Substitution [Flags]"</param>
        public static UrlRewriteOptions AddModRewriteRule(this UrlRewriteOptions options, string rule)
        {
            var builder = new RuleBuilder(rule);
            options.Rules.Add(builder.Build());
            return options;
        }
    }
}
