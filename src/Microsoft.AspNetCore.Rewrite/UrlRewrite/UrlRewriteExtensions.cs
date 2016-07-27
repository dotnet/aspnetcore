// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite.UrlRewrite;

namespace Microsoft.AspNetCore.Rewrite
{
    public static class UrlRewriteExtensions
    {
        /// <summary>
        /// Imports rules from a mod_rewrite file and adds the rules to current rules. 
        /// </summary>
        /// <param name="options">The UrlRewrite options.</param>
        /// <param name="hostingEnv"></param>
        /// <param name="filePath">The path to the file containing urlrewrite rules.</param>
        public static UrlRewriteOptions ImportFromUrlRewrite(this UrlRewriteOptions options, IHostingEnvironment hostingEnv, string filePath)
        {
            if (options == null)
            {
                throw new ArgumentNullException("UrlRewriteOptions is null");
            }

            if (hostingEnv == null)
            {
                throw new ArgumentNullException("HostingEnvironment is null");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            var path = Path.Combine(hostingEnv.ContentRootPath, filePath);
            using (var stream = File.OpenRead(path))
            {
                options.Rules.AddRange(UrlRewriteFileParser.Parse(new StreamReader(stream)));
            };
            return options;
        }
    }
}
