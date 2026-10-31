// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Microsoft.AspNetCore.Analyzer.Testing;

public class DiagnosticProject
{
    /// <summary>
    /// File name prefix used to generate Documents instances from source.
    /// </summary>
    public static string DefaultFilePathPrefix = "Test";

    /// <summary>
    /// Project name.
    /// </summary>
    public static string TestProjectName = "TestProject";

    private static readonly ICompilationAssemblyResolver _assemblyResolver = new AppBaseCompilationAssemblyResolver();
    private static readonly Dictionary<Assembly, Solution> _solutionCache = new Dictionary<Assembly, Solution>();

    public static Project Create(Assembly testAssembly, string[] sources, Func<Workspace> workspaceFactory = null, Type[] analyzerReferences = null)
    {
        Solution solution;
        lock (_solutionCache)
        {
            if (!_solutionCache.TryGetValue(testAssembly, out solution))
            {
                workspaceFactory ??= CreateWorkspace;

                var projectId = ProjectId.CreateNewId(debugName: TestProjectName);
                solution = workspaceFactory()
                    .CurrentSolution
                    .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp);

                foreach (var defaultCompileLibrary in DependencyContext.Load(testAssembly).CompileLibraries)
                {
                    foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(_assemblyResolver))
                    {
                        solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(resolveReferencePath));
                    }
                }

                if (analyzerReferences != null)
                {
                    foreach (var analyzerReference in analyzerReferences)
                    {
                        solution = solution.AddAnalyzerReference(
                            projectId,
                            new AnalyzerFileReference(analyzerReference.Assembly.Location, AssemblyLoader.Instance));
                    }
                }

                _solutionCache.Add(testAssembly, solution);
            }
        }

        var testProject = solution.ProjectIds.Single();
        var fileNamePrefix = DefaultFilePathPrefix;

        for (var i = 0; i < sources.Length; i++)
        {
            var newFileName = fileNamePrefix;
            if (sources.Length > 1)
            {
                newFileName += i;
            }
            newFileName += ".cs";

            var documentId = DocumentId.CreateNewId(testProject, debugName: newFileName);
            solution = solution.AddDocument(documentId, newFileName, SourceText.From(sources[i]));
        }

        return solution.GetProject(testProject);
    }

    private static Workspace CreateWorkspace()
    {
        return new AdhocWorkspace();
    }

    internal sealed class AssemblyLoader : IAnalyzerAssemblyLoader
    {
        public static AssemblyLoader Instance = new AssemblyLoader();

        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
        {
            return Assembly.LoadFrom(fullPath);
        }
    }
}
