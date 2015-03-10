// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
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
        /// <param name="context">The <see cref="CodeBuilderContext"/>.</param>
        public CSharpTagHelperRunnerInitializationVisitor([NotNull] CSharpCodeWriter writer,
                                                          [NotNull] CodeBuilderContext context)
            : base(writer, context)
        {
            _tagHelperContext = Context.Host.GeneratedClassContext.GeneratedTagHelperContext;
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