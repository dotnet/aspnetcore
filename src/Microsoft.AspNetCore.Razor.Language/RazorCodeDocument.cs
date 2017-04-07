// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorCodeDocument
    {
        public static RazorCodeDocument Create(RazorSourceDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Create(source, imports: null);
        }

        public static RazorCodeDocument Create(
            RazorSourceDocument source,
            IEnumerable<RazorSourceDocument> imports)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            return new DefaultRazorCodeDocument(source, imports);
        }

        public abstract IReadOnlyList<RazorSourceDocument> Imports { get; }

        public abstract ItemCollection Items { get; }

        public abstract RazorSourceDocument Source { get; }
    }
}
