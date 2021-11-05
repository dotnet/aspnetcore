// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class AddImportChunkGenerator : SpanChunkGenerator
{
    public AddImportChunkGenerator(string usingContent, string parsedNamespace, bool isStatic)
    {
        Namespace = usingContent;
        ParsedNamespace = parsedNamespace;
        IsStatic = isStatic;
    }

    public string Namespace { get; }

    public string ParsedNamespace { get; }

    public bool IsStatic { get; }

    public override string ToString()
    {
        return "Import:" + Namespace + ";";
    }

    public override bool Equals(object obj)
    {
        var other = obj as AddImportChunkGenerator;
        return other != null &&
            string.Equals(Namespace, other.Namespace, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        // Hash code should include only immutable properties.
        return Namespace == null ? 0 : StringComparer.Ordinal.GetHashCode(Namespace);
    }
}
