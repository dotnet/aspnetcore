// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    /// <summary>
    /// A <see cref="SpanCodeGenerator"/> responsible for generating
    /// <see cref="Compiler.TagHelperPrefixDirectiveChunk"/>s.
    /// </summary>
    public class TagHelperPrefixDirectiveCodeGenerator : SpanCodeGenerator
    {
        /// <summary>
        /// Instantiates a new <see cref="TagHelperPrefixDirectiveCodeGenerator"/>.
        /// </summary>
        /// <param name="prefix">
        /// Text used as a required prefix when matching HTML.
        /// </param>
        public TagHelperPrefixDirectiveCodeGenerator(string prefix)
        {
            Prefix = prefix;
        }

        /// <summary>
        /// Text used as a required prefix when matching HTML.
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// Generates <see cref="Compiler.TagHelperPrefixDirectiveChunk"/>s.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Span"/> responsible for this <see cref="TagHelperPrefixDirectiveCodeGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about
        /// the current code generation process.</param>
        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.AddTagHelperPrefixDirectiveChunk(Prefix, target);
        }
    }
}