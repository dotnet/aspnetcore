// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.CodeAnalysis.CSharp;
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
        private readonly IMvcRazorHost _host;

        public RazorPreCompiler([NotNull] IServiceProvider designTimeServiceProvider,
                                [NotNull] CompilationSettings compilationSettings) :
            this(designTimeServiceProvider,
                 designTimeServiceProvider.GetRequiredService<IMvcRazorHost>(),
                 designTimeServiceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>(),
                 compilationSettings)
        {
        }

        public RazorPreCompiler([NotNull] IServiceProvider designTimeServiceProvider,
                                [NotNull] IMvcRazorHost host,
                                [NotNull] IOptions<RazorViewEngineOptions> optionsAccessor,
                                [NotNull] CompilationSettings compilationSettings)
        {
            _serviceProvider = designTimeServiceProvider;
            _host = host;
            _fileSystem = optionsAccessor.Options.FileSystem;
            CompilationSettings = compilationSettings;
        }

        protected CompilationSettings CompilationSettings { get; }

        protected virtual string FileExtension { get; } = ".cshtml";

        public virtual void CompileViews([NotNull] IBeforeCompileContext context)
        {
            var descriptors = CreateCompilationDescriptors(context);

            if (descriptors.Count > 0)
            {
                var collectionGenerator = new RazorFileInfoCollectionGenerator(
                                                descriptors,
                                                CompilationSettings);

                var tree = collectionGenerator.GenerateCollection();
                context.CSharpCompilation = context.CSharpCompilation.AddSyntaxTrees(tree);
            }
        }

        protected virtual IReadOnlyList<RazorFileInfo> CreateCompilationDescriptors(
                                                            [NotNull] IBeforeCompileContext context)
        {
            var list = new List<RazorFileInfo>();

            foreach (var info in GetFileInfosRecursive(string.Empty))
            {
                var descriptor = ParseView(info, context);

                if (descriptor != null)
                {
                    list.Add(descriptor);
                }
            }

            return list;
        }

        private IEnumerable<RelativeFileInfo> GetFileInfosRecursive(string currentPath)
        {
            var path = currentPath;

            var fileInfos = _fileSystem.GetDirectoryContents(path);
            if (!fileInfos.Exists)
            {
                yield break;
            }

            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.IsDirectory)
                {
                    var subPath = Path.Combine(path, fileInfo.Name);

                    foreach (var info in GetFileInfosRecursive(subPath))
                    {
                        yield return info;
                    }
                }
                else if (Path.GetExtension(fileInfo.Name)
                         .Equals(FileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = Path.Combine(currentPath, fileInfo.Name);
                    var info = new RelativeFileInfo(fileInfo, relativePath);
                    yield return info;
                }
            }
        }

        protected virtual RazorFileInfo ParseView([NotNull] RelativeFileInfo fileInfo,
                                                  [NotNull] IBeforeCompileContext context)
        {
            using (var stream = fileInfo.FileInfo.CreateReadStream())
            {
                var results = _host.GenerateCode(fileInfo.RelativePath, stream);

                foreach (var parserError in results.ParserErrors)
                {
                    var diagnostic = parserError.ToDiagnostics(fileInfo.FileInfo.PhysicalPath);
                    context.Diagnostics.Add(diagnostic);
                }

                var generatedCode = results.GeneratedCode;

                if (generatedCode != null)
                {
                    var syntaxTree = SyntaxTreeGenerator.Generate(generatedCode,
                                                                  fileInfo.FileInfo.PhysicalPath,
                                                                  CompilationSettings);
                    var fullTypeName = results.GetMainClassName(_host, syntaxTree);

                    if (fullTypeName != null)
                    {
                        context.CSharpCompilation = context.CSharpCompilation.AddSyntaxTrees(syntaxTree);

                        var hash = RazorFileHash.GetHash(fileInfo.FileInfo);

                        return new RazorFileInfo()
                        {
                            FullTypeName = fullTypeName,
                            RelativePath = fileInfo.RelativePath,
                            LastModified = fileInfo.FileInfo.LastModified,
                            Length = fileInfo.FileInfo.Length,
                            Hash = hash,
                        };
                    }
                }
            }

            return null;
        }
    }
}
