// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    /// <summary>
    /// A <see cref="SpanChunkGenerator"/> responsible for generating
    /// <see cref="TagHelperPrefixDirectiveChunk"/>s.
    /// </summary>
    public class TagHelperPrefixDirectiveChunkGenerator : SpanChunkGenerator
    {
        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance of <see cref="TagHelperPrefixDirectiveChunkGenerator"/>.
        /// </summary>
        /// <param name="prefix">
        /// Text used as a required prefix when matching HTML.
        /// </param>
        public TagHelperPrefixDirectiveChunkGenerator(string prefix)
        {
            _prefix = prefix;
        }

        /// <summary>
        /// Generates <see cref="TagHelperPrefixDirectiveChunk"/>s.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Span"/> responsible for this <see cref="TagHelperPrefixDirectiveChunkGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="ChunkGeneratorContext"/> instance that contains information about
        /// the current chunk generation process.</param>
        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            context.ChunkTreeBuilder.AddTagHelperPrefixDirectiveChunk(_prefix, target);
        }
    }
}