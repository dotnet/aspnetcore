// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Extensions for adding Apache mod_rewrite rules to <see cref="RewriteOptions"/>
    /// </summary>
    public static class ApacheModRewriteOptionsExtensions
    {
        /// <summary>
        /// Add rules from an Apache mod_rewrite file
        /// </summary>
        /// <param name="options">The <see cref="RewriteOptions"/></param>
        /// <param name="fileProvider">The <see cref="IFileProvider"/> </param>
        /// <param name="filePath">The path to the file containing mod_rewrite rules.</param>
        public static RewriteOptions AddApacheModRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            var fileInfo = fileProvider.GetFileInfo(filePath);
            using (var stream = fileInfo.CreateReadStream())
            {
                return options.AddApacheModRewrite(new StreamReader(stream));
            }
        }

        /// <summary>
        /// Add rules from an Apache mod_rewrite file
        /// </summary>
        /// <param name="options">The <see cref="RewriteOptions"/></param>
        /// <param name="reader">A stream of mod_rewrite rules.</param>
        public static RewriteOptions AddApacheModRewrite(this RewriteOptions options, TextReader reader)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            var rules = new FileParser().Parse(reader);

            foreach (var rule in rules)
            {
                options.Rules.Add(rule);
            }
            return options;
        }

    }
}
