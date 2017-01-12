// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class RazorCodeDocument
    {
        public static RazorCodeDocument Create(RazorSourceDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Create(source, imports: null, includes: null);
        }

        public static RazorCodeDocument Create(
            RazorSourceDocument source,
            IEnumerable<RazorSourceDocument> imports,
            IEnumerable<RazorSourceDocument> includes)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            return new DefaultRazorCodeDocument(source, imports, includes);
        }

        public abstract IReadOnlyList<RazorSourceDocument> Imports { get; }

        public abstract ItemCollection Items { get; }

        public abstract IReadOnlyList<RazorSourceDocument> Includes { get; }

        public abstract RazorSourceDocument Source { get; }
    }
}
