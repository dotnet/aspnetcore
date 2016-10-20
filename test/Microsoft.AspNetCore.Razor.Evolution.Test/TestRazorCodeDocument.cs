// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TestRazorCodeDocument : DefaultRazorCodeDocument
    {
        public static TestRazorCodeDocument CreateEmpty()
        {
            var source = TestRazorSourceDocument.Create(content: string.Empty);
            return new TestRazorCodeDocument(source);
        }

        private TestRazorCodeDocument(RazorSourceDocument source)
            : base(source)
        {
        }
    }
}
