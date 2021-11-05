// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class CompiledCSharpCode
{
    public CompiledCSharpCode(Compilation baseCompilation, RazorCodeDocument codeDocument)
    {
        BaseCompilation = baseCompilation;
        CodeDocument = codeDocument;
    }

    // A compilation that can be used *with* this code to compile an assembly
    public Compilation BaseCompilation { get; set; }

    public RazorCodeDocument CodeDocument { get; set; }
}
