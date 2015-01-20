// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileProviders;
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
        private readonly IFileProvider _fileProvider;
        private readonly IReadOnlyList<Chunk> _defaultInheritedChunks;

        /// <summary>
        /// Initializes a new instance of <see cref="ChunkInheritanceUtility"/>.
        /// </summary>
        /// <param name="razorHost">The <see cref="MvcRazorHost"/> used to parse _ViewStart pages.</param>
        /// <param name="fileProvider">The fileProvider that represents the application.</param>
        /// <param name="defaultInheritedChunks">Sequence of <see cref="Chunk"/>s inherited by default.</param>
        public ChunkInheritanceUtility([NotNull] MvcRazorHost razorHost,
                                       [NotNull] IFileProvider fileProvider,
                                       [NotNull] IReadOnlyList<Chunk> defaultInheritedChunks)
        {
            _razorHost = razorHost;
            _fileProvider = fileProvider;
            _defaultInheritedChunks = defaultInheritedChunks;
            _parsedCodeTrees = new Dictionary<string, CodeTree>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets an ordered <see cref="IReadOnlyList{T}"/> of parsed <see cref="CodeTree"/> for each _ViewStart that
        /// is applicable to the page located at <paramref name="pagePath"/>. The list is ordered so that the
        /// <see cref="CodeTree"/> for the _ViewStart closest to the <paramref name="pagePath"/> in the fileProvider
        /// appears first.
        /// </summary>
        /// <param name="pagePath">The path of the page to locate inherited chunks for.</param>
        /// <returns>A <see cref="IReadOnlyList{CodeTree}"/> of parsed _ViewStart <see cref="CodeTree"/>s.</returns>
        public IReadOnlyList<CodeTree> GetInheritedCodeTrees([NotNull] string pagePath)
        {
            var inheritedCodeTrees = new List<CodeTree>();

            var templateEngine = new RazorTemplateEngine(_razorHost);
            foreach (var viewStartPath in ViewStartUtility.GetViewStartLocations(pagePath))
            {
                CodeTree codeTree;

                if (_parsedCodeTrees.TryGetValue(viewStartPath, out codeTree))
                {
                    inheritedCodeTrees.Add(codeTree);
                }
                else
                {
                    var fileInfo = _fileProvider.GetFileInfo(viewStartPath);
                    if (fileInfo.Exists)
                    {
                        // viewStartPath contains the app-relative path of the ViewStart.
                        // Since the parsing of a _ViewStart would cause parent _ViewStarts to be parsed
                        // we need to ensure the paths are app-relative to allow the GetViewStartLocations
                        // for the current _ViewStart to succeed.
                        codeTree = ParseViewFile(templateEngine, fileInfo, viewStartPath);
                        _parsedCodeTrees.Add(viewStartPath, codeTree);

                        inheritedCodeTrees.Add(codeTree);
                    }
                }
            }

            return inheritedCodeTrees;
        }

        /// <summary>
        /// Merges <see cref="Chunk"/> inherited by default and <see cref="CodeTree"/> instances produced by parsing
        /// _ViewStart files into the specified <paramref name="codeTree"/>.
        /// </summary>
        /// <param name="codeTree">The <see cref="CodeTree"/> to merge in to.</param>
        /// <param name="inheritedCodeTrees"><see cref="IReadOnlyList{CodeTree}"/> inherited from _ViewStart
        /// files.</param>
        /// <param name="defaultModel">The list of chunks to merge.</param>
        public void MergeInheritedCodeTrees([NotNull] CodeTree codeTree,
                                            [NotNull] IReadOnlyList<CodeTree> inheritedCodeTrees,
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
            // Read the chunks outside in - that is chunks from the _ViewStart closest to the page get merged in first
            // and the furthest one last. This allows the merger to ignore a directive like @model that was previously
            // seen.
            var chunksToMerge = inheritedCodeTrees.SelectMany(tree => tree.Chunks)
                                                  .Concat(_defaultInheritedChunks);
            foreach (var chunk in chunksToMerge)
            {
                if (mergerMappings.TryGetValue(chunk.GetType(), out merger))
                {
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

        private static CodeTree ParseViewFile(RazorTemplateEngine engine,
                                              IFileInfo fileInfo,
                                              string viewStartPath)
        {
            using (var stream = fileInfo.CreateReadStream())
            {
                using (var streamReader = new StreamReader(stream))
                {
                    var parseResults = engine.ParseTemplate(streamReader, viewStartPath);
                    var className = ParserHelpers.SanitizeClassName(fileInfo.Name);
                    var language = engine.Host.CodeLanguage;
                    var codeGenerator = language.CreateCodeGenerator(className,
                                                                     engine.Host.DefaultNamespace,
                                                                     viewStartPath,
                                                                     engine.Host);
                    codeGenerator.Visit(parseResults);

                    return codeGenerator.Context.CodeTreeBuilder.CodeTree;
                }
            }
        }
    }
}