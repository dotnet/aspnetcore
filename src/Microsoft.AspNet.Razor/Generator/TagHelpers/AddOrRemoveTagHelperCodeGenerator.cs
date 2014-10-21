// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    /// <summary>
    /// A <see cref="SpanCodeGenerator"/> responsible for generating <see cref="Compiler.AddTagHelperChunk"/>s and
    /// <see cref="Compiler.RemoveTagHelperChunk"/>s.
    /// </summary>
    public class AddOrRemoveTagHelperCodeGenerator : SpanCodeGenerator
    {
        /// <summary>
        /// Instantiates a new <see cref="AddOrRemoveTagHelperCodeGenerator"/>.
        /// </summary>
        /// <param name="lookupText">
        /// Text used to look up <see cref="TagHelpers.TagHelperDescriptor"/>s that should be added or removed.
        /// </param>
        public AddOrRemoveTagHelperCodeGenerator(bool removeTagHelperDescriptors, string lookupText)
        {
            RemoveTagHelperDescriptors = removeTagHelperDescriptors;
            LookupText = lookupText;
        }

        /// <summary>
        /// Gets the text used to look up <see cref="TagHelpers.TagHelperDescriptor"/>s that should be added to or 
        /// removed from the Razor page.
        /// </summary>
        public string LookupText { get; }

        /// <summary>
        /// Whether we want to remove <see cref="TagHelpers.TagHelperDescriptor"/>s from the Razor page.
        /// </summary>
        /// <remarks>If <c>true</c> <see cref="GenerateCode"/> generates <see cref="Compiler.AddTagHelperChunk"/>s,
        /// <see cref="Compiler.RemoveTagHelperChunk"/>s otherwise.</remarks>
        public bool RemoveTagHelperDescriptors { get; }

        /// <summary>
        /// Generates <see cref="Compiler.AddTagHelperChunk"/>s if <see cref="RemoveTagHelperDescriptors"/> is 
        /// <c>true</c>, otherwise <see cref="Compiler.RemoveTagHelperChunk"/>s are generated.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Span"/> responsible for this <see cref="AddOrRemoveTagHelperCodeGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about 
        /// the current code generation process.</param>
        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            if (RemoveTagHelperDescriptors)
            {
                context.CodeTreeBuilder.AddRemoveTagHelperChunk(LookupText, target);
            }
            else
            {
                context.CodeTreeBuilder.AddAddTagHelperChunk(LookupText, target);
            }
        }
    }
}