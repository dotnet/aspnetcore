// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;

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
                   parserResults.ParserErrors,
                   codeBuilderResult,
                   codeTree)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="GeneratorResults"/> instance.
        /// </summary>
        /// <param name="document">The <see cref="Block"/> for the syntax tree.</param>
        /// <param name="tagHelperDescriptors"><see cref="TagHelperDescriptor"/>s for the document.</param>
        /// <param name="parserErrors"><see cref="RazorError"/>s encountered when parsing the document.</param>
        /// <param name="codeBuilderResult">The results of generating code for the document.</param>
        /// <param name="codeTree">A <see cref="CodeTree"/> for the document.</param>
        public GeneratorResults([NotNull] Block document,
                                [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                                [NotNull] IList<RazorError> parserErrors,
                                [NotNull] CodeBuilderResult codeBuilderResult,
                                [NotNull] CodeTree codeTree)
            : this(parserErrors.Count == 0, document, tagHelperDescriptors, parserErrors, codeBuilderResult, codeTree)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="GeneratorResults"/> instance.
        /// </summary>
        /// <param name="success"><c>true</c> if parsing was successful, <c>false</c> otherwise.</param>
        /// <param name="document">The <see cref="Block"/> for the syntax tree.</param>
        /// <param name="tagHelperDescriptors"><see cref="TagHelperDescriptor"/>s for the document.</param>
        /// <param name="parserErrors"><see cref="RazorError"/>s encountered when parsing the document.</param>
        /// <param name="codeBuilderResult">The results of generating code for the document.</param>
        /// <param name="codeTree">A <see cref="CodeTree"/> for the document.</param>
        protected GeneratorResults(bool success,
                                   [NotNull] Block document,
                                   [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                                   [NotNull] IList<RazorError> parserErrors,
                                   [NotNull] CodeBuilderResult codeBuilderResult,
                                   [NotNull] CodeTree codeTree)
            : base(success, document, tagHelperDescriptors, parserErrors)
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
