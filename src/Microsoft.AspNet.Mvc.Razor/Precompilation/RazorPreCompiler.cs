// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Razor.Runtime.Precompilation;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.CompilationAbstractions;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    public class RazorPreCompiler
    {
        private const string CacheKeyDirectorySeparator = "/";

        public RazorPreCompiler(
            BeforeCompileContext compileContext,
            IFileProvider fileProvider,
            IMemoryCache precompilationCache)
        {
            if (compileContext == null)
            {
                throw new ArgumentNullException(nameof(compileContext));
            }

            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            if (precompilationCache == null)
            {
                throw new ArgumentNullException(nameof(precompilationCache));
            }

            CompileContext = compileContext;
            FileProvider = fileProvider;
            // There should always be a syntax tree even if there are no files (we generate one)
            Debug.Assert(compileContext.Compilation.SyntaxTrees.Length > 0);
            var defines = compileContext.Compilation.SyntaxTrees[0].Options.PreprocessorSymbolNames;
            CompilationSettings = new CompilationSettings
            {
                CompilationOptions = compileContext.Compilation.Options,
                Defines = defines,
                LanguageVersion = compileContext.Compilation.LanguageVersion
            };
            PreCompilationCache = precompilationCache;
            TagHelperTypeResolver = new PrecompilationTagHelperTypeResolver(CompileContext.Compilation);
        }

        /// <summary>
        /// Gets or sets a value that determines if symbols (.pdb) file for the precompiled views is generated.
        /// </summary>
        public bool GenerateSymbols { get; set; }

        protected IFileProvider FileProvider { get; }

        protected BeforeCompileContext CompileContext { get; }

        protected CompilationSettings CompilationSettings { get; }

        protected IMemoryCache PreCompilationCache { get; }

        protected virtual string FileExtension { get; } = ".cshtml";

        protected virtual int MaxDegreesOfParallelism { get; } = Environment.ProcessorCount;

        protected virtual TagHelperTypeResolver TagHelperTypeResolver { get; }

        public virtual void CompileViews()
        {
            var result = CreateFileInfoCollection();
            if (result != null)
            {
                var generatedCode = RazorFileInfoCollectionGenerator.GenerateCode(result);
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    generatedCode,
                    SyntaxTreeGenerator.GetParseOptions(CompilationSettings));
                CompileContext.Compilation = CompileContext.Compilation.AddSyntaxTrees(syntaxTree);
            }
        }

        protected virtual RazorFileInfoCollection CreateFileInfoCollection()
        {
            var filesToProcess = new List<RelativeFileInfo>();
            GetFileInfosRecursive(root: string.Empty, razorFiles: filesToProcess);
            if (filesToProcess.Count == 0)
            {
                return null;
            }

            var razorFiles = new RazorFileInfo[filesToProcess.Count];
            var syntaxTrees = new SyntaxTree[filesToProcess.Count];
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreesOfParallelism };
            var diagnosticsLock = new object();
            var hasErrors = false;

            Parallel.For(0, filesToProcess.Count, parallelOptions, index =>
            {
                var file = filesToProcess[index];

                PrecompilationCacheEntry cacheEntry;
                if (!PreCompilationCache.TryGetValue(file.RelativePath, out cacheEntry))
                {
                    cacheEntry = GetCacheEntry(file);
                    PreCompilationCache.Set(
                        file.RelativePath,
                        cacheEntry,
                        GetMemoryCacheEntryOptions(file, cacheEntry));
                }

                if (cacheEntry != null)
                {
                    if (cacheEntry.Success)
                    {
                        syntaxTrees[index] = cacheEntry.SyntaxTree;
                        razorFiles[index] = cacheEntry.FileInfo;
                    }
                    else
                    {
                        hasErrors = true;
                        lock (diagnosticsLock)
                        {
                            AddRange(CompileContext.Diagnostics, cacheEntry.Diagnostics);
                        }
                    }
                }
            });

            if (hasErrors)
            {
                // If any of the Razor files had syntax errors, don't emit the precompiled views assembly.
                return null;
            }

            return GeneratePrecompiledAssembly(
                syntaxTrees.Where(tree => tree != null),
                razorFiles.Where(file => file != null));
        }

        protected virtual RazorFileInfoCollection GeneratePrecompiledAssembly(
            IEnumerable<SyntaxTree> syntaxTrees,
            IEnumerable<RazorFileInfo> razorFileInfos)
        {
            if (syntaxTrees == null)
            {
                throw new ArgumentNullException(nameof(syntaxTrees));
            }

            if (razorFileInfos == null)
            {
                throw new ArgumentNullException(nameof(razorFileInfos));
            }

            var resourcePrefix = string.Join(".", CompileContext.Compilation.AssemblyName,
                                                  nameof(RazorPreCompiler),
                                                  Path.GetRandomFileName());
            var assemblyResourceName = resourcePrefix + ".dll";


            var applicationReference = CompileContext.Compilation.ToMetadataReference();
            var references = CompileContext.Compilation.References
                                                             .Concat(new[] { applicationReference });

            var preCompilationOptions = CompilationSettings
                .CompilationOptions
                .WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create(
                assemblyResourceName,
                options: preCompilationOptions,
                syntaxTrees: syntaxTrees,
                references: references);

            var generateSymbols = GenerateSymbols && SymbolsUtility.SupportsSymbolsGeneration();
            // These streams are returned to the runtime and consequently cannot be disposed.
            var assemblyStream = new MemoryStream();
            var pdbStream = generateSymbols ? new MemoryStream() : null;
            var emitResult = compilation.Emit(assemblyStream, pdbStream);
            if (!emitResult.Success)
            {
                AddRange(CompileContext.Diagnostics, emitResult.Diagnostics);
                return null;
            }
            else
            {
                var assemblyResource = new ResourceDescriptor()
                {
                    FileName = Path.GetFileName(assemblyResourceName),
                    Name = assemblyResourceName,
                    StreamFactory = () => GetNonDisposableStream(assemblyStream)
                };
                CompileContext.Resources.Add(assemblyResource);

                string symbolsResourceName = null;
                if (pdbStream != null)
                {
                    symbolsResourceName = resourcePrefix + ".pdb";
                    var pdbResource = new ResourceDescriptor()
                    {
                        FileName = Path.GetFileName(symbolsResourceName),
                        Name = symbolsResourceName,
                        StreamFactory = () => GetNonDisposableStream(pdbStream)
                    };

                    CompileContext.Resources.Add(pdbResource);
                }

                return new PrecompileRazorFileInfoCollection(assemblyResourceName,
                                                             symbolsResourceName,
                                                             razorFileInfos.ToList());
            }
        }

        protected IMvcRazorHost GetRazorHost()
        {
            var descriptorResolver = new TagHelperDescriptorResolver(TagHelperTypeResolver, designTime: false);
            return new MvcRazorHost(new DefaultChunkTreeCache(FileProvider))
            {
                TagHelperDescriptorResolver = descriptorResolver
            };
        }

        private MemoryCacheEntryOptions GetMemoryCacheEntryOptions(
            RelativeFileInfo fileInfo,
            PrecompilationCacheEntry cacheEntry)
        {
            var options = new MemoryCacheEntryOptions();
            options.AddExpirationToken(FileProvider.Watch(fileInfo.RelativePath));
            foreach (var path in ViewHierarchyUtility.GetViewImportsLocations(fileInfo.RelativePath))
            {
                options.AddExpirationToken(FileProvider.Watch(path));
            }
            return options;
        }

        private void GetFileInfosRecursive(string root, List<RelativeFileInfo> razorFiles)
        {
            var fileInfos = FileProvider.GetDirectoryContents(root);

            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.IsDirectory)
                {
                    var subPath = CombinePath(root, fileInfo.Name);
                    GetFileInfosRecursive(subPath, razorFiles);
                }
                else if (Path.GetExtension(fileInfo.Name)
                         .Equals(FileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = CombinePath(root, fileInfo.Name);
                    var info = new RelativeFileInfo(fileInfo, relativePath);
                    razorFiles.Add(info);
                }
            }
        }

        protected virtual PrecompilationCacheEntry GetCacheEntry(RelativeFileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            using (var stream = fileInfo.FileInfo.CreateReadStream())
            {
                var host = GetRazorHost();
                var results = host.GenerateCode(fileInfo.RelativePath, stream);

                if (results.Success)
                {
                    var syntaxTree = SyntaxTreeGenerator.Generate(
                        results.GeneratedCode,
                        fileInfo.FileInfo.PhysicalPath,
                        CompilationSettings);
                    var fullTypeName = results.GetMainClassName(host, syntaxTree);

                    if (fullTypeName != null)
                    {
                        var razorFileInfo = new RazorFileInfo
                        {
                            RelativePath = fileInfo.RelativePath,
                            FullTypeName = fullTypeName
                        };

                        return new PrecompilationCacheEntry(razorFileInfo, syntaxTree);
                    }
                }
                else
                {
                    var diagnostics = results
                        .ParserErrors
                        .Select(error => error.ToDiagnostics(fileInfo.FileInfo.PhysicalPath))
                        .ToList();

                    return new PrecompilationCacheEntry(diagnostics);
                }
            }

            return null;
        }

        private static void AddRange<TVal>(IList<TVal> target, IEnumerable<TVal> source)
        {
            foreach (var diagnostic in source)
            {
                target.Add(diagnostic);
            }
        }

        private static Stream GetNonDisposableStream(Stream stream)
        {
            stream.Position = 0;
            return new NonDisposableStream(stream);
        }

        private static string CombinePath(string root, string name)
        {
            // We use string.Join instead of Path.Combine here to ensure that the path
            // separator we produce matches the one used by the CompilerCache.
            return string.Join(CacheKeyDirectorySeparator, root, name);
        }

        private class PrecompileRazorFileInfoCollection : RazorFileInfoCollection
        {
            public PrecompileRazorFileInfoCollection(
                string assemblyResourceName,
                string symbolsResourceName,
                IReadOnlyList<RazorFileInfo> fileInfos)
            {
                AssemblyResourceName = assemblyResourceName;
                SymbolsResourceName = symbolsResourceName;
                FileInfos = fileInfos;
            }
        }
    }
}
