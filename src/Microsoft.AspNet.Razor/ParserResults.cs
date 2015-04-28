// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

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
        /// <param name="document">The <see cref="Block"/> for the syntax tree.</param>
        /// <param name="tagHelperDescriptors">
        /// The <see cref="TagHelperDescriptor"/>s that apply to the current Razor document.
        /// </param>
        /// <param name="errorSink">
        /// The <see cref="ErrorSink"/> used to collect <see cref="RazorError"/>s encountered when parsing the
        /// current Razor document.
        /// </param>
        public ParserResults([NotNull] Block document,
                             [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                             [NotNull] ErrorSink errorSink)
            : this(!errorSink.Errors.Any(), document, tagHelperDescriptors, errorSink)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="ParserResults"/> instance.
        /// </summary>
        /// <param name="success"><c>true</c> if parsing was successful, <c>false</c> otherwise.</param>
        /// <param name="document">The <see cref="Block"/> for the syntax tree.</param>
        /// <param name="tagHelperDescriptors">
        /// The <see cref="TagHelperDescriptor"/>s that apply to the current Razor document.
        /// </param>
        /// <param name="errorSink">
        /// The <see cref="ErrorSink"/> used to collect <see cref="RazorError"/>s encountered when parsing the
        /// current Razor document.
        /// </param>
        protected ParserResults(bool success,
                                [NotNull] Block document,
                                [NotNull] IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                                [NotNull] ErrorSink errorSink)
        {
            Success = success;
            Document = document;
            TagHelperDescriptors = tagHelperDescriptors;
            ErrorSink = errorSink;
            ParserErrors = errorSink.Errors;
            Prefix = tagHelperDescriptors.FirstOrDefault()?.Prefix;
        }

        /// <summary>
        /// Indicates if parsing was successful (no errors).
        /// </summary>
        /// <value><c>true</c> if parsing was successful, <c>false</c> otherwise.</value>
        public bool Success { get; }

        /// <summary>
        /// The root node in the document's syntax tree.
        /// </summary>
        public Block Document { get; }

        /// <summary>
        /// Used to aggregate <see cref="RazorError"/>s.
        /// </summary>
        public ErrorSink ErrorSink { get; }

        /// <summary>
        /// The list of errors which occurred during parsing.
        /// </summary>
        public IEnumerable<RazorError> ParserErrors { get; }

        /// <summary>
        /// The <see cref="TagHelperDescriptor"/>s found for the current Razor document.
        /// </summary>
        public IEnumerable<TagHelperDescriptor> TagHelperDescriptors { get; }

        /// <summary>
        /// Text used as a required prefix when matching HTML.
        /// </summary>
        public string Prefix { get; }
    }
}
