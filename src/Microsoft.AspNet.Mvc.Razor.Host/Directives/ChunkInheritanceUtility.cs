// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A utility type for supporting inheritance of tag helpers and chunks into a page from applicable _ViewStart
    /// pages.
    /// </summary>
    public class ChunkInheritanceUtility
    {
        private readonly Dictionary<string, CodeTree> _parsedCodeTrees;
        private readonly MvcRazorHost _razorHost;
        private readonly IFileSystem _fileSystem;
        private readonly IEnumerable<Chunk> _defaultInheritedChunks;

        /// <summary>
        /// Initializes a new instance of <see cref="ChunkInheritanceUtility"/>.
        /// </summary>
        /// <param name="razorHost">The <see cref="MvcRazorHost"/> used to parse _ViewStart pages.</param>
        /// <param name="fileSystem">The filesystem that represents the application.</param>
        /// <param name="defaultInheritedChunks">Sequence of <see cref="Chunk"/>s inherited by default.</param>
        public ChunkInheritanceUtility([NotNull] MvcRazorHost razorHost,
                                       [NotNull] IFileSystem fileSystem,
                                       [NotNull] IEnumerable<Chunk> defaultInheritedChunks)
        {
            _razorHost = razorHost;
            _fileSystem = fileSystem;
            _defaultInheritedChunks = defaultInheritedChunks;
            _parsedCodeTrees = new Dictionary<string, CodeTree>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets a <see cref="IReadOnlyList{T}"/> of <see cref="Chunk"/> containing parsed results of _ViewStart files
        /// that are used for inheriting tag helpers and chunks to the page located at <paramref name="pagePath"/>.
        /// </summary>
        /// <param name="pagePath">The path of the page to locate inherited chunks for.</param>
        /// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="Chunk"/> from _ViewStart pages.</returns>
        public IReadOnlyList<Chunk> GetInheritedChunks([NotNull] string pagePath)
        {
            var inheritedChunks = new List<Chunk>();

            var templateEngine = new RazorTemplateEngine(_razorHost);
            foreach (var viewStart in ViewStartUtility.GetViewStartLocations(_fileSystem, pagePath))
            {
                CodeTree codeTree;
                IFileInfo fileInfo;

                if (_parsedCodeTrees.TryGetValue(viewStart, out codeTree))
                {
                    inheritedChunks.AddRange(codeTree.Chunks);
                }
                else if (_fileSystem.TryGetFileInfo(viewStart, out fileInfo))
                {
                    codeTree = ParseViewFile(templateEngine, fileInfo);
                    _parsedCodeTrees.Add(viewStart, codeTree);
                    inheritedChunks.AddRange(codeTree.Chunks);
                }
            }

            inheritedChunks.AddRange(_defaultInheritedChunks);

            return inheritedChunks;
        }

        /// <summary>
        /// Merges a list of chunks into the specified <paramref name="codeTree"/>.
        /// </summary>
        /// <param name="codeTree">The <see cref="CodeTree"/> to merge.</param>
        /// <param name="inherited">The <see credit="IReadOnlyList{T}"/> of <see cref="Chunk"/> to merge.</param>
        /// <param name="defaultModel">The list of chunks to merge.</param>
        public void MergeInheritedChunks([NotNull] CodeTree codeTree,
                                         [NotNull] IReadOnlyList<Chunk> inherited,
                                         string defaultModel)
        {
            var mergerMappings = GetMergerMappings(codeTree, defaultModel);
            IChunkMerger merger;

            // We merge chunks into the codeTree in two passes. In the first pass, we traverse the CodeTree visiting
            // a mapped IChunkMerger for types that are registered.
            foreach (var chunk in codeTree.Chunks)
            {
                if (mergerMappings.TryGetValue(chunk.GetType(), out merger))
                {
                    merger.VisitChunk(chunk);
                }
            }

            // In the second phase we invoke IChunkMerger.Merge for each chunk that has a mapped merger.
            // During this phase, the merger can either add to the CodeTree or ignore the chunk based on the merging
            // rules.
            foreach (var chunk in inherited)
            {
                if (mergerMappings.TryGetValue(chunk.GetType(), out merger))
                {
                    // TODO: When mapping chunks, we should remove mapping information since it would be incorrect
                    // to generate it in the page that inherits it. Tracked by #945
                    merger.Merge(codeTree, chunk);
                }
            }
        }

        private static Dictionary<Type, IChunkMerger> GetMergerMappings(CodeTree codeTree, string defaultModel)
        {
            var modelType = ChunkHelper.GetModelTypeName(codeTree, defaultModel);
            return new Dictionary<Type, IChunkMerger>
            {
                { typeof(UsingChunk), new UsingChunkMerger() },
                { typeof(InjectChunk), new InjectChunkMerger(modelType) },
                { typeof(SetBaseTypeChunk), new SetBaseTypeChunkMerger(modelType) }
            };
        }

        // TODO: This needs to be cached (#1016)
        private static CodeTree ParseViewFile(RazorTemplateEngine engine,
                                              IFileInfo fileInfo)
        {
            using (var stream = fileInfo.CreateReadStream())
            {
                using (var streamReader = new StreamReader(stream))
                {
                    var parseResults = engine.ParseTemplate(streamReader, fileInfo.PhysicalPath);
                    var className = ParserHelpers.SanitizeClassName(fileInfo.Name);
                    var language = engine.Host.CodeLanguage;
                    var codeGenerator = language.CreateCodeGenerator(className,
                                                                     engine.Host.DefaultNamespace,
                                                                     fileInfo.PhysicalPath,
                                                                     engine.Host);
                    codeGenerator.Visit(parseResults);

                    return codeGenerator.Context.CodeTreeBuilder.CodeTree;
                }
            }
        }
    }
}