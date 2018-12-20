// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    // Razor.Language doesn't reference Microsoft.CodeAnalysis.CSharp so we
    // need some indirection.
    internal abstract class TypeNameRewriter
    {
        public abstract string Rewrite(string typeName);
    }
}
