// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Provides an <see cref="ITagHelper"/>'s target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TargetElementAttribute : Attribute
    {
        public const string ElementCatchAllTarget = TagHelperDescriptorProvider.ElementCatchAllTarget;

        /// <summary>
        /// Instantiates a new instance of the <see cref="TargetElementAttribute"/> class with <see cref="Tag"/>
        /// set to <c>*</c>.
        /// </summary>
        /// <remarks>A <c>*</c> <see cref="Tag"/> value indicates an <see cref="ITagHelper"/>
        /// that targets all HTML elements with the required <see cref="Attributes"/>.</remarks>
        public TargetElementAttribute()
            : this(ElementCatchAllTarget)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TargetElementAttribute"/> class.
        /// </summary>
        /// <param name="tag">
        /// The HTML tag the <see cref="ITagHelper"/> targets.
        /// </param>
        /// <remarks>A <c>*</c> <paramref name="tag"/> value indicates an <see cref="ITagHelper"/>
        /// that targets all HTML elements with the required <see cref="Attributes"/>.</remarks>
        public TargetElementAttribute(string tag)
        {
            Tag = tag;
        }

        /// <summary>
        /// The HTML tag the <see cref="ITagHelper"/> targets.
        /// </summary>
        /// <remarks>A <c>*</c> <see cref="Tag"/> value indicates an <see cref="ITagHelper"/>
        /// that targets all HTML elements with the required <see cref="Attributes"/>.</remarks>
        public string Tag { get; }

        /// <summary>
        /// A comma-separated <see cref="string"/> of attribute names the HTML element must contain for the
        /// <see cref="ITagHelper"/> to run.
        /// </summary>
        /// <remarks>
        /// <c>*</c> at the end of an attribute name acts as a prefix match.
        /// </remarks>
        public string Attributes { get; set; }

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
    }
}