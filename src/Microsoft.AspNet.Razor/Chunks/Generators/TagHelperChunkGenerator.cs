// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    /// <summary>
    /// A <see cref="ParentChunkGenerator"/> that is responsible for generating valid <see cref="TagHelperChunk"/>s.
    /// </summary>
    public class TagHelperChunkGenerator : ParentChunkGenerator
    {
        private IEnumerable<TagHelperDescriptor> _tagHelperDescriptors;

        /// <summary>
        /// Instantiates a new <see cref="TagHelperChunkGenerator"/>.
        /// </summary>
        /// <param name="tagHelperDescriptors">
        /// <see cref="TagHelperDescriptor"/>s associated with the current HTML tag.
        /// </param>
        public TagHelperChunkGenerator(IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
        {
            _tagHelperDescriptors = tagHelperDescriptors;
        }

        /// <summary>
        /// Starts the generation of a <see cref="TagHelperChunk"/>.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Block"/> responsible for this <see cref="TagHelperChunkGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="ChunkGeneratorContext"/> instance that contains information about
        /// the current chunk generation process.</param>
        public override void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
            var tagHelperBlock = target as TagHelperBlock;

            Debug.Assert(
                tagHelperBlock != null,
                $"A {nameof(TagHelperChunkGenerator)} must only be used with {nameof(TagHelperBlock)}s.");

            var attributes = new List<KeyValuePair<string, Chunk>>();

            // We need to create a chunk generator to create chunks for each of the attributes.
            var chunkGenerator = context.Host.CreateChunkGenerator(
                context.ClassName,
                context.RootNamespace,
                context.SourceFile);

            foreach (var attribute in tagHelperBlock.Attributes)
            {
                ParentChunk attributeChunkValue = null;

                if (attribute.Value != null)
                {
                    // Populates the chunk tree with chunks associated with attributes
                    attribute.Value.Accept(chunkGenerator);

                    var chunks = chunkGenerator.Context.ChunkTreeBuilder.Root.Children;
                    var first = chunks.FirstOrDefault();

                    attributeChunkValue = new ParentChunk
                    {
                        Association = first?.Association,
                        Children = chunks,
                        Start = first == null ? SourceLocation.Zero : first.Start
                    };
                }

                attributes.Add(new KeyValuePair<string, Chunk>(attribute.Key, attributeChunkValue));

                // Reset the chunk tree builder so we can build a new one for the next attribute
                chunkGenerator.Context.ChunkTreeBuilder = new ChunkTreeBuilder();
            }

            var unprefixedTagName = tagHelperBlock.TagName.Substring(_tagHelperDescriptors.First().Prefix.Length);

            context.ChunkTreeBuilder.StartParentChunk(
                new TagHelperChunk(
                    unprefixedTagName,
                    tagHelperBlock.TagMode,
                    attributes,
                    _tagHelperDescriptors),
                target,
                topLevel: false);
        }

        /// <summary>
        /// Ends the generation of a <see cref="TagHelperChunk"/> capturing all previously visited children
        /// since the <see cref="GenerateStartParentChunk"/> method was called.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Block"/> responsible for this <see cref="TagHelperChunkGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="ChunkGeneratorContext"/> instance that contains information about
        /// the current chunk generation process.</param>
        public override void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
            context.ChunkTreeBuilder.EndParentChunk();
        }
    }
}