// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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

            return new DefaultRazorCodeDocument(source);
        }

        public abstract ItemCollection Items { get; }

        public abstract RazorSourceDocument Source { get; }
    }
}
