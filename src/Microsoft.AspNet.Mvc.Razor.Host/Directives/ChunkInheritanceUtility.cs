// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A utility type for supporting inheritance of directives into a page from applicable <c>_ViewImports</c> pages.
    /// </summary>
    public class ChunkInheritanceUtility
    {
        private readonly MvcRazorHost _razorHost;
        private readonly IReadOnlyList<Chunk> _defaultInheritedChunks;
        private readonly IChunkTreeCache _chunkTreeCache;

        /// <summary>
        /// Initializes a new instance of <see cref="ChunkInheritanceUtility"/>.
        /// </summary>
        /// <param name="razorHost">The <see cref="MvcRazorHost"/> used to parse <c>_ViewImports</c> pages.</param>
        /// <param name="chunkTreeCache"><see cref="IChunkTreeCache"/> that caches <see cref="ChunkTree"/> instances.
        /// </param>
        /// <param name="defaultInheritedChunks">Sequence of <see cref="Chunk"/>s inherited by default.</param>
        public ChunkInheritanceUtility(
            [NotNull] MvcRazorHost razorHost,
            [NotNull] IChunkTreeCache chunkTreeCache,
            [NotNull] IReadOnlyList<Chunk> defaultInheritedChunks)
        {
            _razorHost = razorHost;
            _defaultInheritedChunks = defaultInheritedChunks;
            _chunkTreeCache = chunkTreeCache;
        }

        /// <summary>
        /// Gets an ordered <see cref="IReadOnlyList{T}"/> of parsed <see cref="ChunkTree"/> for each
        /// <c>_ViewImports</c> that is applicable to the page located at <paramref name="pagePath"/>. The list is
        /// ordered so that the <see cref="ChunkTree"/> for the <c>_ViewImports</c> closest to the
        /// <paramref name="pagePath"/> in the file system appears first.
        /// </summary>
        /// <param name="pagePath">The path of the page to locate inherited chunks for.</param>
        /// <returns>A <see cref="IReadOnlyList{ChunkTree}"/> of parsed <c>_ViewImports</c>
        /// <see cref="ChunkTree"/>s.</returns>
        public virtual IReadOnlyList<ChunkTree> GetInheritedChunkTrees([NotNull] string pagePath)
        {
            var inheritedChunkTrees = new List<ChunkTree>();
            var templateEngine = new RazorTemplateEngine(_razorHost);
            foreach (var viewImportsPath in ViewHierarchyUtility.GetViewImportsLocations(pagePath))
            {
                // viewImportsPath contains the app-relative path of the _ViewImports.
                // Since the parsing of a _ViewImports would cause parent _ViewImports to be parsed
                // we need to ensure the paths are app-relative to allow the GetGlobalFileLocations
                // for the current _ViewImports to succeed.
                var chunkTree = _chunkTreeCache.GetOrAdd(
                    viewImportsPath,
                    fileInfo => ParseViewFile(
                        templateEngine,
                        fileInfo,
                        viewImportsPath));

                if (chunkTree != null)
                {
                    inheritedChunkTrees.Add(chunkTree);
                }
            }

            return inheritedChunkTrees;
        }

        /// <summary>
        /// Merges <see cref="Chunk"/> inherited by default and <see cref="ChunkTree"/> instances produced by parsing
        /// <c>_ViewImports</c> files into the specified <paramref name="chunkTree"/>.
        /// </summary>
        /// <param name="chunkTree">The <see cref="ChunkTree"/> to merge in to.</param>
        /// <param name="inheritedChunkTrees"><see cref="IReadOnlyList{ChunkTree}"/> inherited from <c>_ViewImports</c>
        /// files.</param>
        /// <param name="defaultModel">The list of chunks to merge.</param>
        public void MergeInheritedChunkTrees(
            [NotNull] ChunkTree chunkTree,
            [NotNull] IReadOnlyList<ChunkTree> inheritedChunkTrees,
            string defaultModel)
        {
            var mergerMappings = GetMergerMappings(chunkTree, defaultModel);
            IChunkMerger merger;

            // We merge chunks into the ChunkTree in two passes. In the first pass, we traverse the ChunkTree visiting
            // a mapped IChunkMerger for types that are registered.
            foreach (var chunk in chunkTree.Chunks)
            {
                if (mergerMappings.TryGetValue(chunk.GetType(), out merger))
                {
                    merger.VisitChunk(chunk);
                }
            }

            // In the second phase we invoke IChunkMerger.Merge for each chunk that has a mapped merger.
            // During this phase, the merger can either add to the ChunkTree or ignore the chunk based on the merging
            // rules.
            // Read the chunks outside in - that is chunks from the _ViewImports closest to the page get merged in first
            // and the furthest one last. This allows the merger to ignore a directive like @model that was previously
            // seen.
            var chunksToMerge = inheritedChunkTrees.SelectMany(tree => tree.Chunks)
                                                  .Concat(_defaultInheritedChunks);
            foreach (var chunk in chunksToMerge)
            {
                if (mergerMappings.TryGetValue(chunk.GetType(), out merger))
                {
                    merger.Merge(chunkTree, chunk);
                }
            }
        }

        private static Dictionary<Type, IChunkMerger> GetMergerMappings(ChunkTree chunkTree, string defaultModel)
        {
            var modelType = ChunkHelper.GetModelTypeName(chunkTree, defaultModel);
            return new Dictionary<Type, IChunkMerger>
            {
                { typeof(UsingChunk), new UsingChunkMerger() },
                { typeof(InjectChunk), new InjectChunkMerger(modelType) },
                { typeof(SetBaseTypeChunk), new SetBaseTypeChunkMerger(modelType) }
            };
        }

        private static ChunkTree ParseViewFile(
            RazorTemplateEngine engine,
            IFileInfo fileInfo,
            string viewImportsPath)
        {
            using (var stream = fileInfo.CreateReadStream())
            {
                using (var streamReader = new StreamReader(stream))
                {
                    var parseResults = engine.ParseTemplate(streamReader, viewImportsPath);
                    var className = ParserHelpers.SanitizeClassName(fileInfo.Name);
                    var language = engine.Host.CodeLanguage;
                    var chunkGenerator = language.CreateChunkGenerator(
                        className,
                        engine.Host.DefaultNamespace,
                        viewImportsPath,
                        engine.Host);
                    chunkGenerator.Visit(parseResults);

                    // Rewrite the location of inherited chunks so they point to the global import file.
                    var chunkTree = chunkGenerator.Context.ChunkTreeBuilder.ChunkTree;
                    foreach (var chunk in chunkTree.Chunks)
                    {
                        chunk.Start = new SourceLocation(
                            viewImportsPath,
                            chunk.Start.AbsoluteIndex,
                            chunk.Start.LineIndex,
                            chunk.Start.CharacterIndex);
                    }

                    return chunkTree;
                }
            }
        }
    }
}