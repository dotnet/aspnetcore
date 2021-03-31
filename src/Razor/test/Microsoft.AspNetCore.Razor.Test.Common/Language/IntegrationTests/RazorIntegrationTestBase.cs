// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class RazorIntegrationTestBase
    {
        internal const string ArbitraryWindowsPath = "x:\\dir\\subdir\\Test";
        internal const string ArbitraryMacLinuxPath = "/dir/subdir/Test";

        // Creating the initial compilation + reading references is on the order of 250ms without caching
        // so making sure it doesn't happen for each test.
        protected static readonly CSharpCompilation DefaultBaseCompilation;

        private static CSharpParseOptions CSharpParseOptions { get; }

        static RazorIntegrationTestBase()
        {
            var referenceAssemblyRoots = new[]
            {
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly, // System.Runtime
                typeof(Enumerable).Assembly, // Other .NET fundamental types
                typeof(System.Linq.Expressions.Expression).Assembly,
                typeof(ComponentBase).Assembly,
            };

            var referenceAssemblies = referenceAssemblyRoots
                .SelectMany(assembly => assembly.GetReferencedAssemblies().Concat(new[] { assembly.GetName() }))
                .Distinct()
                .Select(Assembly.Load)
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                .ToList();
            DefaultBaseCompilation = CSharpCompilation.Create(
                "TestAssembly",
                Array.Empty<SyntaxTree>(),
                referenceAssemblies,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            CSharpParseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        }

        public RazorIntegrationTestBase()
        {
            AdditionalSyntaxTrees = new List<SyntaxTree>();
            AdditionalRazorItems = new List<RazorProjectItem>();
            ImportItems = new List<RazorProjectItem>();

            BaseCompilation = DefaultBaseCompilation;
            Configuration = RazorConfiguration.Default;
            FileSystem = new VirtualRazorProjectFileSystem();
            PathSeparator = Path.DirectorySeparatorChar.ToString();
            WorkingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ArbitraryWindowsPath : ArbitraryMacLinuxPath;

            DefaultRootNamespace = "Test"; // Matches the default working directory
            DefaultFileName = "TestComponent.cshtml";
        }

        internal List<RazorProjectItem> AdditionalRazorItems { get; }

        internal List<RazorProjectItem> ImportItems { get; }

        internal List<SyntaxTree> AdditionalSyntaxTrees { get; }

        internal virtual CSharpCompilation BaseCompilation { get; }

        internal virtual RazorConfiguration Configuration { get; }

        internal virtual string DefaultRootNamespace { get; }

        internal virtual string DefaultFileName { get; }

        internal virtual bool DesignTime { get; }

        internal virtual bool DeclarationOnly { get; }

        /// <summary>
        /// Gets a hardcoded document kind to be added to each code document that's created. This can
        /// be used to generate components.
        /// </summary>
        internal virtual string FileKind { get; }

        internal virtual VirtualRazorProjectFileSystem FileSystem { get; }

        // Used to force a specific style of line-endings for testing. This matters
        // for the baseline tests that exercise line mappings. Even though we normalize
        // newlines for testing, the difference between platforms affects the data through
        // the *count* of characters written.
        internal virtual string LineEnding { get; }

        internal virtual string PathSeparator { get; }

        internal virtual bool NormalizeSourceLineEndings { get; }

        internal virtual bool UseTwoPhaseCompilation { get; }

        internal virtual string WorkingDirectory { get; }

        // intentionally private - we don't want individual tests messing with the project engine
        private RazorProjectEngine CreateProjectEngine(RazorConfiguration configuration, MetadataReference[] references)
        {
            return RazorProjectEngine.Create(configuration, FileSystem, b =>
            {
                b.SetRootNamespace(DefaultRootNamespace);

                // Turn off checksums, we're testing code generation.
                b.Features.Add(new SuppressChecksum());

                b.Features.Add(new TestImportProjectFeature(ImportItems));

                if (LineEnding != null)
                {
                    b.Phases.Insert(0, new ForceLineEndingPhase(LineEnding));
                }

                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultMetadataReferenceFeature()
                {
                    References = references,
                });

                b.SetCSharpLanguageVersion(CSharpParseOptions.LanguageVersion);

                CompilerFeatures.Register(b);
            });
        }

        internal RazorProjectItem CreateProjectItem(string cshtmlRelativePath, string cshtmlContent, string fileKind = null, string cssScope = null)
        {
            var fullPath = WorkingDirectory + PathSeparator + cshtmlRelativePath;

            // FilePaths in Razor are **always** are of the form '/a/b/c.cshtml'
            var filePath = cshtmlRelativePath.Replace('\\', '/');
            if (!filePath.StartsWith("/", StringComparison.Ordinal))
            {
                filePath = '/' + filePath;
            }

            if (NormalizeSourceLineEndings)
            {
                cshtmlContent = cshtmlContent.Replace("\r", "").Replace("\n", LineEnding);
            }

            return new TestRazorProjectItem(
                filePath: filePath,
                physicalPath: fullPath,
                relativePhysicalPath: cshtmlRelativePath,
                basePath: WorkingDirectory,
                fileKind: fileKind ?? FileKind,
                cssScope: cssScope)
            {
                Content = cshtmlContent.TrimStart(),
            };
        }

        protected CompileToCSharpResult CompileToCSharp(string cshtmlContent, bool throwOnFailure=true, string cssScope = null)
        {
            return CompileToCSharp(DefaultFileName, cshtmlContent, throwOnFailure, cssScope: cssScope);
        }

        protected CompileToCSharpResult CompileToCSharp(string cshtmlRelativePath, string cshtmlContent, bool throwOnFailure = true, string fileKind = null, string cssScope = null)
        {
            if (DeclarationOnly && DesignTime)
            {
                throw new InvalidOperationException($"{nameof(DeclarationOnly)} cannot be used with {nameof(DesignTime)}.");
            }

            if (DeclarationOnly && UseTwoPhaseCompilation)
            {
                throw new InvalidOperationException($"{nameof(DeclarationOnly)} cannot be used with {nameof(UseTwoPhaseCompilation)}.");
            }

            if (UseTwoPhaseCompilation)
            {
                // The first phase won't include any metadata references for component discovery. This mirrors
                // what the build does.
                var projectEngine = CreateProjectEngine(Configuration, Array.Empty<MetadataReference>());

                RazorCodeDocument codeDocument;
                foreach (var item in AdditionalRazorItems)
                {
                    // Result of generating declarations
                    codeDocument = projectEngine.ProcessDeclarationOnly(item);
                    Assert.Empty(codeDocument.GetCSharpDocument().Diagnostics);

                    var syntaxTree = Parse(codeDocument.GetCSharpDocument().GeneratedCode, path: item.FilePath);
                    AdditionalSyntaxTrees.Add(syntaxTree);
                }

                // Result of generating declarations
                var projectItem = CreateProjectItem(cshtmlRelativePath, cshtmlContent, fileKind, cssScope);
                codeDocument = projectEngine.ProcessDeclarationOnly(projectItem);
                var declaration = new CompileToCSharpResult
                {
                    BaseCompilation = BaseCompilation.AddSyntaxTrees(AdditionalSyntaxTrees),
                    CodeDocument = codeDocument,
                    Code = codeDocument.GetCSharpDocument().GeneratedCode,
                    Diagnostics = codeDocument.GetCSharpDocument().Diagnostics,
                };

                // Result of doing 'temp' compilation
                var tempAssembly = CompileToAssembly(declaration, throwOnFailure);

                // Add the 'temp' compilation as a metadata reference 
                var references = BaseCompilation.References.Concat(new[] { tempAssembly.Compilation.ToMetadataReference() }).ToArray();
                projectEngine = CreateProjectEngine(Configuration, references);

                // Now update the any additional files
                foreach (var item in AdditionalRazorItems)
                {
                    // Result of generating definition
                    codeDocument = DesignTime ? projectEngine.ProcessDesignTime(item) : projectEngine.Process(item);
                    Assert.Empty(codeDocument.GetCSharpDocument().Diagnostics);

                    // Replace the 'declaration' syntax tree
                    var syntaxTree = Parse(codeDocument.GetCSharpDocument().GeneratedCode, path: item.FilePath);
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
                // This will include the built-in components.
                var projectEngine = CreateProjectEngine(Configuration, BaseCompilation.References.ToArray());

                var projectItem = CreateProjectItem(cshtmlRelativePath, cshtmlContent, fileKind, cssScope);

                RazorCodeDocument codeDocument;
                if (DeclarationOnly)
                {
                    codeDocument = projectEngine.ProcessDeclarationOnly(projectItem);
                }
                else if (DesignTime)
                {
                    codeDocument = projectEngine.ProcessDesignTime(projectItem);
                }
                else
                {
                    codeDocument = projectEngine.Process(projectItem);
                }

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

        protected CompileToAssemblyResult CompileToAssembly(CompileToCSharpResult cSharpResult, bool throwOnFailure = true)
        {
            if (cSharpResult.Diagnostics.Any() && throwOnFailure)
            {
                var diagnosticsLog = string.Join(Environment.NewLine, cSharpResult.Diagnostics.Select(d => d.ToString()).ToArray());
                throw new InvalidOperationException($"Aborting compilation to assembly because RazorCompiler returned nonempty diagnostics: {diagnosticsLog}");
            }

            var syntaxTrees = new[]
            {
                Parse(cSharpResult.Code),
            };

            var compilation = cSharpResult.BaseCompilation.AddSyntaxTrees(syntaxTrees);

            var diagnostics = compilation
                .GetDiagnostics()
                .Where(d => d.Severity != DiagnosticSeverity.Hidden);

            if (diagnostics.Any() && throwOnFailure)
            {
                throw new CompilationFailedException(compilation);
            }
            else if (diagnostics.Any())
            {
                return new CompileToAssemblyResult
                {
                    Compilation = compilation,
                    Diagnostics = diagnostics,
                };
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

            var componentFullTypeName = $"{DefaultRootNamespace}.{Path.GetFileNameWithoutExtension(DefaultFileName)}";
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

        protected static CSharpSyntaxTree Parse(string text, string path = null)
        {
            return (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(text, CSharpParseOptions, path: path);
        }

        protected static string FullTypeName<T>() => typeof(T).FullName.Replace('+', '.');

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

        private class ForceLineEndingPhase : RazorEnginePhaseBase
        {
            public ForceLineEndingPhase(string lineEnding)
            {
                LineEnding = lineEnding;
            }

            public string LineEnding { get; }

            protected override void ExecuteCore(RazorCodeDocument codeDocument)
            {
                var field = typeof(CodeRenderingContext).GetField("NewLineString", BindingFlags.Static | BindingFlags.NonPublic);
                var key = field.GetValue(null);
                codeDocument.Items[key] = LineEnding;
            }
        }

        private class TestImportProjectFeature : IImportProjectFeature
        {
            private List<RazorProjectItem> _imports;

            public TestImportProjectFeature(List<RazorProjectItem> imports)
            {
                _imports = imports;
            }

            public RazorProjectEngine ProjectEngine { get; set; }

            public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
            {
                return _imports;
            }
        }
    }
}
