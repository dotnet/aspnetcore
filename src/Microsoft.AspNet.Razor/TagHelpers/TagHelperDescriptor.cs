// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

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
        internal TagHelperDescriptor(
            [NotNull] string tagName,
            [NotNull] string typeName,
            [NotNull] string assemblyName)
            : this(
                tagName,
                typeName,
                assemblyName,
                attributes: Enumerable.Empty<TagHelperAttributeDescriptor>())
        {
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal TagHelperDescriptor(
            [NotNull] string tagName,
            [NotNull] string typeName,
            [NotNull] string assemblyName,
            [NotNull] IEnumerable<TagHelperAttributeDescriptor> attributes)
            : this(
                tagName,
                typeName,
                assemblyName,
                attributes,
                requiredAttributes: Enumerable.Empty<string>())
        {
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal TagHelperDescriptor(
            [NotNull] string tagName,
            [NotNull] string typeName,
            [NotNull] string assemblyName,
            [NotNull] IEnumerable<TagHelperAttributeDescriptor> attributes,
            [NotNull] IEnumerable<string> requiredAttributes)
            : this(
                prefix: string.Empty,
                tagName: tagName,
                typeName: typeName,
                assemblyName: assemblyName,
                attributes: attributes,
                requiredAttributes: requiredAttributes,
                designTimeDescriptor: null)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptor"/> class with the given
        /// <paramref name="attributes"/>.
        /// </summary>
        /// <param name="prefix">
        /// Text used as a required prefix when matching HTML start and end tags in the Razor source to available
        /// tag helpers.
        /// </param>
        /// <param name="tagName">The tag name that the tag helper targets. '*' indicates a catch-all
        /// <see cref="TagHelperDescriptor"/> which applies to every HTML tag.</param>
        /// <param name="typeName">The full name of the tag helper class.</param>
        /// <param name="assemblyName">The name of the assembly containing the tag helper class.</param>
        /// <param name="attributes">
        /// The <see cref="TagHelperAttributeDescriptor"/>s to request from the HTML tag.
        /// </param>
        /// <param name="requiredAttributes">
        /// The attribute names required for the tag helper to target the HTML tag.
        /// </param>
        /// <param name="designTimeDescriptor">The <see cref="TagHelperDesignTimeDescriptor"/> that contains design
        /// time information about the tag helper.</param>
        public TagHelperDescriptor(
            string prefix,
            [NotNull] string tagName,
            [NotNull] string typeName,
            [NotNull] string assemblyName,
            [NotNull] IEnumerable<TagHelperAttributeDescriptor> attributes,
            [NotNull] IEnumerable<string> requiredAttributes,
            TagHelperDesignTimeDescriptor designTimeDescriptor)
        {
            Prefix = prefix ?? string.Empty;
            TagName = tagName;
            FullTagName = Prefix + TagName;
            TypeName = typeName;
            AssemblyName = assemblyName;
            Attributes = new List<TagHelperAttributeDescriptor>(attributes);
            RequiredAttributes = new List<string>(requiredAttributes);
            DesignTimeDescriptor = designTimeDescriptor;
        }

        /// <summary>
        /// Text used as a required prefix when matching HTML start and end tags in the Razor source to available
        /// tag helpers.
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// The tag name that the tag helper should target.
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// The full tag name that is required for the tag helper to target an HTML element.
        /// </summary>
        /// <remarks>This is equivalent to <see cref="Prefix"/> and <see cref="TagName"/> concatenated.</remarks>
        public string FullTagName { get; }

        /// <summary>
        /// The full name of the tag helper class.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// The name of the assembly containing the tag helper class.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// The list of attributes the tag helper expects.
        /// </summary>
        public IReadOnlyList<TagHelperAttributeDescriptor> Attributes { get; }

        /// <summary>
        /// The list of required attribute names the tag helper expects to target an element.
        /// </summary>
        /// <remarks>
        /// <c>*</c> at the end of an attribute name acts as a prefix match.
        /// </remarks>
        public IReadOnlyList<string> RequiredAttributes { get; }

        /// <summary>
        /// The <see cref="TagHelperDesignTimeDescriptor"/> that contains design time information about this
        /// tag helper.
        /// </summary>
        public TagHelperDesignTimeDescriptor DesignTimeDescriptor { get; }
    }
}