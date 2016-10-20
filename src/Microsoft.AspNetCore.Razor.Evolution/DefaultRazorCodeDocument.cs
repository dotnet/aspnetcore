// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorCodeDocument : RazorCodeDocument
    {
        public DefaultRazorCodeDocument(RazorSourceDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Source = source;

            Items = new DefaultItemCollection();
        }

        public override ItemCollection Items { get; }

        public override RazorSourceDocument Source { get; }
    }
}
