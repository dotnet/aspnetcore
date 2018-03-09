// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Razor;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class RazorIntegrationTestBase
    {
        internal const string ArbitraryWindowsPath = "x:\\dir\\subdir\\Test";
        internal const string ArbitraryMacLinuxPath = "/dir/subdir/Test";

        // Creating the initial compilation + reading references is on the order of 250ms without caching
        // so making sure it doesn't happen for each test.
        private static readonly CSharpCompilation BaseCompilation;

        static RazorIntegrationTestBase()
        {
            var referenceAssemblyRoots = new[]
            {
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly, // System.Runtime
                typeof(BlazorComponent).Assembly,
                typeof(RazorIntegrationTestBase).Assembly, // Reference this assembly, so that we can refer to test component types
            };

            var referenceAssemblies = referenceAssemblyRoots
                .SelectMany(assembly => assembly.GetReferencedAssemblies().Concat(new[] { assembly.GetName() }))
                .Distinct()
                .Select(Assembly.Load)
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                .ToList();
            BaseCompilation = CSharpCompilation.Create(
                "TestAssembly",
                Array.Empty<SyntaxTree>(),
                referenceAssemblies,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        public RazorIntegrationTestBase()
        {
            AdditionalSyntaxTrees = new List<SyntaxTree>();
            AdditionalRazorItems = new List<RazorProjectItem>();

            Configuration = BlazorExtensionInitializer.DefaultConfiguration;
            FileSystem = new VirtualRazorProjectFileSystem();
            WorkingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ArbitraryWindowsPath : ArbitraryMacLinuxPath;

            DefaultBaseNamespace = "Test"; // Matches the default working directory
            DefaultFileName = "TestComponent.cshtml";

        }

        internal List<RazorProjectItem> AdditionalRazorItems { get; }

        internal List<SyntaxTree> AdditionalSyntaxTrees { get; }

        internal virtual RazorConfiguration Configuration { get; }

        internal virtual string DefaultBaseNamespace { get; }

        internal virtual string DefaultFileName { get; }

        internal virtual bool DesignTime { get; }
        
        internal virtual VirtualRazorProjectFileSystem FileSystem { get; }

        internal virtual bool UseTwoPhaseCompilation { get; }

        internal virtual string WorkingDirectory { get; }

        internal RazorProjectEngine CreateProjectEngine(RazorConfiguration configuration, MetadataReference[] references)
        {
            return RazorProjectEngine.Create(configuration, FileSystem, b =>
            {
                // Turn off checksums, we're testing code generation.
                b.Features.Add(new SuppressChecksum());

                BlazorExtensionInitializer.Register(b);

                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultMetadataReferenceFeature()
                {
                    References = references,
                });
            });
        }

        internal RazorProjectItem CreateProjectItem(string chtmlRelativePath, string cshtmlContent)
        {
            // FilePaths in Razor are **always** are of the form '/a/b/c.cshtml'
            var filePath = chtmlRelativePath.Replace('\\', '/');
            if (!filePath.StartsWith('/'))
            {
                filePath = '/' + filePath;
            }

            return new VirtualProjectItem(
                WorkingDirectory, 
                filePath, 
                Path.Combine(WorkingDirectory, chtmlRelativePath),
                chtmlRelativePath,
                Encoding.UTF8.GetBytes(cshtmlContent.TrimStart()));
        }

        protected CompileToCSharpResult CompileToCSharp(string cshtmlContent)
        {
            return CompileToCSharp(DefaultFileName, cshtmlContent);
        }

        protected CompileToCSharpResult CompileToCSharp(string cshtmlRelativePath, string cshtmlContent)
        {
            if (UseTwoPhaseCompilation)
            {
                // The first phase won't include any metadata references for component discovery. This mirrors
                // what the build does.
                var projectEngine = CreateProjectEngine(BlazorExtensionInitializer.DeclarationConfiguration, Array.Empty<MetadataReference>());
                
                RazorCodeDocument codeDocument;
                foreach (var item in AdditionalRazorItems)
                {
                    // Result of generating declarations
                    codeDocument = projectEngine.Process(item);
                    Assert.Empty(codeDocument.GetCSharpDocument().Diagnostics);

                    var syntaxTree = CSharpSyntaxTree.ParseText(codeDocument.GetCSharpDocument().GeneratedCode, path: item.FilePath);
                    AdditionalSyntaxTrees.Add(syntaxTree);
                }

                // Result of generating declarations
                var projectItem = CreateProjectItem(cshtmlRelativePath, cshtmlContent);
                codeDocument = projectEngine.Process(projectItem);
                var declaration = new CompileToCSharpResult
                {
                    BaseCompilation = BaseCompilation.AddSyntaxTrees(AdditionalSyntaxTrees),
                    CodeDocument = codeDocument,
                    Code = codeDocument.GetCSharpDocument().GeneratedCode,
                    Diagnostics = codeDocument.GetCSharpDocument().Diagnostics,
                };

                // Result of doing 'temp' compilation
                var tempAssembly = CompileToAssembly(declaration);

                // Add the 'temp' compilation as a metadata reference 
                var references = BaseCompilation.References.Concat(new[] { tempAssembly.Compilation.ToMetadataReference() }).ToArray();
                projectEngine = CreateProjectEngine(BlazorExtensionInitializer.DefaultConfiguration, references);

                // Now update the any additional files
                foreach (var item in AdditionalRazorItems)
                {
                    // Result of generating declarations
                    codeDocument = projectEngine.Process(item);
                    Assert.Empty(codeDocument.GetCSharpDocument().Diagnostics);

                    // Replace the 'declaration' syntax tree
                    var syntaxTree = CSharpSyntaxTree.ParseText(codeDocument.GetCSharpDocument().GeneratedCode, path: item.FilePath);
                    AdditionalSyntaxTrees.RemoveAll(st => st.FilePath == item.FilePath);
                    AdditionalSyntaxTrees.Add(syntaxTree);
                }

                // Result of real code generation for the document under test
                codeDocument = DesignTime ? projectEngine.ProcessDesignTime(projectItem) : projectEngine.Process(projectItem);
                return new CompileToCSharpResult
                {
                    BaseCompilation = BaseCompilation.AddSyntaxTrees(AdditionalSyntaxTrees),
                    CodeDocument = codeDocument,
                    Code = codeDocument.GetCSharpDocument().GeneratedCode,
                    Diagnostics = codeDocument.GetCSharpDocument().Diagnostics,
                };
            }
            else
            {
                // For single phase compilation tests just use the base compilation's references.
                // This will include the built-in Blazor components.
                var projectEngine = CreateProjectEngine(Configuration, BaseCompilation.References.ToArray());

                var projectItem = CreateProjectItem(cshtmlRelativePath, cshtmlContent);
                var codeDocument = DesignTime ? projectEngine.ProcessDesignTime(projectItem) : projectEngine.Process(projectItem);
                return new CompileToCSharpResult
                {
                    BaseCompilation = BaseCompilation.AddSyntaxTrees(AdditionalSyntaxTrees),
                    CodeDocument = codeDocument,
                    Code = codeDocument.GetCSharpDocument().GeneratedCode,
                    Diagnostics = codeDocument.GetCSharpDocument().Diagnostics,
                };
            }
        }

        protected CompileToAssemblyResult CompileToAssembly(string cshtmlRelativePath, string cshtmlContent)
        {
            var cSharpResult = CompileToCSharp(cshtmlRelativePath, cshtmlContent);
            return CompileToAssembly(cSharpResult);
        }

        protected CompileToAssemblyResult CompileToAssembly(CompileToCSharpResult cSharpResult)
        {
            if (cSharpResult.Diagnostics.Any())
            {
                var diagnosticsLog = string.Join(Environment.NewLine, cSharpResult.Diagnostics.Select(d => d.ToString()).ToArray());
                throw new InvalidOperationException($"Aborting compilation to assembly because RazorCompiler returned nonempty diagnostics: {diagnosticsLog}");
            }

            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(cSharpResult.Code),
            };

            var compilation = cSharpResult.BaseCompilation.AddSyntaxTrees(syntaxTrees);

            var diagnostics = compilation
                .GetDiagnostics()
                .Where(d => d.Severity != DiagnosticSeverity.Hidden);

            if (diagnostics.Any())
            {
                throw new CompilationFailedException(compilation);
            }

            using (var peStream = new MemoryStream())
            {
                compilation.Emit(peStream);

                return new CompileToAssemblyResult
                {
                    Compilation = compilation,
                    Diagnostics = diagnostics,
                    Assembly = diagnostics.Any() ? null : Assembly.Load(peStream.ToArray())
                };
            }

        }

        protected IComponent CompileToComponent(string cshtmlSource)
        {
            var assemblyResult = CompileToAssembly(DefaultFileName, cshtmlSource);

            var componentFullTypeName = $"{DefaultBaseNamespace}.{Path.GetFileNameWithoutExtension(DefaultFileName)}";
            return CompileToComponent(assemblyResult, componentFullTypeName);
        }

        protected IComponent CompileToComponent(CompileToCSharpResult cSharpResult, string fullTypeName)
        {
            return CompileToComponent(CompileToAssembly(cSharpResult), fullTypeName);
        }

        protected IComponent CompileToComponent(CompileToAssemblyResult assemblyResult, string fullTypeName)
        {
            var componentType = assemblyResult.Assembly.GetType(fullTypeName);
            if (componentType == null)
            {
                throw new XunitException(
                    $"Failed to find component type '{fullTypeName}'. Found types:" + Environment.NewLine +
                    string.Join(Environment.NewLine, assemblyResult.Assembly.ExportedTypes.Select(t => t.FullName)));
            }

            return (IComponent)Activator.CreateInstance(componentType);
        }

        protected static string FullTypeName<T>() => typeof(T).FullName.Replace('+', '.');

        protected RenderTreeFrame[] GetRenderTree(IComponent component)
        {
            var renderer = new TestRenderer();
            renderer.AttachComponent(component);
            component.SetParameters(ParameterCollection.Empty);
            return renderer.LatestBatchReferenceFrames;
        }

        protected ArrayRange<RenderTreeFrame> GetFrames(RenderFragment fragment)
        {
            var builder = new RenderTreeBuilder(new TestRenderer());
            fragment(builder);
            return builder.GetFrames();
        }

        protected static void AssertSourceEquals(string expected, CompileToCSharpResult generated)
        {
            // Normalize the paths inside the expected result to match the OS paths
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var windowsPath = Path.Combine(ArbitraryWindowsPath, generated.CodeDocument.Source.RelativePath).Replace('/', '\\');
                expected = expected.Replace(windowsPath, generated.CodeDocument.Source.FilePath);
            }

            expected = expected.Trim();
            Assert.Equal(expected, generated.Code.Trim(), ignoreLineEndingDifferences: true);
        }

        protected class CompileToCSharpResult
        {
            // A compilation that can be used *with* this code to compile an assembly
            public Compilation BaseCompilation { get; set; }
            public RazorCodeDocument CodeDocument { get; set; }
            public string Code { get; set; }
            public IEnumerable<RazorDiagnostic> Diagnostics { get; set; }
        }

        protected class CompileToAssemblyResult
        {
            public Assembly Assembly { get; set; }
            public Compilation Compilation { get; set; }
            public string VerboseLog { get; set; }
            public IEnumerable<Diagnostic> Diagnostics { get; set; }
        }

        private class TestRenderer : Renderer
        {
            public TestRenderer() : base(new TestServiceProvider())
            {
            }

            public RenderTreeFrame[] LatestBatchReferenceFrames { get; private set; }

            public void AttachComponent(IComponent component)
                => AssignComponentId(component);

            protected override void UpdateDisplay(RenderBatch renderBatch)
            {
                LatestBatchReferenceFrames = renderBatch.ReferenceFrames.ToArray();
            }
        }

        private class CompilationFailedException : XunitException
        {
            public CompilationFailedException(Compilation compilation) 
            {
                Compilation = compilation;
            }

            public Compilation Compilation { get; }

            public override string Message
            {
                get
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("Compilation failed: ");

                    var diagnostics = Compilation.GetDiagnostics();
                    var syntaxTreesWithErrors = new HashSet<SyntaxTree>();
                    foreach (var diagnostic in diagnostics)
                    {
                        builder.AppendLine(diagnostic.ToString());

                        if (diagnostic.Location.IsInSource)
                        {
                            syntaxTreesWithErrors.Add(diagnostic.Location.SourceTree);
                        }
                    }

                    if (syntaxTreesWithErrors.Any())
                    {
                        builder.AppendLine();
                        builder.AppendLine();

                        foreach (var syntaxTree in syntaxTreesWithErrors)
                        {
                            builder.AppendLine($"File {syntaxTree.FilePath ?? "unknown"}:");
                            builder.AppendLine(syntaxTree.GetText().ToString());
                        }
                    }

                    return builder.ToString();
                }
            }
        }

        private class SuppressChecksum : IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order => 0;

            public RazorEngine Engine { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                options.SuppressChecksum = true;
            }
        }
    }
}
