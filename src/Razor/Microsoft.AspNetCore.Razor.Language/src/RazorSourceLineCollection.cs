// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorSourceLineCollection
    {
        public abstract int Count { get; }

        public abstract int GetLineLength(int index);

        internal abstract SourceLocation GetLocation(int position);
    }
}
