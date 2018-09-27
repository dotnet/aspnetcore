// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if NET46
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using Xunit.Sdk;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    [IntializeTestFile]
    public abstract class IntegrationTestBase
    {
#if !NET46
        private static readonly AsyncLocal<string> _fileName = new AsyncLocal<string>();
#endif

        private static readonly CSharpCompilation DefaultBaseCompilation;

        static IntegrationTestBase()
        {
            var referenceAssemblyRoots = new[]
            {
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly, // System.Runtime
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
        }

        protected IntegrationTestBase(bool? generateBaselines = null)
        {
            TestProjectRoot = TestProject.GetProjectDirectory(GetType());

            if (generateBaselines.HasValue)
            {
                GenerateBaselines = generateBaselines.Value;
            }
        }

        /// <summary>
        /// Gets the <see cref="CSharpCompilation"/> that will be used as the 'app' compilation.
        /// </summary>
        protected virtual CSharpCompilation BaseCompilation => DefaultBaseCompilation;

        /// <summary>
        /// Gets the parse options applied when using <see cref="AddCSharpSyntaxTree(string, string)"/>.
        /// </summary>
        protected virtual CSharpParseOptions CSharpParseOptions { get; } = new CSharpParseOptions(LanguageVersion.Latest);

        /// <summary>
        /// Gets the compilation options applied when compiling assemblies.
        /// </summary>
        protected virtual CSharpCompilationOptions CSharpCompilationOptions { get; } = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        /// <summary>
        /// Gets a list of CSharp syntax trees used that are considered part of the 'app'.
        /// </summary>
        protected virtual List<CSharpSyntaxTree> CSharpSyntaxTrees { get; } = new List<CSharpSyntaxTree>();

        /// <summary>
        /// Gets the <see cref="RazorConfiguration"/> that will be used for code generation.
        /// </summary>
        protected virtual RazorConfiguration Configuration { get; } = RazorConfiguration.Default;

        protected virtual bool DesignTime { get; } = false;

        /// <summary>
        /// Gets the 
        /// </summary>
        internal VirtualRazorProjectFileSystem FileSystem { get; } = new VirtualRazorProjectFileSystem();

        /// <summary>
        /// Used to force a specific style of line-endings for testing. This matters for the baseline tests that exercise line mappings. 
        /// Even though we normalize newlines for testing, the difference between platforms affects the data through the *count* of 
        /// characters written.
        /// </summary>
        protected virtual string LineEnding { get; } = "\r\n";

#if GENERATE_BASELINES
        protected bool GenerateBaselines { get; } = true;
#else
        protected bool GenerateBaselines { get; } = false;
#endif

        protected string TestProjectRoot { get; }

        // Used by the test framework to set the 'base' name for test files.
        public static string FileName
        {
#if NET46
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("IntegrationTestBase_FileName");
                return (string)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("IntegrationTestBase_FileName", new ObjectHandle(value));
            }
#elif NETCOREAPP2_2
            get { return _fileName.Value; }
            set { _fileName.Value = value; }
#endif
        }

        protected virtual void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        {
        }

        protected CSharpSyntaxTree AddCSharpSyntaxTree(string text, string filePath = null)
        {
            var syntaxTree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(text, CSharpParseOptions, path: filePath);
            CSharpSyntaxTrees.Add(syntaxTree);
            return syntaxTree;
        }

        protected RazorProjectItem AddProjectItemFromText(string text, string filePath = "_ViewImports.cshtml")
        {
            var projectItem = CreateProjectItemFromText(text, filePath);
            FileSystem.Add(projectItem);
            return projectItem;
        }

        private RazorProjectItem CreateProjectItemFromText(string text, string filePath)
        {
            // Consider the file path to be relative to the 'FileName' of the test.
            var workingDirectory = Path.GetDirectoryName(FileName);

            // Since these paths are used in baselines, we normalize them to windows style. We
            // use "" as the base path by convention to avoid baking in an actual file system
            // path.
            var basePath = "";
            var physicalPath = Path.Combine(workingDirectory, filePath).Replace('/', '\\');
            var relativePhysicalPath = physicalPath;

            // FilePaths in Razor are **always** are of the form '/a/b/c.cshtml'
            filePath = physicalPath.Replace('\\', '/');
            if (!filePath.StartsWith("/"))
            {
                filePath = '/' + filePath;
            }

            text = NormalizeNewLines(text);

            var projectItem = new TestRazorProjectItem(
                basePath: basePath,
                filePath: filePath,
                physicalPath: physicalPath,
                relativePhysicalPath: relativePhysicalPath)
            {
                Content = text,
            };
            
            return projectItem;
        }

        protected RazorProjectItem CreateProjectItemFromFile(string filePath = null)
        {
            if (FileName == null)
            {
                var message = $"{nameof(CreateProjectItemFromFile)} should only be called from an integration test, ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var suffixIndex = FileName.LastIndexOf("_");
            var normalizedFileName = suffixIndex == -1 ? FileName : FileName.Substring(0, suffixIndex);
            var sourceFileName = Path.ChangeExtension(normalizedFileName, ".cshtml");
            var testFile = TestFile.Create(sourceFileName, GetType().GetTypeInfo().Assembly);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {sourceFileName} was not found.");
            }
            var fileContent = testFile.ReadAllText();
            var normalizedContent = NormalizeNewLines(fileContent);

            var workingDirectory = Path.GetDirectoryName(FileName);
            var fullPath = sourceFileName;

            // Normalize to forward-slash - these strings end up in the baselines.
            fullPath = fullPath.Replace('\\', '/');
            sourceFileName = sourceFileName.Replace('\\', '/');

            // FilePaths in Razor are **always** are of the form '/a/b/c.cshtml'
            filePath = filePath ?? sourceFileName;
            if (!filePath.StartsWith("/"))
            {
                filePath = '/' + filePath;
            }

            var projectItem = new TestRazorProjectItem(
                basePath: workingDirectory,
                filePath: filePath,
                physicalPath: fullPath,
                relativePhysicalPath: sourceFileName)
            {
                Content = fileContent,
            };
            
            return projectItem;
        }

        protected CompiledCSharpCode CompileToCSharp(string text, string path = "test.cshtml", bool? designTime = null)
        {
            var projectItem = CreateProjectItemFromText(text, path);
            return CompileToCSharp(projectItem, designTime);
        }

        protected CompiledCSharpCode CompileToCSharp(RazorProjectItem projectItem, bool? designTime = null)
        {
            var compilation = CreateCompilation();
            var references = compilation.References.Concat(new[] { compilation.ToMetadataReference(), }).ToArray();

            var projectEngine = CreateProjectEngine(Configuration, references, ConfigureProjectEngine);
            var codeDocument = (designTime ?? DesignTime) ? projectEngine.ProcessDesignTime(projectItem) : projectEngine.Process(projectItem);

            return new CompiledCSharpCode(CSharpCompilation.Create(compilation.AssemblyName + ".Views", references: references, options: CSharpCompilationOptions), codeDocument);
        }

        protected CompiledAssembly CompileToAssembly(string text, string path = "test.cshtml", bool? designTime = null, bool throwOnFailure = true)
        {
            var compiled = CompileToCSharp(text, path, designTime);
            return CompileToAssembly(compiled);
        }

        protected CompiledAssembly CompileToAssembly(RazorProjectItem projectItem, bool? designTime = null, bool throwOnFailure = true)
        {
            var compiled = CompileToCSharp(projectItem, designTime);
            return CompileToAssembly(compiled, throwOnFailure: throwOnFailure);
        }

        protected CompiledAssembly CompileToAssembly(CompiledCSharpCode code, bool throwOnFailure = true)
        {
            var cSharpDocument = code.CodeDocument.GetCSharpDocument();
            if (cSharpDocument.Diagnostics.Any())
            {
                var diagnosticsLog = string.Join(Environment.NewLine, cSharpDocument.Diagnostics.Select(d => d.ToString()).ToArray());
                throw new InvalidOperationException($"Aborting compilation to assembly because RazorCompiler returned nonempty diagnostics: {diagnosticsLog}");
            }

            var syntaxTrees = new[]
            {
                (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(cSharpDocument.GeneratedCode, CSharpParseOptions, path: code.CodeDocument.Source.FilePath),
            };

            var compilation = code.BaseCompilation.AddSyntaxTrees(syntaxTrees);

            var diagnostics = compilation
                .GetDiagnostics()
                .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                .ToArray();

            if (diagnostics.Length > 0 && throwOnFailure)
            {
                throw new CompilationFailedException(compilation, diagnostics);
            }
            else if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return new CompiledAssembly(compilation, code.CodeDocument, assembly: null);
            }

            using (var peStream = new MemoryStream())
            {
                var emit = compilation.Emit(peStream);
                diagnostics = emit
                    .Diagnostics
                    .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                    .ToArray();
                if (diagnostics.Length > 0 && throwOnFailure)
                {
                    throw new CompilationFailedException(compilation, diagnostics);
                }

                return new CompiledAssembly(compilation, code.CodeDocument, Assembly.Load(peStream.ToArray()));
            }
        }

        private CSharpCompilation CreateCompilation()
        {
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTrees);
            var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToArray();
            if (diagnostics.Length > 0)
            {
                throw new CompilationFailedException(compilation, diagnostics);
            }

            return compilation;
        }

        protected RazorProjectEngine CreateProjectEngine(Action<RazorProjectEngineBuilder> configure = null)
        {
            var compilation = CreateCompilation();
            var references = compilation.References.Concat(new[] { compilation.ToMetadataReference(), }).ToArray();
            return CreateProjectEngine(Configuration, references, configure);
        }

        private RazorProjectEngine CreateProjectEngine(RazorConfiguration configuration, MetadataReference[] references, Action<RazorProjectEngineBuilder> configure)
        {
            return RazorProjectEngine.Create(configuration, FileSystem, b =>
            {
                b.Phases.Insert(0, new ConfigureCodeRenderingPhase(LineEnding));

                configure?.Invoke(b);

                // Allow the test to do custom things with tag helpers, but do the default thing most of the time.
                if (!b.Features.OfType<ITagHelperFeature>().Any())
                {
                    b.Features.Add(new CompilationTagHelperFeature());
                    b.Features.Add(new DefaultMetadataReferenceFeature()
                    {
                        References = references,
                    });
                }

                // Decorate the import feature so we can normalize line endings.
                var importFeature = b.Features.OfType<IImportProjectFeature>().FirstOrDefault();
                b.Features.Add(new NormalizedDefaultImportFeature(importFeature, LineEnding));
                b.Features.Remove(importFeature);
            });
        }

        protected void AssertDocumentNodeMatchesBaseline(DocumentIntermediateNode document)
        {
            if (FileName == null)
            {
                var message = $"{nameof(AssertDocumentNodeMatchesBaseline)} should only be called from an integration test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var baselineFileName = Path.ChangeExtension(FileName, ".ir.txt");

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, IntermediateNodeSerializer.Serialize(document));
                return;
            }

            var irFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!irFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = irFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            IntermediateNodeVerifier.Verify(document, baseline);
        }

        protected void AssertCSharpDocumentMatchesBaseline(RazorCSharpDocument document)
        {
            if (FileName == null)
            {
                var message = $"{nameof(AssertCSharpDocumentMatchesBaseline)} should only be called from an integration test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var baselineFileName = Path.ChangeExtension(FileName, ".codegen.cs");
            var baselineDiagnosticsFileName = Path.ChangeExtension(FileName, ".diagnostics.txt");

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, document.GeneratedCode);

                var baselineDiagnosticsFullPath = Path.Combine(TestProjectRoot, baselineDiagnosticsFileName);
                var lines = document.Diagnostics.Select(RazorDiagnosticSerializer.Serialize).ToArray();
                if (lines.Any())
                {
                    File.WriteAllLines(baselineDiagnosticsFullPath, lines);
                }
                else if (File.Exists(baselineDiagnosticsFullPath))
                {
                    File.Delete(baselineDiagnosticsFullPath);
                }

                return;
            }

            var codegenFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!codegenFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = codegenFile.ReadAllText();

            // Normalize newlines to match those in the baseline.
            var actual = document.GeneratedCode.Replace("\r", "").Replace("\n", "\r\n");
            Assert.Equal(baseline, actual);

            var baselineDiagnostics = string.Empty;
            var diagnosticsFile = TestFile.Create(baselineDiagnosticsFileName, GetType().GetTypeInfo().Assembly);
            if (diagnosticsFile.Exists())
            {
                baselineDiagnostics = diagnosticsFile.ReadAllText();
            }

            var actualDiagnostics = string.Concat(document.Diagnostics.Select(d => RazorDiagnosticSerializer.Serialize(d) + "\r\n"));
            Assert.Equal(baselineDiagnostics, actualDiagnostics);
        }

        protected void AssertSourceMappingsMatchBaseline(RazorCodeDocument document)
        {
            if (FileName == null)
            {
                var message = $"{nameof(AssertSourceMappingsMatchBaseline)} should only be called from an integration test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var csharpDocument = document.GetCSharpDocument();
            Assert.NotNull(csharpDocument);

            var baselineFileName = Path.ChangeExtension(FileName, ".mappings.txt");
            var serializedMappings = SourceMappingsSerializer.Serialize(csharpDocument, document.Source);

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, serializedMappings);
                return;
            }

            var testFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = testFile.ReadAllText();

            // Normalize newlines to match those in the baseline.
            var actual = serializedMappings.Replace("\r", "").Replace("\n", "\r\n");

            Assert.Equal(baseline, actual);
        }

        private string NormalizeNewLines(string content)
        {
            return NormalizeNewLines(content, LineEnding);
        }

        private static string NormalizeNewLines(string content, string lineEnding)
        {
            return Regex.Replace(content, "(?<!\r)\n", lineEnding, RegexOptions.None, TimeSpan.FromSeconds(10));
        }

        // This is to prevent you from accidentally checking in with GenerateBaselines = true
        [Fact]
        public void GenerateBaselinesMustBeFalse()
        {
            Assert.False(GenerateBaselines, "GenerateBaselines should be set back to false before you check in!");
        }

        private class ConfigureCodeRenderingPhase : RazorEnginePhaseBase
        {
            public ConfigureCodeRenderingPhase(string lineEnding)
            {
                LineEnding = lineEnding;
            }

            public string LineEnding { get; }

            protected override void ExecuteCore(RazorCodeDocument codeDocument)
            {
                codeDocument.Items[CodeRenderingContext.SuppressUniqueIds] = "test";
                codeDocument.Items[CodeRenderingContext.NewLineString] = LineEnding;
            }
        }

        // 'Default' imports won't have normalized line-endings, which is unfriendly for testing.
        private class NormalizedDefaultImportFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
        {
            private readonly IImportProjectFeature _inner;
            private readonly string _lineEnding;

            public NormalizedDefaultImportFeature(IImportProjectFeature inner, string lineEnding)
            {
                _inner = inner;
                _lineEnding = lineEnding;
            }

            protected override void OnInitialized()
            {
                if (_inner != null)
                {
                    _inner.ProjectEngine = ProjectEngine;
                }
            }

            public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
            {
                if (_inner == null)
                {
                    return Array.Empty<RazorProjectItem>();
                }

                var normalizedImports = new List<RazorProjectItem>();
                var imports = _inner.GetImports(projectItem);
                foreach (var import in imports)
                {
                    if (import.Exists)
                    {
                        var text = string.Empty;
                        using (var stream = import.Read())
                        using (var reader = new StreamReader(stream))
                        {
                            text = reader.ReadToEnd().Trim();
                        }

                        // It's important that we normalize the newlines in the default imports. The default imports will
                        // be created with Environment.NewLine, but we need to normalize to `\r\n` so that the indices
                        // are the same on xplat.
                        var normalizedText = NormalizeNewLines(text, _lineEnding);
                        var normalizedImport = new TestRazorProjectItem(import.FilePath, import.PhysicalPath, import.RelativePhysicalPath, import.BasePath)
                        {
                            Content = normalizedText
                        };

                        normalizedImports.Add(normalizedImport);
                    }
                }

                return normalizedImports;
            }
        }
    }
}
