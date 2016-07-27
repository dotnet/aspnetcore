// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite;

namespace Microsoft.AspNetCore.Rewrite
{
    public static class ModRewriteExtensions
    {
        /// <summary>
        /// Imports rules from a mod_rewrite file and adds the rules to current rules. 
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="hostingEnv"></param>
        /// <param name="filePath">The path to the file containing mod_rewrite rules.</param>
        public static RewriteOptions ImportFromModRewrite(this RewriteOptions options, IHostingEnvironment hostingEnv, string filePath)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            var path = Path.Combine(hostingEnv.ContentRootPath, filePath);
            using (var stream = File.OpenRead(path))
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
        public static RewriteOptions ImportFromModRewrite(this RewriteOptions options, TextReader reader)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            options.Rules.AddRange(FileParser.Parse(reader));
            return options;
        }

        /// <summary>
        /// Adds a mod_rewrite rule to the current rules.
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="rule">The literal string of a mod_rewrite rule: 
        /// "RewriteRule Pattern Substitution [Flags]"</param>
        public static RewriteOptions AddModRewriteRule(this RewriteOptions options, string rule)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var builder = new RuleBuilder(rule);
            options.Rules.Add(builder.Build());
            return options;
        }
    }
}
