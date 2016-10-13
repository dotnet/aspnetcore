// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class RazorSyntaxTree
    {
        internal static RazorSyntaxTree Create(Block root, IEnumerable<RazorError> diagnostics)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            return new DefaultRazorSyntaxTree(root, new List<RazorError>(diagnostics));
        }

        public static RazorSyntaxTree Parse(RazorSourceDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var parser = new RazorParser();

            using (var reader = source.CreateReader())
            {
                return parser.Parse(reader);
            }
        }

        internal abstract IReadOnlyList<RazorError> Diagnostics { get; }

        internal abstract Block Root { get; }
    }
}
