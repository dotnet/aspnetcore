// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Blazor.Build.Core.RazorCompilation
{
    public class RazorCompiler
    {
        public ICollection<RazorCompilerDiagnostic> CompileFiles(IEnumerable<string> inputPaths, string outputNamespace, TextWriter resultOutput, TextWriter verboseOutput)
        {
            var diagnostics = new List<RazorCompilerDiagnostic>();

            foreach (var inputFilePath in inputPaths)
            {
                verboseOutput?.WriteLine($"Compiling {inputFilePath}...");
                resultOutput.WriteLine($"// TODO: Compile {inputFilePath}");
                diagnostics.Add(new RazorCompilerDiagnostic(
                    RazorCompilerDiagnostic.DiagnosticType.Warning,
                    inputFilePath,
                    1,
                    1,
                    "Compiler not implemented"));
            }

            return diagnostics;
        }
    }
}
