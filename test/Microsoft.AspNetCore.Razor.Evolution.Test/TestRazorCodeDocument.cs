// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TestRazorCodeDocument : DefaultRazorCodeDocument
    {
        public static TestRazorCodeDocument CreateEmpty()
        {
            var source = TestRazorSourceDocument.Create(content: string.Empty);
            return new TestRazorCodeDocument(source, imports: null);
        }

        public static TestRazorCodeDocument Create(string content)
        {
            var source = TestRazorSourceDocument.Create(content);
            return new TestRazorCodeDocument(source, imports: null);
        }

        private TestRazorCodeDocument(
            RazorSourceDocument source,
            IEnumerable<RazorSourceDocument> imports)
            : base(source, imports)
        {
        }
    }
}
