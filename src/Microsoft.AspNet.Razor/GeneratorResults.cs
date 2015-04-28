// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor
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
        /// <param name="codeBuilderResult">The results of generating code for the document.</param>
        /// <param name="codeTree">A <see cref="CodeTree"/> for the document.</param>
        public GeneratorResults([NotNull] ParserResults parserResults,
                                [NotNull] CodeBuilderResult codeBuilderResult,
                                [NotNull] CodeTree codeTree)
            : this(parserResults.Document,
                   parserResults.TagHelperDescriptors,
                   parserResults.ErrorSink,
                   codeBuilderResult,
                   codeTree)
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
        /// <param name="codeBuilderResult">The results of generating code for the document.</param>
        /// <param name="codeTree">A <see cref="CodeTree"/> for the document.</param>
        public GeneratorResults([NotNull] Block document,
                                [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                                [NotNull] ErrorSink errorSink,
                                [NotNull] CodeBuilderResult codeBuilderResult,
                                [NotNull] CodeTree codeTree)
            : base(document, tagHelperDescriptors, errorSink)
        {
            GeneratedCode = codeBuilderResult.Code;
            DesignTimeLineMappings = codeBuilderResult.DesignTimeLineMappings;
            CodeTree = codeTree;
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
        /// A <see cref="Generator.Compiler.CodeTree"/> for the document.
        /// </summary>
        public CodeTree CodeTree { get; }
    }
}
