// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPreCompiler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileProvider _fileProvider;

        public RazorPreCompiler([NotNull] IServiceProvider designTimeServiceProvider,
                                [NotNull] IBeforeCompileContext compileContext,
                                [NotNull] IMemoryCache precompilationCache,
                                [NotNull] CompilationSettings compilationSettings) :
            this(designTimeServiceProvider,
                 compileContext,
                 designTimeServiceProvider.GetRequiredService<IAssemblyLoadContextAccessor>(),
                 designTimeServiceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>(),
                 precompilationCache,
                 compilationSettings)
        {
        }

        public RazorPreCompiler([NotNull] IServiceProvider designTimeServiceProvider,
                                [NotNull] IBeforeCompileContext compileContext,
                                [NotNull] IAssemblyLoadContextAccessor loadContextAccessor,
                                [NotNull] IOptions<RazorViewEngineOptions> optionsAccessor,
                                [NotNull] IMemoryCache precompilationCache,
                                [NotNull] CompilationSettings compilationSettings)
        {
            _serviceProvider = designTimeServiceProvider;
            CompileContext = compileContext;
            LoadContext = loadContextAccessor.GetLoadContext(GetType().GetTypeInfo().Assembly);
            _fileProvider = optionsAccessor.Options.FileProvider;
            CompilationSettings = compilationSettings;
            PreCompilationCache = precompilationCache;
            TagHelperTypeResolver = new PrecompilationTagHelperTypeResolver(CompileContext, LoadContext);
        }

        /// <summary>
        /// Gets or sets a value that determines if symbols (.pdb) file for the precompiled views is generated.
        /// </summary>
        public bool GenerateSymbols { get; set; }

        protected IBeforeCompileContext CompileContext { get; }

        protected IAssemblyLoadContext LoadContext { get; }

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
                var collectionGenerator = new RazorFileInfoCollectionGenerator(
                                                result,
                                                CompilationSettings);

                var tree = collectionGenerator.GenerateCollection();
                CompileContext.Compilation = CompileContext.Compilation.AddSyntaxTrees(tree);
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
                var cacheEntry = PreCompilationCache.GetOrSet(file.RelativePath,
                                                              file,
                                                              OnCacheMiss);
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

            return GeneratePrecompiledAssembly(syntaxTrees.Where(tree => tree != null),
                                               razorFiles.Where(file => file != null));
        }

        protected virtual RazorFileInfoCollection GeneratePrecompiledAssembly(
            [NotNull] IEnumerable<SyntaxTree> syntaxTrees,
            [NotNull] IEnumerable<RazorFileInfo> razorFileInfos)
        {
            var resourcePrefix = string.Join(".", CompileContext.Compilation.AssemblyName,
                                                  nameof(RazorPreCompiler),
                                                  Path.GetRandomFileName());
            var assemblyResourceName = resourcePrefix + ".dll";


            var applicationReference = CompileContext.Compilation.ToMetadataReference();
            var references = CompileContext.Compilation.References
                                                             .Concat(new[] { applicationReference });

            var preCompilationOptions = CompilationSettings.CompilationOptions
                                                           .WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create(assemblyResourceName,
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
                assemblyStream.Position = 0;
                var assemblyResource = new ResourceDescription(assemblyResourceName,
                                                               () => assemblyStream,
                                                               isPublic: true);
                CompileContext.Resources.Add(assemblyResource);

                string symbolsResourceName = null;
                if (pdbStream != null)
                {
                    symbolsResourceName = resourcePrefix + ".pdb";
                    pdbStream.Position = 0;

                    var pdbResource = new ResourceDescription(symbolsResourceName,
                                                              () => pdbStream,
                                                              isPublic: true);

                    CompileContext.Resources.Add(pdbResource);
                }

                return new PrecompileRazorFileInfoCollection(assemblyResourceName,
                                                             symbolsResourceName,
                                                             razorFileInfos.ToList());
            }
        }

        protected IMvcRazorHost GetRazorHost()
        {
            var descriptorResolver = new TagHelperDescriptorResolver(TagHelperTypeResolver);
            return new MvcRazorHost(new DefaultCodeTreeCache(_fileProvider))
            {
                TagHelperDescriptorResolver = descriptorResolver
            };
        }

        private PrecompilationCacheEntry OnCacheMiss(ICacheSetContext cacheSetContext)
        {
            var fileInfo = (RelativeFileInfo)cacheSetContext.State;
            var entry = GetCacheEntry(fileInfo);

            if (entry != null)
            {
                cacheSetContext.AddExpirationTrigger(_fileProvider.Watch(fileInfo.RelativePath));
                foreach (var path in ViewHierarchyUtility.GetGlobalImportLocations(fileInfo.RelativePath))
                {
                    cacheSetContext.AddExpirationTrigger(_fileProvider.Watch(path));
                }
            }

            return entry;
        }

        private void GetFileInfosRecursive(string root, List<RelativeFileInfo> razorFiles)
        {
            var fileInfos = _fileProvider.GetDirectoryContents(root);

            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.IsDirectory)
                {
                    var subPath = Path.Combine(root, fileInfo.Name);
                    GetFileInfosRecursive(subPath, razorFiles);
                }
                else if (Path.GetExtension(fileInfo.Name)
                         .Equals(FileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = Path.Combine(root, fileInfo.Name);
                    var info = new RelativeFileInfo(fileInfo, relativePath);
                    razorFiles.Add(info);
                }
            }
        }

        protected virtual PrecompilationCacheEntry GetCacheEntry([NotNull] RelativeFileInfo fileInfo)
        {
            using (var stream = fileInfo.FileInfo.CreateReadStream())
            {
                var host = GetRazorHost();
                var results = host.GenerateCode(fileInfo.RelativePath, stream);

                if (results.Success)
                {
                    var syntaxTree = SyntaxTreeGenerator.Generate(results.GeneratedCode,
                                                                  fileInfo.FileInfo.PhysicalPath,
                                                                  CompilationSettings);
                    var fullTypeName = results.GetMainClassName(host, syntaxTree);

                    if (fullTypeName != null)
                    {
                        var hashAlgorithmVersion = RazorFileHash.HashAlgorithmVersion1;
                        var hash = RazorFileHash.GetHash(fileInfo.FileInfo, hashAlgorithmVersion);
                        var razorFileInfo = new RazorFileInfo
                        {
                            RelativePath = fileInfo.RelativePath,
                            LastModified = fileInfo.FileInfo.LastModified,
                            Length = fileInfo.FileInfo.Length,
                            FullTypeName = fullTypeName,
                            Hash = hash,
                            HashAlgorithmVersion = hashAlgorithmVersion
                        };

                        return new PrecompilationCacheEntry(razorFileInfo, syntaxTree);
                    }
                }
                else
                {
                    var diagnostics = results.ParserErrors
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

        private class PrecompileRazorFileInfoCollection : RazorFileInfoCollection
        {
            public PrecompileRazorFileInfoCollection(string assemblyResourceName,
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
