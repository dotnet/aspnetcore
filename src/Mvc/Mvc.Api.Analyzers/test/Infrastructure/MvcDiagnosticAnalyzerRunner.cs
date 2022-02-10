// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc;

public class MvcDiagnosticAnalyzerRunner : DiagnosticAnalyzerRunner
{
    public MvcDiagnosticAnalyzerRunner(DiagnosticAnalyzer analyzer)
    {
        Analyzer = analyzer;
    }

    public DiagnosticAnalyzer Analyzer { get; }

    public Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var project = CreateProjectWithReferencesInBinDir(GetType().Assembly, source);

        return GetDiagnosticsAsync(project);
    }

    public static Project CreateProjectWithReferencesInBinDir(Assembly testAssembly, params string[] source)
    {
        // The deps file in the project is incorrect and does not contain "compile" nodes for some references.
        // However these binaries are always present in the bin output. As a "temporary" workaround, we'll add
        // every dll file that's present in the test's build output as a metadatareference.

        var project = DiagnosticProject.Create(testAssembly, source);

        foreach (var assembly in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
        {
            if (!project.MetadataReferences.Any(c => string.Equals(Path.GetFileNameWithoutExtension(c.Display), Path.GetFileNameWithoutExtension(assembly), StringComparison.OrdinalIgnoreCase)))
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(assembly));
            }
        }

        return project;
    }

    public Task<Diagnostic[]> GetDiagnosticsAsync(Project project)
    {
        return GetDiagnosticsAsync(new[] { project }, Analyzer, Array.Empty<string>());
    }
}
