// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test
{
    public static class TestCompilation
    {
        public static Compilation Create(SyntaxTree syntaxTree = null)
        {
            IEnumerable<SyntaxTree> syntaxTrees = null;

            if (syntaxTree != null)
            {
                syntaxTrees = new[] { syntaxTree };
            }
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));

            var compilation = CSharpCompilation.Create("TestAssembly", syntaxTrees, references);

            return compilation;
        }
    }
}
