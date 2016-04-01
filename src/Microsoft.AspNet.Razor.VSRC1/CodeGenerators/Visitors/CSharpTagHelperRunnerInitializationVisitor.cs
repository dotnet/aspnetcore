// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    /// <summary>
    /// The <see cref="CodeVisitor{T}"/> that generates the code to initialize the TagHelperRunner.
    /// </summary>
    public class CSharpTagHelperRunnerInitializationVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private readonly GeneratedTagHelperContext _tagHelperContext;
        private bool _foundTagHelpers;

        /// <summary>
        /// Creates a new instance of <see cref="CSharpTagHelperRunnerInitializationVisitor"/>.
        /// </summary>
        /// <param name="writer">The <see cref="CSharpCodeWriter"/> used to generate code.</param>
        /// <param name="context">The <see cref="CodeGeneratorContext"/>.</param>
        public CSharpTagHelperRunnerInitializationVisitor(CSharpCodeWriter writer,
                                                          CodeGeneratorContext context)
            : base(writer, context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _tagHelperContext = Context.Host.GeneratedClassContext.GeneratedTagHelperContext;
        }

        /// <inheritdoc />
        public override void Accept(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            // If at any ParentChunk other than a TagHelperChunk, then dive into its Children to search for more
            // TagHelperChunk nodes. This method avoids overriding each of the ParentChunk-specific Visit() methods to
            // dive into Children.
            var parentChunk = chunk as ParentChunk;
            if (parentChunk != null && !(parentChunk is TagHelperChunk))
            {
                Accept(parentChunk.Children);
            }
            else
            {
                // If at a TagHelperChunk or any non-ParentChunk, "Accept()" it. This ensures the Visit(TagHelperChunk)
                // method below is called.
                base.Accept(chunk);
            }
        }

        /// <summary>
        /// Writes the TagHelperRunner initialization code to the Writer.
        /// </summary>
        /// <param name="chunk">The <see cref="TagHelperChunk"/>.</param>
        protected override void Visit(TagHelperChunk chunk)
        {
            if (!_foundTagHelpers && !Context.Host.DesignTimeMode)
            {
                _foundTagHelpers = true;

                Writer
                    .WriteStartAssignment(CSharpTagHelperCodeRenderer.RunnerVariableName)
                    .Write(CSharpTagHelperCodeRenderer.RunnerVariableName)
                    .Write(" ?? ")
                    .WriteStartNewObject(_tagHelperContext.RunnerTypeName)
                    .WriteEndMethodInvocation();
            }
        }
    }
}