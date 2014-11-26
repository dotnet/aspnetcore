// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Represents the results of parsing a Razor document
    /// </summary>
    public class ParserResults
    {
        /// <summary>
        /// Instantiates a new <see cref="ParserResults"/> instance.
        /// </summary>
        /// <param name="document">The Razor syntax tree.</param>
        /// <param name="tagHelperDescriptors"><see cref="TagHelperDescriptor"/>s that apply to the current Razor 
        /// document.</param>
        /// <param name="parserErrors"><see cref="RazorError"/>s encountered when parsing the current Razor
        /// document.</param>
        public ParserResults([NotNull] Block document,
                             [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                             [NotNull] IList<RazorError> parserErrors)
            : this(parserErrors == null || parserErrors.Count == 0, document, tagHelperDescriptors, parserErrors)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="ParserResults"/> instance.
        /// </summary>
        /// <param name="success"><c>true</c> if parsing was successful, <c>false</c> otherwise.</param>
        /// <param name="document">The Razor syntax tree.</param>
        /// <param name="tagHelperDescriptors"><see cref="TagHelperDescriptor"/>s that apply to the current Razor 
        /// document.</param>
        /// <param name="errors"><see cref="RazorError"/>s encountered when parsing the current Razor
        /// document.</param>
        protected ParserResults(bool success,
                                [NotNull] Block document,
                                [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                                [NotNull] IList<RazorError> errors)
        {
            Success = success;
            Document = document;
            TagHelperDescriptors = tagHelperDescriptors;
            ParserErrors = errors ?? new List<RazorError>();
        }

        /// <summary>
        /// Indicates if parsing was successful (no errors)
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// The root node in the document's syntax tree
        /// </summary>
        public Block Document { get; private set; }

        /// <summary>
        /// The list of errors which occurred during parsing.
        /// </summary>
        public IList<RazorError> ParserErrors { get; private set; }

        /// <summary>
        /// The <see cref="TagHelperDescriptor"/>s found for the current Razor document.
        /// </summary>
        public IEnumerable<TagHelperDescriptor> TagHelperDescriptors { get; private set; }
    }
}
