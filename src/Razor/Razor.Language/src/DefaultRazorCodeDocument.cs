// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCodeDocument : RazorCodeDocument
    {
        public DefaultRazorCodeDocument(
            RazorSourceDocument source,
            IEnumerable<RazorSourceDocument> imports)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Source = source;
            Imports = imports?.ToArray() ?? RazorSourceDocument.EmptyArray;

            Items = new ItemCollection();
        }

        public override IReadOnlyList<RazorSourceDocument> Imports { get; }

        public override ItemCollection Items { get; }

        public override RazorSourceDocument Source { get; }
    }
}
