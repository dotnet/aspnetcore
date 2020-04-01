// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class RazorIntegrationTestBase
    {
        private static readonly AsyncLocal<ITestOutputHelper> _output = new AsyncLocal<ITestOutputHelper>();

        internal const string ArbitraryWindowsPath = "x:\\dir\\subdir\\Test";
        internal const string ArbitraryMacLinuxPath = "/dir/subdir/Test";

        // Creating the initial compilation + reading references is on the order of 250ms without caching
        // so making sure it doesn't happen for each test.
        private static readonly CSharpCompilation BaseCompilation;

        private static CSharpParseOptions CSharpParseOptions { get; }

        static RazorIntegrationTestBase()
        {
            var referenceAssemblyRoots = new[]
            {
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly, // System.Runtime
                typeof(ComponentBase).Assembly,
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

            CSharpParseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        }

        public RazorIntegrationTestBase(ITestOutputHelper output)
        {
            _output.Value = output;

            AdditionalSyntaxTrees = new List<SyntaxTree>();
            AdditionalRazorItems = new List<RazorProjectItem>();

            Configuration = RazorConfiguration.Create(RazorLanguageVersion.Latest, "MVC-3.0", Array.Empty<RazorExtension>());
            FileKind = FileKinds.Component; // Treat input files as components by default.
            FileSystem = new VirtualRazorProjectFileSystem();
            PathSeparator = Path.DirectorySeparatorChar.ToString();
            WorkingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ArbitraryWindowsPath : ArbitraryMacLinuxPath;

            // Many of the rendering tests include line endings in the output.
            LineEnding = "\n";
            NormalizeSourceLineEndings = true;

            DefaultRootNamespace = "Test"; // Matches the default working directory
            DefaultFileName = "TestComponent.cshtml";
        }

        internal List<RazorProjectItem> AdditionalRazorItems { get; }

        internal List<SyntaxTree> AdditionalSyntaxTrees { get; }

        internal virtual RazorConfiguration Configuration { get; }

        internal virtual string DefaultRootNamespace { get; }

        internal virtual string DefaultFileName { get; }

        internal virtual bool DesignTime { get; }

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

        // Intentionally private, we don't want tests messing with this because it's fragile.
        private RazorProjectEngine CreateProjectEngine(MetadataReference[] references)
        {
            return RazorProjectEngine.Create(Configuration, FileSystem, b =>
            {
                b.SetRootNamespace(DefaultRootNamespace);

                // Turn off checksums, we're testing code generation.
                b.Features.Add(new SuppressChecksum());

                if (LineEnding != null)
                {
                    b.Phases.Insert(0, new ForceLineEndingPhase(LineEnding));
                }

                // Including MVC here so that we can find any issues that arise from mixed MVC + Components.
                Microsoft.AspNetCore.Mvc.Razor.Extensions.RazorExtensions.Register(b);

                // Features that use Roslyn are mandatory for components
                Microsoft.CodeAnalysis.Razor.CompilerFeatures.Register(b);

                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultMetadataReferenceFeature()
                {
                    References = references,
                });
            });
        }

        internal RazorProjectItem CreateProjectItem(string cshtmlRelativePath, string cshtmlContent)
        {
            var fullPath = WorkingDirectory + PathSeparator + cshtmlRelativePath;

            // FilePaths in Razor are **always** are of the form '/a/b/c.cshtml'
            var filePath = cshtmlRelativePath.Replace('\\', '/');
            if (!filePath.StartsWith('/'))
            {
                filePath = '/' + filePath;
            }

            if (NormalizeSourceLineEndings)
            {
                cshtmlContent = cshtmlContent.Replace("\r", "").Replace("\n", LineEnding);
            }

            return new VirtualProjectItem(
                WorkingDirectory,
                filePath,
                fullPath,
                cshtmlRelativePath,
                FileKind,
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
                var projectEngine = CreateProjectEngine(Array.Empty<MetadataReference>());

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
                var projectItem = CreateProjectItem(cshtmlRelativePath, cshtmlContent);
                codeDocument = projectEngine.ProcessDeclarationOnly(projectItem);
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
                projectEngine = CreateProjectEngine(references);

                // Now update the any additional files
                foreach (var item in AdditionalRazorItems)
                {
                    // Result of generating declarations
                    codeDocument = DesignTime ? projectEngine.ProcessDesignTime(item) : projectEngine.Process(item);
                    Assert.Empty(codeDocument.GetCSharpDocument().Diagnostics);

                    // Replace the 'declaration' syntax tree
                    var syntaxTree = Parse(codeDocument.GetCSharpDocument().GeneratedCode, path: item.FilePath);
                    AdditionalSyntaxTrees.RemoveAll(st => st.FilePath == item.FilePath);
                    AdditionalSyntaxTrees.Add(syntaxTree);
                }

                // Result of real code generation for the document under test
                codeDocument = DesignTime ? projectEngine.ProcessDesignTime(projectItem) : projectEngine.Process(projectItem);

                _output.Value.WriteLine("Use this output when opening an issue");
                _output.Value.WriteLine(string.Empty);

                _output.Value.WriteLine($"## Main source file ({projectItem.FileKind}):");
                _output.Value.WriteLine("```");
                _output.Value.WriteLine(ReadProjectItem(projectItem));
                _output.Value.WriteLine("```");
                _output.Value.WriteLine(string.Empty);

                foreach (var item in AdditionalRazorItems)
                {
                    _output.Value.WriteLine($"### Additional source file ({item.FileKind}):");
                    _output.Value.WriteLine("```");
                    _output.Value.WriteLine(ReadProjectItem(item));
                    _output.Value.WriteLine("```");
                    _output.Value.WriteLine(string.Empty);
                }

                _output.Value.WriteLine("## Generated C#:");
                _output.Value.WriteLine("```C#");
                _output.Value.WriteLine(codeDocument.GetCSharpDocument().GeneratedCode);
                _output.Value.WriteLine("```");

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
                var projectEngine = CreateProjectEngine(BaseCompilation.References.ToArray());

                var projectItem = CreateProjectItem(cshtmlRelativePath, cshtmlContent);
                var codeDocument = DesignTime ? projectEngine.ProcessDesignTime(projectItem) : projectEngine.Process(projectItem);

                // Log the generated code for test results.
                _output.Value.WriteLine("Use this output when opening an issue");
                _output.Value.WriteLine(string.Empty);

                _output.Value.WriteLine($"## Main source file ({projectItem.FileKind}):");
                _output.Value.WriteLine("```");
                _output.Value.WriteLine(ReadProjectItem(projectItem));
                _output.Value.WriteLine("```");
                _output.Value.WriteLine(string.Empty);

                _output.Value.WriteLine("## Generated C#:");
                _output.Value.WriteLine("```C#");
                _output.Value.WriteLine(codeDocument.GetCSharpDocument().GeneratedCode);
                _output.Value.WriteLine("```");

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
            if (cSharpResult.Diagnostics.Any())
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

        protected RenderTreeFrame[] GetRenderTree(IComponent component)
        {
            var renderer = new TestRenderer();
            return GetRenderTree(renderer, component);
        }

        protected private RenderTreeFrame[] GetRenderTree(TestRenderer renderer, IComponent component)
        {
            renderer.AttachComponent(component);
            var task = renderer.Dispatcher.InvokeAsync(() => component.SetParametersAsync(ParameterView.Empty));
            // we will have to change this method if we add a test that does actual async work.
            Assert.True(task.Status.HasFlag(TaskStatus.RanToCompletion) || task.Status.HasFlag(TaskStatus.Faulted));
            if (task.IsFaulted)
            {
                ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
            }
            return renderer.LatestBatchReferenceFrames;
        }

        protected ArrayRange<RenderTreeFrame> GetFrames(RenderFragment fragment)
        {
            var builder = new RenderTreeBuilder();
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

        private static string ReadProjectItem(RazorProjectItem item)
        {
            using (var reader = new StreamReader(item.Read()))
            {
                return reader.ReadToEnd();
            }
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

        protected class TestRenderer : Renderer
        {
            public TestRenderer() : base(new TestServiceProvider(), NullLoggerFactory.Instance)
            {
            }

            public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

            public RenderTreeFrame[] LatestBatchReferenceFrames { get; private set; }

            public void AttachComponent(IComponent component)
                => AssignRootComponentId(component);

            protected override void HandleException(Exception exception)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            {
                LatestBatchReferenceFrames = renderBatch.ReferenceFrames.AsEnumerable().ToArray();
                return Task.CompletedTask;
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
    }
}
