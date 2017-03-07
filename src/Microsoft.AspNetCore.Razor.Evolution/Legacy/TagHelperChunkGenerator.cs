// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class TagHelperChunkGenerator : ParentChunkGenerator
    {
        public override void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
            //var tagHelperBlock = target as TagHelperBlock;

            //Debug.Assert(
            //    tagHelperBlock != null,
            //    $"A {nameof(TagHelperChunkGenerator)} must only be used with {nameof(TagHelperBlock)}s.");

            //var attributes = new List<TagHelperAttributeTracker>();

            //// We need to create a chunk generator to create chunks for each of the attributes.
            //var chunkGenerator = context.Host.CreateChunkGenerator(
            //    context.ClassName,
            //    context.RootNamespace,
            //    context.SourceFile);

            //foreach (var attribute in tagHelperBlock.Attributes)
            //{
            //    ParentChunk attributeChunkValue = null;

            //    if (attribute.Value != null)
            //    {
            //        // Populates the chunk tree with chunks associated with attributes
            //        attribute.Value.Accept(chunkGenerator);

            //        var chunks = chunkGenerator.Context.ChunkTreeBuilder.Root.Children;
            //        var first = chunks.FirstOrDefault();

            //        attributeChunkValue = new ParentChunk
            //        {
            //            Association = first?.Association,
            //            Children = chunks,
            //            Start = first == null ? SourceLocation.Zero : first.Start
            //        };
            //    }

            //    var attributeChunk = new TagHelperAttributeTracker(
            //        attribute.Name,
            //        attributeChunkValue,
            //        attribute.ValueStyle);

            //    attributes.Add(attributeChunk);

            //    // Reset the chunk tree builder so we can build a new one for the next attribute
            //    chunkGenerator.Context.ChunkTreeBuilder = new ChunkTreeBuilder();
            //}

            //var unprefixedTagName = tagHelperBlock.TagName.Substring(_tagHelperDescriptors.First().Prefix.Length);

            //context.ChunkTreeBuilder.StartParentChunk(
            //    new TagHelperChunk(
            //        unprefixedTagName,
            //        tagHelperBlock.TagMode,
            //        attributes,
            //        _tagHelperDescriptors),
            //    target,
            //    topLevel: false);
        }

        public override void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.EndParentChunk();
        }

        public override void Accept(ParserVisitor visitor, Block block)
        {
            visitor.VisitTagHelperBlock(this, block);
        }
    }
}