// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Contains information needed to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDirectiveDescriptor
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperDirectiveDescriptor"/>.
        /// </summary>
        /// <param name="lookupText">A <see cref="string"/> used to find tag helper <see cref="System.Type"/>s.</param>
        /// <param name="directiveType">The <see cref="TagHelperDirectiveType"/> of this directive.</param>
        public TagHelperDirectiveDescriptor([NotNull] string lookupText, TagHelperDirectiveType directiveType)
        {
            LookupText = lookupText;
            DirectiveType = directiveType;
        }

        /// <summary>
        /// A <see cref="string"/> used to find tag helper <see cref="System.Type"/>s.
        /// </summary>
        public string LookupText { get; private set; }

        /// <summary>
        /// The <see cref="TagHelperDirectiveType"/> of this directive.
        /// </summary>
        public TagHelperDirectiveType DirectiveType { get; private set;  }
    }
}