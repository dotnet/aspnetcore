// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
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
}
