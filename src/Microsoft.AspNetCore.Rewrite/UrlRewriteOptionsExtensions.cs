// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite;

namespace Microsoft.AspNetCore.Rewrite
{
    public static class UrlRewriteOptionsExtensions
    {
        /// <summary>
        /// Imports rules from a mod_rewrite file and adds the rules to current rules. 
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="filePath">The path to the file containing urlrewrite rules.</param>
        public static RewriteOptions ImportFromUrlRewrite(this RewriteOptions options, IHostingEnvironment hostingEnvironment, string filePath)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            var path = Path.Combine(hostingEnvironment.ContentRootPath, filePath);
            using (var stream = File.OpenRead(path))
            {
                return ImportFromUrlRewrite(options, new StreamReader(stream));
            }
        }

        /// <summary>
        /// Imports rules from a mod_rewrite file and adds the rules to current rules. 
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="reader">The text reader stream.</param>
        public static RewriteOptions ImportFromUrlRewrite(this RewriteOptions options, TextReader reader)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (reader == null)
            {
                throw new ArgumentException(nameof(reader));
            }

            var rules = new UrlRewriteFileParser().Parse(reader);

            foreach (var rule in rules)
            {
                options.Rules.Add(rule);
            }
            return options;
        }
    }
}
