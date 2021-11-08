// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

// Razor.Language doesn't reference Microsoft.CodeAnalysis.CSharp so we
// need some indirection.
internal abstract class TypeNameFeature : RazorEngineFeatureBase
{
    public abstract IReadOnlyList<string> ParseTypeParameters(string typeName);

    public abstract TypeNameRewriter CreateGenericTypeRewriter(Dictionary<string, string> bindings);

    public abstract TypeNameRewriter CreateGlobalQualifiedTypeNameRewriter(ICollection<string> ignore);

    public abstract bool IsLambda(string expression);
}
