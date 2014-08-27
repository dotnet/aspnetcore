// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A utility type for supporting inheritance of chunks into a page from _ViewStart pages that apply to it.
    /// </summary>
    public class ChunkInheritanceUtility
    {
        private readonly IReadOnlyList<Chunk> _defaultInheritedChunks;

        /// <summary>
        /// Instantiates a new instance of <see cref="ChunkInheritanceUtility"/>.
        /// </summary>
        /// <param name="codeTree">The <see cref="CodeTree"/> instance to add <see cref="Chunk"/>s to.</param>
        /// <param name="defaultInheritedChunks">The list of <see cref="Chunk"/>s inherited by default.</param>
        /// <param name="defaultModel">The model type used in the event no model is specified via the
        /// <c>@model</c> keyword.</param>
        public ChunkInheritanceUtility([NotNull] CodeTree codeTree,
                                       [NotNull] IReadOnlyList<Chunk> defaultInheritedChunks,
                                       [NotNull] string defaultModel)
        {
            CodeTree = codeTree;
            _defaultInheritedChunks = defaultInheritedChunks;
            ChunkMergers = GetMergerMappings(codeTree, defaultModel);
        }

        /// <summary>
        /// Gets the CodeTree to add inherited <see cref="Chunk"/> instances to.
        /// </summary>
        public CodeTree CodeTree { get; private set; }

        /// <summary>
        /// Gets a dictionary mapping <see cref="Chunk"/> type to the <see cref="IChunkMerger"/> used to merge
        /// chunks of that type.
        /// </summary>
        public IDictionary<Type, IChunkMerger> ChunkMergers { get; private set; }

        /// <summary>
        /// Gets the list of chunks that are to be inherited by a specified page.
        /// Chunks are inherited from _ViewStarts that are applicable to the page.
        /// </summary>
        /// <param name="razorHost">The <see cref="MvcRazorHost"/> used to parse _ViewStart pages.</param>
        /// <param name="fileSystem">The filesystem that represents the application.</param>
        /// <param name="pagePath">The path of the page to locate inherited chunks for.</param>
        /// <returns>A list of chunks that are applicable to the given page.</returns>
        public List<Chunk> GetInheritedChunks([NotNull] MvcRazorHost razorHost,
                                              [NotNull] IFileSystem fileSystem,
                                              [NotNull] string pagePath)
        {
            var inheritedChunks = new List<Chunk>();

            var templateEngine = new RazorTemplateEngine(razorHost);
            foreach (var viewStart in ViewStartUtility.GetViewStartLocations(fileSystem, pagePath))
            {
                IFileInfo fileInfo;
                if (fileSystem.TryGetFileInfo(viewStart, out fileInfo))
                {
                    var parsedTree = ParseViewFile(templateEngine, fileInfo);
                    var chunksToAdd = parsedTree.Chunks
                                                .Where(chunk => ChunkMergers.ContainsKey(chunk.GetType()));
                    inheritedChunks.AddRange(chunksToAdd);
                }
            }

            inheritedChunks.AddRange(_defaultInheritedChunks);

            return inheritedChunks;
        }

        /// <summary>
        /// Merges a list of chunks into the <see cref="CodeTree"/> instance.
        /// </summary>
        /// <param name="inherited">The list of chunks to merge.</param>
        public void MergeInheritedChunks(List<Chunk> inherited)
        {
            var current = CodeTree.Chunks;

            // We merge chunks into the codeTree in two passes. In the first pass, we traverse the CodeTree visiting
            // a mapped IChunkMerger for types that are registered.
            foreach (var chunk in current)
            {
                if (ChunkMergers.TryGetValue(chunk.GetType(), out var merger))
                {
                    merger.VisitChunk(chunk);
                }
            }

            // In the second phase we invoke IChunkMerger.Merge for each chunk that has a mapped merger.
            // During this phase, the merger can either add to the CodeTree or ignore the chunk based on the merging
            // rules.
            foreach (var chunk in inherited)
            {
                if (ChunkMergers.TryGetValue(chunk.GetType(), out var merger))
                {
                    // TODO: When mapping chunks, we should remove mapping information since it would be incorrect
                    // to generate it in the page that inherits it. Tracked by #945
                    merger.Merge(CodeTree, chunk);
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
        private CodeTree ParseViewFile(RazorTemplateEngine engine,
                                       IFileInfo fileInfo)
        {
            using (var stream = fileInfo.CreateReadStream())
            {
                using (var streamReader = new StreamReader(stream))
                {
                    var parseResults = engine.ParseTemplate(streamReader);
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