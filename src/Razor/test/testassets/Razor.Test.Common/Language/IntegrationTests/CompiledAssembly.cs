// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class CompiledAssembly
    {
        public CompiledAssembly(Compilation compilation, RazorCodeDocument codeDocument, Assembly assembly)
        {
            Compilation = compilation;
            CodeDocument = codeDocument;
            Assembly = assembly;
        }

        public Assembly Assembly { get; }

        public RazorCodeDocument CodeDocument { get; }

        public Compilation Compilation { get; }
    }
}
