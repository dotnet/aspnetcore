// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Contains information needed to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDirectiveDescriptor
    {
        // Internal for testing purposes.
        internal TagHelperDirectiveDescriptor(string directiveText,
                                              TagHelperDirectiveType directiveType)
            : this(directiveText, SourceLocation.Zero, directiveType)
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperDirectiveDescriptor"/>.
        /// </summary>
        /// <param name="directiveText">A <see cref="string"/> used to understand tag helper
        /// <see cref="System.Type"/>s.</param>
        /// <param name="location">The <see cref="SourceLocation"/> of the directive.</param>
        /// <param name="directiveType">The <see cref="TagHelperDirectiveType"/> of this directive.</param>
        public TagHelperDirectiveDescriptor([NotNull] string directiveText,
                                            SourceLocation location,
                                            TagHelperDirectiveType directiveType)
        {
            DirectiveText = directiveText;
            Location = location;
            DirectiveType = directiveType;
        }

        /// <summary>
        /// A <see cref="string"/> used to find tag helper <see cref="System.Type"/>s.
        /// </summary>
        public string DirectiveText { get; }

        /// <summary>
        /// The <see cref="TagHelperDirectiveType"/> of this directive.
        /// </summary>
        public TagHelperDirectiveType DirectiveType { get; }

        /// <summary>
        /// The <see cref="SourceLocation"/> of the directive.
        /// </summary>
        public SourceLocation Location { get; }
    }
}