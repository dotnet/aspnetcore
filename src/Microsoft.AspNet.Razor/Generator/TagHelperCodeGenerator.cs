// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    /// <summary>
    /// A <see cref="BlockCodeGenerator"/> that is responsible for generating valid <see cref="TagHelperChunk"/>s.
    /// </summary>
    public class TagHelperCodeGenerator : BlockCodeGenerator
    {
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
        }

        /// <summary>
        /// Ends the generation of a <see cref="TagHelperChunk"/> capturing all previously visited children
        /// since <see cref="GenerateStartBlockCode"/> method was called.
        /// </summary>
        /// <param name="target">
        /// The <see cref="Block"/> responsible for this <see cref="TagHelperCodeGenerator"/>.
        /// </param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about 
        /// the current code generation process.</param>
        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
        }
    }
}