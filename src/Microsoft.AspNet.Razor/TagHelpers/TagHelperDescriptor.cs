// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A metadata class describing a tag helper.
    /// </summary>
    public class TagHelperDescriptor
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptor"/> class.
        /// </summary>
        /// <param name="tagName">The tag name that the tag helper targets. '*' indicates a catch-all
        /// <see cref="TagHelperDescriptor"/> which applies to every HTML tag.</param>
        /// <param name="tagHelperName">The full name of the tag helper class.</param>
        /// <param name="contentBehavior">The <see cref="TagHelpers.ContentBehavior"/>
        /// of the tag helper.</param>
        public TagHelperDescriptor([NotNull] string tagName,
                                   [NotNull] string tagHelperName,
                                   ContentBehavior contentBehavior)
            : this(tagName, tagHelperName, contentBehavior, Enumerable.Empty<TagHelperAttributeDescriptor>())
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptor"/> class with the given 
        /// <paramref name="attributes"/>.
        /// </summary>
        /// <param name="tagName">The tag name that the tag helper targets. '*' indicates a catch-all
        /// <see cref="TagHelperDescriptor"/> which applies to every HTML tag.</param>
        /// <param name="tagHelperName">The code class used to render the tag helper. Corresponds to
        /// the tag helper's <see cref="Type.FullName"/>.</param>
        /// <param name="contentBehavior">The <see cref="TagHelpers.ContentBehavior"/>
        /// of the tag helper.</param>
        /// <param name="attributes">
        /// The <see cref="TagHelperAttributeDescriptor"/>s to request from the HTML tag.
        /// </param>
        public TagHelperDescriptor([NotNull] string tagName,
                                   [NotNull] string tagHelperName,
                                   ContentBehavior contentBehavior,
                                   [NotNull] IEnumerable<TagHelperAttributeDescriptor> attributes)
        {
            TagName = tagName;
            TagHelperName = tagHelperName;
            ContentBehavior = contentBehavior;
            Attributes = new List<TagHelperAttributeDescriptor>(attributes);
        }

        /// <summary>
        /// The tag name that the tag helper should target.
        /// </summary>
        public string TagName { get; private set; }

        /// <summary>
        /// The full name of the tag helper class.
        /// </summary>
        public string TagHelperName { get; private set; }

        /// <summary>
        /// The <see cref="TagHelpers.ContentBehavior"/> of the tag helper.
        /// </summary>
        public ContentBehavior ContentBehavior { get; private set; }

        /// <summary>
        /// The list of attributes the tag helper expects.
        /// </summary>
        public virtual List<TagHelperAttributeDescriptor> Attributes { get; private set; }
    }
}