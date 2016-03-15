// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
#if NETSTANDARD1_5
using System.Runtime.Loader;
#endif
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// A type that uses Roslyn to compile C# content.
    /// </summary>
    public class DefaultRoslynCompilationService : ICompilationService
    {
        private readonly Lazy<bool> _supportsPdbGeneration = new Lazy<bool>(SymbolsUtility.SupportsSymbolsGeneration);
        private readonly ConcurrentDictionary<string, AssemblyMetadata> _metadataFileCache =
            new ConcurrentDictionary<string, AssemblyMetadata>(StringComparer.OrdinalIgnoreCase);
        private readonly IFileProvider _fileProvider;
        private readonly Lazy<List<MetadataReference>> _applicationReferences;
        private readonly Action<RoslynCompilationContext> _compilationCallback;
        private readonly CSharpParseOptions _parseOptions;
        private readonly CSharpCompilationOptions _compilationOptions;
        private readonly ILogger _logger;
        private readonly DependencyContext _dependencyContext;

#if NETSTANDARD1_5
        private readonly RazorLoadContext _razorLoadContext;
#endif

        /// <summary>
        /// Initalizes a new instance of the <see cref="DefaultRoslynCompilationService"/> class.
        /// </summary>
        /// <param name="environment">The <see cref="IHostingEnvironment"/>.</param>
        /// <param name="optionsAccessor">Accessor to <see cref="RazorViewEngineOptions"/>.</param>
        /// <param name="fileProviderAccessor">The <see cref="IRazorViewEngineFileProviderAccessor"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DefaultRoslynCompilationService(
            IHostingEnvironment environment,
            IOptions<RazorViewEngineOptions> optionsAccessor,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            ILoggerFactory loggerFactory)
            : this(
                  GetDependencyContext(environment),
                  optionsAccessor.Value,
                  fileProviderAccessor,
                  loggerFactory)
        {
        }

        // Internal for unit testing
        internal DefaultRoslynCompilationService(
            DependencyContext dependencyContext,
            RazorViewEngineOptions viewEngineOptions,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            ILoggerFactory loggerFactory)
        {
            _dependencyContext = dependencyContext;
            _applicationReferences = new Lazy<List<MetadataReference>>(GetApplicationReferences);
            _fileProvider = fileProviderAccessor.FileProvider;
            _compilationCallback = viewEngineOptions.CompilationCallback;
            _parseOptions = viewEngineOptions.ParseOptions;
            _compilationOptions = viewEngineOptions.CompilationOptions;
            _logger = loggerFactory.CreateLogger<DefaultRoslynCompilationService>();

#if NETSTANDARD1_5
            _razorLoadContext = new RazorLoadContext();
#endif

        }

        /// <inheritdoc />
        public CompilationResult Compile(RelativeFileInfo fileInfo, string compilationContent)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            if (compilationContent == null)
            {
                throw new ArgumentNullException(nameof(compilationContent));
            }

            _logger.GeneratedCodeToAssemblyCompilationStart(fileInfo.RelativePath);

            var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;

            var assemblyName = Path.GetRandomFileName();

            var sourceText = SourceText.From(compilationContent, Encoding.UTF8);
            var syntaxTree = CSharpSyntaxTree.ParseText(
                sourceText,
                path: assemblyName,
                options: _parseOptions);

            var references = _applicationReferences.Value;

            var compilation = CSharpCompilation.Create(
                assemblyName,
                options: _compilationOptions,
                syntaxTrees: new[] { syntaxTree },
                references: references);

            compilation = Rewrite(compilation);

            var compilationContext = new RoslynCompilationContext(compilation);
            _compilationCallback(compilationContext);
            compilation = compilationContext.Compilation;

            using (var ms = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    EmitResult result;

                    if (_supportsPdbGeneration.Value)
                    {
                        result = compilation.Emit(ms, pdbStream: pdb);
                    }
                    else
                    {
                        result = compilation.Emit(ms);
                    }

                    if (!result.Success)
                    {
                        return GetCompilationFailedResult(
                            fileInfo.RelativePath,
                            compilationContent,
                            assemblyName,
                            result.Diagnostics);
                    }

                    Assembly assembly;
                    ms.Seek(0, SeekOrigin.Begin);

                    if (_supportsPdbGeneration.Value)
                    {
                        pdb.Seek(0, SeekOrigin.Begin);
                        assembly = LoadStream(ms, pdb);
                    }
                    else
                    {
                        assembly = LoadStream(ms, assemblySymbols: null);
                    }

                    var type = assembly.GetExportedTypes().FirstOrDefault(a => !a.IsNested);
                    _logger.GeneratedCodeToAssemblyCompilationEnd(fileInfo.RelativePath, startTimestamp);

                    return new CompilationResult(type);
                }
            }
        }

        private Assembly LoadStream(MemoryStream ms, MemoryStream assemblySymbols)
        {
#if NET451
            return Assembly.Load(ms.ToArray(), assemblySymbols?.ToArray());
#else
            return _razorLoadContext.Load(ms, assemblySymbols);
#endif
        }

        private CSharpCompilation Rewrite(CSharpCompilation compilation)
        {
            var rewrittenTrees = new List<SyntaxTree>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
                var rewriter = new ExpressionRewriter(semanticModel);

                var rewrittenTree = tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
                rewrittenTrees.Add(rewrittenTree);
            }

            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(rewrittenTrees);
        }

        // Internal for unit testing
        internal CompilationResult GetCompilationFailedResult(
            string relativePath,
            string compilationContent,
            string assemblyName,
            IEnumerable<Diagnostic> diagnostics)
        {
            var diagnosticGroups = diagnostics
                .Where(IsError)
                .GroupBy(diagnostic => GetFilePath(relativePath, diagnostic), StringComparer.Ordinal);

            var failures = new List<CompilationFailure>();
            foreach (var group in diagnosticGroups)
            {
                var sourceFilePath = group.Key;
                string sourceFileContent;
                if (string.Equals(assemblyName, sourceFilePath, StringComparison.Ordinal))
                {
                    // The error is in the generated code and does not have a mapping line pragma
                    sourceFileContent = compilationContent;
                    sourceFilePath = Resources.GeneratedCodeFileName;
                }
                else
                {
                    sourceFileContent = ReadFileContentsSafely(_fileProvider, sourceFilePath);
                }

                var compilationFailure = new CompilationFailure(
                    sourceFilePath,
                    sourceFileContent,
                    compilationContent,
                    group.Select(GetDiagnosticMessage));

                failures.Add(compilationFailure);
            }

            return new CompilationResult(failures);
        }

        private static string GetFilePath(string relativePath, Diagnostic diagnostic)
        {
            if (diagnostic.Location == Location.None)
            {
                return relativePath;
            }

            return diagnostic.Location.GetMappedLineSpan().Path;
        }

        private List<MetadataReference> GetApplicationReferences()
        {
            var metadataReferences = new List<MetadataReference>();
            if (_dependencyContext == null)
            {
                // Avoid null ref if the entry point does not have DependencyContext specified.
                return metadataReferences;
            }

            for (var i = 0; i < _dependencyContext.CompileLibraries.Count; i++)
            {
                var library = _dependencyContext.CompileLibraries[i];
                IEnumerable<string> referencePaths;
                try
                {
                    referencePaths = library.ResolveReferencePaths();
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                metadataReferences.AddRange(referencePaths.Select(CreateMetadataFileReference));
            }

            return metadataReferences;
        }

        private MetadataReference CreateMetadataFileReference(string path)
        {
            var metadata = _metadataFileCache.GetOrAdd(path, _ =>
            {
                using (var stream = File.OpenRead(path))
                {
                    var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                    return AssemblyMetadata.Create(moduleMetadata);
                }
            });

            return metadata.GetReference(filePath: path);
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }

        private static string ReadFileContentsSafely(IFileProvider fileProvider, string filePath)
        {
            var fileInfo = fileProvider.GetFileInfo(filePath);
            if (fileInfo.Exists)
            {
                try
                {
                    using (var reader = new StreamReader(fileInfo.CreateReadStream()))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch
                {
                    // Ignore any failures
                }
            }

            return null;
        }

        private static DiagnosticMessage GetDiagnosticMessage(Diagnostic diagnostic)
        {
            var mappedLineSpan = diagnostic.Location.GetMappedLineSpan();
            return new DiagnosticMessage(
                diagnostic.GetMessage(),
                CSharpDiagnosticFormatter.Instance.Format(diagnostic),
                mappedLineSpan.Path,
                mappedLineSpan.StartLinePosition.Line + 1,
                mappedLineSpan.StartLinePosition.Character + 1,
                mappedLineSpan.EndLinePosition.Line + 1,
                mappedLineSpan.EndLinePosition.Character + 1);
        }

        private static DependencyContext GetDependencyContext(IHostingEnvironment environment)
        {
            if (environment.ApplicationName != null)
            {
                var applicationAssembly = Assembly.Load(new AssemblyName(environment.ApplicationName));
                return DependencyContext.Load(applicationAssembly);
            }

            return null;
        }

#if NETSTANDARD1_5
        private class RazorLoadContext : AssemblyLoadContext
        {
            protected override Assembly Load(AssemblyName assemblyName)
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }

            public Assembly Load(Stream assembly, Stream assemblySymbols)
            {
                return LoadFromStream(assembly, assemblySymbols);
            }
        }
#endif
    }
}
