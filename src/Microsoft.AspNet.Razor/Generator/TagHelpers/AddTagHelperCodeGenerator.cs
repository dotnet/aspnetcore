// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    /// <summary>
    /// A <see cref="SpanCodeGenerator"/> responsible for generating <see cref="Compiler.AddTagHelperChunk"/>s.
    /// </summary>
    public class AddTagHelperCodeGenerator : SpanCodeGenerator
    {
        /// <summary>
        /// Instantiates a new <see cref="AddTagHelperCodeGenerator"/>.
        /// </summary>
        /// <param name="lookupText">
        /// Text used to look up <see cref="TagHelpers.TagHelperDescriptor"/>s.
        /// </param>
        public AddTagHelperCodeGenerator(string lookupText)
        {
            LookupText = lookupText;
        }

        /// <summary>
        /// Text used to look up <see cref="TagHelpers.TagHelperDescriptor"/>s.
        /// </summary>
        public string LookupText { get; private set; }

        /// <summary>
        /// Generates a <see cref="Compiler.AddTagHelperChunk"/>.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Span"/> responsible for this <see cref="AddTagHelperCodeGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about 
        /// the current code generation process.</param>
        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.AddAddTagHelperChunk(LookupText, target);
        }
    }
}