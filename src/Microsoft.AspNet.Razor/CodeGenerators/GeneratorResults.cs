// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    /// <summary>
    /// The results of parsing and generating code for a Razor document.
    /// </summary>
    public class GeneratorResults : ParserResults
    {
        /// <summary>
        /// Instantiates a new <see cref="GeneratorResults"/> instance.
        /// </summary>
        /// <param name="parserResults">The results of parsing a document.</param>
        /// <param name="codeGeneratorResult">The results of generating code for the document.</param>
        /// <param name="chunkTree">A <see cref="ChunkTree"/> for the document.</param>
        public GeneratorResults([NotNull] ParserResults parserResults,
                                [NotNull] CodeGeneratorResult codeGeneratorResult,
                                [NotNull] ChunkTree chunkTree)
            : this(parserResults.Document,
                   parserResults.TagHelperDescriptors,
                   parserResults.ErrorSink,
                   codeGeneratorResult,
                   chunkTree)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="GeneratorResults"/> instance.
        /// </summary>
        /// <param name="document">The <see cref="Block"/> for the syntax tree.</param>
        /// <param name="tagHelperDescriptors">
        /// The <see cref="TagHelperDescriptor"/>s that apply to the current Razor document.
        /// </param>
        /// <param name="errorSink">
        /// The <see cref="ErrorSink"/> used to collect <see cref="RazorError"/>s encountered when parsing the
        /// current Razor document.
        /// </param>
        /// <param name="codeGeneratorResult">The results of generating code for the document.</param>
        /// <param name="chunkTree">A <see cref="ChunkTree"/> for the document.</param>
        public GeneratorResults([NotNull] Block document,
                                [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                                [NotNull] ErrorSink errorSink,
                                [NotNull] CodeGeneratorResult codeGeneratorResult,
                                [NotNull] ChunkTree chunkTree)
            : base(document, tagHelperDescriptors, errorSink)
        {
            GeneratedCode = codeGeneratorResult.Code;
            DesignTimeLineMappings = codeGeneratorResult.DesignTimeLineMappings;
            ChunkTree = chunkTree;
        }

        /// <summary>
        /// The generated code for the document.
        /// </summary>
        public string GeneratedCode { get; }

        /// <summary>
        /// <see cref="LineMapping"/>s used to project code from a file during design time.
        /// </summary>
        public IList<LineMapping> DesignTimeLineMappings { get; }

        /// <summary>
        /// A <see cref="Chunks.ChunkTree"/> for the document.
        /// </summary>
        public ChunkTree ChunkTree { get; }
    }
}
