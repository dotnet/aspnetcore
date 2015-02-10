// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator
{
    /// <summary>
    /// A <see cref="BlockCodeGenerator"/> that is responsible for generating valid <see cref="TagHelperChunk"/>s.
    /// </summary>
    public class TagHelperCodeGenerator : BlockCodeGenerator
    {
        private IEnumerable<TagHelperDescriptor> _tagHelperDescriptors;

        /// <summary>
        /// Instantiates a new <see cref="TagHelperCodeGenerator"/>.
        /// </summary>
        /// <param name="tagHelperDescriptors">
        /// <see cref="TagHelperDescriptor"/>s associated with the current HTML tag.
        /// </param>
        public TagHelperCodeGenerator(IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
        {
            _tagHelperDescriptors = tagHelperDescriptors;
        }

        /// <summary>
        /// Starts the generation of a <see cref="TagHelperChunk"/>.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Block"/> responsible for this <see cref="TagHelperCodeGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about
        /// the current code generation process.</param>
        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            var tagHelperBlock = target as TagHelperBlock;

            if (tagHelperBlock == null)
            {
                throw new ArgumentException(
                    RazorResources.TagHelpers_TagHelperCodeGeneartorMustBeAssociatedWithATagHelperBlock);
            }

            var attributes = new Dictionary<string, Chunk>(StringComparer.OrdinalIgnoreCase);

            // We need to create a code generator to create chunks for each of the attributes.
            var codeGenerator = context.Host.CreateCodeGenerator(
                context.ClassName,
                context.RootNamespace,
                context.SourceFile);

            foreach (var attribute in tagHelperBlock.Attributes)
            {
                // Populates the code tree with chunks associated with attributes
                attribute.Value.Accept(codeGenerator);

                var chunks = codeGenerator.Context.CodeTreeBuilder.CodeTree.Chunks;
                var first = chunks.FirstOrDefault();

                attributes[attribute.Key] = new ChunkBlock
                {
                    Association = first?.Association,
                    Children = chunks,
                    Start = first == null ? SourceLocation.Zero : first.Start
                };

                // Reset the code tree builder so we can build a new one for the next attribute
                codeGenerator.Context.CodeTreeBuilder = new CodeTreeBuilder();
            }

            context.CodeTreeBuilder.StartChunkBlock(
                new TagHelperChunk(
                    tagHelperBlock.TagName,
                    tagHelperBlock.SelfClosing,
                    attributes,
                    _tagHelperDescriptors),
                target,
                topLevel: false);
        }

        /// <summary>
        /// Ends the generation of a <see cref="TagHelperChunk"/> capturing all previously visited children
        /// since the <see cref="GenerateStartBlockCode"/> method was called.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Block"/> responsible for this <see cref="TagHelperCodeGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about
        /// the current code generation process.</param>
        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.EndChunkBlock();
        }
    }
}