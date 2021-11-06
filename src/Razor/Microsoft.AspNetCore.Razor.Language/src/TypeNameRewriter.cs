// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

// Razor.Language doesn't reference Microsoft.CodeAnalysis.CSharp so we
// need some indirection.
internal abstract class TypeNameRewriter
{
    public abstract string Rewrite(string typeName);
}
