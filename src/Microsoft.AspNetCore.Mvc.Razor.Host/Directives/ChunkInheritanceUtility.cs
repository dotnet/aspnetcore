// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Directives
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
            MvcRazorHost razorHost,
            IChunkTreeCache chunkTreeCache,
            IReadOnlyList<Chunk> defaultInheritedChunks)
        {
            if (razorHost == null)
            {
                throw new ArgumentNullException(nameof(razorHost));
            }

            if (chunkTreeCache == null)
            {
                throw new ArgumentNullException(nameof(chunkTreeCache));
            }

            if (defaultInheritedChunks == null)
            {
                throw new ArgumentNullException(nameof(defaultInheritedChunks));
            }

            _razorHost = razorHost;
            _defaultInheritedChunks = defaultInheritedChunks;
            _chunkTreeCache = chunkTreeCache;
        }

        /// <summary>
        /// Gets an ordered <see cref="IReadOnlyList{ChunkTreeResult}"/> of parsed <see cref="ChunkTree"/>s and
        /// file paths for each <c>_ViewImports</c> that is applicable to the page located at
        /// <paramref name="pagePath"/>. The list is ordered so that the <see cref="ChunkTreeResult"/>'s
        /// <see cref="ChunkTreeResult.ChunkTree"/> for the <c>_ViewImports</c> closest to the
        /// <paramref name="pagePath"/> in the file system appears first.
        /// </summary>
        /// <param name="pagePath">The path of the page to locate inherited chunks for.</param>
        /// <returns>A <see cref="IReadOnlyList{ChunkTreeResult}"/> of parsed <c>_ViewImports</c>
        /// <see cref="ChunkTree"/>s and their file paths.</returns>
        /// <remarks>
        /// The resulting <see cref="IReadOnlyList{ChunkTreeResult}"/> is ordered so that the result
        /// for a _ViewImport closest to the application root appears first and the _ViewImport
        /// closest to the page appears last i.e.
        /// [ /_ViewImport, /Views/_ViewImport, /Views/Home/_ViewImport ]
        /// </remarks>
        public virtual IReadOnlyList<ChunkTreeResult> GetInheritedChunkTreeResults(string pagePath)
        {
            if (pagePath == null)
            {
                throw new ArgumentNullException(nameof(pagePath));
            }

            var inheritedChunkTreeResults = new List<ChunkTreeResult>();
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
                    var result = new ChunkTreeResult(chunkTree, viewImportsPath);
                    inheritedChunkTreeResults.Insert(0, result);
                }
            }

            return inheritedChunkTreeResults;
        }

        /// <summary>
        /// Merges <see cref="Chunk"/> inherited by default and <see cref="ChunkTree"/> instances produced by parsing
        /// <c>_ViewImports</c> files into the specified <paramref name="chunkTree"/>.
        /// </summary>
        /// <param name="chunkTree">The <see cref="ChunkTree"/> to merge in to.</param>
        /// <param name="inheritedChunkTrees"><see cref="IReadOnlyList{ChunkTree}"/> inherited from <c>_ViewImports</c>
        /// files.</param>
        /// <param name="defaultModel">The default model <see cref="Type"/> name.</param>
        public void MergeInheritedChunkTrees(
            ChunkTree chunkTree,
            IReadOnlyList<ChunkTree> inheritedChunkTrees,
            string defaultModel)
        {
            if (chunkTree == null)
            {
                throw new ArgumentNullException(nameof(chunkTree));
            }

            if (inheritedChunkTrees == null)
            {
                throw new ArgumentNullException(nameof(inheritedChunkTrees));
            }

            var chunkMergers = GetChunkMergers(chunkTree, defaultModel);
            // We merge chunks into the ChunkTree in two passes. In the first pass, we traverse the ChunkTree visiting
            // a mapped IChunkMerger for types that are registered.
            foreach (var chunk in chunkTree.Children)
            {
                foreach (var merger in chunkMergers)
                {
                    merger.VisitChunk(chunk);
                }
            }

            var inheritedChunks = _defaultInheritedChunks.Concat(
                inheritedChunkTrees.SelectMany(tree => tree.Children)).ToArray();

            foreach (var merger in chunkMergers)
            {
                merger.MergeInheritedChunks(chunkTree, inheritedChunks);
            }
        }

        private static IChunkMerger[] GetChunkMergers(ChunkTree chunkTree, string defaultModel)
        {
            var modelType = ChunkHelper.GetModelTypeName(chunkTree, defaultModel);
            return new IChunkMerger[]
            {
                new UsingChunkMerger(),
                new InjectChunkMerger(modelType),
                new SetBaseTypeChunkMerger(modelType)
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
                    var chunkTree = chunkGenerator.Context.ChunkTreeBuilder.Root;
                    foreach (var chunk in chunkTree.Children)
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