// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A metadata class describing a tag helper.
    /// </summary>
    public class TagHelperDescriptor
    {
        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal TagHelperDescriptor([NotNull] string tagName,
                                     [NotNull] string typeName,
                                     [NotNull] string assemblyName)
            : this(tagName,
                   typeName,
                   assemblyName,
                   Enumerable.Empty<TagHelperAttributeDescriptor>())
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptor"/> class with the given
        /// <paramref name="attributes"/>.
        /// </summary>
        /// <param name="tagName">The tag name that the tag helper targets. '*' indicates a catch-all
        /// <see cref="TagHelperDescriptor"/> which applies to every HTML tag.</param>
        /// <param name="typeName">The full name of the tag helper class.</param>
        /// <param name="assemblyName">The name of the assembly containing the tag helper class.</param>
        /// <param name="attributes">
        /// The <see cref="TagHelperAttributeDescriptor"/>s to request from the HTML tag.
        /// </param>
        public TagHelperDescriptor([NotNull] string tagName,
                                   [NotNull] string typeName,
                                   [NotNull] string assemblyName,
                                   [NotNull] IEnumerable<TagHelperAttributeDescriptor> attributes)
        {
            TagName = tagName;
            TypeName = typeName;
            AssemblyName = assemblyName;
            Attributes = new List<TagHelperAttributeDescriptor>(attributes);
        }

        /// <summary>
        /// The tag name that the tag helper should target.
        /// </summary>
        public string TagName { get; private set; }

        /// <summary>
        /// The full name of the tag helper class.
        /// </summary>
        public string TypeName { get; private set; }

        /// <summary>
        /// The name of the assembly containing the tag helper class.
        /// </summary>
        public string AssemblyName { get; private set; }

        /// <summary>
        /// The list of attributes the tag helper expects.
        /// </summary>
        public virtual List<TagHelperAttributeDescriptor> Attributes { get; private set; }
    }
}