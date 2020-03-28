// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.AspNetCore.Analyzers
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            public Test()
            {
                ReferenceAssemblies = CodeAnalysis.Testing.ReferenceAssemblies.NetCore.NetCoreApp50;

                foreach (var assembly in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
                {
                    TestState.AdditionalReferences.Add(CreateReferenceFromFile(assembly));
                }

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    return solution;
                });
            }

            private static MetadataReference CreateReferenceFromFile(string path)
            {
                var documentationFile = Path.ChangeExtension(path, ".xml");
                return MetadataReference.CreateFromFile(path, documentation: XmlDocumentationProvider.CreateFromFile(documentationFile));
            }
        }
    }
}
