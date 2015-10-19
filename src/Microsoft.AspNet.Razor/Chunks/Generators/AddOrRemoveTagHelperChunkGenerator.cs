// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    /// <summary>
    /// A <see cref="SpanChunkGenerator"/> responsible for generating <see cref="AddTagHelperChunk"/>s and
    /// <see cref="RemoveTagHelperChunk"/>s.
    /// </summary>
    public class AddOrRemoveTagHelperChunkGenerator : SpanChunkGenerator
    {
        /// <summary>
        /// Instantiates a new <see cref="AddOrRemoveTagHelperChunkGenerator"/>.
        /// </summary>
        /// <param name="lookupText">
        /// Text used to look up <see cref="Compilation.TagHelpers.TagHelperDescriptor"/>s that should be added or removed.
        /// </param>
        public AddOrRemoveTagHelperChunkGenerator(bool removeTagHelperDescriptors, string lookupText)
        {
            RemoveTagHelperDescriptors = removeTagHelperDescriptors;
            LookupText = lookupText;
        }

        /// <summary>
        /// Gets the text used to look up <see cref="Compilation.TagHelpers.TagHelperDescriptor"/>s that should be added to or
        /// removed from the Razor page.
        /// </summary>
        public string LookupText { get; }

        /// <summary>
        /// Whether we want to remove <see cref="Compilation.TagHelpers.TagHelperDescriptor"/>s from the Razor page.
        /// </summary>
        /// <remarks>If <c>true</c> <see cref="GenerateChunk"/> generates <see cref="AddTagHelperChunk"/>s,
        /// <see cref="RemoveTagHelperChunk"/>s otherwise.</remarks>
        public bool RemoveTagHelperDescriptors { get; }

        /// <summary>
        /// Generates <see cref="AddTagHelperChunk"/>s if <see cref="RemoveTagHelperDescriptors"/> is
        /// <c>true</c>, otherwise <see cref="RemoveTagHelperChunk"/>s are generated.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Span"/> responsible for this <see cref="AddOrRemoveTagHelperChunkGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="ChunkGeneratorContext"/> instance that contains information about
        /// the current chunk generation process.</param>
        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            if (RemoveTagHelperDescriptors)
            {
                context.ChunkTreeBuilder.AddRemoveTagHelperChunk(LookupText, target);
            }
            else
            {
                context.ChunkTreeBuilder.AddAddTagHelperChunk(LookupText, target);
            }
        }
    }
}