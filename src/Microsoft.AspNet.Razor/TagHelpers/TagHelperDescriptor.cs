// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A metadata class describing a tag helper.
    /// </summary>
    public class TagHelperDescriptor
    {
        /// <summary>
        /// Text used as a required prefix when matching HTML start and end tags in the Razor source to available
        /// tag helpers.
        /// </summary>
        public string Prefix { get; [param: NotNull] set; } = string.Empty;

        /// <summary>
        /// The tag name that the tag helper should target.
        /// </summary>
        public string TagName { get; [param: NotNull] set; }

        /// <summary>
        /// The full tag name that is required for the tag helper to target an HTML element.
        /// </summary>
        /// <remarks>This is equivalent to <see cref="Prefix"/> and <see cref="TagName"/> concatenated.</remarks>
        public string FullTagName
        {
            get
            {
                return Prefix + TagName;
            }
        }

        /// <summary>
        /// The full name of the tag helper class.
        /// </summary>
        public string TypeName { get; [param: NotNull] set; }

        /// <summary>
        /// The name of the assembly containing the tag helper class.
        /// </summary>
        public string AssemblyName { get; [param: NotNull] set; }

        /// <summary>
        /// The list of attributes the tag helper expects.
        /// </summary>
        public IEnumerable<TagHelperAttributeDescriptor> Attributes { get; [param: NotNull] set; }
            = Enumerable.Empty<TagHelperAttributeDescriptor>();

        /// <summary>
        /// The list of required attribute names the tag helper expects to target an element.
        /// </summary>
        /// <remarks>
        /// <c>*</c> at the end of an attribute name acts as a prefix match.
        /// </remarks>
        public IEnumerable<string> RequiredAttributes { get; [param: NotNull] set; } = Enumerable.Empty<string>();

        /// <summary>
        /// Get the names of elements allowed as children. Tag helpers must target all such elements.
        /// </summary>
        /// <remarks><c>null</c> indicates all children are allowed.</remarks>
        public IEnumerable<string> AllowedChildren { get; set; }

        /// <summary>
        /// The expected tag structure.
        /// </summary>
        /// <remarks>
        /// If <see cref="TagStructure.Unspecified"/> and no other tag helpers applying to the same element specify
        /// their <see cref="TagStructure"/> the <see cref="TagStructure.NormalOrSelfClosing"/> behavior is used:
        /// <para>
        /// <code>
        /// &lt;my-tag-helper&gt;&lt;/my-tag-helper&gt;
        /// &lt;!-- OR --&gt;
        /// &lt;my-tag-helper /&gt;
        /// </code>
        /// Otherwise, if another tag helper applying to the same element does specify their behavior, that behavior
        /// is used.
        /// </para>
        /// <para>
        /// If <see cref="TagStructure.WithoutEndTag"/> HTML elements can be written in the following formats:
        /// <code>
        /// &lt;my-tag-helper&gt;
        /// &lt;!-- OR --&gt;
        /// &lt;my-tag-helper /&gt;
        /// </code>
        /// </para>
        /// </remarks>
        public TagStructure TagStructure { get; set; }

        /// <summary>
        /// The <see cref="TagHelperDesignTimeDescriptor"/> that contains design time information about this
        /// tag helper.
        /// </summary>
        public TagHelperDesignTimeDescriptor DesignTimeDescriptor { get; set; }
    }
}