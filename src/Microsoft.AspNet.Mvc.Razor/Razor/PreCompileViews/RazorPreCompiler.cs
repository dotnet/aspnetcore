// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPreCompiler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileSystem _fileSystem;

        public RazorPreCompiler([NotNull] IServiceProvider designTimeServiceProvider,
                                [NotNull] IMemoryCache precompilationCache,
                                [NotNull] CompilationSettings compilationSettings) :
            this(designTimeServiceProvider,
                 designTimeServiceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>(),
                 precompilationCache,
                 compilationSettings)
        {
        }

        public RazorPreCompiler([NotNull] IServiceProvider designTimeServiceProvider,
                                [NotNull] IOptions<RazorViewEngineOptions> optionsAccessor,
                                [NotNull] IMemoryCache precompilationCache,
                                [NotNull] CompilationSettings compilationSettings)
        {
            _serviceProvider = designTimeServiceProvider;
            _fileSystem = optionsAccessor.Options.FileSystem;
            CompilationSettings = compilationSettings;
            PreCompilationCache = precompilationCache;
        }

        protected CompilationSettings CompilationSettings { get; }

        protected IMemoryCache PreCompilationCache { get; }

        protected virtual string FileExtension { get; } = ".cshtml";

        protected virtual int MaxDegreesOfParallelism { get; } = Environment.ProcessorCount;


        public virtual void CompileViews([NotNull] IBeforeCompileContext context)
        {
            var descriptors = CreateCompilationDescriptors(context);

            if (descriptors.Any())
            {
                var collectionGenerator = new RazorFileInfoCollectionGenerator(
                                                descriptors,
                                                CompilationSettings);

                var tree = collectionGenerator.GenerateCollection();
                context.CSharpCompilation = context.CSharpCompilation.AddSyntaxTrees(tree);
            }
        }

        protected virtual IEnumerable<RazorFileInfo> CreateCompilationDescriptors(
            [NotNull] IBeforeCompileContext context)
        {
            var filesToProcess = new List<RelativeFileInfo>();
            GetFileInfosRecursive(root: string.Empty, razorFiles: filesToProcess);

            var razorFiles = new RazorFileInfo[filesToProcess.Count];
            var syntaxTrees = new SyntaxTree[filesToProcess.Count];
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreesOfParallelism };
            var diagnosticsLock = new object();

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
                        lock (diagnosticsLock)
                        {
                            foreach (var diagnostic in cacheEntry.Diagnostics)
                            {
                                context.Diagnostics.Add(diagnostic);
                            }
                        }
                    }
                }
            });

            context.CSharpCompilation = context.CSharpCompilation
                                               .AddSyntaxTrees(syntaxTrees.Where(tree => tree != null));
            return razorFiles.Where(file => file != null);
        }

        protected IMvcRazorHost GetRazorHost()
        {
            return _serviceProvider.GetRequiredService<IMvcRazorHost>();
        }

        private PrecompilationCacheEntry OnCacheMiss(ICacheSetContext cacheSetContext)
        {
            var fileInfo = (RelativeFileInfo)cacheSetContext.State;
            var entry = GetCacheEntry(fileInfo);

            if (entry != null)
            {
                cacheSetContext.AddExpirationTrigger(_fileSystem.Watch(fileInfo.RelativePath));
                foreach (var viewStartPath in ViewStartUtility.GetViewStartLocations(fileInfo.RelativePath))
                {
                    cacheSetContext.AddExpirationTrigger(_fileSystem.Watch(viewStartPath));
                }
            }

            return entry;
        }

        private void GetFileInfosRecursive(string root, List<RelativeFileInfo> razorFiles)
        {
            var fileInfos = _fileSystem.GetDirectoryContents(root);

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
                        var hash = RazorFileHash.GetHash(fileInfo.FileInfo);
                        var razorFileInfo = new RazorFileInfo
                        {
                            RelativePath = fileInfo.RelativePath,
                            LastModified = fileInfo.FileInfo.LastModified,
                            Length = fileInfo.FileInfo.Length,
                            FullTypeName = fullTypeName
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
    }
}
