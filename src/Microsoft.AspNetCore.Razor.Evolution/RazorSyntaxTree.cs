// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class RazorSyntaxTree
    {
        internal static RazorSyntaxTree Create(
            Block root,
            RazorSourceDocument source,
            IEnumerable<RazorError> diagnostics,
            RazorParserOptions options)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new DefaultRazorSyntaxTree(root, source, new List<RazorError>(diagnostics), options);
        }

        public static RazorSyntaxTree Parse(RazorSourceDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Parse(source, options: null);
        }

        public static RazorSyntaxTree Parse(RazorSourceDocument source, RazorParserOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            var parser = new RazorParser(options ?? RazorParserOptions.CreateDefaultOptions());
            return parser.Parse(source);
        }

        internal abstract IReadOnlyList<RazorError> Diagnostics { get; }

        public abstract RazorParserOptions Options { get; }

        internal abstract Block Root { get; }

        public abstract RazorSourceDocument Source { get; }
    }
}
